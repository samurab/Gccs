import {Audio, Video} from "@remotion/media";
import {
  AbsoluteFill,
  Easing,
  Img,
  Interactive,
  Sequence,
  interpolate,
  staticFile,
  useCurrentFrame,
  useVideoConfig
} from "remotion";
import type {
  CaptionCue,
  CompositionDefinition,
  CompositionTiming,
  SceneDefinition,
  SceneTiming
} from "./types";

type DemoVideoProps = {
  composition: CompositionDefinition;
  timing: CompositionTiming;
  captions: CaptionCue[];
  disclosure: string;
  organization: string;
};

const palette = {
  ink: "#081522",
  navy: "#10263b",
  blue: "#1d5576",
  cyan: "#67d6d0",
  gold: "#d7ac54",
  cream: "#fbfaf5",
  white: "#ffffff",
  muted: "#a8bdc9"
};

export const DemoVideo = ({
  composition,
  timing,
  captions,
  disclosure,
  organization
}: DemoVideoProps) => {
  const {fps} = useVideoConfig();
  const sceneById = new Map(composition.scenes.map((scene) => [scene.id, scene]));

  return (
    <AbsoluteFill style={{backgroundColor: palette.ink, fontFamily: "Inter, Avenir Next, Helvetica, Arial, sans-serif"}}>
      <AmbientBackground />
      {timing.scenes.map((sceneTiming, index) => {
        const scene = sceneById.get(sceneTiming.id);
        if (!scene) throw new Error(`Missing scene definition for ${sceneTiming.id}.`);
        return (
          <Sequence
            key={scene.id}
            from={frameOffset(sceneTiming.startMs, fps)}
            durationInFrames={frameCount(sceneTiming.durationMs, fps)}
            name={scene.title}
          >
            <Scene
              scene={scene}
              timing={sceneTiming}
              transitionMs={timing.transitionMs}
              captions={captions}
              organization={organization}
              disclosure={disclosure}
              isFirst={index === 0}
              isLast={index === timing.scenes.length - 1}
              vertical={composition.id === "Social30"}
            />
          </Sequence>
        );
      })}
    </AbsoluteFill>
  );
};

type SceneProps = {
  scene: SceneDefinition;
  timing: SceneTiming;
  transitionMs: number;
  captions: CaptionCue[];
  organization: string;
  disclosure: string;
  isFirst: boolean;
  isLast: boolean;
  vertical: boolean;
};

