<#
.SYNOPSIS
Witness for tools/Show-RunInProgress.ps1 with a dummy run on this laptop. Never restarts anything.

.DESCRIPTION
Automated (default). Starts a dummy "run" process and the signal beside it, then proves each
mechanism without a real restart:
  1. the signal window exists and is top-most on the desktop, and a screenshot of the banner is saved;
  2. the window answers WM_QUERYENDSESSION with FALSE, which is the answer that makes Windows stop on
     its "These apps are preventing restart" screen (the message is only sent to that one window;
     no session actually ends);
  3. the window holds a registered shutdown-block reason (read back where Windows allows it);
  4. when the dummy run ends, the signal closes itself within a minute.
It writes a receipt to -EvidenceDirectory.

-Interactive. For the Technical Director: starts a 5-minute dummy run with the signal and asks the
person to choose Start > Power > Restart, read the screen, and press Cancel. The script then checks
the dummy run survived. This is the witness of the actual Windows screen, which no script can press.
#>
param(
    [string]$EvidenceDirectory = (Join-Path (Split-Path $PSScriptRoot -Parent) ("local-evidence\run-in-progress-signal\" + (Get-Date -Format 'yyyyMMdd-HHmmss'))),
    [switch]$Interactive
)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $EvidenceDirectory | Out-Null
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
if (-not ('SessSignalProbe' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class SessSignalProbe
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool ShutdownBlockReasonQuery(IntPtr hWnd, StringBuilder buffer, ref uint size);
    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int index);
    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);
}
'@
}

$signal = Join-Path $PSScriptRoot 'Show-RunInProgress.ps1'
$minutes = if ($Interactive) { 5 } else { 3 }
$dummy = Start-Process powershell.exe -PassThru -WindowStyle Hidden -ArgumentList @('-NoProfile', '-Command', "Start-Sleep -Seconds $($minutes * 60)")
$progress = Join-Path $EvidenceDirectory 'dummy-progress.log'
"dummy gate running $(Get-Date -Format o)" | Set-Content $progress -Encoding utf8
# One quoted argument string: Windows PowerShell 5.1 does not quote array elements containing spaces.
$helper = Start-Process powershell.exe -PassThru -WindowStyle Hidden -ArgumentList (
    '-NoProfile -ExecutionPolicy Bypass -File "{0}" -RunProcessId {1} -RunName "DUMMY witness run" -ProgressLog "{2}"' -f $signal, $dummy.Id, $progress)

$handle = [IntPtr]::Zero
for ($i = 0; $i -lt 60 -and $handle -eq [IntPtr]::Zero; $i++) {
    Start-Sleep -Milliseconds 500
    $p = Get-Process -Id $helper.Id -ErrorAction SilentlyContinue
    if ($p) { $p.Refresh(); $handle = $p.MainWindowHandle }
}
$receipt = [ordered]@{
    Computer = $env:COMPUTERNAME; Started = (Get-Date -Format o); Mode = $(if ($Interactive) { 'interactive' } else { 'automated' })
    DummyRunPid = $dummy.Id; SignalPid = $helper.Id; WindowFound = ($handle -ne [IntPtr]::Zero)
}

if ($handle -ne [IntPtr]::Zero) {
    $receipt.WindowTitle = (Get-Process -Id $helper.Id).MainWindowTitle
    $receipt.WindowVisible = [SessSignalProbe]::IsWindowVisible($handle)
    $receipt.TopMost = ((([SessSignalProbe]::GetWindowLong($handle, -20)) -band 0x8) -ne 0)

    Start-Sleep -Seconds 2
    $bounds = [Windows.Forms.Screen]::PrimaryScreen.Bounds
    $shot = New-Object Drawing.Bitmap($bounds.Width, 120)
    $g = [Drawing.Graphics]::FromImage($shot)
    $g.CopyFromScreen($bounds.Left, $bounds.Top, 0, 0, $shot.Size)
    $g.Dispose()
    $shotPath = Join-Path $EvidenceDirectory 'banner.png'
    $shot.Save($shotPath, [Drawing.Imaging.ImageFormat]::Png); $shot.Dispose()
    $receipt.Screenshot = $shotPath

    $buffer = New-Object Text.StringBuilder 512; [uint32]$size = 512
    $receipt.ReasonReadBack = [SessSignalProbe]::ShutdownBlockReasonQuery($handle, $buffer, [ref]$size)
    $receipt.ReasonText = $buffer.ToString()
    if (-not $receipt.ReasonReadBack) {
        $receipt.ReasonReadBackNote = "Windows refused the cross-process read (error $([Runtime.InteropServices.Marshal]::GetLastWin32Error())); the Interactive witness shows the text on the real screen."
    }

    if (-not $Interactive) {
        # WM_QUERYENDSESSION = 0x11, lParam ENDSESSION_CLOSEAPP-free (0 = shutdown/restart query).
        $answer = [IntPtr]::Zero
        $sent = [SessSignalProbe]::SendMessageTimeout($handle, 0x11, [IntPtr]::Zero, [IntPtr]::Zero, 0x2, 5000, [ref]$answer)
        $receipt.QueryEndSessionDelivered = ($sent -ne [IntPtr]::Zero)
        $receipt.QueryEndSessionAnswer = [int64]$answer
        $receipt.BlocksRestart = ($sent -ne [IntPtr]::Zero -and $answer -eq [IntPtr]::Zero)
    }
}

if ($Interactive) {
    Write-Host ''
    Write-Host 'The red banner is at the top of the screen. Now, within 4 minutes:' -ForegroundColor Yellow
    Write-Host '  1. Start > Power > Restart.'
    Write-Host '  2. Windows should stop on "These apps are preventing restart" naming the NexaERP run.'
    Write-Host '  3. Press CANCEL. Do NOT press "Restart anyway".'
    Write-Host ''
    $seen = Read-Host 'Did the screen name the NexaERP witness run? (yes/no)'
    $receipt.PersonSawRunNamedOnRestartScreen = ($seen -match '^y')
    $receipt.DummyRunSurvivedCancel = [bool](Get-Process -Id $dummy.Id -ErrorAction SilentlyContinue)
}

Stop-Process -Id $dummy.Id -Force -ErrorAction SilentlyContinue
$closedAfter = $null
for ($s = 0; $s -lt 75; $s++) {
    if (-not (Get-Process -Id $helper.Id -ErrorAction SilentlyContinue)) { $closedAfter = $s; break }
    Start-Sleep -Seconds 1
}
$receipt.SignalClosedAfterRunEnded = ($null -ne $closedAfter)
$receipt.SignalClosedWithinSeconds = $closedAfter
if ($null -eq $closedAfter) { Stop-Process -Id $helper.Id -Force -ErrorAction SilentlyContinue }
$receipt.Finished = (Get-Date -Format o)

$pass = $receipt.WindowFound -and $receipt.WindowVisible -and $receipt.TopMost -and $receipt.SignalClosedAfterRunEnded -and
    ($(if ($Interactive) { $receipt.PersonSawRunNamedOnRestartScreen -and $receipt.DummyRunSurvivedCancel } else { $receipt.BlocksRestart }))
$receipt.Result = $(if ($pass) { 'PASS' } else { 'FAIL' })
$receipt | ConvertTo-Json | Set-Content (Join-Path $EvidenceDirectory 'receipt.json') -Encoding utf8
$receipt | ConvertTo-Json
if (-not $pass) { exit 1 }
