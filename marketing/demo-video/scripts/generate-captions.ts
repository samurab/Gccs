import {promises as fs} from "node:fs";
import {dirname, join, resolve} from "node:path";
import {fileURLToPath} from "node:url";

type Scene = {
  id: string;
  narration: string;
};

type ScriptDocument = {
  compositions: Array<{
    id: string;
    scenes: Scene[];
  }>;
};

type TimingsDocument = {
  compositions: Array<{
    id: string;
    scenes: Array<{
      id: string;
      startMs: number;
      leadInMs: number;
      audioDurationMs: number;
    }>;
  }>;
};

type Caption = {
  text: string;
  startMs: number;
  endMs: number;
  timestampMs: null;
  confidence: null;
};

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(scriptDirectory, "..");
const captionsDirectory = join(projectRoot, "captions");
const narrationDirectory = join(projectRoot, "narration");

const [script, timings] = await Promise.all([
  readJson<ScriptDocument>(join(narrationDirectory, "script.json")),
  readJson<TimingsDocument>(join(narrationDirectory, "timings.json"))
]);

const timingByComposition = new Map(timings.compositions.map((composition) => [composition.id, composition]));
const outputNames: Record<string, string> = {
  Flagship: "fedril-demo",
  Homepage60: "fedril-homepage-60",
  Social30: "fedril-social-30"
};

await fs.mkdir(captionsDirectory, {recursive: true});

for (const composition of script.compositions) {
  const compositionTiming = timingByComposition.get(composition.id);
  if (!compositionTiming) {
    throw new Error(`Timing metadata is missing for ${composition.id}. Run narration generation first.`);
  }

  const sceneTimingById = new Map(compositionTiming.scenes.map((scene) => [scene.id, scene]));
  const captions: Caption[] = [];
  for (const scene of composition.scenes) {
    const timing = sceneTimingById.get(scene.id);
    if (!timing) {
      throw new Error(`Timing metadata is missing for ${scene.id}.`);
    }
    captions.push(...createSceneCaptions(scene.narration, timing.startMs + timing.leadInMs, timing.audioDurationMs));
  }

  validateCaptionSequence(captions, composition.id);
  const outputName = outputNames[composition.id] ?? composition.id.toLowerCase();
  const writes = [
    writeFile(join(captionsDirectory, `${outputName}.json`), `${JSON.stringify(captions, null, 2)}\n`),
    writeFile(join(captionsDirectory, `${outputName}.vtt`), toWebVtt(captions)),
    writeFile(join(captionsDirectory, `${outputName}.srt`), toSrt(captions))
  ];
  if (composition.id === "Flagship") {
    writes.push(writeFile(join(captionsDirectory, "captions.json"), `${JSON.stringify(captions, null, 2)}\n`));
    writes.push(writeFile(join(captionsDirectory, "gccs-demo.vtt"), toWebVtt(captions)));
    writes.push(writeFile(join(captionsDirectory, "gccs-demo.srt"), toSrt(captions)));
  }
  await Promise.all(writes);
  process.stdout.write(`[captions] ${composition.id}: ${captions.length} cues\n`);
}

function createSceneCaptions(sourceText: string, speechStartMs: number, speechDurationMs: number): Caption[] {
  const chunks = splitForReading(sourceText);
  const weights = chunks.map((chunk) => Math.max(1, countWords(chunk)));
  const totalWeight = weights.reduce((sum, weight) => sum + weight, 0);
  const minimumCueMs = 1_500;
  const minimumRequiredMs = minimumCueMs * chunks.length;
  if (speechDurationMs < minimumRequiredMs) {
    throw new Error(`Narration duration is too short for ${chunks.length} readable caption cues.`);
  }

  const flexibleMs = speechDurationMs - minimumRequiredMs;
  let cursorMs = speechStartMs;
  return chunks.map((chunk, index) => {
    const isLast = index === chunks.length - 1;
    const proportionalMs = Math.round(flexibleMs * (weights[index] / totalWeight));
    const durationMs = isLast
      ? speechStartMs + speechDurationMs - cursorMs
      : minimumCueMs + proportionalMs;
    const caption: Caption = {
      text: chunk,
      startMs: Math.round(cursorMs),
      endMs: Math.round(cursorMs + durationMs),
      timestampMs: null,
      confidence: null
    };
    cursorMs += durationMs;
    return caption;
  });
}

