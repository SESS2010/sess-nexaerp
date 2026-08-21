# REV869B Phase-A A5 Controlled Official Package Acquisition and Identity

Date: 2026-08-21

Verdict: `A5_CONTROLLED_OFFICIAL_PACKAGE_IDENTITY_GATE=GO`

## Scope and Stage 0

This is the authoritative package-identity amendment only. It changes no source, test, project, solution, checkpoint, package, lock file, or existing report.

| Check | Result |
|---|---|
| Starting HEAD | `f7c7c4ebd59973d549dc00effeba6b16d983ba5c` |
| Parent | `3d01502ad456105e3e55921e56a7545e226d3baa` |
| Subject / branch | `REV869B Phase-A A5 Npgsql package artifact reconciliation` / `master` |
| HEAD boundary | exactly the prior package-artifact reconciliation |
| Prior report SHA-256 | `3DD0D598E83F24C29B6A7668FDE09B38CC4CC3DA29AAE06BF6F7DBCBEB3649AD` |
| Target scope | clean; no implementation changes |
| Legacy sibling | remained untracked and untouched; no file content was read or modified |

The required architecture freeze, package reconciliation, latest blocker, revised authorization, mutant-harness reconciliation, and project/package boundary evidence were read completely. All entry conditions passed.

## Isolation and tools

Quarantine: `C:\Users\User\AppData\Local\Temp\rev869b-a5-official-npgsql-f7c7c4e`. It contained separate package, HTTP, plugin, scratch, probe, local-source, and offline-package directories. Normal global NuGet caches were read only for comparison and were not cleared or modified. No nupkg, lock, assets, or probe file entered the repository.

| Tool | Exact identity |
|---|---|
| PowerShell | Windows PowerShell `5.1.19041.6456`, Desktop |
| dotnet | `C:\Program Files\dotnet\dotnet.exe`; SDK `10.0.303` |
| NuGet | `NuGet.CommandLine.XPlat.dll` file version `7.6.0.37803`; product `7.6.0-rc.37803+e730f1db756d11c93f246830ba7b94ee6fcf4b94.e730f1db756d11c93f246830ba7b94ee6fcf4b94` |
| curl | `C:\WINDOWS\system32\curl.exe` `8.13.0`, Schannel |

WinHTTP reported direct access. Curl used `--noproxy *`; restore proxy variables were cleared. The temporary online NuGet configuration contained `<clear />` and only `https://api.nuget.org/v3/index.json`.

## Official acquisition

The official service index returned HTTP 200 and advertised package base `https://api.nuget.org/v3-flatcontainer/`. The final package request was:

`https://api.nuget.org/v3-flatcontainer/npgsql/10.0.3/npgsql.10.0.3.nupkg`

It returned HTTP 200, no redirect, TLS verification result 0, `application/octet-stream`, and 1,847,911 bytes. Response metadata included:

- `Content-Length: 1847911`
- `Content-MD5: GhoLwlF8k6FbKFsZg7tJMg==`
- `Last-Modified: Wed, 27 May 2026 11:37:01 GMT`
- `ETag: 0x8DEBBE444CAA4C0`
- `x-ms-request-id: a1bd0dc5-c01e-006f-27cd-edb704000000`
- `x-ms-meta-SHA512: 9wZYlR6r1uLGnAxBuUPOCmgbS6SDtVKyGkZ0tDGPaBHYbbdEDEajD0VErq+W/dn3gHfJ3WvOdZaNNNybnFCupA==`

The nuspec confirms ID `Npgsql`, version `10.0.3`, repository `https://github.com/npgsql/npgsql`, commit `d3768398c17877b3a916c3c4d87e8e11698991fc`.

## Canonical identity amendment

| Identity | Canonical value |
|---|---|
| Exact version | `Npgsql 10.0.3` |
| Archive bytes | 1,847,911 |
| Raw archive SHA-256 | `75D0970923A8C9FCBBD37E4EBE72FEE0B10362A1E36723E86777DF1B6728316D` |
| Raw archive SHA-512 hex | `F70658951EABD6E2C69C0C41B943CE0A681B4BA483B552B21A4674B4318F6811D86DB7440C46A30F4544AEAF96FDD9F78077C9DD6BCE75968D34DC9B9C50AEA4` |
| Raw archive SHA-512 Base64 | `9wZYlR6r1uLGnAxBuUPOCmgbS6SDtVKyGkZ0tDGPaBHYbbdEDEajD0VErq+W/dn3gHfJ3WvOdZaNNNybnFCupA==` |
| NuGet lock/content hash | `7nb5YzXuvWWJxB0J8DiyL3we+X4FOctZrt0fIBnucOIaIevFEEwGQVZKtiu9olXdlNAK1eNgqSral6r/jlhI4w==` |

Streaming comparison proved the official and cached archives byte-for-byte equal. Official, cached, online-restored, and offline-restored archives have the canonical raw hashes above. The official raw SHA-512 equals both the server `x-ms-meta-SHA512` and cached sidecar; approval does not rely on the sidecar.

