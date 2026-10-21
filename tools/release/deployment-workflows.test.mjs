import test from "node:test";
import assert from "node:assert/strict";
import { readFile, readdir } from "node:fs/promises";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const readWorkflow = name => readFile(new URL(`../../.github/workflows/${name}`, import.meta.url), "utf8");
const staging = await readWorkflow("staging.yml");
const production = await readWorkflow("production-release.yml");
const rollback = await readWorkflow("production.yml");
const previews = await readWorkflow("azure-static-web-apps-mango-rock-016ff040f.yml");
const ci = await readWorkflow("ci.yml");

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

test("staging checks exact-ref artifact eligibility before candidate checkout or build", () => {
  const gate = staging.indexOf('node tools/release/release-evidence.mjs artifact-policy');
  assert.ok(gate >= 0 && gate < staging.indexOf('git checkout --detach'), "Candidate policy must pass before source checkout");
  assert.ok(gate < staging.indexOf('npm ci'), "Candidate policy must pass before build inputs run");
  assert.match(staging, /artifact-policy "\$GITHUB_REPOSITORY" "\$GITHUB_REPOSITORY_ID" "\$commit_sha" "refs\/tags\/\$candidate_tag"/);
  assert.match(staging, /artifact-policy "\$GITHUB_REPOSITORY" "\$GITHUB_REPOSITORY_ID" "\$commit_sha" "\$GITHUB_REF"/);
});

test("production binds artifact policy and verified provenance to the approved staging run before mutations", () => {
  const runGate = production.indexOf('node tools/release/release-evidence.mjs staging-run');
  const provenanceGate = production.indexOf('node tools/release/release-evidence.mjs attestation');
  const download = production.indexOf('gh release download');
  assert.ok(runGate >= 0 && runGate < download, "Validate the live staging run before download");
  assert.ok(provenanceGate > runGate && provenanceGate < production.indexOf('unzip -q'), "Validate verified provenance before extraction");
  assert.match(production, /artifact-policy "\$GITHUB_REPOSITORY" "\$GITHUB_REPOSITORY_ID" "\$commit_sha" "refs\/tags\/\$tag"/);
  assert.match(production, /git merge-base --is-ancestor "\$staging_workflow_sha" origin\/main/);
  assert.match(production, /commits\/\$\{staging_workflow_sha\}/);
  for (const workflow of [staging, production]) assert.match(workflow, /^      actions: read$/m);
});

test("every CI job inherits the read-only contents token", () => {
  assert.equal(topSection(ci, "permissions").trim(), "contents: read");
  const jobs = [...topSection(ci, "jobs").matchAll(/^  ([A-Za-z_][A-Za-z0-9_-]*):$/gm)].map(match => match[1]);
  assert.deepEqual(jobs, ["backend-prerequisites", "backend-tests", "backend", "frontend", "real-stack-critical-uat", "secret-scan"]);
  for (const job of jobs) {
    assert.doesNotMatch(deploymentJob(ci, job), /^    permissions:/m, `${job} must inherit the read-only workflow token`);
  }
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

test("main staging web publishing has a single owner; preview workflow is PR-only", () => {
  assert.match(topSection(staging, "on"), /^  push:/m);
  const events = topSection(previews, "on");
  assert.match(events, /^  pull_request:/m);
  assert.doesNotMatch(events, /^  (push|workflow_dispatch|pull_request_target|workflow_run):/m);
  assert.match(previews, /github\.event_name == 'pull_request'/);
  assert.match(previews, /github\.event\.pull_request\.head\.repo\.full_name == github\.repository/);
});

test("staging serializes candidates and main pushes in one deployment group", () => {
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
    assert.match(job, /client-id: \$\{\{ vars\.AZURE_CLIENT_ID \}\}/);
    assert.match(job, /tenant-id: \$\{\{ vars\.AZURE_TENANT_ID \}\}/);
    assert.match(job, /subscription-id: \$\{\{ vars\.AZURE_SUBSCRIPTION_ID \}\}/);
    assert.doesNotMatch(job, /creds:|AZURE_CREDENTIALS_GCCS_/);
  });
}

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
    const validation = job.indexOf("node tools/release/validate-auth-config.mjs");
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

test("artifact policy records its event-derived full ref, SHA and history scope", async () => {
  const policy = await readWorkflow("repository-artifact-policy.yml");
  assert.ok(policy.includes("run-name: artifact-policy ${{ github.ref }} ${{ github.sha }} ${{ github.event.before || 'none' }}"));
  assert.ok(policy.includes('SENSITIVE_ARTIFACT_BASE_SHA: ${{ github.event.pull_request.base.sha || github.event.before }}'));
  assert.ok(policy.includes('node tools/security/check-sensitive-artifacts.mjs --changed-since "$SENSITIVE_ARTIFACT_BASE_SHA"'));
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

test("CI checkouts do not persist credentials beyond source acquisition", () => {
  const jobs = [...topSection(ci, "jobs").matchAll(/^  ([A-Za-z_][A-Za-z0-9_-]*):$/gm)].map(match => match[1]);
  const observed = [];
  for (const name of jobs) {
    const job = deploymentJob(ci, name);
    const checkouts = [...job.matchAll(/^      - (?:name: [^\n]+\n        )?uses: actions\/checkout@[^\n]+\n([\s\S]*?)(?=^      - |$(?![\s\S]))/gm)];
    if (name === "backend") {
      assert.equal(checkouts.length, 0, "Summary job must not acquire source credentials");
      continue;
    }
    assert.equal(checkouts.length, 1, `${name} has one source checkout`);
    assert.deepEqual([...checkouts[0][1].matchAll(/^          persist-credentials: ([^\n]+)$/gm)].map(match => match[1]), ["false"], name);
    if (name === "secret-scan") {
      assert.match(checkouts[0][1], /^          fetch-depth: 0$/m);
      assert.match(job, /^          GITHUB_TOKEN: \$\{\{ secrets\.GITHUB_TOKEN \}\}$/m);
      assert.match(job, /^          GITLEAKS_LICENSE: \$\{\{ secrets\.GITLEAKS_LICENSE \}\}$/m);
    }
    observed.push(name);
  }
  assert.deepEqual(observed, ["backend-prerequisites", "backend-tests", "frontend", "real-stack-critical-uat", "secret-scan"]);
});
