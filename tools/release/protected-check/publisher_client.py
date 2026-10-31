"""Held provider client; imports do not post, load tokens or create journal files."""
import hashlib
import base64
import http.client
import json
import os
from pathlib import Path
import re
import ssl
import types

ADAPTER_PATH = Path(__file__).with_name('result_adapter.py')
ADAPTER_SHA256 = 'f7784b6c147db0e49d7bb0273d15be11ba39d486b358190b584c8c2fa69ca703'
RESPONSE_CAP = 1024 * 1024
SOCKET_TIMEOUT_SECONDS = 30
API_VERSION = '2026-03-10'


class PublishError(ValueError):
    """Fixed reason codes only; no provider bodies, token or raw exceptions."""


def require(condition, code):
    if not condition:
        raise PublishError(code)


def digest(value):
    return hashlib.sha256(value).hexdigest()


def load_adapter():
    source = ADAPTER_PATH.read_bytes()
    require(digest(source) == ADAPTER_SHA256, 'adapter_identity_changed')
    module = types.ModuleType('fedril_held_result_adapter')
    exec(compile(source, str(ADAPTER_PATH), 'exec'), module.__dict__)
    return module


ADAPTER = load_adapter()


def repository_path(repository):
    require(type(repository) is str and re.fullmatch(r'[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+', repository)
            and all(part not in ('.', '..') for part in repository.split('/')), 'repository_path_rejected')
    return '/repos/' + repository + '/check-runs'


def add_admission(payload, binding, binding_sha256, carrier):
    """Add a separately authorized consumer admission; retain original result/time."""
    expected = {'schemaVersion', 'purpose', 'repository', 'runId', 'runAttempt', 'mode', 'workflowSha',
                'candidate', 'bindingSha256', 'bootstrapPolicyBase64', 'canonicalAuthorityBase64',
                'helperSha256', 'baselineSnapshot'}
    require(type(carrier) is dict and set(carrier) == expected, 'admission_schema')
    require(type(carrier['schemaVersion']) is int and carrier['schemaVersion'] == 1 and
            carrier['purpose'] == 'fedril-workflow-admission-v1', 'admission_purpose')
    require(carrier['repository'] == binding['repository'] and carrier['bindingSha256'] == binding_sha256,
            'admission_result_binding')
    repository_path(carrier['repository'])
    for key in ['runId', 'runAttempt']:
        require(type(carrier[key]) is str and re.fullmatch('[1-9][0-9]{0,19}', carrier[key]), 'admission_run')
    require(type(carrier['workflowSha']) is str and re.fullmatch('[a-f0-9]{40}', carrier['workflowSha']), 'admission_workflow')
    require(type(carrier['mode']) is str and carrier['mode'] in ('ci', 'staging', 'production'), 'admission_mode')
    candidate = carrier['candidate']
    require(type(candidate) is dict and set(candidate) == {'sha', 'treeSha', 'tag', 'stagingWorkflowSha'}, 'admission_candidate')
    require(candidate['sha'] == binding['candidateSha'] and candidate['treeSha'] == binding['candidateTreeSha'],
            'admission_candidate_binding')
    require(candidate['tag'] is None or (type(candidate['tag']) is str and
            re.fullmatch(r'v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)-rc\.[1-9][0-9]*', candidate['tag'])), 'admission_tag')
    if carrier['mode'] == 'ci':
        require(carrier['bootstrapPolicyBase64'] is None and carrier['baselineSnapshot'] is None and
                candidate['stagingWorkflowSha'] is None and candidate['tag'] is None, 'admission_ci_delivery')
        value = carrier['canonicalAuthorityBase64']
        require(type(value) is str and 0 < len(value) <= 60000, 'admission_ci_authority_size')
        try:
            authority = ADAPTER.read_json(base64.b64decode(value, validate=True), 65536)
        except Exception:
            raise PublishError('admission_ci_authority_encoding') from None
        require(type(authority) is dict and type(authority.get('schemaVersion')) is int and
                authority['schemaVersion'] == 2 and authority.get('purpose') == 'classified-ci-history' and
                authority.get('repository') == carrier['repository'] and
                authority.get('workflowSha') == carrier['workflowSha'] and
                authority.get('tipSha') == binding['candidateSha'], 'admission_ci_authority_binding')
    else:
        decoded = []
        for key in ['bootstrapPolicyBase64', 'canonicalAuthorityBase64']:
            value = carrier[key]
            require(type(value) is str and 0 < len(value) <= 60000, 'admission_delivery_size')
            try:
                decoded.append(base64.b64decode(value, validate=True))
            except Exception:
                raise PublishError('admission_delivery_encoding') from None
        bootstrap = ADAPTER.read_json(decoded[0], 16384)
        authority = ADAPTER.read_json(decoded[1], 65536)
        require(type(bootstrap) is dict and type(authority) is dict and
                bootstrap.get('repository') == authority.get('repository') == carrier['repository'] and
                bootstrap.get('workflowSha') == authority.get('workflowSha') == carrier['workflowSha'] and
                bootstrap.get('canonicalAuthoritySha256') == digest(decoded[1]), 'admission_delivery_binding')
        snapshot = carrier['baselineSnapshot']
        require(type(snapshot) is dict and set(snapshot) == {'path', 'sha256'} and
                snapshot['path'] == 'release/migration-baseline.snapshot.json' and
                type(snapshot['sha256']) is str and re.fullmatch('[a-f0-9]{64}', snapshot['sha256']), 'admission_snapshot')
        stage = candidate['stagingWorkflowSha']
        require((carrier['mode'] == 'staging' and stage is None) or
                (carrier['mode'] == 'production' and candidate['tag'] is not None and
                 type(stage) is str and re.fullmatch('[a-f0-9]{40}', stage)),
                'admission_staging_source')
    helpers = carrier['helperSha256']
    require(type(helpers) is dict and 0 < len(helpers) <= 64, 'admission_helpers')
    for path, identity in helpers.items():
        require(type(path) is str and re.fullmatch(r'tools/release/[A-Za-z0-9_./-]+', path) and
                all(x not in ('', '.', '..') for x in path.split('/')) and
                type(identity) is str and re.fullmatch('[a-f0-9]{64}', identity), 'admission_helper_path')
    text = json.dumps(carrier, sort_keys=True, separators=(',', ':'), ensure_ascii=True)
    require(len(text.encode()) <= 60000, 'admission_carrier_size')
    result = dict(payload)
    result['output'] = dict(payload['output']) | {'text': text}
    result['details_url'] = ('https://github.com/' + carrier['repository'] + '/actions/runs/' +
                             carrier['runId'] + '/attempts/' + carrier['runAttempt'])
    return result


