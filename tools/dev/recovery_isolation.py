"""Test-owned dependencies and configuration for the real recovery stress runner.

Containers, volumes and run artifacts are retained after their processes stop.
This is resource separation, not an operating-system security sandbox.
"""

import base64
import datetime
import json
import os
from pathlib import Path
import signal
import socket
import subprocess
import time
import uuid


SHARED_PORTS = frozenset((5062, 5173, 5174, 37830, 37941))
IMAGES = {
    "postgres": "postgres:17",
    "redis": "redis:8",
    "azurite": "mcr.microsoft.com/azure-storage/azurite:3.35.0",
}


def clean_env():
    """Do not inherit application configuration, cloud credentials or profiler hooks."""
    allowed = ("PATH", "HOME", "USER", "LOGNAME", "TMPDIR", "LANG", "LC_ALL",
               "DOTNET_ROOT", "DOTNET_ROOT_ARM64", "SystemRoot")
    return {**{key: os.environ[key] for key in allowed if key in os.environ},
            "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1"}


def stop_owned_group(child, grace_seconds=5):
    """Stop only a Popen created by this runner with start_new_session=True."""
    if os.name != "posix":
        raise RuntimeError("Recovery process-group cleanup requires POSIX")
    if grace_seconds < 0:
        raise ValueError("Process-group grace period must not be negative")
    try:
        if os.getpgid(child.pid) != child.pid:
            raise RuntimeError("Refusing to signal a process outside its owned session")
    except ProcessLookupError:
        pass  # A dead leader can leave live, non-listening descendants in its group.

    def alive():
        child.poll()
        listing = subprocess.run(["ps", "-axo", "pgid=,stat="], check=True,
                                 text=True, capture_output=True, timeout=5, env=clean_env()).stdout
        return any(int(parts[0]) == child.pid and not parts[1].startswith("Z")
                   for line in listing.splitlines() if len(parts := line.split()) == 2)

    def send(requested_signal):
        try:
            os.killpg(child.pid, requested_signal)
        except ProcessLookupError:
            pass

    def wait(seconds):
        deadline = time.monotonic() + seconds
        while alive():
            if time.monotonic() >= deadline:
                return False
            time.sleep(.05)
        return True

    if not alive():
        child.wait(timeout=5)
        return
    send(signal.SIGTERM)
    send(signal.SIGCONT)
    if wait(grace_seconds):
        child.wait(timeout=5)
        return
    send(signal.SIGKILL)
    if not wait(5):
        raise RuntimeError("Owned recovery process group did not exit after SIGKILL")
    child.wait(timeout=5)


def run_owned_command(command, *, cwd, env, stdout, timeout):
    child = subprocess.Popen(command, cwd=cwd, env=env, stdout=stdout,
                             stderr=subprocess.STDOUT, start_new_session=True)
    try:
        code = child.wait(timeout=timeout)
        if code:
            raise subprocess.CalledProcessError(code, command)
    finally:
        stop_owned_group(child)


def lease_port(port):
    return (port - 1 + 32768) % 65535 + 1


def validate_app_ports(api_port, web_port):
    ports = (api_port, web_port, lease_port(api_port), lease_port(web_port))
    if any(not 1 <= port <= 65535 for port in (api_port, web_port)):
        raise ValueError("Recovery app ports must be between 1 and 65535")
    if len(set(ports)) != 4 or SHARED_PORTS.intersection(ports):
        raise ValueError("Recovery app and lease ports must be distinct and exclude shared ports")
    return ports


def allocate_app_ports():
    # Reservations prevent collisions during selection; normal exclusive binds fail closed afterward.
    for _ in range(100):
        held = []
        try:
            for _ in range(2):
                listener = socket.socket()
                held.append(listener)
                listener.bind(("127.0.0.1", 0))
            ports = validate_app_ports(*(listener.getsockname()[1] for listener in held))
            for port in ports[2:]:
                listener = socket.socket()
                held.append(listener)
                listener.bind(("127.0.0.1", port))
            return ports[:2]
        except (OSError, ValueError):
            continue
        finally:
            for listener in held:
                listener.close()
    raise RuntimeError("Could not allocate isolated recovery app and lease ports")


