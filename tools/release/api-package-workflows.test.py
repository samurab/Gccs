"""Package integration checks; shell effects are mocked, workflows never dispatched."""
import pathlib,re,subprocess,unittest

ROOT=pathlib.Path(__file__).resolve().parents[2]
WORKFLOWS={n:(ROOT/'.github/workflows'/n).read_text() for n in ['staging.yml','production-release.yml','production.yml']}

def named(text,name):
    return text.split('      - name: '+name+'\n',1)[1].split('      - name:',1)[0]

def body(step):
    return '\n'.join(line[10:] if line.startswith('          ') else line for line in step.split('        run: |\n',1)[1].splitlines())

def shell(source,stubs):
    return subprocess.run(['/bin/bash','--noprofile','--norc','-e','-o','pipefail','-c',stubs+'\n'+source],env={'PATH':'/usr/bin:/bin','RUNNER_TEMP':'/synthetic path','GITHUB_RUN_ID':'11','GITHUB_RUN_ATTEMPT':'2','GITHUB_ENV':'/synthetic-no-write','GITHUB_OUTPUT':'/synthetic-no-write','API_PACKAGE_GUARD':'/guard path','API_PACKAGE_GUARD_SHA256':'a'*64,'API_PACKAGE_SHA256':'b'*64,'API_PACKAGE_PATH':'/zip path','APPROVED_RELEASE_MANIFEST':'/manifest'},capture_output=True,text=True,timeout=5)


