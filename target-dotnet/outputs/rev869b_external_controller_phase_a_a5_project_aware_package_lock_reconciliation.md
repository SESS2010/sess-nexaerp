# REV869B A5 project-aware package-lock architecture reconciliation

Date: 2026-08-22

Decision type: report-only architecture/package-lock reconciliation

## Decision

`A5_PROJECT_AWARE_PACKAGE_LOCK_RECONCILIATION_GATE=GO`

The real Control Plane Persistence project-aware lock is reproducible and cycle-free. Its full SHA-256 is:

`4D99C6E0124356EB289FCB231E89A6534A6BB1FAE1F42A5A01BCFE0D60F89DEB`

The lock contains exactly 35 NuGet package identities and one mandatory project-reference node, for 36 total lock
dependency nodes. The project node is not a 36th NuGet package. All 35 package ID/version/contentHash identities are
identical to the officially verified Control Plane subset, so the official `41/41 PASS` package-trust result remains
valid and no package reverification is required.

The earlier package-only lock
`64DC53ED03457021DFCBC985D9C8C5C0468B82BB102BC8382C3D920827137AA6` remains valid evidence for its isolated
four-package-root probe. It is superseded only as the future lock identity of the real
`SESS.NexaERP.ControlPlane.Persistence` project, because that real project has the mandatory Contracts
`ProjectReference`.

Subject to separate management authorization, the next bounded implementation may set:

`A5_REVISED_SOURCE_IMPLEMENTATION_GATE=GO`

using the full project-aware lock hash above, the existing 39-path allowlist and maximum 38 changed-path outcome.
This report does not authorize or start implementation.

## Stage 0

| Check | Observed result | Status |
|---|---|---|
| HEAD | `7e015878dc5e36f8a7f908e6b544be88279c7550` | PASS |
| Parent | `141b316245475ddbd861c7dfc4aa0838c4067d11` | PASS |
| Branch | `master` | PASS |
| Subject | `REV869B Phase-A A5 package lock blocker evidence` | PASS |
| HEAD boundary | Exactly `outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_blocker_3.md` | PASS |
| Blocker SHA-256 | `9DB66B2AB078344B61B87565B5FD7D23FC4114650EEABAC515B567A5EB25C85C` | PASS |
| EF tooling report SHA-256 | `24D1C67C3B75447F47A7A6E789C7709D14BF48592A2B1EB85E1712C2771044CE` | PASS |
| Target-scoped status at entry | Clean | PASS |
| Rolled-back implementation state | Persistence project/lock and A5 implementation paths absent | PASS |

The latest blocker report, persistence/classifier architecture freeze, dual-context migration/package decision,
official EF package-graph verification, package evidence-integrity reconciliation and EF tooling-boundary
reconciliation were read completely. All Git status, diff, untracked and committed-boundary evidence was constrained
to the target path. The external legacy sibling was not listed, enumerated, opened, hashed, verified or modified.

## Root-cause classification

The blocker is confirmed as a lock topology mismatch, not package drift:

| Identity | Bytes | NuGet packages | Project nodes | Total nodes | SHA-256 |
|---|---:|---:|---:|---:|---|
| Earlier package-only Control probe | 13,361 | 35 | 0 | 35 | `64DC53ED03457021DFCBC985D9C8C5C0468B82BB102BC8382C3D920827137AA6` |
| Real project-aware candidate | 13,446 | 35 | 1 | 36 | `4D99C6E0124356EB289FCB231E89A6534A6BB1FAE1F42A5A01BCFE0D60F89DEB` |

NuGet `NU1004` occurred when the package-only lock was applied to the real candidate because it omitted
`sess.nexaerp.controlplane.contracts` with lock node type `Project`. Regenerating from the exact real graph adds that
one structural node. It changes no package ID, version, direct/transitive classification, `contentHash`, archive,
signature, timestamp, certificate, revocation result or official artifact.

Accordingly:

