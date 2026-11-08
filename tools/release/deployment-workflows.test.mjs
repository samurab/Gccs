import test from "node:test";
import assert from "node:assert/strict";
import { readFile, readdir } from "node:fs/promises";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { createHash } from "node:crypto";
import { bootstrapFromEnvironment } from "./protected/inline-bootstrap.mjs";
import { EXPECTED_HELPERS } from "./protected/admission.mjs";
import { bindWorkflowBootstrap } from "./protected/workflow-bootstrap-binding.mjs";
import { controlledGitArgv, gitEnvironment } from "./protected/native-checkout.mjs";

const readWorkflow = name => readFile(new URL(`../../.github/workflows/${name}`, import.meta.url), "utf8");
const staging = await readWorkflow("staging.yml");
const production = await readWorkflow("production-release.yml");
const rollback = await readWorkflow("production.yml");
const previews = await readWorkflow("azure-static-web-apps-mango-rock-016ff040f.yml");
const ci = await readWorkflow("ci.yml");
const readTool = name => readFile(new URL(name, import.meta.url), "utf8");
const bootstrapSource = await readTool("./protected/inline-bootstrap.mjs");
const runnerSource = await readTool("./protected/runner.mjs");
const compositionSource = await readTool("./protected/composition.mjs");
const finiteSource = await readTool("./protected/finite-canonical-transport.mjs");
const nativeSource = await readTool("./protected/native-checkout.mjs");
const artifactHistorySource = await readTool("../security/finite-artifact-history.mjs");
const snapshotSource = await readTool("./protected/migration-source-snapshots.mjs");

// These checks deliberately target the checked-in workflow structure; they do
// not attempt to implement a YAML parser or execute GitHub expression syntax.
const topSection = (text, name) => {
  const match = text.match(new RegExp(`^${name}:\\n([\\s\\S]*?)(?=^[A-Za-z][A-Za-z_-]*:|$(?![\\s\\S]))`, "m"));
  assert.ok(match, `Missing ${name} section`);
  return match[1];
};
const deploymentJob = (text, name) => {
  const match = text.match(new RegExp(`^  ${name}:\\n([\\s\\S]*?)(?=^  [a-z][a-z_-]*:\\n|$(?![\\s\\S]))`, "m"));
  assert.ok(match, `Missing ${name} job`);
  return match[1];
};

const assertBefore = (text, earlier, later) => {
  const first = text.indexOf(earlier), second = text.indexOf(later);
  assert.ok(first >= 0 && second >= 0 && first < second, `${earlier} must precede ${later}`);
};

const namedStep = (text, name) => {
  const start = text.indexOf(`      - name: ${name}\n`);
  assert.ok(start >= 0, `Missing ${name} step`);
  const end = text.indexOf("      - name:", start + 1);
  return text.slice(start, end < 0 ? undefined : end);
};

for (const [environment, workflow, migrationName, apiName] of [
  ["staging", staging, "Apply staging migrations through approved CI/CD", "Deploy staging API App Service"],
  ["production", production, "Apply the staged migration artifact", "Deploy the staged API artifact"]
]) {
  test(`${environment} fences migrations and API startup with cleanup disabled`, () => {
    const migration = namedStep(workflow, migrationName);
    const api = namedStep(workflow, apiName);
    const start = namedStep(workflow, `Start ${environment} API with cleanup held`);
    const login = workflow.indexOf(`      - name: Login to Azure ${environment} subscription`);
    assert.ok(login >= 0 && login < workflow.indexOf(`      - name: ${migrationName}`));
    assert.ok(migration.indexOf(` ${environment} Stopped`) < migration.indexOf("psql "));
    assert.ok(migration.includes(` ${environment} Stopped`), "Missing pre-SQL cleanup gate");
    assert.match(api, /^          restart: false$/m);
    assert.ok(start.indexOf(` ${environment} Stopped`) >= 0 &&
      start.indexOf(` ${environment} Stopped`) < start.indexOf("az webapp start"));
    assert.ok(start.indexOf(` ${environment} Running`) > start.indexOf("az webapp start"));
    assert.match(start, /az webapp start --subscription "\$DEPLOYMENT_AZURE_SUBSCRIPTION_ID"/);
    assert.doesNotMatch(start, /if:|always\(\)|continue-on-error/);
    assert.doesNotMatch(workflow, /ObjectCleanupProcessing(?:__|:)Enabled\s*=\s*true/i);
    assert.match(workflow, /^      DEPLOYMENT_AZURE_SUBSCRIPTION_ID: \$\{\{ vars\.AZURE_SUBSCRIPTION_ID \}\}$/m);
  });

  test(`${environment} guard failure executes neither SQL nor API start`, () => {
    for (const stepName of [migrationName, `Start ${environment} API with cleanup held`]) {
      const step = namedStep(workflow, stepName);
      const body = step.split("        run: |\n")[1].split("\n").map(line => line.replace(/^          /, "")).join("\n");
      const stub = 'sudo() { return 0; }\nnode() { return 17; }\npsql() { printf MIGRATION_BYPASSED; }\naz() { printf START_BYPASSED; }\n';
      const result = spawnSync("/bin/bash", ["--noprofile", "--norc", "-e", "-c", stub + body], {
        env: { PATH: "/usr/bin:/bin", RUNNER_TEMP: "/synthetic-runner", STAGING_DATABASE_URL: "synthetic", PRODUCTION_DATABASE_URL: "synthetic" },
        encoding: "utf8", timeout: 5000, maxBuffer: 64 * 1024
      });
      assert.ifError(result.error);
      assert.equal(result.signal, null);
      assert.equal(result.status, 17);
      assert.equal(result.stdout, "");
      assert.equal(result.stderr, "");
    }
  });
}

