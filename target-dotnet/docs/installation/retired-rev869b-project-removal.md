# Retired REV869B project removal

The user's overnight instruction requested removal of ControlPlane, ControlPlane.Contracts, ControlPlane.Persistence and SecurityMigrations after checking references.

The active solution contains only Api, Application, Domain, Infrastructure, Installer and the main Tests project. Those projects and tools have no references to the retired projects. The reference scan did find two dependents outside the active solution: AcceptanceVerifier references ControlPlane.Contracts, and the old ControlPlane.Tests project references both ControlPlane and AcceptanceVerifier. They belong to the same retired deployment design and were removed together to avoid dangling project references.

Removed six directories containing 20 tracked files; ControlPlane.Persistence was empty. No untracked files or reparse points were present in those directories. All resolved deletion targets were verified inside the workspace before removal. Git retains the sources at 1fac0e1. The unrelated legacy-reference directory and the existing output TEX file were preserved.

Expected business/database row changes: zero. Active frontend routes, fields and envelopes: unchanged. No new tests were added for unreachable source removal. Post-removal builds passed: Release 27.72s and Debug 54.25s, both zero warnings/errors. Combined full routine Release passed 836/836, zero failures/skips, 23m42s, retained in report5-routine-release.trx. Focused report verification passed 3/3 in each configuration; a full Debug run is not claimed.

Source instructions: the final overnight request and Three_Day_Plan; direct solution, project-reference and active source/tools scans. Evidence: local-evidence/overnight-20260914/retired-project-removal.json.
