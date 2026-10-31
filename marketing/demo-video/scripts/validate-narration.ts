import {createHash} from "node:crypto";
import {promises as fs} from "node:fs";
import {dirname, join, resolve} from "node:path";
import {fileURLToPath} from "node:url";

type Scene = {
  id: string;
  narration: string;
};

type ScriptDocument = {
  brand: string;
  disclosure: string;
  compositions: Array<{
    id: string;
    targetSeconds: number;
    scenes: Scene[];
  }>;
};

type ManifestEntry = {
  sceneId: string;
  compositionId: string;
  sourceNarrationText: string;
  narrationFriendlyText: string;
  voice: string;
  model: string;
  voiceInstructions: string;
  outputFilename: string;
  audioDurationSeconds: number;
  sourceTextHash: string;
  configHash: string;
  generationDate: string;
  generationStatus: "generated" | "placeholder-silent" | "failed";
  normalizationStatus: "normalized" | "skipped-tool-unavailable" | "not-applicable";
};

type Manifest = {
  entries: ManifestEntry[];
};

type Timings = {
  compositions: Array<{
    id: string;
    transitionMs: number;
    totalDurationMs: number;
    scenes: Array<{
      id: string;
      startMs: number;
      leadInMs: number;
      audioDurationMs: number;
      tailMs: number;
      durationMs: number;
    }>;
  }>;
};

type Caption = {
  text: string;
  startMs: number;
  endMs: number;
};

type VoiceConfig = {
  model: string;
  defaultVoice: string;
  instructions: string;
  responseFormat: "wav";
  auditionVoices: string[];
  auditionExcerpt: string;
  normalization: {
    sampleRateHz: number;
    channels: number;
    integratedLoudnessLufs: number;
    loudnessRange: number;
    truePeakDb: number;
  };
};

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(scriptsDirectory, "..");
const narrationDirectory = join(projectRoot, "narration");
const captionsDirectory = join(projectRoot, "captions");
const strict = process.argv.includes("--strict");
const errors: string[] = [];
const warnings: string[] = [];

const [script, manifest, timings, pronunciations, voiceConfig] = await Promise.all([
  readJson<ScriptDocument>(join(narrationDirectory, "script.json")),
  readJson<Manifest>(join(narrationDirectory, "manifest.json")),
  readJson<Timings>(join(narrationDirectory, "timings.json")),
  readJson<Record<string, string>>(join(narrationDirectory, "pronunciations.json")),
  readJson<VoiceConfig>(join(narrationDirectory, "voice-config.json"))
]);

validateDisclosure(script);
validatePronunciationDictionary(pronunciations);
validateCustomerFacingText(script);
await validateManifest(script, manifest, pronunciations, voiceConfig);
await validateAuditions(pronunciations, voiceConfig);
validateTiming(script, manifest, timings);
await validateCaptions(script);

for (const warning of warnings) {
  process.stdout.write(`WARNING: ${warning}\n`);
}

if (errors.length > 0) {
  for (const error of errors) {
    process.stderr.write(`ERROR: ${error}\n`);
  }
  process.exitCode = 1;
} else {
  process.stdout.write(`Narration validation passed${strict ? " in strict mode" : ""}.\n`);
}

function validateDisclosure(document: ScriptDocument) {
  const expected = "Narration generated using AI voice technology.";
  if (document.disclosure !== expected) {
    errors.push(`AI narration disclosure must exactly match: ${expected}`);
  }
}

function validatePronunciationDictionary(dictionary: Record<string, string>) {
  const required: Record<string, string> = {
    "FeDril": "Fee-drill",
    "CMMC": "C-M-M-C",
    "CUI": "C-U-I",
    "FCI": "F-C-I",
    "DFARS": "Dee-Fars",
    "NIST": "Nist",
    "DoD": "Department of Defense",
    "RBAC": "role-based access control",
    "SaaS": "Software as a Service",
    "NIST SP 800-171": "Nist Special Publication eight hundred, dash one seventy-one"
  };

  for (const [term, expected] of Object.entries(required)) {
    if (dictionary[term] !== expected) {
      errors.push(`Pronunciation replacement for ${term} is missing or incorrect.`);
    }
  }
}