- a `ProjectReference` node is not a NuGet package identity;
- the Control Plane subset remains exactly 35 packages;
- the ERP subset remains exactly 41 packages;
- their union remains exactly 41 verified package identities;
- official package verification remains `41/41 PASS`;
- the four earlier package-only offline replays remain valid for the graphs they tested; and
- future real-project locked restore must use the new project-aware lock bytes.

## Frozen project graph and ownership

The exact future graph remains:

```text
SESS.NexaERP.ControlPlane
  -> SESS.NexaERP.ControlPlane.Contracts
  -> SESS.NexaERP.ControlPlane.Persistence
       -> SESS.NexaERP.ControlPlane.Contracts

SESS.NexaERP.ControlPlane.Persistence
  -> Microsoft.EntityFrameworkCore 10.0.10
  -> Microsoft.EntityFrameworkCore.Design 10.0.10 (PrivateAssets=all)
  -> Npgsql 10.0.3
  -> Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3
```

The candidate project is
`src/SESS.NexaERP.ControlPlane.Persistence/SESS.NexaERP.ControlPlane.Persistence.csproj`. Its exact required edge is:

```xml
<ProjectReference Include="..\SESS.NexaERP.ControlPlane.Contracts\SESS.NexaERP.ControlPlane.Contracts.csproj" />
```

The referenced project is
`src/SESS.NexaERP.ControlPlane.Contracts/SESS.NexaERP.ControlPlane.Contracts.csproj`. Its committed HEAD SHA-256 is
`9B8458580BE4E786DD73ED606F80627AB81A4545FBE8834E205DC430A56E7544`.

Control Plane Persistence remains both the EF target and startup project under Option T1. EF Design remains owned
only there with this exact metadata:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.10">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

There is no central package management or repository `NuGet.Config`. The production Control Plane executable has no
EF Design or Npgsql package. ERP Infrastructure does not reference the Control Plane executable or Persistence.
Production does not reference tests. No project edge, package owner, deployment boundary or composition-root
ownership changes in this reconciliation.

## Restore-affecting project properties

The exact repository-shaped disposable candidate project SHA-256 was
`8D7E9B62E81BAE18F2F1C530F38379DB96964E5952825CB048BBCCF596E69BB3`. The copied Contracts project SHA-256 was
`9B8458580BE4E786DD73ED606F80627AB81A4545FBE8834E205DC430A56E7544`, exactly matching committed HEAD.

| Property/input | Persistence candidate | Contracts project |
|---|---|---|
| SDK | `Microsoft.NET.Sdk` | `Microsoft.NET.Sdk` |
| `TargetFramework` | `net10.0` | `net10.0` |
| `TargetFrameworks` | empty | empty |
| `RuntimeIdentifier` / `RuntimeIdentifiers` | empty / empty | empty / empty |
| `RestoreProjectStyle` | `PackageReference` | `PackageReference` |
| `RestorePackagesWithLockFile` | `true` | empty |
| `RestoreLockedMode` in project | empty; acceptance supplies `--locked-mode` | empty |
| `RestoreSources` in project | empty | empty |
| `RestoreFallbackFolders` | empty | empty |
| `RestorePackagesPath` in project | empty; command supplies isolated path | empty |
| `NuGetLockFilePath` | empty; project-local default | empty |
| `ManagePackageVersionsCentrally` | empty/false | empty/false |
| `CentralPackageTransitivePinningEnabled` | empty/false | empty/false |
| `NETCoreSdkVersion` | `10.0.303` | `10.0.303` |
| `MSBuildVersion` | `18.6.14` | `18.6.14` |
| `Nullable` | `enable` | `enable` |
| `ImplicitUsings` | `enable` | `enable` |
| `TreatWarningsAsErrors` | `true` | `true` |

The repository `global.json` pins SDK `10.0.302` with `rollForward=latestFeature`; installed SDK `10.0.303` is the
resolved feature-band SDK. Direct package versions are exact, non-floating values. The only source used by the probe
was an isolated `<clear />` configuration plus the verified local archive directory. Packages, HTTP cache and CLI
home were isolated per replay. HTTP/HTTPS proxies were set to unreachable loopback and NuGet audit was disabled only
for offline replay.

