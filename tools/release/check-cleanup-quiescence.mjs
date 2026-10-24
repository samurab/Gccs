import { execFileSync } from "node:child_process";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

const normalize = name => name.replaceAll("__", ":").toLowerCase();
const check = (condition, message) => { if (!condition) throw new Error(message); };

function azureJson(args) {
  return JSON.parse(execFileSync("az", [...args, "--output", "json", "--only-show-errors"], {
    encoding: "utf8", stdio: ["ignore", "pipe", "pipe"], timeout: 30_000, maxBuffer: 512 * 1024
  }));
}

// shortcut: current deployment topology is one API app without slots; review the
// complete consumer inventory before supporting another app, slot or worker host.
export function verifyCleanupQuiescence(environment, state, env = process.env, lookup = azureJson) {
  check(["staging", "production"].includes(environment), "Unknown deployment environment");
  check(["Stopped", "Running"].includes(state), "Unknown expected API state");
  const required = name => {
    const value = env[name]?.trim();
    check(Boolean(value), "Missing cleanup verification configuration");
    return value;
  };
  const prefix = environment.toUpperCase();
  const subscription = required("DEPLOYMENT_AZURE_SUBSCRIPTION_ID");
  const group = required(`${prefix}_RESOURCE_GROUP`);
  const app = required(`${prefix}_API_APP_NAME`);
  const groupArgs = ["--subscription", subscription, "--resource-group", group];
  const appArgs = [...groupArgs, "--name", app];
  const read = (command, args, query) => {
    try { return lookup(["webapp", ...command, ...args, "--query", query]); }
    catch { throw new Error("Cleanup verification lookup failed; provider diagnostics suppressed"); }
  };
  const appState = () => check(read(["show"], appArgs, "state") === state, "API state is not eligible for this rollout phase");
  const topology = () => {
    const names = read(["list"], groupArgs, "[].name");
    check(Array.isArray(names) && names.length === 1 && names[0] === app, "Unreviewed API application topology");
    const slots = read(["deployment", "slot", "list"], appArgs, "[].name");
    check(Array.isArray(slots) && slots.length === 0, "Unreviewed API slot topology");
  };
  const cleanupKey = () => {
    const names = read(["config", "appsettings", "list"], appArgs, "[].name");
    check(Array.isArray(names) && names.every(name => typeof name === "string") && new Set(names).size === names.length,
      "Malformed setting-name response");
    const keys = names.filter(name => normalize(name) === "objectcleanupprocessing:enabled");
    check(keys.length === 1 && /^[A-Za-z0-9_:]+$/.test(keys[0]), "Cleanup override absent or ambiguous");
    return keys[0];
  };
  const disabled = key => {
    // --query projects CLI output locally; it does not constrain Azure's response.
    const values = read(["config", "appsettings", "list"], appArgs, `[?name=='${key}'].value`);
    check(Array.isArray(values) && values.length === 1 && typeof values[0] === "string" &&
      values[0].trim().toLowerCase() === "false", "Cleanup processing is not explicitly disabled");
  };
  topology(); appState();
  const key = cleanupKey();
  disabled(key);
  check(cleanupKey() === key, "Cleanup setting population changed during verification");
  disabled(key);
  appState(); topology();
  // Observed configuration and ARM states are not an atomic lock or proof of
  // every database consumer. The operator's reviewed fleet/drain evidence is separate.
  return { environment, state, cleanupDisabled: true };
}

export function main(args = process.argv.slice(2), env = process.env, lookup = azureJson, output = console) {
  try {
    check(args.length === 2, "Expected environment and API state");
    verifyCleanupQuiescence(args[0], args[1], env, lookup);
    output.log("Cleanup disabled; reviewed single-app/no-slot topology and expected API state observed.");
    return 0;
  } catch {
    output.error("Cleanup quiescence not established; values and provider diagnostics suppressed. Rollout held.");
    return 1;
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  process.exitCode = main();
}
