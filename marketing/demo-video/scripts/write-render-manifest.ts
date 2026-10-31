import {promises as fs} from "node:fs";
import {dirname, resolve} from "node:path";
import {fileURLToPath} from "node:url";
import {
  computeRenderSourceDigest,
  renderedOutputs,
  sha256File,
  type RenderManifest
} from "./media-integrity.ts";

const projectRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const outputs: RenderManifest["outputs"] = [];

for (const output of renderedOutputs) {
  const absolutePath = resolve(projectRoot, output.path);
  const stat = await fs.stat(absolutePath);
  if (!stat.isFile() || stat.size === 0) {
    throw new Error(`Cannot record render integrity for missing output ${output.name}.`);
  }
  outputs.push({
    name: output.name,
    path: output.path,
    sha256: await sha256File(absolutePath),
    bytes: stat.size
  });
}

const manifest: RenderManifest = {
  version: 1,
  generatedAt: new Date().toISOString(),
  sourceDigest: await computeRenderSourceDigest(projectRoot),
  outputs
};

const manifestPath = resolve(projectRoot, "out/render-manifest.json");
await fs.writeFile(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`, {encoding: "utf8", mode: 0o600});
process.stdout.write("[media] Render manifest bound all three outputs to the current capture, narration, caption, and composition sources.\n");
