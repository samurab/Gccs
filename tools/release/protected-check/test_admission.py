"""Synthetic policies and in-memory provider only; no actual HTTP or journal IO."""
import base64
import copy
import hashlib
import json
from pathlib import Path
import runpy
import unittest
from unittest.mock import patch

M = runpy.run_path(str(Path(__file__).with_name('admission.py')))
C = M['CLIENT']
sha = lambda b: hashlib.sha256(b).hexdigest()
raw = lambda d: json.dumps(d, sort_keys=True, separators=(',', ':')).encode()


class Journal:
    def __init__(self):
        self.records = {}
    def begin(self, value):
        if self.records:
            raise OSError('collision')
        self.records['intent'] = copy.deepcopy(value)
    def record(self, key, value):
        self.records[key] = copy.deepcopy(value)


class Transport:
    def __init__(self, checks):
        self.checks = checks
        self.calls = []
    def request(self, method, path, body=None):
        self.calls.append((method, path, body))
        return (201 if method == 'POST' else 200), raw(self.checks[len(self.calls)-1])


class AdmissionTests(unittest.TestCase):
    def setUp(self):
        self.binding = {k: 'd'*64 for k in M['PRODUCER_FIELDS']}
        self.binding.update(repository='example/source', repositoryId='41', appId='42', candidateSha='a'*40,
                            candidateTreeSha='b'*40, runNonce='11111111-2222-3333-4444-555555555555',
                            runtimeDigest='sha256:'+'e'*64, completedAt='2026-10-31T01:01:00Z')
        self.context = {k: self.binding[k] for k in ['repository','repositoryId','appId','candidateSha','candidateTreeSha']}
        self.context.update(workflowSha='f'*40, runId='51', runAttempt='2', mode='ci', candidateTag=None, stagingWorkflowSha=None)
        self.context.update(eventName='pull_request', sourceRef='refs/pull/7/merge', comparisonBaseSha='c'*40, prHeadSha='d'*40)
        self.policy = {'schemaVersion': 2, 'purpose': 'protected129-consumer-admission',
                       'producer': {k:self.binding[k] for k in M['PRODUCER_FIELDS']}, 'consumer': dict(self.context),
                       'reuse': {'basis':'fresh-execution','producerCompletedAt':self.binding['completedAt'],
                                 'decisionReceiptSha256':'6'*64},
                       'delivery': {'bootstrapPolicyBase64':None,'canonicalAuthorityBase64':base64.b64encode(raw({
                            'schemaVersion':2,'purpose':'classified-ci-history','repository':self.binding['repository'],
                            'workflowSha':self.context['workflowSha'],'tipSha':self.binding['candidateSha'],
                            **{k:self.context[k] for k in ['sourceRef','comparisonBaseSha','prHeadSha']}})).decode(),
                                    'helperSha256':{'tools/release/protected/launcher.mjs':'7'*64},'baselineSnapshot':None}}
        self.b = raw(self.binding)
        self.pin = sha(self.b)
        self.payload = {'name':'FeDril protected historical assertions (129)','head_sha':'a'*40,
                        'external_id':'fedril-historical129:'+self.pin,'status':'completed','conclusion':'success',
                        'started_at':'2026-10-31T01:00:00Z','completed_at':self.binding['completedAt'],
                        'output':{'title':'Synthetic','summary':'Synthetic'}}

    def prepare(self, policy=None, context=None):
        p = raw(self.policy if policy is None else policy)
        return M['prepare_admission'](p,sha(p),self.context if context is None else context,self.b,self.pin)

    def test_scope_and_canonical_url(self):
        carrier = self.prepare()
        payload = C.add_admission(self.payload,self.binding,self.pin,carrier)
        self.assertEqual(payload['details_url'],'https://github.com/example/source/actions/runs/51/attempts/2')
        self.assertEqual(payload['completed_at'],self.binding['completedAt'])
        self.assertEqual(json.loads(payload['output']['text']),carrier)
        self.assertNotIn('text',self.payload['output'])

    def test_manual_ci_dispatch_with_zero_base_and_no_pr_head(self):
        context=self.context|{'eventName':'workflow_dispatch','sourceRef':'refs/heads/release-main',
                              'comparisonBaseSha':'0'*40,'prHeadSha':None}
        policy=copy.deepcopy(self.policy);policy['consumer']=context
        authority=json.loads(base64.b64decode(policy['delivery']['canonicalAuthorityBase64']))
        authority.update({k:context[k] for k in ['sourceRef','comparisonBaseSha','prHeadSha']})
        policy['delivery']['canonicalAuthorityBase64']=base64.b64encode(raw(authority)).decode()
        self.assertEqual(self.prepare(policy=policy,context=context)['mode'],'ci')

    def test_every_current_context_mismatch_rejected(self):
        for k in self.context:
            with self.subTest(key=k),self.assertRaises(M['AdmissionError']):
                self.prepare(context=self.context|{k:'old'})

    def test_every_producer_scope_mismatch_rejected(self):
        for k in M['PRODUCER_FIELDS']:
            p = copy.deepcopy(self.policy);p['producer'][k]='old'
            with self.subTest(key=k),self.assertRaises(M['AdmissionError']):self.prepare(policy=p)

    def test_valid_reuse_retains_original_producer_time_and_nonce(self):
        p = copy.deepcopy(self.policy);p['reuse']['basis']='unchanged-source-environment-population'
        p['consumer']['runAttempt']='3'
        carrier=self.prepare(policy=p,context=self.context|{'runAttempt':'3'})
        self.assertEqual(carrier['bindingSha256'],self.pin)
        self.assertEqual(self.binding['completedAt'],p['reuse']['producerCompletedAt'])
        self.assertEqual(p['producer']['runNonce'],self.binding['runNonce'])

    def test_reuse_requires_bound_time_and_receipt(self):
        for k,v in [('basis','assume'),('producerCompletedAt','new'),('decisionReceiptSha256','bad')]:
            p=copy.deepcopy(self.policy);p['reuse'][k]=v
            with self.subTest(key=k),self.assertRaises(M['AdmissionError']):self.prepare(policy=p)

    def test_guard_denial_never_calls_client(self):
        with patch.object(C,'publish_once') as client:
            with self.assertRaises(M['AdmissionError']):
                M['publish_admission'](b'{}','0'*64,{},self.b,self.pin,{}, {}, {},b'',b'',object(),object())
            client.assert_not_called()

    def test_one_post_exact_get_and_admission_match(self):
        carrier=self.prepare();payload=C.add_admission(self.payload,self.binding,self.pin,carrier)
        check=payload|{'id':61,'app':{'id':42}}
        j=Journal();t=Transport([check,check]);p=raw(self.policy)
        with patch.object(C.ADAPTER,'verify_result',return_value={'checkRunPayload':self.payload}):
            result=M['publish_admission'](p,sha(p),self.context,self.b,self.pin,{}, {}, {},b'',b'',j,t)
        self.assertTrue(result['providerWritePerformed'])
        self.assertEqual([c[0] for c in t.calls],['POST','GET'])
        self.assertTrue(t.calls[-1][1].endswith('/61'))
        self.assertIn('verified',j.records)

    def test_ack_and_get_require_exact_run_url_and_carrier(self):
        carrier=self.prepare();payload=C.add_admission(self.payload,self.binding,self.pin,carrier)
        check=payload|{'id':61,'app':{'id':42}}
        for phase in [0,1]:
            for change in [{'details_url':payload['details_url'].replace('/2','/1')},
                           {'output':payload['output']|{'text':'{}'}}]:
                checks=[check,check];checks[phase]=check|change;j=Journal();t=Transport(checks)
                with self.subTest(phase=phase,change=change),patch.object(C.ADAPTER,'verify_result',return_value={'checkRunPayload':self.payload}):
                    with self.assertRaises(C.PublishError):
                        C.publish_once(self.b,self.pin,{}, {}, {},b'',b'',j,t,admission=carrier)
                self.assertEqual(sum(x[0]=='POST' for x in t.calls),1)
                self.assertIn('uncertain',j.records)

    def test_invalid_carrier_never_establishes_intent_or_http(self):
        for change in [{'runId':'../51'},{'runAttempt':True},{'workflowSha':'bad'},
                       {'repository':'other/repository'},{'bindingSha256':'0'*64},
                       {'extra':'notallowed'},{'mode':'other'}]:
            j=Journal();t=Transport([])
            with self.subTest(change=change),patch.object(C.ADAPTER,'verify_result',return_value={'checkRunPayload':self.payload}):
                with self.assertRaises(C.PublishError):
                    C.publish_once(self.b,self.pin,{}, {}, {},b'',b'',j,t,admission=self.prepare()|change)
            self.assertEqual(j.records,{})
            self.assertEqual(t.calls,[])

    def test_real_invalid_result_still_denied(self):
        p=raw(self.policy);j=Journal();t=Transport([])
        with self.assertRaises(C.PublishError):
            M['publish_admission'](p,sha(p),self.context,self.b,self.pin,{}, {}, {},b'',b'',j,t)
        self.assertEqual(j.records,{})
        self.assertEqual(t.calls,[])

    def release(self,mode='production'):
        authority=raw({'repository':self.binding['repository'],'workflowSha':self.context['workflowSha']})
        bootstrap=raw({'repository':self.binding['repository'],'workflowSha':self.context['workflowSha'],
                       'canonicalAuthoritySha256':sha(authority)})
        return self.prepare()|{'mode':mode,'bootstrapPolicyBase64':base64.b64encode(bootstrap).decode(),
               'canonicalAuthorityBase64':base64.b64encode(authority).decode(),
               'helperSha256':{'tools/release/protected/launcher.mjs':'7'*64},
               'baselineSnapshot':{'path':'release/migration-baseline.snapshot.json','sha256':'8'*64},
               'candidate':{'sha':self.binding['candidateSha'],'treeSha':self.binding['candidateTreeSha'],
                            'tag':'v0.2.1-rc.1','stagingWorkflowSha':'9'*40 if mode=='production' else None}}

    def test_release_delivery_basic_bindings(self):
        for mode in ['staging','production']:
            self.assertIn('details_url',C.add_admission(self.payload,self.binding,self.pin,self.release(mode)))

    def test_release_delivery_rejects_malformed_or_unbound_data(self):
        for change in [{'bootstrapPolicyBase64':'!'}, {'helperSha256':{'tools/release/../escape':'7'*64}},
                       {'baselineSnapshot':{'path':'https://evil','sha256':'8'*64}},
                       {'candidate':self.release()['candidate']|{'stagingWorkflowSha':None}}]:
            with self.subTest(change=change),self.assertRaises(C.PublishError):
                C.add_admission(self.payload,self.binding,self.pin,self.release()|change)


if __name__=='__main__':unittest.main()
