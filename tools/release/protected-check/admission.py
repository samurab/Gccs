"""Local trusted producer admission. No CLI, key loading or token creation."""
import hashlib
import base64
from pathlib import Path
import re
import types

CLIENT_SHA256 = '4e141dafa886c474c46e3dfb3cfc8d3371dcf033fce5faaa6f2c537787883d38'
client_path = Path(__file__).with_name('publisher_client.py')
source = client_path.read_bytes()
if hashlib.sha256(source).hexdigest() != CLIENT_SHA256:
    raise ValueError('admission_client_identity_changed')
CLIENT = types.ModuleType('fedril_admission_client')
CLIENT.__dict__['__file__'] = str(client_path)
exec(compile(source, str(client_path), 'exec'), CLIENT.__dict__)

PRODUCER_FIELDS = {'repository', 'repositoryId', 'appId', 'candidateSha', 'candidateTreeSha', 'runNonce',
                   'runtimeDigest', 'producerSha256', 'populationSha256', 'historicalManifestSha256',
                   'dependencyGraphSha256', 'currentInputInventorySha256', 'sourceInventorySha256',
                   'assertionFixtureInventorySha256', 'testViewInventorySha256'}
CONSUMER_FIELDS = {'repository', 'repositoryId', 'appId', 'workflowSha', 'candidateSha', 'candidateTreeSha',
                   'runId', 'runAttempt', 'mode', 'candidateTag', 'stagingWorkflowSha',
                   'eventName', 'sourceRef', 'comparisonBaseSha', 'prHeadSha'}
DELIVERY_FIELDS = {'bootstrapPolicyBase64', 'canonicalAuthorityBase64', 'helperSha256', 'baselineSnapshot'}


class AdmissionError(ValueError):
    pass


def prepare_admission(policy_bytes, policy_pin, context, binding_bytes, binding_pin):
    """Scope consistency only; approved caller authenticates current policy/context."""
    try:
        a = CLIENT.ADAPTER
        r = a.require
        r(type(policy_pin) is str and re.fullmatch('[a-f0-9]{64}', policy_pin) and
          type(policy_bytes) is bytes and hashlib.sha256(policy_bytes).hexdigest() == policy_pin, 'policy_pin')
        policy = a.read_json(policy_bytes, 65536)
        r(type(policy) is dict and set(policy) == {'schemaVersion', 'purpose', 'producer', 'consumer', 'reuse', 'delivery'} and
          type(policy['schemaVersion']) is int and policy['schemaVersion'] == 2 and
          policy['purpose'] == 'protected129-consumer-admission', 'policy_schema')
        producer, consumer, delivery, reuse = (policy[k] for k in ['producer', 'consumer', 'delivery', 'reuse'])
        r(type(producer) is dict and set(producer) == PRODUCER_FIELDS and
          type(consumer) is dict and set(consumer) == CONSUMER_FIELDS and
          type(context) is dict and set(context) == CONSUMER_FIELDS and consumer == context and
          type(delivery) is dict and set(delivery) == DELIVERY_FIELDS, 'scope_schema')
        r(all(type(v) is str for v in producer.values()), 'producer_type')
        for key in CONSUMER_FIELDS - {'candidateTag', 'stagingWorkflowSha', 'comparisonBaseSha', 'prHeadSha'}:
            r(type(consumer[key]) is str, 'consumer_type')
        r(type(binding_pin) is str and re.fullmatch('[a-f0-9]{64}', binding_pin) and
          type(binding_bytes) is bytes and hashlib.sha256(binding_bytes).hexdigest() == binding_pin, 'binding_pin')
        binding = a.read_json(binding_bytes, 65536)
        r(type(binding) is dict and all(binding.get(k) == producer[k] for k in PRODUCER_FIELDS), 'producer_scope')
        for key in ['repository', 'repositoryId', 'appId', 'candidateSha', 'candidateTreeSha']:
            r(producer[key] == consumer[key], 'consumer_result_scope')
        if consumer['mode'] == 'ci':
            r(consumer['eventName'] in ('push', 'pull_request', 'workflow_dispatch'), 'ci_event')
            authority = a.read_json(base64.b64decode(delivery['canonicalAuthorityBase64'], validate=True), 65536)
            for key in ['sourceRef', 'comparisonBaseSha', 'prHeadSha']:
                r(authority.get(key) == consumer[key], 'ci_context_authority')
        else:
            r(consumer['eventName'] == 'workflow_dispatch' and consumer['sourceRef'] == 'refs/heads/release-main' and
              consumer['comparisonBaseSha'] is None and consumer['prHeadSha'] is None, 'release_event')
        r(type(reuse) is dict and set(reuse) == {'basis', 'producerCompletedAt', 'decisionReceiptSha256'} and
          reuse['basis'] in ('fresh-execution', 'unchanged-source-environment-population') and
          reuse['producerCompletedAt'] == binding.get('completedAt') and
          type(reuse['decisionReceiptSha256']) is str and re.fullmatch('[a-f0-9]{64}', reuse['decisionReceiptSha256']),
          'reuse_basis')
        carrier = {'schemaVersion': 1, 'purpose': 'fedril-workflow-admission-v1', 'repository': consumer['repository'],
                   'runId': consumer['runId'], 'runAttempt': consumer['runAttempt'], 'mode': consumer['mode'],
                   'workflowSha': consumer['workflowSha'], 'bindingSha256': binding_pin,
                   'candidate': {'sha': consumer['candidateSha'], 'treeSha': consumer['candidateTreeSha'],
                                 'tag': consumer['candidateTag'], 'stagingWorkflowSha': consumer['stagingWorkflowSha']},
                   **delivery}
        # Validate the carrier with the same function used immediately before POST.
        CLIENT.add_admission({'output': {}}, binding, binding_pin, carrier)
        return carrier
    except Exception:
        raise AdmissionError('current_consumer_admission_rejected_no_post') from None


def publish_admission(policy_bytes, policy_pin, context, binding_bytes, binding_pin,
                      observed, phases, integrity, population_bytes, trx_bytes, journal, transport):
    carrier = prepare_admission(policy_bytes, policy_pin, context, binding_bytes, binding_pin)
    return CLIENT.publish_once(binding_bytes, binding_pin, observed, phases, integrity,
                               population_bytes, trx_bytes, journal, transport, admission=carrier)
