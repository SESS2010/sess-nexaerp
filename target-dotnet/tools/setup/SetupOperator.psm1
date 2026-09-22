Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Net.Http
Add-Type -AssemblyName System.Web
[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12

function Get-SetupSha([byte[]]$Bytes) {
 $sha=[Security.Cryptography.SHA256]::Create()
 try { return ([BitConverter]::ToString($sha.ComputeHash($Bytes))).Replace('-','').ToLowerInvariant() } finally {$sha.Dispose()}
}
function New-SetupRandom {
 $bytes=New-Object byte[] 32; $rng=[Security.Cryptography.RandomNumberGenerator]::Create()
 try {$rng.GetBytes($bytes)} finally {$rng.Dispose()}
 return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+','-').Replace('/','_')
}
function Get-SetupChallenge([string]$Verifier) {
 $sha=[Security.Cryptography.SHA256]::Create()
 try {return [Convert]::ToBase64String($sha.ComputeHash([Text.Encoding]::ASCII.GetBytes($Verifier))).TrimEnd('=').Replace('+','-').Replace('/','_')} finally {$sha.Dispose()}
}
function New-SetupHttp {
 $handler=[Net.Http.HttpClientHandler]::new();$handler.AllowAutoRedirect=$false
 $client=[Net.Http.HttpClient]::new($handler);$client.Timeout=[TimeSpan]::FromSeconds(60)
 return $client
}
function Invoke-SetupHttp($Client,[string]$Method,[string]$Uri,[string]$Token,[string]$Company,$Body,[string]$Key='', $Content=$null) {
 $request=[Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::new($Method),$Uri)
 try {
  if ($Token) {$request.Headers.Authorization=[Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$Token)}
  if ($Company) {$request.Headers.Add('X-NexaERP-Company',$Company)}
  if ($Key) {$request.Headers.Add('Idempotency-Key',$Key)}
  if ($Content) {$request.Content=$Content} elseif ($null -ne $Body) {$request.Content=[Net.Http.StringContent]::new(($Body | ConvertTo-Json -Depth 20 -Compress),[Text.Encoding]::UTF8,'application/json')}
  $response=$Client.SendAsync($request).GetAwaiter().GetResult()
  try {
   # Never echo provider errors or token-bearing request bodies/URLs.
   if (-not $response.IsSuccessStatusCode) {throw "HTTP $([int]$response.StatusCode); no automatic retry. Inspect approved server audit/logs and read current state."}
   return ,($response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult())
  } finally {$response.Dispose()}
 } finally {$request.Dispose()}
}
function Invoke-SetupJson($Client,[string]$Method,[string]$Uri,[string]$Token,[string]$Company,$Body,[string]$Key='') {
 $bytes=Invoke-SetupHttp $Client $Method $Uri $Token $Company $Body $Key
 return ([Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json)
}
function Read-SetupCallback([string]$RequestLine,[string]$ExpectedState,[string]$Issuer) {
 if ($RequestLine -notmatch '^GET (/callback/\?[^ ]+) HTTP/1\.[01]$') {throw 'Invalid loopback callback.'}
 $query=[Web.HttpUtility]::ParseQueryString(([uri]('http://127.0.0.1:8765'+$Matches[1])).Query)
 if (@($query.GetValues('state')).Count -ne 1 -or $query['state'] -cne $ExpectedState) {throw 'OAuth state mismatch.'}
 if ($query['error'] -or -not $query['code'] -or @($query.GetValues('code')).Count -ne 1) {throw 'Login did not return one authorization code.'}
 if ($query['iss'] -and $query['iss'] -cne $Issuer) {throw 'OAuth issuer mismatch.'}
 return $query['code']
}
function Connect-SetupEmployee($Client,[ValidateSet('staff','approvers')][string]$Realm) {
 $issuer='https://192.168.68.130:8444/realms/'+$Realm
 $clientId='nexaerp-'+$Realm; $redirect='http://127.0.0.1:8765/callback/'
 $discovery=Invoke-SetupJson $Client GET ($issuer+'/.well-known/openid-configuration') '' '' $null
 if ($discovery.issuer -cne $issuer -or $discovery.authorization_endpoint -cne ($issuer+'/protocol/openid-connect/auth') -or $discovery.token_endpoint -cne ($issuer+'/protocol/openid-connect/token')) {throw 'Unapproved OIDC discovery endpoints.'}
 $verifier=New-SetupRandom; $state=New-SetupRandom
 $parameters=[ordered]@{client_id=$clientId;redirect_uri=$redirect;response_type='code';scope='openid nexaerp/access';state=$state;code_challenge=(Get-SetupChallenge $verifier);code_challenge_method='S256';prompt='login';max_age='0'}
 $query=($parameters.GetEnumerator() | ForEach-Object {[uri]::EscapeDataString($_.Key)+'='+[uri]::EscapeDataString($_.Value)}) -join '&'
 $listener=[Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,8765)
 try {
  $listener.Start(); Write-Host 'Sign in as yourself in the browser. Approvers must complete the required MFA flow.'
  Start-Process ($discovery.authorization_endpoint+'?'+$query)
  $deadline=[DateTime]::UtcNow.AddMinutes(5)
  while (-not $listener.Pending()) {if ([DateTime]::UtcNow -gt $deadline) {throw 'Login timed out.'};Start-Sleep -Milliseconds 200}
  $socket=$listener.AcceptTcpClient();$socket.ReceiveTimeout=5000;$socket.SendTimeout=5000
  try {
   $stream=$socket.GetStream();$buffer=New-Object byte[] 8192;$length=0
   do {if ([DateTime]::UtcNow -gt $deadline) {throw 'Callback timed out.'};if ($length -ge $buffer.Length) {throw 'Callback headers too large.'};$n=$stream.Read($buffer,$length,1);if ($n -eq 0) {throw 'Incomplete callback.'};$length+=$n;$text=[Text.Encoding]::ASCII.GetString($buffer,0,$length)} while (-not $text.EndsWith("`r`n`r`n"))
   $code=Read-SetupCallback ($text.Split("`r`n")[0]) $state $issuer
   $reply=[Text.Encoding]::ASCII.GetBytes("HTTP/1.1 200 OK`r`nContent-Type: text/plain`r`nCache-Control: no-store`r`nConnection: close`r`nContent-Length: 40`r`n`r`nLogin received. Return to the setup CLI.")
   $stream.Write($reply,0,$reply.Length)
  } finally {$socket.Dispose()}
  $form=[Collections.Generic.Dictionary[string,string]]::new()
  foreach ($p in @{grant_type='authorization_code';client_id=$clientId;redirect_uri=$redirect;code=$code;code_verifier=$verifier}.GetEnumerator()) {$form.Add($p.Key,$p.Value)}
  $bytes=Invoke-SetupHttp $Client POST $discovery.token_endpoint '' '' $null '' ([Net.Http.FormUrlEncodedContent]::new($form))
  $tokens=[Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json
  if ($tokens.token_type -ine 'Bearer' -or -not $tokens.access_token) {throw 'No bearer access token returned.'}
  return $tokens.access_token
 } finally {$listener.Stop();$verifier=$null;$code=$null;$tokens=$null}
}
function Assert-SetupSession($Session,[string]$Employee,[string]$Company) {
 if ($Session.EmployeeCode -cne $Employee -or $Session.OrganizationId -cne $Company -or -not $Session.EmployeeId) {throw 'Resolved employee/company differs from expected operator; no write allowed.'}
}
function Get-SetupRows($Client,[string]$Base,[string]$Path,[string]$Token,[string]$Company) {
 $all=@();$page=1
 do {
  $separator='?';if ($Path.Contains('?')) {$separator='&'}
  $data=Invoke-SetupJson $Client GET ($Base+$Path+$separator+'page='+$page+'&pageSize=200') $Token $Company $null
  if ($null -eq $data) {return @()}
  if ($null -eq $data.PSObject.Properties['Items']) {return @($data)}
  $items=@($data.Items);$all+=$items
  if ($all.Count -ge $data.TotalCount) {break}
  if ($items.Count -eq 0 -or $page -ge 1000) {throw 'Pagination incomplete; refuse partial verification.'}
  $page++
 } while ($true)
 return $all
}
function Get-SetupDefinition([string]$Kind) {
 $config='/api/v1/rev869a/configuration/'
 switch ($Kind) {
  'ConditionLocations' {return @{Path=$config+'warehouse-condition-locations';Actions=@('Create','Read');Fields=@('OrganizationId','WarehouseCode','RackBinId','ConditionCode','EffectiveFrom','EffectiveTo','Remarks')}}
  'CategoryRoutes' {return @{Path=$config+'store-category-routes';Actions=@('Create','Read');Fields=@('OrganizationId','ItemCategoryCode','QcHoldConditionLocationId','PendingReturnConditionLocationId','DefaultAcceptedConditionLocationId','EffectiveFrom','EffectiveTo','Remarks')}}
  'TaxRules' {return @{Path=$config+'tax-gst';Actions=@('Create','Approve','Read');Fields=@('OrganizationId','JurisdictionCode','HsnSacCode','SupplierStateCode','PlaceOfSupplyStateCode','VendorRegistrationType','GstRate','CgstRate','SgstRate','IgstRate','CessRate','IsExempt','IsReverseCharge','CurrencyCode','RoundingScale','EffectiveFrom','EffectiveTo','Remarks','SupersedesTaxGstSettingId','ItcEligibility','RecoverableTaxPercent')}}
  'VendorQualifications' {return @{Path=$config+'vendor-qualifications';Actions=@('Create','Verify','Approve','Read');Fields=@('OrganizationId','VendorCode','ItemCategoryCode','QualificationCode','EffectiveFrom','EffectiveTo','Remarks')}}
  'Warehouses' {return @{Path='/api/v1/inventory/warehouses';Actions=@('Template','Import','Submit','Approve','Read');Master='warehouses'}}
  'RackBins' {return @{Path='/api/v1/inventory/rack-bins';Actions=@('Template','Import','Submit','Approve','Read');Master='rack-bins'}}
  'Identities' {return @{Path=$config+'employee-identities';Actions=@('Create','Read');Fields=@('OrganizationId','Issuer','Subject','EmployeeCode','IdentityType','EffectiveFrom','EffectiveTo','Remarks')}}
  'Scopes' {return @{Path=$config+'operational-scopes';Actions=@('Create','Read');Fields=@('OrganizationId','EmployeeCode','DepartmentCode','WarehouseCode','RackBinId','OwnRecordsOnly','AllowsPrivilegedCrossScope','EffectiveFrom','EffectiveTo','Remarks')}}
  default {throw 'Unknown setup kind.'}
 }
}
function Assert-SetupPlan($Plan,$Definition) {
 if ($Plan.Company -cnotin @('SESS_PROPRIETORSHIP','SESS_PVT_LTD')) {throw 'Unknown company.'}
 if ($Plan.TargetDatabase -cnotin @('sess_nexa_erp_DEMO','sess_nexa_erp')) {throw 'Unknown target database.'}
 if ($Plan.Action -cnotin $Definition.Actions) {throw 'Action not allowed for this setup kind.'}
 if ($Plan.Action -in @('Read','Template')) {return}
 if (@($Plan.Rows).Count -eq 0) {throw 'No input rows.'}
 $ids=@{}
 foreach ($row in $Plan.Rows) {
  $id=[guid]::Parse($row.OperationId).ToString();if ($id -eq [guid]::Empty.ToString()) {throw 'Empty operation ID.'};if ($ids.ContainsKey($id)) {throw 'Duplicate operation ID.'};$ids[$id]=$true
  if ($Plan.Action -eq 'Import') {continue}
  $allowed=@('Version','Remarks');if ($Plan.Kind -in @('TaxRules','VendorQualifications')) {$allowed=@('ExpectedVersion','Remarks')}
  if ($Plan.Action -eq 'Create') {$allowed=$Definition.Fields}
  foreach ($property in $row.Body.PSObject.Properties) {if ($property.Name -cnotin $allowed) {throw "Unexpected body field: $($property.Name)"}}
  if ([string]::IsNullOrWhiteSpace($row.Body.Remarks)) {throw 'Remarks required.'}
  if ($Plan.Action -eq 'Create') {
   foreach ($field in $Definition.Fields) {if ($null -eq $row.Body.PSObject.Properties[$field]) {throw "Missing template field: $field (use explicit null for optional fields)."}}
   foreach ($property in $row.Body.PSObject.Properties) {
    if ($property.Name.EndsWith('Id') -and $property.Name -ne 'OrganizationId' -and $null -ne $property.Value) {if ([guid]::Parse($property.Value) -eq [guid]::Empty) {throw 'Empty reference ID.'}}
   }
   if ($Plan.Kind -eq 'TaxRules') {
    foreach ($field in @('GstRate','CgstRate','SgstRate','IgstRate','CessRate','RoundingScale')) {if ($null -eq $row.Body.$field -or $row.Body.$field -is [string]) {throw "Numeric JSON value required: $field"}}
    foreach ($field in @('IsExempt','IsReverseCharge')) {if ($row.Body.$field -isnot [bool]) {throw "Boolean JSON value required: $field"}}
   }
   $from=[datetime]::ParseExact($row.Body.EffectiveFrom,'yyyy-MM-dd',[Globalization.CultureInfo]::InvariantCulture)
   if ($row.Body.EffectiveTo -and [datetime]::ParseExact($row.Body.EffectiveTo,'yyyy-MM-dd',[Globalization.CultureInfo]::InvariantCulture) -lt $from) {throw 'Invalid effective range.'}
   if ($Plan.Kind -in @('CategoryRoutes','VendorQualifications') -and $row.Body.ItemCategoryCode -cnotin @('ELE','FAB','REF')) {throw 'Canonical category required.'}
   if ($Plan.Kind -eq 'Scopes' -and [string]::IsNullOrWhiteSpace($row.Body.DepartmentCode)) {throw 'An explicitly assigned department is required.'}
   if ($row.Body.OrganizationId -cne $Plan.Company) {throw 'Body company mismatch.'}
   if ($Plan.Kind -eq 'Scopes' -and $row.Body.AllowsPrivilegedCrossScope -ne $false) {throw 'Cross-scope privilege prohibited.'}
   if ($Plan.Kind -eq 'Identities' -and ($row.Body.IdentityType -cne 'HUMAN' -or $row.Body.Issuer -cnotin @('https://192.168.68.130:8444/realms/staff','https://192.168.68.130:8444/realms/approvers'))) {throw 'Only verified human identities from approved issuers.'}
  } else {
   if ($Plan.Kind -eq 'Warehouses') {if ($row.RecordId -notmatch '\A[A-Za-z0-9_-]{1,80}\z') {throw 'Warehouse business code required.'}} else {$null=[guid]::Parse($row.RecordId)}
   $version='Version';if ($Plan.Kind -in @('TaxRules','VendorQualifications')) {$version='ExpectedVersion'}
   if ($null -eq $row.Body.PSObject.Properties[$version] -or [string]$row.Body.$version -notmatch '\A\d+\z') {throw 'Explicit current version required.'}
  }
 }
}
function Assert-SetupReadback($Plan,$Row,$Actual) {
 if (-not $Actual) {throw 'Read-back row missing.'}
 if ($Plan.Action -eq 'Create') {
  foreach ($p in $Row.Body.PSObject.Properties) {
   if ($p.Name -in @('Remarks','OrganizationId','Subject')) {continue}
   if ($null -eq $Actual.PSObject.Properties[$p.Name]) {throw "Read-back field absent: $($p.Name)"}
   if ([string]$Actual.($p.Name) -cne [string]$p.Value) {throw "Read-back differs: $($p.Name)"}
  }
  if ($Plan.Kind -eq 'Identities') {
   $expected=Get-SetupSha ([Text.Encoding]::UTF8.GetBytes($Row.Body.Subject))
   if ($Actual.SubjectSha256 -ine $expected) {throw 'Read-back identity subject hash mismatch.'}
  }
 } elseif ($Plan.Action -eq 'Verify') {
  if ($Actual.VerificationStatus -cne 'Verified') {throw 'Qualification not Verified.'}
 } elseif ($Plan.Action -eq 'Approve') {
  if ($Actual.ApprovalStatus -cne 'Approved') {throw 'Approval not effective.'}
 } elseif ($Plan.Action -eq 'Submit') {
  if ($Actual.ApprovalStatus -cne 'Pending Approval') {throw 'Submission not pending approval.'}
 }
 if ($Plan.Action -in @('Approve','Verify','Submit')) {
  $v='Version';if ($Plan.Kind -in @('TaxRules','VendorQualifications')) {$v='ExpectedVersion'}
  if ([uint64]$Actual.Version -le [uint64]$Row.Body.$v) {throw 'Decision did not advance version.'}
 }
}
Export-ModuleMember -Function *-Setup*