function validateCustomerFacingText(document: ScriptDocument) {
  if (document.brand !== "FeDril") {
    errors.push("The external brand must be FeDril.");
  }

  const prohibitedClaims = [
    /FeDril makes (?:you|teams|organizations) compliant/i,
    /FeDril guarantees/i,
    /guarantees? (?:CMMC )?certification/i,
    /guarantees? assessment success/i,
    /approved by the (?:Department of Defense|DoD)/i,
    /government approved/i,
    /officially certified/i,
    /secure CUI storage/i,
    /audit[- ]ready/i
  ];

  for (const composition of document.compositions) {
    const sceneIds = new Set<string>();
    const sentences = new Set<string>();
    for (const scene of composition.scenes) {
      if (sceneIds.has(scene.id)) {
        errors.push(`${composition.id} contains duplicate scene ID ${scene.id}.`);
      }
      sceneIds.add(scene.id);

      if (/\bGCCS\b/i.test(scene.narration)) {
        errors.push(`${scene.id} exposes the internal product name in narration.`);
      }
      for (const pattern of prohibitedClaims) {
        if (pattern.test(scene.narration)) {
          errors.push(`${scene.id} contains prohibited or unsupported claim wording.`);
        }
      }

      for (const sentence of splitSentences(scene.narration)) {
        const normalized = normalizeText(sentence);
        if (sentences.has(normalized)) {
          errors.push(`${composition.id} repeats a narration sentence: ${sentence}`);
        }
        sentences.add(normalized);
      }
    }
  }
}

