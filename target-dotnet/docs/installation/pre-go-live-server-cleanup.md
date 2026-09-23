# E — server cleanup status and pending TD decisions

Updated 22 September 2026 from the Technical Director's consolidated instruction.
This replaces the previous cleanup checklist and its removal commands. **No further
product removal is authorized here.** This is not a claim of independent server access.

| Item | Current status | Read-only verification / outstanding receipt |
|---|---|---|
| NI and Siemens on MAGESHWARI/server | **Reported completed by the server agent**: licence checks first, vendor uninstallers, then approved leftovers. | Obtain dated operator receipt listing products/versions and licence disposition. Inspect Apps & features/uninstall registry and remaining service names/paths; confirm no NI/Siemens autostart or unintended remnants. Do not use Win32_Product, reinstall, restore or run laptop maintenance scripts there. |
| SOLIDWORKS, Electrical Collaborative Server / ewserver, SQL SESS_SQLEXPRESS and TEW_SQLEXPRESS, SQLBrowser and ~77 design databases | **Retained and protected.** | Verify service state and representative engineering project access with the engineering owner. Never stop, disable, alter or delete to release memory/ports. |
| Suspicious SolidWorks Flexnet Server service whose executable folder is named for a software-piracy site | **Server agent reports removal and Defender full scan in progress. Completion receipt pending.** This specific investigation does not authorize removing SOLIDWORKS or legitimate licensing services. | Obtain exact service/path identified by server agent, removal result, scan completion timestamp and detections/disposition. Read back service absence and Defender scan result on that machine. Folder naming alone is not independent proof of malware; no clean-machine claim before receipt. |
| Rockwell, IIS Default Web Site and /Updater | Retained and protected. | Read back service/site states and the existing application check. No new IIS binding takeover. |
| Wampserver | Leave Apache/MySQL/MariaDB stopped and Manual. | Read state/start mode only. |
| All other products, including editors/agents/Node/Git/remote-support products/accounts | **Pending server inventory and TD KEEP/REMOVE decisions.** Prior proposed removals are withdrawn. | Inventory exact publisher/version/scope and service/task dependencies; TD signs decisions later. No uninstall, account/credential/config deletion or ACL procedure is supplied at this stage. |

## Failing HDD — explicit TD trial exception

D: and E: remain physically connected **during the TD-approved trial**, despite the
reported failing HDD. This supersedes a disconnection requirement; it does not make
the disk trustworthy. **Nothing ERP-related may ever use D: or E:** no database,
WAL, backup, verification, staging, logs, certificates, installer temp or exports.
Do not scan, move, remove or change unrelated files there as part of ERP commissioning.

The server agent's daily disk watch records System events from Disk, NTFS and WHEA
with timestamp, provider, event ID, disk identity and counts; save its evidence on C:
or the approved off-machine receiver, never the failing disk. Retain a daily receipt,
including an explicit zero-events result or a visible missed-watch failure. Correlate
new events to physical disk, notify TD on new/worsening errors, and obtain TD's next
trial decision. A quiet event log is not proof the disk is healthy. Obtain the watch schedule/installation receipt, first daily result, reviewer and
trial end/review date from the server agent; do not reinstall an already-running watch.

## Current never-touch boundary

Protect SQL Server/engineering databases, SOLIDWORKS Electrical, retained legitimate
licensing, Rockwell, IIS Default Web Site and /Updater, and Wamp's existing stopped/
Manual configuration. No clean Windows installation. NI and Siemens are now reported
removed: remove them from current retained-service lists and **do not reinstall**.
The specific suspicious service remediation above remains with the server agent.
Nothing in this document performs a server change. Historical 21 September inventories
remain dated observations, not instructions to resurrect removed software.
