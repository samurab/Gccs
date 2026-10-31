import {expect, test, type Browser, type Locator, type Page} from "@playwright/test";
import {spawnSync} from "node:child_process";
import {promises as fs} from "node:fs";
import {dirname, resolve} from "node:path";
import {fileURLToPath} from "node:url";
import {seedDemo} from "../scripts/seed-demo.ts";
import scriptSource from "../narration/script.json" with {type: "json"};
import timingSource from "../narration/timings.json" with {type: "json"};

type CaptureLogEntry = {
  sceneId: string;
  status: "started" | "completed" | "failed";
  recordedAt: string;
  asset?: string;
  errorCode?: string;
};

type DemoActor = {
  role: "Admin" | "ComplianceManager";
  userId: string;
  email: string;
};

const projectRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const rawDirectory = resolve(projectRoot, "assets/capture/raw");
const stillDirectory = resolve(projectRoot, "assets/capture/stills");
const publicCaptureDirectory = resolve(projectRoot, "public/captures");
const executionLogPath = resolve(projectRoot, "assets/capture/execution-log.json");
const entries: CaptureLogEntry[] = [];
const demoTenantId = "11111111-1111-1111-1111-111111111113";
const complianceManager: DemoActor = {
  role: "ComplianceManager",
  userId: "22222222-2222-2222-2222-222222222243",
  email: "priya.shah.northstar@example.com"
};
const auditReviewer: DemoActor = {
  role: "Admin",
  userId: "22222222-2222-2222-2222-222222222242",
  email: "alex.morgan.northstar@example.com"
};

const scenes = [
  {id: "scene-02-dashboard", action: showDashboard, actor: complianceManager},
  {id: "scene-03-gap-review", action: showGapReview, actor: complianceManager},
  {id: "scene-04-remediation", action: assignRemediationOwner, actor: complianceManager},
  {id: "scene-05-evidence", action: showEvidenceMetadata, actor: complianceManager},
  {id: "scene-06-no-cui-boundary", action: showNoCuiBoundary, actor: complianceManager},
  {id: "scene-07-auditability", action: showAuditHistory, actor: auditReviewer},
  {id: "scene-08-reporting", action: showReadinessSummary, actor: complianceManager}
].map((scene) => {
  const requiredDurationMs = requiredCaptureDurationMs(scene.id);
  return {...scene, requiredDurationMs, holdMs: requiredDurationMs + 2_500};
});
const requestedSceneIds = new Set(
  (process.env.CAPTURE_SCENES ?? "")
    .split(",")
    .map((value) => value.trim())
    .filter(Boolean)
);
const scenesToCapture = requestedSceneIds.size === 0
  ? scenes
  : scenes.filter((scene) => requestedSceneIds.has(scene.id));

if (scenesToCapture.length !== (requestedSceneIds.size || scenes.length)) {
  throw new Error("CAPTURE_SCENES contains an unknown scene identifier.");
}

test("records the approved FeDril walkthrough", async ({browser}) => {
  await Promise.all([
    fs.mkdir(rawDirectory, {recursive: true}),
    fs.mkdir(stillDirectory, {recursive: true}),
    fs.mkdir(publicCaptureDirectory, {recursive: true})
  ]);
  await seedDemo();

  try {
    for (const scene of scenesToCapture) {
      await captureScene(browser, scene.id, scene.holdMs, scene.requiredDurationMs, scene.action, scene.actor);
    }
  } finally {
    await fs.writeFile(
      executionLogPath,
      `${JSON.stringify({version: 1, entries}, null, 2)}\n`,
      {encoding: "utf8", mode: 0o600}
    );
  }
});

