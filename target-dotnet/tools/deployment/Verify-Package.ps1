[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][string]$Root,
 [Parameter(Mandatory=$true)][string]$ExpectedHead,
 [Parameter(Mandatory=$true)][string]$ExpectedManifestSha256
)
$ErrorActionPreference='Stop'
$Root=[IO.Path]::GetFullPath($Root).TrimEnd('\')
$current=$Root
while ($current) {
 if ((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Package ancestors cannot be reparse points.' }
 $current=[IO.Path]::GetDirectoryName($current)
}
$items=Get-ChildItem -LiteralPath $Root -Recurse -Force
foreach ($item in $items) { if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse points are forbidden.' } }
$manifestPath=Join-Path $Root 'MANIFEST'
if ((Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash -ne $ExpectedManifestSha256) { throw 'MANIFEST hash differs from the separately supplied digest.' }
$manifest=Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.HeadSha -cne $ExpectedHead) { throw 'Unexpected source HEAD.' }
$expected=@{}
foreach ($entry in $manifest.Files) {
 $relative=[string]$entry.Path
 if ([IO.Path]::IsPathRooted($relative) -or $relative -match '(^|[\\/])\.\.([\\/]|$)' -or $relative.Contains(':')) { throw 'Unsafe manifest path.' }
 $path=[IO.Path]::GetFullPath((Join-Path $Root $relative))
 if (-not $path.StartsWith($Root+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Path escaped package root.' }
 if ($expected.ContainsKey($path) -or $relative -eq 'MANIFEST') { throw 'Duplicate or self-referential manifest entry.' }
 $expected[$path]=$true
 $item=Get-Item -LiteralPath $path -Force
 if ($item.PSIsContainer -or $item.Length -ne $entry.Length -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $entry.Sha256) { throw "Payload mismatch: $relative" }
}
$items=Get-ChildItem -LiteralPath $Root -Recurse -Force
if ((Get-Item -LiteralPath $Root -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Package root is a reparse point.' }
foreach ($item in $items) {
 if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse points are forbidden.' }
 if (-not $item.PSIsContainer -and $item.FullName -ne $manifestPath -and -not $expected.ContainsKey($item.FullName)) { throw "Unlisted payload: $($item.Name)" }
}
Write-Output "VERIFIED: $($expected.Count) files; HEAD $ExpectedHead; MANIFEST $ExpectedManifestSha256"
