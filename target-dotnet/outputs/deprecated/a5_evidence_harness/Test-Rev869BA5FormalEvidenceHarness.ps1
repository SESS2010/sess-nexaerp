[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$EvidenceRoot)
Set-StrictMode -Version 3.0
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$runner=Join-Path $root 'Invoke-Rev869BA5FormalAcceptance.ps1'
$verifier=Join-Path $root 'Verify-Rev869BA5FormalEvidence.ps1'
$plan=Join-Path $root 'Rev869BA5FormalPlan.v1.json'
$schema=Join-Path $root 'Rev869BA5FormalEvidence.v1.schema.json'
$powershell=(Get-Command powershell.exe -CommandType Application).Source
$utf8=New-Object Text.UTF8Encoding($false)
$zero='0'*64
function Sha([string]$Path){(Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToUpperInvariant()}
function TextSha([string]$Text){
 $s=[Security.Cryptography.SHA256]::Create()
 try{([BitConverter]::ToString($s.ComputeHash([Text.Encoding]::UTF8.GetBytes($Text)))).Replace('-','')}
 finally{$s.Dispose()}
}
function Canon($Value){$Value|ConvertTo-Json -Depth 30 -Compress}
function Run-PowerShell([string]$Script,[object[]]$Arguments,[string]$Out,[string]$Err){
 $all=@('-NoProfile','-ExecutionPolicy','Bypass','-File',$Script)+$Arguments
 $oldPreference=$ErrorActionPreference
 try{
  $ErrorActionPreference='Continue'
  $text=& $powershell @all 2>&1
  $code=$LASTEXITCODE
 }finally{$ErrorActionPreference=$oldPreference}
 [IO.File]::WriteAllLines($Out,@($text|ForEach-Object{[string]$_}),$utf8)
 [IO.File]::WriteAllText($Err,'',$utf8)
 [ordered]@{exitCode=[int]$code;command=($powershell+' '+($all -join ' '));stdoutSha256=Sha $Out;stderrSha256=Sha $Err}
}
function Rechain([string]$Journal,[object[]]$Events,[switch]$Renumber){
 $previous=$zero;$lines=New-Object Collections.Generic.List[string];$n=0
 foreach($e in $Events){
  $n++;if($Renumber){$e.sequence=[int64]$n}
  $e.previousEventSha256=$previous;$e.currentEventSha256=$zero
  $e.currentEventSha256=TextSha (Canon $e);$previous=$e.currentEventSha256
  $lines.Add((Canon $e))
 }
 [IO.File]::WriteAllLines($Journal,$lines,$utf8)
}
function Copy-Case([string]$Source,[string]$Name){
 $d=Join-Path $EvidenceRoot $Name
 if(Test-Path -LiteralPath $d){[IO.Directory]::Delete($d,$true)}
 [IO.Directory]::CreateDirectory($d)|Out-Null
 Copy-Item -Path (Join-Path $Source '*') -Destination $d -Recurse -Force
 $d
}
if(Test-Path -LiteralPath $EvidenceRoot){throw 'Validation evidence root must be new.'}
[IO.Directory]::CreateDirectory($EvidenceRoot)|Out-Null
$fixture=Join-Path $EvidenceRoot 'fixture'
$repo=Join-Path $fixture 'candidate'
$commandRoot=Join-Path $fixture 'commands'
[IO.Directory]::CreateDirectory($repo)|Out-Null
[IO.Directory]::CreateDirectory($commandRoot)|Out-Null
& git.exe -C $repo init -q
& git.exe -C $repo config user.email 'rev869b-harness@example.invalid'
& git.exe -C $repo config user.name 'REV869B Harness Validation'
[IO.File]::WriteAllText((Join-Path $repo 'fixture.txt'),'base',$utf8)
& git.exe -C $repo add -- fixture.txt
& git.exe -C $repo commit -q -m 'fixture base'
$base=(& git.exe -C $repo rev-parse HEAD).Trim()
[IO.File]::WriteAllText((Join-Path $repo 'fixture.txt'),'candidate',$utf8)
& git.exe -C $repo add -- fixture.txt
& git.exe -C $repo commit -q -m 'fixture candidate'
$candidate=(& git.exe -C $repo rev-parse HEAD).Trim()
$tree=(& git.exe -C $repo rev-parse 'HEAD^{tree}').Trim()
& git.exe -C $repo checkout -q --detach $candidate
$fake=Join-Path $commandRoot 'fake-pass.cmd'
[IO.File]::WriteAllLines($fake,@('@echo off','if defined PGHOST exit /b 9','echo FAKE_OK','exit /b 0'),[Text.Encoding]::ASCII)
$trx=Join-Path $commandRoot 'fake.trx'
$trxText='<?xml version="1.0" encoding="utf-8"?><TestRun><Results><UnitTestResult testId="11111111-1111-1111-1111-111111111111" outcome="Passed"/></Results></TestRun>'
[IO.File]::WriteAllText($trx,$trxText,$utf8)
$authPath=Join-Path $fixture 'authorization.json'
$auth=[ordered]@{
 schemaVersion='rev869b.a5.validation-authorization/1';targetBranch='master'
 authorizedStartingCommit=$base;candidateCommit=$candidate;candidateTree=$tree
 changedPathAllowlist=@('fixture.txt');changedPathMaximum=1
 commands=@([ordered]@{planId='FAKE-01';stage=1;subordinal=1;executable=(Get-Command cmd.exe).Source;
  arguments=@('/d','/c',$fake);workingDirectory=$commandRoot;expectedExitCode=0;resultPath='fake.trx'})
}
[IO.File]::WriteAllText($authPath,(Canon $auth),$utf8)
$authHash=Sha $authPath
$env:PGHOST='must_be_scrubbed'
function New-ValidRun([string]$Name){
 $id=[Guid]::NewGuid();$parent=Join-Path $EvidenceRoot $Name
 [IO.Directory]::CreateDirectory($parent)|Out-Null
 $o=Join-Path $parent 'runner.out';$e=Join-Path $parent 'runner.err'
 $args=@('-Mode','FORMAL_ACCEPTANCE','-Purpose','HARNESS_VALIDATION','-RunId',$id.ToString('D'),
  '-CandidateWorktree',$repo,'-AuthorizationRecordPath',$authPath,'-AuthorizationRecordSha256',$authHash,
  '-EvidenceRoot',$parent,'-PlanPath',$plan,'-SchemaPath',$schema)
 $run=Run-PowerShell $runner $args $o $e
 if($run.exitCode -ne 0){throw ('Positive runner failed: '+(Get-Content -LiteralPath $o -Raw))}
 [ordered]@{id=$id;directory=(Join-Path $parent $id.ToString('D').ToLowerInvariant());runner=$run}
}
$valid1=New-ValidRun 'positive-1'
$valid2=New-ValidRun 'positive-2'
$results=New-Object Collections.Generic.List[object]
function Verify-Case([string]$Id,[string]$Name,[string]$Directory,[Guid]$IdValue,[int]$ExpectedExit){
 $journal=Join-Path $Directory 'journal.jsonl'
 $out=Join-Path $Directory 'verify.out';$err=Join-Path $Directory 'verify.err'
 $detached=Join-Path $Directory 'case-verification.json'
 $args=@('-JournalPath',$journal,'-PlanPath',$plan,'-SchemaPath',$schema,
  '-AuthorizationRecordPath',$authPath,'-AuthorizationRecordSha256',$authHash,
  '-CandidateWorktree',$repo,'-ExpectedPurpose','HARNESS_VALIDATION','-ExpectedRunId',$IdValue.ToString('D'),
  '-DetachedResultPath',$detached)
 $before=Sha $journal
 $r=Run-PowerShell $verifier $args $out $err
 $actual='FAIL';if($r.exitCode -eq 0){$actual='PASS'}
 $expected='FAIL';if($ExpectedExit -eq 0){$expected='PASS'}
 $row=[ordered]@{caseId=$Id;name=$Name;fixtureSha256=$before;command=$r.command;expectedOutcome=$expected;
  actualOutcome=$actual;expectedExitCode=$ExpectedExit;exitCode=$r.exitCode;
  decisiveErrorCode=$r.exitCode;stdoutSha256=$r.stdoutSha256;stderrSha256=$r.stderrSha256;
  passed=($r.exitCode -eq $ExpectedExit)}
 $results.Add($row)
 if(-not $row.passed){throw ('Validation case failed: '+$Id)}
}
Verify-Case 'V01' 'correct complete sequence passes' $valid1.directory $valid1.id 0
$source=$valid2.directory
$d=Copy-Case $source 'V02-before-marker'
$ev=@(Get-Content -LiteralPath (Join-Path $d 'journal.jsonl')|ForEach-Object{$_|ConvertFrom-Json})
$ordered=@($ev[0],$ev[1],$ev[3],$ev[2],$ev[4],$ev[5],$ev[6])
Rechain (Join-Path $d 'journal.jsonl') $ordered -Renumber
Verify-Case 'V02' 'command before start marker fails' $d $valid2.id 70

$d=Copy-Case $source 'V03-missing-sequence'
$ev=@(Get-Content -LiteralPath (Join-Path $d 'journal.jsonl')|ForEach-Object{$_|ConvertFrom-Json})
$ev[3].sequence=[int64]$ev[3].sequence+1
Rechain (Join-Path $d 'journal.jsonl') $ev
Verify-Case 'V03' 'missing sequence number fails' $d $valid2.id 70

$d=Copy-Case $source 'V04-duplicate-sequence'
$ev=@(Get-Content -LiteralPath (Join-Path $d 'journal.jsonl')|ForEach-Object{$_|ConvertFrom-Json})
$ev[3].sequence=$ev[2].sequence
Rechain (Join-Path $d 'journal.jsonl') $ev
Verify-Case 'V04' 'duplicate sequence number fails' $d $valid2.id 70

$d=Copy-Case $source 'V05-reordered'
$ev=@(Get-Content -LiteralPath (Join-Path $d 'journal.jsonl')|ForEach-Object{$_|ConvertFrom-Json})
$swap=$ev[3];$ev[3]=$ev[4];$ev[4]=$swap
Rechain (Join-Path $d 'journal.jsonl') $ev
Verify-Case 'V05' 'reordered event fails' $d $valid2.id 70

$d=Copy-Case $source 'V06-reversed-time'
$ev=@(Get-Content -LiteralPath (Join-Path $d 'journal.jsonl')|ForEach-Object{$_|ConvertFrom-Json})
$done=@($ev|Where-Object{$_.eventType -eq 'COMMAND_COMPLETED'})[0]
$done.timing.endUtc='2000-01-01T00:00:00.0000000Z';$done.timing.endTicks=0;$done.timing.durationTicks=0
Rechain (Join-Path $d 'journal.jsonl') $ev
Verify-Case 'V06' 'reversed timestamp fails' $d $valid2.id 70
$d=Copy-Case $source 'V07-candidate-substitution'
$ev=@(Get-Content -LiteralPath (Join-Path $d 'journal.jsonl')|ForEach-Object{$_|ConvertFrom-Json})
$ev[4].candidate.commit='aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
Rechain (Join-Path $d 'journal.jsonl') $ev
Verify-Case 'V07' 'candidate commit or tree substitution fails' $d $valid2.id 72

$d=Copy-Case $source 'V08-manifest-substitution'
$ev=@(Get-Content -LiteralPath (Join-Path $d 'journal.jsonl')|ForEach-Object{$_|ConvertFrom-Json})
$ev[4].candidate.manifestSha256='A'*64
Rechain (Join-Path $d 'journal.jsonl') $ev
Verify-Case 'V08' 'manifest substitution fails' $d $valid2.id 72

$d=Copy-Case $source 'V09-evidence-tamper'
$ev=@(Get-Content -LiteralPath (Join-Path $d 'journal.jsonl')|ForEach-Object{$_|ConvertFrom-Json})
$done=@($ev|Where-Object{$_.eventType -eq 'COMMAND_COMPLETED'})[0]
$tamper=Join-Path $d ([string]$done.stdout.path).Replace('/','\')
[IO.File]::AppendAllText($tamper,'tamper',$utf8)
Verify-Case 'V09' 'modified stdout stderr or result fails' $d $valid2.id 73

$d=Copy-Case $source 'V10-retry-after-failure'
$ev=@(Get-Content -LiteralPath (Join-Path $d 'journal.jsonl')|ForEach-Object{$_|ConvertFrom-Json})
$done=@($ev|Where-Object{$_.eventType -eq 'COMMAND_COMPLETED'})[0]
$done.eventType='FORMAL_ACCEPTANCE_FAILED'
Rechain (Join-Path $d 'journal.jsonl') $ev
Verify-Case 'V10' 'retry or event after failure fails' $d $valid2.id 74

$d=Copy-Case $source 'V11-development-relabel'
$ev=@(Get-Content -LiteralPath (Join-Path $d 'journal.jsonl')|ForEach-Object{$_|ConvertFrom-Json})
$ev[4].mode='DEVELOPMENT_FEEDBACK_ONLY'
Rechain (Join-Path $d 'journal.jsonl') $ev
Verify-Case 'V11' 'development evidence relabeling fails' $d $valid2.id 75
$d=Copy-Case $source 'V12-freeze-change'
[IO.File]::WriteAllText((Join-Path $repo 'fixture.txt'),'changed-after-freeze',$utf8)
try{Verify-Case 'V12' 'candidate file modification after freeze fails' $d $valid2.id 76}
finally{[IO.File]::WriteAllText((Join-Path $repo 'fixture.txt'),'candidate',$utf8)}

$d=Copy-Case $source 'V13-mutant-restore'
$ev=@(Get-Content -LiteralPath (Join-Path $d 'journal.jsonl')|ForEach-Object{$_|ConvertFrom-Json})
$last=$ev[$ev.Count-1];$last.eventType='MUTANT_COMPLETED'
$last.mutant=[ordered]@{id='A5-M11';enforcementLocation='fake';originalBlob='1'*40;originalSha256='A'*64;
 mutatedBlob='2'*40;mutatedSha256='B'*64;compiled=$true;killed=$true;restoredBlob='3'*40;
 restoredSha256='C'*64;restorationEquality=$false}
Rechain (Join-Path $d 'journal.jsonl') $ev
Verify-Case 'V13' 'mutant restoration mismatch fails' $d $valid2.id 77

$d=Copy-Case $source 'V14-counter'
$ev=@(Get-Content -LiteralPath (Join-Path $d 'journal.jsonl')|ForEach-Object{$_|ConvertFrom-Json})
$counter=@($ev|Where-Object{$_.eventType -eq 'COUNTERS_OBSERVED'})[0]
$counter.counters.postgresqlConnections=1
Rechain (Join-Path $d 'journal.jsonl') $ev
Verify-Case 'V14' 'nonzero prohibited counter fails' $d $valid2.id 78

$d=Copy-Case $source 'V15-fabricated-pass'
[IO.File]::WriteAllText((Join-Path $d 'case-verification.json'),'fabricated PASS',$utf8)
Verify-Case 'V15' 'runner supplied or fabricated verifier PASS fails' $d $valid2.id 79
Verify-Case 'A01' 'second successful run and deterministic replay' $valid2.directory $valid2.id 0
function Projection([string]$Journal){
 $items=New-Object Collections.Generic.List[object]
 foreach($e in @(Get-Content -LiteralPath $Journal|ForEach-Object{$_|ConvertFrom-Json})){
  $count=$null
  if($null -ne $e.counters){
   $count=[ordered]@{postgresqlConnections=$e.counters.postgresqlConnections;
    postgresqlTestsExecuted=$e.counters.postgresqlTestsExecuted;migrationApplications=$e.counters.migrationApplications;
    migrationRemovals=$e.counters.migrationRemovals;serviceStarts=$e.counters.serviceStarts;
    productionAccess=$e.counters.productionAccess;externalDeployments=$e.counters.externalDeployments;
    networkRequests=$e.counters.networkRequests}
  }
  $items.Add([ordered]@{eventType=$e.eventType;mode=$e.mode;purpose=$e.purpose;candidate=$e.candidate;
   command=$e.command;tool=$e.tool;result=$e.result;
   stdoutSha=$(if($null -eq $e.stdout){$null}else{$e.stdout.sha256});
   stderrSha=$(if($null -eq $e.stderr){$null}else{$e.stderr.sha256});
   trxSha=$(if($null -eq $e.trx){$null}else{$e.trx.sha256});mutant=$e.mutant;counters=$count})
 }
 $items
}
$p1=Canon (Projection (Join-Path $valid1.directory 'journal.jsonl'))
$p2=Canon (Projection (Join-Path $valid2.directory 'journal.jsonl'))
if($p1 -cne $p2){throw 'Deterministic semantic replay mismatch.'}
$planObject=Get-Content -LiteralPath $plan -Raw|ConvertFrom-Json
$schemaObject=Get-Content -LiteralPath $schema -Raw|ConvertFrom-Json
$schemaPass=(@($planObject.stages).Count -eq 18 -and @($planObject.mutants).Count -eq 40 -and
 @($schemaObject.required).Count -eq 24 -and @($schemaObject.properties.PSObject.Properties).Count -eq 24)
$schemaOut=Join-Path $EvidenceRoot 'schema-validation.txt'
[IO.File]::WriteAllText($schemaOut,('stages=18;mutants=40;properties=24;pass='+$schemaPass),$utf8)
$results.Add([ordered]@{caseId='A02';name='JSON schema and immutable-plan cardinality';
 fixtureSha256=(Sha $schema);command='PowerShell ConvertFrom-Json and exact cardinality checks';expectedOutcome='PASS';
 actualOutcome=$(if($schemaPass){'PASS'}else{'FAIL'});expectedExitCode=0;exitCode=$(if($schemaPass){0}else{70});
 decisiveErrorCode=$(if($schemaPass){0}else{70});stdoutSha256=Sha $schemaOut;stderrSha256=Sha $schemaOut;passed=$schemaPass})
if(-not $schemaPass){throw 'Schema validation failed.'}
$summary=[ordered]@{state='HARNESS_VALIDATION_NOT_A5_FORMAL_EVIDENCE';total=$results.Count;
 passed=@($results|Where-Object{$_.passed}).Count;failed=@($results|Where-Object{-not $_.passed}).Count;
 deterministicSemanticReplay=$true;cleanEnvironmentScrubbed=$true;cases=$results}
$resultPath=Join-Path $EvidenceRoot 'validation-results.json'
[IO.File]::WriteAllText($resultPath,(Canon $summary),$utf8)
Remove-Item Env:PGHOST -ErrorAction SilentlyContinue
$summary|ConvertTo-Json -Depth 12
if($summary.failed -ne 0){exit 1}
exit 0
