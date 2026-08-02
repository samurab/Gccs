import {spawnSync} from "node:child_process";
import {promises as fs} from "node:fs";
import {dirname, resolve} from "node:path";
import {fileURLToPath} from "node:url";
import {
  computeRenderSourceDigest,
  customerFacingSourcePaths,
  renderedOutputs,
  sha256File,
  type RenderManifest
} from "./media-integrity.ts";

type ProbeStream = {
  codec_type?: string;
  width?: number;
  height?: number;
  avg_frame_rate?: string;
};

type ProbeResult = {
  format?: {duration?: string};
  streams?: ProbeStream[];
};

const projectRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const requireRender = process.argv.includes("--require-render");
const outputs = renderedOutputs.map((output) => ({...output, path: resolve(projectRoot, output.path)}));

await validateCustomerFacingSources();

const remotion = await findRemotionBinary();
const existingOutputs = [];
for (const output of outputs) {
  if (await exists(output.path)) existingOutputs.push(output);
}

if (existingOutputs.length === 0) {
  if (requireRender) throw new Error("No rendered FeDril videos were found for strict media validation.");
  process.stdout.write("[media] Rendered files are not present; source and claim validation passed.\n");
  process.exit(0);
}
if (requireRender && existingOutputs.length !== outputs.length) {
  throw new Error("Strict media validation requires all three rendered FeDril videos.");
}
if (!remotion) throw new Error("Remotion FFprobe is unavailable. Run npm install before validating rendered media.");
await validateCaptureDurations(remotion);
await validateRenderIntegrity(existingOutputs);

for (const output of existingOutputs) {
  const probe = probeMedia(remotion, output.path);
  const video = probe.streams?.find((stream) => stream.codec_type === "video");
  const audio = probe.streams?.find((stream) => stream.codec_type === "audio");
  const duration = Number(probe.format?.duration ?? "0");
  if (!video || video.width !== output.width || video.height !== output.height) {
    throw new Error(`${output.name} does not have the expected ${output.width}x${output.height} video stream.`);
  }
  if (Math.abs(readFrameRate(video.avg_frame_rate) - 30) > 0.01) {
    throw new Error(`${output.name} does not use the expected 30 fps frame rate.`);
  }
  if (!audio) throw new Error(`${output.name} does not contain an audio stream.`);
  if (duration < output.min || duration > output.max) {
    throw new Error(`${output.name} duration ${duration.toFixed(2)}s is outside ${output.min}-${output.max}s.`);
  }
  assertNoBlankSegments(remotion, output.path, output.name);
  process.stdout.write(`[media] ${output.name}: ${duration.toFixed(2)}s, ${video.width}x${video.height}, 30 fps, audio present.\n`);
}

async function validateCustomerFacingSources() {
  const sources = await Promise.all(customerFacingSourcePaths.map(async (path) => {
    const absolute = resolve(projectRoot, path);
    if (!await exists(absolute)) throw new Error(`Customer-facing source is missing: ${path}.`);
    return [path, await fs.readFile(absolute, "utf8")] as const;
  }));
  const sourceByPath = new Map(sources);
  const combined = sources.map(([, content]) => content).join("\n");
  if (/\bGCCS\b/i.test(combined)) {
    throw new Error("Customer-facing video sources contain the internal product name.");
  }
  const forbiddenClaims = [
    "makes you compliant",
    "guarantees CMMC certification",
    "guarantees assessment success",
    "approved by the Department of Defense",
    "officially certified",
    "government approved",
    "government endorsed",
    "secure CUI storage",
    "audit ready",
    "audit-ready"
  ];
  for (const claim of forbiddenClaims) {
    if (combined.toLowerCase().includes(claim.toLowerCase())) {
      throw new Error(`Customer-facing video sources contain a prohibited claim: ${claim}.`);
    }
  }
  if (!combined.includes("Narration generated using AI voice technology.")) {
    throw new Error("The required AI narration disclosure is missing from customer-facing sources.");
  }

  const script = JSON.parse(sourceByPath.get("narration/script.json") ?? "null") as {
    brand?: string;
    disclosure?: string;
    compositions?: Array<{id?: string; scenes?: unknown[]}>;
  };
  if (script.brand !== "FeDril" || script.disclosure !== "Narration generated using AI voice technology.") {
    throw new Error("The narration source must define the FeDril brand and exact AI narration disclosure.");
  }
  if (!Array.isArray(script.compositions) || script.compositions.length !== 3 ||
      script.compositions.some((composition) => !Array.isArray(composition.scenes) || composition.scenes.length === 0)) {
    throw new Error("All three customer-facing compositions must contain at least one scene.");
  }

  const videoSource = sourceByPath.get("src/DemoVideo.tsx") ?? "";
  const requiredRenderContracts = [
    {label: "last-scene closing-card gate", pattern: /showClosingCard\s*=\s*isLast/},
    {
      label: "closing card disclosure and active-caption props",
      pattern: /<ClosingCard\b(?=[\s\S]{0,320}\bdisclosure=\{disclosure\})(?=[\s\S]{0,320}\bactiveCaption=\{activeCaption\})/
    },
    {
      label: "opening card organization and active-caption props",
      pattern: /<OpeningCard\b(?=[\s\S]{0,320}\borganization=\{organization\})(?=[\s\S]{0,320}\bactiveCaption=\{activeCaption\})/
    },
    {label: "rendered AI narration disclosure", pattern: /\{disclosure\}<\/div>/},
    {
      label: "card caption active-caption and orientation props",
      pattern: /<CardCaption\b(?=[\s\S]{0,320}\bactiveCaption=\{activeCaption\})(?=[\s\S]{0,320}\bvertical=\{vertical\})/
    }
  ];
  for (const contract of requiredRenderContracts) {
    if (!contract.pattern.test(videoSource)) {
      throw new Error(`The Remotion source is missing a required disclosure/caption render contract: ${contract.label}.`);
    }
  }
}