async function captureScene(
  browser: Browser,
  sceneId: string,
  holdMs: number,
  requiredDurationMs: number,
  action: (page: Page) => Promise<void>,
  actor: DemoActor
) {
  entries.push({sceneId, status: "started", recordedAt: new Date().toISOString()});
  const context = await browser.newContext({
    viewport: {width: 1920, height: 1080},
    colorScheme: "light",
    locale: "en-US",
    recordVideo: {dir: rawDirectory, size: {width: 1920, height: 1080}}
  });
  let blockedExternalRequestCount = 0;
  await context.route(/^https?:\/\//, async (route) => {
    const requestUrl = new URL(route.request().url());
    if (requestUrl.hostname !== "127.0.0.1" && requestUrl.hostname !== "localhost") {
      blockedExternalRequestCount += 1;
      await route.abort("blockedbyclient");
      return;
    }
    await route.continue();
  });
  await context.addInitScript(
    ({tenantId: selectedTenantId, role, userId, email}) => {
      window.localStorage.setItem("gccs.selectedTenantId", selectedTenantId);
      window.localStorage.setItem("gccs.developmentRole", role);
      window.localStorage.setItem("gccs.developmentUserId", userId);
      window.localStorage.setItem("gccs.developmentUserEmail", email);
    },
    {tenantId: demoTenantId, ...actor}
  );
  const page = await context.newPage();
  const runtimeErrors: string[] = [];
  const failedApiStatuses: number[] = [];
  page.on("pageerror", () => runtimeErrors.push("page-error"));
  page.on("console", (message) => {
    if (message.type() === "error") runtimeErrors.push("console-error");
  });
  page.on("response", (response) => {
    if (response.url().includes("/api/") && response.status() >= 400) {
      failedApiStatuses.push(response.status());
    }
  });

  const video = page.video();
  const destination = resolve(publicCaptureDirectory, `${sceneId}.webm`);
  try {
    await page.goto("/app#/dashboard", {waitUntil: "domcontentloaded"});
    await page.waitForLoadState("networkidle");
    await expect(page.getByText("Northstar Precision Systems", {exact: true}).first()).toBeVisible();
    await installPresentationCursor(page);
    await action(page);
    await deliberatePause(page, 900);
    await assertSafeVisibleState(page, runtimeErrors, failedApiStatuses, blockedExternalRequestCount);
    await page.screenshot({path: resolve(stillDirectory, `${sceneId}.png`), animations: "disabled"});
    await deliberatePause(page, holdMs);
    await assertSafeVisibleState(page, runtimeErrors, failedApiStatuses, blockedExternalRequestCount);
    await context.close();
    if (!video) throw new Error("video-unavailable");
    await video.saveAs(destination);
    assertRecordedDuration(destination, requiredDurationMs, sceneId);
    entries.push({
      sceneId,
      status: "completed",
      recordedAt: new Date().toISOString(),
      asset: `public/captures/${sceneId}.webm`
    });
  } catch (error) {
    await context.close().catch(() => undefined);
    entries.push({
      sceneId,
      status: "failed",
      recordedAt: new Date().toISOString(),
      errorCode: "expected-element-missing-or-capture-failed"
    });
    throw new Error(`FeDril walkthrough capture failed for ${sceneId}.`, {cause: error});
  }
}

function requiredCaptureDurationMs(sceneId: string) {
  const script = scriptSource as {
    compositions: Array<{id: string; scenes: Array<{id: string; captureAsset: string | null}>}>;
  };
  const timings = timingSource as {
    compositions: Array<{id: string; scenes: Array<{id: string; durationMs: number}>}>;
  };
  const assetName = `${sceneId}.webm`;
  const durations: number[] = [];
  for (const composition of script.compositions) {
    const timing = timings.compositions.find((candidate) => candidate.id === composition.id);
    if (!timing) throw new Error(`Capture timing is missing for ${composition.id}.`);
    for (const scene of composition.scenes.filter((candidate) => candidate.captureAsset === assetName)) {
      const sceneTiming = timing.scenes.find((candidate) => candidate.id === scene.id);
      if (!sceneTiming) throw new Error(`Capture timing is missing for ${scene.id}.`);
      durations.push(sceneTiming.durationMs);
    }
  }
  if (durations.length === 0) throw new Error(`No composition references ${assetName}.`);
  return Math.max(...durations);
}

