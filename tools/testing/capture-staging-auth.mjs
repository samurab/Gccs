import { chmod, mkdir, writeFile } from "node:fs/promises";
import path from "node:path";
import process from "node:process";
import { chromium } from "playwright";

const profiles = {
  manager: {
    fileName: "staging-manager.json",
    required: ["ManageEvidence", "ManageReports", "ViewReports"],
    forbidden: []
  },
  auditor: {
    fileName: "staging-auditor.json",
    required: ["ViewEvidence", "ViewReports"],
    forbidden: ["ManageEvidence", "ManageReports"]
  }
};

const profileName = process.argv[2];
const profile = profiles[profileName];
if (!profile) throw new Error("Choose an auth profile: manager or auditor.");

const baseURL = requireStagingUrl("PLAYWRIGHT_STAGING_WEB_URL");
const stagingEmail = process.env.PLAYWRIGHT_STAGING_EMAIL?.trim();
const authDirectory = path.resolve(process.env.PLAYWRIGHT_STAGING_AUTH_DIR ?? ".auth");
const outputPath = path.join(authDirectory, profile.fileName);
const browser = await chromium.launch({ headless: Boolean(stagingEmail) });

try {
  const context = await browser.newContext();
  const page = await context.newPage();
  console.log(stagingEmail
    ? `Starting real staging email-code sign-in for the ${profileName} identity.`
    : `Complete the real staging sign-in for the ${profileName} identity in the opened browser.`);
  let accessResponse;
  if (stagingEmail) {
    await page.goto(`${baseURL}/app`);
    accessResponse = await completeEmailCodeSignIn(page, stagingEmail);
  } else {
    const accessResponsePromise = waitForAccessResponse(page);
    await page.goto(`${baseURL}/app`);
    accessResponse = await accessResponsePromise;
  }
  if (!accessResponse.request().headers()["authorization"]?.startsWith("Bearer ")) {
    throw new Error("The staging access check did not use bearer authentication.");
  }
  if (accessResponse.request().headers()["x-gccs-dev-auth"]) {
    throw new Error("Development authentication must not be used for staging evidence.");
  }
  const access = await accessResponse.json();
  assertPermissions(access.permissions ?? [], profile);
  await page.getByText(/^Signed in as /).waitFor({ timeout: 30_000 });

  const storageState = await context.storageState();
  const sessionStorage = await page.evaluate(() => Object.entries(window.sessionStorage));
  const state = {
    ...storageState,
    sessionStorage: { [new URL(baseURL).origin]: sessionStorage }
  };

  await mkdir(authDirectory, { recursive: true });
  await writeFile(outputPath, `${JSON.stringify(state, null, 2)}\n`, { mode: 0o600 });
  await chmod(outputPath, 0o600);
  console.log(`Captured ${profileName} staging auth state at ${outputPath}. Run the staging tests immediately.`);
} finally {
  await browser.close();
}

function requireStagingUrl(name) {
  const value = process.env[name];
  if (!value) throw new Error(`${name} is required.`);
  const url = new URL(value);
  if (url.protocol !== "https:" || ["localhost", "127.0.0.1"].includes(url.hostname)) {
    throw new Error(`${name} must be a deployed HTTPS URL.`);
  }
  return value.replace(/\/$/, "");
}

function assertPermissions(actual, profile) {
  for (const permission of profile.required) {
    if (!actual.includes(permission)) throw new Error(`Signed-in identity is missing ${permission}.`);
  }
  for (const permission of profile.forbidden) {
    if (actual.includes(permission)) throw new Error(`Signed-in identity unexpectedly has ${permission}.`);
  }
}

async function completeEmailCodeSignIn(page, email) {
  await page.getByRole("button", { name: "Sign in with email code" }).click();

  const emailInput = page.getByRole("textbox", { name: "Email address" });
  await emailInput.waitFor({ state: "visible", timeout: 60_000 });
  console.log("Customer sign-in page reached.");
  await emailInput.fill(email);
  await page.getByRole("button", { name: /Send (verification )?code|Continue|Next/i }).first().click();

  const codeInput = page.getByRole("textbox", { name: /Enter (the )?code you received/i });
  const accountMissing = page.getByText("We couldn't find an account with this email address.");
  const nextStep = await Promise.race([
    codeInput.waitFor({ state: "visible", timeout: 60_000 }).then(() => "code"),
    accountMissing.waitFor({ state: "visible", timeout: 60_000 }).then(() => "create")
  ]);
  if (nextStep === "create") {
    await page.getByRole("button", { name: "No account? Create one" }).click();
    await page.getByRole("textbox", { name: "Email" }).fill(email);
    await page.getByRole("button", { name: "Next" }).click();
    await codeInput.waitFor({ state: "visible", timeout: 60_000 });
  }

  console.log("Verification code requested.");
  const code = await readSecret("Enter the staging email verification code: ");
  if (!code) throw new Error("A staging email verification code is required.");
  await codeInput.fill(code);
  const submit = page.getByRole("button", { name: /^(Sign in|Next)$/ });
  const firstAccessResponse = waitForAccessResponse(page, 30_000).catch(() => null);
  await submit.click();
  console.log("Verification code submitted.");
  await page.getByText(/^Signed in as /).waitFor({ timeout: 60_000 });

  const accessResponse = await firstAccessResponse;
  if (accessResponse) return accessResponse;

  const retryAccessResponse = waitForAccessResponse(page);
  await page.reload();
  return await retryAccessResponse;
}

function waitForAccessResponse(page, timeout = 10 * 60 * 1000) {
  return page.waitForResponse(
    response => new URL(response.url()).pathname === "/api/me/access" && response.request().method() === "GET" && response.status() === 200,
    { timeout }
  );
}

function readSecret(prompt) {
  if (!process.stdin.isTTY || !process.stdout.isTTY || typeof process.stdin.setRawMode !== "function") {
    throw new Error("Interactive terminal input is required for the staging email verification code.");
  }

  process.stdout.write(prompt);
  return new Promise((resolve, reject) => {
    let value = "";
    const wasRaw = process.stdin.isRaw;
    const finish = callback => {
      process.stdin.off("data", onData);
      process.stdin.setRawMode(Boolean(wasRaw));
      process.stdin.pause();
      process.stdout.write("\n");
      callback();
    };
    const onData = chunk => {
      for (const character of chunk) {
        if (character === "\u0003") {
          finish(() => reject(new Error("Staging authentication cancelled.")));
          return;
        }
        if (character === "\r" || character === "\n") {
          finish(() => resolve(value.trim()));
          return;
        }
        if (character === "\u007f" || character === "\b") {
          value = value.slice(0, -1);
        } else if (character >= " ") {
          value += character;
        }
      }
    };

    process.stdin.setEncoding("utf8");
    process.stdin.setRawMode(true);
    process.stdin.resume();
    process.stdin.on("data", onData);
  });
}
