# REV869B A5 ERP Infrastructure project-aware package-lock reconciliation

Date: 2026-08-22

Decision type: report-only project/package-lock reconciliation and complete affected-lock audit

## 1. Decision

A5_ERP_PROJECT_AWARE_PACKAGE_LOCK_RECONCILIATION_GATE=GO

The complete future ERP Infrastructure project graph is reproducible from authoritative Git content and the
previously verified local package source. Its project-aware lock is:

06DF9719F8A02E344C4F565DBB538DD5E39F5D49B600081291FC940EF1E1F953

It contains exactly 41 NuGet package identities and three direct Project nodes, for 44 total lock dependency nodes.
The project nodes are Application, Control Plane Contracts and Domain. Domain is also reachable through Application,
but it remains a direct Infrastructure reference; therefore the transitive-only project-node count is zero.

All 41 package ID/version/contentHash triples match the authoritative official package verification exactly.
Generation plus two fresh offline locked replays retained identical lock bytes and identical path-independent assets
graphs. No package reverification is required.

The Control Plane Persistence project-aware lock was independently regenerated and replayed twice. It remains:

4D99C6E0124356EB289FCB231E89A6534A6BB1FAE1F42A5A01BCFE0D60F89DEB

It contains 35 NuGet packages plus one direct Contracts Project node, for 36 total nodes.

The affected-lock audit found no third lock owner. Control Plane, API and Control Plane tests change only
ProjectReference wiring; they do not set RestorePackagesWithLockFile, and no lock path for them is frozen or needed.
Requiring project-local locks for those nonowners would create unnamed paths and is explicitly rejected. The two
required owner lock paths already exist in the frozen 39-path allowlist, so no allowlist expansion is required.

Subject to separate management authorization:

A5_REVISED_SOURCE_IMPLEMENTATION_GATE=GO

may use both full project-aware lock hashes frozen by this and the Control Plane reconciliation. This report does not
implement or automatically authorize A5.

## 2. Stage 0 and repository boundary

| Check | Observed result | Status |
|---|---|---|
| HEAD | 6b72ba8766281bab8e7bb2dffde8a1b9671de81e | PASS |
| Parent | fadcdd48731dee78fc1b50354af85982fba337b4 | PASS |
| Branch | master | PASS |
| Subject | REV869B A5 ERP project-aware lock blocker | PASS |
| HEAD boundary | Exactly one modified report: outputs/rev869b_external_controller_phase_a_a5_revised_source_implementation_blocker_3.md | PASS |
| Blocker report SHA-256 | 3722BFFCB7F4BB394D49BD004C91EE2B7C666BFDC926123CDD1FE3519C379788 | PASS |
| Control project-aware report SHA-256 | 336F45F661BA1194762EE2CEDD6EA980E66E0896CCE328ACAE5F4A3ECF262A95 | PASS |
| Target-scoped status at entry | Clean | PASS |
| Partial implementation changes | None; all implementation paths remained at HEAD | PASS |

The latest blocker, immutable plan-contract decision, persistence/classifier architecture freeze, dual-context
migration/package decision, official package-graph verification, evidence-integrity reconciliation, EF tooling
boundary reconciliation and Control Plane project-aware lock reconciliation were read completely.

All Git evidence was target-scoped. The external legacy sibling was not accessed, enumerated, hashed or verified.

## 3. Restore-affecting committed inputs

| Committed input | Bytes | SHA-256 |
|---|---:|---|
| global.json | 81 | ABB754E4DE3E434515EC3362DB26346BB65DE9D1EAF47850B75ACEB298AF6A8F |
| src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj | 1,075 | D1FCC71A593A9DAD9ECAF193309FCC71D7667747311CD7FAE3833281D99E500A |
| src/SESS.NexaERP.Application/SESS.NexaERP.Application.csproj | 533 | E3FBCE7C18FFF25FF2A945A27A2A1CCE36676379A0856A2C80C3EBB11E701E25 |
| src/SESS.NexaERP.Domain/SESS.NexaERP.Domain.csproj | 316 | E31C184AC470CE562D1DDED8456AF616F5496F697494DDD6C8185720F1BCD2A9 |
| src/SESS.NexaERP.ControlPlane.Contracts/SESS.NexaERP.ControlPlane.Contracts.csproj | 261 | 9B8458580BE4E786DD73ED606F80627AB81A4545FBE8834E205DC430A56E7544 |
| src/SESS.NexaERP.ControlPlane/SESS.NexaERP.ControlPlane.csproj | 422 | 1CF5E7D3B8820C1EF65B6FE5424C70DEB6E66003161B5BBE80CE2F3112312407 |
| src/SESS.NexaERP.Api/SESS.NexaERP.Api.csproj | 1,184 | 01A2EFFC3528CA7A38132C2DD3862D71809C2DC3118AD2CFE561E2FE7669EF4C |
| src/SESS.NexaERP.AcceptanceVerifier/SESS.NexaERP.AcceptanceVerifier.csproj | 411 | D4486F2F7BB74A71C50D346922F603663FB7F31BCD49F2E0F091EEC391C45BEE |
| tests/SESS.NexaERP.ControlPlane.Tests/SESS.NexaERP.ControlPlane.Tests.csproj | 1,136 | 38525DDB266817756355CF7C33D27DEEA3C81E85A63DF9A1BCBEB42B3412CD74 |