function assertRecordedDuration(path: string, requiredDurationMs: number, sceneId: string) {
  const remotion = resolve(projectRoot, "../../node_modules/.bin/remotion");
  const result = spawnSync(remotion, [
    "ffprobe",
    "-v", "quiet",
    "-print_format", "json",
    "-show_format",
    path
  ], {encoding: "utf8"});
  if (result.status !== 0) throw new Error(`Duration inspection failed for ${sceneId}.`);
  const durationMs = Number((JSON.parse(result.stdout) as {format?: {duration?: string}}).format?.duration ?? "0") * 1000;
  if (!Number.isFinite(durationMs) || durationMs < requiredDurationMs + 500) {
    throw new Error(`${sceneId} recording is too short for measured narration timing; recapture after narration generation.`);
  }
}

async function showDashboard(page: Page) {
  await expect(page.getByRole("heading", {name: "Dashboard", exact: true})).toBeVisible();
  const metrics = page.getByLabel("Workspace priority summary");
  await expect(metrics).toContainText("No-CUI");
  await movePointerTo(page, metrics.getByText("High risk", {exact: true}).first());
  await movePointerTo(page, metrics.getByText("Overdue", {exact: true}).first());
  await movePointerTo(page, page.getByText("No-CUI / compliance management only", {exact: true}).last());
}

async function showGapReview(page: Page) {
  await openPrimaryObligation(page);
  const detail = page.getByLabel("Obligation detail");
  await expect(detail).toContainText("FAR 52.204-21");
  await movePointerTo(page, detail.getByText("FAR 52.204-21", {exact: true}).first());
  await movePointerTo(page, detail.getByText("Linked tasks", {exact: true}));
}

async function assignRemediationOwner(page: Page) {
  await openPrimaryObligation(page);
  const detail = page.getByLabel("Obligation detail");
  const ownerKind = detail.getByTestId("obligation-owner-kind");
  await indicateControlChange(page, ownerKind);
  await ownerKind.selectOption("user");
  const ownerMember = detail.getByTestId("obligation-owner-member");
  await indicateControlChange(page, ownerMember);
  await ownerMember.selectOption({label: "Priya Shah"});
  const notify = detail.getByLabel("Also send assignment email");
  await movePointerTo(page, notify);
  if (await notify.isChecked()) await clickWithPointer(page, notify);
  const assignmentResponse = page.waitForResponse(
    (response) => response.request().method() === "PATCH" && response.url().includes("/owner")
  );
  await clickWithPointer(page, detail.getByTestId("obligation-owner-submit"));
  await expect((await assignmentResponse).ok()).toBeTruthy();
  await expect(detail).toContainText("Obligation owner assigned.");
  await expect(detail).toContainText("Priya Shah (tenant member)");
  await expect(detail).toContainText("due 2026-07-24");
  await movePointerTo(page, detail.getByText("Obligation owner assigned.", {exact: true}));
  await movePointerTo(page, detail.getByRole("status").filter({hasText: "Priya Shah"}).first());
}

async function showEvidenceMetadata(page: Page) {
  await clickWithPointer(page, page.getByRole("link", {name: "Evidence"}));
  await expect(page.getByRole("heading", {name: "No-CUI evidence management"})).toBeVisible();
  const evidence = page.getByTestId("evidence-item").filter({hasText: "Northstar quarterly access review summary"});
  await expect(evidence).toHaveCount(1);
  await clickWithPointer(page, evidence);
  const evidenceWorkspace = page.getByLabel("Evidence metadata");
  await expect(evidenceWorkspace.getByLabel("Title")).toHaveValue("Northstar quarterly access review summary");
  const obligationInput = evidenceWorkspace.getByLabel("Obligations");
  await expect(obligationInput).toHaveValue("far-52-204-21");
  await movePointerTo(page, obligationInput);
}

