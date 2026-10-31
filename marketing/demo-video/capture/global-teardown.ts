import {execFileSync} from "node:child_process";
import {dirname, resolve} from "node:path";
import {fileURLToPath} from "node:url";

const projectRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");

export default function globalTeardown() {
  execFileSync("bash", [resolve(projectRoot, "scripts/stop-demo.sh")], {
    cwd: projectRoot,
    stdio: "inherit"
  });
}