global.json pins SDK 10.0.302 with rollForward latestFeature. Installed SDK 10.0.303 is in that feature band.
No target-scoped NuGet.Config, nuget.config, Directory.Build.props, Directory.Build.targets,
Directory.Packages.props or Directory.Packages.targets exists. No project has an explicit Import, central package
management property, RestoreSources, RestoreFallbackFolders, RuntimeIdentifier or RuntimeIdentifiers.

The disposable offline NuGet configuration used clear plus the sole verified local source. Its SHA-256 was
1B7B32327F3DA5A9FB96C37E22D30D68803BCFC36F52269054C108E4395F1DE8.

## 4. Exact future ERP project graph

All four projects target net10.0 and use Microsoft.NET.Sdk.

| Project | Exact direct ProjectReference Include | Transitive reachability from Infrastructure |
|---|---|---|
| SESS.NexaERP.Infrastructure | ..\SESS.NexaERP.Application\SESS.NexaERP.Application.csproj | Root |
| SESS.NexaERP.Infrastructure | ..\SESS.NexaERP.ControlPlane.Contracts\SESS.NexaERP.ControlPlane.Contracts.csproj | Root |
| SESS.NexaERP.Infrastructure | ..\SESS.NexaERP.Domain\SESS.NexaERP.Domain.csproj | Root and also through Application |
| SESS.NexaERP.Application | ..\SESS.NexaERP.Domain\SESS.NexaERP.Domain.csproj | Domain |
| SESS.NexaERP.ControlPlane.Contracts | None | Leaf |
| SESS.NexaERP.Domain | None | Leaf |

Infrastructure directly owns these unchanged package roots:

| Package | Version |
|---|---:|
| Microsoft.EntityFrameworkCore | 10.0.10 |
| Microsoft.EntityFrameworkCore.Design | 10.0.10 |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 10.0.10 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 |

Microsoft.EntityFrameworkCore.Design retains PrivateAssets=all and
IncludeAssets=runtime; build; native; contentfiles; analyzers; buildtransitive. Application retains its direct
Microsoft.Extensions.DependencyInjection.Abstractions 10.0.10 package and its Domain reference.

The exact future Infrastructure candidate adds only the Contracts reference and
RestorePackagesWithLockFile=true. Its UTF-8/LF candidate identity is:

| Candidate | Bytes | SHA-256 |
|---|---:|---|
| src/SESS.NexaERP.Infrastructure/SESS.NexaERP.Infrastructure.csproj | 1,256 | FAC12BDF53A2278457B5A6D4CB3DD0A7CFC8E135C9F04E80123B0337CF9C86D1 |

Project-node arithmetic:

| Kind | Count |
|---|---:|
| Direct project nodes | 3 |
| Transitive-only project nodes | 0 |
| Unique project nodes | 3 |
| NuGet package nodes | 41 |
| Total lock dependency nodes | 44 |

## 5. Complete ERP package table

Kind is the classification in the generated Infrastructure lock.