async function showNoCuiBoundary(page: Page) {
  await clickWithPointer(page, page.getByRole("link", {name: "Evidence"}));
  await expect(page.getByRole("heading", {name: "No-CUI evidence management"})).toBeVisible();
  const boundary = page.getByText("No-CUI acknowledgement", {exact: true});
  await boundary.scrollIntoViewIfNeeded();
  await expect(boundary).toBeVisible();
  await expect(page.getByText("Upload is disabled until the No-CUI notice is acknowledged.")).toBeVisible();
  await movePointerTo(page, boundary);
  await movePointerTo(page, page.getByLabel("I will not upload, paste, import, or attach real CUI."));
  await movePointerTo(page, page.getByLabel("I will use synthetic, redacted, or non-sensitive data during the pilot."));
  await movePointerTo(page, page.getByRole("button", {name: "I acknowledge the No-CUI upload limitation"}));
}

async function showAuditHistory(page: Page) {
  await clickWithPointer(page, page.getByRole("link", {name: "Settings"}));
  const auditTable = page.getByRole("table", {name: "Tenant audit logs"});
  await auditTable.scrollIntoViewIfNeeded();
  await expect(auditTable).toBeVisible();
  await expect(page.getByTestId("audit-row").first()).toContainText("Tenant user");
  await movePointerTo(page, page.getByRole("button", {name: "Filter"}));
  await movePointerTo(page, page.getByTestId("audit-row").first());
}

async function showReadinessSummary(page: Page) {
  await clickWithPointer(page, page.getByRole("link", {name: "Reports"}));
  await expect(page.getByRole("heading", {name: "Reports and audit packages"})).toBeVisible();
  const report = page.getByTestId("report-card").filter({hasText: "Compliance status report"}).first();
  await expect(report).toBeVisible();
  await clickWithPointer(page, report);
  const detail = page.getByLabel("Generated report detail");
  await detail.scrollIntoViewIfNeeded();
  await expect(detail).toBeVisible();
  await expect(detail).toContainText("Artifact limitations");
  await movePointerTo(page, detail.getByText("Artifact limitations", {exact: true}));
  await movePointerTo(page, detail.getByText("Overdue tasks", {exact: true}));
}

async function openPrimaryObligation(page: Page) {
  await clickWithPointer(page, page.getByRole("link", {name: "Obligations"}));
  await expect(page.getByText("Obligation work queue", {exact: true})).toBeVisible();
  const card = page.getByTestId("obligation-card").filter({hasText: "FAR 52.204-21"});
  await expect(card).toHaveCount(1);
  await movePointerTo(page, card.getByText("High", {exact: true}));
  await clickWithPointer(page, card.getByRole("button", {name: "View details"}));
  const detail = page.getByLabel("Obligation detail");
  await detail.scrollIntoViewIfNeeded();
  await expect(detail).toContainText("Basic Safeguarding of Covered Contractor Information Systems");
}

async function installPresentationCursor(page: Page) {
  await page.evaluate(() => {
    document.getElementById("fedril-demo-pointer")?.remove();
    const pointer = document.createElement("div");
    pointer.id = "fedril-demo-pointer";
    pointer.setAttribute("aria-hidden", "true");
    pointer.dataset.x = "84";
    pointer.dataset.y = "84";
    pointer.style.cssText = [
      "position:fixed",
      "left:0",
      "top:0",
      "width:34px",
      "height:42px",
      "z-index:2147483646",
      "pointer-events:none",
      "opacity:0",
      "transform:translate3d(84px,84px,0)",
      "filter:drop-shadow(0 3px 5px rgba(8,21,34,.42))"
    ].join(";");
    pointer.innerHTML = `
      <svg width="34" height="42" viewBox="0 0 34 42" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M3 2.5L29 25.5L18.2 27.3L24.2 38L18.2 41L12.4 30.4L4.2 38L3 2.5Z" fill="white" stroke="#081522" stroke-width="2.5" stroke-linejoin="round"/>
      </svg>`;
    document.body.append(pointer);
  });
}

