import {createHash} from "node:crypto";
import {spawnSync} from "node:child_process";
import {promises as fs} from "node:fs";
import {dirname, join, relative, resolve} from "node:path";
import {fileURLToPath} from "node:url";

type Scene = {
  id: string;
  narration: string;
  leadInMs: number;
  plannedSpeechDurationMs: number;
  tailMs: number;
};

type Composition = {
  id: string;
  targetSeconds: number;
  scenes: Scene[];
};

type ScriptDocument = {
  compositions: Composition[];
};

type VoiceConfig = {
  model: string;
  responseFormat: "wav";
  defaultVoice: string;
  auditionVoices: string[];
  instructions: string;
  auditionExcerpt: string;
  estimatedWordsPerMinute: number;
  normalization: {
    integratedLoudnessLufs: number;
    loudnessRange: number;
    truePeakDb: number;
    sampleRateHz: number;
    channels: number;
  };
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
  errorCode?: string;
};

type NarrationManifest = {
  version: number;
  updatedAt: string;
  entries: ManifestEntry[];
};

type FfmpegInvocation = {
  command: string;
  prefixArguments: string[];
};

class NarrationGenerationError extends Error {
  public readonly code: string;

  constructor(code: string) {
    super(code);
    this.code = code;
  }
}

const moduleDirectory = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(moduleDirectory, "..");
const scriptPath = join(moduleDirectory, "script.json");
const voiceConfigPath = join(moduleDirectory, "voice-config.json");
const pronunciationsPath = join(moduleDirectory, "pronunciations.json");
const manifestPath = join(moduleDirectory, "manifest.json");
const timingsPath = join(moduleDirectory, "timings.json");
const narrationAssetDirectory = join(projectRoot, "assets", "narration");
const publicNarrationDirectory = join(projectRoot, "public", "narration");
const runtimeDirectory = join(projectRoot, ".runtime", "narration");
const auditionDirectory = join(moduleDirectory, "auditions");
const transitionMs = 500;
const requiredNormalizationFilters = ["afade", "areverse", "loudnorm", "silenceremove"] as const;
let cachedFfmpegInvocation: FfmpegInvocation | null | undefined;

const mode = readMode(process.argv.slice(2));
const [script, voiceConfig, pronunciations] = await Promise.all([
  readJson<ScriptDocument>(scriptPath),
  readJson<VoiceConfig>(voiceConfigPath),
  readJson<Record<string, string>>(pronunciationsPath)
]);
validateInputs(script, voiceConfig, pronunciations);

const voice = normalizeVoice(process.env.TTS_VOICE, voiceConfig.defaultVoice);
const apiKey = process.env.OPENAI_API_KEY?.trim() || null;

if (mode === "auditions") {
  await generateAuditions(voiceConfig, pronunciations, apiKey);
} else {
  await generateScenes(script, voiceConfig, pronunciations, voice, apiKey);
}

function readMode(argumentsList: string[]): "auditions" | "scenes" {
  const modeIndex = argumentsList.indexOf("--mode");
  const requestedMode = modeIndex >= 0 ? argumentsList[modeIndex + 1] : "scenes";
  if (requestedMode !== "auditions" && requestedMode !== "scenes") {
    throw new Error("--mode must be either auditions or scenes.");
  }

  return requestedMode;
}