| Package | Version | Kind | NuGet contentHash |
|---|---:|:---:|---|
| Humanizer.Core | 2.14.1 | T | lQKvtaTDOXnoVJ20ibTuSIOf2i0uO0MPbDhd1jm238I+U/2ZnRENj0cktKZhtchBMtCUSRQ5v4xBCUbKNmyVMw== |
| Microsoft.Build.Framework | 18.0.2 | T | sOSb+0J4G/jCBW/YqmRuL0eOMXgfw1KQLdC9TkbvfA5xs7uNm+PBQXJCOzSJGXtZcZrtXozcwxPmUiRUbmd7FA== |
| Microsoft.CodeAnalysis.Analyzers | 3.11.0 | T | v/EW3UE8/lbEYHoC2Qq7AR/DnmvpgdtAMndfQNmpuIMx/Mto8L5JnuCfdBYtgvalQOtfNCnxFejxuRrryvUTsg== |
| Microsoft.CodeAnalysis.Common | 5.0.0 | T | ZXRAdvH6GiDeHRyd3q/km8Z44RoM6FBWHd+gen/la81mVnAdHTEsEkO5J0TCNXBymAcx5UYKt5TvgKBhaLJEow== |
| Microsoft.CodeAnalysis.CSharp | 5.0.0 | T | 5DSyJ9bk+ATuDy7fp2Zt0mJStDVKbBoiz1DyfAwSa+k4H4IwykAUcV3URelw5b8/iVbfSaOwkwmPUZH6opZKCw== |
| Microsoft.CodeAnalysis.CSharp.Workspaces | 5.0.0 | T | Al/Q8B+yO8odSqGVpSvrShMFDvlQdIBU//F3E6Rb0YdiLSALE9wh/pvozPNnfmh5HDnvU+mkmSjpz4hQO++jaA== |
| Microsoft.CodeAnalysis.Workspaces.Common | 5.0.0 | T | ZbUmIvT6lqTNKiv06Jl5wf0MTMi1vQ1oH7ou4CLcs2C/no/L7EhP3T8y3XXvn9VbqMcJaJnEsNA1jwYUMgc5jg== |
| Microsoft.CodeAnalysis.Workspaces.MSBuild | 5.0.0 | T | /G+LVoAGMz6Ae8nm+PGLxSw+F5RjYx/J7irbTO5uKAPw1bxHyQJLc/YOnpDxt+EpPtYxvC9wvBsg/kETZp1F9Q== |
| Microsoft.EntityFrameworkCore | 10.0.10 | D | a0V7zj/VbYP6dTdWpUgE/r2PuLKtUGe2aJ0lVKkn/wP9ZhaxUz2kQydVfvOjCv2SKxlrqdBfHhPD4Cvlf+4ffA== |
| Microsoft.EntityFrameworkCore.Abstractions | 10.0.10 | T | bOzrFCl6uZCjaSh2bG1ToRQRdx+iXvxosCg9hFyG9OWeAzOFI4xev9OqKeWfKf/kAHyox2JnbcvLVf2ceA7sqA== |
| Microsoft.EntityFrameworkCore.Analyzers | 10.0.10 | T | 2gLDordUCGf3aNOOuqtTbP5mxhiP9nk6TnvGiE3RnqT891O+Zf/qKu1PIREubs1M16A0SImr4vULBfU5BTDs1Q== |
| Microsoft.EntityFrameworkCore.Design | 10.0.10 | D | BsvxiKcy8k4/ijAPitmwKG1mlVsdC2lQtFLP28K2N8PlsGYbqPFOyfJ7p2kWil3gM6xXgQGf8Hz/pJB8ej+Dug== |
| Microsoft.EntityFrameworkCore.Relational | 10.0.10 | T | wNonj40aZxia+GtuBiiD6ZqVh4h6y5Nje1bGdmzZ8/ui0QRsAN+S0SIrLHFCEGbG9cDbeaE40sh+Lr7o9rRs6g== |
| Microsoft.Extensions.Caching.Abstractions | 10.0.10 | T | 4ZFBNE+jzR+CrWWlhOesnmywCW7pYKT0dxyAQRdL11yJwxe4jvcAu31eorFtEkoFeCDcUTeNssgPv2yaRRptaQ== |
| Microsoft.Extensions.Caching.Memory | 10.0.10 | T | N1w5H7uK6gCTnCBZAWzE0/EQYSPysij/uYwDqntqBVvBa6bjMmBKitsnEFd6yh/SX3wLm67nO6+OnZ84K+gZWg== |
| Microsoft.Extensions.Configuration.Abstractions | 10.0.10 | T | 5Vnd2I75DmZCVEjSynIdJ/0EGafgnLQwgR3t2C2/fkjx/nRG+cLwxLLdInoHeCEpkD5K4Ov/g9ZCRYrl4TRsaA== |
| Microsoft.Extensions.DependencyInjection | 10.0.10 | T | ANyvsgkNBRvcJh2XLgn8veGmajf+8m0AbKK+HPWdRL1yraSNVVSmQhFntLtdz/C795jxqqup+k05cs/3jZQPOA== |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.10 | T | z/2xXlFw2aLGjHyEm6E0tQ+In6VfzQzTrtArbQ2c0TQE16ZbyDCMGPvaUT9I0s8rgy9sRWlU2P9waW37qV04qA== |
| Microsoft.Extensions.DependencyModel | 10.0.10 | T | rfZA1RjR021RPqSmIPovfz2aOd79TGqJ9BengbjnzIISOVwjLmuSDnhCMmiY/1c6iYvGolQ1iNGzkav0u11XEA== |
| Microsoft.Extensions.Diagnostics.Abstractions | 10.0.10 | T | 9uWiKpeOVac355STyChWR/pliFX/5CeLqChW9kKsaxyDH4EUTZxMkT4Jwp/J/peLm0GBFmSX5c0WCse3yCnq1Q== |
| Microsoft.Extensions.Diagnostics.HealthChecks | 10.0.10 | T | R0O5oG+zAJeBSM8nNTa+Ycj2Zobyr/v6Ilo7Dha0sNB2Vq/XXoLdoecj9DAWGbN8YrPaW6u8+osTQ5Ypj7ZF0w== |
| Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions | 10.0.10 | T | 6euxgVR7NS83y0a2wLRAxfYXusLQJ2e1ah0MpQgYTYMs5lYrmdNP79C6T8uvRZdP87n5mcCcp6+w0EyWAidKZw== |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 10.0.10 | D | jgYLn+CG1/EgZ3lsAuRUvb0RFhr1q23/z4U85arQ8XgABVZAuJIVecLaShMxsB/AS0ufY+Z4OCv1facaiEyc5g== |
| Microsoft.Extensions.FileProviders.Abstractions | 10.0.10 | T | c5zqFCY9DiIpMovLd7/d/CTiEtrMOuQ639dhv3PABtKQIKNQikSHwQt8+N679uii9q+B55lgK28Uv64FOwEu8w== |
| Microsoft.Extensions.Hosting.Abstractions | 10.0.10 | T | 5LugpYGHk+mkn0a8IZgcyfBca8PCTAU9RQFoMrTdtOOidq88M2SI5f3px6ugnzgxC+eTkvYYJi8pzlUnG5xdAQ== |
| Microsoft.Extensions.Logging | 10.0.10 | T | Tf6z5HsL0VDYRTfvsoNrTGHGheCwkTsZBA2FFh5ATJUbkAwug+FFNISJK2gjpUNemlAOoWllAK52HOWCjto3EQ== |
| Microsoft.Extensions.Logging.Abstractions | 10.0.10 | T | zkFxGYUvdxAvIKTyXHrmW+Sux53D4SezD9dMyZ6hrwwzPQJNuwCRy1f5W7AvYTqacEGhWF2XderRQG1OvbV8og== |
| Microsoft.Extensions.Options | 10.0.10 | T | srnhnk7nE8krBiIXp71LvBmKBtraBONWSRzdjJgRv1Ko9Mp8IVNqv4vIS9hGeVteBig8aQkva9ZG+sC+o5sVcA== |
| Microsoft.Extensions.Primitives | 10.0.10 | T | 5wu/GrYVd8mG2DVUw3vFJzF+O336TyTGg/Kmcgw9bfwYhCoFiV5lR5QeEmKecJyrW4W54nMfD3p3589E8a7czQ== |
| Microsoft.VisualStudio.SolutionPersistence | 1.0.52 | T | oNv2JtYXhpdJrX63nibx1JT3uCESOBQ1LAk7Dtz/sr0+laW0KRM6eKp4CZ3MHDR2siIkKsY8MmUkeP5DKkQQ5w== |
| Mono.TextTemplating | 3.0.0 | T | YqueG52R/Xej4VVbKuRIodjiAhV0HR/XVbLbNrJhCZnzjnSjgMJ/dCdV0akQQxavX6hp/LC6rqLGLcXeQYU7XA== |
| Newtonsoft.Json | 13.0.3 | T | HrC5BXdl00IP9zeV+0Z848QWPAoCr9P3bDEZguI+gkLcBKAOxix/tLEAAHC+UvDNPv4a2d18lOReHMOagPa+zQ== |
| Npgsql | 10.0.3 | T | 7nb5YzXuvWWJxB0J8DiyL3we+X4FOctZrt0fIBnucOIaIevFEEwGQVZKtiu9olXdlNAK1eNgqSral6r/jlhI4w== |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 | D | IPGrrZnRkuW7OlHDhUESZz4G5DLkW7Nej/O3Cx+0iTsgyU5XJxBgpsvTHLloo3WWuAKKbDHXBvWPVkX1deRh1Q== |
| System.CodeDom | 6.0.0 | T | CPc6tWO1LAer3IzfZufDBRL+UZQcj5uS207NHALQzP84Vp/z6wF0Aa0YZImOQY8iStY0A2zI/e3ihKNPfUm8XA== |
| System.Composition | 9.0.0 | T | 3Djj70fFTraOarSKmRnmRy/zm4YurICm+kiCtI0dYRqGJnLX6nJ+G3WYuFJ173cAPax/gh96REcbNiVqcrypFQ== |
| System.Composition.AttributedModel | 9.0.0 | T | iri00l/zIX9g4lHMY+Nz0qV1n40+jFYAmgsaiNn16xvt2RDwlqByNG4wgblagnDYxm3YSQQ0jLlC/7Xlk9CzyA== |
| System.Composition.Convention | 9.0.0 | T | +vuqVP6xpi582XIjJi6OCsIxuoTZfR0M7WWufk3uGDeCl3wGW6KnpylUJ3iiXdPByPE0vR5TjJgR6hDLez4FQg== |
| System.Composition.Hosting | 9.0.0 | T | OFqSeFeJYr7kHxDfaViGM1ymk7d4JxK//VSoNF9Ux0gpqkLsauDZpu89kTHHNdCWfSljbFcvAafGyBoY094btQ== |
| System.Composition.Runtime | 9.0.0 | T | w1HOlQY1zsOWYussjFGZCEYF2UZXgvoYnS94NIu2CBnAGMbXFAX8PY8c92KwUItPmowal68jnVLBCzdrWLeEKA== |
| System.Composition.TypedParts | 9.0.0 | T | aRZlojCCGEHDKqh43jaDgaVpYETsgd7Nx4g1zwLKMtv4iTo0627715ajEFNpEEBTgLmvZuv8K0EVxc3sM4NWJA== |

