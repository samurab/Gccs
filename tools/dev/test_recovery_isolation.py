import json
import concurrent.futures
import os
from pathlib import Path
import socket
import signal
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

from recovery_isolation import (RecoveryCancellation, RecoveryCancelled, RecoveryEnvironment, SHARED_PORTS, allocate_app_ports,
                                clean_env, lease_port, run_owned_command, stop_owned_group,
                                validate_app_ports)


class RecoveryIsolationTests(unittest.TestCase):
    def test_cancellation_interrupts_workers_and_repeated_signals_allow_cleanup(self):
        cancellation = RecoveryCancellation()
        def wait_for_owner():
            while True:
                cancellation.check()
                time.sleep(.01)
        with concurrent.futures.ThreadPoolExecutor(max_workers=1) as pool:
            worker = pool.submit(wait_for_owner)
            with self.assertRaises(RecoveryCancelled):
                cancellation.request(signal.SIGINT)
            with self.assertRaises(RecoveryCancelled):
                worker.result(timeout=1)
            cancellation.cleaning_up = True
            cancellation.request(signal.SIGINT)
            cancellation.request(signal.SIGTERM)
        self.assertEqual(cancellation.signal_number, signal.SIGINT)

    def test_first_cancellation_during_cleanup_does_not_interrupt_cleanup(self):
        cancellation = RecoveryCancellation()
        cancellation.cleaning_up = True
        cancellation.request(signal.SIGTERM)
        cancellation.request(signal.SIGINT)
        self.assertEqual(cancellation.signal_number, signal.SIGTERM)

    def test_dependency_commands_are_outside_the_npm_signal_group(self):
        environment = self.environment()
        with patch('recovery_isolation.subprocess.run') as run:
            environment._run(['docker', 'inspect', 'test-owned-container'])
        self.assertTrue(run.call_args.kwargs['start_new_session'])

    def environment(self):
        # Retain test artifacts; the repository's file-preservation rule forbids automatic removal.
        folder = Path(tempfile.mkdtemp(prefix="fedril-isolation-unit-"))
        with patch("recovery_isolation.Path.home", return_value=folder):
            return RecoveryEnvironment(Path(__file__).resolve().parents[2])

    def test_caller_credentials_configuration_and_instrumentation_are_not_inherited(self):
        inherited = {
            "PATH": "/usr/bin", "HOME": "/Users/operator", "DOTNET_ROOT": "/opt/dotnet",
            "ConnectionStrings__GccsDatabase": "shared database",
            "ASPNETCORE_URLS": "http://localhost:5062", "DOTNET_ENVIRONMENT": "Production",
            "VITE_API_BASE_URL": "https://production.example", "NODE_OPTIONS": "--require hooks.js",
            "APPLICATIONINSIGHTS_CONNECTION_STRING": "shared telemetry",
            "AZURE_CLIENT_SECRET": "secret", "AWS_SECRET_ACCESS_KEY": "secret",
            "CORECLR_PROFILER_PATH": "/shared/profiler", "OTEL_EXPORTER_OTLP_ENDPOINT": "https://external",
            "PYTHONPATH": "/shared/hooks", "DOTNET_CLI_TELEMETRY_OPTOUT": "0",
        }
        with patch.dict(os.environ, inherited, clear=True):
            environment = clean_env()
        self.assertEqual(environment, {
            "PATH": "/usr/bin", "HOME": "/Users/operator", "DOTNET_ROOT": "/opt/dotnet",
            "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1",
        })

    def test_shared_and_colliding_lease_ports_are_rejected(self):
        for shared in SHARED_PORTS:
            for pair in ((shared, 30000), (30000, shared),
                         (((shared - 1 - 32768) % 65535) + 1, 30000)):
                with self.subTest(pair=pair), self.assertRaises(ValueError):
                    validate_app_ports(*pair)
        for pair in ((30000, 30000), (30000, lease_port(30000)), (0, 30000), (30000, 65536)):
            with self.subTest(pair=pair), self.assertRaises(ValueError):
                validate_app_ports(*pair)

    def test_allocated_app_and_lease_ports_are_available_and_distinct(self):
        api, web = allocate_app_ports()
        ports = validate_app_ports(api, web)
        held = []
        try:
            for port in ports:
                listener = socket.socket()
                held.append(listener)
                listener.bind(("127.0.0.1", port))
        finally:
            for listener in held:
                listener.close()
        self.assertEqual(len(set(ports)), 4)

    def test_generated_api_config_uses_only_test_dependencies_and_disables_side_effects(self):
        environment = self.environment()
        environment.api_cwd = environment.folder
        environment._configure_api(24001, 24002, 24003, "synthetic-password", "synthetic-key")
        config = json.loads((environment.folder / "appsettings.json").read_text())
        self.assertIn("Port=24001;Database=recovery;Username=recovery", config["ConnectionStrings"]["GccsDatabase"])
        self.assertIn("http://127.0.0.1:24003/recovery", config["ConnectionStrings"]["AzureStorage"])
        self.assertEqual(config["LocalDependencies"]["Redis"]["ConnectionString"], "127.0.0.1:24002")
        for section in ("DemoRequests", "InvitationDelivery", "ExtractionProcessing", "ReportExportProcessing",
                        "ObjectCleanupProcessing", "AiOutputRetentionProcessing", "DueDateReminderProcessing",
                        "PortalPackageLifecycleProcessing", "MarketingDemo", "MalwareScanning", "LocalDependencies"):
            self.assertIs(config[section]["Enabled"], False, section)
        self.assertIs(config["Operations"]["D01RuntimeDiagnostic"]["Enabled"], False)
        self.assertIs(config["DemoRequests"]["HubSpot"]["Enabled"], False)
        self.assertIs(config["LocalDevelopment"]["SeedData"]["Enabled"], False)
        self.assertIs(config["LocalDependencies"]["SeedData"]["Enabled"], False)
        self.assertNotIn("APPLICATIONINSIGHTS_CONNECTION_STRING", config)
        self.assertEqual(json.loads((environment.folder / "appsettings.Development.json").read_text()), {})
        self.assertEqual(environment.api_env["ASPNETCORE_URLS"], environment.api_url)
        self.assertEqual(environment.web_env["VITE_API_BASE_URL"], environment.api_url)

    def test_failed_setup_stops_only_its_created_container_and_retains_files(self):
        environment = self.environment()
        identifier = "a" * 64
        commands = []

        def command(args, **kwargs):
            commands.append(args)
            if args[:2] == ["docker", "inspect"]:
                return json.dumps([{"Config": {"Labels": {"fedril.recovery.run": environment.run_id}}}])
            return ""

        def create(service, port, extra):
            if service == "postgres":
                environment.containers[service] = identifier
                return 24001
            raise RuntimeError("Injected Redis setup failure")

        evidence = environment.folder / "preserved.txt"
        evidence.write_text("preserved")
        with patch.object(environment, "_run", side_effect=command), \
                patch.object(environment, "_build_api"), \
                patch.object(environment, "_create_container", side_effect=create):
            with self.assertRaisesRegex(RuntimeError, "Injected Redis setup failure"):
                environment.start()
        self.assertEqual([args for args in commands if args[:2] == ["docker", "stop"]],
                         [["docker", "stop", "--time", "10", identifier]])
        self.assertEqual(evidence.read_text(), "preserved")
        self.assertFalse(any("rm" in args or "prune" in args for args in commands))

    def test_container_with_wrong_ownership_label_is_never_stopped(self):
        environment = self.environment()
        environment.containers["postgres"] = "b" * 64
        with patch.object(environment, "_run", return_value=json.dumps([
                {"Config": {"Labels": {"fedril.recovery.run": "another-run"}}}])) as run:
            with self.assertRaisesRegex(RuntimeError, "ownership label"):
                environment.close()
        self.assertEqual(run.call_count, 1)
        self.assertEqual(run.call_args.args[0][:2], ["docker", "inspect"])

    def process_fixture(self, descendant_code, leader_exits):
        folder = Path(tempfile.mkdtemp(prefix="fedril-isolation-process-unit-"))
        log_path = folder / "process.log"
        log = log_path.open("x")
        script = ("import subprocess,sys,time; subprocess.Popen([sys.executable,'-c'," +
                  repr(descendant_code) + "]);" + ("" if leader_exits else "time.sleep(60)"))
        child = subprocess.Popen([sys.executable, "-c", script], stdout=log,
                                 stderr=subprocess.STDOUT, start_new_session=True, env=clean_env())
        self.addCleanup(log.close)
        self.addCleanup(lambda: stop_owned_group(child, grace_seconds=.1))
        deadline = time.monotonic() + 5
        while not log_path.read_text().strip():
            self.assertLess(time.monotonic(), deadline, "Descendant did not report readiness")
            time.sleep(.02)
        descendant = int(log_path.read_text().strip())
        return child, descendant

    def assert_process_stopped(self, pid):
        state = subprocess.run(["ps", "-p", str(pid), "-o", "stat="],
                               text=True, capture_output=True, check=False).stdout.strip()
        self.assertTrue(not state or state.startswith("Z"), (pid, state))

    @unittest.skipUnless(os.name == "posix", "POSIX process-group cleanup")
    def test_cleanup_stops_non_listening_descendant_after_leader_has_exited(self):
        child, descendant = self.process_fixture(
            "import os,time;print(os.getpid(),flush=True);time.sleep(60)", leader_exits=True)
        self.assertEqual(child.wait(timeout=5), 0)
        with patch('recovery_isolation.subprocess.run', wraps=subprocess.run) as run:
            stop_owned_group(child, grace_seconds=.1)
        self.assertTrue(run.call_args_list)
        self.assertTrue(all(call.kwargs['start_new_session'] for call in run.call_args_list))
        self.assert_process_stopped(descendant)

    @unittest.skipUnless(os.name == "posix", "POSIX process-group cleanup")
    def test_cleanup_escalates_for_a_descendant_ignoring_sigterm(self):
        child, descendant = self.process_fixture(
            "import os,signal,time;signal.signal(signal.SIGTERM,signal.SIG_IGN);"
            "print(os.getpid(),flush=True);time.sleep(60)", leader_exits=True)
        self.assertEqual(child.wait(timeout=5), 0)
        started = time.monotonic()
        stop_owned_group(child, grace_seconds=.1)
        self.assertLess(time.monotonic() - started, 6)
        self.assert_process_stopped(descendant)

    @unittest.skipUnless(os.name == "posix", "POSIX process-group cleanup")
    def test_owned_command_timeout_stops_leader_and_descendant(self):
        folder = Path(tempfile.mkdtemp(prefix="fedril-isolation-timeout-unit-"))
        log_path = folder / "process.log"
        code = "import os,time;print(os.getpid(),flush=True);time.sleep(60)"
        script = ("import subprocess,sys,time;subprocess.Popen([sys.executable,'-c'," +
                  repr(code) + "]);time.sleep(60)")
        with log_path.open("x") as log:
            with self.assertRaises(subprocess.TimeoutExpired):
                run_owned_command([sys.executable, "-c", script], cwd=folder,
                                  env=clean_env(), stdout=log, timeout=.5)
        descendant = int(log_path.read_text().strip())
        self.assert_process_stopped(descendant)


if __name__ == "__main__":
    unittest.main()