function validateInputs(
  sourceScript: ScriptDocument,
  config: VoiceConfig,
  replacements: Record<string, string>
) {
  if (!Array.isArray(sourceScript.compositions) || sourceScript.compositions.length === 0) {
    throw new Error("narration/script.json must contain at least one composition.");
  }
  if (config.model !== "gpt-4o-mini-tts" || config.responseFormat !== "wav") {
    throw new Error("voice-config.json must use gpt-4o-mini-tts with WAV output.");
  }
  if (typeof config.defaultVoice !== "string") {
    throw new Error("voice-config.json must contain a default voice.");
  }
  normalizeVoice(config.defaultVoice, config.defaultVoice);
  if (!Array.isArray(config.auditionVoices) || config.auditionVoices.length < 3) {
    throw new Error("voice-config.json must contain at least three audition voices.");
  }
  const auditionVoices = new Set(config.auditionVoices.map((auditionVoice) => {
    if (typeof auditionVoice !== "string") {
      throw new Error("voice-config.json contains an invalid audition voice.");
    }
    return normalizeVoice(auditionVoice, config.defaultVoice);
  }));
  if (auditionVoices.size !== config.auditionVoices.length) {
    throw new Error("voice-config.json contains duplicate audition voices.");
  }
  if (typeof config.instructions !== "string" || config.instructions.trim().length === 0) {
    throw new Error("voice-config.json must include voice instructions.");
  }
  if (typeof config.auditionExcerpt !== "string" || config.auditionExcerpt.trim().length === 0) {
    throw new Error("voice-config.json must include an audition excerpt.");
  }
  if (!Number.isFinite(config.estimatedWordsPerMinute) || config.estimatedWordsPerMinute <= 0) {
    throw new Error("voice-config.json estimatedWordsPerMinute must be positive.");
  }
  if (!config.normalization || typeof config.normalization !== "object") {
    throw new Error("voice-config.json must contain normalization settings.");
  }
  const normalizationValues = [
    config.normalization.integratedLoudnessLufs,
    config.normalization.loudnessRange,
    config.normalization.truePeakDb,
    config.normalization.sampleRateHz,
    config.normalization.channels
  ];
  if (normalizationValues.some((value) => !Number.isFinite(value))) {
    throw new Error("voice-config.json contains invalid normalization settings.");
  }
  if (config.normalization.sampleRateHz < 8_000 || config.normalization.channels !== 1) {
    throw new Error("voice-config.json must use a valid mono narration sample rate.");
  }
  if (!replacements || typeof replacements !== "object" || Array.isArray(replacements)) {
    throw new Error("pronunciations.json must be an object of speech-only replacements.");
  }
  for (const [term, replacement] of Object.entries(replacements)) {
    if (!term.trim() || typeof replacement !== "string" || !replacement.trim()) {
      throw new Error("pronunciations.json contains an empty term or replacement.");
    }
  }

  const compositionIds = new Set<string>();
  const sceneIds = new Set<string>();
  for (const composition of sourceScript.compositions) {
    if (!composition || typeof composition.id !== "string" || !composition.id.trim()) {
      throw new Error("Every narration composition must have an ID.");
    }
    if (compositionIds.has(composition.id)) {
      throw new Error(`narration/script.json contains duplicate composition ID ${composition.id}.`);
    }
    compositionIds.add(composition.id);
    if (!Array.isArray(composition.scenes) || composition.scenes.length === 0) {
      throw new Error(`${composition.id} must contain at least one narration scene.`);
    }
    if (!Number.isFinite(composition.targetSeconds) || composition.targetSeconds <= 0) {
      throw new Error(`${composition.id} must contain a positive targetSeconds value.`);
    }
    for (const scene of composition.scenes) {
      if (!scene || typeof scene.id !== "string" || !/^[a-z0-9][a-z0-9-]{1,79}$/.test(scene.id)) {
        throw new Error(`${composition.id} contains an invalid scene ID.`);
      }
      if (sceneIds.has(scene.id)) {
        throw new Error(`narration/script.json contains duplicate scene ID ${scene.id}.`);
      }
      sceneIds.add(scene.id);
      if (typeof scene.narration !== "string" || !scene.narration.trim()) {
        throw new Error(`${scene.id} must contain narration text.`);
      }
      for (const [field, value] of [
        ["leadInMs", scene.leadInMs],
        ["plannedSpeechDurationMs", scene.plannedSpeechDurationMs],
        ["tailMs", scene.tailMs]
      ] as const) {
        if (!Number.isFinite(value) || value < 0 || (field === "plannedSpeechDurationMs" && value === 0)) {
          throw new Error(`${scene.id} contains an invalid ${field} value.`);
        }
      }
    }
  }
}