Machine comparison against the 41 authoritative official report rows produced:

ERP official rows=41, lock packages=41, exact matches=41, mismatches=0.

## 6. Complete ERP project-node table

| Identity | Type | Root classification | Dependencies recorded by lock |
|---|---|---|---|
| sess.nexaerp.application | Project | Direct | Microsoft.Extensions.DependencyInjection.Abstractions [10.0.10, ); SESS.NexaERP.Domain [1.0.0, ) |
| sess.nexaerp.controlplane.contracts | Project | Direct | None |
| sess.nexaerp.domain | Project | Direct and also transitively reachable | None |

No transitive-only project node exists.

## 7. Offline source and tool identity

| Evidence | Value |
|---|---|
| Verified archive count | 41 |
| Verified archive bytes | 53,929,626 |
| Canonical filename/bytes/SHA-256 manifest | 7BE5281B6DF17BAACC3EEC865312A18CB7C5137FAE65B67CB0F93E03650872CD |
| Restore executable | C:\Program Files\dotnet\dotnet.exe |
| SDK version | 10.0.303 |
| Restore executable bytes | 167,208 |
| Restore executable SHA-256 | AB1B71FD3DD71062E074C9FAB8312081A81B7F2B3E0327C48C4D249C8D1A3135 |
| NuGet CLI assembly | C:\Program Files\dotnet\sdk\10.0.303\NuGet.CommandLine.XPlat.dll |
| NuGet CLI file version | 7.6.0.37803 |
| NuGet CLI bytes | 1,255,248 |
| NuGet CLI SHA-256 | 5D22918CEC4034F7919DB50AA1DDD353AAE11228DEFFE794F707572CB826D034 |

