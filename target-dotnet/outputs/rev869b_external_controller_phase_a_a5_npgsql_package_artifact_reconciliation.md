# REV869B Phase-A A5 Npgsql Package-Artifact Identity Reconciliation

Date: 2026-08-21
Mode: report-only, offline
Decision: `A5_NPGSQL_PACKAGE_ARTIFACT_IDENTITY_GATE=NO_GO`

## Outcome

The two local `Npgsql 10.0.3` archive representations are byte-identical. Their raw SHA-512 is `9wZYlR6r1uLGnAxBuUPOCmgbS6SDtVKyGkZ0tDGPaBHYbbdEDEajD0VErq+W/dn3gHfJ3WvOdZaNNNybnFCupA==` and the sidecar matches. The competing `7nb5YzXuvWWJxB0J8DiyL3we+X4FOctZrt0fIBnucOIaIevFEEwGQVZKtiu9olXdlNAK1eNgqSral6r/jlhI4w==` is a NuGet metadata/restored-assets identity, not the raw SHA-512 of either local archive.

The embedded NuGet.org repository CMS signature is cryptographically intact, but full NuGet chain/revocation verification was unavailable with enforced no-network execution. Its prerequisite therefore failed and the isolated restore probe was not run. The gate is NO-GO.

## Stage 0

| Check | Result |
|---|---|
| HEAD | exact `3d01502ad456105e3e55921e56a7545e226d3baa` |
| Parent | exact `08a2afe1f78110bf83c032920b281fe5c8420f92` |
| Subject / branch | `REV869B Phase-A A5 package identity blocker` / `master` |
| HEAD boundary | exactly `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_blocker.md` |
| Blocker SHA-256 | `45043B6FCBE75C02E21BAC5B4F5E336F47C1FC2426A460319B5D17EE74B25207` |
| Initial target scope | clean; no implementation changes |
| `../legacy-reference/` | remained untracked and untouched; no file content was read or modified |

The persistence/classifier architecture freeze, revised implementation authorization, latest blocker, and project/package boundary reconciliation were read completely. All entry conditions matched.

## Offline tools

`dotnet` resolves to `C:\Program Files\dotnet\dotnet.exe`. SDK `10.0.303` (commit `e730f1db75`), MSBuild `18.6.14`, and runtime `10.0.11` are installed. `C:\Program Files\dotnet\sdk\10.0.303\NuGet.CommandLine.XPlat.dll` has file version `7.6.0.37803` and product version `7.6.0-rc.37803+e730f1db756d11c93f246830ba7b94ee6fcf4b94.e730f1db756d11c93f246830ba7b94ee6fcf4b94`.

No external endpoint was contacted. No package/cache content was modified, cleared, deleted, or repaired.

## Local candidates

| Candidate | Exact path | Bytes | Created UTC | Written UTC |
|---|---|---:|---|---|
| global archive | `C:\Users\User\.nuget\packages\npgsql\10.0.3\npgsql.10.0.3.nupkg` | 1,847,911 | `2026-08-08T10:58:07.3723378Z` | `2026-08-08T10:58:07.5375064Z` |
| HTTP-v3 cache | `C:\Users\User\AppData\Local\NuGet\v3-cache\670c1461c29885f9aa22c281d8b7da90845b38e4$ps_api.nuget.org_v3_index.json\nupkg_npgsql.10.0.3.dat` | 1,847,911 | `2026-08-08T10:58:06.9596300Z` | `2026-08-08T10:58:07.0025966Z` |

Both have:

- SHA-256 `75D0970923A8C9FCBBD37E4EBE72FEE0B10362A1E36723E86777DF1B6728316D`
- SHA-512 hex `F70658951EABD6E2C69C0C41B943CE0A681B4BA483B552B21A4674B4318F6811D86DB7440C46A30F4544AEAF96FDD9F78077C9DD6BCE75968D34DC9B9C50AEA4`
- SHA-512 Base64 `9wZYlR6r1uLGnAxBuUPOCmgbS6SDtVKyGkZ0tDGPaBHYbbdEDEajD0VErq+W/dn3gHfJ3WvOdZaNNNybnFCupA==`

No third local `Npgsql 10.0.3` archive or byte-conflicting duplicate exists in the inspected global-packages, HTTP-v3, scratch, or plugin caches. Other versions and the EF provider are different identities.