async function generateScenes(
  sourceScript: ScriptDocument,
  config: VoiceConfig,
  replacements: Record<string, string>,
  selectedVoice: string,
  key: string | null
) {
  await Promise.all([
    fs.mkdir(narrationAssetDirectory, {recursive: true}),
    fs.mkdir(publicNarrationDirectory, {recursive: true}),
    fs.mkdir(runtimeDirectory, {recursive: true})
  ]);

  const existingManifest = await readOptionalJson<NarrationManifest>(manifestPath);
  const existingById = new Map((existingManifest?.entries ?? []).map((entry) => [entry.sceneId, entry]));
  const nextEntries: ManifestEntry[] = [];
  let failures = 0;

  for (const composition of sourceScript.compositions) {
    for (const scene of composition.scenes) {
      const sourceTextHash = sha256(scene.narration);
      const narrationFriendlyText = applyPronunciations(scene.narration, replacements);
      const configHash = sha256(JSON.stringify({
        narrationFriendlyText,
        selectedVoice,
        model: config.model,
        instructions: config.instructions,
        responseFormat: config.responseFormat,
        normalization: config.normalization
      }));
      const outputPath = join(narrationAssetDirectory, `${scene.id}.wav`);
      const publicPath = join(publicNarrationDirectory, `${scene.id}.wav`);
      const previous = existingById.get(scene.id);
      const canReuse = await isReusable(previous, outputPath, sourceTextHash, configHash, key !== null);

      if (canReuse && previous) {
        await fs.copyFile(outputPath, publicPath);
        nextEntries.push(previous);
        writeStatus(scene.id, "cached");
        continue;
      }

      try {
        const result = key
          ? await generateSpeechFile({
              apiKey: key,
              config,
              input: narrationFriendlyText,
              outputPath,
              selectedVoice,
              temporaryName: scene.id
            })
          : await generateSilentPlaceholder(outputPath, scene.plannedSpeechDurationMs, config.normalization.sampleRateHz);

        await fs.copyFile(outputPath, publicPath);
        nextEntries.push({
          sceneId: scene.id,
          compositionId: composition.id,
          sourceNarrationText: scene.narration,
          narrationFriendlyText,
          voice: selectedVoice,
          model: config.model,
          voiceInstructions: config.instructions,
          outputFilename: toProjectRelative(outputPath),
          audioDurationSeconds: result.durationSeconds,
          sourceTextHash,
          configHash,
          generationDate: new Date().toISOString(),
          generationStatus: key ? "generated" : "placeholder-silent",
          normalizationStatus: result.normalizationStatus
        });
        writeStatus(scene.id, key ? "generated" : "placeholder-silent");
      } catch (error) {
        failures += 1;
        await Promise.all([
          fs.rm(outputPath, {force: true}),
          fs.rm(publicPath, {force: true})
        ]);
        nextEntries.push({
          sceneId: scene.id,
          compositionId: composition.id,
          sourceNarrationText: scene.narration,
          narrationFriendlyText,
          voice: selectedVoice,
          model: config.model,
          voiceInstructions: config.instructions,
          outputFilename: toProjectRelative(outputPath),
          audioDurationSeconds: 0,
          sourceTextHash,
          configHash,
          generationDate: new Date().toISOString(),
          generationStatus: "failed",
          normalizationStatus: "not-applicable",
          errorCode: safeErrorCode(error)
        });
        writeStatus(scene.id, "failed");
      }
    }
  }

  const manifest: NarrationManifest = {
    version: 1,
    updatedAt: new Date().toISOString(),
    entries: nextEntries
  };
  await writeJson(manifestPath, manifest);
  await writeTimings(sourceScript, manifest);

  if (!key) {
    const placeholders = nextEntries.filter((entry) => entry.generationStatus === "placeholder-silent").length;
    const retained = nextEntries.filter((entry) => entry.generationStatus === "generated").length;
    process.stdout.write(
      `Narration API calls were skipped because OPENAI_API_KEY is not configured. ${placeholders} silent placeholder(s) are present and timing metadata was generated.\n`
    );
    if (retained > 0) {
      process.stdout.write(`${retained} unchanged generated narration asset(s) were retained from the validated cache.\n`);
    }
    process.stdout.write("After configuring the environment variable, run: npm run narration:generate\n");
  }

  if (failures > 0) {
    throw new Error(`${failures} narration scene(s) failed. See narration/manifest.json for sanitized status codes.`);
  }
}