def compare_admission(check, payload):
    if 'details_url' in payload:
        require(check.get('details_url') == payload['details_url'] and type(check.get('output')) is dict and
                check['output'].get('text') == payload['output']['text'], 'provider_admission_binding')


class GitHubChecksTransport:
    """Future approved caller supplies an opaque token; no token creation/storage."""
    def __init__(self, token):
        require(type(token) is str and 0 < len(token) <= 4096 and
                all(33 <= ord(char) <= 126 for char in token), 'opaque_token_rejected')
        self._token = token

    def request(self, method, path, body=None):
        match = re.fullmatch(r'/repos/([A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+)/check-runs(?:/([1-9][0-9]{0,19}))?', path)
        require(match is not None, 'http_target_rejected')
        base = repository_path(match[1])
        require(path == base + ('/' + match[2] if match[2] else '')
                and ((method == 'POST' and match[2] is None and type(body) is bytes)
                     or (method == 'GET' and match[2] is not None and body is None)), 'http_target_rejected')
        connection = None
        try:
            connection = http.client.HTTPSConnection('api.github.com', 443, timeout=SOCKET_TIMEOUT_SECONDS,
                                                      context=ssl.create_default_context())
            headers = {'Accept': 'application/vnd.github+json', 'Authorization': 'Bearer ' + self._token,
                       'X-GitHub-Api-Version': API_VERSION, 'User-Agent': 'FeDril-Protected-Publisher'}
            if method == 'POST':
                headers['Content-Type'] = 'application/json'
            connection.request(method, path, body=body, headers=headers)
            response = connection.getresponse()
            data = response.read(RESPONSE_CAP + 1)
            require(type(data) is bytes and len(data) <= RESPONSE_CAP, 'http_response_size')
            return response.status, data
        except Exception:
            raise PublishError('http_exchange_failed_reconcile') from None
        finally:
            if connection is not None:
                try:
                    connection.close()
                except Exception:
                    pass


