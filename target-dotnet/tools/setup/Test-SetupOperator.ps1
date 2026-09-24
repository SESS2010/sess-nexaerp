$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'SetupOperator.psm1') -Force
$count=0
function Check($Name,[scriptblock]$Test) {& $Test;$script:count++;Write-Output "PASS $Name"}
function Refuses([scriptblock]$Code) {$failed=$false;try {& $Code | Out-Null} catch {$failed=$true};if (-not $failed) {throw 'Expected refusal.'}}
function Same($Actual,$Expected) {if ($Actual -cne $Expected) {throw "Expected $Expected; got $Actual"}}
function Plan {return ('{"Kind":"ConditionLocations","Action":"Create","Company":"SESS_PVT_LTD","TargetDatabase":"sess_nexa_erp_DEMO","Rows":[{"OperationId":"2b9935c7-47d2-43da-964b-a01d6059471a","Body":{"OrganizationId":"SESS_PVT_LTD","WarehouseCode":"MAIN","RackBinId":"46219b67-99b1-49f6-bfbc-5fb3ff6d4b7e","ConditionCode":"AVAILABLE","EffectiveFrom":"2026-09-28","EffectiveTo":null,"Remarks":"Approved setup"}}]}' | ConvertFrom-Json)}
Check 'RFC7636 S256 vector' {Same (Get-SetupChallenge 'dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk') 'E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM'}
Check 'Fresh entropy' {if ((New-SetupRandom) -ceq (New-SetupRandom)) {throw 'Repeated entropy'}}
Check 'Valid callback' {Same (Read-SetupCallback 'GET /callback/?state=expected&code=abc&iss=https%3A%2F%2Fissuer HTTP/1.1' expected 'https://issuer') abc}
foreach ($request in @('GET /callback/?code=abc HTTP/1.1','GET /callback/?state=wrong&code=abc HTTP/1.1','GET /callback/?state=expected&state=expected&code=abc HTTP/1.1','GET /callback/?state=expected&code=abc&code=def HTTP/1.1','GET /callback/?state=expected&code=abc&iss=https%3A%2F%2Fevil HTTP/1.1','GET /callback/?state=expected&error=denied HTTP/1.1','POST /callback/?state=expected&code=abc HTTP/1.1','GET /other/?state=expected&code=abc HTTP/1.1')) {Check 'Reject invalid callback' {Refuses {Read-SetupCallback $request expected 'https://issuer'}}}
$me=[pscustomobject]@{EmployeeCode='SESS-41';OrganizationId='SESS_PVT_LTD';EmployeeId=[guid]::NewGuid()}
Check 'Resolved session' {Assert-SetupSession $me SESS-41 SESS_PVT_LTD}
Check 'Cannot select another employee' {Refuses {Assert-SetupSession $me SESS-01 SESS_PVT_LTD}}
Check 'Cannot select another company' {Refuses {Assert-SetupSession $me SESS-41 SESS_PROPRIETORSHIP}}
Check 'Valid plan' {$p=Plan;Assert-SetupPlan $p (Get-SetupDefinition $p.Kind)}
Check 'Reject injected actor' {$p=Plan;$p.Rows[0].Body | Add-Member RoleCode TECHNICAL_DIRECTOR;Refuses {Assert-SetupPlan $p (Get-SetupDefinition $p.Kind)}}
Check 'Reject foreign body scope' {$p=Plan;$p.Rows[0].Body.OrganizationId='SESS_PROPRIETORSHIP';Refuses {Assert-SetupPlan $p (Get-SetupDefinition $p.Kind)}}
Check 'Reject stock kind' {Refuses {Get-SetupDefinition 'GoodsReceipts'}}
Check 'Reject unavailable approval route' {$p=Plan;$p.Action='Approve';Refuses {Assert-SetupPlan $p (Get-SetupDefinition $p.Kind)}}
Check 'Reject duplicate operation' {$p=Plan;$p.Rows+=@($p.Rows[0]);Refuses {Assert-SetupPlan $p (Get-SetupDefinition $p.Kind)}}
Check 'Reject unknown database' {$p=Plan;$p.TargetDatabase='postgres';Refuses {Assert-SetupPlan $p (Get-SetupDefinition $p.Kind)}}
Check 'Read-back payload equality' {$p=Plan;$actual=$p.Rows[0].Body | ConvertTo-Json | ConvertFrom-Json;Assert-SetupReadback $p $p.Rows[0] $actual}
Check 'Reject read-back mismatch' {$p=Plan;$actual=$p.Rows[0].Body | ConvertTo-Json | ConvertFrom-Json;$actual.ConditionCode='QC_HOLD';Refuses {Assert-SetupReadback $p $p.Rows[0] $actual}}
Check 'Reject missing record' {$p=Plan;Refuses {Assert-SetupReadback $p $p.Rows[0] $null}}
Check 'Read-back approval and version' {$p=Plan;$p.Kind='TaxRules';$p.Action='Approve';$r=[pscustomobject]@{Body=[pscustomobject]@{ExpectedVersion=2;Remarks='Approved'}};Assert-SetupReadback $p $r ([pscustomobject]@{ApprovalStatus='Approved';Version=3});Refuses {Assert-SetupReadback $p $r ([pscustomobject]@{ApprovalStatus='Approved';Version=2})}}
Add-Type -ReferencedAssemblies System.Net.Http -TypeDefinition @"
using System; using System.Net; using System.Net.Http; using System.Threading; using System.Threading.Tasks;
public sealed class SetupFakeHandler : HttpMessageHandler {
 public string[] Bodies; public int Status=200; public int Calls; public string Authorization; public string Company;
 protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken t) {
  Authorization=r.Headers.Authorization == null ? null : r.Headers.Authorization.ToString();
  Company=r.Headers.Contains("X-NexaERP-Company") ? string.Join("",r.Headers.GetValues("X-NexaERP-Company")) : null;
  var response=new HttpResponseMessage((HttpStatusCode)Status); response.Content=new StringContent(Bodies[Math.Min(Calls++,Bodies.Length-1)]);
  return Task.FromResult(response);
 }
}
"@
$handler=[SetupFakeHandler]::new();$client=[Net.Http.HttpClient]::new($handler)
try {
 Check 'Pagination reads all pages' {$handler.Bodies=@('{"Items":[{"Id":1}],"TotalCount":2}','{"Items":[{"Id":2}],"TotalCount":2}');$handler.Calls=0;$rows=@(Get-SetupRows $client 'https://unit.invalid' '/read' test-token SESS_PVT_LTD);Same $rows.Count 2;Same $handler.Company SESS_PVT_LTD;Same $handler.Authorization 'Bearer test-token'}
 Check 'Nonpaged locations' {$handler.Bodies=@('[{"Id":1},{"Id":2}]');$handler.Calls=0;Same @(Get-SetupRows $client 'https://unit.invalid' '/read' test-token SESS_PVT_LTD).Count 2}
 foreach ($status in @(302,401,403,409,500)) {Check 'HTTP failures never auto-retry' {$handler.Status=$status;$handler.Calls=0;$handler.Bodies=@('{"access_token":"DO-NOT-PRINT"}');$message='';try {Invoke-SetupJson $client POST 'https://unit.invalid/write' test-token SESS_PVT_LTD @{Remarks='test'} | Out-Null} catch {$message=$_.Exception.Message};if (-not $message -or $message.Contains('DO-NOT-PRINT')) {throw 'Unsafe error'};Same $handler.Calls 1}}
 # An operator on 1-3 October must be told WHICH condition location is missing or WHICH date is
 # wrong. "HTTP 409" alone turns every refusal into an escalation during the worst possible week.
 $refusal='{"Type":"https://api.sess.example/problems/business-rule-conflict","Title":"Business rule conflict","Status":409,"Code":"BUSINESS_RULE_CONFLICT","Detail":"Route requires effective same-company QC_HOLD, PENDING_RETURNABLE_DC and AVAILABLE condition locations.","TraceId":"trace-1","Errors":{}}'
 foreach ($status in @(400,404,409,422)) {Check 'Business refusal prints the server reason' {$handler.Status=$status;$handler.Calls=0;$handler.Bodies=@($refusal);$message='';try {Invoke-SetupJson $client POST 'https://unit.invalid/write' test-token SESS_PVT_LTD @{Remarks='test'} | Out-Null} catch {$message=$_.Exception.Message};if (-not $message.Contains('PENDING_RETURNABLE_DC')) {throw "Refusal reason withheld: $message"};if (-not $message.Contains("HTTP $status")) {throw 'Wrapper line lost'}}}
 # An authority answer is not the operator's to act on, and a 5xx detail is suppressed by design.
 foreach ($status in @(401,403,500,503)) {Check 'Authority and server errors stay opaque' {$handler.Status=$status;$handler.Calls=0;$handler.Bodies=@($refusal);$message='';try {Invoke-SetupJson $client POST 'https://unit.invalid/write' test-token SESS_PVT_LTD @{Remarks='test'} | Out-Null} catch {$message=$_.Exception.Message};if ($message.Contains('PENDING_RETURNABLE_DC')) {throw "Detail echoed for $status"}}}
 # Identity-provider responses are never echoed, whatever status they carry.
 foreach ($status in @(400,409)) {Check 'Provider refusals are never echoed' {$handler.Status=$status;$handler.Calls=0;$handler.Bodies=@($refusal);$message='';try {Invoke-SetupJson $client POST 'https://unit.invalid/token' '' '' $null '' -Provider | Out-Null} catch {$message=$_.Exception.Message};if ($message.Contains('PENDING_RETURNABLE_DC')) {throw 'Provider detail echoed'}}}
 Check 'A refusal body without a named field prints nothing extra' {$handler.Status=409;$handler.Calls=0;$handler.Bodies=@('{"access_token":"DO-NOT-PRINT","secret":"DO-NOT-PRINT"}');$message='';try {Invoke-SetupJson $client POST 'https://unit.invalid/write' test-token SESS_PVT_LTD @{Remarks='test'} | Out-Null} catch {$message=$_.Exception.Message};if ($message.Contains('DO-NOT-PRINT')) {throw 'Unnamed body content echoed'};if ($message.Contains('Server said')) {throw 'Empty reason line printed'}}
 Check 'A non-JSON refusal body prints nothing extra' {$handler.Status=409;$handler.Calls=0;$handler.Bodies=@('<html>DO-NOT-PRINT</html>');$message='';try {Invoke-SetupJson $client POST 'https://unit.invalid/write' test-token SESS_PVT_LTD @{Remarks='test'} | Out-Null} catch {$message=$_.Exception.Message};if ($message.Contains('DO-NOT-PRINT')) {throw 'Raw body echoed'}}
} finally {$client.Dispose()}
Write-Output "PASSED $count setup operator checks; live Keycloak/DEMO witness not implied."