async function generateAuditions(
  config: VoiceConfig,
  replacements: Record<string, string>,
  key: string | null
) {
  await Promise.all([
    fs.mkdir(auditionDirectory, {recursive: true}),
    fs.mkdir(runtimeDirectory, {recursive: true})
  ]);

  const auditionManifestPath = join(auditionDirectory, "manifest.json");
  const existingManifest = await readOptionalJson<NarrationManifest>(auditionManifestPath);
  const existingById = new Map((existingManifest?.entries ?? []).map((entry) => [entry.sceneId, entry]));
  const narrationFriendlyText = applyPronunciations(config.auditionExcerpt, replacements);
  const sourceTextHash = sha256(config.auditionExcerpt);
  const entries: ManifestEntry[] = [];
  let failures = 0;
  const failureCodes = new Set<string>();

  for (const auditionVoice of config.auditionVoices) {
    const sceneId = `audition-${auditionVoice}`;
    const outputPath = join(auditionDirectory, `${auditionVoice}.wav`);
    const configHash = sha256(JSON.stringify({
      narrationFriendlyText,
      auditionVoice,
      model: config.model,
      instructions: config.instructions,
      responseFormat: config.responseFormat,
      normalization: config.normalization
    }));
    const previous = existingById.get(sceneId);
    const canReuse = await isReusable(previous, outputPath, sourceTextHash, configHash, key !== null);

    if (canReuse && previous) {
      entries.push(previous);
      writeStatus(sceneId, "cached");
      continue;
    }

    try {
      const plannedDurationMs = estimateSpeechDurationMs(config.auditionExcerpt, config.estimatedWordsPerMinute);
      const result = key
        ? await generateSpeechFile({
            apiKey: key,
            config,
            input: narrationFriendlyText,
            outputPath,
            selectedVoice: auditionVoice,
            temporaryName: `audition-${auditionVoice}`
          })
        : await generateSilentPlaceholder(outputPath, plannedDurationMs, config.normalization.sampleRateHz);

      entries.push({
        sceneId,
        compositionId: "VoiceAuditions",
        sourceNarrationText: config.auditionExcerpt,
        narrationFriendlyText,
        voice: auditionVoice,
        model: config.model,
        voiceInstructions: config.instructions,
        outputFilename: toProjectRelative(outputPath),
        audioDurationSeconds: result.durationSeconds,
        sourceTextHash,
        configHash,
        generationDate: new Date().toISOString(),
        generationStatus: key ? "generated" : "placeholder-silent",
        normalizationStatus: result.normalizationStatus
      });
      writeStatus(sceneId, key ? "generated" : "placeholder-silent");
    } catch (error) {
      failures += 1;
      const errorCode = safeErrorCode(error);
      failureCodes.add(errorCode);
      await fs.rm(outputPath, {force: true});
      entries.push({
        sceneId,
        compositionId: "VoiceAuditions",
        sourceNarrationText: config.auditionExcerpt,
        narrationFriendlyText,
        voice: auditionVoice,
        model: config.model,
        voiceInstructions: config.instructions,
        outputFilename: toProjectRelative(outputPath),
        audioDurationSeconds: 0,
        sourceTextHash,
        configHash,
        generationDate: new Date().toISOString(),
        generationStatus: "failed",
        normalizationStatus: "not-applicable",
        errorCode
      });
      writeStatus(sceneId, `failed (${errorCode})`);
    }
  }

  await writeJson(auditionManifestPath, {
    version: 1,
    updatedAt: new Date().toISOString(),
    entries
  });

  if (!key) {
    const placeholders = entries.filter((entry) => entry.generationStatus === "placeholder-silent").length;
    const retained = entries.filter((entry) => entry.generationStatus === "generated").length;
    process.stdout.write(
      `Voice audition API calls were skipped because OPENAI_API_KEY is not configured. ${placeholders} silent audition placeholder(s) are present.\n`
    );
    if (retained > 0) {
      process.stdout.write(`${retained} unchanged generated audition asset(s) were retained from the validated cache.\n`);
    }
    process.stdout.write("After configuring the environment variable, run: npm run narration:auditions\n");
  }

  if (failures > 0) {
    throw new Error(
      `${failures} audition(s) failed with sanitized code(s): ${[...failureCodes].join(", ")}. ` +
      "See narration/auditions/manifest.json for per-voice status."
    );
  }
}