### Representation semantics

The raw SHA-256 and raw SHA-512 protect the exact signed ZIP byte stream, including `.signature.p7s` and ZIP structure. NuGet verification independently computed `7nb5...` as the package content hash. For a signed nupkg, this is the signature-stable canonical package-content identity, excluding the mutable repository-signature container representation; it is therefore different from the raw signed archive SHA-512.

`packages.lock.json` must contain full `7nb5YzXuvWWJxB0J8DiyL3we+X4FOctZrt0fIBnucOIaIevFEEwGQVZKtiu9olXdlNAK1eNgqSral6r/jlhI4w==`. The architecture/checkpoint must freeze both representations: raw SHA-256 and raw SHA-512 for artifact bytes, plus the NuGet contentHash for lock/restore identity.

The prior architecture freeze incorrectly called `7nb5...` the package SHA-512 without distinguishing it from raw archive SHA-512. This report supersedes only that identity wording; its ownership, graph, scope, and security decisions remain unchanged.

## Complete official archive inventory

| Entry | Uncompressed / compressed bytes |
|---|---:|
| `.signature.p7s` | 13,005 / 13,005 |
| `[Content_Types].xml` | 644 / 224 |
| `_rels/.rels` | 496 / 279 |
| `lib/net10.0/Npgsql.dll` | 1,478,656 / 520,883 |
| `lib/net10.0/Npgsql.xml` | 688,223 / 86,259 |
| `lib/net8.0/Npgsql.dll` | 1,477,632 / 520,598 |
| `lib/net8.0/Npgsql.xml` | 688,223 / 86,259 |
| `lib/net9.0/Npgsql.dll` | 1,477,632 / 520,697 |
| `lib/net9.0/Npgsql.xml` | 688,223 / 86,259 |
| `Npgsql.nuspec` | 1,601 / 719 |
| `package/services/metadata/core-properties/69bf8487a48544d18b86f26da003f93a.psmdcp` | 824 / 501 |
| `postgresql.png` | 9,675 / 9,680 |
| `README.md` | 2,099 / 960 |

Exactly 13 entries were present; no extra payload existed.

## Signature, timestamp, chain, and revocation

Exact verification command:

`& 'C:\Program Files\dotnet\dotnet.exe' nuget verify --all 'C:\Users\User\AppData\Local\Temp\rev869b-a5-official-npgsql-f7c7c4e\download\npgsql.10.0.3.nupkg' --verbosity detailed`

It was run once with normal defaults and again with `NUGET_CERT_REVOCATION_MODE=online`. No validation suppression was used. Both returned exit code 0 and `Successfully verified package 'Npgsql.10.0.3'`, with no ignored warning.

Signature type is Repository; no separate author signature is present. Service index is `https://api.nuget.org/v3/index.json`; owners are `brar`, `ninofloris`, `Npgsql`, and `roji`.

Repository signer:

- subject `CN=NuGet.org Repository by Microsoft, O=NuGet.org Repository by Microsoft, L=Redmond, S=Washington, C=US`
- issuer `CN=DigiCert Trusted G4 Code Signing RSA4096 SHA384 2021 CA1, O=DigiCert, Inc., C=US`
- serial `08AAB367D718124BAC892012FEEE317D`
- SHA-1 thumbprint `C72FE7739A9EECB8EC1E4F596DB3BB74039B1DE2`
- SHA-256 thumbprint `1F4B311D9ACC115C8DC8018B5A49E00FCE6DA8E2855F9F014CA6F34570BC482D`
- validity `2024-02-23T00:00:00Z` through `2027-05-18T23:59:59Z`

Code-signing chain SHA-1 thumbprints: intermediate `7B0F360B775F76C94A12CA48445AA2D2A875701C`; trusted root `DDFB16CD4931C973A2037D3FC83A4D7D775D05E4`.

Repository timestamp: `2026-05-27T11:34:48Z`. Timestamp signer:

- subject `CN=DigiCert SHA256 RSA4096 Timestamp Responder 2025 1, O=DigiCert, Inc., C=US`
- issuer `CN=DigiCert Trusted G4 TimeStamping RSA4096 SHA256 2025 CA1, O=DigiCert, Inc., C=US`
- serial `0A80EF184B8DF10582D1C476A7957468`
- SHA-1 thumbprint `DD6230AC860A2D306BDA38B16879523007FB417E`
- SHA-256 thumbprint `4AA03FA22CD75C84C55C938F828E676B9CAECAB33FE36D269AA334F146110A33`
- validity `2025-06-04T00:00:00Z` through `2036-09-03T23:59:59Z`

Timestamp intermediate SHA-1 is `07894D00FC194A17DB273AEB5CF8FACEF14423A4`. The verifier traced the timestamp chain to trusted roots and completed online revocation mode successfully. Repository and timestamp chain verification both passed.

## Network boundary and download inventory

Contacted/validated hostname set was bounded to:

- `api.nuget.org` for service index, package acquisition, and restore;
- `ocsp.digicert.com` and `crl3.digicert.com` for DigiCert revocation evidence used by Windows chain validation.

No other package feed or external content host was configured. Curl resolved `api.nuget.org` to `183.82.248.57` for both captured requests.

Application-level network count was two direct acquisition GETs plus six clean-restore GETs, all HTTP 200/OK from `api.nuget.org`. The six restore requests comprised three version-index queries and these three package downloads:

1. `Npgsql 10.0.3`, 1,847,911 bytes;
2. `Microsoft.Extensions.Logging.Abstractions 10.0.0`, 822,954 bytes;
3. `Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0`, 285,647 bytes.

The locked replay used a local-only source and recorded zero HTTP requests, zero external downloads, and zero external package sources.

## Clean restore and lock evidence

The disposable net10.0 project referenced exactly `Npgsql 10.0.3`. A clean official-only restore from empty isolated package/cache directories completed with `Build succeeded`, 0 warnings, 0 errors, and three installed packages.

Generated file evidence:

| Artifact | SHA-256 / result |
|---|---|
| `packages.lock.json` | `88854FDAE4B2F324034BE7AA67A365230C399E91C32BE6526D03A4C8F8D6F6EA` |
| online `project.assets.json` | `8F408CFF15CF71C59CDCEC85C66915A01AF5B1BEC2665787AA4DAB524D4C9570` |
| offline `project.assets.json` | `BEBE33BCCCB744B35A98D4099C41A5FC755CDBD2B64AF64459D623AF4F2B63BA` |
| post-replay lock SHA-256 | unchanged: `88854FDAE4B2F324034BE7AA67A365230C399E91C32BE6526D03A4C8F8D6F6EA` |

Assets bytes differ only because their package-folder/source paths differ between online and offline quarantines. Their package identity tuples and all content hashes are identical.

Exact net10.0 lock closure:

| Package | Role | Version | NuGet contentHash |
|---|---|---|---|
| Npgsql | Direct | 10.0.3 | `7nb5YzXuvWWJxB0J8DiyL3we+X4FOctZrt0fIBnucOIaIevFEEwGQVZKtiu9olXdlNAK1eNgqSral6r/jlhI4w==` |
| Microsoft.Extensions.Logging.Abstractions | Transitive | 10.0.0 | `FU/IfjDfwaMuKr414SSQNTIti/69bHEMb+QKrskRb26oVqpx3lNFXMjs/RC9ZUuhBhcwDM2BwOgoMw+PZ+beqQ==` |
| Microsoft.Extensions.DependencyInjection.Abstractions | Transitive | 10.0.0 | `L3AdmZ1WOK4XXT5YFPEwyt0ep6l8lGIPs7F5OOBZc77Zqeo01Of7XXICy47628sdVl0v/owxYJTe86DTgFwKCA==` |

The isolated local source contained exactly those three official nupkgs. A fresh empty package directory restored with `--locked-mode`, only that local source, unreachable loopback proxy settings, and no HTTP cache. Exit code was 0; lock hash did not drift.

Online and offline extracted Npgsql payloads each had 10 content files after excluding NuGet metadata/sidecars. Their sorted path-length-file-hash manifest SHA-256 was identically `3439B1F2E754CE94B4CAE3A85E83E5A268EE85D30A518C8BE65EE614B5AFB8C6`.

## Frozen future package-lock requirements

The future `src/SESS.NexaERP.ControlPlane.Persistence/packages.lock.json` must:

1. be generated, never hand-authored, for the exact direct/transitive closure above;
2. record Npgsql `10.0.3` with the full canonical NuGet contentHash;
3. retain `RestorePackagesWithLockFile=true`;
4. pass local-source-only `--locked-mode` restore with no drift;
5. be accepted only when each source nupkg matches its separately recorded raw SHA-256 and raw SHA-512;
6. reject version range/floating changes, new transitives, contentHash drift, fallback sources, or network acceptance.

No central package-management file is introduced. The persistence adapter remains the only new direct Npgsql owner. This amendment changes no project graph, ownership boundary, migration rule, or deployment topology.

## Cleanup and prohibited-operation status

After all evidence above was recorded, the validated quarantine was deleted: 238 temporary files totaling 34,407,818 bytes, including downloaded packages, isolated caches, probe project, lock/assets, configs, traces, and logs. Deletion was confirmed. The normal global NuGet cache was not modified.

No ERP build, test, mutant, PostgreSQL connection, migration, service, provisioning, deployment, production access, Phase B, or Correction 2 operation occurred.

## Decision and next gate

Official acquisition, exact ID/version, repository signature, timestamp, certificate chains, explicit online revocation, archive comparison, contentHash semantics, lock/assets identities, and network-disabled locked replay all passed.

`A5_CONTROLLED_OFFICIAL_PACKAGE_IDENTITY_GATE=GO`

The single next management gate is a fresh bounded authorization to resume revised A5 source implementation from the new report commit, using the frozen 30-path allowlist and this canonical identity set. Implementation must not restart automatically.