## Disposable package source and tool identity

The source was copied only after its authoritative canonical manifest matched official evidence:

```text
archive_count=41
archive_bytes=53929626
canonical_filename_bytes_sha256_manifest=7BE5281B6DF17BAACC3EEC865312A18CB7C5137FAE65B67CB0F93E03650872CD
```

The copied source reproduced the same count, bytes and manifest hash. This exact manifest covers every archive name,
byte length and SHA-256, so no unverified or substituted archive entered the probe.

One preliminary disposable layout used shortened sibling directory names. Internal QA rejected it because its
relative `ProjectReference` text was not byte-identical to the frozen future Include, even though it resolved the
same project node and lock. That preliminary root was permanently removed and none of its project/EF artifact hashes
is used in this report. The entire accepted restore and EF matrix was rerun in a fresh repository-shaped layout with
the exact frozen Include shown above.

| Tool | Version | Bytes | SHA-256 |
|---|---|---:|---|
| `C:\Program Files\dotnet\dotnet.exe` | SDK `10.0.303` | 167,208 | `AB1B71FD3DD71062E074C9FAB8312081A81B7F2B3E0327C48C4D249C8D1A3135` |
| `C:\Program Files\dotnet\sdk\10.0.303\NuGet.CommandLine.XPlat.dll` | file `7.6.0.37803` | 1,255,248 | `5D22918CEC4034F7919DB50AA1DDD353AAE11228DEFFE794F707572CB826D034` |
| `C:\Users\User\.nuget\packages\dotnet-ef\10.0.10\tools\net8.0\any\dotnet-ef.dll` | `10.0.10` | 91,448 | `520513FA1B7AC3E6F4195CF3CEFDF9D2F50924750EB480E44583A30A69BD8D25` |
| `C:\Users\User\.nuget\packages\dotnet-ef\10.0.10\tools\net8.0\any\tools\net8.0\any\ef.dll` | `10.0.10` | 114,016 | `252596F74AE15A65ECBB9228063F6FBA3B1344F507AC306FC898BD7395428FFD` |

## Complete project-aware lock table

The following is the complete NuGet package portion of the project-aware lock. `D` means direct and `T` means
transitive. The one project node is recorded separately after the table.

