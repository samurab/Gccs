"""In-memory orchestration and mocked HTTPS only; no actual HTTP or journal IO."""
import copy
import hashlib
import json
from pathlib import Path
import runpy
import ssl
import unittest
from unittest.mock import Mock, patch

M = runpy.run_path(str(Path(__file__).with_name('publisher_client.py')))
CLIENT = M['publish_once']
ERROR = M['PublishError']
TOKEN = 'synthetic-opaque-token-never-log'
BINDING = json.dumps({'repository': 'owner/repository', 'repositoryId': '101', 'appId': '202',
                      'candidateSha': 'a' * 40}).encode()
PIN = hashlib.sha256(BINDING).hexdigest()
PAYLOAD = {'name': 'FeDril protected historical assertions (129)', 'head_sha': 'a' * 40,
           'external_id': 'fedril-historical129:' + PIN, 'status': 'completed', 'conclusion': 'success',
           'started_at': '2026-10-30T01:00:00Z', 'completed_at': '2026-10-30T01:01:00Z',
           'output': {'title': 'Synthetic', 'summary': 'Synthetic adapter orchestration test only'}}
CHECK = PAYLOAD | {'id': 303, 'app': {'id': 202}}
ARGS = (BINDING, PIN, {}, {}, {}, b'synthetic population', b'synthetic TRX')


def body(value=CHECK):
    return json.dumps(value).encode()


class MemoryJournal:
    def __init__(self, fail=None):
        self.started = False
        self.records = {}
        self.fail = fail

    def begin(self, metadata):
        if self.started or self.fail == 'intent':
            raise OSError('synthetic journal collision/failure')
        self.started = True
        self.records['intent'] = copy.deepcopy(metadata)

    def record(self, name, metadata):
        if name in self.records or self.fail == name:
            raise OSError('synthetic journal failure')
        self.records[name] = copy.deepcopy(metadata)


class FakeTransport:
    def __init__(self, responses=None):
        self.responses = list(responses if responses is not None else [(201, body()), (200, body())])
        self.calls = []

    def request(self, method, path, payload=None):
        self.calls.append((method, path, payload))
        response = self.responses.pop(0)
        if isinstance(response, Exception):
            raise response
        return response