Each restore used a fresh isolated packages directory, HTTP cache and CLI home, clear plus the sole verified local
source, unreachable loopback HTTP/HTTPS proxies, no HTTP cache, NuGetAudit=false and warning-as-error handling.
Restore output contained zero HTTP request/download indicators.

## 8. ERP generation, replay and build evidence

| Run | Exit | Warnings/errors | Lock SHA-256 | Lock bytes | Raw project.assets.json SHA-256 | Canonical 44-node assets SHA-256 |
|---|---:|---|---|---:|---|---|
| Generation | 0 | 0/0 | 06DF9719F8A02E344C4F565DBB538DD5E39F5D49B600081291FC940EF1E1F953 | 16,501 | 26822D3772F8C61EB06486C773010B4F29A313C1D8EF82A395549E9FCB7C7A33 | 87E79CD650F00D0BDA1D2469AC180268AD0B172F834E1FD9F706A074101D30BB |
| Locked replay 1 | 0 | 0/0 | 06DF9719F8A02E344C4F565DBB538DD5E39F5D49B600081291FC940EF1E1F953 | 16,501 | 937E539487471496FA204F5FF48F33F6832B3A0F8BC3D4A9C66CAF657D02825C | 87E79CD650F00D0BDA1D2469AC180268AD0B172F834E1FD9F706A074101D30BB |
| Locked replay 2 | 0 | 0/0 | 06DF9719F8A02E344C4F565DBB538DD5E39F5D49B600081291FC940EF1E1F953 | 16,501 | B8893F5BBA1F1F4B27984A446E6487CDA87F39D63B7ED97C13DAF476EE9FF31A | 87E79CD650F00D0BDA1D2469AC180268AD0B172F834E1FD9F706A074101D30BB |

