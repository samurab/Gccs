#!/usr/bin/env python3
"""Run four real mixed startup-failure rounds without using the shared local stack."""
import subprocess, urllib.request, json, datetime, time, hashlib, os, signal, traceback, re, concurrent.futures, sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from recovery_isolation import RecoveryCancellation, RecoveryCancelled, RecoveryEnvironment, clean_env, stop_owned_group

if sys.argv[1:] == ['--help']:
    print('Usage: python3 tools/dev/recovery_stress.py\nFour isolated API-crash/web-hang and API-hang/web-crash rounds. No shared-service stops.\nRun artifacts and stopped dependency containers are retained; no automatic deletion.')
    raise SystemExit(0)
if sys.argv[1:]:
    raise SystemExit('Unexpected arguments; use --help.')
if os.name != 'posix':
    raise SystemExit('The real crash/hang stress test requires POSIX process groups and signals.')
repo = Path(__file__).resolve().parents[2]
environment = RecoveryEnvironment(repo)
folder = environment.folder
api_port, web_port = environment.api_port, environment.web_port
api_lease = (api_port - 1 + 32768) % 65535 + 1
web_lease = (web_port - 1 + 32768) % 65535 + 1
report = {'startedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'directory': str(folder), 'race': None}
attempts=[]; handles=[]; instrumentation=[]; failure=None
cancellation = RecoveryCancellation()
shared_before = None
last_shared_check = 0

sources=['tools/dev/recovery_stress.py','tools/dev/recovery_isolation.py','tools/dev/fixtures/pause-web-startup.mjs','apps/api/Program.cs','apps/api/LocalDevelopment/ExistingDevelopmentApi.cs','apps/api/LocalDevelopment/DevelopmentStartupLease.cs','apps/api/Properties/launchSettings.json','apps/web/package.json','apps/web/vite.config.ts','tools/dev/start-web.mjs','tools/dev/startup-lease.mjs','tools/dev/start-local.mjs','tools/dev/startup-owner-watchdog.sh','tools/dev/startup-watchdog.mjs','apps/api/LocalDevelopment/DevelopmentStartupWatchdog.cs','apps/api/Gccs.Api.csproj','apps/api/LocalDevelopment/DevelopmentStartupOwner.cs','tools/dev/startup-owner-identity.sh','tools/dev/startup-owner-identity.mjs']
def hashes():return {p:hashlib.sha256((repo/p).read_bytes()).hexdigest() for p in sources}
def rows():
    result=[]
    for line in subprocess.check_output(['ps','-axo','pid=,ppid=,pgid=,state=,command='],text=True,start_new_session=True).splitlines():
        p=line.strip().split(None,4)
        if len(p)==5:result.append({'pid':int(p[0]),'ppid':int(p[1]),'pgid':int(p[2]),'state':p[3],'command':p[4]})
    return result
def get_process(pid):
    output=subprocess.run(['ps','-p',str(pid),'-o','pid=,ppid=,pgid=,state=,command='],capture_output=True,text=True,start_new_session=True).stdout.strip()
    if not output:return None
    p=output.split(None,4)
    return {'pid':int(p[0]),'ppid':int(p[1]),'pgid':int(p[2]),'state':p[3],'command':p[4]}
def roots():return [p for p in rows() if p['command']=='node tools/dev/start-local.mjs']
def api_exes():return [p for p in rows() if p['pgid'] in {a['process'].pid for a in attempts if a['record']['app']=='API'} and p['command'].startswith(str(environment.api_command[0]))]
def web_exes():return [p for p in rows() if p['pgid'] in {a['process'].pid for a in attempts if a['record']['app']=='Web'} and str(repo/'tools/dev/start-web.mjs') in p['command']]
def listeners(port):
    r=subprocess.run(['lsof','-nP','-t','-iTCP:'+str(port),'-sTCP:LISTEN'],capture_output=True,text=True,start_new_session=True)
    if r.returncode not in [0,1]:raise RuntimeError(r.stderr)
    return sorted(set(map(int,r.stdout.split())))
def all_listeners():return {str(port):listeners(port) for port in [api_port,web_port,api_lease,web_lease]}
def healthy(app):
    try:
        with urllib.request.urlopen('http://127.0.0.1:'+(str(api_port)+'/health' if app=='API' else str(web_port)+'/'),timeout=2) as response:
            body=response.read().decode()
            return response.status==200 and (json.loads(body).get('status')=='ok' and json.loads(body).get('service')=='gccs-api' if app=='API' else '<title>FeDril | GovCon Compliance Readiness Software</title>' in body)
    except Exception:return False
def docker_inventory():return environment.docker_inventory()
def db_query(sql):return environment.db_query(sql)
def row_counts():return environment.row_counts()

def shared_snapshot():
    current = {str(port): listeners(port) for port in [5062,5173,37830,37941]}
    identities = {}
    for pid in sorted({pid for owners in current.values() for pid in owners} | {p['pid'] for p in roots()}):
        identities[str(pid)] = subprocess.check_output(['ps','-p',str(pid),'-o','pid=,ppid=,lstart=,command='],text=True,start_new_session=True).strip()
    for port,path in [(5062,'/health'),(5173,'/')]:
        with urllib.request.urlopen('http://127.0.0.1:'+str(port)+path, timeout=5) as response:
            body=response.read().decode()
            assert response.status==200
            assert json.loads(body).get('status')=='ok' if port==5062 else '<title>FeDril | GovCon Compliance Readiness Software</title>' in body
    return {'listeners':current,'processes':identities}

def shared_unchanged(force=False):
    global last_shared_check
    if force or time.monotonic()-last_shared_check>=2:
        last_shared_check=time.monotonic()
        assert shared_snapshot()==shared_before, 'Shared API, web or supervisor changed during the isolated test'
        report['sharedHealthSamples']=report.get('sharedHealthSamples',0)+1
    return True

def stop_manual_groups():
    errors=[]
    for a in attempts:
        try:stop_owned_group(a['process'])
        except Exception as error:errors.append(str(error))
    if errors:raise RuntimeError('Owned app cleanup failed: '+'; '.join(errors))
    for _ in range(150):
        if not any(listeners(port) for port in [api_port,web_port,api_lease,web_lease]) and not api_exes() and not web_exes():return
        time.sleep(.1)
    raise RuntimeError('Test app processes remain; no supervisor started over them')
def cold():
    for _ in range(150):
        state=all_listeners()
        if not any(state.values()) and shared_unchanged() and not api_exes() and not web_exes():
            return {'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'listeners':state,'supervisors':[],'apiExecutables':[],'webExecutables':[]}
        time.sleep(.1)
    raise RuntimeError('Could not establish cold app state')
def launch(app,index,result):
    cancellation.check()
    args,cwd=environment.api_command,environment.api_cwd
    if app=='Web':
        args=['node','--inspect-brk=127.0.0.1:0',str(repo/'tools/dev/start-web.mjs'),'--host','127.0.0.1','--port',str(web_port),'--strictPort','--config',str(folder/'vite.config.mjs'),'--configLoader','native']
        cwd=repo/'apps/web'
    path=folder/(result['target'].lower()+'-crash-'+app.lower()+'-'+str(index+1)+'.log')
    handle=path.open('x');handles.append(handle)
    p=subprocess.Popen(args,cwd=cwd,stdout=handle,stderr=subprocess.STDOUT,start_new_session=True,env=environment.api_env if app=='API' else environment.web_env)
    record={'app':app,'index':index+1,'launcherPid':p.pid,'args':args,'cwd':str(cwd),'log':str(path)}
    a={'process':p,'record':record}
    attempts.append(a);result['attempts'].append(record)
    return a
leaseLine=next(i for i,line in enumerate((repo/'tools/dev/start-web.mjs').read_text().splitlines()) if 'if (lease.existingUrl)' in line)
clients={}
def events(path):
    result=[]
    for line in Path(path).read_text().splitlines():
        if line.startswith('{'):
            try:result.append(json.loads(line))
            except json.JSONDecodeError:pass
    return result
def attach_web_client(attempt):
    deadline=time.monotonic()+30;endpoint=None
    while time.monotonic()<deadline:
        cancellation.check()
        match=re.search(r'Debugger listening on (ws://[^\s]+)',Path(attempt['record']['log']).read_text())
        if match:endpoint=match.group(1);break
        assert attempt['process'].poll() is None,'Web attempt exited before debugger attach'
        time.sleep(.01)
    if endpoint is None:raise RuntimeError('No inspector endpoint')
    cancellation.check()
    debugpath=folder/('web-debug-'+str(attempt['record']['index'])+'.log')
    handle=debugpath.open('x');handles.append(handle)
    process=subprocess.Popen(['node',str(repo/'tools/dev/fixtures/pause-web-startup.mjs'),endpoint,str(repo/'tools/dev/start-web.mjs'),str(leaseLine)],stdin=subprocess.PIPE,stdout=handle,stderr=subprocess.STDOUT,text=True,start_new_session=True,env=clean_env())
    item={'process':process,'log':str(debugpath),'targetPid':attempt['process'].pid}
    clients[attempt['process'].pid]=item;instrumentation.append(item)
    report.setdefault('instrumentationProcesses',[]).append({'pid':process.pid,'targetPid':attempt['process'].pid})
    deadline=time.monotonic()+30
    while time.monotonic()<deadline:
        cancellation.check()
        if any(e.get('event') in ['running','paused-at-lease'] for e in events(debugpath)):return item
        assert process.poll() is None,'Debugger exited before starting real entry point'
        time.sleep(.01)
    raise RuntimeError('Web debugger never resumed entry point')
def app_attempt_for_group(app,group):
    found=[a for a in attempts if a['record']['app']==app and a['process'].pid==group]
    assert len(found)==1,('Startup process is not test-owned',app,group)
    return found[0]
def capture_api(excluded):
    deadline=time.monotonic()+65
    while time.monotonic()<deadline:
        cancellation.check()
        owners=listeners(api_lease)
        if owners and owners[0] not in excluded:
            assert len(owners)==1
            candidate=get_process(owners[0])
            if candidate is None:continue
            attempt=app_attempt_for_group('API',candidate['pgid'])
            assert candidate['command'].startswith(str(environment.api_command[0]))
            cancellation.check()
            os.kill(candidate['pid'],signal.SIGSTOP)
            owner=get_process(candidate['pid'])
            assert owner and owner['state'].startswith('T') and not listeners(api_port),'Missed API pre-health window'
            assert 'Now listening on:' not in Path(attempt['record']['log']).read_text()
            return {'app':'API','process':owner,'launcherPid':attempt['process'].pid,'leaseOwners':listeners(api_lease),'applicationListeners':listeners(api_port),'capturedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat()}
        time.sleep(.001)
    raise RuntimeError('No API replacement startup lease')
def capture_web(excluded):
    deadline=time.monotonic()+65
    while time.monotonic()<deadline:
        cancellation.check()
        owners=listeners(web_lease)
        if owners and owners[0] not in excluded:
            assert len(owners)==1
            candidate=get_process(owners[0])
            if candidate is None:continue
            attempt=app_attempt_for_group('Web',candidate['pgid'])
            assert candidate['pid']==attempt['process'].pid
            item=clients[candidate['pid']]
            pauses=[e for e in events(item['log']) if e.get('event')=='paused-at-lease']
            if not pauses:
                assert item['process'].poll() is None,'Debugger exited before lease pause'
                time.sleep(.005);continue
            assert pauses[-1]['location']['lineNumber']==leaseLine and not listeners(web_port)
            cancellation.check()
            os.kill(candidate['pid'],signal.SIGSTOP)
            owner=get_process(candidate['pid'])
            assert owner and owner['state'].startswith('T') and not listeners(web_port)
            assert 'ready in' not in Path(attempt['record']['log']).read_text()
            return {'app':'Web','process':owner,'launcherPid':attempt['process'].pid,'leaseOwners':listeners(web_lease),'applicationListeners':listeners(web_port),'pauseEvent':pauses[-1],'capturedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat()}
        time.sleep(.001)
    raise RuntimeError('No web replacement startup lease')
def release_client(item):
    assert item['process'].poll() is None,'Debugger client exited before release'
    item['process'].stdin.write('{"command":"release"}\n');item['process'].stdin.flush()
def wait_client(item):
    assert item['process'].wait(timeout=10)==0,('Debugger release failed',item['log'])
signal.signal(signal.SIGINT,cancellation.request)
signal.signal(signal.SIGTERM,cancellation.request)

try:
    report['sourceHashesBefore']=hashes()
    report['candidateSha']=subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo,text=True,start_new_session=True).strip()
    report['harness']={'controller':__file__,'sha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'fault':'Two cycles / four rounds: API crash-Web hang, API hang-Web crash, repeated; all failures before health','waiterPolicy':'same original pool; no replacement launches'}
    shared_before=shared_snapshot()
    report['sharedBefore']=shared_before
    report['isolation']={'apiPort':api_port,'webPort':web_port,'apiLease':api_lease,'webLease':web_lease,'dataDirectory':str(folder),'scope':'Real API/Vite startup and lease/watchdog takeover; isolated PostgreSQL/Redis/Azurite, delivery and processing disabled'}
    environment.start()
    report['dockerBefore']=docker_inventory()
    # The native config loader avoids generating/removing bundled config files in shared node_modules.
    config = "import {createRequire} from 'node:module';\nimport {pathToFileURL} from 'node:url';\nconst require=createRequire(" + json.dumps((repo/'apps/web/package.json').as_uri()) + ");\nconst {default:react}=await import(pathToFileURL(require.resolve('@vitejs/plugin-react')).href);\nexport default " + json.dumps({'root':str(repo/'apps/web'),'envDir':str(folder),'cacheDir':str(folder/'web-cache'),'resolve':{'alias':{'@':str(repo/'apps/web/src')}},'optimizeDeps':{'noDiscovery':True,'include':[]},'server':{'host':'127.0.0.1','port':web_port,'strictPort':True}}).replace('"root":', '"plugins":[react()],"root":', 1) + ";\n"
    with (folder/'vite.config.mjs').open('x') as f:f.write(config)
    # Warm only the isolated database so migrations do not invalidate preservation comparisons.
    warmup={'target':'Warmup','attempts':[]}
    report['warmup']=warmup
    launch('API',0,warmup)
    deadline=time.monotonic()+60
    while not healthy('API'):
        assert time.monotonic()<deadline,'Isolated API warmup failed; inspect warmup log'
        shared_unchanged()
        time.sleep(.25)
    request=urllib.request.Request(environment.api_url+'/api/platform/me/access',headers={'X-Gccs-Dev-Auth':'true'})
    with urllib.request.urlopen(request,timeout=5) as response:
        access=json.loads(response.read())
    report['deliveryModes']={key:access[key] for key in ['demoRequestDeliveryMode','invitationDeliveryMode']}
    assert all(mode=='Disabled' for mode in report['deliveryModes'].values())
    stop_manual_groups()
    attempts=[]
    db_query("CREATE SCHEMA recovery_fixture; CREATE TABLE recovery_fixture.sentinel (id integer PRIMARY KEY, value text NOT NULL); INSERT INTO recovery_fixture.sentinel VALUES (1,'synthetic recovery fixture');")
    report['initialCold']=cold()
    report['databaseBefore']=row_counts()
    report['sentinelBefore']=db_query('SELECT id,value FROM recovery_fixture.sentinel ORDER BY id;')
    result={'target':'RepeatedOppositeFailures','attempts':[],'generations':[],'faultsByRound':[{'API':'crash','Web':'hang'},{'API':'hang','Web':'crash'}]*2,'attemptsPerApp':7,'coldBaseline':report['initialCold']}
    report['race']=result
    began=time.monotonic()
    print('COLD: isolated apps and leases absent; shared API/UI remain running.',flush=True)
    initial={app:launch(app,0,result) for app in ['API','Web']}
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        apiFuture=pool.submit(capture_api,set())
        attach_web_client(initial['Web'])
        webFuture=pool.submit(capture_web,set())
        owners={'API':apiFuture.result(timeout=75),'Web':webFuture.result(timeout=75)}
    result['initialOwners']=owners
    def detach_hung_web(owner):
        item=clients[owner['process']['pid']]
        item['process'].terminate();item['process'].wait(timeout=5)
        current=get_process(owner['process']['pid'])
        assert current and current['state'].startswith('T'),'Debugger detachment released suspension'
    detach_hung_web(owners['Web'])
    waiting={app:[launch(app,index,result) for index in range(1,7)] for app in ['API','Web']}
    with concurrent.futures.ThreadPoolExecutor(max_workers=5) as pool:list(pool.map(attach_web_client,waiting['Web']))
    stableSince=None;deadline=time.monotonic()+30
    while time.monotonic()<deadline:
        matches={}
        for app,native in [('API',api_exes()),('Web',web_exes())]:
            groups={a['process'].pid for a in waiting[app]}
            matches[app]=[p for p in native if p['pgid'] in groups]
        complete=all({p['pgid'] for p in matches[app]}=={a['process'].pid for a in waiting[app]} for app in waiting)
        for app in waiting:
            for a in waiting[app]:assert a['process'].poll() is None,('Waiter exited too soon',a['record'])
        assert not listeners(api_port) and not listeners(web_port) and shared_unchanged()
        if complete:
            if stableSince is None:stableSince=time.monotonic()
            if time.monotonic()-stableSince>=1:
                result['waitingProof']={'nativeProcesses':matches,'observedWaitingSeconds':round(time.monotonic()-stableSince,2),'webEntriesRunningWithAfterLeaseBreakpoint':True}
                break
        else:stableSince=None
        time.sleep(.1)
    else:raise RuntimeError('Could not establish all twelve native waiters')
    print('WAITING: six native waiting attempts per app behind the first suspended owners.',flush=True)
    expired=set();killed=set();samples=0
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        for generation in range(1,5):
            before=all_listeners()
            assert not before[str(api_port)] and not before[str(web_port)] and shared_unchanged()
            for app,port in [('API',api_lease),('Web',web_lease)]:
                owner=get_process(owners[app]['process']['pid'])
                assert owner and owner['state'].startswith('T') and before[str(port)]==[owner['pid']]
            faults={'API':'crash','Web':'hang'} if generation%2==1 else {'API':'hang','Web':'crash'}
            entry={'generation':generation,'faults':faults,'preHealthOwners':owners,'elapsedSecondsAtObservation':round(time.monotonic()-began,2),'observedTerminations':{}}
            result['generations'].append(entry)
            excluded=killed|{owners[app]['process']['pid'] for app in owners}
            if generation<4:
                apiFuture=pool.submit(capture_api,excluded.copy())
                webFuture=pool.submit(capture_web,excluded.copy())
            entry['injectedCrashUtc']=datetime.datetime.now(datetime.timezone.utc).isoformat()
            entry['signals']=[]
            for app in ['API','Web']:
                if faults[app]=='crash':
                    pid=owners[app]['process']['pid']
                    os.kill(pid,signal.SIGKILL)
                    entry['signals'].append({'app':app,'pid':pid,'signal':'SIGKILL','monotonicNs':time.monotonic_ns()})
            print('FAULT ROUND '+str(generation)+': '+json.dumps(faults)+' before HTTP health.',flush=True)
            nextProgress=0;deadline=time.monotonic()+65
            while time.monotonic()<deadline:
                current=all_listeners();samples+=1
                assert all(len(v)<=1 for v in current.values()) and shared_unchanged(),current
                assert all(owners[app]['process']['pid'] not in current[str(port)] for app,port in [('API',api_port),('Web',web_port)]),'A targeted owner became healthy before hang capture'
                if generation<4:assert not current[str(api_port)] and not current[str(web_port)],'Missed replacement pre-health capture'
                for app in owners:
                    pid=owners[app]['process']['pid'];process=get_process(pid)
                    if process is None or process['state'].startswith('Z'):
                        attempt=app_attempt_for_group(app,owners[app]['process']['pgid'])
                        log=Path(attempt['record']['log']).read_text()
                        if faults[app]=='hang':assert 'FeDril startup watchdog: owner '+str(pid) in log,('Missing automatic watchdog termination',app,pid)
                        entry['observedTerminations'].setdefault(app,{'pid':pid,'elapsedSeconds':round(time.monotonic()-began,2)})
                        attempt['record'].update(outcome='failed_startup_owner',faultGeneration=generation,faultType=faults[app])
                        killed.add(pid)
                for attempt in attempts:
                    if attempt['process'].poll() not in [None,0] and attempt['record'].get('outcome')!='failed_startup_owner':
                        text=Path(attempt['record']['log']).read_text()
                        if 'did not become healthy within the startup wait' in text:
                            expired.add(attempt['process'].pid)
                            attempt['record'].setdefault('expiredAfterSeconds',round(time.monotonic()-began,2))
                elapsed=time.monotonic()-began
                if elapsed>=nextProgress:
                    print('OBSERVE '+json.dumps({'generation':generation,'elapsedSeconds':round(elapsed,1),'faults':faults,'terminatedOwners':list(entry['observedTerminations']),'expiredWaiters':len(expired),'listeners':current}),flush=True)
                    nextProgress=elapsed+20
                if len(entry['observedTerminations'])==2:break
                time.sleep(.25)
            else:raise RuntimeError('A suspended owner survived its watchdog deadline')
            if generation<4:
                owners={'API':apiFuture.result(timeout=15),'Web':webFuture.result(timeout=15)}
                for app in owners:assert owners[app]['process']['pid'] in [p['pid'] for p in result['waitingProof']['nativeProcesses'][app]]
                detach_hung_web(owners['Web'])
                entry['waitingTakeovers']=owners
                print('TAKEOVER '+str(generation)+': existing waiters acquired both leases and were suspended before health.',flush=True)
    # Remove debugger breakpoints from surviving candidates, then observe actual final outcomes.
    for pid,item in clients.items():
        if pid not in killed and item['process'].poll() is None:
            target=get_process(pid)
            if target and not target['state'].startswith('Z'):
                release_client(item);wait_client(item)
    deadline=time.monotonic()+30
    stable=0
    while time.monotonic()<deadline:
        active=[a for a in attempts if a['process'].poll() is None]
        if not active:break
        ready=healthy('API') and healthy('Web') and all(sum(a['record']['app']==app for a in active)==1 for app in ['API','Web'])
        stable=stable+1 if ready else 0
        if stable>=5:break
        time.sleep(.25)
    current=all_listeners()
    result['finalObservation']={'elapsedSeconds':round(time.monotonic()-began,2),'listeners':current,'healthy':{app:healthy(app) for app in ['API','Web']},'apiExecutables':api_exes(),'webExecutables':web_exes()}
    clean=[];errors=[]
    for a in attempts:
        record=a['record'];code=a['process'].poll();text=Path(record['log']).read_text()
        record['exitCodeAtVerification']=code
        record['printedHealthyUrl']='already running and healthy at ' in text
        record['waitExpired']='did not become healthy within the startup wait' in text
        assert 'address already in use' not in text.lower() and 'EADDRINUSE' not in text
        if record.get('outcome')!='failed_startup_owner':
            if code==0 and record['printedHealthyUrl']:
                record['outcome']='reused';clean.append(record)
                expected='http://127.0.0.1:'+str(api_port) if record['app']=='API' else 'http://127.0.0.1:'+str(web_port)+'/'
                assert expected in text and 'Executed DbCommand' not in text,record
            elif code is not None:record['outcome']='failed_waiter';errors.append(record)
            else:record['outcome']='running'
    result.update(samples=samples,failedStartupOwnerCount=len(killed),watchdogTerminatedOwnerCount=sum(a['record'].get('faultType')=='hang' for a in attempts),injectedCrashOwnerCount=sum(a['record'].get('faultType')=='crash' for a in attempts),cleanUrlExitCount=len(clean),failedWaiters=errors,expiredWaiterCount=sum(a['record']['waitExpired'] for a in attempts))
    result['databaseAfter']=row_counts();result['dockerAfter']=docker_inventory()
    result['sentinelAfter']=db_query('SELECT id,value FROM recovery_fixture.sentinel ORDER BY id;')
    assert result['sentinelAfter']==report['sentinelBefore']
    assert result['databaseAfter']==report['databaseBefore'] and result['dockerAfter']==report['dockerBefore']
    success=not errors and len(clean)==4 and all(result['finalObservation']['healthy'].values()) and len(api_exes())==1 and len(web_exes())==1
    result['status']='PASS' if success else 'FAIL'
    if success:
        winners={}
        for app,port,leasePort,native in [('API',api_port,api_lease,api_exes()),('Web',web_port,web_lease,web_exes())]:
            assert len(native)==1 and current[str(port)]==current[str(leasePort)] and len(current[str(port)])==1
            winner=get_process(current[str(port)][0])
            assert winner['pid'] in [p['pid'] for p in result['waitingProof']['nativeProcesses'][app]]
            assert winner['pid'] not in killed
            winners[app]=winner
        result['finalWaiterTakeovers']=winners
        stableUntil=time.monotonic()+47
        while time.monotonic()<stableUntil:
            assert healthy('API') and healthy('Web') and shared_unchanged()
            assert all_listeners()==current and len(api_exes())==1 and len(web_exes())==1
            time.sleep(1)
        result['healthyWinnerStableSeconds']=47
    if not success:
        result['reason']='The repeated opposite-failure test did not meet all expected takeover, clean URL exit and process-count criteria; inspect attempt logs.'
        failure=RuntimeError(result['reason']);report['failure']=result['reason']
    print('RESULT '+json.dumps({'status':result['status'],'failedStartupOwners':len(killed),'watchdogTerminations':result['watchdogTerminatedOwnerCount'],'injectedCrashes':result['injectedCrashOwnerCount'],'expiredWaiters':result['expiredWaiterCount'],'cleanUrlExits':len(clean),'healthy':result['finalObservation']['healthy']}),flush=True)

except RecoveryCancelled as error:
    print('CANCELLED '+str(error)+'; stopping only test-owned resources.',flush=True)
except Exception as error:
    failure=error;report['failure']=repr(error);report['traceback']=traceback.format_exc()
    print('TEST FAILURE '+repr(error),flush=True)
finally:
    cancellation.cleaning_up = True
    cleanup_started = time.monotonic()
    try:
        try:
            stop_manual_groups()
        finally:
            environment.close()
        shared_unchanged(force=True)
        report['sharedAfter']=shared_snapshot()
        report['isolatedStopped']={'listeners':all_listeners(),'containers':environment.docker_inventory()}
        assert not any(report['isolatedStopped']['listeners'].values())
        assert all(not c['running'] for c in report['isolatedStopped']['containers'].values())
        print('STOPPED only isolated apps/dependencies; shared API/UI process identities unchanged.',flush=True)
    except Exception as error:
        report['restorationFailure']=repr(error)
        if failure is None:failure=error
    for item in instrumentation:
        helper=item['process']
        try:stop_owned_group(helper)
        except Exception as error:
            report.setdefault('instrumentationCleanupFailures',[]).append(str(error))
            if failure is None:failure=error
    for handle in handles:handle.close()
    report['sourceHashesAfter']=hashes()
    report['sourcesUnchanged']=report['sourceHashesAfter']==report['sourceHashesBefore']
    report['finishedUtc']=datetime.datetime.now(datetime.timezone.utc).isoformat()
    report['cleanupSeconds']=round(time.monotonic()-cleanup_started,3)
    if cancellation.signal_number is not None:
        report['cancellation']={'signal':cancellation.signal_number}
    report['status']='FAIL' if failure is not None else 'CANCELLED' if cancellation.signal_number is not None else 'PASS'
    with (folder/'report.json').open('x') as f:json.dump(report,f,indent=2)
    print('EVIDENCE '+str(folder/'report.json'),flush=True)
if failure is not None:raise failure
if cancellation.signal_number is not None:raise SystemExit(128+cancellation.signal_number)