async function generateSpeechFile(options: {
  apiKey: string;
  config: VoiceConfig;
  input: string;
  outputPath: string;
  selectedVoice: string;
  temporaryName: string;
}) {
  const rawPath = join(runtimeDirectory, `${options.temporaryName}.raw.wav`);
  const stagedPath = join(runtimeDirectory, `${options.temporaryName}.normalized.wav`);
  await Promise.all([
    fs.rm(rawPath, {force: true}),
    fs.rm(stagedPath, {force: true})
  ]);

  try {
    const response = await requestSpeech(options);
    await fs.writeFile(rawPath, Buffer.from(await response.arrayBuffer()), {mode: 0o600});
    const normalizationStatus = await normalizeWav(rawPath, stagedPath, options.config);
    const durationSeconds = await readWavDurationSeconds(stagedPath);
    if (!Number.isFinite(durationSeconds) || durationSeconds <= 0) {
      throw new NarrationGenerationError("invalid_wav_duration");
    }

    await fs.rename(stagedPath, options.outputPath);
    return {durationSeconds, normalizationStatus};
  } finally {
    await Promise.all([
      fs.rm(rawPath, {force: true}),
      fs.rm(stagedPath, {force: true})
    ]);
  }
}

async function requestSpeech(options: {
  apiKey: string;
  config: VoiceConfig;
  input: string;
  selectedVoice: string;
}) {
  const maximumAttempts = 3;
  for (let attempt = 1; attempt <= maximumAttempts; attempt += 1) {
    try {
      const response = await fetch("https://api.openai.com/v1/audio/speech", {
        method: "POST",
        headers: {
          Authorization: `Bearer ${options.apiKey}`,
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          model: options.config.model,
          voice: options.selectedVoice,
          input: options.input,
          instructions: options.config.instructions,
          response_format: options.config.responseFormat
        }),
        signal: AbortSignal.timeout(120_000)
      });

      if (response.ok) {
        return response;
      }
      if (!isRetryableStatus(response.status) || attempt === maximumAttempts) {
        throw new NarrationGenerationError(`speech_http_${response.status}`);
      }
      await wait(retryDelayMs(attempt, response.headers.get("retry-after")));
    } catch (error) {
      if (error instanceof NarrationGenerationError) {
        throw error;
      }
      if (attempt === maximumAttempts) {
        throw new NarrationGenerationError("speech_request_failed");
      }
      await wait(retryDelayMs(attempt, null));
    }
  }

  throw new NarrationGenerationError("speech_request_failed");
}

function isRetryableStatus(status: number) {
  return status === 408 || status === 429 || status >= 500;
}

function retryDelayMs(attempt: number, retryAfter: string | null) {
  if (retryAfter) {
    const seconds = Number(retryAfter);
    if (Number.isFinite(seconds) && seconds >= 0) {
      return Math.min(15_000, Math.ceil(seconds * 1_000));
    }
    const retryDate = Date.parse(retryAfter);
    if (Number.isFinite(retryDate)) {
      return Math.min(15_000, Math.max(0, retryDate - Date.now()));
    }
  }
  return Math.min(8_000, 1_000 * (2 ** (attempt - 1)));
}

function wait(milliseconds: number) {
  return new Promise<void>((resolveWait) => setTimeout(resolveWait, milliseconds));
}

async function normalizeWav(rawPath: string, outputPath: string, config: VoiceConfig) {
  const ffmpeg = findFfmpegInvocation();
  if (!ffmpeg) {
    await fs.copyFile(rawPath, outputPath);
    return "skipped-tool-unavailable" as const;
  }

  const filter = [
    "silenceremove=start_periods=1:start_duration=0.05:start_threshold=-50dB",
    "areverse",
    "silenceremove=start_periods=1:start_duration=0.05:start_threshold=-50dB",
    "areverse",
    `loudnorm=I=${config.normalization.integratedLoudnessLufs}:LRA=${config.normalization.loudnessRange}:TP=${config.normalization.truePeakDb}`,
    "afade=t=in:d=0.03",
    "areverse",
    "afade=t=in:d=0.03",
    "areverse"
  ].join(",");

  const result = spawnSync(ffmpeg.command, [
    ...ffmpeg.prefixArguments,
    "-y",
    "-i",
    rawPath,
    "-af",
    filter,
    "-ar",
    String(config.normalization.sampleRateHz),
    "-ac",
    String(config.normalization.channels),
    "-c:a",
    "pcm_s16le",
    outputPath
  ], {stdio: "ignore"});

  if (result.status !== 0) {
    throw new NarrationGenerationError("audio_normalization_failed");
  }

  return "normalized" as const;
}