async function validateManifest(
  document: ScriptDocument,
  sourceManifest: Manifest,
  dictionary: Record<string, string>,
  config: VoiceConfig
) {
  const expectedScenes = document.compositions.flatMap((composition) =>
    composition.scenes.map((scene) => ({...scene, compositionId: composition.id}))
  );
  const expectedIds = new Set(expectedScenes.map((scene) => scene.id));
  const manifestById = new Map<string, ManifestEntry>();

  for (const entry of sourceManifest.entries) {
    if (manifestById.has(entry.sceneId)) {
      errors.push(`Narration manifest contains duplicate entry ${entry.sceneId}.`);
    }
    manifestById.set(entry.sceneId, entry);
    if (!expectedIds.has(entry.sceneId)) {
      errors.push(`Narration manifest contains unexpected entry ${entry.sceneId}.`);
    }
  }

  for (const scene of expectedScenes) {
    const entry = manifestById.get(scene.id);
    if (!entry) {
      errors.push(`Narration manifest is missing ${scene.id}.`);
      continue;
    }
    if (entry.compositionId !== scene.compositionId) {
      errors.push(`${scene.id} is assigned to the wrong composition in the manifest.`);
    }
    if (entry.sourceNarrationText !== scene.narration) {
      errors.push(`${scene.id} manifest source text differs from the approved script.`);
    }
    if (entry.narrationFriendlyText !== applyPronunciations(scene.narration, dictionary)) {
      errors.push(`${scene.id} narration-friendly text does not match the pronunciation dictionary.`);
    }
    if (entry.sourceTextHash !== sha256(scene.narration)) {
      errors.push(`${scene.id} source-text hash is stale.`);
    }
    if (entry.model !== config.model || entry.voiceInstructions !== config.instructions) {
      errors.push(`${scene.id} model or voice instructions differ from voice-config.json.`);
    }
    const expectedConfigHash = sha256(JSON.stringify({
      narrationFriendlyText: applyPronunciations(scene.narration, dictionary),
      selectedVoice: entry.voice,
      model: config.model,
      instructions: config.instructions,
      responseFormat: config.responseFormat,
      normalization: config.normalization
    }));
    if (entry.configHash !== expectedConfigHash) {
      errors.push(`${scene.id} narration configuration hash is stale.`);
    }
    if (!Number.isFinite(entry.audioDurationSeconds) || entry.audioDurationSeconds <= 0) {
      errors.push(`${scene.id} has an invalid audio duration.`);
    }
    if (!Number.isFinite(Date.parse(entry.generationDate))) {
      errors.push(`${scene.id} has an invalid generation date.`);
    }
    const expectedOutputFilename = `assets/narration/${scene.id}.wav`;
    if (entry.outputFilename !== expectedOutputFilename) {
      errors.push(`${scene.id} narration output must remain inside assets/narration.`);
    }
    if (entry.generationStatus === "failed") {
      errors.push(`${scene.id} narration generation failed.`);
      continue;
    }
    if (entry.generationStatus === "placeholder-silent") {
      const message = `${scene.id} uses silent placeholder audio.`;
      strict ? errors.push(message) : warnings.push(message);
    }
    if (entry.generationStatus === "generated" && entry.normalizationStatus !== "normalized") {
      const message = `${scene.id} generated audio was not normalized.`;
      strict ? errors.push(message) : warnings.push(message);
    }

    const audioPath = join(projectRoot, expectedOutputFilename);
    try {
      const wav = await inspectWav(audioPath);
      if (Math.abs(wav.durationSeconds - entry.audioDurationSeconds) > 0.03) {
        errors.push(`${scene.id} manifest duration differs from the WAV duration.`);
      }
      if (wav.sampleRateHz !== config.normalization.sampleRateHz || wav.channels !== config.normalization.channels) {
        errors.push(`${scene.id} WAV format differs from the configured narration format.`);
      }
      if (entry.generationStatus === "generated" && wav.isSilent) {
        errors.push(`${scene.id} is marked generated but contains silent PCM audio.`);
      }
      const [canonicalAudio, renderAudio] = await Promise.all([
        fs.readFile(audioPath),
        fs.readFile(join(projectRoot, "public", "narration", `${scene.id}.wav`))
      ]);
      if (!canonicalAudio.equals(renderAudio)) {
        errors.push(`${scene.id} public render audio differs from the canonical narration asset.`);
      }
    } catch {
      errors.push(`${scene.id} canonical or public render WAV file is missing or invalid.`);
    }
  }

  for (const composition of document.compositions) {
    const voices = new Set(
      composition.scenes.map((scene) => manifestById.get(scene.id)?.voice).filter((value): value is string => Boolean(value))
    );
    if (voices.size !== 1) {
      errors.push(`${composition.id} must use exactly one narration voice.`);
    }
  }

  const allVoices = new Set(
    expectedScenes.map((scene) => manifestById.get(scene.id)?.voice).filter((value): value is string => Boolean(value))
  );
  if (allVoices.size !== 1) {
    errors.push("All demo compositions must use one consistent narration voice.");
  }
}

