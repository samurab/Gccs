import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const readWorkflow = name => readFile(new URL(`../../.github/workflows/${name}`, import.meta.url), "utf8");
const staging = await readWorkflow("staging.yml");
const production = await readWorkflow("production-release.yml");
const rollback = await readWorkflow("production.yml");
const previews = await readWorkflow("azure-static-web-apps-mango-rock-016ff040f.yml");

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

test("legacy rollback validates the live realm before migration and preserves customer build inputs", () => {
  const job = deploymentJob(rollback, "production-deploy");
  assert.match(job, /^      FEDRIL_REQUIRE_CUSTOMER_AUTH: "false"$/m);
  const validator = job.indexOf("node tools/release/validate-rollback-auth.mjs");
  assert.ok(validator > job.indexOf("uses: azure/login@v3"), "Azure login precedes live realm inspection");
  assert.ok(validator >= 0 && validator < job.indexOf("psql "), "Rollback validation precedes migrations");
  assert.ok(validator < job.indexOf("az webapp config appsettings set"), "Rollback validation precedes API settings mutation");
  for (const suffix of ["CLIENT_ID", "TENANT_ID", "TENANT_SUBDOMAIN", "API_SCOPE"]) {
    assert.match(job, new RegExp(`VITE_CUSTOMER_MSAL_${suffix}: \\$\\{\\{ env\\.FEDRIL_CUSTOMER_${suffix} \\}\\}`));
  }
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
    assert.match(job, /uses: azure\/login@v3/);
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