async function movePointerTo(
  page: Page,
  locator: Locator,
  options: {durationMs?: number; settleMs?: number} = {}
) {
  await locator.scrollIntoViewIfNeeded();
  await expect(locator).toBeVisible();
  const box = await locator.boundingBox();
  if (!box) throw new Error("presentation-pointer-target-unavailable");
  const x = Math.round(box.x + Math.min(Math.max(box.width * 0.58, 12), Math.max(12, box.width - 12)));
  const y = Math.round(box.y + Math.min(Math.max(box.height * 0.52, 10), Math.max(10, box.height - 10)));
  const durationMs = options.durationMs ?? 720;

  await Promise.all([
    page.mouse.move(x, y, {steps: 18}),
    page.evaluate(
      ({nextX, nextY, duration}) => new Promise<void>((resolveMove) => {
        const pointer = document.getElementById("fedril-demo-pointer");
        if (!(pointer instanceof HTMLElement)) throw new Error("presentation-pointer-missing");
        const previousX = Number(pointer.dataset.x ?? "84");
        const previousY = Number(pointer.dataset.y ?? "84");
        pointer.style.opacity = "1";
        const animation = pointer.animate(
          [
            {transform: `translate3d(${previousX}px,${previousY}px,0)`},
            {transform: `translate3d(${nextX}px,${nextY}px,0)`}
          ],
          {duration, easing: "cubic-bezier(.22,.8,.3,1)", fill: "forwards"}
        );
        animation.onfinish = () => {
          pointer.style.transform = `translate3d(${nextX}px,${nextY}px,0)`;
          pointer.dataset.x = String(nextX);
          pointer.dataset.y = String(nextY);
          animation.cancel();
          resolveMove();
        };
      }),
      {nextX: x, nextY: y, duration: durationMs}
    )
  ]);
  await deliberatePause(page, options.settleMs ?? 300);
}

async function indicateControlChange(page: Page, locator: Locator) {
  await movePointerTo(page, locator);
  await showClickRipple(page);
  await deliberatePause(page, 240);
}

async function clickWithPointer(page: Page, locator: Locator) {
  await movePointerTo(page, locator);
  await showClickRipple(page);
  await locator.click();
  await deliberatePause(page, 520);
}

async function showClickRipple(page: Page) {
  await page.evaluate(() => {
    const pointer = document.getElementById("fedril-demo-pointer");
    if (!(pointer instanceof HTMLElement)) throw new Error("presentation-pointer-missing");
    const ripple = document.createElement("div");
    ripple.setAttribute("aria-hidden", "true");
    ripple.style.cssText = [
      "position:fixed",
      `left:${Number(pointer.dataset.x ?? "0") - 15}px`,
      `top:${Number(pointer.dataset.y ?? "0") - 15}px`,
      "width:30px",
      "height:30px",
      "border:3px solid #d7ac54",
      "border-radius:999px",
      "z-index:2147483645",
      "pointer-events:none"
    ].join(";");
    document.body.append(ripple);
    const animation = ripple.animate(
      [
        {opacity: 0.95, transform: "scale(.45)"},
        {opacity: 0, transform: "scale(2.4)"}
      ],
      {duration: 520, easing: "cubic-bezier(.16,1,.3,1)"}
    );
    animation.onfinish = () => ripple.remove();
  });
}

async function assertSafeVisibleState(
  page: Page,
  runtimeErrors: string[],
  failedApiStatuses: number[],
  blockedExternalRequestCount: number
) {
  const visibleText = await page.locator("body").innerText();
  expect(visibleText).not.toMatch(/\bGCCS\b|@gccs\.local/i);
  expect(visibleText).not.toMatch(/localhost|127\.0\.0\.1|OPENAI_API_KEY|Bearer\s+[A-Za-z0-9._-]+/i);
  expect(visibleText).not.toMatch(/\bUAT(?:-|\b)/i);
  expect(visibleText).not.toMatch(/\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/i);
  const visibleErrorMessages = await page.locator(".form-status--error:visible").allTextContents();
  expect(
    visibleErrorMessages.filter(
      (message) => !message.includes("Upload is disabled until the No-CUI notice is acknowledged.")
    )
  ).toEqual([]);
  expect(runtimeErrors).toEqual([]);
  expect(failedApiStatuses).toEqual([]);
  expect(blockedExternalRequestCount).toBe(0);
}

async function deliberatePause(page: Page, milliseconds: number) {
  await page.waitForTimeout(milliseconds);
}