class WorkflowTests(unittest.TestCase):
    def test_ci_runs_python_suites_and_stops_on_either_failure(self):
        ci=(ROOT/'.github/workflows/ci.yml').read_text()
        step=named(ci,'Test API package preservation guards')
        expected=['python3 -I -B tools/release/check-api-package.test.py','python3 -I -B tools/release/api-package-workflows.test.py']
        self.assertEqual(body(step).strip().splitlines(),expected)
        self.assertLess(ci.index(step),ci.index('      - name: Setup .NET SDK'))
        job=ci.split('  backend-prerequisites:',1)[1].split('  backend-tests:',1)[0]
        self.assertNotIn('id-token: write',job)
        self.assertNotIn('environment:',job)
        self.assertNotIn('continue-on-error:',step)
        for first,expected_exit in [(17,17),(0,19)]:
            stubs='python3() { case "$3" in tools/release/check-api-package.test.py) return '+str(first)+';; tools/release/api-package-workflows.test.py) return 19;; *) return 91;; esac; };'
            result=shell(body(step)+'\nprintf BAD_CONTINUATION',stubs)
            self.assertEqual((result.returncode,result.stdout,result.stderr),(expected_exit,'',''))

    def test_all_run_blocks_parse_without_execution(self):
        for name,text in WORKFLOWS.items():
            for block in text.split('        run: |\n')[1:]:
                lines=[]
                for line in block.splitlines():
                    if line and not line.startswith('          '):break
                    lines.append(line[10:] if line else '')
                with self.subTest(name=name,block=len(lines)):
                    result=subprocess.run(['/bin/bash','-n'],input='\n'.join(lines),text=True,capture_output=True)
                    self.assertEqual(result.returncode,0,result.stderr)

    def test_trusted_current_helper_precedes_source_change(self):
        for name,text in WORKFLOWS.items():
            retain=named(text,'Retain current API package guard before source checkout')
            self.assertIn('mkdir "$guard_dir"',retain)
            self.assertNotIn('mkdir -p',retain)
            self.assertIn('cp -n tools/release/check-api-package.py',retain)
            self.assertIn('cmp -s tools/release/check-api-package.py',retain)
            if name=='staging.yml':self.assertLess(text.index(retain),text.index('git checkout --detach'))
            if name=='production.yml':self.assertLess(text.index(retain),text.index('Checkout approved launch candidate artifact source'))
            result=shell(body(retain),'mkdir() { return 17; }; cp() { printf BAD_COPY; }; cmp() { printf BAD_COMPARE; }')
            self.assertEqual((result.returncode,result.stdout,result.stderr),(17,'',''))

    def test_staging_root_failure_stops_before_any_package_creation(self):
        text=WORKFLOWS['staging.yml'];pack=named(text,'Package immutable deployment artifacts')
        self.assertLess(pack.index('--check-tree'),pack.index('zip -X'))
        result=shell(body(pack),'sha256sum() { cat >/dev/null; }; python3() { return 17; }; mkdir() { printf BAD_MKDIR; }; zip() { printf BAD_ZIP; }')
        self.assertEqual((result.returncode,result.stdout,result.stderr),(17,'',''))

    def test_promotion_uses_original_manifest_digest_and_checks_before_unzip(self):
        text=WORKFLOWS['production-release.yml'];step=named(text,'Inspect manifest-selected API ZIP before extraction')
        self.assertIn("jq -r '.artifacts.api.sha256'",step)
        self.assertNotIn('sha256sum "$api_zip"',step)
        self.assertLess(text.index(step),text.index('unzip -q'))
        self.assertIn('unzip -q "$API_PACKAGE_PATH"',text)
        self.assertIn('package: ${{ steps.api_package.outputs.path }}',text)
        result=shell(body(step)+'\nunzip','sha256sum() { cat >/dev/null; }; jq() { case "$2" in .artifacts.api.name) printf fedril-api.zip;; .artifacts.api.sha256) printf "%064d" 0;; esac; }; python3() { return 17; }; unzip() { printf BAD_UNZIP; }')
        self.assertEqual((result.returncode,result.stdout,result.stderr),(17,'',''))

    def test_exclusive_api_extraction_and_immediate_parity_before_other_effects(self):
        text=WORKFLOWS['production-release.yml'];step=named(text,'Extract approved deployment artifacts')
        self.assertIn('mkdir "$RUNNER_TEMP/gccs-api-production"',step)
        self.assertNotIn('mkdir -p "$RUNNER_TEMP/gccs-api-production"',step)
        self.assertLess(step.index('unzip -q "$API_PACKAGE_PATH"'),step.index('--publish-root'))
        self.assertLess(step.index('--publish-root'),step.index('unzip -q "$RUNNER_TEMP/release-bundle/'))
        result=shell(body(step),'mkdir() { return 17; }; unzip() { printf BAD_UNZIP; }')
        self.assertEqual((result.returncode,result.stdout,result.stderr),(17,'',''))
        result=shell(body(step)+'\npsql','mkdir() { return 0; }; unzip() { printf API_UNZIP; }; sha256sum() { cat >/dev/null; }; python3() { return 17; }; psql() { printf BAD_SQL; }')
        self.assertEqual((result.returncode,result.stdout,result.stderr),(17,'API_UNZIP',''))

    def test_legacy_new_exclusive_destination_and_collision_preservation(self):
        step=named(WORKFLOWS['production.yml'],'Package explicitly classified legacy source rebuild')
        self.assertIn('soc2-39.8-api-legacy-${GITHUB_RUN_ID}-${GITHUB_RUN_ATTEMPT}',step)
        self.assertIn('mkdir "$archive_dir"',step)
        self.assertNotIn('mkdir -p',step)
        self.assertIn('legacy-source-rebuild',step)
        self.assertLess(step.index('--check-tree'),step.index('zip -X'))
        result=shell(body(step),'sha256sum() { cat >/dev/null; }; python3() { return 0; }; mkdir() { return 17; }; zip() { printf BAD_ZIP; }')
        self.assertEqual((result.returncode,result.stdout,result.stderr),(17,'',''))

    def test_every_preaction_guard_is_adjacent_and_stops_live_calls_on_failure(self):
        choices=[('staging.yml','Recheck original staging API ZIP immediately before deployment','Deploy staging API App Service'),('production-release.yml','Recheck original promoted API ZIP immediately before deployment','Deploy the staged API artifact'),('production.yml','Recheck legacy rebuild API ZIP immediately before deployment','Deploy production API App Service')]
        for name,guard,deploy in choices:
            text=WORKFLOWS[name];guardstep=named(text,guard)
            self.assertIn('--publish-root',guardstep)
            self.assertTrue(text.split('      - name: '+guard+'\n',1)[1].split('      - name:',1)[1].startswith(' '+deploy+'\n'))
            for failure in ['guard','hash']:
                stubs='sha256sum() { cat >/dev/null; return '+('17' if failure=='hash' else '0')+'; }; python3() { return 17; }; az() { printf BAD_AZURE; };'
                result=shell(body(guardstep)+'\naz',stubs)
                self.assertEqual((result.returncode,result.stdout,result.stderr),(17,'',''))

    def test_previous_runtime_restart_and_pinned_action_contracts_remain(self):
        for name,text in WORKFLOWS.items():
            self.assertIn('azure/webapps-deploy@02a81bead70021f5284939794bcec79c271ab383',text)
            self.assertNotIn('clean: false',text)
            self.assertNotRegex(text,r'(?m)^\s*(?:rm|unlink|rmdir) ')
            if name!='production.yml':self.assertIn('          restart: false',text)
        self.assertIn('package: ${{ runner.temp }}/release-bundle/fedril-api.zip',WORKFLOWS['staging.yml'])

if __name__=='__main__':unittest.main(verbosity=2)
