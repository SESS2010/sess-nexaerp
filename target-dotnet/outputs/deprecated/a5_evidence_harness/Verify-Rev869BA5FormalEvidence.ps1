[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][string]$JournalPath,
 [Parameter(Mandatory=$true)][string]$PlanPath,
 [Parameter(Mandatory=$true)][string]$SchemaPath,
 [Parameter(Mandatory=$true)][string]$AuthorizationRecordPath,
 [Parameter(Mandatory=$true)][string]$AuthorizationRecordSha256,
 [Parameter(Mandatory=$true)][string]$CandidateWorktree,
 [Parameter(Mandatory=$true)][ValidateSet('A5_ACCEPTANCE','HARNESS_VALIDATION')][string]$ExpectedPurpose,
 [Parameter(Mandatory=$true)][Guid]$ExpectedRunId,
 [Parameter(Mandatory=$true)][string]$DetachedResultPath,
 [string]$CalculatedCheckpointPath
)
Set-StrictMode -Version 3.0
$ErrorActionPreference='Stop'
$zero='0'*64
$required=@('schemaVersion','runId','sequence','eventType','mode','purpose','timestampUtc','timestampLocal',
 'timezoneId','utcOffsetMinutes','monotonicTicks','monotonicFrequency','candidate','command','tool','timing',
 'result','stdout','stderr','trx','mutant','counters','previousEventSha256','currentEventSha256')