class PublisherClientTests(unittest.TestCase):
    def invoke(self, transport=None, journal=None, args=ARGS):
        journal = journal or MemoryJournal()
        transport = transport or FakeTransport()
        with patch.object(M['ADAPTER'], 'verify_result', return_value={'checkRunPayload': copy.deepcopy(PAYLOAD)}) as verifier:
            result = CLIENT(*args, journal, transport)
            verifier.assert_called_once_with(*args)
        return result, journal, transport

    def rejected(self, transport, journal=None, args=ARGS):
        journal = journal or MemoryJournal()
        with patch.object(M['ADAPTER'], 'verify_result', return_value={'checkRunPayload': copy.deepcopy(PAYLOAD)}):
            with self.assertRaises(ERROR) as caught:
                CLIENT(*args, journal, transport)
        self.assertNotIn(TOKEN, str(caught.exception))
        self.assertNotIn(TOKEN, json.dumps(journal.records))
        return journal, str(caught.exception)

    def test_one_post_then_exact_id_get(self):
        result, journal, transport = self.invoke()
        self.assertEqual(result, {'result': 'matched-success', 'checkRunId': 303,
                                  'providerWritePerformed': True, 'releaseAuthorized': False})
        self.assertEqual([c[:2] for c in transport.calls], [('POST', '/repos/owner/repository/check-runs'),
                                                          ('GET', '/repos/owner/repository/check-runs/303')])
        self.assertEqual(json.loads(transport.calls[0][2]), PAYLOAD)
        self.assertEqual(set(journal.records), {'intent', 'post-observed', 'verified'})

    def test_real_verifier_denial_has_no_intent_or_http(self):
        journal, transport = MemoryJournal(), FakeTransport()
        with self.assertRaises(ERROR):
            CLIENT(*ARGS, journal, transport)
        self.assertEqual(transport.calls, [])
        self.assertEqual(journal.records, {})

    def test_verifier_exception_is_redacted(self):
        journal, transport = MemoryJournal(), FakeTransport()
        with patch.object(M['ADAPTER'], 'verify_result', side_effect=ValueError(TOKEN)):
            with self.assertRaisesRegex(ERROR, '^producer_verification_rejected_no_post$'):
                CLIENT(*ARGS, journal, transport)
        self.assertEqual(transport.calls, [])

    def test_intent_failure_has_no_http(self):
        transport = FakeTransport()
        _, code = self.rejected(transport, MemoryJournal(fail='intent'))
        self.assertEqual(code, 'intent_not_durable_no_post')
        self.assertEqual(transport.calls, [])

    def test_repeat_same_journal_never_posts_again(self):
        _, journal, transport = self.invoke()
        _, code = self.rejected(transport, journal)
        self.assertEqual(code, 'intent_not_durable_no_post')
        self.assertEqual(len(transport.calls), 2)

    def test_intent_precedes_post(self):
        journal = MemoryJournal()
        class OrderedTransport(FakeTransport):
            def request(self, method, path, payload=None):
                if method == 'POST':
                    self_outer.assertIn('intent', journal.records)
                return super().request(method, path, payload)
        self_outer = self
        self.invoke(OrderedTransport(), journal)

    def test_post_statuses_stop_without_get_or_retry(self):
        for status in [200, 202, 301, 302, 307, 308, 401, 403, 429, 500, True]:
            with self.subTest(status=status):
                transport = FakeTransport([(status, body())])
                journal, _ = self.rejected(transport)
                self.assertEqual(len(transport.calls), 1)
                self.assertEqual(journal.records['uncertain']['stage'], 'post_attempt')

    def test_post_timeout_is_uncertain_without_retry(self):
        transport = FakeTransport([TimeoutError(TOKEN)])
        journal, _ = self.rejected(transport)
        self.assertEqual(len(transport.calls), 1)
        self.assertIsNone(journal.records['uncertain']['observedCheckRunId'])

    def test_post_invalid_json_and_oversize_stop(self):
        for value in [b'not json', b'{}', b'[]', b'{"id":303,"id":304}', b'x' * (M['RESPONSE_CAP'] + 1)]:
            with self.subTest(size=len(value)):
                transport = FakeTransport([(201, value)])
                self.rejected(transport)
                self.assertEqual(len(transport.calls), 1)

    def test_post_invalid_ids_stop(self):
        for identity in [None, True, 0, -1, '303']:
            with self.subTest(identity=identity):
                transport = FakeTransport([(201, body(CHECK | {'id': identity}))])
                self.rejected(transport)
                self.assertEqual(len(transport.calls), 1)

    def test_post_binding_mismatch_stops_before_get(self):
        for change in [{'app': {'id': 999}}, {'name': 'wrong'}, {'head_sha': 'b' * 40},
                       {'external_id': 'fedril-historical129:' + '0' * 64},
                       {'status': 'in_progress'}, {'conclusion': 'failure'}]:
            with self.subTest(change=change):
                transport = FakeTransport([(201, body(CHECK | change))])
                journal, _ = self.rejected(transport)
                self.assertEqual(len(transport.calls), 1)
                self.assertEqual(journal.records['post-observed']['checkRunId'], 303)
                self.assertFalse(journal.records['post-observed']['bindingValidated'])

    def test_get_timeout_keeps_acknowledged_id(self):
        transport = FakeTransport([(201, body()), TimeoutError(TOKEN)])
        journal, _ = self.rejected(transport)
        self.assertEqual(len(transport.calls), 2)
        self.assertEqual(journal.records['uncertain']['observedCheckRunId'], 303)
        self.assertEqual(journal.records['uncertain']['stage'], 'get_attempt')

    def test_get_statuses_stop_without_retry(self):
        for status in [201, 301, 302, 401, 403, 404, 429, 500, True]:
            with self.subTest(status=status):
                transport = FakeTransport([(201, body()), (status, body())])
                self.rejected(transport)
                self.assertEqual(len(transport.calls), 2)

    def test_get_must_match_acknowledged_id_and_binding(self):
        for change in [{'id': 304}, {'id': True}, {'app': {'id': 999}}, {'name': 'wrong'},
                       {'head_sha': 'b' * 40}, {'external_id': 'wrong'}, {'status': 'queued'}, {'conclusion': 'failure'}]:
            with self.subTest(change=change):
                transport = FakeTransport([(201, body()), (200, body(CHECK | change))])
                journal, _ = self.rejected(transport)
                self.assertEqual(len(transport.calls), 2)
                self.assertNotIn('verified', journal.records)

    def test_get_invalid_json_and_oversize_stop(self):
        for value in [b'null', b'bad', b'{"id":303,"id":303}', b'x' * (M['RESPONSE_CAP'] + 1)]:
            transport = FakeTransport([(201, body()), (200, value)])
            self.rejected(transport)
            self.assertEqual(len(transport.calls), 2)

    def test_journal_failure_after_post_does_not_retry(self):
        for phase, expected in [('post-observed', 1), ('verified', 2)]:
            with self.subTest(phase=phase):
                transport = FakeTransport()
                self.rejected(transport, MemoryJournal(fail=phase))
                self.assertEqual(len(transport.calls), expected)

    def test_repository_injection_rejected_before_intent(self):
        for repository in ['../repository', 'owner/..', 'owner/repo?x=y', 'owner/repo#x',
                           'owner/repo/extra', 'owner/%2e%2e', 'https://evil/repo', 'owner/repo\r\nX:bad']:
            with self.subTest(repository=repository):
                binding = json.dumps(json.loads(BINDING) | {'repository': repository}).encode()
                transport, journal = FakeTransport(), MemoryJournal()
                self.rejected(transport, journal, (binding, PIN, {}, {}, {}, b'p', b't'))
                self.assertEqual(transport.calls, [])
                self.assertEqual(journal.records, {})

    def test_token_stays_out_of_records_and_error_text(self):
        for responses in [[RuntimeError(TOKEN)], [(201, body()), RuntimeError(TOKEN)]]:
            journal, code = self.rejected(FakeTransport(responses))
            self.assertNotIn(TOKEN, json.dumps(journal.records) + code)
            self.assertFalse(any('response' in key and key != 'responseSha256' for record in journal.records.values() for key in record))


