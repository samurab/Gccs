import { pathToFileURL } from "node:url";
const [endpoint, scriptPath, line] = process.argv.slice(2);
const socket = new WebSocket(endpoint);
let nextId = 1;
let breakpointId;
let paused = false;
let everPaused = false;
let released = false;
const pending = new Map();
const timeout = setTimeout(() => { console.error("Startup stress debugger timed out"); process.exit(1); }, 300_000);
function report(event, extra = {}) { console.log(JSON.stringify({ event, ...extra })); }
function send(method, params = {}) {
  const id = nextId++;
  return new Promise((resolve, reject) => {
    pending.set(id, { resolve, reject });
    socket.send(JSON.stringify({ id, method, params }));
  });
}
socket.addEventListener("message", (event) => {
  const message = JSON.parse(event.data);
  if (message.id) {
    const handlers = pending.get(message.id);
    if (handlers) {
      pending.delete(message.id);
      if (message.error) handlers.reject(new Error(JSON.stringify(message.error)));
      else handlers.resolve(message.result);
    }
    return;
  }
  if (message.method === "Debugger.paused") {
    paused = true;
    if (!released && message.params.hitBreakpoints?.includes(breakpointId)) {
      everPaused = true;
      report("paused-at-lease", { breakpointId, location: message.params.callFrames[0].location });
    } else {
      void send("Debugger.resume").then(() => { paused = false; report("running"); })
        .catch((error) => { console.error(error); process.exit(1); });
    }
  }
});
socket.addEventListener("close", () => { clearTimeout(timeout); process.exit(everPaused || released ? 0 : 1); });
socket.addEventListener("error", () => { if (!everPaused && !released) process.exitCode = 1; });
await new Promise((resolve, reject) => {
  socket.addEventListener("open", resolve, { once: true });
  socket.addEventListener("error", reject, { once: true });
});
await send("Debugger.enable");
breakpointId = (await send("Debugger.setBreakpointByUrl", { url: pathToFileURL(scriptPath).href, lineNumber: Number(line) })).breakpointId;
await send("Runtime.runIfWaitingForDebugger");
let input = "";
process.stdin.on("data", (chunk) => {
  input += chunk;
  let newline;
  while ((newline = input.indexOf("\n")) >= 0) {
    const command = JSON.parse(input.slice(0, newline));
    input = input.slice(newline + 1);
    if (command.command === "release" && !released) {
      released = true;
      void (async () => {
        await send("Debugger.removeBreakpoint", { breakpointId });
        if (paused) await send("Debugger.resume");
        await send("Debugger.disable");
        report("released");
        socket.close();
      })().catch((error) => { console.error(error); process.exit(1); });
    }
  }
});
