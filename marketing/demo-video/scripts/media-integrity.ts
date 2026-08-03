import {createHash} from "node:crypto";
import {promises as fs} from "node:fs";
import {join, relative, resolve} from "node:path";

export const renderedOutputs = [
  {name: "flagship", path: "out/fedril-flagship.mp4", width: 1920, height: 1080, min: 180, max: 240},
  {name: "homepage", path: "out/fedril-homepage-60.mp4", width: 1920, height: 1080, min: 59.95, max: 60.05},
  {name: "social", path: "out/fedril-social-30.mp4", width: 1080, height: 1920, min: 29.95, max: 30.05}
] as const;

export const customerFacingSourcePaths = [
  "narration/script.json",
  "captions/gccs-demo.vtt",
  "captions/gccs-demo.srt",
  "captions/captions.json",
  "captions/fedril-demo.json",
  "captions/fedril-demo.vtt",
  "captions/fedril-demo.srt",
  "captions/fedril-homepage-60.json",
  "captions/fedril-homepage-60.vtt",
  "captions/fedril-homepage-60.srt",
  "captions/fedril-social-30.json",
  "captions/fedril-social-30.vtt",
  "captions/fedril-social-30.srt",
  "src/DemoVideo.tsx",
  "src/Root.tsx",
  "VIDEO-DESCRIPTION.md",
  "STORYBOARD.md"
] as const;

const renderInputFiles = [
  "package.json",
  "remotion.config.ts",
  "narration/script.json",
  "narration/manifest.json",
  "narration/timings.json",
  "captions/fedril-demo.json",
  "captions/fedril-homepage-60.json",
  "captions/fedril-social-30.json",
  "src/DemoVideo.tsx",
  "src/Root.tsx",
  "src/index.ts",
  "src/types.ts",
  "../../apps/web/public/F.svg"
] as const;

const renderInputDirectories = ["public/captures", "public/narration"] as const;

export type RenderManifest = {
  version: 1;
  generatedAt: string;
  sourceDigest: string;
  outputs: Array<{
    name: string;
    path: string;
    sha256: string;
    bytes: number;
  }>;
};

export async function computeRenderSourceDigest(projectRoot: string) {
  const paths = renderInputFiles.map((path) => resolve(projectRoot, path));
  for (const directory of renderInputDirectories) {
    paths.push(...await listFiles(resolve(projectRoot, directory)));
  }

  const hash = createHash("sha256");
  for (const path of paths.sort()) {
    const label = relative(projectRoot, path).split("\\").join("/");
    hash.update(label, "utf8");
    hash.update("\0");
    hash.update(await fs.readFile(path));
    hash.update("\0");
  }
  return hash.digest("hex");
}

export async function sha256File(path: string) {
  return createHash("sha256").update(await fs.readFile(path)).digest("hex");
}

async function listFiles(directory: string): Promise<string[]> {
  const entries = await fs.readdir(directory, {withFileTypes: true});
  const files: string[] = [];
  for (const entry of entries) {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) files.push(...await listFiles(path));
    if (entry.isFile() && entry.name !== ".gitkeep") files.push(path);
  }
  return files;
}