function findFfmpegInvocation(): FfmpegInvocation | null {
  if (cachedFfmpegInvocation !== undefined) {
    return cachedFfmpegInvocation;
  }

  const candidates: FfmpegInvocation[] = [
    {command: "ffmpeg", prefixArguments: []}
  ];
  const remotionCandidates = [
    join(projectRoot, "node_modules", ".bin", "remotion"),
    resolve(projectRoot, "..", "..", "node_modules", ".bin", "remotion")
  ];
  for (const remotionBinary of remotionCandidates) {
    candidates.push({command: remotionBinary, prefixArguments: ["ffmpeg"]});
  }

  for (const candidate of candidates) {
    if (supportsNormalizationFilters(candidate)) {
      cachedFfmpegInvocation = candidate;
      return candidate;
    }
  }

  cachedFfmpegInvocation = null;
  return null;
}

function supportsNormalizationFilters(invocation: FfmpegInvocation) {
  const result = spawnSync(invocation.command, [
    ...invocation.prefixArguments,
    "-hide_banner",
    "-filters"
  ], {encoding: "utf8"});
  if (result.status !== 0) {
    return false;
  }

  const filterList = `${result.stdout ?? ""}\n${result.stderr ?? ""}`;
  return requiredNormalizationFilters.every((filter) => (
    new RegExp(`^\\s*[TSC.]{2,3}\\s+${filter}\\s`, "m").test(filterList)
  ));
}

async function generateSilentPlaceholder(outputPath: string, durationMs: number, sampleRateHz: number) {
  const durationSeconds = Math.max(1, durationMs / 1000);
  const wav = createSilentPcmWav(durationSeconds, sampleRateHz);
  await fs.writeFile(outputPath, wav, {mode: 0o600});
  return {durationSeconds, normalizationStatus: "not-applicable" as const};
}

function createSilentPcmWav(durationSeconds: number, sampleRateHz: number) {
  const channels = 1;
  const bitsPerSample = 16;
  const bytesPerSample = bitsPerSample / 8;
  const sampleCount = Math.ceil(durationSeconds * sampleRateHz);
  const dataSize = sampleCount * channels * bytesPerSample;
  const buffer = Buffer.alloc(44 + dataSize);
  buffer.write("RIFF", 0);
  buffer.writeUInt32LE(36 + dataSize, 4);
  buffer.write("WAVE", 8);
  buffer.write("fmt ", 12);
  buffer.writeUInt32LE(16, 16);
  buffer.writeUInt16LE(1, 20);
  buffer.writeUInt16LE(channels, 22);
  buffer.writeUInt32LE(sampleRateHz, 24);
  buffer.writeUInt32LE(sampleRateHz * channels * bytesPerSample, 28);
  buffer.writeUInt16LE(channels * bytesPerSample, 32);
  buffer.writeUInt16LE(bitsPerSample, 34);
  buffer.write("data", 36);
  buffer.writeUInt32LE(dataSize, 40);
  return buffer;
}

async function readWavDurationSeconds(path: string) {
  const buffer = await fs.readFile(path);
  if (buffer.length < 44 || buffer.toString("ascii", 0, 4) !== "RIFF" || buffer.toString("ascii", 8, 12) !== "WAVE") {
    throw new NarrationGenerationError("invalid_wav_container");
  }

  let offset = 12;
  let byteRate = 0;
  let dataSize = 0;
  while (offset + 8 <= buffer.length) {
    const chunkId = buffer.toString("ascii", offset, offset + 4);
    const chunkSize = buffer.readUInt32LE(offset + 4);
    const dataOffset = offset + 8;
    if (chunkId === "fmt " && chunkSize >= 16) {
      byteRate = buffer.readUInt32LE(dataOffset + 8);
    }
    if (chunkId === "data") {
      dataSize += Math.min(chunkSize, buffer.length - dataOffset);
    }
    offset = dataOffset + chunkSize + (chunkSize % 2);
  }

  if (byteRate <= 0 || dataSize <= 0) {
    throw new NarrationGenerationError("invalid_wav_chunks");
  }
  return dataSize / byteRate;
}