| Package | Version | Kind | NuGet `contentHash` |
|---|---:|:---:|---|
| `Humanizer.Core` | `2.14.1` | T | `lQKvtaTDOXnoVJ20ibTuSIOf2i0uO0MPbDhd1jm238I+U/2ZnRENj0cktKZhtchBMtCUSRQ5v4xBCUbKNmyVMw==` |
| `Microsoft.Build.Framework` | `18.0.2` | T | `sOSb+0J4G/jCBW/YqmRuL0eOMXgfw1KQLdC9TkbvfA5xs7uNm+PBQXJCOzSJGXtZcZrtXozcwxPmUiRUbmd7FA==` |
| `Microsoft.CodeAnalysis.Analyzers` | `3.11.0` | T | `v/EW3UE8/lbEYHoC2Qq7AR/DnmvpgdtAMndfQNmpuIMx/Mto8L5JnuCfdBYtgvalQOtfNCnxFejxuRrryvUTsg==` |
| `Microsoft.CodeAnalysis.Common` | `5.0.0` | T | `ZXRAdvH6GiDeHRyd3q/km8Z44RoM6FBWHd+gen/la81mVnAdHTEsEkO5J0TCNXBymAcx5UYKt5TvgKBhaLJEow==` |
| `Microsoft.CodeAnalysis.CSharp` | `5.0.0` | T | `5DSyJ9bk+ATuDy7fp2Zt0mJStDVKbBoiz1DyfAwSa+k4H4IwykAUcV3URelw5b8/iVbfSaOwkwmPUZH6opZKCw==` |
| `Microsoft.CodeAnalysis.CSharp.Workspaces` | `5.0.0` | T | `Al/Q8B+yO8odSqGVpSvrShMFDvlQdIBU//F3E6Rb0YdiLSALE9wh/pvozPNnfmh5HDnvU+mkmSjpz4hQO++jaA==` |
| `Microsoft.CodeAnalysis.Workspaces.Common` | `5.0.0` | T | `ZbUmIvT6lqTNKiv06Jl5wf0MTMi1vQ1oH7ou4CLcs2C/no/L7EhP3T8y3XXvn9VbqMcJaJnEsNA1jwYUMgc5jg==` |
| `Microsoft.CodeAnalysis.Workspaces.MSBuild` | `5.0.0` | T | `/G+LVoAGMz6Ae8nm+PGLxSw+F5RjYx/J7irbTO5uKAPw1bxHyQJLc/YOnpDxt+EpPtYxvC9wvBsg/kETZp1F9Q==` |
| `Microsoft.EntityFrameworkCore` | `10.0.10` | D | `a0V7zj/VbYP6dTdWpUgE/r2PuLKtUGe2aJ0lVKkn/wP9ZhaxUz2kQydVfvOjCv2SKxlrqdBfHhPD4Cvlf+4ffA==` |
| `Microsoft.EntityFrameworkCore.Abstractions` | `10.0.10` | T | `bOzrFCl6uZCjaSh2bG1ToRQRdx+iXvxosCg9hFyG9OWeAzOFI4xev9OqKeWfKf/kAHyox2JnbcvLVf2ceA7sqA==` |
| `Microsoft.EntityFrameworkCore.Analyzers` | `10.0.10` | T | `2gLDordUCGf3aNOOuqtTbP5mxhiP9nk6TnvGiE3RnqT891O+Zf/qKu1PIREubs1M16A0SImr4vULBfU5BTDs1Q==` |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.10` | D | `BsvxiKcy8k4/ijAPitmwKG1mlVsdC2lQtFLP28K2N8PlsGYbqPFOyfJ7p2kWil3gM6xXgQGf8Hz/pJB8ej+Dug==` |
| `Microsoft.EntityFrameworkCore.Relational` | `10.0.10` | T | `wNonj40aZxia+GtuBiiD6ZqVh4h6y5Nje1bGdmzZ8/ui0QRsAN+S0SIrLHFCEGbG9cDbeaE40sh+Lr7o9rRs6g==` |
| `Microsoft.Extensions.Caching.Abstractions` | `10.0.10` | T | `4ZFBNE+jzR+CrWWlhOesnmywCW7pYKT0dxyAQRdL11yJwxe4jvcAu31eorFtEkoFeCDcUTeNssgPv2yaRRptaQ==` |
| `Microsoft.Extensions.Caching.Memory` | `10.0.10` | T | `N1w5H7uK6gCTnCBZAWzE0/EQYSPysij/uYwDqntqBVvBa6bjMmBKitsnEFd6yh/SX3wLm67nO6+OnZ84K+gZWg==` |
| `Microsoft.Extensions.Configuration.Abstractions` | `10.0.10` | T | `5Vnd2I75DmZCVEjSynIdJ/0EGafgnLQwgR3t2C2/fkjx/nRG+cLwxLLdInoHeCEpkD5K4Ov/g9ZCRYrl4TRsaA==` |
| `Microsoft.Extensions.DependencyInjection` | `10.0.10` | T | `ANyvsgkNBRvcJh2XLgn8veGmajf+8m0AbKK+HPWdRL1yraSNVVSmQhFntLtdz/C795jxqqup+k05cs/3jZQPOA==` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `10.0.10` | T | `z/2xXlFw2aLGjHyEm6E0tQ+In6VfzQzTrtArbQ2c0TQE16ZbyDCMGPvaUT9I0s8rgy9sRWlU2P9waW37qV04qA==` |
| `Microsoft.Extensions.DependencyModel` | `10.0.10` | T | `rfZA1RjR021RPqSmIPovfz2aOd79TGqJ9BengbjnzIISOVwjLmuSDnhCMmiY/1c6iYvGolQ1iNGzkav0u11XEA==` |
| `Microsoft.Extensions.Logging` | `10.0.10` | T | `Tf6z5HsL0VDYRTfvsoNrTGHGheCwkTsZBA2FFh5ATJUbkAwug+FFNISJK2gjpUNemlAOoWllAK52HOWCjto3EQ==` |
| `Microsoft.Extensions.Logging.Abstractions` | `10.0.10` | T | `zkFxGYUvdxAvIKTyXHrmW+Sux53D4SezD9dMyZ6hrwwzPQJNuwCRy1f5W7AvYTqacEGhWF2XderRQG1OvbV8og==` |
| `Microsoft.Extensions.Options` | `10.0.10` | T | `srnhnk7nE8krBiIXp71LvBmKBtraBONWSRzdjJgRv1Ko9Mp8IVNqv4vIS9hGeVteBig8aQkva9ZG+sC+o5sVcA==` |
| `Microsoft.Extensions.Primitives` | `10.0.10` | T | `5wu/GrYVd8mG2DVUw3vFJzF+O336TyTGg/Kmcgw9bfwYhCoFiV5lR5QeEmKecJyrW4W54nMfD3p3589E8a7czQ==` |
| `Microsoft.VisualStudio.SolutionPersistence` | `1.0.52` | T | `oNv2JtYXhpdJrX63nibx1JT3uCESOBQ1LAk7Dtz/sr0+laW0KRM6eKp4CZ3MHDR2siIkKsY8MmUkeP5DKkQQ5w==` |
| `Mono.TextTemplating` | `3.0.0` | T | `YqueG52R/Xej4VVbKuRIodjiAhV0HR/XVbLbNrJhCZnzjnSjgMJ/dCdV0akQQxavX6hp/LC6rqLGLcXeQYU7XA==` |
| `Newtonsoft.Json` | `13.0.3` | T | `HrC5BXdl00IP9zeV+0Z848QWPAoCr9P3bDEZguI+gkLcBKAOxix/tLEAAHC+UvDNPv4a2d18lOReHMOagPa+zQ==` |
| `Npgsql` | `10.0.3` | D | `7nb5YzXuvWWJxB0J8DiyL3we+X4FOctZrt0fIBnucOIaIevFEEwGQVZKtiu9olXdlNAK1eNgqSral6r/jlhI4w==` |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `10.0.3` | D | `IPGrrZnRkuW7OlHDhUESZz4G5DLkW7Nej/O3Cx+0iTsgyU5XJxBgpsvTHLloo3WWuAKKbDHXBvWPVkX1deRh1Q==` |
| `System.CodeDom` | `6.0.0` | T | `CPc6tWO1LAer3IzfZufDBRL+UZQcj5uS207NHALQzP84Vp/z6wF0Aa0YZImOQY8iStY0A2zI/e3ihKNPfUm8XA==` |
| `System.Composition` | `9.0.0` | T | `3Djj70fFTraOarSKmRnmRy/zm4YurICm+kiCtI0dYRqGJnLX6nJ+G3WYuFJ173cAPax/gh96REcbNiVqcrypFQ==` |
| `System.Composition.AttributedModel` | `9.0.0` | T | `iri00l/zIX9g4lHMY+Nz0qV1n40+jFYAmgsaiNn16xvt2RDwlqByNG4wgblagnDYxm3YSQQ0jLlC/7Xlk9CzyA==` |
| `System.Composition.Convention` | `9.0.0` | T | `+vuqVP6xpi582XIjJi6OCsIxuoTZfR0M7WWufk3uGDeCl3wGW6KnpylUJ3iiXdPByPE0vR5TjJgR6hDLez4FQg==` |
| `System.Composition.Hosting` | `9.0.0` | T | `OFqSeFeJYr7kHxDfaViGM1ymk7d4JxK//VSoNF9Ux0gpqkLsauDZpu89kTHHNdCWfSljbFcvAafGyBoY094btQ==` |
| `System.Composition.Runtime` | `9.0.0` | T | `w1HOlQY1zsOWYussjFGZCEYF2UZXgvoYnS94NIu2CBnAGMbXFAX8PY8c92KwUItPmowal68jnVLBCzdrWLeEKA==` |
| `System.Composition.TypedParts` | `9.0.0` | T | `aRZlojCCGEHDKqh43jaDgaVpYETsgd7Nx4g1zwLKMtv4iTo0627715ajEFNpEEBTgLmvZuv8K0EVxc3sM4NWJA==` |