async function validateAuditions(dictionary: Record<string, string>, config: VoiceConfig) {
  let auditionManifest: Manifest;
  try {
    auditionManifest = await readJson<Manifest>(join(narrationDirectory, "auditions", "manifest.json"));
  } catch {
    errors.push("Voice audition manifest is missing. Run npm run narration:auditions.");
    return;
  }

  const expectedIds = new Set(config.auditionVoices.map((voice) => `audition-${voice}`));
  const entriesById = new Map<string, ManifestEntry>();
  for (const entry of auditionManifest.entries) {
    if (entriesById.has(entry.sceneId)) {
      errors.push(`Voice audition manifest contains duplicate entry ${entry.sceneId}.`);
    }
    entriesById.set(entry.sceneId, entry);
    if (!expectedIds.has(entry.sceneId)) {
      errors.push(`Voice audition manifest contains unexpected entry ${entry.sceneId}.`);
    }
  }

  for (const auditionVoice of config.auditionVoices) {
    const sceneId = `audition-${auditionVoice}`;
    const entry = entriesById.get(sceneId);
    if (!entry) {
      errors.push(`Voice audition manifest is missing ${sceneId}.`);
      continue;
    }
    const narrationFriendlyText = applyPronunciations(config.auditionExcerpt, dictionary);
    if (
      entry.compositionId !== "VoiceAuditions" ||
      entry.sourceNarrationText !== config.auditionExcerpt ||
      entry.narrationFriendlyText !== narrationFriendlyText ||
      entry.voice !== auditionVoice ||
      entry.model !== config.model ||
      entry.voiceInstructions !== config.instructions
    ) {
      errors.push(`${sceneId} does not match voice-config.json.`);
    }
    if (entry.sourceTextHash !== sha256(config.auditionExcerpt)) {
      errors.push(`${sceneId} source-text hash is stale.`);
    }
    const expectedConfigHash = sha256(JSON.stringify({
      narrationFriendlyText,
      auditionVoice,
      model: config.model,
      instructions: config.instructions,
      responseFormat: config.responseFormat,
      normalization: config.normalization
    }));
    if (entry.configHash !== expectedConfigHash) {
      errors.push(`${sceneId} narration configuration hash is stale.`);
    }
    if (!Number.isFinite(entry.audioDurationSeconds) || entry.audioDurationSeconds <= 0) {
      errors.push(`${sceneId} has an invalid audio duration.`);
    }
    if (!Number.isFinite(Date.parse(entry.generationDate))) {
      errors.push(`${sceneId} has an invalid generation date.`);
    }
    const expectedOutputFilename = `narration/auditions/${auditionVoice}.wav`;
    if (entry.outputFilename !== expectedOutputFilename) {
      errors.push(`${sceneId} output must remain inside narration/auditions.`);
    }
    if (entry.generationStatus === "failed") {
      errors.push(`${sceneId} generation failed.`);
      continue;
    }
    if (entry.generationStatus === "placeholder-silent") {
      const message = `${sceneId} uses silent placeholder audio.`;
      strict ? errors.push(message) : warnings.push(message);
    }
    if (entry.generationStatus === "generated" && entry.normalizationStatus !== "normalized") {
      const message = `${sceneId} generated audio was not normalized.`;
      strict ? errors.push(message) : warnings.push(message);
    }

    try {
      const wav = await inspectWav(join(projectRoot, expectedOutputFilename));
      if (Math.abs(wav.durationSeconds - entry.audioDurationSeconds) > 0.03) {
        errors.push(`${sceneId} manifest duration differs from the WAV duration.`);
      }
      if (wav.sampleRateHz !== config.normalization.sampleRateHz || wav.channels !== config.normalization.channels) {
        errors.push(`${sceneId} WAV format differs from the configured narration format.`);
      }
      if (entry.generationStatus === "generated" && wav.isSilent) {
        errors.push(`${sceneId} is marked generated but contains silent PCM audio.`);
      }
    } catch {
      errors.push(`${sceneId} WAV file is missing or invalid.`);
    }
  }
}

