# One invocation = one real employee session, one company, one kind/action.
[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][string]$PlanPath,
 [Parameter(Mandatory=$true)][string]$ExpectedEmployeeCode,
 [Parameter(Mandatory=$true)][ValidateSet('staff','approvers')][string]$Realm,
 [Parameter(Mandatory=$true)][ValidateSet('sess_nexa_erp_DEMO','sess_nexa_erp')][string]$ConfirmedServerDatabase,
 [Parameter(Mandatory=$true)][string]$EvidenceDirectory,
 [switch]$Apply
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'SetupOperator.psm1') -Force
$base='https://192.168.68.130:8443'
$planBytes=[IO.File]::ReadAllBytes([IO.Path]::GetFullPath($PlanPath))
$plan=[Text.Encoding]::UTF8.GetString($planBytes).TrimStart([char]0xfeff) | ConvertFrom-Json
$definition=Get-SetupDefinition $plan.Kind
Assert-SetupPlan $plan $definition
if ($plan.TargetDatabase -cne $ConfirmedServerDatabase) {throw 'Server database acknowledgement differs from plan.'}
# This acknowledgement does not select or remotely prove a database. Server agent checks service settings.
$hash=Get-SetupSha $planBytes
Write-Host "Plan $hash : $($plan.Kind)/$($plan.Action), $($plan.Company), acknowledged database $ConfirmedServerDatabase"
if (-not $Apply) {Write-Host 'PREVIEW ONLY: no authentication or network. Review JSON, IDs, dates and actor hand-offs. Use -Apply after server target confirmation.';return}
New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null
$evidence=[IO.Path]::GetFullPath($EvidenceDirectory)
$gate=[IO.File]::Open((Join-Path $evidence 'operator.lock'),'OpenOrCreate','ReadWrite','None')
$client=New-SetupHttp;$token=$null
function CurrentEmployee {
 $me=Invoke-SetupJson $client GET ($base+'/api/v1/session/me') $token $plan.Company $null
 Assert-SetupSession $me $ExpectedEmployeeCode $plan.Company
 if ($me.IdentityIssuer -cne ('https://192.168.68.130:8444/realms/'+$Realm)) {throw 'Resolved issuer mismatch.'}
 return $me
}
function SaveEvidence([string]$Path,$Value) {
 # Atomic replacement inside the same evidence directory; never a token or provider response.
 $temp=$Path+'.'+[guid]::NewGuid().ToString('N')+'.tmp'
 [IO.File]::WriteAllText($temp,($Value | ConvertTo-Json -Depth 30),[Text.UTF8Encoding]::new($false))
 if (Test-Path -LiteralPath $Path) {[IO.File]::Replace($temp,$Path,[NullString]::Value)} else {[IO.File]::Move($temp,$Path)}
}
function ReadRow([string]$Id) {
 if ($plan.Kind -eq 'Warehouses') {return Invoke-SetupJson $client GET ($base+$definition.Path+'/'+[uri]::EscapeDataString($Id)) $token $plan.Company $null}
 if ($plan.Kind -eq 'RackBins') {return Invoke-SetupJson $client GET ($base+$definition.Path+'/'+$Id) $token $plan.Company $null}
 $matches=@(Get-SetupRows $client $base $definition.Path $token $plan.Company | Where-Object { [string]$_.Id -eq $Id })
 if ($matches.Count -ne 1) {throw 'Read-back did not resolve exactly one record.'}
 return $matches[0]
}
try {
 $token=Connect-SetupEmployee $client $Realm
 $actor=CurrentEmployee
 Write-Host "Authenticated $($actor.EmployeeCode), $($actor.OrganizationId); authority comes from API, not these arguments."
 if ($plan.Action -eq 'Read') {
  $rows=@(Get-SetupRows $client $base $definition.Path $token $plan.Company)
  SaveEvidence (Join-Path $evidence ($plan.Kind+'-'+$plan.Company+'-read.json')) @{AtUtc=[DateTime]::UtcNow.ToString('o');EmployeeId=$actor.EmployeeId;Company=$plan.Company;Kind=$plan.Kind;Rows=$rows}
  Write-Host "Read $($rows.Count) records; no mutation.";return
 }
 if ($plan.Action -eq 'Template') {
  $bytes=Invoke-SetupHttp $client GET ($base+'/api/v1/master-data/'+$definition.Master+'/template') $token $plan.Company $null
  $path=Join-Path $evidence ($definition.Master+'-'+$plan.Company+'-template.xlsx')
  $file=[IO.File]::Open($path,'CreateNew','Write','None');try {$file.Write($bytes,0,$bytes.Length)} finally {$file.Dispose()}
  Write-Host "Template saved: $path";return
 }
 foreach ($row in $plan.Rows) {
  $actor=CurrentEmployee
  $operation=[guid]::Parse($row.OperationId).ToString()
  $journalPath=Join-Path $evidence ($operation+'.json')
  $workbookBytes=$null;$workbookHash=$null
  if ($plan.Action -eq 'Import') {
   $workbook=[IO.Path]::GetFullPath((Join-Path (Split-Path -Parent ([IO.Path]::GetFullPath($PlanPath))) $row.Workbook))
   if ([IO.Path]::GetExtension($workbook) -ine '.xlsx') {throw 'Only API-template .xlsx imports.'}
   $workbookBytes=[IO.File]::ReadAllBytes($workbook);$workbookHash=Get-SetupSha $workbookBytes
  }
  if (Test-Path -LiteralPath $journalPath) {
   $prior=Get-Content -LiteralPath $journalPath -Raw | ConvertFrom-Json
   if ($prior.PlanSha256 -cne $hash -or $prior.EmployeeId -ne $actor.EmployeeId -or $prior.WorkbookSha256 -cne $workbookHash) {throw 'Operation ID reused with changed plan, workbook or employee.'}
   if ($prior.Phase -cne 'VERIFIED') {throw 'Uncertain prior write: use Read and reconcile the retained pending receipt; never delete it or blindly retry.'}
   if ($plan.Action -eq 'Import') {
    $check=Invoke-SetupJson $client GET ($base+'/api/v1/master-data/imports/'+$prior.ResultId) $token $plan.Company $null
    if ($check.Status -cne 'COMPLETED' -or $check.InvalidRows -ne 0 -or $check.RejectedRows -ne 0) {throw 'Import replay read-back not complete.'}
   } else {Assert-SetupReadback $plan $row (ReadRow $prior.ResultId)}
   Write-Host "Already verified $operation; read-only replay.";continue
  }
  if ($plan.Kind -eq 'Scopes' -and $plan.Action -eq 'Create' -and $row.Body.WarehouseCode) {
   $warehouse=Invoke-SetupJson $client GET ($base+'/api/v1/inventory/warehouses/'+[uri]::EscapeDataString($row.Body.WarehouseCode)) $token $plan.Company $null
   if (-not $warehouse.IsActive -or $warehouse.WarehouseCode -cne $row.Body.WarehouseCode) {throw 'Requested scoped warehouse is not active/exact; refusing broader scope.'}
  }
  $journal=[ordered]@{OperationId=$operation;PlanSha256=$hash;WorkbookSha256=$workbookHash;EmployeeId=$actor.EmployeeId;EmployeeCode=$actor.EmployeeCode;Company=$plan.Company;AcknowledgedDatabase=$ConfirmedServerDatabase;Kind=$plan.Kind;Action=$plan.Action;Phase='PENDING';AtUtc=[DateTime]::UtcNow.ToString('o');ResultId=$null;Readback=$null}
  # Before sending, persist a pending intent. Any ambiguity fails closed, including non-idempotent creates.
  SaveEvidence $journalPath $journal
  if ($plan.Action -eq 'Import') {
   $content=[Net.Http.MultipartFormDataContent]::new()
   $content.Add([Net.Http.ByteArrayContent]::new($workbookBytes),'File',[IO.Path]::GetFileName($workbook))
   $content.Add([Net.Http.StringContent]::new('REJECT_ENTIRE_FILE'),'Mode')
   $content.Add([Net.Http.StringContent]::new($operation),'IdempotencyKey')
   $bytes=Invoke-SetupHttp $client POST ($base+'/api/v1/master-data/'+$definition.Master+'/import') $token $plan.Company $null '' $content
   $result=[Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json
   $journal.ResultId=[string]$result.BatchId;SaveEvidence $journalPath $journal
   $readback=Invoke-SetupJson $client GET ($base+'/api/v1/master-data/imports/'+$result.BatchId) $token $plan.Company $null
   $journal.Readback=$readback;SaveEvidence $journalPath $journal
   if ($readback.Status -cne 'COMPLETED' -or $readback.InvalidRows -ne 0 -or $readback.RejectedRows -ne 0 -or $readback.NotImportedRows -ne 0) {throw 'Workbook import not wholly successful; retained batch read-back requires review.'}
   foreach ($importRow in $readback.Rows) {if ($importRow.ResultRecordId) {$null=ReadRow $(if ($plan.Kind -eq 'Warehouses') {$importRow.BusinessCode} else {[string]$importRow.ResultRecordId})}}
  } else {
   $body=$row.Body;$path=$definition.Path
   if ($plan.Action -ne 'Create') {
    $path+='/'+[uri]::EscapeDataString($row.RecordId)+'/'+$plan.Action.ToLowerInvariant()
    if ($plan.Kind -eq 'TaxRules') {$body | Add-Member -NotePropertyName IdempotencyKey -NotePropertyValue $operation}
   }
   $result=Invoke-SetupJson $client POST ($base+$path) $token $plan.Company $body $operation
   if ($plan.Kind -in @('Warehouses','RackBins')) {$id=[string]$row.RecordId} else {$id=[string]$result.Id}
   if (-not $id) {throw 'Write returned no record ID; reconcile before retry.'}
   $journal.ResultId=$id;SaveEvidence $journalPath $journal
   $readback=ReadRow $id
   $journal.Readback=$readback;SaveEvidence $journalPath $journal
   Assert-SetupReadback $plan $row $readback
  }
  $journal.Phase='VERIFIED';$journal.AtUtc=[DateTime]::UtcNow.ToString('o');SaveEvidence $journalPath $journal
  Write-Host "VERIFIED $operation; record $($journal.ResultId). Inspect effective dates and next required approval."
 }
} finally {$token=$null;$client.Dispose();$gate.Dispose()}