The separate project node is exact:

```text
identity=sess.nexaerp.controlplane.contracts
type=Project
count=1
```

## Locked restore and assets evidence

The initial project-aware generation and two fresh locked replays produced the same lock hash:

| Run | Work/cache state | Exit | Warnings | Errors | HTTP request/download indicators | Pre/post lock |
|---:|---|---:|---:|---:|---:|---|
| Generation | New project, isolated packages/HTTP/CLI cache | 0 | 0 | 0 | 0 package requests/downloads; only local source configured | `4D99C6E0...` generated |
| Locked replay 1 | Fresh work directory and empty isolated caches | 0 | 0 | 0 | 0 | unchanged `4D99C6E0...` |
| Locked replay 2 | Second fresh work directory and empty isolated caches | 0 | 0 | 0 | 0 | unchanged `4D99C6E0...` |

Each replay used `--locked-mode --force --no-http-cache -p:NuGetAudit=false -warnaserror`. The exact initial `obj`
directories were deleted before replay. No repository file was used as a restore output.

Raw `project.assets.json` hashes legitimately differ because assets encode absolute work/cache paths:

| Assets instance | Raw SHA-256 |
|---|---|
| Locked replay 1 | `899E576509F3204190CD704CEFB20313660268BFD9DF9689395A0A962B155D3A` |
| Locked replay 2 | `8A7BC1DD22B470109607A5803C48C10FB479DA608685E037AD350C37720CE8A8` |