test("staging retains the externally pinned cleanup guard before candidate preparation", () => {
  const bootstrap = staging.indexOf("const admissionResult = await bootstrapFromEnvironment(process.env)");
  const prepare = staging.indexOf("      - name: Prepare verified workflow and candidate sources with snapshot comparison");
  assert.ok(bootstrap >= 0 && prepare > bootstrap);
  assert.ok(EXPECTED_HELPERS.includes("tools/release/check-cleanup-quiescence.mjs"));
  assertBefore(bootstrapSource, "admission.hash(bytes) !== selected.carrier.helperSha256[path]", "await put(destination, bytes");
  assert.match(bootstrapSource, /raw\(repository, workflowSha, path, 1024 \* 1024\)/);
  assert.match(bootstrapSource, /await put\(destination, bytes, \{ flag: 'wx', mode: 0o600 \}\)/);
  assert.match(staging, /cleanup_checker="\$FEDRIL_TRUSTED_ROOT\/tools\/release\/check-cleanup-quiescence\.mjs"/);
  assert.match(staging, /node "\$cleanup_checker" staging Stopped/);
  assert.doesNotMatch(staging, /cp .*check-cleanup-quiescence|git checkout --detach/);
});

test("trusted bootstrap preserves existing closure roots and helper destinations", async () => {
  const bytes = Buffer.from("synthetic helper"), hash = createHash("sha256").update(bytes).digest("hex");
  const env = { GITHUB_REPOSITORY: "owner/repository", GITHUB_REPOSITORY_ID: "1", GITHUB_SHA: "a".repeat(40),
    GITHUB_EVENT_NAME: "push", GITHUB_REF: "refs/heads/release-main", GITHUB_RUN_ID: "2", GITHUB_RUN_ATTEMPT: "1",
    FEDRIL_WORKFLOW_SHA: "a".repeat(40), FEDRIL_WORKFLOW_REF: "owner/repository/.github/workflows/ci.yml@refs/heads/release-main",
    FEDRIL_MODE: "ci", PROTECTED_ASSERTIONS_APP_ID: "3", FEDRIL_COMPARISON_BASE_SHA: "0".repeat(40),
    RUNNER_TEMP: "/synthetic-temp", GITHUB_ENV: "/synthetic-env", GH_TOKEN: "synthetic-token" };
  for (const collision of ["root", "helper"]) {
    const calls = [];
    await assert.rejects(bootstrapFromEnvironment(env, {
      read: async () => ({ sha: env.GITHUB_SHA, commit: { tree: { sha: "b".repeat(40) } } }),
      wait: async () => ({ checkRunId: 4, carrier: { helperSha256: Object.fromEntries(EXPECTED_HELPERS.map(path => [path, hash])) } }),
      raw: async () => { calls.push("raw"); return bytes; },
      makeDirectory: async (path, options) => {
        calls.push(["mkdir", path, options]);
        if (collision === "root") { assert.equal(options.recursive, undefined); throw new Error("synthetic EEXIST"); }
      },
      put: async (path, value, options) => {
        assert.equal(options.flag, "wx"); calls.push(["put", path]);
        if (path.endsWith(EXPECTED_HELPERS[0])) throw new Error("synthetic EEXIST");
      },
      append: async () => { assert.fail("A collision must not publish the trusted root"); },
    }), /protected_bootstrap_held_reconcile_no_retry/);
    if (collision === "root") assert.equal(calls.length, 1);
    else {
      assert.equal(calls.filter(call => call === "raw").length, 1);
      assert.equal(calls.filter(call => Array.isArray(call) && call[0] === "put").length, 2);
    }
  }
});

test("staging admits source before acquisition and exact-ref artifact eligibility before candidate execution", () => {
  const prepare = staging.indexOf("      - name: Prepare verified workflow and candidate sources with snapshot comparison");
  const gate = staging.indexOf('node "$FEDRIL_TRUSTED_ROOT/tools/release/release-evidence.mjs" artifact-policy');
  assertBefore(staging, "const admissionResult = await bootstrapFromEnvironment(process.env)", "      - name: Prepare verified workflow and candidate sources with snapshot comparison");
  assert.ok(gate > prepare && gate < staging.indexOf("dotnet restore") && gate < staging.indexOf("npm ci"));
  assert.ok(gate < staging.indexOf("Login to Azure staging subscription"));
  assert.match(staging, /artifact-policy "\$GITHUB_REPOSITORY" "\$GITHUB_REPOSITORY_ID" "\$commit_sha" "refs\/tags\/\$candidate_tag"/);
  assert.match(staging, /artifact-policy "\$GITHUB_REPOSITORY" "\$GITHUB_REPOSITORY_ID" "\$commit_sha" "\$GITHUB_REF"/);
  assertBefore(runnerSource, "const selected = await reconcileExactAdmission", "await native.materializeCandidate");
  assertBefore(compositionSource, "plan = helpers.planCanonicalTransport", "await callbacks.checkoutExact");
  assert.doesNotMatch(runnerSource, /execFile|spawn|npm ci|dotnet build/);
});

test("production binds artifact policy and verified provenance to the approved staging run before mutations", () => {
  const runGate = production.indexOf('node "$FEDRIL_TRUSTED_ROOT/tools/release/release-evidence.mjs" staging-run');
  const provenanceGate = production.indexOf('node "$FEDRIL_TRUSTED_ROOT/tools/release/release-evidence.mjs" attestation');
  const download = production.indexOf("gh release download");
  assert.ok(runGate >= 0 && runGate < download, "Validate the live staging run before download");
  assert.ok(provenanceGate > runGate && provenanceGate < production.indexOf("unzip -q"), "Validate provenance before extraction");
  assert.match(production, /artifact-policy "\$GITHUB_REPOSITORY" "\$GITHUB_REPOSITORY_ID" "\$commit_sha" "refs\/tags\/\$tag"/);
  assert.match(production, /commits\/\$\{staging_workflow_sha\}/);
  assertBefore(runnerSource, "validateStagingRun(document.stagingRun", "await prepareTrustedWorkflow");
  assert.match(compositionSource, /source\.mode === 'production' && source\.trustedStagingWorkflowSha === null/);
  assert.match(finiteSource, /stagingWorkflowSha/);
  assert.doesNotMatch(production, /git fetch .*--tags|origin\/main/);
  for (const workflow of [staging, production]) assert.match(workflow, /^      actions: read$/m);
});