const Scene = ({
  scene,
  timing,
  transitionMs,
  captions,
  organization,
  disclosure,
  isFirst,
  isLast,
  vertical
}: SceneProps) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const durationFrames = frameCount(timing.durationMs, fps);
  const transitionFrames = frameCount(transitionMs, fps);
  const fadeIn = isFirst
    ? 1
    : interpolate(frame, [0, transitionFrames], [0, 1], {
        extrapolateLeft: "clamp",
        extrapolateRight: "clamp"
      });
  const fadeOut = isLast
    ? 1
    : interpolate(frame, [durationFrames - transitionFrames, durationFrames], [1, 0], {
        extrapolateLeft: "clamp",
        extrapolateRight: "clamp"
      });
  const opacity = Math.min(fadeIn, fadeOut);
  const globalTimeMs = timing.startMs + (frame / fps) * 1000;
  const activeCaption = captions.find(
    (caption) => globalTimeMs >= caption.startMs && globalTimeMs < caption.endMs
  );
  const captionAgeFrames = activeCaption
    ? frameOffset(globalTimeMs - activeCaption.startMs, fps)
    : 0;
  const audioStartFrame = frameOffset(timing.leadInMs, fps);
  const showClosingCard = isLast && frame >= Math.max(0, durationFrames - frameCount(5_500, fps));

  return (
    <AbsoluteFill style={{opacity}}>
      {isFirst && scene.captureAsset === null ? (
        <OpeningCard
          organization={organization}
          vertical={vertical}
          activeCaption={activeCaption}
          captionAgeFrames={captionAgeFrames}
        />
      ) : (
        <ProductScene
          scene={scene}
          activeCaption={activeCaption}
          captionAgeFrames={captionAgeFrames}
          vertical={vertical}
        />
      )}

      {showClosingCard ? (
        <AbsoluteFill style={{opacity: interpolate(frame, [durationFrames - frameCount(5_500, fps), durationFrames - frameCount(4_900, fps)], [0, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp"})}}>
          <ClosingCard
            disclosure={disclosure}
            vertical={vertical}
            activeCaption={activeCaption}
            captionAgeFrames={captionAgeFrames}
          />
        </AbsoluteFill>
      ) : null}

      <Sequence from={audioStartFrame} durationInFrames={frameCount(timing.audioDurationMs, fps)}>
        <Audio src={staticFile(`narration/${scene.id}.wav`)} />
      </Sequence>
    </AbsoluteFill>
  );
};

const ProductScene = ({
  scene,
  activeCaption,
  captionAgeFrames,
  vertical
}: {
  scene: SceneDefinition;
  activeCaption?: CaptionCue;
  captionAgeFrames: number;
  vertical: boolean;
}) => {
  const frame = useCurrentFrame();
  const entrance = interpolate(frame, [0, 18], [18, 0], {extrapolateLeft: "clamp", extrapolateRight: "clamp"});

  if (vertical) {
    return (
      <AbsoluteFill style={{padding: "104px 64px 88px", color: palette.white}}>
        <BrandLockup compact />
        <Interactive.Div name="Section label" style={{marginTop: 96, color: palette.cyan, fontSize: 26, fontWeight: 750, letterSpacing: 2.2, textTransform: "uppercase", opacity: interpolate(frame, [4, 18], [0, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1)}), translate: interpolate(frame, [4, 18], ["0px 14px", "0px 0px"], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1)})}}>{scene.section}</Interactive.Div>
        <Interactive.Div name="Scene title" style={{fontSize: 58, lineHeight: 1.04, fontWeight: 750, marginTop: 18, maxWidth: 920, opacity: interpolate(frame, [8, 24], [0, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1)}), translate: interpolate(frame, [8, 24], ["0px 18px", "0px 0px"], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1)})}}>{scene.title}</Interactive.Div>
        {scene.captureAsset ? (
          <CaptureWindow scene={scene} vertical />
        ) : null}
        <Interactive.Div name="Feature statement" style={{marginTop: 52, borderLeft: `6px solid ${palette.gold}`, paddingLeft: 28, fontSize: 32, lineHeight: 1.3, color: palette.cream, opacity: interpolate(frame, [18, 34], [0, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp"}), translate: interpolate(frame, [18, 34], ["18px 0px", "0px 0px"], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1)})}}>{scene.onScreenCaption}</Interactive.Div>
        <div style={{marginTop: 34, minHeight: 210, borderRadius: 24, background: "rgba(255,255,255,.96)", padding: "30px 34px", color: palette.ink, fontSize: 34, lineHeight: 1.28, boxShadow: "0 22px 55px rgba(0,0,0,.25)"}}>
          <CaptionText activeCaption={activeCaption} captionAgeFrames={captionAgeFrames} vertical />
        </div>
        <Interactive.Div name="Feature callout" style={{marginTop: 28, color: palette.muted, fontSize: 24, opacity: interpolate(frame, [26, 42], [0, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp"})}}>{scene.callout}</Interactive.Div>
      </AbsoluteFill>
    );
  }

  return (
    <AbsoluteFill style={{padding: "54px 72px 48px", color: palette.white, translate: `0px ${entrance}px`}}>
      <div style={{display: "flex", justifyContent: "space-between", alignItems: "center"}}>
        <BrandLockup compact />
        <Interactive.Div name="Section label" style={{fontSize: 20, letterSpacing: 1.8, textTransform: "uppercase", color: palette.cyan, fontWeight: 750, opacity: interpolate(frame, [2, 16], [0, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp"}), translate: interpolate(frame, [2, 16], ["20px 0px", "0px 0px"], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1)})}}>{scene.section}</Interactive.Div>
      </div>
      <div style={{display: "grid", gridTemplateColumns: "1400px 1fr", gap: 34, marginTop: 34, minHeight: 846}}>
        {scene.captureAsset ? <CaptureWindow scene={scene} vertical={false} /> : <div style={{height: "100%", display: "grid", placeItems: "center"}}><BrandLockup /></div>}
        <aside style={{display: "flex", flexDirection: "column", minWidth: 0}}>
          <Interactive.Div name="Scene title" style={{fontSize: 48, lineHeight: 1.05, fontWeight: 760, opacity: interpolate(frame, [4, 18], [0, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1)}), translate: interpolate(frame, [4, 18], ["18px 0px", "0px 0px"], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1)})}}>{scene.title}</Interactive.Div>
          <Interactive.Div name="Feature statement" style={{marginTop: 30, color: palette.cream, borderLeft: `5px solid ${palette.gold}`, paddingLeft: 22, fontSize: 25, lineHeight: 1.35, opacity: interpolate(frame, [12, 28], [0, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp"}), translate: interpolate(frame, [12, 28], ["18px 0px", "0px 0px"], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1)})}}>{scene.onScreenCaption}</Interactive.Div>
          <Interactive.Div name="Feature callout" style={{marginTop: 30, color: palette.cyan, fontSize: 21, lineHeight: 1.35, fontWeight: 650, borderRadius: 999, border: "1px solid rgba(103,214,208,.3)", background: "rgba(103,214,208,.08)", padding: "12px 15px", opacity: interpolate(frame, [20, 36], [0, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp"}), scale: interpolate(frame, [20, 36], [.96, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1), output: "perceptual-scale"})}}>{scene.callout}</Interactive.Div>
          <div style={{marginTop: "auto", minHeight: 270, borderRadius: 20, background: "rgba(255,255,255,.97)", padding: "27px 28px", color: palette.ink, boxShadow: "0 20px 60px rgba(0,0,0,.28)", display: "flex", alignItems: "center"}}>
            <CaptionText activeCaption={activeCaption} captionAgeFrames={captionAgeFrames} vertical={false} />
          </div>
        </aside>
      </div>
    </AbsoluteFill>
  );
};

const CaptureWindow = ({scene, vertical}: {scene: SceneDefinition; vertical: boolean}) => {
  const frame = useCurrentFrame();
  const {durationInFrames} = useVideoConfig();
  const focusOrigin = captureFocusOrigin(scene.captureAsset);

  return (
    <Interactive.Div
      name={`Product capture · ${scene.title}`}
      style={{
        position: "relative",
        marginTop: vertical ? 58 : 0,
        border: vertical ? `2px solid ${palette.blue}` : `1px solid ${palette.blue}`,
        borderRadius: vertical ? 30 : 22,
        overflow: "hidden",
        boxShadow: vertical ? "0 30px 80px rgba(0,0,0,.42)" : "0 30px 90px rgba(0,0,0,.42)",
        height: vertical ? 554 : "100%",
        background: palette.navy,
        opacity: interpolate(frame, [0, 14], [0, 1], {
          extrapolateLeft: "clamp",
          extrapolateRight: "clamp",
          easing: Easing.bezier(.16, 1, .3, 1)
        }),
        scale: interpolate(frame, [0, 18], [.985, 1], {
          extrapolateLeft: "clamp",
          extrapolateRight: "clamp",
          easing: Easing.bezier(.16, 1, .3, 1),
          output: "perceptual-scale"
        })
      }}
    >
      {scene.captureAsset ? (
        <Video
          name={`Verified browser capture · ${scene.captureAsset}`}
          src={staticFile(`captures/${scene.captureAsset}`)}
          muted
          objectFit="contain"
          style={{
            width: "100%",
            height: "100%",
            transformOrigin: focusOrigin,
            scale: interpolate(
              frame,
              [0, durationInFrames * .28, durationInFrames * .62, durationInFrames - 1],
              [1.002, 1.034, 1.018, 1.042],
              {
                extrapolateLeft: "clamp",
                extrapolateRight: "clamp",
                easing: Easing.bezier(.33, 0, .2, 1),
                output: "perceptual-scale"
              }
            )
          }}
        />
      ) : null}
      <div style={{position: "absolute", inset: 0, pointerEvents: "none", boxShadow: "inset 0 0 0 1px rgba(255,255,255,.08), inset 0 -70px 80px rgba(8,21,34,.08)"}} />
      <Interactive.Div
        name="Walkthrough progress"
        style={{
          position: "absolute",
          left: 0,
          bottom: 0,
          height: vertical ? 6 : 5,
          width: interpolate(frame, [0, durationInFrames - 1], ["0%", "100%"], {
            extrapolateLeft: "clamp",
            extrapolateRight: "clamp",
            easing: Easing.linear
          }),
          background: `linear-gradient(90deg, ${palette.cyan}, ${palette.gold})`,
          boxShadow: "0 -2px 14px rgba(103,214,208,.55)"
        }}
      />
      <div style={{position: "absolute", left: 16, top: 16, width: 34, height: 34, borderLeft: `3px solid ${palette.gold}`, borderTop: `3px solid ${palette.gold}`, opacity: interpolate(frame, [8, 22], [0, .8], {extrapolateLeft: "clamp", extrapolateRight: "clamp"})}} />
      <div style={{position: "absolute", right: 16, bottom: 18, width: 34, height: 34, borderRight: `3px solid ${palette.cyan}`, borderBottom: `3px solid ${palette.cyan}`, opacity: interpolate(frame, [12, 28], [0, .72], {extrapolateLeft: "clamp", extrapolateRight: "clamp"})}} />
    </Interactive.Div>
  );
};

const CaptionText = ({
  activeCaption,
  captionAgeFrames,
  vertical
}: {
  activeCaption?: CaptionCue;
  captionAgeFrames: number;
  vertical: boolean;
}) => (
  <Interactive.Div
    name="Active caption"
    style={{
      fontSize: vertical ? 34 : 29,
      lineHeight: 1.32,
      fontWeight: 560,
      opacity: activeCaption
        ? interpolate(captionAgeFrames, [0, 8], [0, 1], {
            extrapolateLeft: "clamp",
            extrapolateRight: "clamp",
            easing: Easing.bezier(.16, 1, .3, 1)
          })
        : 0,
      translate: activeCaption
        ? interpolate(captionAgeFrames, [0, 8], ["0px 10px", "0px 0px"], {
            extrapolateLeft: "clamp",
            extrapolateRight: "clamp",
            easing: Easing.bezier(.16, 1, .3, 1)
          })
        : "0px 10px"
    }}
  >
    {activeCaption?.text ?? ""}
  </Interactive.Div>
);

const OpeningCard = ({
  organization,
  vertical,
  activeCaption,
  captionAgeFrames
}: {
  organization: string;
  vertical: boolean;
  activeCaption?: CaptionCue;
  captionAgeFrames: number;
}) => {
  const frame = useCurrentFrame();
  const scale = interpolate(frame, [0, 32], [0.94, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp"});
  return (
    <AbsoluteFill style={{display: "flex", alignItems: "center", justifyContent: "center", color: palette.white, padding: vertical ? 80 : 120}}>
      <Interactive.Div name="Opening statement" style={{textAlign: "center", scale, maxWidth: vertical ? 940 : 1420, opacity: interpolate(frame, [0, 22], [0, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1)})}}>
        <BrandLockup />
        <div style={{fontSize: vertical ? 64 : 72, lineHeight: 1.08, fontWeight: 760, marginTop: 72}}>Organize readiness work in one No‑CUI operating view.</div>
        <div style={{margin: "42px auto 0", width: interpolate(frame, [18, 42], [0, 110], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1)}), height: 5, borderRadius: 9, background: palette.gold}} />
        <div style={{marginTop: 36, color: palette.muted, fontSize: vertical ? 28 : 27}}>Fictional demonstration · {organization}</div>
      </Interactive.Div>
      <CardCaption activeCaption={activeCaption} captionAgeFrames={captionAgeFrames} vertical={vertical} />
    </AbsoluteFill>
  );
};

const ClosingCard = ({
  disclosure,
  vertical,
  activeCaption,
  captionAgeFrames
}: {
  disclosure: string;
  vertical: boolean;
  activeCaption?: CaptionCue;
  captionAgeFrames: number;
}) => {
  const frame = useCurrentFrame();
  return (
    <AbsoluteFill style={{background: `linear-gradient(145deg, ${palette.ink}, ${palette.navy})`, display: "flex", alignItems: "center", justifyContent: "center", color: palette.white, padding: vertical ? 76 : 120}}>
    <Interactive.Div name="Closing call to action" style={{textAlign: "center", maxWidth: vertical ? 940 : 1450, opacity: interpolate(frame, [0, 16], [0, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp"}), scale: interpolate(frame, [0, 20], [.96, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1), output: "perceptual-scale"})}}>
      <BrandLockup />
      <div style={{fontSize: vertical ? 65 : 76, lineHeight: 1.05, fontWeight: 760, marginTop: 62}}>Schedule a FeDril demonstration.</div>
      <div style={{fontSize: vertical ? 29 : 30, color: palette.cyan, marginTop: 34}}>Compliance management · No-CUI posture</div>
      <div style={{height: 2, background: `linear-gradient(90deg, transparent, ${palette.gold}, transparent)`, margin: vertical ? "64px auto 45px" : "54px auto 38px", width: interpolate(frame, [12, 34], ["0%", "100%"], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1)})}} />
      <div style={{fontSize: vertical ? 23 : 21, color: palette.muted, lineHeight: 1.45}}>Workflow guidance, not certification or legal advice.</div>
      <div style={{fontSize: vertical ? 22 : 20, color: palette.muted, marginTop: 14}}>{disclosure}</div>
    </Interactive.Div>
    <CardCaption activeCaption={activeCaption} captionAgeFrames={captionAgeFrames} vertical={vertical} />
  </AbsoluteFill>
  );
};

const CardCaption = ({
  activeCaption,
  captionAgeFrames,
  vertical
}: {
  activeCaption?: CaptionCue;
  captionAgeFrames: number;
  vertical: boolean;
}) => {
  if (!activeCaption) return null;
  return (
    <Interactive.Div
      name="Card caption"
      style={{
        position: "absolute",
        left: vertical ? 64 : "50%",
        right: vertical ? 64 : undefined,
        bottom: vertical ? 84 : 48,
        width: vertical ? undefined : 1400,
        borderRadius: vertical ? 24 : 18,
        background: "rgba(255,255,255,.97)",
        padding: vertical ? "26px 30px" : "20px 28px",
        color: palette.ink,
        boxShadow: "0 20px 60px rgba(0,0,0,.32)",
        fontSize: vertical ? 32 : 28,
        fontWeight: 560,
        lineHeight: 1.3,
        textAlign: "center",
        opacity: interpolate(captionAgeFrames, [0, 8], [0, 1], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1)}),
        translate: vertical
          ? interpolate(captionAgeFrames, [0, 8], ["0px 10px", "0px 0px"], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1)})
          : interpolate(captionAgeFrames, [0, 8], ["-50% 10px", "-50% 0px"], {extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.bezier(.16, 1, .3, 1)})
      }}
    >
      {activeCaption.text}
    </Interactive.Div>
  );
};

const BrandLockup = ({compact = false}: {compact?: boolean}) => (
  <div style={{display: "inline-flex", alignItems: "center", gap: compact ? 16 : 24}}>
    <Img src={staticFile("brand/F.svg")} style={{height: compact ? 62 : 110, width: compact ? 54 : 96, objectFit: "contain"}} />
    <span style={{fontSize: compact ? 40 : 72, fontWeight: 780, letterSpacing: -1.5, color: palette.white}}>FeDril</span>
  </div>
);

const AmbientBackground = () => {
  const frame = useCurrentFrame();
  const x = Math.sin(frame / 110) * 45;
  const y = Math.cos(frame / 145) * 35;
  return (
    <AbsoluteFill style={{overflow: "hidden"}}>
      <div style={{position: "absolute", width: 820, height: 820, borderRadius: "50%", left: -260 + x, top: -330 + y, background: "radial-gradient(circle, rgba(55,151,164,.25), transparent 67%)"}} />
      <div style={{position: "absolute", width: 1000, height: 1000, borderRadius: "50%", right: -420 - x, bottom: -520 - y, background: "radial-gradient(circle, rgba(215,172,84,.17), transparent 67%)"}} />
    </AbsoluteFill>
  );
};

function captureFocusOrigin(captureAsset: string | null) {
  switch (captureAsset) {
    case "scene-02-dashboard.webm":
      return "62% 22%";
    case "scene-03-gap-review.webm":
      return "58% 34%";
    case "scene-04-remediation.webm":
      return "61% 22%";
    case "scene-05-evidence.webm":
      return "58% 72%";
    case "scene-06-no-cui-boundary.webm":
      return "56% 66%";
    case "scene-07-auditability.webm":
      return "67% 68%";
    case "scene-08-reporting.webm":
      return "58% 67%";
    default:
      return "50% 50%";
  }
}

function frameCount(milliseconds: number, fps: number) {
  return Math.max(1, Math.round((milliseconds / 1000) * fps));
}

function frameOffset(milliseconds: number, fps: number) {
  return Math.max(0, Math.round((milliseconds / 1000) * fps));
}
