"""Exercise the production runner against in-process PowerShell API doubles; no network/DB."""
from pathlib import Path
import json, subprocess, tempfile, unittest
ROOT=Path(__file__).resolve().parents[2]
class RunnerSafety(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory(); self.root=Path(self.tmp.name)
  self.plan={'Kind':'ConditionLocations','Action':'Create','Company':'SESS_PVT_LTD','TargetDatabase':'sess_nexa_erp_DEMO','Rows':[{'OperationId':'2b9935c7-47d2-43da-964b-a01d6059471a','Body':{'OrganizationId':'SESS_PVT_LTD','WarehouseCode':'MAIN','RackBinId':'46219b67-99b1-49f6-bfbc-5fb3ff6d4b7e','ConditionCode':'AVAILABLE','EffectiveFrom':'2026-09-28','EffectiveTo':None,'Remarks':'Approved'}}]}
  self.path=self.root/'plan.json'; self.path.write_text(json.dumps(self.plan))
 def tearDown(self):self.tmp.cleanup()
 def run_case(self, failure='', employee='SESS-41'):
  source=(ROOT/'tools/setup/Invoke-Setup.ps1').read_text(encoding='utf-8-sig')
  source=source.replace("Import-Module (Join-Path $PSScriptRoot 'SetupOperator.psm1') -Force", "Import-Module '"+str(ROOT/'tools/setup/SetupOperator.psm1').replace("'","''")+"' -Force")
  mock=r'''
function New-SetupHttp { $c=[pscustomobject]@{};$c | Add-Member -MemberType ScriptMethod -Name Dispose -Value {};return $c }
function Connect-SetupEmployee {return 'TEST-ONLY'}
function Invoke-SetupJson($Client,$Method,$Uri,$Token,$Company,$Body,$Key) {
 if ($Uri.EndsWith('/session/me')) {return [pscustomobject]@{EmployeeId='661bb907-2ffb-4625-bcac-2fd152450acb';EmployeeCode='SESS-41';OrganizationId='SESS_PVT_LTD';IdentityIssuer='https://192.168.68.130:8444/realms/staff'}}
 if ($Method -eq 'POST') {
  $counter=Join-Path $PSScriptRoot 'post-count';$n=0;if (Test-Path $counter) {$n=[int](Get-Content $counter)};[IO.File]::WriteAllText($counter,[string]($n+1))
  if ('FAILURE' -eq 'post') {throw 'Simulated lost response'}
  return [pscustomobject]@{Id='6cfd45fd-210c-41e9-b363-63bc0a362e96'}
 }
 throw 'Unexpected request in test'
}
function Get-SetupRows {
 $row=$plan.Rows[0].Body | ConvertTo-Json | ConvertFrom-Json
 $row | Add-Member Id '6cfd45fd-210c-41e9-b363-63bc0a362e96'
 if ('FAILURE' -eq 'readback') {$row.ConditionCode='QC_HOLD'}
 return $row
}
'''.replace('FAILURE',failure)
  source=source.replace("$base='https://192.168.68.130:8443'",mock+"\n$base='https://192.168.68.130:8443'")
  runner=self.root/'runner.ps1';runner.write_text(source,encoding='utf-8-sig')
  return subprocess.run(['powershell','-NoProfile','-File',str(runner),'-PlanPath',str(self.path),'-ExpectedEmployeeCode',employee,'-Realm','staff','-ConfirmedServerDatabase','sess_nexa_erp_DEMO','-EvidenceDirectory',str(self.root/'evidence'),'-Apply'],capture_output=True,text=True)
 def posts(self):
  p=self.root/'post-count';return int(p.read_text()) if p.exists() else 0
 def test_success_and_readonly_replay(self):
  r=self.run_case();self.assertEqual(r.returncode,0,r.stdout+r.stderr)
  r=self.run_case();self.assertEqual(r.returncode,0,r.stdout+r.stderr);self.assertEqual(self.posts(),1)
 def test_changed_input_refuses_reuse(self):
  self.assertEqual(self.run_case().returncode,0)
  self.plan['Rows'][0]['Body']['Remarks']='Changed';self.path.write_text(json.dumps(self.plan))
  self.assertNotEqual(self.run_case().returncode,0);self.assertEqual(self.posts(),1)
 def test_uncertain_post_refuses_retry(self):
  self.assertNotEqual(self.run_case('post').returncode,0)
  self.assertNotEqual(self.run_case().returncode,0);self.assertEqual(self.posts(),1)
 def test_failed_readback_refuses_retry(self):
  self.assertNotEqual(self.run_case('readback').returncode,0)
  self.assertNotEqual(self.run_case().returncode,0);self.assertEqual(self.posts(),1)
 def test_employee_mismatch_before_write(self):
  self.assertNotEqual(self.run_case(employee='SESS-01').returncode,0);self.assertEqual(self.posts(),0)
if __name__=='__main__':unittest.main()