The path-independent canonical 36-node target/dependency/sha512 graph was identical in both locked replays:

`E58EC829423E9C317BE2956EE6E5E6786D7EE7026C270D96A13E124DC2384DDB`

The independently normalized 35-line `ID|resolved|contentHash` package-table SHA-256 was also identical:

`9624D894E2199948B8D722A3527E220260224616F876C0734B485AB37A27A6B2`

These local normalization hashes supplement rather than replace the official Control canonical dependency identity
`798C1AB8B2734E7F398E24BFE278F024B308ED98824FA380125985689755D43C`.

## Option-T1 EF tooling result

The proven Option-T1 topology was rerun with Persistence as both target and startup project against locked replay 1.
A fail-closed `DbConnectionInterceptor` would have recorded and rejected any synchronous or asynchronous connection
opening. Its evidence file remained absent throughout.

| Operation | Result |
|---|---|
| Warning-as-error build | Exit 0; zero warnings/errors |
| `dbcontext info` | Exit 0; exact context, Npgsql provider, design-only database and `control.__EFMigrationsHistory` |
| Initial migration source generation | Exit 0 inside disposable probe |
| Post-generation warning-as-error build | Exit 0; zero warnings/errors |
| `migrations list --no-connect` | Exit 0; exactly one disposable initial migration listed |
| Up SQL generation | Exit 0 |
| Down SQL generation | Exit 0 |
| Snapshot parity migration | Exit 0; `Up` body 0 bytes, `Down` body 0 bytes |
| PostgreSQL connection-open attempts | 0 |
| PostgreSQL connections | 0 |
| Migration applications/removals | `0/0` |
| Service starts | 0 |

The disposable generated artifacts were:

| Artifact | Bytes | Lines | SHA-256 |
|---|---:|---:|---|
| Initial migration source | 1,275 | 39 | `7785C22640D3170187B126B6451B71B28562D3634790CE66498F144DAA345801` |
| Initial migration designer | 1,656 | 47 | `3202D0767AAE8EB386CA1B381052E1C33362B1922F42367EAA348B2CDB557055` |
| Initial snapshot | 1,542 | 44 | `5E6EC0361A7053FC7527A0BD672FD4703F7AE90ADFECEC5413FDFF61F0C9FDD3` |
| Snapshot parity migration | 483 | 22 | `64DBB4CF968C9F66642601E59BC0AB755DD3096C8A91DD194E374B81139298D0` |
| Snapshot parity designer | 1,638 | 47 | `FC60F154A16CC3C753B532DB24DA31B35315B693884AD6FC6EF63C073A4D43EA` |
| Up SQL | 919 | 31 | `E5FA9593AB32620A07B3DF63515A818594DD8D996E473786FC7AF0BE7453B256` |
| Down SQL | 197 | 8 | `F11A1DAC7E0A3A267AEEFE8FA42424234D0E8031F9CE83D3E42B834FB011EE60` |

The generated timestamped migration identities are disposable evidence only and do not change the frozen future
source identity `20260821093000_Rev869BA4ControlPlaneInitial` or any repository migration inventory.

## Allowlist and migration reconciliation

No allowlist expansion is required. The existing 39-path boundary already names:

- the new Persistence project file, which owns the real `ProjectReference` declaration;
- the Persistence project-local `packages.lock.json`;
- the neutral Contracts assembly source boundary; and
- the mutually exclusive success-checkpoint and blocker-report paths.

The committed Contracts `.csproj` is the immutable endpoint of the reference and needs no modification. Therefore it
does not need a new changed-path allowance. The maximum implementation outcome remains 38 changed paths, not 39.

The frozen post-A5 migration inventory remains:

```text
erp_existing_migrations=13
erp_a5_target_migrations=1
erp_post_a5_migrations=14
control_plane_initial_migrations=1
combined_migrations=15
rev869a_erp_ordinal=12
rev869b_erp_ordinal=13
a5_target_erp_ordinal=14
repository_migration_attempts=0
migration_applications=0
migration_removals=0
postgresql_connections=0
```

Two migration sources were generated only inside the authorized disposable EF probe. They were not repository
migration attempts, were never applied or removed and do not alter the `0/0/0` repository-attempt/application/removal
arithmetic.

## Cleanup and prohibited-operation evidence

The disposable root was outside the repository and was resolved beneath the system temp directory before deletion.
It contained 5,048 files and 2,098 directories including the root. It was permanently removed; post-cleanup existence
was `False`.

```text
repository_source_changes=0
repository_project_changes=0
repository_lock_changes=0
repository_migration_changes=0
package_downloads=0
network_package_sources=0
postgresql_connection_attempts=0
postgresql_connections=0
migration_applications=0
migration_removals=0
service_starts=0
production_access=0
deployment_or_external_provisioning=0
mutants=0
phase_b_work=0
correction_2_work=0
```

## Final gate and retained states

The package/project contradiction is resolved without changing package trust, project direction, package ownership,
allowlist or deployment topology.

`A5_PROJECT_AWARE_PACKAGE_LOCK_RECONCILIATION_GATE=GO`

`A5_EF_PACKAGE_TRUST_AND_OFFLINE_LOCK_GATE=GO`

`A5_REVISED_SOURCE_IMPLEMENTATION_GATE=MAY_BE_SEPARATELY_REAUTHORIZED_WITH_LOCK_SHA256_4D99C6E0124356EB289FCB231E89A6534A6BB1FAE1F42A5A01BCFE0D60F89DEB`

`phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW`

`phase_b_state=NO_GO`

`correction_2_state=NO_GO`

`postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN`

`production_readiness_state=NOT_READY`

The exact next gate is separate management authorization for one bounded revised A5 source-only implementation
using the frozen 39-path allowlist, maximum 38 changed-path outcome and full project-aware lock hash. No implementation
begins automatically. Stop after this report's single report-only commit.