class ExclusiveFileJournal:
    """Prepared native journal. Runtime path/intake/retention require exact authority."""
    def __init__(self, root):
        self.root = Path(root)

    def _sync_directory(self, path):
        fd = os.open(path, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW)
        try:
            os.fsync(fd)
        finally:
            os.close(fd)

    def begin(self, metadata):
        require(self.root.is_absolute(), 'journal_absolute_path_required')
        for part in [*reversed(self.root.parents), self.root]:
            require(not part.is_symlink(), 'journal_symlink_rejected')
        self.root.mkdir(mode=0o700, exist_ok=False)
        self._sync_directory(self.root.parent)
        self.record('intent', metadata)

    def record(self, name, metadata):
        require(name in ('intent', 'post-observed', 'verified', 'uncertain'), 'journal_record_rejected')
        with (self.root / (name + '.json')).open('x') as stream:
            json.dump(metadata, stream, indent=2)
            stream.write('\n')
            stream.flush()
            os.fsync(stream.fileno())
        self._sync_directory(self.root)


def publish_once(binding_bytes, binding_sha256, observed, phases, integrity,
                 population_bytes, trx_bytes, journal, transport, *, admission=None):
    """Verify result, exclusively record intent, POST once, then exact-ID GET.

    The approved caller authenticates producer inputs, external binding pin,
    transport/journal implementation, token custody and current-attempt authority.
    The function cannot establish that trust from supplied values alone.
    """
    try:
        result = ADAPTER.verify_result(binding_bytes, binding_sha256, observed, phases, integrity,
                                       population_bytes, trx_bytes)
        binding = ADAPTER.read_json(binding_bytes, 65536)
        path = repository_path(binding['repository'])
        payload = result['checkRunPayload']
        if admission is not None:
            payload = add_admission(payload, binding, binding_sha256, admission)
        body = json.dumps(payload, separators=(',', ':'), ensure_ascii=True).encode()
    except Exception:
        raise PublishError('producer_verification_rejected_no_post') from None
    intent = {'purpose': 'one-protected-check-post', 'repository': binding['repository'],
              'appId': binding['appId'], 'candidateSha': binding['candidateSha'],
              'bindingSha256': binding_sha256, 'requestBodySha256': digest(body),
              'releaseAuthorized': False}
    try:
        journal.begin(intent)
    except Exception:
        raise PublishError('intent_not_durable_no_post') from None
    stage, check_id = 'post_attempt', None
    try:
        status, response = transport.request('POST', path, body)
        require(type(status) is int and status == 201, 'post_status_rejected')
        check = ADAPTER.read_json(response, RESPONSE_CAP)
        require(type(check) is dict and type(check.get('id')) is int and check['id'] > 0, 'post_id_rejected')
        check_id = check['id']
        journal.record('post-observed', {'checkRunId': check_id, 'responseSha256': digest(response),
                                         'bindingValidated': False, 'releaseAuthorized': False})
        ADAPTER.require_provider_success(check, payload, binding['appId'])
        compare_admission(check, payload)
        stage = 'get_attempt'
        status, response = transport.request('GET', path + '/' + str(check_id))
        require(type(status) is int and status == 200, 'get_status_rejected')
        check = ADAPTER.read_json(response, RESPONSE_CAP)
        require(type(check) is dict and type(check.get('id')) is int and check['id'] == check_id, 'get_id_mismatch')
        verified = ADAPTER.require_provider_success(check, payload, binding['appId'])
        compare_admission(check, payload)
        journal.record('verified', {'checkRunId': check_id, 'responseSha256': digest(response),
                                   'bindingSha256': binding_sha256, 'releaseAuthorized': False})
        return verified | {'providerWritePerformed': True, 'releaseAuthorized': False}
    except Exception:
        # external_id is a correlation value, not a provider-enforced idempotency key.
        try:
            journal.record('uncertain', {'stage': stage, 'observedCheckRunId': check_id,
                           'state': 'RECONCILE_NO_RETRY', 'releaseAuthorized': False,
                           'nextAction': 'Authenticated exact-repository GET/list reconciliation under approved scope; never repeat POST automatically.'})
        except Exception:
            pass
        raise PublishError('provider_outcome_uncertain_reconcile_no_retry') from None
