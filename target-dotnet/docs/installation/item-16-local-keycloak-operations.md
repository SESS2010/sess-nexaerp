# Item 16: local Keycloak deployment and recovery

Decision: Technical Director, 15 September 2026, after witnessing fc6de74 and live migration 78 to 103. Item 15 is accepted. Local Keycloak is now the SESS default; the provider-agnostic validator and Cognito profile remain supported. Earlier text that says production awaits Cognito is superseded by this decision.

## Placement and ownership

For the first month, use a dedicated Linux VM on the ERP laptop, initially 2 vCPUs and 4 GB RAM, subject to a measured login/load check. The inspected host has 32 GB RAM, Windows 10 Pro, firmware virtualization enabled and no active hypervisor. Enabling Hyper-V/rebooting is deployment work, not part of a test run. The portable emulated Alpine witness is not a production appliance.

For the first month, run production-mode Keycloak in the Linux VM and a dedicated PostgreSQL 17 instance on the Windows host, containing only the keycloak application database plus PostgreSQL system databases. Give it its own data directory, Windows service, service account and port (the example uses 55432). Do not add identity tables to sess_nexa_erp or use the ERP PostgreSQL instance, and do not give the ERP runtime account access to identity data. Permit database connections only from the identity VM and the backup account/host through reviewed firewall and pg_hba rules. Separate instances isolate schema upgrades, service credentials, backup identity and recovery.

This refines the initial suggestion of placing PostgreSQL inside the Linux VM: the witnessed recovery uses Windows PostgreSQL and the existing Windows Installer/Task Scheduler tooling, so that is the supported first-month handoff. A Linux PostgreSQL deployment needs its own restore rehearsal before selection. Same-laptop placement still shares power/disk failure; later move the identity services to a separate server while retaining the stable LAN DNS name and TLS issuer URLs and repeating backup/restore acceptance on the chosen database platform.

The site IT maintainer installs the VM, production services, LAN DNS, trusted TLS, firewall and local time synchronization. The hostname examples use HTTPS port 443; Keycloak listens on 8443 by default, so IT must configure the reviewed 443-to-8443 mapping or TLS proxy before using those URLs. Do not expose the database or management port to ordinary LAN clients. Suranther administers accounts and checks operations status. The ERP identity administrator performs governed mapping creation/revocation; provider administration alone cannot grant ERP access. The Technical Director approves maintenance windows and role changes. Each customer must nominate the IT maintainer; no automatic unattended Keycloak version upgrades.

## Realms and login policy

Use staff and approvers realms, each with its own public authorization-code/S256 PKCE client. Import the credential-free templates in this directory's keycloak subdirectory only after replacing the ERP callback/origin placeholders with the actual frontend route. There are no users or test passwords in these templates. Disable registration, password grants, implicit flow and service accounts for these interactive clients.

Approvers uses a required password execution followed by a required OTP execution and default Configure OTP enrollment. Staff uses optional OTP. A realm's OTP policy alone does not make OTP mandatory: the bound browser flow and client overrides must be inspected. Do not configure a client override or broker/direct-grant path that bypasses the Approvers flow. The ERP trusts the configured issuer's MFA guarantee, so provider administration is security-sensitive.

Keycloak differs from Cognito: flows can enforce conditional MFA inside a realm, including by role; two realms are the chosen design, not a Keycloak limitation. TECHNICAL_DIRECTOR, MANAGING_DIRECTOR, ACCOUNTS_MANAGER and CHIEF_FINANCIAL_OFFICER must use Approvers. ERP refuses all access in the affected company with MFA_REQUIRED if their active mapping resolves through Staff. Promotion requires creating the Approvers account, verifying its signed-token subject, revoking the old ERP mapping and creating the new mapping; never rewrite identity history.

Minimum password length is 14. New accounts receive a temporary password with first-login change. Access-token lifetime is 900 seconds; client and realm session idle/max lifetimes are bounded at 28,800 seconds. Do not request offline_access or enable Remember Me. The OIDC validator continues to check configured issuer, audience/client claim, token-use claim, subject, scope and signing keys. Use authentication.keycloak.example.json for ERP configuration; retain authentication.cognito.example.json for customers choosing Cognito.

## Backup and restore

Register a second daily task through the existing Register-VerifiedDatabaseBackup.ps1 and Invoke-VerifiedDatabaseBackup.ps1, using keycloak/backup.identity.example.json, its own DPAPI-protected connection, roots and task name SESS-NexaERP-Identity-VerifiedBackup. Schedule it after the ERP task (initially ERP 02:00, identity 02:30; measure contention before deployment). Both tasks must be successful and fresh for site backup status to be green. An ERP backup alone is not a site recovery backup. The two databases have separate snapshot times, not one atomic site snapshot. Keep account, role and mapping administration outside the backup window; after recovery, reconcile active ERP mappings with restored provider subjects and approved access before enabling users.

