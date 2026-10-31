import copy
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

import runpy
_MODULE = runpy.run_path(str(Path(__file__).with_name("result_adapter.py")))
VerificationError, digest, verify_result, require_provider_success = (_MODULE[k] for k in
    ["VerificationError", "digest", "verify_result", "require_provider_success"])


POPULATION = Path(__file__).with_name("expected-test-population.json").read_bytes()
NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
SENTINEL = "PROTECTED_SENTINEL_MUST_NOT_BE_EMITTED"


def encoded(value):
    return json.dumps(value, separators=(",", ":")).encode()


def fixture():
    binding = {
        "schemaVersion": 2, "purpose": "candidate-bound-historical129",
        "repository": "example-owner/example-repository", "repositoryId": "123", "appId": "456",
        "candidateSha": "a" * 40, "candidateTreeSha": "b" * 40,
        "producerSha256": "1" * 64, "populationSha256": digest(POPULATION),
        "trxSha256": "7" * 64,
        "assertionFixtureInventorySha256": "8" * 64, "testViewInventorySha256": "9" * 64,
        "historicalManifestSha256": "2" * 64, "dependencyGraphSha256": "3" * 64,
        "currentInputInventorySha256": "4" * 64, "sourceInventorySha256": "5" * 64,
        "runtimeDigest": "sha256:" + "6" * 64, "runNonce": "11111111-2222-3333-4444-555555555555",
        "startedAt": "2026-10-26T10:00:00Z", "completedAt": "2026-10-26T10:01:00Z",
    }
    observed = {k: v for k, v in binding.items() if k not in {"schemaVersion", "purpose"}}
    phases = {k: 0 for k in ["restore_exit", "build_exit", "test_exit", "integrity_exit"]}
    integrity = {k: {"before": binding[v], "after": binding[v]} for k, v in {
        "source": "sourceInventorySha256", "currentInputs": "currentInputInventorySha256",
        "historical": "historicalManifestSha256", "dependencies": "dependencyGraphSha256",
        "assertionFixtures": "assertionFixtureInventorySha256", "testView": "testViewInventorySha256"}.items()}
    root = ET.Element(NS + "TestRun")
    definitions = ET.SubElement(root, NS + "TestDefinitions")
    results = ET.SubElement(root, NS + "Results")
    index = 0
    for cls, methods in json.loads(POPULATION)["case_map"].items():
        for method, count in methods.items():
            for case in range(count):
                index += 1
                name, test_id, execution = f"{cls}.{method}({case})", f"t{index}", f"e{index}"
                test = ET.SubElement(definitions, NS + "UnitTest", {"id": test_id, "name": name})
                ET.SubElement(test, NS + "Execution", {"id": execution})
                ET.SubElement(test, NS + "TestMethod", {"className": cls, "name": method})
                ET.SubElement(results, NS + "UnitTestResult", {
                    "testId": test_id, "executionId": execution, "testName": name, "outcome": "Passed"})
    summary = ET.SubElement(root, NS + "ResultSummary", {"outcome": "Completed"})
    counters = {k: "0" for k in ["failed", "error", "timeout", "aborted", "inconclusive",
                "passedButRunAborted", "notRunnable", "notExecuted", "disconnected", "warning",
                "completed", "inProgress", "pending"]}
    counters.update(total="129", executed="129", passed="129")
    ET.SubElement(summary, NS + "Counters", counters)
    return binding, observed, phases, integrity, root


def verify(f, population=POPULATION, trx=None, pin=None, bind_trx=True):
    binding, observed, phases, integrity, root = f
    trx = ET.tostring(root, encoding="utf-8") if trx is None else trx
    if bind_trx:
        # Pin each intentionally malformed XML fixture too, to exercise parsing.
        binding["trxSha256"] = observed["trxSha256"] = digest(trx)
    raw = encoded(binding)
    return verify_result(raw, digest(raw) if pin is None else pin, observed, phases, integrity,
                         population, trx)