function Get-Sha256([string]$Path){
 if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){Fail 73 'EVIDENCE_FILE_MISSING'}
 (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToUpperInvariant()
}
function Get-TextSha256([string]$Text){
 $s=[Security.Cryptography.SHA256]::Create()
 try{([BitConverter]::ToString($s.ComputeHash([Text.Encoding]::UTF8.GetBytes($Text)))).Replace('-','')}
 finally{$s.Dispose()}
}
function Canon($Value){$Value|ConvertTo-Json -Depth 30 -Compress}
function Fail([int]$Code,[string]$Message){[Console]::Error.WriteLine($Message);exit $Code}
function Git([string[]]$ArgumentList,[string]$WorkingDirectory){
 $resolved=(Resolve-Path -LiteralPath $WorkingDirectory).Path
 $v=& git.exe -C $resolved @ArgumentList 2>$null
 if($LASTEXITCODE -ne 0){Fail 72 'GIT_IDENTITY_FAILURE'}
 ([string]($v -join [Environment]::NewLine)).Trim()
}
function Check-Evidence($Entry,[string]$RunRoot){
 if($null -eq $Entry){return}
 $p=[IO.Path]::GetFullPath((Join-Path $RunRoot ([string]$Entry.path)))
 if(-not $p.StartsWith($RunRoot,[StringComparison]::OrdinalIgnoreCase)){Fail 73 'EVIDENCE_PATH_ESCAPE'}
 $i=Get-Item -LiteralPath $p -ErrorAction SilentlyContinue
 if($null -eq $i -or $i.Length -ne [int64]$Entry.size -or (Get-Sha256 $p) -cne [string]$Entry.sha256){
  Fail 73 'EVIDENCE_HASH_MISMATCH'
 }
}
if(Test-Path -LiteralPath $DetachedResultPath){Fail 79 'DETACHED_RESULT_ALREADY_EXISTS'}
if((Get-Sha256 $AuthorizationRecordPath) -cne $AuthorizationRecordSha256.ToUpperInvariant()){
 Fail 72 'AUTHORIZATION_HASH_MISMATCH'
}
$auth=Get-Content -LiteralPath $AuthorizationRecordPath -Raw|ConvertFrom-Json
$plan=Get-Content -LiteralPath $PlanPath -Raw|ConvertFrom-Json
$schema=Get-Content -LiteralPath $SchemaPath -Raw|ConvertFrom-Json
if($schema.title -cne 'REV869B A5 formal evidence event'){Fail 70 'SCHEMA_IDENTITY_MISMATCH'}
$runRoot=[IO.Path]::GetFullPath((Split-Path -Parent (Resolve-Path -LiteralPath $JournalPath).Path))
$lines=@(Get-Content -LiteralPath $JournalPath|Where-Object{$_ -ne ''})
if($lines.Count -eq 0){Fail 70 'EMPTY_JOURNAL'}
$events=New-Object Collections.Generic.List[object]
$previous=$zero;$expected=1L;$markerCount=0;$failed=$false;$markerTicks=$null
foreach($line in $lines){
 try{$e=$line|ConvertFrom-Json}catch{Fail 70 'MALFORMED_JSON'}
 $names=@($e.PSObject.Properties.Name)
 if($names.Count -ne $required.Count){Fail 70 'EVENT_PROPERTY_COUNT'}
 for($n=0;$n -lt $required.Count;$n++){if($names[$n] -cne $required[$n]){Fail 70 'EVENT_PROPERTY_ORDER'}}
 if($e.schemaVersion -cne 'rev869b.a5.formal-evidence/1'){Fail 70 'SCHEMA_VERSION'}
 if([int64]$e.sequence -ne $expected){Fail 70 'SEQUENCE_DISCONTINUITY'}
 if([string]$e.runId -cne $ExpectedRunId.ToString('D').ToLowerInvariant()){Fail 72 'RUN_ID_SUBSTITUTION'}
 if($e.mode -cne 'FORMAL_ACCEPTANCE' -or $e.purpose -cne $ExpectedPurpose){Fail 75 'MODE_OR_PURPOSE_RELABEL'}
 if($failed){Fail 74 'EVENT_AFTER_FAILURE'}
 if($e.eventType -eq 'FORMAL_ACCEPTANCE_FAILED'){$failed=$true}
 if($e.eventType -eq 'FORMAL_ACCEPTANCE_GATE_STARTED'){
  $markerCount++;$markerTicks=[int64]$e.monotonicTicks
 }
 if($e.eventType -like 'COMMAND_*' -and $null -eq $markerTicks){Fail 70 'COMMAND_BEFORE_MARKER'}
 if($null -ne $markerTicks -and $e.eventType -like 'COMMAND_*' -and [int64]$e.monotonicTicks -lt $markerTicks){
  Fail 70 'COMMAND_TIMESTAMP_BEFORE_MARKER'
 }
 if([string]$e.previousEventSha256 -cne $previous){Fail 71 'PREVIOUS_HASH_MISMATCH'}
 $actual=[string]$e.currentEventSha256;$e.currentEventSha256=$zero
 $calculated=Get-TextSha256 (Canon $e)
 if($actual -cne $calculated){Fail 71 'CURRENT_HASH_MISMATCH'}
 $e.currentEventSha256=$actual;$previous=$actual;$expected++
 $events.Add($e)
}
if($markerCount -ne 1){Fail 70 'FORMAL_MARKER_CARDINALITY'}
$first=$events[0].candidate
$candidateCanon=Canon $first
foreach($e in $events){if((Canon $e.candidate) -cne $candidateCanon){Fail 72 'CANDIDATE_EVENT_SUBSTITUTION'}}
if($first.commit -cne ([string]$auth.candidateCommit).ToLowerInvariant() -or
   $first.parent -cne ([string]$auth.authorizedStartingCommit).ToLowerInvariant() -or
   $first.tree -cne ([string]$auth.candidateTree).ToLowerInvariant()){Fail 72 'CANDIDATE_AUTHORIZATION_MISMATCH'}
