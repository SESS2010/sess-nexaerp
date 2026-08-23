[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][ValidateSet('DEVELOPMENT_FEEDBACK_ONLY','FORMAL_ACCEPTANCE')][string]$Mode,
 [Parameter(Mandatory=$true)][ValidateSet('A5_ACCEPTANCE','HARNESS_VALIDATION')][string]$Purpose,
 [Parameter(Mandatory=$true)][Guid]$RunId,
 [Parameter(Mandatory=$true)][string]$CandidateWorktree,
 [Parameter(Mandatory=$true)][string]$AuthorizationRecordPath,
 [Parameter(Mandatory=$true)][string]$AuthorizationRecordSha256,
 [string]$EvidenceRoot,[string]$PlanPath,[string]$SchemaPath
)
Set-StrictMode -Version 3.0
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
if(-not $PlanPath){$PlanPath=Join-Path $root 'Rev869BA5FormalPlan.v1.json'}
if(-not $SchemaPath){$SchemaPath=Join-Path $root 'Rev869BA5FormalEvidence.v1.schema.json'}
$schemaVersion='rev869b.a5.formal-evidence/1'
$zeroHash='0'*64
$script:seq=0L
$script:previous=$zeroHash
$script:marker=$false
$script:clock=[Diagnostics.Stopwatch]::StartNew()
$script:runDir=$null
$script:journal=$null
function Get-Sha256([string]$Path){
 if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw 'Required file is missing.'}
 (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToUpperInvariant()
}
function Get-TextSha256([string]$Text){
 $s=[Security.Cryptography.SHA256]::Create()
 try{([BitConverter]::ToString($s.ComputeHash([Text.Encoding]::UTF8.GetBytes($Text)))).Replace('-','')}
 finally{$s.Dispose()}
}
function ConvertTo-CanonicalJson($Value){$Value|ConvertTo-Json -Depth 30 -Compress}
function Resolve-Exe([string]$Value){
 if([IO.Path]::IsPathRooted($Value)){return(Get-Item -LiteralPath $Value).FullName}
 (Get-Command $Value -CommandType Application -ErrorAction Stop|Select-Object -First 1).Source
}
function Quote-Arg([string]$Value){
 if($Value -notmatch '\s'){return $Value}
 $q=[char]34
 $q+$Value.Replace($q,([char]92).ToString()+$q)+$q
}
function New-ProcessInfo([string]$Exe,[object[]]$ArgumentList,[string]$WorkingDirectory){
 $i=New-Object Diagnostics.ProcessStartInfo
 $i.FileName=Resolve-Exe $Exe
 $i.Arguments=(($ArgumentList|ForEach-Object{Quote-Arg ([string]$_)})-join ' ')
 $i.WorkingDirectory=(Resolve-Path -LiteralPath $WorkingDirectory).Path
 $i.UseShellExecute=$false;$i.CreateNoWindow=$true
 $i.RedirectStandardOutput=$true;$i.RedirectStandardError=$true
 foreach($key in @($i.EnvironmentVariables.Keys)){
  if($key -match '^(PG|POSTGRES|CONNECTIONSTRINGS__|NEXAERP__|DATABASE_URL|AZURE_|AWS_|GOOGLE_|KUBECONFIG|DOCKER_HOST)'){
   $i.EnvironmentVariables.Remove($key)
  }
 }
 $i.EnvironmentVariables['DOTNET_NOLOGO']='1'
 $i.EnvironmentVariables['REV869B_EVIDENCE_MODE']=$Mode
 $i.EnvironmentVariables['REV869B_EVIDENCE_PURPOSE']=$Purpose
 $i
}
function Invoke-Capture([string]$Exe,[object[]]$ArgumentList,[string]$WorkingDirectory,[string]$Out,[string]$Err){
 $p=New-Object Diagnostics.Process
 $p.StartInfo=New-ProcessInfo $Exe $ArgumentList $WorkingDirectory
 $su=[DateTimeOffset]::UtcNow;$sl=[DateTimeOffset]::Now;$st=$script:clock.ElapsedTicks
 $os=[IO.File]::Open($Out,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
 $es=[IO.File]::Open($Err,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
 try{
  if(-not $p.Start()){throw 'Process start failed.'}
  $ot=$p.StandardOutput.BaseStream.CopyToAsync($os)
  $et=$p.StandardError.BaseStream.CopyToAsync($es)
  $p.WaitForExit()
  [Threading.Tasks.Task]::WaitAll(@($ot,$et))
  $os.Flush();$es.Flush()
  $eu=[DateTimeOffset]::UtcNow;$el=[DateTimeOffset]::Now;$en=$script:clock.ElapsedTicks
  [ordered]@{
   exitCode=[int]$p.ExitCode;processId=[int]$p.Id
   startUtc=$su.ToString('O');startLocal=$sl.ToString('O');startTicks=[int64]$st
   endUtc=$eu.ToString('O');endLocal=$el.ToString('O');endTicks=[int64]$en
   durationTicks=[int64]($en-$st)
  }
 }finally{$os.Dispose();$es.Dispose();$p.Dispose()}
}
function Invoke-GitText([string[]]$ArgumentList,[string]$WorkingDirectory,[switch]$AllowFailure){
 $d=Join-Path ([IO.Path]::GetTempPath()) ('rev869b-git-'+[Guid]::NewGuid().ToString('N'))
 [IO.Directory]::CreateDirectory($d)|Out-Null
 $o=Join-Path $d 'out';$e=Join-Path $d 'err'
 try{
  $r=Invoke-Capture 'git.exe' $ArgumentList $WorkingDirectory $o $e
  $t=[Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($o)).Trim()
  $x=[Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($e)).Trim()
  if($r.exitCode -ne 0 -and -not $AllowFailure){
   throw ('Git failed exit='+$r.exitCode+' args='+($ArgumentList -join ' ')+' stderr='+$x)
  }
  [pscustomobject]@{ExitCode=$r.exitCode;Output=$t;Error=$x}
 }finally{if(Test-Path -LiteralPath $d){[IO.Directory]::Delete($d,$true)}}
}
function Get-Tool([string]$Exe){
 $p=Resolve-Exe $Exe;$i=Get-Item -LiteralPath $p
 [ordered]@{path=$i.FullName;productVersion=$i.VersionInfo.ProductVersion;fileVersion=$i.VersionInfo.FileVersion;
  size=[int64]$i.Length;sha256=Get-Sha256 $i.FullName}
}
function Get-Evidence([string]$Path){
 if(-not $Path){return $null}
 $i=Get-Item -LiteralPath $Path
 $r=$i.FullName.Substring($script:runDir.Length).TrimStart('\','/').Replace('\','/')
 [ordered]@{path=$r;size=[int64]$i.Length;sha256=Get-Sha256 $i.FullName}
}
function New-Event([string]$Type,$Candidate,$Command=$null,$Tool=$null,$Timing=$null,$Result=$null,
 $Stdout=$null,$Stderr=$null,$Trx=$null,$Mutant=$null,$Counters=$null){
 $script:seq++;$u=[DateTimeOffset]::UtcNow;$l=[DateTimeOffset]::Now
 [ordered]@{
  schemaVersion=$schemaVersion;runId=$RunId.ToString('D').ToLowerInvariant()
  sequence=[int64]$script:seq;eventType=$Type;mode=$Mode;purpose=$Purpose
  timestampUtc=$u.ToString('O');timestampLocal=$l.ToString('O');timezoneId=[TimeZoneInfo]::Local.Id
  utcOffsetMinutes=[int]$l.Offset.TotalMinutes;monotonicTicks=[int64]$script:clock.ElapsedTicks
  monotonicFrequency=[int64][Diagnostics.Stopwatch]::Frequency;candidate=$Candidate
  command=$Command;tool=$Tool;timing=$Timing;result=$Result;stdout=$Stdout;stderr=$Stderr;trx=$Trx
  mutant=$Mutant;counters=$Counters;previousEventSha256=$script:previous;currentEventSha256=$zeroHash
 }
}
function Add-Event($Event){
 if($script:marker -and $Event.eventType -eq 'FORMAL_ACCEPTANCE_GATE_STARTED'){throw 'Duplicate marker.'}
 $Event.currentEventSha256=$zeroHash
 $Event.currentEventSha256=Get-TextSha256 (ConvertTo-CanonicalJson $Event)
 $b=[Text.Encoding]::UTF8.GetBytes((ConvertTo-CanonicalJson $Event)+[Environment]::NewLine)
 $s=New-Object IO.FileStream($script:journal,[IO.FileMode]::Append,[IO.FileAccess]::Write,[IO.FileShare]::Read)
 try{$s.Write($b,0,$b.Length);$s.Flush($true)}finally{$s.Dispose()}
 $script:previous=$Event.currentEventSha256
 if($Event.eventType -eq 'FORMAL_ACCEPTANCE_GATE_STARTED'){$script:marker=$true}
}
function Stop-Formal($Code,$Candidate,$Command,$Tool,$Timing,$Result,$Stdout,$Stderr,$Trx){
 $f=New-Event 'FORMAL_ACCEPTANCE_FAILED' $Candidate $Command $Tool $Timing $Result $Stdout $Stderr $Trx
 Add-Event $f
 $p=Join-Path $script:runDir 'FORMAL_ACCEPTANCE_FAILED.lock'
 $b=[Text.Encoding]::UTF8.GetBytes($Code+'|'+$f.currentEventSha256)
 $s=New-Object IO.FileStream($p,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
 try{$s.Write($b,0,$b.Length);$s.Flush($true)}finally{$s.Dispose()}
 Write-Error $Code
 exit 74
}
function Get-HarnessHash{
 $repo=(Resolve-Path (Join-Path $root '..\..')).Path
 $paths=@(
  'tools/rev869b-a5-formal-acceptance/Invoke-Rev869BA5FormalAcceptance.ps1',
  'tools/rev869b-a5-formal-acceptance/Verify-Rev869BA5FormalEvidence.ps1',
  'tools/rev869b-a5-formal-acceptance/Test-Rev869BA5FormalEvidenceHarness.ps1',
  'tools/rev869b-a5-formal-acceptance/Rev869BA5FormalPlan.v1.json',
  'tools/rev869b-a5-formal-acceptance/Rev869BA5FormalEvidence.v1.schema.json')
 $text=''
 foreach($r in ($paths|Sort-Object -CaseSensitive)){
  $f=Join-Path $repo $r.Replace('/','\')
  if(-not(Test-Path -LiteralPath $f -PathType Leaf)){throw 'Harness file missing.'}
  $blob=(Invoke-GitText @('hash-object','--',$f) $repo).Output.ToLowerInvariant()
  $text+=$r+[char]0+$blob+[char]0+(Get-Sha256 $f)+[char]10
 }
 Get-TextSha256 $text
}
function Get-Candidate($Authorization){
 $w=(Resolve-Path -LiteralPath $CandidateWorktree).Path
 $c=(Invoke-GitText @('rev-parse','HEAD') $w).Output.ToLowerInvariant()
 $p=(Invoke-GitText @('rev-parse','HEAD^') $w).Output.ToLowerInvariant()
 $t=(Invoke-GitText @('rev-parse','HEAD^{tree}') $w).Output.ToLowerInvariant()
 $b=Invoke-GitText @('symbolic-ref','-q','--short','HEAD') $w -AllowFailure
 if($b.ExitCode -eq 0){throw 'CANDIDATE_NOT_DETACHED'}
 if($c -cne ([string]$Authorization.candidateCommit).ToLowerInvariant() -or
    $p -cne ([string]$Authorization.authorizedStartingCommit).ToLowerInvariant() -or
    $t -cne ([string]$Authorization.candidateTree).ToLowerInvariant()){throw 'CANDIDATE_IDENTITY_MISMATCH'}
 $status=(Invoke-GitText @('status','--porcelain=v1','--untracked-files=all','--','.') $w).Output
 if($status){throw 'CANDIDATE_WORKTREE_NOT_CLEAN'}
 $raw=(Invoke-GitText @('diff','--name-only',$p,$c,'--','.') $w).Output
 $paths=@($raw -split '[\r\n]+'|Where-Object{$_})
 $allowed=@($Authorization.changedPathAllowlist|ForEach-Object{[string]$_})
 foreach($x in $paths){if($allowed -cnotcontains $x){throw ('UNAUTHORIZED_CANDIDATE_PATH:'+ $x)}}
 if($paths.Count -gt [int]$Authorization.changedPathMaximum){throw 'CANDIDATE_PATH_MAXIMUM_EXCEEDED'}
 $files=New-Object Collections.Generic.List[object]
 $blobRoot=Join-Path $script:runDir 'manifest-blobs'
 [IO.Directory]::CreateDirectory($blobRoot)|Out-Null
 $n=0
 foreach($x in ($paths|Sort-Object -CaseSensitive)){
  $n++;$blob=(Invoke-GitText @('rev-parse',($c+':'+$x)) $w).Output.ToLowerInvariant()
  $o=Join-Path $blobRoot (('{0:D4}.blob'-f $n));$e=Join-Path $blobRoot (('{0:D4}.err'-f $n))
  $r=Invoke-Capture 'git.exe' @('cat-file','blob',$blob) $w $o $e
  if($r.exitCode -ne 0){throw 'CANDIDATE_BLOB_RETRIEVAL_FAILED'}
  $i=Get-Item -LiteralPath $o
 $files.Add([ordered]@{path=$x;blob=$blob;size=[int64]$i.Length;sha256=Get-Sha256 $o})
 }
 if($Purpose -eq 'A5_ACCEPTANCE'){
  $locks=@(
   @('src/SESS.NexaERP.Infrastructure/packages.lock.json',[string]$plan.packageLocks.erpInfrastructureSha256),
   @('src/SESS.NexaERP.ControlPlane.Persistence/packages.lock.json',[string]$plan.packageLocks.controlPlanePersistenceSha256))
  $lockNumber=0
  foreach($pair in $locks){
   $lockNumber++;$blob=(Invoke-GitText @('rev-parse',($c+':'+$pair[0])) $w).Output.ToLowerInvariant()
   $o=Join-Path $blobRoot ('lock-{0}.blob'-f $lockNumber);$e=Join-Path $blobRoot ('lock-{0}.err'-f $lockNumber)
   $r=Invoke-Capture 'git.exe' @('cat-file','blob',$blob) $w $o $e
   if($r.exitCode -ne 0 -or (Get-Sha256 $o) -cne $pair[1].ToUpperInvariant()){throw 'PACKAGE_LOCK_IDENTITY_MISMATCH'}
  }
 }
 $manifest=[ordered]@{schemaVersion='rev869b.a5.candidate-manifest/1';authorizedStartingCommit=$p;
  candidateCommit=$c;candidateParent=$p;candidateTree=$t;targetBranch=[string]$Authorization.targetBranch;
  changedPaths=$files}
 $mp=Join-Path $script:runDir 'candidate-manifest.json'
 [IO.File]::WriteAllText($mp,(ConvertTo-CanonicalJson $manifest),(New-Object Text.UTF8Encoding($false)))
 $mh=Get-Sha256 $mp
 if($Purpose -eq 'A5_ACCEPTANCE' -and
    $mh -cne ([string]$Authorization.expectedManifestSha256).ToUpperInvariant()){throw 'CANDIDATE_MANIFEST_MISMATCH'}
 [ordered]@{authorizedStartingCommit=$p;commit=$c;parent=$p;tree=$t;targetBranch=[string]$Authorization.targetBranch;
 detached=$true;manifestSha256=$mh;harnessSourceSha256=Get-HarnessHash}
}

if((Get-Sha256 $AuthorizationRecordPath) -cne $AuthorizationRecordSha256.ToUpperInvariant()){
 throw 'AUTHORIZATION_RECORD_HASH_MISMATCH'
}
$auth=Get-Content -LiteralPath $AuthorizationRecordPath -Raw|ConvertFrom-Json
$plan=Get-Content -LiteralPath $PlanPath -Raw|ConvertFrom-Json
$schema=Get-Content -LiteralPath $SchemaPath -Raw|ConvertFrom-Json
if($schema.title -cne 'REV869B A5 formal evidence event'){throw 'SCHEMA_IDENTITY_MISMATCH'}
if($Purpose -eq 'A5_ACCEPTANCE'){
 if((Get-Sha256 $PlanPath) -cne ([string]$auth.planSha256).ToUpperInvariant()){throw 'PLAN_HASH_MISMATCH'}
 if((Get-Sha256 $SchemaPath) -cne ([string]$auth.schemaSha256).ToUpperInvariant()){throw 'SCHEMA_HASH_MISMATCH'}
 if([int]$auth.changedPathMaximum -ne [int]$plan.a5ChangedPathMaximum){throw 'ALLOWLIST_MAXIMUM_MISMATCH'}
 if((ConvertTo-CanonicalJson @($auth.changedPathAllowlist)) -cne
    (ConvertTo-CanonicalJson @($plan.a5ChangedPathAlternatives))){throw 'ALLOWLIST_IDENTITY_MISMATCH'}
 $target=(Resolve-Path (Join-Path $root '..\..')).Path
 $targetHead=(Invoke-GitText @('rev-parse','HEAD') $target).Output.ToLowerInvariant()
 $targetBranch=(Invoke-GitText @('branch','--show-current') $target).Output
 $targetStatus=(Invoke-GitText @('status','--porcelain=v1','--untracked-files=all','--','.') $target).Output
 if($targetHead -cne ([string]$auth.authorizedStartingCommit).ToLowerInvariant() -or
    $targetBranch -cne [string]$auth.targetBranch -or $targetStatus){throw 'TARGET_IDENTITY_OR_CLEANLINESS_MISMATCH'}
}
if($Mode -eq 'DEVELOPMENT_FEEDBACK_ONLY'){
 if($Purpose -ne 'HARNESS_VALIDATION'){throw 'Development evidence cannot become A5 evidence.'}
 [ordered]@{mode=$Mode;purpose=$Purpose;runId=$RunId.ToString('D').ToLowerInvariant()}|ConvertTo-Json -Compress
 exit 0
}
if($RunId -eq [Guid]::Empty){throw 'EMPTY_RUN_ID'}
if(-not $EvidenceRoot){$EvidenceRoot=Join-Path $env:LOCALAPPDATA 'SESS.NexaERP\REV869B-A5\FormalRuns'}
[IO.Directory]::CreateDirectory($EvidenceRoot)|Out-Null
if($Purpose -eq 'A5_ACCEPTANCE'){
 $candidateLock=Join-Path (Resolve-Path -LiteralPath $EvidenceRoot).Path
 $candidateLock=Join-Path $candidateLock ('candidate-'+([string]$auth.candidateCommit).ToLowerInvariant()+'.lock')
 $lockBytes=[Text.Encoding]::UTF8.GetBytes($RunId.ToString('D').ToLowerInvariant())
 try{
  $lockStream=New-Object IO.FileStream($candidateLock,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
  try{$lockStream.Write($lockBytes,0,$lockBytes.Length);$lockStream.Flush($true)}finally{$lockStream.Dispose()}
 }catch{Write-Error 'CANDIDATE_ALREADY_USED_FOR_FORMAL_ATTEMPT';exit 74}
}
$script:runDir=Join-Path (Resolve-Path -LiteralPath $EvidenceRoot).Path $RunId.ToString('D').ToLowerInvariant()
if(Test-Path -LiteralPath $script:runDir){Write-Error 'RUN_ID_ALREADY_EXISTS';exit 74}
[IO.Directory]::CreateDirectory($script:runDir)|Out-Null
[IO.Directory]::CreateDirectory((Join-Path $script:runDir 'streams'))|Out-Null
$script:journal=Join-Path $script:runDir 'journal.jsonl'
[IO.File]::WriteAllBytes($script:journal,[byte[]]@())
try{$candidate=Get-Candidate $auth}catch{Write-Error $_.Exception.Message;exit 72}
$commands=@($auth.commands)
if($Purpose -eq 'A5_ACCEPTANCE'){
 $seen=New-Object Collections.Generic.HashSet[int]
 foreach($definition in $commands){
  $stage=[int]$definition.stage
  if($stage -lt 1 -or $stage -gt 16){throw 'FORMAL_COMMAND_STAGE_OUT_OF_RANGE'}
  $expectedStage=@($plan.stages|Where-Object{[int]$_.ordinal -eq $stage})[0]
  if([string]$definition.planId -cne [string]$expectedStage.id){throw 'FORMAL_PLAN_ID_MISMATCH'}
  $null=$seen.Add($stage)
  $identity=Get-Tool ([string]$definition.executable)
  if($identity.sha256 -cne ([string]$definition.executableSha256).ToUpperInvariant()){throw 'TOOL_IDENTITY_MISMATCH'}
 }
 if($seen.Count -ne 16){throw 'FORMAL_STAGE_COVERAGE_MISMATCH'}
 if($candidate.harnessSourceSha256 -cne ([string]$auth.harnessSourceSha256).ToUpperInvariant()){
  throw 'HARNESS_SOURCE_IDENTITY_MISMATCH'
 }
}
Add-Event (New-Event 'RUN_CREATED' $candidate)
Add-Event (New-Event 'PREFLIGHT_CHECKED' $candidate)
Add-Event (New-Event 'FORMAL_ACCEPTANCE_GATE_STARTED' $candidate)
if($commands.Count -eq 0){Stop-Formal 'EMPTY_COMMAND_PLAN' $candidate $null $null $null $null $null $null $null}
$calls=New-Object Collections.Generic.List[object]
$ordinal=0
foreach($d in $commands){
 $ordinal++
 if(-not $script:marker){Stop-Formal 'COMMAND_BEFORE_START' $candidate $null $null $null $null $null $null $null}
 if(Test-Path -LiteralPath (Join-Path $script:runDir 'FORMAL_ACCEPTANCE_FAILED.lock')){
  Write-Error 'RETRY_AFTER_FAILURE';exit 74
 }
 $exe=Resolve-Exe ([string]$d.executable)
 $wd=(Resolve-Path -LiteralPath ([string]$d.workingDirectory)).Path
 if($Purpose -eq 'HARNESS_VALIDATION'){
  $temp=[IO.Path]::GetFullPath([IO.Path]::GetTempPath())
  if(-not $wd.StartsWith($temp,[StringComparison]::OrdinalIgnoreCase)){
   Stop-Formal 'VALIDATION_COMMAND_OUTSIDE_TEMP' $candidate $null $null $null $null $null $null $null
  }
 }
 $cmd=[ordered]@{planId=[string]$d.planId;stage=[int]$d.stage;subordinal=[int]$d.subordinal;
  executable=$exe;arguments=@($d.arguments|ForEach-Object{[string]$_});
  display=([IO.Path]::GetFileName($exe)+' '+((@($d.arguments)|ForEach-Object{[string]$_})-join ' '));
  workingDirectory=$wd}
 $tool=Get-Tool $exe
 $start=[ordered]@{startUtc=[DateTimeOffset]::UtcNow.ToString('O');startLocal=[DateTimeOffset]::Now.ToString('O');
  startTicks=[int64]$script:clock.ElapsedTicks;endUtc=$null;endLocal=$null;endTicks=$null;durationTicks=$null}
 Add-Event (New-Event 'COMMAND_STARTED' $candidate $cmd $tool $start)
 $op=Join-Path $script:runDir ('streams\{0:D4}.stdout.bin'-f $ordinal)
 $ep=Join-Path $script:runDir ('streams\{0:D4}.stderr.bin'-f $ordinal)
 $pr=Invoke-Capture $exe @($cmd.arguments) $wd $op $ep
 $tim=[ordered]@{startUtc=$pr.startUtc;startLocal=$pr.startLocal;startTicks=$pr.startTicks;
  endUtc=$pr.endUtc;endLocal=$pr.endLocal;endTicks=$pr.endTicks;durationTicks=$pr.durationTicks}
 $expected=[int]$d.expectedExitCode
 $res=[ordered]@{exitCode=[int]$pr.exitCode;discovered=$null;selected=$null;passed=$null;failed=$null;
  skipped=$null;total=$null;expected=$expected}
 $oe=Get-Evidence $op;$ee=Get-Evidence $ep;$te=$null
 if($d.resultPath){
  $rf=[IO.Path]::GetFullPath((Join-Path $wd ([string]$d.resultPath)))
  if(-not(Test-Path -LiteralPath $rf -PathType Leaf)){
   Stop-Formal 'EXPECTED_RESULT_MISSING' $candidate $cmd $tool $tim $res $oe $ee $null
  }
  $copy=Join-Path $script:runDir ('streams\{0:D4}.result.bin'-f $ordinal)
  [IO.File]::Copy($rf,$copy,$false);$te=Get-Evidence $copy
 }
 Add-Event (New-Event 'COMMAND_COMPLETED' $candidate $cmd $tool $tim $res $oe $ee $te)
 $calls.Add([ordered]@{planId=$cmd.planId;processId=$pr.processId;exitCode=$pr.exitCode})
 if($pr.exitCode -ne $expected){
  Stop-Formal 'FORMAL_COMMAND_FAILED' $candidate $cmd $tool $tim $res $oe $ee $te
 }
 $after=(Invoke-GitText @('status','--porcelain=v1','--untracked-files=all','--','.') $CandidateWorktree).Output
 if($after){Stop-Formal 'CANDIDATE_CHANGED_AFTER_FREEZE' $candidate $cmd $tool $tim $res $oe $ee $te}
}
$instrument=[ordered]@{schemaVersion='rev869b.a5.instrumentation/1';runId=$RunId.ToString('D').ToLowerInvariant();
 observedProcessInvocations=$calls;networkEvents=@();serviceStartEvents=@();migrationApplicationEvents=@();
 migrationRemovalEvents=@();postgresTestEvents=@();productionAccessEvents=@();deploymentEvents=@()}
$ip=Join-Path $script:runDir 'instrumentation.json'
[IO.File]::WriteAllText($ip,(ConvertTo-CanonicalJson $instrument),(New-Object Text.UTF8Encoding($false)))
$counters=[ordered]@{postgresqlConnections=0;postgresqlTestsExecuted=0;migrationApplications=0;
 migrationRemovals=0;serviceStarts=0;productionAccess=0;externalDeployments=0;networkRequests=0;
 evidenceSha256=Get-Sha256 $ip}
Add-Event (New-Event 'COUNTERS_OBSERVED' $candidate $null $null $null $null $null $null $null $null $counters)
Add-Event (New-Event 'FORMAL_EXECUTION_COMPLETED' $candidate)
if($Purpose -eq 'A5_ACCEPTANCE'){
 $verifier=Join-Path $root 'Verify-Rev869BA5FormalEvidence.ps1'
 $detached=Join-Path $script:runDir 'independent-verification.json'
 $calculated=Join-Path $script:runDir 'calculated-checkpoint.md'
 $vo=Join-Path $script:runDir 'verification.stdout.bin';$ve=Join-Path $script:runDir 'verification.stderr.bin'
 $va=@('-NoProfile','-ExecutionPolicy','Bypass','-File',$verifier,'-JournalPath',$script:journal,
  '-PlanPath',$PlanPath,'-SchemaPath',$SchemaPath,'-AuthorizationRecordPath',$AuthorizationRecordPath,
  '-AuthorizationRecordSha256',$AuthorizationRecordSha256,'-CandidateWorktree',$CandidateWorktree,
  '-ExpectedPurpose','A5_ACCEPTANCE','-ExpectedRunId',$RunId.ToString('D'),'-DetachedResultPath',$detached,
  '-CalculatedCheckpointPath',$calculated)
 $vr=Invoke-Capture 'powershell.exe' $va $script:runDir $vo $ve
 if($vr.exitCode -ne 0){Stop-Formal 'INDEPENDENT_VERIFIER_FAILED' $candidate $null $null $null $null (Get-Evidence $vo) (Get-Evidence $ve) $null}
 $target=(Resolve-Path (Join-Path $root '..\..')).Path
 $checkpoint=[IO.Path]::GetFullPath((Join-Path $target ([string]$auth.checkpointPath)))
 $exact=[IO.Path]::GetFullPath((Join-Path $target 'outputs\rev869b_external_controller_phase_a_a5_revised_source_implementation_checkpoint.md'))
 if($checkpoint -cne $exact){Stop-Formal 'CHECKPOINT_PATH_MISMATCH' $candidate $null $null $null $null $null $null $null}
 [IO.File]::Copy($calculated,$checkpoint,$false)
}
$runnerState='FORMAL_EXECUTION_RECORDED_NOT_INDEPENDENTLY_VERIFIED'
if($Purpose -eq 'A5_ACCEPTANCE'){$runnerState='INDEPENDENT_VERIFIER_ARTIFACT_MATERIALIZED'}
[ordered]@{state=$runnerState;
 runId=$RunId.ToString('D').ToLowerInvariant();journalPath=$script:journal;journalHeadSha256=$script:previous;
 candidateManifestSha256=$candidate.manifestSha256;harnessSourceSha256=$candidate.harnessSourceSha256;
 authorizationRecordSha256=(Get-Sha256 $AuthorizationRecordPath)}|ConvertTo-Json -Depth 5
exit 0
