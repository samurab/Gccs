"""Held offline result adapter. The caller must supply authenticated runner observations.

No network, filesystem, test execution, identity authentication or provider write.
Matching supplied metadata does not establish native candidate/source equivalence.
"""

from collections import Counter
from datetime import datetime
import hashlib
import json
import re
import xml.etree.ElementTree as ET


class VerificationError(ValueError):
    """Only fixed reason codes; never include protected input values."""


def require(condition, code):
    if not condition:
        raise VerificationError(code)


def digest(value):
    return hashlib.sha256(value).hexdigest()


def read_json(value, cap):
    require(type(value) is bytes and 0 < len(value) <= cap, "json_size")
    def pairs(items):
        result = {}
        for key, item in items:
            require(key not in result, "json_duplicate_key")
            result[key] = item
        return result
    try:
        return json.loads(value.decode("utf-8"), object_pairs_hook=pairs,
                          parse_constant=lambda _: require(False, "json_constant"))
    except (UnicodeError, json.JSONDecodeError, RecursionError):
        raise VerificationError("json_encoding") from None


_CLASSES = {
    "Gccs.Api.Tests.HistoricalEvidencePackageTests": 41,
    "Gccs.Api.Tests.ProductionReadinessChecklistTests": 84,
    "Gccs.Api.Tests.ProductionReadinessHighRiskObligationContentTests": 4,
}
_PHASES = {"restore_exit", "build_exit", "test_exit", "integrity_exit"}
_DIGESTS = {"producerSha256", "populationSha256", "trxSha256", "historicalManifestSha256",
            "dependencyGraphSha256", "currentInputInventorySha256", "sourceInventorySha256",
            "assertionFixtureInventorySha256", "testViewInventorySha256"}
_FIELDS = {"schemaVersion", "purpose", "repository", "repositoryId", "appId",
           "candidateSha", "candidateTreeSha", "runtimeDigest", "runNonce",
           "startedAt", "completedAt"} | _DIGESTS
_COUNTERS = {"total", "executed", "passed", "failed", "error", "timeout", "aborted",
             "inconclusive", "passedButRunAborted", "notRunnable", "notExecuted",
             "disconnected", "warning", "completed", "inProgress", "pending"}
_NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