test("CI jobs retain read-only contents and only admission consumers gain read-only check/action access", () => {
  assert.equal(topSection(ci, "permissions").trim(), "contents: read");
  const jobs = [...topSection(ci, "jobs").matchAll(/^  ([A-Za-z_][A-Za-z0-9_-]*):$/gm)].map(match => match[1]);
  assert.deepEqual(jobs, ["protected-admission", "backend-prerequisites", "backend-tests", "backend", "frontend", "real-stack-critical-uat", "secret-scan"]);
  for (const name of jobs) {
    const job = deploymentJob(ci, name);
    if (["protected-admission", "secret-scan"].includes(name)) {
      const permissions = job.match(/^    permissions:\n((?:      [^\n]+\n)*)/m)?.[1];
      assert.equal(permissions?.trim(), "contents: read\n      checks: read\n      actions: read");
    } else assert.doesNotMatch(job, /^    permissions:/m);
    assert.doesNotMatch(job, /^      (?:contents|checks|actions|id-token): write$/m);
  }
  for (const name of ["backend-prerequisites", "backend-tests", "secret-scan"]) assert.match(deploymentJob(ci, name), /^    needs: protected-admission$/m);
  assert.doesNotMatch(deploymentJob(ci, "protected-admission"), /^    if:|continue-on-error/m);
});

