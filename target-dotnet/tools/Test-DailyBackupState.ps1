function Test-DailyBackupState {
    param([string]$StatusPath, [double]$MaxAgeHours=26, [bool]$TaskEnabled, [long]$TaskResult,
          [DateTimeOffset]$Now=[DateTimeOffset]::UtcNow)
    try {
        if (-not $TaskEnabled -or $TaskResult -ne 0) { throw "task disabled or unsuccessful (result $TaskResult)" }
        if ($MaxAgeHours -le 0 -or $MaxAgeHours -gt 26) { throw 'invalid daily freshness limit' }
        $receipt = Get-Content -LiteralPath $StatusPath -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
        if ($null -eq $receipt.ExitCode -or $receipt.ExitCode -ne 0 -or $receipt.OffMachineVerified -isnot [bool] -or -not $receipt.OffMachineVerified) { throw 'daily copy did not succeed' }
        if (-not $receipt.FinishedUtc) { throw 'missing completion time' }
        $age = ($Now - [DateTimeOffset]::Parse($receipt.FinishedUtc)).TotalHours
        if ($age -lt 0 -or $age -gt $MaxAgeHours) { throw 'daily receipt is future-dated or stale; missed daily copy' }
        return [pscustomobject]@{ Ok=$true; Detail=('ERP and identity off-machine verified; receipt {0:N1} h old' -f $age) }
    } catch { return [pscustomobject]@{ Ok=$false; Detail=$_.Exception.Message } }
}