Raw assets hashes differ only because project.assets.json embeds isolated absolute paths. The normalized
node/type/SHA-512/dependency graph is identical in all three runs. Lock bytes never changed.

An offline warning-as-error build of locked replay 1 compiled Domain, Contracts, Application and Infrastructure:

build exit=0, warnings=0, errors=0.

## 9. Complete affected-lock audit

| A5-affected project | A5 restore-affecting change | Lock required | Future lock identity | Package/project-node expectation | Lock path in 39-path allowlist |
|---|---|:---:|---|---|:---:|
| Control Plane Persistence | New project; four exact PackageReferences; Contracts ProjectReference; net10.0; RestorePackagesWithLockFile=true | Yes | 4D99C6E0124356EB289FCB231E89A6534A6BB1FAE1F42A5A01BCFE0D60F89DEB | 35 NuGet + 1 direct project = 36 | Yes, path 4 |
| ERP Infrastructure | Add Contracts ProjectReference and RestorePackagesWithLockFile=true; existing four package roots and net10.0 unchanged | Yes | 06DF9719F8A02E344C4F565DBB538DD5E39F5D49B600081291FC940EF1E1F953 | 41 NuGet + 3 direct projects + 0 transitive-only = 44 | Yes, path 17 |
| Control Plane executable | Add direct Persistence ProjectReference; existing direct Contracts reference and net10.0 unchanged; no PackageReference or lock property change | No | Not applicable | Direct projects: Contracts and Persistence; unique project nodes 2; Contracts is also transitively reachable; package ownership remains in Persistence | No lock path required |
| ERP API | Add direct Contracts ProjectReference; existing Infrastructure, Application and Domain references, package roots and net10.0 unchanged | No | Not applicable | Direct projects: Contracts, Infrastructure, Application and Domain; unique project nodes 4; no new package identity | No lock path required |
| Control Plane tests | Add direct Persistence, API and Infrastructure references; existing Contracts, Control Plane and Acceptance Verifier references and package roots unchanged | No | Not applicable | Direct projects 6; transitive-only Application and Domain; no package is newly declared by the test project | No lock path required |

The new solution membership is restore orchestration but is not a project lock owner. Contracts source changes do not
change its immutable csproj, package graph, target framework or lock policy.

For nonowner projects, a project-local locked restore is intentionally not applicable: no lock is frozen and
RestorePackagesWithLockFile is absent. The frozen acceptance protocol restores the two owner projects from their
project-local locks and then builds hosts/tests with --no-restore. Creating locks for the other projects would require
unnamed paths and would contradict the exact boundary. No existing frozen lock for a nonowner is incomplete because
none exists or is required.

The exact authoritative Control Plane candidate csproj SHA-256 remains
8D7E9B62E81BAE18F2F1C530F38379DB96964E5952825CB048BBCCF596E69BB3. This audit re-materialized the same exact
restore semantics and independently reproduced its authoritative lock.

Control Plane replay matrix:

| Run | Exit | Warnings/errors | Lock SHA-256 | Lock bytes | Raw project.assets.json SHA-256 | Canonical 36-node assets SHA-256 |
|---|---:|---|---|---:|---|---|
| Generation | 0 | 0/0 | 4D99C6E0124356EB289FCB231E89A6534A6BB1FAE1F42A5A01BCFE0D60F89DEB | 13,446 | 74059C4CB31B50AE5EA5D4B35444818DD4999BB362C9D683B75919EAA8084696 | 3155D0DC2A95FB026659932B608AD109608EEFAD7FB4343CA515BFEBEFD2F182 |
| Locked replay 1 | 0 | 0/0 | 4D99C6E0124356EB289FCB231E89A6534A6BB1FAE1F42A5A01BCFE0D60F89DEB | 13,446 | CA6B8B5206865B0FE5152E258D5F66B685B3F938664AF8CBA07E8D03A45444CF | 3155D0DC2A95FB026659932B608AD109608EEFAD7FB4343CA515BFEBEFD2F182 |
| Locked replay 2 | 0 | 0/0 | 4D99C6E0124356EB289FCB231E89A6534A6BB1FAE1F42A5A01BCFE0D60F89DEB | 13,446 | E60A2A778E2F446C4EED4812A657B7D899D2E1BC545D55AE4BBD0556E0DEC865 | 3155D0DC2A95FB026659932B608AD109608EEFAD7FB4343CA515BFEBEFD2F182 |

Machine comparison against the official inventory produced 35/35 exact Control package matches and zero
mismatches.

## 10. Allowlist and cleanup conclusion

The only required future lock paths are already named:

1. src/SESS.NexaERP.ControlPlane.Persistence/packages.lock.json
2. src/SESS.NexaERP.Infrastructure/packages.lock.json

No central package file, Contracts lock, host lock, API lock, test lock or other unnamed path is required. The package
only ERP lock CF17917E57148E4E35D6C483CEF990615C11405EFD97DE3AB562FD98759E004E remains valid evidence for its isolated
package-only probe but is superseded as the future real Infrastructure lock.

All disposable work was outside the repository. The single audit root contained 12,438 files and 4,605 directories
including its root at cleanup. Its resolved absolute path was checked against the dedicated temp prefix, it was
permanently removed, and post-delete existence was False. Target-scoped status was clean immediately after cleanup.

## 11. Migration inventory and prohibited-operation counters

| Item | Retained value |
|---|---:|
| ERP existing migrations | 13 |
| ERP A5 target migrations | 1 |
| ERP post-A5 migrations | 14 |
| Control Plane initial migrations | 1 |
| Combined migrations | 15 |
| REV869A ERP ordinal | 12 |
| REV869B ERP ordinal | 13 |
| A5 target ERP ordinal | 14 |
| Repository migration attempts/applications/removals | 0/0/0 |
| PostgreSQL connection attempts/connections | 0/0 |
| Services started | 0 |
| Deployments/production access | 0/0 |
| Package downloads/network package sources | 0/0 |
| Mutants | 0 |
| Phase B work | 0 |
| Correction 2 work | 0 |

No repository source, project, package, lock, test, migration, snapshot, checkpoint or prior report was modified.

## 12. Final gate and exact next action

A5_ERP_PROJECT_AWARE_PACKAGE_LOCK_RECONCILIATION_GATE=GO

A5_EF_PACKAGE_TRUST_AND_OFFLINE_LOCK_GATE=GO

A5_REVISED_SOURCE_IMPLEMENTATION_GATE=MAY_BE_SEPARATELY_REAUTHORIZED_WITH_CONTROL_LOCK_4D99C6E0124356EB289FCB231E89A6534A6BB1FAE1F42A5A01BCFE0D60F89DEB_AND_ERP_LOCK_06DF9719F8A02E344C4F565DBB538DD5E39F5D49B600081291FC940EF1E1F953

The exact next gate is separate management authorization for one bounded revised A5 source-only implementation from
the commit containing this report, using the frozen 39-path allowlist, maximum 38 changed-path outcome, Control lock
4D99C6E0... and ERP lock 06DF9719.... No implementation begins automatically.

phase_a_management_acceptance_state=FAIL_PENDING_INDEPENDENT_REVIEW

phase_b_state=NO_GO

correction_2_state=NO_GO

postgresql_execution_state=NOT_AUTHORIZED_NOT_RUN

production_readiness_state=NOT_READY
