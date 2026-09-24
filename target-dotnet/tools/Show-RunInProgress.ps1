<#
.SYNOPSIS
Run-in-progress signal (Part 0 of docs/installation/harness-resilience-proposal.md).
While a witness or acceptance run is in flight, a restart or shutdown of this laptop stops on the
Windows screen that names the run, and a banner stays on top of the desktop.

.DESCRIPTION
Started BESIDE a run, never inside it: the run's gate logic, TRX, freeze and evidence are not
touched. This process only reads the run's progress log.

1. Registers a Windows shutdown-block reason on its window. Choosing Restart or Shut down stops on
   the Windows screen "These apps are preventing restart", showing the reason text, with
   "Cancel" and "Restart anyway". It warns and does not forbid; that choice stays with the person.
2. Shows a small always-on-top banner at the top of the screen, refreshed every 30 seconds from the
   last line of the progress log, with the elapsed time.
3. Closes itself when the run's process ends, however it ends, so a crashed run never leaves a
   stale banner that teaches people to ignore it.

It does NOT cover a power cut, a forced Windows Update restart (Windows bypasses block reasons for
those) or a restart from another session. It needs an interactive desktop: a run started by a
scheduled task in session 0 has no screen to show it on.

.EXAMPLE
# Pass ONE argument string: Windows PowerShell 5.1 does not quote array elements containing spaces.
Start-Process powershell.exe -WindowStyle Hidden -ArgumentList ('-NoProfile -ExecutionPolicy Bypass -File "{0}" -RunProcessId {1} -RunName "UTC converter acceptance" -ProgressLog "{2}"' -f "$repo\tools\Show-RunInProgress.ps1", $PID, "$evidence\progress.log")
#>
param(
    [Parameter(Mandatory = $true)][int]$RunProcessId,
    [Parameter(Mandatory = $true)][string]$RunName,
    [string]$ProgressLog
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing

if (-not ('SessRunInProgressForm' -as [type])) {
    Add-Type -ReferencedAssemblies System.Windows.Forms, System.Drawing -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class SessRunInProgressForm : Form
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool ShutdownBlockReasonCreate(IntPtr hWnd, string reason);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShutdownBlockReasonDestroy(IntPtr hWnd);

    private const int WM_QUERYENDSESSION = 0x0011;
    private string reason = "";
    public bool ReasonRegistered { get; private set; }

    public void SetReason(string text)
    {
        reason = text;
        if (IsHandleCreated) ReasonRegistered = ShutdownBlockReasonCreate(Handle, reason);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (reason.Length > 0) ReasonRegistered = ShutdownBlockReasonCreate(Handle, reason);
    }

    protected override void WndProc(ref Message m)
    {
        // Answering FALSE is what makes Windows stop on its "preventing restart" screen and show
        // the reason. The person can still choose "Restart anyway".
        if (m.Msg == WM_QUERYENDSESSION) { m.Result = IntPtr.Zero; return; }
        base.WndProc(ref m);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (IsHandleCreated) ShutdownBlockReasonDestroy(Handle);
        base.OnFormClosed(e);
    }
}
'@
}

$started = Get-Date
function Get-RunState {
    $last = ''
    if ($ProgressLog -and (Test-Path -LiteralPath $ProgressLog)) {
        $line = Get-Content -LiteralPath $ProgressLog -Tail 1 -ErrorAction SilentlyContinue
        if ($line) { $last = ($line -replace '\s+\d{4}-\d{2}-\d{2}T\S+$', '').Trim() }
    }
    $minutes = [int]((Get-Date) - $started).TotalMinutes
    if ($last) { return "$RunName - $last - $minutes min" }
    return "$RunName - $minutes min"
}
function Test-RunAlive { return [bool](Get-Process -Id $RunProcessId -ErrorAction SilentlyContinue) }

if (-not (Test-RunAlive)) { exit 0 }

$form = New-Object SessRunInProgressForm
$form.Text = "NexaERP run in progress - $RunName"
$form.FormBorderStyle = 'None'
$form.TopMost = $true
$form.ShowInTaskbar = $true
$form.StartPosition = 'Manual'
$form.BackColor = [Drawing.Color]::FromArgb(176, 32, 32)
$screen = [Windows.Forms.Screen]::PrimaryScreen.WorkingArea
$form.Size = New-Object Drawing.Size([Math]::Min(900, $screen.Width), 44)
$form.Location = New-Object Drawing.Point(($screen.Left + [int](($screen.Width - $form.Width) / 2)), $screen.Top)

$label = New-Object Windows.Forms.Label
$label.Dock = 'Fill'
$label.ForeColor = [Drawing.Color]::White
$label.Font = New-Object Drawing.Font('Segoe UI', 11, [Drawing.FontStyle]::Bold)
$label.TextAlign = 'MiddleCenter'
$form.Controls.Add($label)

function Update-Signal {
    $state = Get-RunState
    $label.Text = "WITNESS RUN IN PROGRESS - DO NOT RESTART OR SHUT DOWN`n$state"
    # The reason text is what Windows shows on the restart screen.
    $form.SetReason("NexaERP witness run in progress: $state. Restarting now loses the run.")
}
Update-Signal

$timer = New-Object Windows.Forms.Timer
$timer.Interval = 30000
$timer.Add_Tick({
    if (-not (Test-RunAlive)) { $timer.Stop(); $form.Close(); return }
    Update-Signal
})
$timer.Start()
[Windows.Forms.Application]::Run($form)