class TransportTests(unittest.TestCase):
    def test_fixed_tls_host_headers_timeout_and_no_redirect_follow(self):
        response = Mock(status=302)
        response.read.return_value = b'redirect'
        connection = Mock()
        connection.getresponse.return_value = response
        with patch.object(M['http'].client, 'HTTPSConnection', return_value=connection) as factory:
            transport = M['GitHubChecksTransport'](TOKEN)
            self.assertEqual(transport.request('POST', '/repos/owner/check-runs/check-runs', b'{}'), (302, b'redirect'))
        args, kwargs = factory.call_args
        self.assertEqual(args, ('api.github.com', 443))
        self.assertEqual(kwargs['timeout'], 30)
        self.assertTrue(kwargs['context'].check_hostname)
        self.assertEqual(kwargs['context'].verify_mode, ssl.CERT_REQUIRED)
        headers = connection.request.call_args.kwargs['headers']
        self.assertEqual(headers['Authorization'], 'Bearer ' + TOKEN)
        self.assertEqual(headers['X-GitHub-Api-Version'], '2026-03-10')
        response.read.assert_called_once_with(M['RESPONSE_CAP'] + 1)
        connection.request.assert_called_once()
        connection.close.assert_called_once()

    def test_transport_rejects_redirect_targets_and_methods_before_connection(self):
        cases = [('GET', 'https://evil.test/a', None), ('GET', '/repos/a/b/check-runs/1?x=2', None),
                 ('GET', '/repos/../b/check-runs/1', None), ('GET', '/repos/a/b/check-runs', None),
                 ('POST', '/repos/a/b/check-runs/1', b'{}'), ('DELETE', '/repos/a/b/check-runs/1', None),
                 ('POST', '/repos/a/b/check-runs', None)]
        with patch.object(M['http'].client, 'HTTPSConnection') as factory:
            for method, path, payload in cases:
                with self.subTest(path=path), self.assertRaises(ERROR):
                    M['GitHubChecksTransport'](TOKEN).request(method, path, payload)
            factory.assert_not_called()

    def test_transport_redacts_timeout_and_oversize(self):
        for failure in ['timeout', 'oversize']:
            with self.subTest(failure=failure):
                connection = Mock()
                if failure == 'timeout':
                    connection.request.side_effect = TimeoutError(TOKEN)
                else:
                    connection.getresponse.return_value.read.return_value = b'x' * (M['RESPONSE_CAP'] + 1)
                with patch.object(M['http'].client, 'HTTPSConnection', return_value=connection):
                    with self.assertRaisesRegex(ERROR, '^http_exchange_failed_reconcile$') as caught:
                        M['GitHubChecksTransport'](TOKEN).request('GET', '/repos/a/b/check-runs/303')
                self.assertNotIn(TOKEN, str(caught.exception))
                connection.close.assert_called_once()

    def test_bad_token_syntax_rejected_without_network(self):
        with patch.object(M['http'].client, 'HTTPSConnection') as factory:
            for token in ['', 'x\r\ny', 'x y', 'x' * 4097, None]:
                with self.subTest(kind=type(token).__name__), self.assertRaises(ERROR):
                    M['GitHubChecksTransport'](token)
            factory.assert_not_called()


if __name__ == '__main__':
    unittest.main()