function validateTiming(document: ScriptDocument, manifest: Manifest, sourceTimings: Timings) {
  const manifestById = new Map(manifest.entries.map((entry) => [entry.sceneId, entry]));
  const timingByComposition = new Map(sourceTimings.compositions.map((composition) => [composition.id, composition]));

  for (const composition of document.compositions) {
    const timing = timingByComposition.get(composition.id);
    if (!timing) {
      errors.push(`Timing metadata is missing for ${composition.id}.`);
      continue;
    }
    const timingById = new Map(timing.scenes.map((scene) => [scene.id, scene]));
    if (timing.scenes.length !== composition.scenes.length ||
        timing.scenes.some((scene, index) => scene.id !== composition.scenes[index]?.id)) {
      errors.push(`${composition.id} timing scenes must match the approved script order exactly.`);
    }
    let previousSpeechEndMs = -1;
    let expectedSceneStartMs = 0;
    for (const [index, scene] of composition.scenes.entries()) {
      const sceneTiming = timingById.get(scene.id);
      const entry = manifestById.get(scene.id);
      if (!sceneTiming || !entry) {
        continue;
      }
      const expectedAudioMs = Math.ceil(entry.audioDurationSeconds * 1000);
      if (Math.abs(sceneTiming.audioDurationMs - expectedAudioMs) > 2) {
        errors.push(`${scene.id} timing does not reflect the actual narration duration.`);
      }
      if (sceneTiming.leadInMs < 500 || sceneTiming.tailMs < 500) {
        errors.push(`${scene.id} must preserve visual lead-in and post-speech pause.`);
      }
      if (sceneTiming.startMs !== expectedSceneStartMs) {
        errors.push(`${scene.id} timing is not contiguous with the preceding scene transition.`);
      }
      if (sceneTiming.durationMs !== sceneTiming.leadInMs + sceneTiming.audioDurationMs + sceneTiming.tailMs) {
        errors.push(`${scene.id} duration does not equal its lead-in, audio, and tail segments.`);
      }
      const speechStartMs = sceneTiming.startMs + sceneTiming.leadInMs;
      const speechEndMs = speechStartMs + sceneTiming.audioDurationMs;
      if (speechStartMs < previousSpeechEndMs) {
        errors.push(`${scene.id} narration overlaps the preceding scene.`);
      }
      previousSpeechEndMs = speechEndMs;
      expectedSceneStartMs += sceneTiming.durationMs -
        (index < composition.scenes.length - 1 ? timing.transitionMs : 0);
    }
    if (timing.totalDurationMs !== expectedSceneStartMs) {
      errors.push(`${composition.id} total duration does not match its contiguous scene timeline.`);
    }
    if (composition.id !== "Flagship" && timing.totalDurationMs !== Math.round(composition.targetSeconds * 1000)) {
      errors.push(`${composition.id} must remain exactly ${composition.targetSeconds} seconds after measured narration timing.`);
    }
    if (composition.id === "Flagship" && (timing.totalDurationMs < 180_000 || timing.totalDurationMs > 240_000)) {
      errors.push("Flagship must remain between three and four minutes after measured narration timing.");
    }
  }
}

async function validateCaptions(document: ScriptDocument) {
  const outputNames: Record<string, string> = {
    Flagship: "fedril-demo",
    Homepage60: "fedril-homepage-60",
    Social30: "fedril-social-30"
  };

  for (const composition of document.compositions) {
    const outputName = outputNames[composition.id] ?? composition.id.toLowerCase();
    let captions: Caption[];
    try {
      captions = await readJson<Caption[]>(join(captionsDirectory, `${outputName}.json`));
    } catch {
      errors.push(`${composition.id} caption JSON is missing. Run npm run captions:generate.`);
      continue;
    }

    const expectedText = normalizeText(composition.scenes.map((scene) => scene.narration).join(" "));
    const captionText = normalizeText(captions.map((caption) => caption.text).join(" "));
    if (captionText !== expectedText) {
      errors.push(`${composition.id} captions do not exactly preserve the approved narration source text.`);
    }
    if (captions.some((caption) => /\bGCCS\b/i.test(caption.text))) {
      errors.push(`${composition.id} captions expose the internal product name.`);
    }
    if (captions.some((caption) => /\b(?:G-C-C-S|C-M-M-C|C-U-I|F-C-I)\b/.test(caption.text))) {
      errors.push(`${composition.id} captions contain speech-only pronunciation substitutions.`);
    }
    if (captions.some((caption) => caption.text.length > 118)) {
      errors.push(`${composition.id} contains a caption cue that is too long for the approved layout.`);
    }

    for (let index = 0; index < captions.length; index += 1) {
      const caption = captions[index];
      if (caption.endMs - caption.startMs < 1_500) {
        errors.push(`${composition.id} contains a caption displayed for less than 1.5 seconds.`);
      }
      if (index > 0 && caption.startMs < captions[index - 1].endMs) {
        errors.push(`${composition.id} contains overlapping caption cues.`);
      }
    }

    for (const extension of ["vtt", "srt"]) {
      try {
        await fs.access(join(captionsDirectory, `${outputName}.${extension}`));
      } catch {
        errors.push(`${composition.id} ${extension.toUpperCase()} caption file is missing.`);
      }
    }

    if (composition.id === "Flagship") {
      try {
        const canonicalCaptions = await readJson<Caption[]>(join(captionsDirectory, "captions.json"));
        if (JSON.stringify(canonicalCaptions) !== JSON.stringify(captions)) {
          errors.push("captions/captions.json is stale relative to the flagship caption data.");
        }
      } catch {
        errors.push("captions/captions.json is missing. Run npm run captions:generate.");
      }
      for (const extension of ["vtt", "srt"]) {
        try {
          const [brandedContent, compatibilityContent] = await Promise.all([
            fs.readFile(join(captionsDirectory, `fedril-demo.${extension}`), "utf8"),
            fs.readFile(join(captionsDirectory, `gccs-demo.${extension}`), "utf8")
          ]);
          if (compatibilityContent !== brandedContent) {
            errors.push(`captions/gccs-demo.${extension} is stale relative to the FeDril flagship captions.`);
          }
          if (/\bGCCS\b/i.test(compatibilityContent)) {
            errors.push(`captions/gccs-demo.${extension} exposes the internal product name in caption content.`);
          }
        } catch {
          errors.push(`captions/gccs-demo.${extension} compatibility alias is missing.`);
        }
      }
    }
  }
}

