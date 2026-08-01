import {promises as fs} from "node:fs";
import {dirname, resolve} from "node:path";
import {fileURLToPath} from "node:url";

const projectRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const generatedTargets = [
  "out",
  "capture/results",
  "capture/report",
  "assets/capture/execution-log.json",
  "assets/capture/raw",
  "assets/capture/stills",
  "assets/narration",
  "public/captures",
  "public/narration",
  "narration/auditions/manifest.json",
  ".runtime/narration"
].map((path) => resolve(projectRoot, path));

for (const target of generatedTargets) {
  await fs.rm(target, {recursive: true, force: true});
}

for (const directory of [
  "assets/capture/raw",
  "assets/capture/stills",
  "assets/narration",
  "public/captures",
  "public/narration",
  "narration/auditions"
]) {
  const absolute = resolve(projectRoot, directory);
  await fs.mkdir(absolute, {recursive: true});
  await fs.writeFile(resolve(absolute, ".gitkeep"), "", "utf8");
}

process.stdout.write("Generated capture, narration, and render files were removed. The isolated database volume and runtime secret file were preserved.\n");