class ResultTests(unittest.TestCase):
    def rejected(self, f, **kwargs):
        with self.assertRaises(VerificationError) as result:
            verify(f, **kwargs)
        self.assertNotIn(SENTINEL, str(result.exception))
        self.assertIsNone(result.exception.__cause__)

    def test_complete_population_produces_only_sanitized_prospective_payload(self):
        result = verify(fixture())
        self.assertEqual((result["cases"], result["methods"]), (129, 99))
        self.assertFalse(result["providerWritePerformed"])
        self.assertFalse(result["releaseAuthorized"])
        payload = result["checkRunPayload"]
        self.assertEqual(payload["head_sha"], "a" * 40)
        self.assertEqual(payload["conclusion"], "success")
        self.assertNotIn("Gccs.Api.Tests", json.dumps(result))
        self.assertLess(len(payload["external_id"]), 255)

    def test_wrong_external_digest_and_unrecognized_binding_schema(self):
        self.rejected(fixture(), pin="0" * 64)
        f = fixture(); f[0]["callerSuccess"] = True; self.rejected(f)
        f = fixture(); f[0]["schemaVersion"] = True; self.rejected(f)

    def test_every_observed_identity_must_match_the_pinned_binding(self):
        for field in fixture()[1]:
            if field == "trxSha256":
                continue
            with self.subTest(field=field):
                f = fixture(); f[1][field] = SENTINEL; self.rejected(f)
        f = fixture(); f[1].pop("candidateTreeSha"); self.rejected(f)

    def test_trx_bytes_and_observed_trx_digest_must_match_the_current_run_pin(self):
        f = fixture(); self.rejected(f, bind_trx=False)
        f = fixture(); trx = ET.tostring(f[4], encoding="utf-8")
        f[0]["trxSha256"] = digest(trx); f[1]["trxSha256"] = "0" * 64
        self.rejected(f, bind_trx=False)

    def test_each_phase_failure_missing_phase_and_boolean_exit_reject(self):
        for field in fixture()[2]:
            f = fixture(); f[2][field] = 1; self.rejected(f)
        f = fixture(); f[2].pop("restore_exit"); self.rejected(f)
        f = fixture(); f[2]["test_exit"] = False; self.rejected(f)

    def test_every_integrity_population_must_match_before_and_after(self):
        for field in fixture()[3]:
            for when in ["before", "after"]:
                f = fixture(); f[3][field][when] = "0" * 64; self.rejected(f)
        f = fixture(); f[3].pop("historical"); self.rejected(f)

    def test_population_digest_and_method_distribution_are_exact(self):
        self.rejected(fixture(), population=POPULATION + b" ")
        p = json.loads(POPULATION)
        methods = p["case_map"]["Gccs.Api.Tests.HistoricalEvidencePackageTests"]
        first, second = list(methods)[:2]
        methods[first] += 1; methods[second] += 1
        raw = encoded(p); f = fixture(); f[0]["populationSha256"] = digest(raw)
        f[1]["populationSha256"] = digest(raw); self.rejected(f, population=raw)

    def test_any_nonzero_counter_and_aborted_summary_reject(self):
        for field in ["failed", "error", "timeout", "aborted", "notExecuted", "warning", "pending"]:
            f = fixture(); f[4].find(NS + "ResultSummary/" + NS + "Counters").set(field, "1"); self.rejected(f)
        f = fixture(); f[4].find(NS + "ResultSummary").set("outcome", "Aborted"); self.rejected(f)
        f = fixture(); f[4].find(NS + "ResultSummary/" + NS + "Counters").set("passed", "128"); self.rejected(f)

    def test_failed_skipped_and_unknown_results_cannot_hide_behind_green_counters(self):
        for outcome in ["Failed", "NotExecuted", "Inconclusive", "Timeout", ""]:
            f = fixture(); result = f[4].find(NS + "Results")[0]; result.set("outcome", outcome)
            ET.SubElement(result, NS + "Output").text = SENTINEL; self.rejected(f)

    def test_missing_extra_and_duplicate_cases_reject_even_with_green_counters(self):
        f = fixture(); results = f[4].find(NS + "Results"); results.remove(results[0]); self.rejected(f)
        f = fixture(); results = f[4].find(NS + "Results"); results.append(copy.deepcopy(results[0])); self.rejected(f)
        f = fixture(); results = f[4].find(NS + "Results"); results[1] = copy.deepcopy(results[0]); self.rejected(f)

    def test_unknown_method_or_changed_case_multiplicity_rejects(self):
        f = fixture(); f[4].find(NS + "TestDefinitions")[0].find(NS + "TestMethod").set("name", SENTINEL); self.rejected(f)
        f = fixture(); definitions = f[4].find(NS + "TestDefinitions")
        definitions[1].find(NS + "TestMethod").attrib = dict(definitions[0].find(NS + "TestMethod").attrib)
        self.rejected(f)

    def test_duplicate_definitions_execution_ids_and_case_names_reject(self):
        for key in ["id", "name"]:
            f = fixture(); tests = f[4].find(NS + "TestDefinitions"); tests[1].set(key, tests[0].get(key)); self.rejected(f)
        f = fixture(); tests = f[4].find(NS + "TestDefinitions")
        tests[1].find(NS + "Execution").set("id", tests[0].find(NS + "Execution").get("id")); self.rejected(f)

    def test_result_definition_links_must_match(self):
        for key in ["executionId", "testName", "testId"]:
            f = fixture(); f[4].find(NS + "Results")[0].set(key, SENTINEL); self.rejected(f)

    def test_nested_or_wrong_namespace_results_cannot_hide_failures(self):
        for namespace in [NS, "{other}", ""]:
            f = fixture(); output = ET.SubElement(f[4], NS + "Output")
            ET.SubElement(output, namespace + "UnitTestResult", {"outcome": "Failed"})
            self.rejected(f)

    def test_xml_namespace_structure_encoding_and_declarations_fail_safely(self):
        self.rejected(fixture(), trx=b"<broken>" + SENTINEL.encode())
        self.rejected(fixture(), trx=b"\xff" + SENTINEL.encode())
        self.rejected(fixture(), trx=b'<!DOCTYPE x [<!ENTITY hidden "' + SENTINEL.encode() + b'">]><x/>')
        self.rejected(fixture(), trx=b"<TestRun/>")
        f = fixture(); f[4].append(copy.deepcopy(f[4].find(NS + "Results"))); self.rejected(f)
        self.rejected(fixture(), trx=b"x" * (8 * 1024 * 1024 + 1))

    def test_invalid_times_and_provider_identifiers_reject(self):
        for field, value in [("completedAt", "2026-02-30T10:00:00Z"),
                             ("completedAt", "2026-10-26T09:59:59Z"),
                             ("repository", "../../elsewhere"), ("appId", "0"),
                             ("runtimeDigest", "image:latest"), ("runNonce", "")]:
            f = fixture(); f[0][field] = value; f[1][field] = value; self.rejected(f)

    def test_duplicate_json_keys_and_nonfinite_constants_reject(self):
        read_json = _MODULE['read_json']
        for raw in [b'{"a":1,"a":2}', b'{"a":NaN}', b'{"a":Infinity}']:
            with self.assertRaises(VerificationError):
                read_json(raw, 65536)

    def test_authenticated_check_metadata_requires_exact_app_candidate_and_invocation(self):
        payload = verify(fixture())["checkRunPayload"]
        check = dict(payload, id=7, app={"id": 456})
        self.assertEqual(require_provider_success(check, payload, "456")["result"], "matched-success")
        for field, value in [("head_sha", "0" * 40), ("name", "other"),
                             ("external_id", "fedril-historical129:" + "0" * 64),
                             ("app", {"id": 123}), ("id", True), ("status", "in_progress")]:
            bad = dict(check); bad[field] = value
            with self.assertRaises(VerificationError):
                require_provider_success(bad, payload, "456")

    def test_skipped_neutral_and_all_other_non_success_conclusions_are_rejected(self):
        payload = verify(fixture())["checkRunPayload"]
        for conclusion in [None, "skipped", "neutral", "failure", "cancelled", "timed_out", "stale", "action_required"]:
            check = dict(payload, id=7, app={"id": 456}, conclusion=conclusion)
            with self.assertRaises(VerificationError):
                require_provider_success(check, payload, "456")


if __name__ == "__main__":
    unittest.main()