The same Installer backup run/verify/recover commands handle either source. Source host, port, database and PostgreSQL system identifier remain mandatory. PostgreSQL 17 tools and the original OID-10 administrator identity are retained. Keycloak user password hashes, TOTP credentials and realm signing keys are database data and ARE retained; omitted pg_dumpall role passwords are PostgreSQL service credentials, not user credentials.

ConfigurationFiles explicitly lists the authoritative deployment configuration, certificate/key and version record. Keep that protected deployment directory synchronized with the installed VM at each reviewed deployment. Do not rotate deployment files during backup. Format-2 bundles hash those files with database.dump and globals.sql, check them through restore verification, and copy them as configuration-* files into a fresh recovery directory. They are never applied over running configuration automatically. Existing format-1 ERP bundles remain readable and retain their original checks. Unexpected or duplicate file names, path traversal, reparse points and changed backup bytes are refused.

The destination must be independent external/network storage with appropriate access controls and encryption configured by IT. Backup bundles contain authentication secrets. Filesystem restrictions alone are not encryption and a copy on the laptop disk is not disaster recovery. Retention remains 30 daily days, 84 weekly days and protection of the newest two verified bundles. Daily backups can lose changes since the last usable backup; they are not point-in-time recovery.

After loss, recover into a NEW directory/cluster. Re-establish only database service credentials, restore the reviewed Keycloak version/configuration and TLS identity, then start Keycloak against the recovered identity database. Confirm the same employee subjects, a Staff password login, an Approver password-plus-TOTP login and ERP mapping resolution. Do not re-import templates over restored realms: restoring the database preserves their identities and keys. Realm JSON export is not a complete database backup.

The PostgreSQL-backed scheduled-backup recovery witness passed in Debug and Release with the original identity database unavailable during recovered login. Both full routine suites passed with TRX: Debug 844/844 in 24.606 measured minutes; Release 841/841 in 26.730 minutes, zero failed/skipped. See item-16-identity-recovery-findings.md for evidence, the two corrected defects and configuration-fixture/platform limitations. The owner accepted backend recovery; actual site installation remains outstanding.

## Failure detection and upgrades

New logins, token refresh, password changes and OTP enrollment stop if Keycloak is unavailable. Existing ERP access tokens can work only for their remaining 15 minutes plus configured skew with usable cached signing keys and current ERP authorization. An eight-hour refresh/session lifetime is not eight hours offline. Internet loss alone should not prevent local login; DNS, certificate validation, LAN connectivity and time synchronization must also work locally.

Test-IdentityOperations.ps1 checks readiness including a database health check, both realm discovery/JWKS endpoints, and ERP plus identity last-run.json freshness. The supplied profile also checks each Task Scheduler result, so a task failure that could not update its status file cannot leave the site green. Enable health and metrics in Keycloak; database readiness is absent without the relevant metrics support. Use Register-IdentityOperationsMonitor.ps1 with keycloak/monitor.example.json to register a one-minute task under the operational account. That account needs read access to both protected backup result files, permission to inspect both named backup tasks, and write access to the protected status directory; grant the operational display read access only to the status output. Keep management endpoints private. This is readiness/configuration monitoring, not a synthetic password/TOTP login.

Open identity-status.html on the operational display and leave it open during working hours. It refreshes every 30 seconds and marks itself stale after two minutes without a successful monitor update. Failures return nonzero for Task Scheduler and produce ACTION_REQUIRED. No email/SMS is sent. A monitor hosted only on the laptop cannot independently detect complete laptop loss; run a second LAN monitor/display on another machine if that assurance is required.

The designated IT maintainer performs upgrades from an ERP-maintainer-reviewed pinned release. Read intervening release notes, take and verify identity/configuration backups, restore them to staging, upgrade staging, and test login, MFA, mappings, health and backup/recovery before the maintenance window. Upgrade production only after that evidence is accepted. Rollback requires the previous Keycloak installation AND the pre-upgrade database; do not point old binaries at an upgraded schema. Preserve realm issuer URLs.

## Deployment boundaries

This repository supplies backend validation, configuration templates, backup tooling, monitoring and a click checklist. It does not yet supply a production Hyper-V image or a Windows setup wizard. IT must install the dedicated Windows identity database service as well as the Keycloak VM. The existing development frontend still needs the production PKCE/provider-selection and governed identity-administration integration described in item-16-frontend-contract.md; do not present those screens as installed.

Site hostname, actual frontend callback path, independent backup destination, TLS trust and the named maintainer must be filled before deployment. No owner database is changed by this work. Expected ERP business-row changes: zero. Expected identity rows arise only from deliberate account creation. No ERP HTTP route or response contract changes are needed for this operations package.

## Sources

- https://www.keycloak.org/server/configuration-production
- https://www.keycloak.org/server/db
- https://www.keycloak.org/docs/latest/server_admin/
- https://www.keycloak.org/server/importExport
- https://www.keycloak.org/observability/health
- https://www.keycloak.org/docs/latest/upgrading/

See suranther-local-keycloak-click-list.txt for the single forwardable operations block. The frozen schema remains authoritative; Answers_To_Open_Questions corrects older specifications, and the owner's explicit 15 September instructions update priority and the stage-signature decision.