The 88-byte `C:\Users\User\.nuget\packages\npgsql\10.0.3\npgsql.10.0.3.nupkg.sha512` contains the full raw SHA-512 Base64 above. It corroborates bytes but is not independent trust.

The 182-byte `.nupkg.metadata` records version `2`, source `https://api.nuget.org/v3/index.json`, and `contentHash` `7nb5YzXuvWWJxB0J8DiyL3we+X4FOctZrt0fIBnucOIaIevFEEwGQVZKtiu9olXdlNAK1eNgqSral6r/jlhI4w==`. Existing `project.assets.json`/dependency manifests repeat it, including `src/SESS.NexaERP.Infrastructure/obj/project.assets.json`. No repository lock was created.

## Package and archive inventory

`Npgsql.nuspec` confirms ID `Npgsql`, version `10.0.3`, repository `https://github.com/npgsql/npgsql`, and commit `d3768398c17877b3a916c3c4d87e8e11698991fc`.

All 13 entries:

| Entry | Uncompressed/compressed bytes | Timestamp |
|---|---:|---|
| `.signature.p7s` | 13,005 / 13,005 | `2026-05-26T23:04:50Z` |
| `[Content_Types].xml` | 644 / 224 | `2026-05-27T06:02:58Z` |
| `_rels/.rels` | 496 / 279 | `2026-05-27T06:02:58Z` |
| `lib/net10.0/Npgsql.dll` | 1,478,656 / 520,883 | `2026-05-27T06:02:54Z` |
| `lib/net10.0/Npgsql.xml` | 688,223 / 86,259 | `2026-05-27T06:02:52Z` |
| `lib/net8.0/Npgsql.dll` | 1,477,632 / 520,598 | `2026-05-27T06:02:56Z` |
| `lib/net8.0/Npgsql.xml` | 688,223 / 86,259 | `2026-05-27T06:02:52Z` |
| `lib/net9.0/Npgsql.dll` | 1,477,632 / 520,697 | `2026-05-27T06:02:56Z` |
| `lib/net9.0/Npgsql.xml` | 688,223 / 86,259 | `2026-05-27T06:02:52Z` |
| `Npgsql.nuspec` | 1,601 / 719 | `2026-05-27T06:02:58Z` |
| `package/services/metadata/core-properties/69bf8487a48544d18b86f26da003f93a.psmdcp` | 824 / 501 | `2026-05-27T06:02:58Z` |
| `postgresql.png` | 9,675 / 9,680 | `2026-05-27T06:02:58Z` |
| `README.md` | 2,099 / 960 | `2026-05-27T06:02:58Z` |

## Signature evidence

Attempted command: `dotnet nuget verify --all C:\Users\User\.nuget\packages\npgsql\10.0.3\npgsql.10.0.3.nupkg`; executable path is recorded above.

Both enforced-offline attempts were rejected before process creation with `helper_unknown_error: apply deny-read ACLs`; no NuGet exit code or verification output exists. Unsandboxed execution could access OCSP/CRL endpoints, while forcing offline revocation would weaken validation. Neither was permitted. Full trust verification is incomplete.

## Hash reconciliation

Raw archive SHA-512 decodes to `F70658951EABD6E2C69C0C41B943CE0A681B4BA483B552B21A4674B4318F6811D86DB7440C46A30F4544AEAF96FDD9F78077C9DD6BCE75968D34DC9B9C50AEA4`.

Metadata `7nb5...lhI4w==` decodes to `EE76F96335EEBD6589C41D09F038B22F7C1EF97E0539CB59AEDD1F2019EE70E21A21EBC5104C0641564AB62BBDA255DD94D00AD5E360A92ADA97AAFF8E5848E3`.

The repository-signature SHA-256 decodes to `D5B21697855AD62A1FBC0821467C8A370639439761443241E321A8844E7BEFA7`. No local archive hashes to `7nb5...`; the freeze mislabeled NuGet metadata/restored-content identity as raw archive SHA-512.

Hashing the sidecar text, metadata JSON, signature, and nuspec yields SHA-512 prefixes `BE53E096`, `576A0835`, `12F6DD61`, and `407F6882`, respectively. Other package versions do not explain either value.