class RecoveryEnvironment:
    def __init__(self, repo):
        self.repo = Path(repo).resolve()
        self.run_id = uuid.uuid4().hex
        stamp = datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%dT%H%M%SZ")
        self.folder = (Path.home() / "Library/Logs/FeDrilLocalStack/isolated-recovery" /
                       (stamp + "-" + self.run_id[:12]))
        self.folder.mkdir(parents=True, exist_ok=False)
        self.api_port, self.web_port = allocate_app_ports()
        self.api_lease_port = lease_port(self.api_port)
        self.web_lease_port = lease_port(self.web_port)
        self.api_url = "http://127.0.0.1:" + str(self.api_port)
        self.web_url = "http://127.0.0.1:" + str(self.web_port)
        self.containers = {}
        self._stopped = set()
        self.api_env = clean_env()
        self.web_env = {**clean_env(), "VITE_API_BASE_URL": self.api_url}
        self.api_command = []
        self.api_cwd = None

    def _run(self, command, **kwargs):
        return subprocess.run(command, check=True, text=True, capture_output=True,
                              env=clean_env(), timeout=120, **kwargs).stdout

    def _create_container(self, service, container_port, extra):
        name = "fedril-recovery-" + self.run_id[:12] + "-" + service
        identifier = self._run([
            "docker", "create", "--pull", "never", "--name", name,
            "--label", "fedril.recovery.run=" + self.run_id,
            "--restart", "no", "--publish", "127.0.0.1::" + str(container_port),
            *extra, IMAGES[service],
            *(["azurite-blob", "--blobHost", "0.0.0.0", "--location", "/data"]
              if service == "azurite" else []),
        ]).strip()
        if len(identifier) != 64 or any(c not in "0123456789abcdef" for c in identifier):
            raise RuntimeError("Docker did not return a container ID")
        self.containers[service] = identifier
        self._run(["docker", "start", identifier])
        info = json.loads(self._run(["docker", "inspect", identifier]))[0]
        bindings = info["NetworkSettings"]["Ports"][str(container_port) + "/tcp"]
        if len(bindings) != 1 or bindings[0]["HostIp"] != "127.0.0.1":
            raise RuntimeError("Recovery dependencies must bind only to IPv4 loopback")
        port = int(bindings[0]["HostPort"])
        if port in SHARED_PORTS or port in validate_app_ports(self.api_port, self.web_port):
            raise RuntimeError("Recovery dependency port conflicts with an app or shared port")
        return port

    def _build_api(self):
        guard = (Path.home() / "Library/Application Support/FeDrilProjectRecovery/"
                 "external-project-snapshots/fedril_deployment_guard.py")
        command = ["/opt/homebrew/bin/python3", "-I", "-B", str(guard), "--",
                   "dotnet", "build", "apps/api/Gccs.Api.csproj", "--artifacts-path",
                   str(self.folder / "build"), "-p:UserSecretsId=", "--disable-build-servers"]
        with (self.folder / "build.log").open("x") as log:
            run_owned_command(command, cwd=self.repo, env=clean_env(), stdout=log, timeout=300)
        matches = list((self.folder / "build/bin").rglob("Gccs.Api.runtimeconfig.json"))
        if len(matches) != 1:
            raise RuntimeError("Expected exactly one isolated API build")
        self.api_cwd = matches[0].parent
        executable = self.api_cwd / ("Gccs.Api.exe" if os.name == "nt" else "Gccs.Api")
        if not executable.is_file():
            raise RuntimeError("Isolated API executable was not built")
        self.api_command = [str(executable), "--contentRoot", str(self.api_cwd),
                            "--urls", self.api_url]

    def _configure_api(self, postgres_port, redis_port, blob_port, password, storage_key):
        flags = ("DemoRequests", "InvitationDelivery", "ExtractionProcessing",
                 "ReportExportProcessing", "ObjectCleanupProcessing", "AiOutputRetentionProcessing",
                 "DueDateReminderProcessing", "PortalPackageLifecycleProcessing", "MarketingDemo",
                 "MalwareScanning")
        config = {section: {"Enabled": False} for section in flags}
        config.update({
            "ConnectionStrings": {
                "GccsDatabase": "Host=127.0.0.1;Port=" + str(postgres_port) +
                    ";Database=recovery;Username=recovery;Password=" + password,
                "AzureStorage": "DefaultEndpointsProtocol=http;AccountName=recovery;AccountKey=" +
                    storage_key + ";BlobEndpoint=http://127.0.0.1:" + str(blob_port) + "/recovery;",
            },
            "LocalDependencies": {"Enabled": False, "SeedData": {"Enabled": False},
                                  "Redis": {"ConnectionString": "127.0.0.1:" + str(redis_port)}},
            "LocalDevelopment": {"SeedData": {"Enabled": False}},
            "Storage": {"UseManagedIdentity": False},
            "TaskSearch": {"CursorSigningKey": base64.b64encode(os.urandom(32)).decode()},
            "Cors": {"AllowedOrigins": [self.web_url]},
            "Security": {"DevelopmentAuth": {"Enabled": True},
                         "DevelopmentTesting": {"Enabled": False}},
            "Operations": {"D01RuntimeDiagnostic": {"Enabled": False}},
            "Logging": {"LogLevel": {"Default": "Warning", "Microsoft.Hosting.Lifetime": "Information"}},
        })
        config["DemoRequests"]["HubSpot"] = {"Enabled": False}
        # Both files were just generated inside this run's build, never in the shared checkout.
        (self.api_cwd / "appsettings.json").write_text(json.dumps(config, indent=2) + "\n")
        (self.api_cwd / "appsettings.Development.json").write_text("{}\n")
        self.api_env.update({"ASPNETCORE_ENVIRONMENT": "Development", "DOTNET_ENVIRONMENT": "Development",
                             "ASPNETCORE_URLS": self.api_url, "DOTNET_CLI_TELEMETRY_OPTOUT": "1"})

    def start(self):
        try:
            for image in IMAGES.values():
                self._run(["docker", "image", "inspect", image])
            self._build_api()
            password = uuid.uuid4().hex
            storage_key = base64.b64encode(os.urandom(32)).decode()
            prefix = "fedril-recovery-" + self.run_id[:12]
            postgres_port = self._create_container("postgres", 5432, [
                "--env", "POSTGRES_DB=recovery", "--env", "POSTGRES_USER=recovery",
                "--env", "POSTGRES_PASSWORD=" + password, "--mount",
                "type=volume,source=" + prefix + "-postgres-data,target=/var/lib/postgresql/data"])
            redis_port = self._create_container("redis", 6379, ["--mount",
                "type=volume,source=" + prefix + "-redis-data,target=/data"])
            blob_port = self._create_container("azurite", 10000, [
                "--env", "AZURITE_ACCOUNTS=recovery:" + storage_key, "--mount",
                "type=volume,source=" + prefix + "-blob-data,target=/data"])
            self._configure_api(postgres_port, redis_port, blob_port, password, storage_key)
            deadline = time.monotonic() + 30
            while True:
                try:
                    self.db_query("SELECT 1;")
                    for port in (redis_port, blob_port):
                        with socket.create_connection(("127.0.0.1", port), timeout=1):
                            pass
                    break
                except (subprocess.CalledProcessError, OSError):
                    if time.monotonic() >= deadline:
                        raise RuntimeError("Isolated dependencies did not become ready")
                    time.sleep(.2)
            (self.folder / "resources.json").write_text(json.dumps({
                "runId": self.run_id, "apiPort": self.api_port, "webPort": self.web_port,
                "dependencyPorts": {"postgres": postgres_port, "redis": redis_port, "blob": blob_port},
                "containers": self.docker_inventory(), "retention": "Stopped resources and data retained",
            }, indent=2) + "\n")
            return self
        except BaseException:
            self.close()
            raise

    def db_query(self, sql):
        return self._run(["docker", "exec", "-i", self.containers["postgres"], "psql",
                          "-U", "recovery", "-d", "recovery", "-qAt", "-v", "ON_ERROR_STOP=1"], input=sql)

    def row_counts(self):
        output = self.db_query(r"""BEGIN READ ONLY;
SELECT format('SELECT %L AS table_name, count(*) FROM %I.%I', schemaname || '.' || tablename, schemaname, tablename)
FROM pg_tables WHERE schemaname IN ('gccs', 'recovery_fixture') ORDER BY schemaname, tablename;
\gexec
COMMIT;
""")
        return {line.split("|")[0]: int(line.split("|")[1]) for line in output.splitlines() if "|" in line}

    def docker_inventory(self):
        if not self.containers:
            return {}
        raw = json.loads(self._run(["docker", "inspect", *self.containers.values()]))
        return {item["Name"].lstrip("/"): {"id": item["Id"], "running": item["State"]["Running"],
                "mounts": [{"type": mount["Type"], "name": mount.get("Name"),
                            "source": mount["Source"], "destination": mount["Destination"]}
                           for mount in item["Mounts"]]} for item in raw}

    def close(self):
        errors = []
        for identifier in reversed(list(self.containers.values())):
            if identifier in self._stopped:
                continue
            try:
                info = json.loads(self._run(["docker", "inspect", identifier]))[0]
                if info["Config"]["Labels"].get("fedril.recovery.run") != self.run_id:
                    raise RuntimeError("Refusing to stop a container without this run's ownership label")
                self._run(["docker", "stop", "--time", "10", identifier])
                self._stopped.add(identifier)
            except Exception as error:
                errors.append(str(error))
        if errors:
            raise RuntimeError("Could not stop test-owned containers: " + "; ".join(errors))