async function validateRenderIntegrity(existingOutputs: typeof outputs) {
  if (existingOutputs.length !== outputs.length) {
    throw new Error("Rendered-media integrity validation requires all three outputs when any output is present.");
  }
  const manifestPath = resolve(projectRoot, "out/render-manifest.json");
  if (!await exists(manifestPath)) {
    throw new Error("Rendered files are missing out/render-manifest.json; rerun npm run video:render.");
  }
  const manifest = JSON.parse(await fs.readFile(manifestPath, "utf8")) as RenderManifest;
  if (manifest.version !== 1 || manifest.sourceDigest !== await computeRenderSourceDigest(projectRoot)) {
    throw new Error("Rendered files are stale relative to capture, narration, caption, or composition sources.");
  }
  for (const output of outputs) {
    const entry = manifest.outputs.find((candidate) => candidate.name === output.name && candidate.path === relativeOutputPath(output.path));
    const stat = await fs.stat(output.path);
    if (!entry || entry.bytes !== stat.size || entry.sha256 !== await sha256File(output.path)) {
      throw new Error(`Rendered-file integrity mismatch for ${output.name}; rerun npm run video:render.`);
    }
  }
}

function relativeOutputPath(absolutePath: string) {
  return absolutePath.slice(projectRoot.length + 1).split("\\").join("/");
}

function probeMedia(remotion: string, path: string): ProbeResult {
  const result = spawnSync(remotion, [
    "ffprobe",
    "-v", "quiet",
    "-print_format", "json",
    "-show_format",
    "-show_streams",
    path
  ], {encoding: "utf8"});
  if (result.status !== 0) throw new Error("Remotion FFprobe could not inspect a rendered file.");
  return JSON.parse(result.stdout) as ProbeResult;
}

async function validateCaptureDurations(remotion: string) {
  const [script, timings] = await Promise.all([
    fs.readFile(resolve(projectRoot, "narration/script.json"), "utf8").then((value) => JSON.parse(value) as {
      compositions: Array<{id: string; scenes: Array<{id: string; captureAsset: string | null}>}>;
    }),
    fs.readFile(resolve(projectRoot, "narration/timings.json"), "utf8").then((value) => JSON.parse(value) as {
      compositions: Array<{id: string; scenes: Array<{id: string; durationMs: number}>}>;
    })
  ]);
  const requiredByAsset = new Map<string, number>();
  for (const composition of script.compositions) {
    const timing = timings.compositions.find((candidate) => candidate.id === composition.id);
    if (!timing) throw new Error(`Capture validation is missing timing for ${composition.id}.`);
    for (const scene of composition.scenes) {
      if (!scene.captureAsset) continue;
      const sceneTiming = timing.scenes.find((candidate) => candidate.id === scene.id);
      if (!sceneTiming) throw new Error(`Capture validation is missing timing for ${scene.id}.`);
      requiredByAsset.set(
        scene.captureAsset,
        Math.max(requiredByAsset.get(scene.captureAsset) ?? 0, sceneTiming.durationMs)
      );
    }
  }

  for (const [asset, requiredDurationMs] of requiredByAsset) {
    const path = resolve(projectRoot, "public", "captures", asset);
    if (!await exists(path)) throw new Error(`Required product capture is missing: ${asset}.`);
    const durationMs = Number(probeMedia(remotion, path).format?.duration ?? "0") * 1000;
    if (!Number.isFinite(durationMs) || durationMs < requiredDurationMs + 500) {
      throw new Error(`${asset} is too short for measured narration timing; rerun npm run demo:video:capture.`);
    }
  }
}

function assertNoBlankSegments(remotion: string, path: string, name: string) {
  const result = spawnSync(remotion, [
    "ffmpeg",
    "-v", "error",
    "-i", path,
    "-vf", "scale=1:1",
    "-an",
    "-c:v", "rawvideo",
    "-pix_fmt", "rgb24",
    "-f", "image2pipe",
    "-"
  ], {maxBuffer: 1024 * 1024});
  if (result.status !== 0 || !Buffer.isBuffer(result.stdout) || result.stdout.length % 3 !== 0) {
    throw new Error(`Blank-frame analysis failed for ${name}.`);
  }

  const consecutiveFrameLimit = 15;
  let blackFrameRun = 0;
  for (let offset = 0; offset < result.stdout.length; offset += 3) {
    const red = result.stdout[offset];
    const green = result.stdout[offset + 1];
    const blue = result.stdout[offset + 2];
    const luminance = (0.2126 * red) + (0.7152 * green) + (0.0722 * blue);
    blackFrameRun = luminance <= 5 ? blackFrameRun + 1 : 0;
    if (blackFrameRun >= consecutiveFrameLimit) {
      throw new Error(`${name} contains a near-black segment of at least 0.5 seconds.`);
    }
  }
}

function readFrameRate(value = "0/1") {
  const [numerator, denominator] = value.split("/").map(Number);
  return denominator ? numerator / denominator : 0;
}

async function findRemotionBinary() {
  const candidates = [
    resolve(projectRoot, "node_modules/.bin/remotion"),
    resolve(projectRoot, "../../node_modules/.bin/remotion")
  ];
  for (const candidate of candidates) {
    if (await exists(candidate)) return candidate;
  }
  return null;
}

async function exists(path: string) {
  return fs.access(path).then(() => true, () => false);
}
