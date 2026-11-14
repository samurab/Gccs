import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import test from 'node:test';

const workflow = readFileSync(new URL('../../.github/workflows/staging.yml', import.meta.url), 'utf8');
const steps = [...workflow.matchAll(/^      - name: (.+)\n([\s\S]*?)(?=^      - name: |^  [a-z][a-z-]*:|$(?![\s\S]))/gm)]
  .map(match => ({ name: match[1], body: match[2] }));
const step = name => {
  const matches = steps.filter(item => item.name === name);
  assert.equal(matches.length, 1, name);
  return matches[0].body;
};
function pythonBlock(name, delimiter) {
  const body = step(name);
  const start = body.indexOf(`<<'${delimiter}'\n`);
  assert.notEqual(start, -1);
  const script = body.slice(start + delimiter.length + 5).split(`          ${delimiter}\n`)[0];
  return script.split('\n').map(line => line.startsWith('          ') ? line.slice(10) : line).join('\n');
}
const operation = pythonBlock('Validate closed staging operation', 'PY_OPERATION');
function runOperation(changes = {}) {
  const env = {
    PATH: process.env.PATH, LANG: 'C', LC_ALL: 'C',
    SELECTED_OPERATION: 'api-artifact-restoration-exercise', GITHUB_EVENT_NAME: 'workflow_dispatch',
    GITHUB_REF: 'refs/heads/release-main', SELECTED_CANDIDATE: 'v0.2.1-rc.1',
    SELECTED_TARGET: 'a'.repeat(64), EXERCISE_AUTHORIZATION_B64: 'e30=',
    EXERCISE_AUTHORIZATION_SHA256: 'b'.repeat(64), ...changes,
  };
  return spawnSync(process.env.FEDRIL_TEST_PYTHON ?? 'python3', ['-I', '-B', '-c', operation],
    { env, encoding: 'utf8', timeout: 5000, maxBuffer: 65536 });
}

test('normal deployment remains the default and accepts its existing context', () => {
  assert.match(workflow, /default: normal-deployment/);
  assert.equal(runOperation({ SELECTED_OPERATION: 'normal-deployment' }).status, 0);
});

test('exercise operation permits only manual release-main and exact candidate/target declarations', () => {
  assert.equal(runOperation().status, 0);
  for (const changes of [
    { SELECTED_OPERATION: 'unknown' }, { GITHUB_EVENT_NAME: 'push' },
    { GITHUB_REF: 'refs/heads/other' }, { SELECTED_CANDIDATE: 'v0.2.0-rc.1' },
    { SELECTED_TARGET: '' }, { EXERCISE_AUTHORIZATION_B64: '' },
    { EXERCISE_AUTHORIZATION_SHA256: 'bad' },
  ]) assert.notEqual(runOperation(changes).status, 0, JSON.stringify(changes));
});

test('exercise authority is prepared before Azure login and restoration phases stay ordered', () => {
  const names = steps.map(item => item.name);
  assert.ok(names.indexOf('Authenticate exact current-attempt protected admission') < names.indexOf('Exercise prepare'));
  assert.ok(names.indexOf('Bind evaluated staging target before Azure operations') < names.indexOf('Exercise prepare'));
  assert.ok(names.indexOf('Exercise prepare') < names.indexOf('Login to Azure staging subscription'));
  assert.deepEqual(names.filter(name => name.startsWith('Exercise ')), [
    'Exercise prepare', 'Exercise baseline', 'Exercise stop-B', 'Exercise deploy exact API B',
    'Exercise verify-B', 'Exercise start-B', 'Exercise stop-A', 'Exercise deploy exact API A',
    'Exercise verify-A', 'Exercise start-A',
  ]);
  for (const item of steps.filter(item => item.name.startsWith('Exercise '))) {
    assert.match(item.body, /if: \$\{\{ inputs.operation == 'api-artifact-restoration-exercise' \}\}/);
  }
  for (const name of ['Exercise deploy exact API B', 'Exercise deploy exact API A']) {
    assert.match(step(name), /timeout-minutes: 10/);
    assert.match(step(name), /restart: false/);
  }
});

test('exercise never enters normal migration, settings, web or release-publication steps', () => {
  for (const name of ['Apply staging migrations through approved CI/CD', 'Configure staging runtime dependencies',
    'Deploy staging API App Service', 'Start staging API with cleanup held', 'Deploy staging Static Web App']) {
    assert.match(step(name), /if: \$\{\{ inputs.operation == 'normal-deployment' \}\}/);
  }
  assert.match(workflow.slice(workflow.indexOf('  publish-candidate:')), /inputs.operation == 'normal-deployment'/);
});

test('inline controller is parseable without running its provider or file operations', () => {
  const writer = pythonBlock('Create exact exercise controller and authorization exclusively', 'PY_EXERCISE_CONTROLLER');
  const check = `import ast,sys\nmodule=ast.parse(sys.stdin.read())\nvalues=[node.value for node in ast.walk(module) if isinstance(node,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='controller' for t in node.targets)]\nassert len(values)==1\nsource=ast.literal_eval(values[0])\ncompile(source,'<inline-exercise-controller>','exec')\n`;
  const result = spawnSync(process.env.FEDRIL_TEST_PYTHON ?? 'python3', ['-I', '-B', '-c', check],
    { input: writer, encoding: 'utf8', timeout: 5000, maxBuffer: 65536 });
  assert.equal(result.status, 0, result.stderr);
});