def verify_result(binding_bytes, binding_sha256, observed, phases, integrity,
                  population_bytes, trx_bytes):
    """Build a prospective success payload only after complete bound result checks.

    binding_sha256 must be pinned outside candidate-controlled inputs. observed,
    phases and integrity must come directly from the approved protected producer,
    not a public artifact, dispatch argument or candidate-supplied declaration.
    """
    require(type(binding_sha256) is str and re.fullmatch(r"[a-f0-9]{64}", binding_sha256), "binding_pin")
    require(type(binding_bytes) is bytes and len(binding_bytes) <= 65536, "binding_size")
    require(digest(binding_bytes) == binding_sha256, "binding_digest")
    binding = read_json(binding_bytes, 65536)
    require(type(binding) is dict and set(binding) == _FIELDS, "binding_schema")
    require(type(binding["schemaVersion"]) is int and binding["schemaVersion"] == 2 and
            binding["purpose"] == "candidate-bound-historical129", "binding_purpose")
    for field in _FIELDS - {"schemaVersion"}:
        require(type(binding[field]) is str, "binding_type")
    for field in _DIGESTS:
        require(re.fullmatch(r"[a-f0-9]{64}", binding[field]), "binding_hash")
    for field in ["candidateSha", "candidateTreeSha"]:
        require(re.fullmatch(r"[a-f0-9]{40}", binding[field]), "binding_commit")
    require(re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+", binding["repository"]), "binding_repository")
    for field in ["repositoryId", "appId"]:
        require(re.fullmatch(r"[1-9][0-9]{0,19}", binding[field]), "binding_provider")
    require(re.fullmatch(r"sha256:[a-f0-9]{64}", binding["runtimeDigest"]), "binding_runtime")
    require(re.fullmatch(r"[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12}", binding["runNonce"]), "binding_run")
    times = []
    for field in ["startedAt", "completedAt"]:
        require(re.fullmatch(r"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z", binding[field]), "binding_time")
        try:
            times.append(datetime.strptime(binding[field], "%Y-%m-%dT%H:%M:%SZ"))
        except ValueError:
            raise VerificationError("binding_time") from None
    require(times[0] < times[1], "binding_time_order")
    require(type(observed) is dict and set(observed) == _FIELDS - {"schemaVersion", "purpose"}, "observed_schema")
    require(all(type(observed[k]) is str and observed[k] == binding[k] for k in observed), "observed_binding")
    require(type(phases) is dict and set(phases) == _PHASES and
            all(type(v) is int and v == 0 for v in phases.values()), "phase_failure")
    inventory_fields = {"source": "sourceInventorySha256", "currentInputs": "currentInputInventorySha256",
                        "historical": "historicalManifestSha256", "dependencies": "dependencyGraphSha256",
                        "assertionFixtures": "assertionFixtureInventorySha256", "testView": "testViewInventorySha256"}
    require(type(integrity) is dict and set(integrity) == set(inventory_fields), "integrity_schema")
    for key, field in inventory_fields.items():
        pair = integrity[key]
        require(type(pair) is dict and set(pair) == {"before", "after"} and
                all(type(pair[when]) is str and pair[when] == binding[field] for when in pair), "integrity_binding")
    require(type(population_bytes) is bytes and 0 < len(population_bytes) <= 131072 and
            digest(population_bytes) == binding["populationSha256"], "population_digest")
    population = read_json(population_bytes, 131072)
    require(type(population) is dict and type(population.get("expected_cases")) is int and
            population["expected_cases"] == 129 and type(population.get("expected_methods")) is int and
            population["expected_methods"] == 99, "population_counts")
    case_map = population.get("case_map")
    require(type(case_map) is dict and set(case_map) == set(_CLASSES), "population_classes")
    expected = Counter()
    for cls, methods in case_map.items():
        require(type(methods) is dict and methods, "population_methods")
        for method, count in methods.items():
            require(re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", method) and type(count) is int and count > 0, "population_method")
            expected[(cls, method)] = count
        require(sum(methods.values()) == _CLASSES[cls], "population_class_count")
    require(len(expected) == 99 and sum(expected.values()) == 129, "population_total")
    require(type(trx_bytes) is bytes and 0 < len(trx_bytes) <= 8 * 1024 * 1024, "trx_size")
    require(digest(trx_bytes) == binding["trxSha256"], "trx_digest")
    # Reject non-UTF8 and DTD/entity declarations before the XML parser sees them.
    try:
        xml = trx_bytes.decode("utf-8")
    except UnicodeError:
        raise VerificationError("trx_encoding") from None
    require("<!DOCTYPE" not in xml.upper() and "<!ENTITY" not in xml.upper(), "trx_declaration")
    try:
        root = ET.fromstring(xml)
    except (ET.ParseError, ValueError, RecursionError):
        raise VerificationError("trx_xml") from None
    require(root.tag == _NS + "TestRun" and sum(1 for _ in root.iter()) <= 10000, "trx_root")
    for name, count in {"UnitTest": 129, "UnitTestResult": 129, "ResultSummary": 1, "Counters": 1}.items():
        nodes = [node for node in root.iter() if node.tag.rsplit("}", 1)[-1] == name]
        require(len(nodes) == count and all(node.tag == _NS + name for node in nodes), "trx_population_scope")
    def one(parent, name):
        items = parent.findall(_NS + name)
        require(len(items) == 1, "trx_structure")
        return items[0]
    summary = one(root, "ResultSummary")
    require(summary.get("outcome") in {"Completed", "Passed"}, "trx_outcome")
    counters = one(summary, "Counters").attrib
    require(set(counters) == _COUNTERS, "trx_counters")
    require(all(counters[k] == ("129" if k in {"total", "executed", "passed"} else "0")
                for k in counters), "trx_counts")
    definitions = one(root, "TestDefinitions")
    results = one(root, "Results")
    require(len(definitions) == len(results) == 129, "trx_case_count")
    tests, execution_ids, names = {}, set(), set()
    for test in definitions:
        require(test.tag == _NS + "UnitTest", "trx_definition")
        method = one(test, "TestMethod"); execution = one(test, "Execution").get("id")
        test_id = test.get("id"); name = test.get("name")
        require(test_id and test_id not in tests and execution and execution not in execution_ids and
                name and name not in names, "trx_duplicate")
        pair = (method.get("className"), method.get("name"))
        require(pair in expected, "trx_unknown_method")
        tests[test_id] = (execution, name, pair); execution_ids.add(execution); names.add(name)
    actual, used = Counter(), set()
    for result in results:
        require(result.tag == _NS + "UnitTestResult" and result.get("outcome") == "Passed", "trx_failed_case")
        test_id = result.get("testId")
        require(test_id in tests and test_id not in used, "trx_result_identity")
        execution, name, pair = tests[test_id]
        require(result.get("executionId") == execution and result.get("testName") == name, "trx_result_link")
        actual[pair] += 1; used.add(test_id)
    require(actual == expected and len(used) == 129, "trx_population")
    return {
        "purpose": "prospective-protected-historical-check", "bindingSha256": binding_sha256,
        "trxSha256": digest(trx_bytes), "cases": 129, "methods": 99,
        "providerWritePerformed": False, "releaseAuthorized": False,
        "checkRunPayload": {
            "name": "FeDril protected historical assertions (129)", "head_sha": binding["candidateSha"],
            "external_id": "fedril-historical129:" + binding_sha256,
            "status": "completed", "conclusion": "success",
            "started_at": binding["startedAt"], "completed_at": binding["completedAt"],
            "output": {"title": "Protected historical assertions passed",
                       "summary": "129 historical assertions passed; 0 failed; 0 skipped. Frozen source fixtures verified. "
                                  "Current controls require separate evidence. "
                                  "Separate protected diagnostic custody is required. This check does not authorize release."},
        },
    }


def require_provider_success(check, expected_payload, expected_app_id):
    """Compare an authenticated exact-repository API response; no provider lookup here."""
    require(type(expected_app_id) is str and re.fullmatch(r"[1-9][0-9]{0,19}", expected_app_id), "provider_app_pin")
    require(type(expected_payload) is dict and
            expected_payload.get("name") == "FeDril protected historical assertions (129)" and
            type(expected_payload.get("head_sha")) is str and
            re.fullmatch(r"[a-f0-9]{40}", expected_payload["head_sha"]) and
            type(expected_payload.get("external_id")) is str and
            re.fullmatch(r"fedril-historical129:[a-f0-9]{64}", expected_payload["external_id"]), "provider_expected")
    require(type(check) is dict and type(check.get("id")) is int and check["id"] > 0, "provider_check")
    app = check.get("app")
    require(type(app) is dict and type(app.get("id")) is int and
            str(app["id"]) == expected_app_id, "provider_app")
    require(all(check.get(k) == expected_payload[k] for k in ["name", "head_sha", "external_id"]), "provider_binding")
    require(check.get("status") == "completed" and check.get("conclusion") == "success", "provider_not_success")
    return {"result": "matched-success", "checkRunId": check["id"], "releaseAuthorized": False}
