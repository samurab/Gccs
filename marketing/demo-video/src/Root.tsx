import {Composition} from "remotion";
import scriptSource from "../narration/script.json";
import timingSource from "../narration/timings.json";
import flagshipCaptionsSource from "../captions/fedril-demo.json";
import homepageCaptionsSource from "../captions/fedril-homepage-60.json";
import socialCaptionsSource from "../captions/fedril-social-30.json";
import {DemoVideo} from "./DemoVideo";
import type {
  CaptionCue,
  CompositionDefinition,
  CompositionTiming,
  ScriptDocument,
  TimingsDocument
} from "./types";

const fps = 30;
const script = scriptSource as ScriptDocument;
const timings = timingSource as TimingsDocument;

const captionsByComposition: Record<CompositionDefinition["id"], CaptionCue[]> = {
  Flagship: flagshipCaptionsSource as CaptionCue[],
  Homepage60: homepageCaptionsSource as CaptionCue[],
  Social30: socialCaptionsSource as CaptionCue[]
};

export const VideoRoot = () => (
  <>
    {script.compositions.map((composition) => {
      const timing = getTiming(composition.id);
      const vertical = composition.id === "Social30";
      return (
        <Composition
          key={composition.id}
          id={composition.id}
          component={DemoVideo}
          durationInFrames={millisecondsToFrames(timing.totalDurationMs)}
          fps={fps}
          width={vertical ? 1080 : 1920}
          height={vertical ? 1920 : 1080}
          defaultProps={{
            composition,
            timing,
            captions: captionsByComposition[composition.id],
            disclosure: script.disclosure,
            organization: script.organization
          }}
        />
      );
    })}
  </>
);

function getTiming(id: CompositionDefinition["id"]): CompositionTiming {
  const timing = timings.compositions.find((candidate) => candidate.id === id);
  if (!timing) {
    throw new Error(`Missing measured timing metadata for ${id}.`);
  }
  return timing;
}

function millisecondsToFrames(milliseconds: number) {
  return Math.max(1, Math.round((milliseconds / 1000) * fps));
}