async function inspectWav(path: string) {
  const buffer = await fs.readFile(path);
  if (buffer.length < 44 || buffer.toString("ascii", 0, 4) !== "RIFF" || buffer.toString("ascii", 8, 12) !== "WAVE") {
    throw new Error("Invalid WAV container.");
  }
  let offset = 12;
  let byteRate = 0;
  let bitsPerSample = 0;
  let audioFormat = 0;
  let channels = 0;
  let sampleRateHz = 0;
  const dataChunks: Buffer[] = [];
  while (offset + 8 <= buffer.length) {
    const chunkId = buffer.toString("ascii", offset, offset + 4);
    const chunkSize = buffer.readUInt32LE(offset + 4);
    const dataOffset = offset + 8;
    if (chunkId === "fmt " && chunkSize >= 16) {
      audioFormat = buffer.readUInt16LE(dataOffset);
      channels = buffer.readUInt16LE(dataOffset + 2);
      sampleRateHz = buffer.readUInt32LE(dataOffset + 4);
      byteRate = buffer.readUInt32LE(dataOffset + 8);
      bitsPerSample = buffer.readUInt16LE(dataOffset + 14);
    }
    if (chunkId === "data") {
      dataChunks.push(buffer.subarray(dataOffset, Math.min(dataOffset + chunkSize, buffer.length)));
    }
    offset = dataOffset + chunkSize + (chunkSize % 2);
  }
  const data = Buffer.concat(dataChunks);
  if (byteRate <= 0 || data.length === 0) {
    throw new Error("Invalid WAV data chunks.");
  }
  let isSilent = false;
  if (audioFormat === 1 && bitsPerSample === 16) {
    isSilent = true;
    for (let index = 0; index + 1 < data.length; index += 2) {
      if (data.readInt16LE(index) !== 0) {
        isSilent = false;
        break;
      }
    }
  }
  return {durationSeconds: data.length / byteRate, isSilent, channels, sampleRateHz};
}

function applyPronunciations(source: string, replacements: Record<string, string>) {
  return Object.entries(replacements)
    .sort(([left], [right]) => right.length - left.length)
    .reduce((result, [term, replacement]) => {
      const escaped = term.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
      return result.replace(new RegExp(`\\b${escaped}\\b`, "g"), replacement);
    }, source);
}

function splitSentences(value: string) {
  return value.split(/(?<=[.!?])\s+(?=[A-Z])/).map((sentence) => sentence.trim()).filter(Boolean);
}

function normalizeText(value: string) {
  return value.replace(/\s+/g, " ").trim();
}

function sha256(value: string) {
  return createHash("sha256").update(value, "utf8").digest("hex");
}

async function readJson<T>(path: string): Promise<T> {
  return JSON.parse(await fs.readFile(path, "utf8")) as T;
}