async function writeTimings(sourceScript: ScriptDocument, manifest: NarrationManifest) {
  const bySceneId = new Map(manifest.entries.map((entry) => [entry.sceneId, entry]));
  const compositions = sourceScript.compositions.map((composition) => {
    let cursorMs = 0;
    const scenes = composition.scenes.map((scene, index) => {
      const entry = bySceneId.get(scene.id);
      if (!entry || entry.audioDurationSeconds <= 0) {
        throw new Error(`Narration timing is unavailable for ${scene.id}.`);
      }
      const audioDurationMs = Math.ceil(entry.audioDurationSeconds * 1000);
      const durationMs = scene.leadInMs + audioDurationMs + scene.tailMs;
      const timing = {
        id: scene.id,
        startMs: cursorMs,
        leadInMs: scene.leadInMs,
        audioDurationMs,
        tailMs: scene.tailMs,
        durationMs,
        audioAvailable: entry.generationStatus === "generated",
        generationStatus: entry.generationStatus
      };
      cursorMs += durationMs - (index < composition.scenes.length - 1 ? transitionMs : 0);
      return timing;
    });
    if (composition.id !== "Flagship") {
      const targetDurationMs = Math.round(composition.targetSeconds * 1000);
      if (cursorMs > targetDurationMs) {
        throw new Error(
          `${composition.id} measured narration requires ${cursorMs} ms, exceeding its ${targetDurationMs} ms target. Rewrite the script or increase the approved target; narration will not be sped up.`
        );
      }
      const closingHoldMs = targetDurationMs - cursorMs;
      const lastScene = scenes.at(-1);
      if (!lastScene) throw new Error(`${composition.id} has no scene available for target-duration padding.`);
      lastScene.tailMs += closingHoldMs;
      lastScene.durationMs += closingHoldMs;
      cursorMs = targetDurationMs;
    }

    return {
      id: composition.id,
      transitionMs,
      totalDurationMs: cursorMs,
      scenes
    };
  });

  await writeJson(timingsPath, {
    version: 1,
    generatedAt: new Date().toISOString(),
    compositions
  });
}

function applyPronunciations(source: string, replacements: Record<string, string>) {
  return Object.entries(replacements)
    .sort(([left], [right]) => right.length - left.length)
    .reduce((result, [term, replacement]) => {
      const escaped = term.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
      return result.replace(new RegExp(`\\b${escaped}\\b`, "g"), replacement);
    }, source);
}

async function isReusable(
  previous: ManifestEntry | undefined,
  outputPath: string,
  sourceTextHash: string,
  configHash: string,
  hasApiKey: boolean
) {
  if (!previous || previous.sourceTextHash !== sourceTextHash || previous.configHash !== configHash) {
    return false;
  }
  if (hasApiKey && previous.generationStatus !== "generated") {
    return false;
  }
  if (!hasApiKey && previous.generationStatus === "failed") {
    return false;
  }
  return fs.access(outputPath).then(() => true, () => false);
}

function normalizeVoice(value: string | undefined, fallback: string) {
  const selectedVoice = value?.trim() || fallback;
  if (!/^[a-z][a-z0-9_-]{1,31}$/i.test(selectedVoice)) {
    throw new Error("TTS_VOICE contains unsupported characters.");
  }
  return selectedVoice;
}

function estimateSpeechDurationMs(text: string, wordsPerMinute: number) {
  const wordCount = text.trim().split(/\s+/).filter(Boolean).length;
  return Math.max(1_000, Math.ceil((wordCount / wordsPerMinute) * 60_000));
}

function sha256(value: string) {
  return createHash("sha256").update(value, "utf8").digest("hex");
}

function toProjectRelative(path: string) {
  return relative(projectRoot, path).split("\\").join("/");
}

function safeErrorCode(error: unknown) {
  return error instanceof NarrationGenerationError ? error.code : "unexpected_generation_failure";
}

function writeStatus(identifier: string, status: string) {
  process.stdout.write(`[narration] ${identifier}: ${status}\n`);
}

async function readJson<T>(path: string): Promise<T> {
  return JSON.parse(await fs.readFile(path, "utf8")) as T;
}

async function readOptionalJson<T>(path: string): Promise<T | null> {
  try {
    return await readJson<T>(path);
  } catch (error) {
    if ((error as NodeJS.ErrnoException).code === "ENOENT") {
      return null;
    }
    throw error;
  }
}

async function writeJson(path: string, value: unknown) {
  await fs.mkdir(dirname(path), {recursive: true});
  await fs.writeFile(path, `${JSON.stringify(value, null, 2)}\n`, {encoding: "utf8", mode: 0o644});
}