test("third-party workflow actions use full immutable commits", async () => {
  const files = (await readdir(new URL("../../.github/workflows/", import.meta.url))).filter(name => /\.ya?ml$/.test(name));
  let uses = 0;
  for (const file of files) {
    for (const [, reference] of (await readWorkflow(file)).matchAll(/^\s*(?:-\s*)?uses:\s*([^\s#]+)/gm)) {
      assert.match(reference, /^[\w.-]+\/[\w.-]+(?:\/[\w./-]+)?@[a-f0-9]{40}$/, `${file}: ${reference} must use a full commit SHA`);
      uses += 1;
    }
  }
  assert.ok(uses > 0, "Expected third-party workflow action references");
});

for (const [name, workflow, jobName, consumers] of [
  ["staging", staging, "staging-deploy", ["Validate staging No-CUI and no production data guardrails", "Configure staging runtime dependencies"]],
  ["production", production, "production-deploy", ["Validate production CI/CD and No-CUI deployment guardrails", "Configure production services and release identity"]],
  ["legacy rollback", rollback, "production-deploy", ["Validate production CI/CD and No-CUI deployment guardrails", "Configure production email delivery"]]
]) {
  test(`${name} scopes application secrets to their consuming steps`, () => {
    const secretNames = ["DEMO_REQUEST_FOLLOW_UP_TOKEN_SIGNING_KEY", "HUBSPOT_PRIVATE_APP_TOKEN", "TASK_SEARCH_CURSOR_SIGNING_KEY", "TASK_SEARCH_PREVIOUS_CURSOR_SIGNING_KEY"];
    const keys = secretNames.filter(key => name !== "staging" || key !== "HUBSPOT_PRIVATE_APP_TOKEN");
    const job = deploymentJob(workflow, jobName);
    const jobEnv = job.match(/^    env:\n([\s\S]*?)^    steps:\n/m)?.[1];
    assert.ok(jobEnv, "Missing job environment");
    assert.doesNotMatch(jobEnv, /secrets\./, "Build and setup steps must not inherit application secrets");
    const observedConsumers = [];
    for (const [, stepName, body] of job.matchAll(/^      - (?:name: ([^\n]+)\n)?([\s\S]*?)(?=^      - |$(?![\s\S]))/gm)) {
      const bindings = [...body.matchAll(/^          ([A-Z_]+): \$\{\{ secrets\.([A-Z_]+) \}\}$/gm)]
        .filter(match => secretNames.includes(match[1]) || secretNames.includes(match[2]));
      if (consumers.includes(stepName)) {
        assert.deepEqual(bindings.map(match => [match[1], match[2]]), keys.map(key => [key, key]), `${stepName} retains all original bindings, including the optional previous key`);
        observedConsumers.push(stepName);
      } else {
        assert.equal(bindings.length, 0, `${stepName} must not receive application secrets`);
      }
    }
    assert.deepEqual(observedConsumers, consumers);
  });
}

test("canonical publication does not deploy staging; preview workflow is PR-only", () => {
  const stagingEvents = topSection(staging, "on");
  assert.match(stagingEvents, /^  workflow_dispatch:/m);
  assert.doesNotMatch(stagingEvents, /^  (push|pull_request|pull_request_target|workflow_run):/m);
  const events = topSection(previews, "on");
  assert.match(events, /^  pull_request:/m);
  assert.doesNotMatch(events, /^  (push|workflow_dispatch|pull_request_target|workflow_run):/m);
  assert.match(previews, /github\.event_name == 'pull_request'/);
  assert.match(previews, /github\.event\.pull_request\.head\.repo\.full_name == github\.repository/);
});

test("staging serializes all manual candidates in one deployment group", () => {
  const concurrency = topSection(staging, "concurrency");
  assert.match(concurrency, /^  group: staging-deployment$/m);
  assert.match(concurrency, /^  cancel-in-progress: false$/m);
  assert.doesNotMatch(concurrency, /\$\{\{/);
});

test("production promotion and rollback cannot run concurrently for different tags", () => {
  for (const workflow of [production, rollback]) {
    const concurrency = topSection(workflow, "concurrency");
    assert.match(concurrency, /^  group: production-deployment$/m);
    assert.match(concurrency, /^  cancel-in-progress: false$/m);
    assert.doesNotMatch(concurrency, /\$\{\{/);
  }
});

test("production attestations name their expected signer workflows", async () => {
  const commands = [...production.matchAll(/^ +gh attestation verify ([^\n]+)$/gm)].map(match => match[1]);
  assert.deepEqual(commands, [
    '"$APPROVED_RELEASE_MANIFEST" --repo "$GITHUB_REPOSITORY" --signer-workflow "$GITHUB_REPOSITORY/.github/workflows/approved-release-attestation.yml"',
    '"$RUNNER_TEMP/release-bundle/$artifact" --repo "$GITHUB_REPOSITORY" --signer-workflow "$GITHUB_REPOSITORY/.github/workflows/staging.yml" --format json |'
  ]);
  const approval = await readWorkflow("approved-release-attestation.yml");
  assert.match(approval, /subject-path: docs\/release\/approved-release\.json/);
  assert.match(staging, /subject-path: \$\{\{ runner\.temp \}\}\/release-bundle\/\*/);
  assert.match(production, /for artifact in \$\(jq -r '\.artifacts \| \[\.api\.name, \.web\.name, \.migration\.name, \.sbom\.name, \.metadata\.name\] \| \.\[\]'/);
  const manifestVerification = production.indexOf('gh attestation verify "$APPROVED_RELEASE_MANIFEST"');
  const bundleVerification = production.indexOf('gh attestation verify "$RUNNER_TEMP/release-bundle/$artifact"');
  assert.ok(manifestVerification >= 0 && manifestVerification < production.indexOf("gh release download"));
  assert.ok(bundleVerification > manifestVerification && bundleVerification < production.indexOf("unzip -q"));
  const env = {
    PATH: "/usr/bin:/bin", GITHUB_REPOSITORY: "synthetic-owner/synthetic-repo",
    APPROVED_RELEASE_MANIFEST: "/synthetic path/approved-release.json",
    RUNNER_TEMP: "/synthetic path", artifact: "synthetic-artifact.zip"
  };
  for (const [index, command] of commands.entries()) {
    const verifierCommand = command.replace(/ \|$/, "");
    const execute = stub => spawnSync("/bin/bash", ["--noprofile", "--norc", "-e", "-c", `${stub}\ngh attestation verify ${verifierCommand}\nprintf bypassed`], {
      env, encoding: "utf8", timeout: 5000, maxBuffer: 64 * 1024
    });
    const forwarded = execute('gh() { printf "%s\\0" "$@"; }');
    assert.ifError(forwarded.error);
    assert.equal(forwarded.signal, null);
    assert.equal(forwarded.status, 0);
    assert.equal(forwarded.stderr, "");
    assert.deepEqual(forwarded.stdout.split("\0"), [
      "attestation", "verify",
      index === 0 ? env.APPROVED_RELEASE_MANIFEST : `${env.RUNNER_TEMP}/release-bundle/${env.artifact}`,
      "--repo", env.GITHUB_REPOSITORY,
      "--signer-workflow", `${env.GITHUB_REPOSITORY}/.github/workflows/${index === 0 ? "approved-release-attestation" : "staging"}.yml`,
      ...(index === 0 ? [] : ["--format", "json"]),
      "bypassed"
    ]);
    const rejected = execute("gh() { return 17; }");
    assert.ifError(rejected.error);
    assert.equal(rejected.signal, null);
    assert.equal(rejected.status, 17);
    assert.equal(rejected.stdout, "", "Verifier failure must stop later shell commands");
    assert.equal(rejected.stderr, "");
  }
});

test("legacy rollback validates the live realm before migration and preserves customer build inputs", () => {
  const job = deploymentJob(rollback, "production-deploy");
  assert.match(job, /^      FEDRIL_REQUIRE_CUSTOMER_AUTH: "false"$/m);
  const validator = job.indexOf("node tools/release/validate-rollback-auth.mjs");
  const login = job.indexOf("uses: azure/login@");
  assert.ok(login >= 0 && validator > login, "Azure login precedes live realm inspection");
  assert.ok(validator >= 0 && validator < job.indexOf("psql "), "Rollback validation precedes migrations");
  assert.ok(validator < job.indexOf("az webapp config appsettings set"), "Rollback validation precedes API settings mutation");
  for (const suffix of ["CLIENT_ID", "TENANT_ID", "TENANT_SUBDOMAIN", "API_SCOPE"]) {
    assert.match(job, new RegExp(`VITE_CUSTOMER_MSAL_${suffix}: \\$\\{\\{ env\\.FEDRIL_CUSTOMER_${suffix} \\}\\}`));
  }
});

test("legacy rollback dispatch input is shell data and preserves candidate gates", async t => {
  const step = rollback.match(/^      - name: Validate approved launch candidate input\n([\s\S]*?)(?=^      - name:)/m)?.[1];
  assert.ok(step, "Missing launch-candidate input validation step");
  assert.match(step, /^        env:\n          REQUESTED_LAUNCH_CANDIDATE_TAG: \$\{\{ github\.event\.inputs\.launch_candidate_tag \}\}$/m);
  const body = step.match(/^        run: \|\n((?:          .*\n|\n)*)/m)?.[1];
  assert.ok(body, "Missing validation script");
  assert.doesNotMatch(body, /\$\{\{/, "Dispatch expressions must not be interpolated into executable shell text");

  const tag = "synthetic-approved-tag";
  const sha = "a".repeat(40);
  const stubs = `
    jq() {
      case "$2" in
        .approvedLaunchCandidateTag) printf '%s\\n' "$SYNTHETIC_APPROVED_TAG" ;;
        .approvedCommitSha) printf '%s\\n' "$SYNTHETIC_APPROVED_SHA" ;;
        *) return 91 ;;
      esac
    }
    git() {
      case "$1" in
        fetch)
          [[ "$#" -eq 3 && "$2" == --force && "$3" == --tags ]] || return 92
          printf 'fetch\\n' >&2
          return "$SYNTHETIC_FETCH_STATUS"
          ;;
        rev-list)
          [[ "$#" -eq 4 && "$2" == -n && "$3" == 1 && "$4" == "$SYNTHETIC_APPROVED_TAG" ]] || return 93
          printf 'resolve\\n' >&2
          [[ "$SYNTHETIC_RESOLVE_STATUS" == 0 ]] || return "$SYNTHETIC_RESOLVE_STATUS"
          printf '%s\\n' "$SYNTHETIC_ACTUAL_SHA"
          ;;
        *) return 94 ;;
      esac
    }
  `;
  const cases = [
    ["approved tag and SHA", {}, true, "fetch\nresolve\n"],
    ...["", "synthetic-other", `${tag}\n`, '"; printf INJECTION; #', "$(printf INJECTION)", "`printf INJECTION`", "; printf INJECTION", "*"].map(value =>
      [`reject literal input ${JSON.stringify(value)}`, { REQUESTED_LAUNCH_CANDIDATE_TAG: value }, false, ""]),
    ["missing manifest", { APPROVED_LAUNCH_CANDIDATE_MANIFEST: "" }, false, ""],
    ["empty approved tag", { SYNTHETIC_APPROVED_TAG: "" }, false, ""],
    ["null approved tag", { SYNTHETIC_APPROVED_TAG: "null" }, false, ""],
    ["empty approved SHA", { SYNTHETIC_APPROVED_SHA: "" }, false, ""],
    ["null approved SHA", { SYNTHETIC_APPROVED_SHA: "null" }, false, ""],
    ["non-legacy tag", { LEGACY_ROLLBACK_TAG: "synthetic-other" }, false, ""],
    ["different resolved SHA", { SYNTHETIC_ACTUAL_SHA: "b".repeat(40) }, false, "fetch\nresolve\n"],
    ["fetch failure", { SYNTHETIC_FETCH_STATUS: "7" }, false, "fetch\n"],
    ["tag resolution failure", { SYNTHETIC_RESOLVE_STATUS: "8" }, false, "fetch\nresolve\n"]
  ];
  for (const [name, overrides, allowed, calls] of cases) {
    await t.test(name, () => {
      const result = spawnSync("/bin/bash", ["--noprofile", "--norc", "-e", "-c", stubs + body.replace(/^          /gm, "")], {
        encoding: "utf8", timeout: 5000, maxBuffer: 64 * 1024,
        env: {
          PATH: "/usr/bin:/bin",
          // Only file existence is real; jq and git are in-memory stubs.
          APPROVED_LAUNCH_CANDIDATE_MANIFEST: fileURLToPath(import.meta.url),
          REQUESTED_LAUNCH_CANDIDATE_TAG: tag, LEGACY_ROLLBACK_TAG: tag,
          SYNTHETIC_APPROVED_TAG: tag, SYNTHETIC_APPROVED_SHA: sha, SYNTHETIC_ACTUAL_SHA: sha,
          SYNTHETIC_FETCH_STATUS: "0", SYNTHETIC_RESOLVE_STATUS: "0", ...overrides
        }
      });
      assert.ifError(result.error);
      assert.equal(result.signal, null);
      assert.equal(result.status === 0, allowed);
      assert.equal(result.stdout, "", "Input must never execute shell commands");
      assert.equal(result.stderr, calls, "Rejected input must stop before Git operations");
    });
  }
});

test("legacy rollback retains current runtime tools and publishes metadata for repeated rollback verification", () => {
  const job = deploymentJob(rollback, "production-deploy");
  const copy = job.indexOf('cp tools/release/write-runtime-config.mjs tools/release/validate-auth-config.mjs "$RUNNER_TEMP/"');
  const historicalCheckout = job.indexOf("Checkout approved launch candidate artifact source");
  const write = job.indexOf('node "$RUNNER_TEMP/write-runtime-config.mjs" apps/web/dist/runtime-config.js');
  const publish = job.indexOf("uses: Azure/static-web-apps-deploy@");
  assert.ok(copy >= 0 && copy < historicalCheckout, "Current runtime tooling must survive historical checkout");
  assert.ok(write > historicalCheckout && write > job.indexOf("npm run build:web"), "Runtime metadata follows historical build");
  assert.ok(publish > write, "Runtime metadata must be included in published web artifact");
});

for (const [name, workflow, jobName, environment] of [
  ["staging", staging, "staging-deploy", "staging"],
  ["production", production, "production-deploy", "Production"],
  ["legacy rollback", rollback, "production-deploy", "Production"]
]) {
  test(`${name} binds OIDC to its exact protected environment with job permissions`, () => {
    const job = deploymentJob(workflow, jobName);
    assert.match(job, new RegExp(`^    environment:\\n      name: ${environment}$`, "m"));
    assert.match(job, /^    permissions:\n(?:      [^\n]+\n)*?      id-token: write$/m);
    assert.match(job, /uses: azure\/login@[a-f0-9]{40} # v3/);
    if (name === "staging") {
      for (const [field, variable, output] of [
        ["client-id", "AZURE_CLIENT_ID", "client_id"],
        ["tenant-id", "AZURE_TENANT_ID", "tenant_id"],
        ["subscription-id", "AZURE_SUBSCRIPTION_ID", "subscription_id"]
      ]) {
        assert.ok(job.includes(field + ": ${{ inputs.expected_staging_target_sha256 == '' && vars." +
          variable + " || steps.staging_target.outputs." + output + " }}"));
      }
    } else {
      assert.match(job, /client-id: \$\{\{ vars\.AZURE_CLIENT_ID \}\}/);
      assert.match(job, /tenant-id: \$\{\{ vars\.AZURE_TENANT_ID \}\}/);
      assert.match(job, /subscription-id: \$\{\{ vars\.AZURE_SUBSCRIPTION_ID \}\}/);
    }
    assert.doesNotMatch(job, /creds:|AZURE_CREDENTIALS_GCCS_/);
  });
}

test("staging optionally binds evaluated target before Azure login", () => {
  const input = topSection(staging, "on");
  assert.match(input, /^      expected_staging_target_sha256:\n(?:        [^\n]+\n)*?        required: false$/m);
  const guard = namedStep(staging, "Bind evaluated staging target before Azure operations");
  assert.match(guard, /id: staging_target/);
  assert.match(guard, /EXPECTED_STAGING_TARGET_SHA256: \$\{\{ inputs\.expected_staging_target_sha256 \}\}/);
  assert.doesNotMatch(guard, /continue-on-error:/);
  assertBefore(staging, "Bind evaluated staging target before Azure operations", "Login to Azure staging subscription");
  assertBefore(staging, "Bind evaluated staging target before Azure operations", "Apply staging migrations through approved CI/CD");
});

for (const [name, workflow, jobName] of [
  ["staging", staging, "staging-deploy"],
  ["production", production, "production-deploy"]
]) {
  test(`${name} validates complete customer configuration before changing Azure or database state`, () => {
    const job = deploymentJob(workflow, jobName);
    assert.match(job, /^      FEDRIL_REQUIRE_CUSTOMER_AUTH: "true"$/m);
    for (const suffix of ["CLIENT_ID", "TENANT_ID", "TENANT_SUBDOMAIN", "API_SCOPE", "AUTHORITY", "AUDIENCE"]) {
      assert.match(job, new RegExp(`^      FEDRIL_CUSTOMER_${suffix}: \\$\\{\\{ vars\\.[A-Z_]+ \\}\\}$`, "m"));
    }
    const validation = job.indexOf('node "$FEDRIL_TRUSTED_ROOT/tools/release/validate-auth-config.mjs"');
    assert.ok(validation >= 0, "Missing preflight validation");
    const mutations = [...job.matchAll(/psql |az webapp config appsettings set|uses: azure\/webapps-deploy@|uses: Azure\/static-web-apps-deploy@/g)];
    assert.ok(mutations.length >= 3, "Expected migration and application publishing steps");
    for (const mutation of mutations) {
      assert.ok(validation < mutation.index, `Validation occurs after ${mutation[0]}`);
    }
    assert.match(job, /Authentication__Customer__Authority="\$FEDRIL_CUSTOMER_AUTHORITY"/);
    assert.match(job, /Authentication__Customer__Audience="\$FEDRIL_CUSTOMER_AUDIENCE"/);
  });
}

test("artifact policy binds actual event metadata before exact checkout and complete finite history inspection", async () => {
  const policy = await readWorkflow("repository-artifact-policy.yml");
  assert.ok(policy.includes("run-name: artifact-policy ${{ github.ref }} ${{ github.sha }} ${{ github.event.before || 'none' }}"));
  assert.ok(policy.includes("ARTIFACT_COMPARISON_BASE_SHA: ${{ github.event.pull_request.base.sha || github.event.before || '0000000000000000000000000000000000000000' }}"));
  assert.ok(policy.includes("ARTIFACT_PR_HEAD_SHA: ${{ github.event.pull_request.head.sha || '' }}"));
  const capture = policy.indexOf('node "$helper_dir/finite-artifact-history.mjs" capture');
  const checkout = policy.indexOf("      - name: Checkout exact event source without broad history");
  const verify = policy.indexOf('node "$ARTIFACT_HISTORY_HELPER" verify');
  const scan = policy.indexOf('node tools/security/check-sensitive-artifacts.mjs --changed-since "$ARTIFACT_BASE_COMMIT_SHA"');
  assert.ok(capture >= 0 && capture < checkout && checkout < verify && verify < scan);
  assert.match(policy, /^          ref: \$\{\{ env\.ARTIFACT_TIP_COMMIT_SHA \}\}$/m);
  assert.match(policy, /^          fetch-depth: 1$/m);
  assert.doesNotMatch(policy, /fetch-depth: 0|fetch .*--tags|git fetch --all/);
  assert.match(artifactHistorySource, /JSON\.stringify\(observed\) !== JSON\.stringify\(expected\)/);
  assert.match(artifactHistorySource, /completeEventHistoryAvailable: true/);
});

test("preview checkout does not persist credentials into later build steps", () => {
  const job = deploymentJob(previews, "build_and_deploy_job");
  const checkouts = [...job.matchAll(/^      - uses: actions\/checkout@[^\n]+\n([\s\S]*?)(?=^      - |$(?![\s\S]))/gm)];
  assert.equal(checkouts.length, 1, "Expected one preview source checkout");
  const inputs = checkouts[0][1];
  assert.match(inputs, /^        with:\n/m);
  assert.deepEqual([...inputs.matchAll(/^          persist-credentials: ([^\n]+)$/gm)].map(match => match[1]), ["false"]);
  assert.match(inputs, /^          submodules: true$/m);
  assert.match(inputs, /^          lfs: false$/m);
  assert.ok(checkouts[0].index < job.indexOf("      - name: Build web artifact"));
});

test("CI source acquisition preserves credential isolation for checkout and controlled finite history", () => {
  const jobs = [...topSection(ci, "jobs").matchAll(/^  ([A-Za-z_][A-Za-z0-9_-]*):$/gm)].map(match => match[1]);
  const observed = [];
  for (const name of jobs) {
    const job = deploymentJob(ci, name);
    const checkouts = [...job.matchAll(/^      - (?:name: [^\n]+\n        )?uses: actions\/checkout@[^\n]+\n([\s\S]*?)(?=^      - |$(?![\s\S]))/gm)];
    if (["backend", "protected-admission", "secret-scan"].includes(name)) {
      assert.equal(checkouts.length, 0, `${name} does not use actions/checkout`);
      if (name === "secret-scan") {
        assert.match(job, /ci-runner\.mjs" prepare "\$FEDRIL_TRUSTED_ROOT\/admission-context\.json"/);
        assert.match(job, /^          GITHUB_TOKEN: \$\{\{ secrets\.GITHUB_TOKEN \}\}$/m);
        assert.match(job, /^          GITLEAKS_LICENSE: \$\{\{ secrets\.GITLEAKS_LICENSE \}\}$/m);
        assert.match(job, /--log-opts="--full-history -m \$CI_TESTED_SHA"/);
        assert.doesNotMatch(job, /fetch-depth: 0|git fetch .*--tags|--log-opts="--all/);
      }
      continue;
    }
    assert.equal(checkouts.length, 1, `${name} has one source checkout`);
    assert.deepEqual([...checkouts[0][1].matchAll(/^          persist-credentials: ([^\n]+)$/gm)].map(match => match[1]), ["false"], name);
    observed.push(name);
  }
  assert.deepEqual(observed, ["backend-prerequisites", "backend-tests", "frontend", "real-stack-critical-uat"]);
  const args = controlledGitArgv("/synthetic-root", ["git", "fetch", "--no-tags", "https://github.com/owner/repository.git", "a".repeat(40)]);
  assert.ok(args.includes("credential.helper="));
  assert.ok(args.includes("core.hooksPath=/synthetic-root/empty-hooks"));
  assert.ok(args.includes("core.attributesFile=/dev/null"));
  assert.equal(gitEnvironment.GIT_CONFIG_GLOBAL, "/dev/null");
  assert.equal(gitEnvironment.GIT_CONFIG_NOSYSTEM, "1");
  for (const key of ["HOME", "GH_TOKEN", "GITHUB_TOKEN", "HTTPS_PROXY", "GIT_CONFIG_COUNT", "GIT_SSH_COMMAND"]) assert.equal(Object.hasOwn(gitEnvironment, key), false);
  assert.throws(() => controlledGitArgv("/synthetic-root", ["sh", "-c", "unsafe"]));
  assert.match(nativeSource, /config\['persist-credentials'\] !== false/);
});

test("workflow Node setups select the reviewed exact LTS version", async () => {
  const observed = [];
  for (const file of (await readdir(new URL("../../.github/workflows/", import.meta.url))).filter(name => /\.ya?ml$/.test(name)).sort()) {
    const workflow = await readWorkflow(file);
    const steps = [...workflow.matchAll(/^      - (?:name: [^\n]+\n        )?uses: actions\/setup-node@[^\n]+\n([\s\S]*?)(?=^      - |$(?![\s\S]))/gm)];
    for (const [, body] of steps) {
      assert.deepEqual([...body.matchAll(/^          node-version: ([^\n]+)$/gm)].map(match => match[1]), ['"24.22.1"'], file);
      assert.doesNotMatch(body, /^          (?:node-version-file|check-latest):/m, file);
    }
    if (steps.length) observed.push([file, steps.length]);
  }
  assert.deepEqual(observed, [
    ["approved-release-attestation.yml", 1],
    ["azure-static-web-apps-mango-rock-016ff040f.yml", 1],
    ["ci.yml", 5],
    ["production-infrastructure-drift.yml", 1],
    ["production-release.yml", 1],
    ["production.yml", 2],
    ["repository-artifact-policy.yml", 1],
    ["staging.yml", 2]
  ]);
});

test("workflow SDK setups retain the exact repository SDK without roll-forward", async () => {
  const sdk = JSON.parse(await readFile(new URL("../../global.json", import.meta.url), "utf8")).sdk;
  assert.equal(sdk.version, "10.0.402");
  assert.equal(sdk.rollForward, "disable");
  let setups = 0;
  for (const file of ["ci.yml", "production.yml", "staging.yml"]) {
    for (const [, body] of (await readWorkflow(file)).matchAll(/^      - (?:name: [^\n]+\n        )?uses: actions\/setup-dotnet@[^\n]+\n([\s\S]*?)(?=^      - |$(?![\s\S]))/gm)) {
      assert.match(body, file === "staging.yml" ? /^          global-json-file: fedril-source\/global.json$/m : /^          global-json-file: global.json$/m, file);
      assert.doesNotMatch(body, /^          (?:dotnet-version|dotnet-quality):/m, file);
      setups += 1;
    }
  }
  assert.equal(setups, 5);
});

test("CI service images retain their reviewed content digests", () => {
  const job = deploymentJob(ci, "real-stack-critical-uat");
  assert.deepEqual([...job.matchAll(/^        image: ([^\n]+)$/gm)].map(match => match[1]), [
    "postgres:17@sha256:a63c1ea3f6a3a5a1621f97d2aba3b30faecfab4224c1b0000cc85ced57bb1939",
    "mcr.microsoft.com/azure-storage/azurite:3.35.0@sha256:647c63a91102a9d8e8000aab803436e1fc85fbb285e7ce830a82ee5d6661cf37",
    "clamav/clamav-debian:stable@sha256:d7ede3cefeaf3b8a72ebcdee849d13f504bbca3f3b3cfade5bc7b6ee06a4f62c"
  ]);
});


test("canonical source remains exact while CI retains existing main coverage", async () => {
  assert.match(topSection(ci, "on"), /^    branches: \["main", "release-main"\]$/m);
  assert.match(topSection(ci, "on"), /^  pull_request:$/m);
  assert.match(topSection(previews, "on"), /^      - release-main$/m);
  const approved = await readWorkflow("approved-release-attestation.yml");
  assert.match(topSection(approved, "on"), /^  workflow_dispatch:$/m);
  for (const workflow of [staging, production]) {
    assert.match(workflow, /FEDRIL_WORKFLOW_SHA: \$\{\{ github\.workflow_sha \}\}/);
    assert.match(workflow, /FEDRIL_WORKFLOW_REF: \$\{\{ github\.workflow_ref \}\}/);
    assert.doesNotMatch(workflow, /origin\/main\b|git fetch .*--tags|git merge-base/);
  }
  assert.match(finiteSource, /refs\/heads\/release-main/);
  assert.match(finiteSource, /candidateSha/);
  assert.match(compositionSource, /checkout\.headSha !== bootstrap\.workflowSha/);
});

test("snapshot migration comparison pins approved rollback endpoints without claiming Git merge-base equivalence", () => {
  assert.match(topSection(staging, "on"), /default: merge-base/);
  assertBefore(staging, "Prepare verified workflow and candidate sources with snapshot comparison", "dotnet restore");
  assert.match(runnerSource, /mode === 'merge-base' && requestedBaseline === ''/);
  assert.match(runnerSource, /mode === 'approved-endpoints' && requestedBaseline === baseline\.sourceCommitSha/);
  assert.match(runnerSource, /if \(!\(\(mode === 'merge-base'[\s\S]*?\)\)\) fail\(\)/);
  assert.match(runnerSource, /baselineRef\.commitSha !== baseline\.sourceCommitSha \|\| baselineRef\.tag !== approvedManifest\.rollback\.sourceTag/);
  assert.match(runnerSource, /hash\(baselineBytes\) !== carrier\.baselineSnapshot\.sha256/);
  assert.match(runnerSource, /source-only-staging/);
  assert.match(snapshotSource, /validateMigrationSourceEdits/);
  assert.match(snapshotSource, /validateMigrationDeletions/);
  assert.doesNotMatch(runnerSource, /git merge-base|migration-diff/);
});


test("staging bootstrap and approval attestation require manual exact canonical source before checkout", async t => {
  assert.match(topSection(staging, "on"), /^  workflow_dispatch:$/m);
  assert.doesNotMatch(topSection(staging, "on"), /^  (push|pull_request|workflow_run|schedule):/m);
  for (const mode of ["staging", "production"]) {
    const workflowPath = mode === "staging" ? ".github/workflows/staging.yml" : ".github/workflows/production-release.yml";
    const policy = { schemaVersion: 1, repository: "owner/repository", event: "workflow_dispatch", ref: "refs/heads/release-main",
      workflowPath, workflowSha: "a".repeat(40), canonicalAuthoritySha256: "b".repeat(64) };
    const bytes = Buffer.from(JSON.stringify(policy)), pin = createHash("sha256").update(bytes).digest("hex");
    const context = { repository: policy.repository, event: policy.event, ref: policy.ref,
      workflowRef: `${policy.repository}/${workflowPath}@${policy.ref}`, workflowSha: policy.workflowSha, sourceSha: policy.workflowSha };
    await t.test(`${mode} accepts the exact approved dispatch source`, () => {
      const allowed = bindWorkflowBootstrap(bytes, pin, context, mode);
      assert.equal(allowed.checkout.ref, policy.workflowSha); assert.equal(allowed.checkout["persist-credentials"], false);
    });
    for (const [event, ref] of [["push", policy.ref], [policy.event, "refs/heads/main"], [policy.event, "refs/tags/v0.2.0-rc.1"],
      [policy.event, "refs/heads/release-main-extra"], [policy.event, "refs/heads/release-main\n"], [policy.event, ""]]) {
      await t.test(`${mode} rejects ${JSON.stringify([event, ref])}`, () => {
        assert.throws(() => bindWorkflowBootstrap(bytes, pin, { ...context, event, ref }, mode));
      });
    }
    await t.test(`${mode} rejects altered policy pin, workflow SHA or source identity`, () => {
      assert.throws(() => bindWorkflowBootstrap(bytes, "0".repeat(64), context, mode));
      assert.throws(() => bindWorkflowBootstrap(bytes, pin, { ...context, workflowSha: "c".repeat(40) }, mode));
      assert.throws(() => bindWorkflowBootstrap(bytes, pin, { ...context, sourceSha: "c".repeat(40) }, mode));
    });
  }
  for (const workflow of [await readWorkflow("approved-release-attestation.yml")]) {
    const events = topSection(workflow, "on");
    assert.match(events, /^  workflow_dispatch:$/m);
    assert.doesNotMatch(events, /^  (push|pull_request|workflow_run|schedule):/m);
    const guard = namedStep(workflow, "Validate canonical dispatch source");
    assert.ok(workflow.indexOf(guard) < workflow.indexOf("      - name: Checkout repository"));
    const body = guard.split("        run: |\n")[1].replace(/^          /gm, "");
    assert.doesNotMatch(body, /\$\{\{/);
    for (const [event, ref, allowed] of [
      ["workflow_dispatch", "refs/heads/release-main", true],
      ["push", "refs/heads/release-main", false],
      ["workflow_dispatch", "refs/heads/main", false],
      ["workflow_dispatch", "refs/tags/v0.2.0-rc.1", false],
      ["workflow_dispatch", "refs/heads/release-main-extra", false],
      ["workflow_dispatch", "refs/heads/release-main\n", false],
      ["workflow_dispatch", "", false]
    ]) {
      await t.test(`${workflow.split("\n")[0]} ${JSON.stringify([event, ref])}`, () => {
        const result = spawnSync("/bin/bash", ["--noprofile", "--norc", "-e", "-c", body], {
          env: { PATH: "/usr/bin:/bin", GITHUB_EVENT_NAME: event, GITHUB_REF: ref },
          encoding: "utf8", timeout: 5000, maxBuffer: 64 * 1024
        });
        assert.ifError(result.error);
        assert.equal(result.signal, null);
        assert.equal(result.status === 0, allowed);
        assert.equal(result.stdout, "");
        assert.equal(result.stderr, "");
      });
    }
  }
});
