import { execFile } from 'node:child_process';
import { createHash } from 'node:crypto';
import { lstat, mkdir, open, readFile, realpath, readdir } from 'node:fs/promises';
import { join, resolve } from 'node:path';

const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const fail = () => { throw new Error('controlled_git_operation_failed_reconcile_no_retry'); };
const sha = value => typeof value === 'string' && /^[a-f0-9]{40}$/.test(value);
export const gitEnvironment = Object.freeze({ PATH: '/usr/bin:/bin', LANG: 'C', LC_ALL: 'C', TZ: 'UTC',
  GIT_CONFIG_NOSYSTEM: '1', GIT_CONFIG_SYSTEM: '/dev/null', GIT_CONFIG_GLOBAL: '/dev/null',
  GIT_ATTR_NOSYSTEM: '1', GIT_NO_REPLACE_OBJECTS: '1', GIT_TERMINAL_PROMPT: '0', GIT_OPTIONAL_LOCKS: '0' });

export function processGit(binary, args, options) {
  return new Promise((accept, reject) => execFile(binary, args, { ...options, encoding: 'buffer' }, (error, stdout, stderr) => {
    if (error && (!Number.isInteger(error.code) || error.killed)) { reject(new Error('bounded_git_process_uncertain')); return; }
    accept({ exitCode: error ? error.code : 0, stdout, stderr });
  }));
}

export function controlledGitArgv(root, argv) {
  if (!Array.isArray(argv) || argv[0] !== 'git' || argv.some(value => typeof value !== 'string' || value.includes('\0'))) fail();
  return ['--no-replace-objects', '-c', `core.hooksPath=${join(root, 'empty-hooks')}`, '-c', 'core.attributesFile=/dev/null',
    '-c', 'credential.helper=', '-c', 'gc.auto=0', '-c', 'maintenance.auto=false', '-c', 'core.logAllRefUpdates=false',
    '-c', 'fetch.fsckObjects=true', ...argv.slice(1)];
}

export function createNativeCheckout({ trustedRoot, workspace, repository, workflowSha, candidateAtWorkspace = false, runProcess = processGit }) {
  if (!sha(workflowSha) || !/^[A-Za-z0-9][A-Za-z0-9-]*\/[A-Za-z0-9][A-Za-z0-9._-]*$/.test(repository)) fail();
  if (typeof candidateAtWorkspace !== 'boolean') fail();
  const root = join(resolve(trustedRoot), 'native'), workflow = join(root, 'workflow'),
    candidate = candidateAtWorkspace ? resolve(workspace) : join(resolve(workspace), 'fedril-source');
  let binary, log, attempted = false, sequence = 0;
  const absent = async path => {
    try { await lstat(path); fail(); } catch (error) { if (error.code !== 'ENOENT') fail(); }
  };
  const candidateAvailable = async () => {
    if (!candidateAtWorkspace) { await absent(candidate); return; }
    const stat = await lstat(candidate);
    if (!stat.isDirectory() || stat.isSymbolicLink() || stat.uid !== process.getuid() || (await readdir(candidate)).length !== 0) fail();
  };
  const record = async value => { await log.write(JSON.stringify(value) + '\n'); await log.sync(); };
  const runGit = async argv => {
    if (!binary || !log) fail();
    const args = controlledGitArgv(root, argv), n = ++sequence;
    await record({ n, phase: 'intent', binary, argv: args });
    let result;
    try { result = await runProcess(binary, args, { cwd: workflow, env: gitEnvironment, timeout: 30000, maxBuffer: 64 * 1024 * 1024 }); }
    catch { await record({ n, phase: 'uncertain_reconcile_no_retry' }); fail(); }
    if (!result || !Number.isInteger(result.exitCode) || !Buffer.isBuffer(result.stdout) || !Buffer.isBuffer(result.stderr)) fail();
    await record({ n, phase: 'result', exitCode: result.exitCode, stdoutBytes: result.stdout.length,
      stdoutSha256: hash(result.stdout), stderrBytes: result.stderr.length, stderrSha256: hash(result.stderr) });
    return { exitCode: result.exitCode, stdout: Buffer.from(result.stdout) };
  };
  const requireGit = async argv => { const result = await runGit(argv); if (result.exitCode !== 0) fail(); return result.stdout; };
  return {
    workflowPath: workflow, candidatePath: candidate, runGit,
    async checkoutExact(config) {
      if (attempted || config.repository !== repository || config.ref !== workflowSha || config['fetch-depth'] !== 1 ||
          config['fetch-tags'] !== false || config.submodules !== false || config.clean !== false || config['persist-credentials'] !== false) fail();
      attempted = true;
      await absent(root); await candidateAvailable();
      await mkdir(root, { mode: 0o700 });
      await mkdir(join(root, 'empty-hooks'), { mode: 0o700 }); await mkdir(join(root, 'empty-template'), { mode: 0o700 });
      await mkdir(workflow, { mode: 0o700 });
      log = await open(join(root, 'commands.jsonl'), 'wx', 0o600);
      binary = await realpath('/usr/bin/git');
      const stat = await lstat(binary); if (!stat.isFile() || stat.size > 64 * 1024 * 1024) fail();
      const binarySha256 = hash(await readFile(binary));
      const version = (await requireGit(['git', '--version'])).toString('utf8').trim();
      await record({ phase: 'git_observation', binary, binarySha256, version });
      await requireGit(['git', 'init', '--object-format=sha1', '--initial-branch=fedril-bootstrap', `--template=${join(root, 'empty-template')}`, workflow]);
      const ref = `refs/fedril/bootstrap/${workflowSha}`;
      await requireGit(['git', 'fetch', '--depth=1', '--no-tags', '--no-prune', '--no-prune-tags', '--no-recurse-submodules',
        '--no-auto-maintenance', '--no-write-fetch-head', '--no-write-commit-graph', '--refmap=',
        `https://github.com/${repository}.git`, `${workflowSha}:${ref}`]);
      await requireGit(['git', 'checkout', '--detach', '--no-guess', workflowSha]);
      const headSha = (await requireGit(['git', 'rev-parse', '--verify', 'HEAD'])).toString().trim();
      const shallow = (await requireGit(['git', 'rev-parse', '--is-shallow-repository'])).toString().trim();
      if (headSha !== workflowSha || !['true', 'false'].includes(shallow)) fail();
      return { repository, headSha, shallow: shallow === 'true' };
    },
    async materializeCandidate(candidateSha, candidateTreeSha) {
      if (!sha(candidateSha) || !sha(candidateTreeSha)) fail();
      await candidateAvailable();
      await requireGit(['git', 'worktree', 'add', '--detach', candidate, candidateSha]);
      const head = (await requireGit(['git', '-C', candidate, 'rev-parse', '--verify', 'HEAD'])).toString().trim();
      const tree = (await requireGit(['git', '-C', candidate, 'rev-parse', '--verify', 'HEAD^{tree}'])).toString().trim();
      if (head !== candidateSha || tree !== candidateTreeSha) fail();
      return candidate;
    },
    async close() { if (log) await log.close(); },
  };
}