$work=(Resolve-Path -LiteralPath $CandidateWorktree).Path
if((Git @('rev-parse','HEAD') $work).ToLowerInvariant() -cne $first.commit){Fail 72 'WORKTREE_COMMIT_SUBSTITUTION'}
if((Git @('rev-parse','HEAD^{tree}') $work).ToLowerInvariant() -cne $first.tree){Fail 72 'WORKTREE_TREE_SUBSTITUTION'}
$status=Git @('status','--porcelain=v1','--untracked-files=all','--','.') $work
if($status){Fail 76 'CANDIDATE_CHANGED_AFTER_FREEZE'}
$manifest=Join-Path $runRoot 'candidate-manifest.json'
if((Get-Sha256 $manifest) -cne $first.manifestSha256){Fail 72 'MANIFEST_SUBSTITUTION'}
$started=@($events|Where-Object{$_.eventType -eq 'COMMAND_STARTED'})
$completed=@($events|Where-Object{$_.eventType -eq 'COMMAND_COMPLETED'})
if($started.Count -ne $completed.Count){Fail 70 'COMMAND_PAIR_COUNT'}
for($i=0;$i -lt $completed.Count;$i++){
 $s=$started[$i];$c=$completed[$i]
 if($s.command.planId -cne $c.command.planId){Fail 70 'COMMAND_PAIR_ID'}
 if([DateTimeOffset]::Parse($c.timing.endUtc) -lt [DateTimeOffset]::Parse($c.timing.startUtc)){Fail 70 'TIMESTAMP_REVERSAL'}
 if([int64]$c.timing.endTicks -lt [int64]$c.timing.startTicks){Fail 70 'MONOTONIC_REVERSAL'}
 if($c.result.exitCode -ne $c.result.expected){Fail 74 'COMMAND_RESULT_FAILURE'}
 Check-Evidence $c.stdout $runRoot;Check-Evidence $c.stderr $runRoot;Check-Evidence $c.trx $runRoot
}
$expectedCommands=@($auth.commands)
if($completed.Count -ne $expectedCommands.Count){Fail 70 'COMMAND_INVENTORY_COUNT'}
for($i=0;$i -lt $completed.Count;$i++){
 if($completed[$i].command.planId -cne [string]$expectedCommands[$i].planId -or
    [int]$completed[$i].command.stage -ne [int]$expectedCommands[$i].stage -or
    [int]$completed[$i].command.subordinal -ne [int]$expectedCommands[$i].subordinal){
  Fail 70 'COMMAND_INVENTORY_ORDER'
 }
}
if($ExpectedPurpose -eq 'A5_ACCEPTANCE'){
 $seen=New-Object Collections.Generic.HashSet[int]
 foreach($e in $completed){
  $stage=[int]$e.command.stage
  if($stage -lt 1 -or $stage -gt 16){Fail 70 'FORMAL_STAGE_OUT_OF_RANGE'}
  $frozen=@($plan.stages|Where-Object{[int]$_.ordinal -eq $stage})[0]
  if([string]$e.command.planId -cne [string]$frozen.id){Fail 70 'FORMAL_PLAN_ID_MISMATCH'}
  $null=$seen.Add($stage)
 }
 if($seen.Count -ne 16){Fail 70 'FORMAL_STAGE_COVERAGE'}
}
$mutants=@($events|Where-Object{$_.eventType -eq 'MUTANT_COMPLETED'})
foreach($e in $mutants){
 if(-not $e.mutant.compiled -or -not $e.mutant.killed -or -not $e.mutant.restorationEquality -or
    $e.mutant.originalBlob -cne $e.mutant.restoredBlob -or
    $e.mutant.originalSha256 -cne $e.mutant.restoredSha256){Fail 77 'MUTANT_RESTORATION_OR_RESULT'}
}
if($ExpectedPurpose -eq 'A5_ACCEPTANCE'){
 if($mutants.Count -ne 40){Fail 77 'MUTANT_ARITHMETIC'}
 $ids=@($mutants|ForEach-Object{$_.mutant.id}|Sort-Object -Unique)
 if($ids.Count -ne 40){Fail 77 'MUTANT_ID_DUPLICATION'}
}
$counterEvents=@($events|Where-Object{$_.eventType -eq 'COUNTERS_OBSERVED'})
if($counterEvents.Count -ne 1){Fail 78 'COUNTER_EVENT_CARDINALITY'}
$c=$counterEvents[0].counters
foreach($name in @('postgresqlConnections','postgresqlTestsExecuted','migrationApplications','migrationRemovals',
 'serviceStarts','productionAccess','externalDeployments','networkRequests')){
 if([int64]$c.$name -ne 0){Fail 78 ('NONZERO_COUNTER_'+$name)}
}
$instrument=Join-Path $runRoot 'instrumentation.json'
if((Get-Sha256 $instrument) -cne $c.evidenceSha256){Fail 78 'COUNTER_EVIDENCE_HASH'}
$trxIds=New-Object Collections.Generic.HashSet[string]
foreach($e in $completed){
 if($null -ne $e.trx){
  $tp=[IO.Path]::GetFullPath((Join-Path $runRoot ([string]$e.trx.path)))
  try{[xml]$xml=Get-Content -LiteralPath $tp -Raw}catch{Fail 73 'RESULT_PARSE_FAILURE'}
  foreach($unit in @($xml.SelectNodes('//*[local-name()="UnitTestResult"]'))){
   $id=[string]$unit.testId
   if($id -and -not $trxIds.Add($id)){Fail 70 'DUPLICATE_TEST_IDENTITY'}
  }
 }
}
if($failed){
 if($events[$events.Count-1].eventType -cne 'FORMAL_ACCEPTANCE_FAILED'){Fail 74 'FAILURE_NOT_FINAL'}
 Fail 74 'FORMAL_FAILURE_RETAINED'
}
if($events[$events.Count-1].eventType -cne 'FORMAL_EXECUTION_COMPLETED'){Fail 70 'COMPLETION_EVENT_MISSING'}
$self=(Get-Item -LiteralPath $MyInvocation.MyCommand.Path).FullName
$result=[ordered]@{schemaVersion='rev869b.a5.detached-verification/1';state='PASS_CALCULATED';
 runId=$ExpectedRunId.ToString('D').ToLowerInvariant();purpose=$ExpectedPurpose;candidateCommit=$first.commit;
 candidateTree=$first.tree;manifestSha256=$first.manifestSha256;journalHeadSha256=$previous;
 verifiedEvents=$events.Count;verifiedCommands=$completed.Count;uniqueTestIdentities=$trxIds.Count;
 verifiedMutants=$mutants.Count;verifierPath=$self;verifierSha256=Get-Sha256 $self;
 timestampUtc=[DateTimeOffset]::UtcNow.ToString('O')}
$bytes=[Text.Encoding]::UTF8.GetBytes((Canon $result))
$stream=New-Object IO.FileStream($DetachedResultPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
try{$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)}finally{$stream.Dispose()}
if($ExpectedPurpose -eq 'A5_ACCEPTANCE'){
 if(-not $CalculatedCheckpointPath){Fail 79 'CALCULATED_CHECKPOINT_PATH_REQUIRED'}
 $body=@(
  '# REV869B A5 independently calculated formal acceptance checkpoint','',
  ('run_id='+$result.runId),('candidate_commit='+$result.candidateCommit),('candidate_tree='+$result.candidateTree),
  ('candidate_manifest_sha256='+$result.manifestSha256),('journal_head_sha256='+$result.journalHeadSha256),
  ('verified_events='+$result.verifiedEvents),('verified_commands='+$result.verifiedCommands),
  ('verified_mutants='+$result.verifiedMutants),'A5_FORMAL_ACCEPTANCE_EVIDENCE=PASS_CALCULATED') -join [Environment]::NewLine
 [IO.File]::WriteAllText($CalculatedCheckpointPath,$body,(New-Object Text.UTF8Encoding($false)))
}
$result|ConvertTo-Json -Depth 8
exit 0