The mismatch is raw-archive digest versus NuGet metadata/restored-content identity, not conflicting archives. An extracted tree has no canonical byte serialization and none was invented.

## Conditional probe

A disposable external directory was prepared only far enough to copy the candidate; its SHA-256 matched `75D0970923A8C9FCBBD37E4EBE72FEE0B10362A1E36723E86777DF1B6728316D`.

No project or restore ran because full signature trust did not pass. The validated directory `C:\Users\User\AppData\Local\Temp\rev869b-a5-npgsql-artifact-reconciliation-20260821-1608` and its copy were deleted. No cache changed. Temporary lock/assets identity, restored inventory, and repeat locked restore remain unproven.

## Required amendment

Before implementation resumes, a separately authorized report-only amendment must:

1. label full `7nb5YzXuvWWJxB0J8DiyL3we+X4FOctZrt0fIBnucOIaIevFEEwGQVZKtiu9olXdlNAK1eNgqSral6r/jlhI4w==` only as observed NuGet metadata/restore `contentHash`;
2. separately freeze raw SHA-256 `75D0970923A8C9FCBBD37E4EBE72FEE0B10362A1E36723E86777DF1B6728316D`;
3. separately freeze raw SHA-512 `9wZYlR6r1uLGnAxBuUPOCmgbS6SDtVKyGkZ0tDGPaBHYbbdEDEajD0VErq+W/dn3gHfJ3WvOdZaNNNybnFCupA==` only after official acquisition and full verification;
4. freeze verified source, signer/chain/transcript, and reproduced lock/assets identities.

This report does not amend the frozen architecture.

## Separate management gate

Management must authorize controlled official acquisition of exactly `Npgsql 10.0.3` from `https://api.nuget.org/v3/index.json`. Capture the resolved URL, time, HTTP/TLS provenance, exact bytes, size, and full hashes.

Run `dotnet nuget verify --all` with normal, non-weakened chain/revocation validation and record executable/version/command/exit/output/signer/chain/timestamp evidence.

Compare current archives without cache changes; transfer the verified archive to an isolated local source; run two clean minimal-project restores with external sources disabled and the second locked; reconcile archive, sidecar, metadata, lock, assets, and restored inventory; delete disposable directories; then produce the separate amendment.

Mandatory NO-GO applies to any byte difference, incomplete/failed trust, offline network use, irreproducible restore, or identity mismatch. Nothing was downloaded here.

## Stop

The local archive has consistent bytes, correct ID/version, duplicate agreement, and internally valid repository CMS evidence. It lacks full trustworthy signature verification and reproducible offline lock/assets proof. Trust cannot rest on cache location, sidecar, or restore metadata.

`A5_NPGSQL_PACKAGE_ARTIFACT_IDENTITY_GATE=NO_GO`

Stop for management review. Revised A5 implementation remains prohibited.

## Additional signature detail

Type: embedded NuGet CMS repository signature. Signer: `CN=NuGet.org Repository by Microsoft, O=NuGet.org Repository by Microsoft, L=Redmond, S=Washington, C=US`. Issuer: `DigiCert Trusted G4 Code Signing RSA4096 SHA384 2021 CA1`.

Signer SHA-1 thumbprint: `C72FE7739A9EECB8EC1E4F596DB3BB74039B1DE2`. Certificate SHA-256: `1F4B311D9ACC115C8DC8018B5A49E00FCE6DA8E2855F9F014CA6F34570BC482D`.

Certificate validity is `2024-02-23T00:00:00Z` through `2027-05-18T23:59:59Z`; CMS signing time is `2026-05-27T11:34:47Z`. Repository evidence names `https://api.nuget.org/v3/index.json` and owners `brar`, `ninofloris`, `Npgsql`, and `roji`.

Signed content is `Version:1` with SHA-256 `1bIWl4Va1iofvAghRnyKNwY5Q5dhRDJB4yGohE5776c=`.

`SignedCms.CheckSignature(true)` passed, proving signature mathematics against the embedded certificate only. It does not prove chain trust or revocation.

## Final decision

`A5_NPGSQL_PACKAGE_ARTIFACT_IDENTITY_GATE=NO_GO`

Stop for management review. Revised A5 implementation remains prohibited.