function splitForReading(sourceText: string) {
  const maximumCharacters = 118;
  const sentences = sourceText
    .split(/(?<=[.!?])\s+(?=[A-Z])/)
    .map((value) => value.trim())
    .filter(Boolean);
  const chunks: string[] = [];
  for (const sentence of sentences) {
    if (sentence.length <= maximumCharacters) {
      chunks.push(sentence);
      continue;
    }

    const clauses = sentence.split(/(?<=,)\s+/);
    let current = "";
    for (const clause of clauses) {
      const candidate = current ? `${current} ${clause}` : clause;
      if (candidate.length <= maximumCharacters || !current) {
        current = candidate;
      } else {
        chunks.push(current);
        current = clause;
      }
    }
    if (current) {
      chunks.push(current);
    }
  }

  const boundedChunks = chunks.flatMap((chunk) => wrapAtWordBoundaries(chunk, maximumCharacters));
  if (boundedChunks.length === 0) {
    throw new Error("Narration source text cannot be empty.");
  }
  return boundedChunks;
}

function wrapAtWordBoundaries(value: string, maximumCharacters: number) {
  if (value.length <= maximumCharacters) {
    return [value];
  }

  const lines: string[] = [];
  let current = "";
  for (const word of value.split(/\s+/)) {
    const candidate = current ? `${current} ${word}` : word;
    if (candidate.length <= maximumCharacters || !current) {
      current = candidate;
    } else {
      lines.push(current);
      current = word;
    }
  }
  if (current) {
    lines.push(current);
  }
  return lines;
}

function validateCaptionSequence(captions: Caption[], compositionId: string) {
  for (let index = 0; index < captions.length; index += 1) {
    const caption = captions[index];
    if (caption.endMs <= caption.startMs) {
      throw new Error(`${compositionId} contains a non-positive caption duration.`);
    }
    if (index > 0 && caption.startMs < captions[index - 1].endMs) {
      throw new Error(`${compositionId} contains overlapping captions.`);
    }
  }
}

function toWebVtt(captions: Caption[]) {
  const cues = captions.map((caption) =>
    `${formatTimestamp(caption.startMs, ".")} --> ${formatTimestamp(caption.endMs, ".")}\n${caption.text}`
  );
  return `WEBVTT\n\n${cues.join("\n\n")}\n`;
}

function toSrt(captions: Caption[]) {
  return `${captions.map((caption, index) =>
    `${index + 1}\n${formatTimestamp(caption.startMs, ",")} --> ${formatTimestamp(caption.endMs, ",")}\n${caption.text}`
  ).join("\n\n")}\n`;
}

function formatTimestamp(milliseconds: number, decimalSeparator: "." | ",") {
  const bounded = Math.max(0, Math.round(milliseconds));
  const hours = Math.floor(bounded / 3_600_000);
  const minutes = Math.floor((bounded % 3_600_000) / 60_000);
  const seconds = Math.floor((bounded % 60_000) / 1_000);
  const millis = bounded % 1_000;
  return `${pad(hours, 2)}:${pad(minutes, 2)}:${pad(seconds, 2)}${decimalSeparator}${pad(millis, 3)}`;
}

function pad(value: number, width: number) {
  return String(value).padStart(width, "0");
}

function countWords(value: string) {
  return value.trim().split(/\s+/).filter(Boolean).length;
}

async function readJson<T>(path: string): Promise<T> {
  return JSON.parse(await fs.readFile(path, "utf8")) as T;
}

async function writeFile(path: string, value: string) {
  await fs.mkdir(dirname(path), {recursive: true});
  await fs.writeFile(path, value, {encoding: "utf8", mode: 0o644});
}
