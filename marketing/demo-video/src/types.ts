export type SceneDefinition = {
  id: string;
  title: string;
  section: string;
  narration: string;
  onScreenCaption: string;
  callout: string;
  visual: string;
  captureAsset: string | null;
  leadInMs: number;
  plannedSpeechDurationMs: number;
  tailMs: number;
};

export type CompositionDefinition = {
  id: "Flagship" | "Homepage60" | "Social30";
  title: string;
  targetSeconds: number;
  scenes: SceneDefinition[];
};

export type ScriptDocument = {
  version: number;
  brand: string;
  organization: string;
  disclosure: string;
  compositions: CompositionDefinition[];
};

export type SceneTiming = {
  id: string;
  startMs: number;
  leadInMs: number;
  audioDurationMs: number;
  tailMs: number;
  durationMs: number;
  audioAvailable: boolean;
  generationStatus: "generated" | "placeholder-silent" | "failed";
};

export type CompositionTiming = {
  id: CompositionDefinition["id"];
  transitionMs: number;
  totalDurationMs: number;
  scenes: SceneTiming[];
};

export type TimingsDocument = {
  version: number;
  generatedAt: string;
  compositions: CompositionTiming[];
};

export type CaptionCue = {
  text: string;
  startMs: number;
  endMs: number;
  timestampMs: null;
  confidence: null;
};
