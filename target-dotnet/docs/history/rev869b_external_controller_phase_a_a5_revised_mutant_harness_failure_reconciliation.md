# REV869B Phase-A A5 revised mutant-harness failure reconciliation

## 1. Decision

`A5_REVISED_MUTANT_HARNESS_GATE=GO`

This is a report-only reconciliation. It modifies no source, test, project, migration, helper, checkpoint, or existing report. It runs no build, test, mutant, PostgreSQL operation, migration, service, deployment, Phase B, or Correction 2.

The gate is `GO` because the retained evidence establishes a mutation-restoration harness failure, not a production-source defect; the revised harness below eliminates reverse-patch restoration and gives every mutant a fresh isolated worktree at one immutable candidate-baseline commit. A5-M12 remains a valid semantic production mutation and must be rerun unchanged in meaning. Historical M11/M12 observations are evidence only and count as zero toward the next required `40/40` campaign.

## 2. Exact entry and report boundary

- Required and observed HEAD: `0a787aa2a9f3a98ca877e86dde4587fd48f49505`.
- Observed parent: `89ded58e591ff0a9fb6d9b615c2c50d173d4ebf4`.
- HEAD subject: `REV869B Phase-A A5 revised mutant gate blocker checkpoint`.
- Parent subject: `REV869B Phase-A A5 mutant gate failure reconciliation`.
- HEAD contains exactly one added path:
  `outputs/rev869b_external_controller_phase_a_a5_revised_implementation_checkpoint.md`.
- Blocker checkpoint SHA-256:
  `2D241E3E1AD7BC738D9B9DC043568EEFCBE56CBAA7996E85076C0AD354CDFEB7`.
- Target-scoped status was clean before this report.
- Entry `git diff --check` passed.
- The pre-existing untracked sibling `../legacy-reference/` is outside target scope and was not enumerated, opened, read, changed, or used.

The following governing artifacts were read completely:

| Artifact | Lines | Bytes | SHA-256 |
|---|---:|---:|---|
| `outputs/rev869b_external_controller_phase_a_a5_mutant_gate_failure_reconciliation.md` | 257 | 27,371 | `6CE986BC221D09FE5DE071DA1D5660D6DE7454E434B7B149B080950E5E682FDB` |
| `outputs/rev869b_external_controller_phase_a_a5_boundary_and_immutable_plan_contract_decision.md` | 317 | 30,452 | `D41FFDA84969F4E64575FF207EC7413C4A7E7194AEEDC50179EF87937710F4A3` |
| `outputs/rev869b_external_controller_phase_a_a5_revised_implementation_checkpoint.md` | 53 | 3,020 | `2D241E3E1AD7BC738D9B9DC043568EEFCBE56CBAA7996E85076C0AD354CDFEB7` |

The explicit management authorization for the failed correction and the present report-only authorization were also read in full. Earlier reports and checkpoints remain immutable.

## 3. What the M12 evidence proves

| Observation | Value |
|---|---|
| Production file | `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs` |
| Pre-M12 file SHA-256 | `D9490745BBB6B519241FB22707B2BFBBFC149530DDC18E282F6DA80687C26C8C` |
| M12 patch SHA-256 | `F74F4CDF1D85453C428CA70721FB98B5BEF3EA46FAC20C06122BE1BCEFA7DE2E` |
| Mutated file SHA-256 | `F4B2DCC6A9ABB13E091A362A463BD40A9B839A60ABA9304DCDAE3025621E88F7` |
| Compile | passed |
| Intended killer | `A5_RawAuthorizeAcquireBeginTargetReconcileUsesOneCanonicalProductionPath` |
| Killer result | failed under the mutant |
| TRX SHA-256 | `5528067BFEC42CE075E6BC32F1555673F6506747F01571E2BB86BFEB2067D4A1` |
| Reverse-patch tool result | reported success |
| Post-reverse file SHA-256 | `9726B2E11E35222CAA4BE4ACC209CC4D34385F52F2DC4C09A08945FBEF6D9714` |
| Exact restoration | failed |

M11 used the same production file immediately before M12. Its pre-mutant SHA was the same `D949...`, and it restored exactly to `D949...`. Therefore M11 left no evidenced tracked overlap before M12.

The disposable worktree, post-reverse byte diff, file length, and post-reverse EOL inventory were deleted after the mandatory stop. The exact differing byte or bytes can no longer be reconstructed. No report may claim a more specific root cause.

## 4. Separate causal assessment

### Reverse-patch failure

**Established proximate failure.** M12 restoration depended on a mechanically sign-inverted patch rather than an authoritative Git object. The reverse patch reported success but did not reproduce the pre-mutant SHA. A successful textual patch application is insufficient restoration evidence.

The reverse hunk had less authoritative context than a Git blob identity. Whether it matched fuzzily, changed whitespace/EOL bytes, or reconstructed a logically equivalent but byte-different line is not recoverable. The harness was defective because it used a reverse patch as restoration.

### CRLF/LF conversion

**Plausible, not proven.** Observed Git configuration has global `core.autocrlf=true`; no path-specific `.gitattributes` rule was reported for `SignedEnvelopeService.cs`. The current committed checkout is all CRLF. An LF patch through a Windows patch tool creates EOL risk.

M11 nevertheless restored the same file exactly in the same campaign, so EOL conversion is not the demonstrated M12 cause. The revised harness fixes disposable clone EOL policy and requires materialized bytes to hash to the committed blob.

### Formatting drift

**No supporting evidence.** No formatter was authorized or invoked. Read-only inspection found no project target that writes or formats `SignedEnvelopeService.cs`. Whitespace drift remains possible as a patch effect, not as an evidenced formatter action.

### Overlapping edits

**No surviving overlap evidenced.** M11 restored to the exact SHA that became M12's pre-mutant SHA. Reusing one worktree was an avoidable risk, but retained evidence shows no M11 content surviving into M12.

### Nondeterministic generation

**No supporting evidence.** `SignedEnvelopeService.cs` is tracked production source, not generated output in the inspected graph. No generator/build target was found that rewrites it.

### Mutation-tool defect

**Possible tool/harness interaction; specific engine defect unproven.** Forward and reverse patches were accepted while final bytes differed. This proves patch success is not byte-restoration proof. It does not distinguish patch-engine EOL/fuzzy behavior from an inadequately contextualized generated reverse patch.

The defensible classification is:

`M12_FAILURE_CLASS=MUTANT_RESTORATION_HARNESS_AND_EVIDENCE_DEFECT`

There is no evidence that the A5 production design or M12 transformation caused nondeterministic source bytes. M12 is not a production-source defect.

## 5. M12 disposition

M12 does **not** require semantic redesign. It remains:

> At the sole composite durable-call construction point, replace the exact non-null `A4Operation` argument with `null`, leaving raw verification and every other request field unchanged.

It must be rerun unchanged in meaning from the new immutable candidate baseline. Its forward patch must have one unique contextual preimage match. No reverse patch may be generated or applied.

The decisive A5-01 assertion must observe the actual request delivered to the composite durable boundary and prove the exact non-null operation produced by the canonical builder is delivered once. The mutant must fail that equality/non-null assertion with zero legacy lifecycle call and zero durable mutation. A generic exception, source-string check, missing marker, or unrelated failure is not a kill.

Historical compile/kill evidence supports semantic validity but counts as zero in the next campaign.

## 6. Deterministic revised mutant harness

### Candidate baseline isolation

1. Leave the target repository and branch untouched while testing.
2. Create one disposable offline local clone. No network remote operation is permitted.
3. Set clone-local `core.autocrlf=false`, `core.eol=lf`, and `core.safecrlf=true`.
4. Check out the authorized entry, implement only the separately authorized A5 allowlist in the clone, and create one disposable candidate-baseline commit there. It is never created in, fetched into, or referenced by the target repository.
5. Verify parent, tree, exact allowlist, manifest bytes/hash, 30 literal A5 tests, retained tests, and clean status. Run all separately authorized baseline offline builds/tests before any mutant.
6. Record candidate commit/tree. The target branch receives no commit unless the correction succeeds; its final correction remains exactly one commit.

### Fresh worktree per mutant

For every mutant create a new detached disposable worktree at the exact candidate commit; never reuse one. Before mutation require exact HEAD, empty tracked/non-ignored status, passing `git diff --check`, empty worktree/index diffs, and the exact declared production path set.

For each affected file record candidate blob OID, blob size, SHA-256 of blob bytes, materialized SHA-256, and `git hash-object --no-filters path`. Require the no-filter object ID and worktree SHA-256 to equal the candidate blob evidence before mutation.

### Exact mutation application

Each mutant has one predeclared production patch artifact under the disposable campaign directory. Record its SHA-256. Require a named preimage symbol/snippet, exactly one selector match, passing `git apply --check`, exactly the declared production file set, no test/helper/project/report change, a unique production diff SHA-256, and passing `git diff --check`.

M18 may change its two declared predicates in one production file. Every other mutant changes one declared semantic decision in one production file.

### Compile and decisive kill

The unmodified candidate's named killer must first pass individually. Compile the mutant warning-as-error offline, then run the named killer. Require compilation success and exactly the predeclared assertion identity/message/stack location to fail for the intended invariant, with no crash, discovery error, timeout, missing file, fixture failure, source-marker failure, or unrelated exception. Record TRX/console hashes and the precise failed subcase/counters.

A nonzero process alone is never a kill. Source-string, line-order, `IndexOf`, missing-marker, formatting, reflection-name-only, or generic-exception failures are not decisive unless the invariant itself is the exact exported runtime capability surface.

### Authoritative restoration

Never apply a reverse patch. Restore changed paths directly from the candidate commit under fixed LF configuration. Require original blob/no-filter OID, worktree SHA-256, and size; passing `git diff --check`; empty worktree/index diff against candidate; no tracked residue; and no mutation artifact in the worktree. Delete and prune the worktree before creating the next one.

### Failure rule

Absent/ambiguous selector, noncompilation, survivor, wrong assertion, unrelated failure, duplicate diff hash, blob mismatch, failed `diff --check`, or tracked residue makes the mutant invalid and stops immediately. No alternate patch or silent retry is permitted.

## 7. Individual reconciliation of all 40 mutants

The matrix is authoritative for the next campaign. "Killer" means the exact behavioral/runtime-contract assertion, not merely a nonzero test process.

| ID | One semantic production transformation and real enforcement point | Decisive non-vacuous killer | Disjoint primary invariant |
|---|---|---|---|
| A4-M01 | In A4 authorization state construction, populate a lease/fence instead of retaining `Lease=null`. | A4 test 1: authorization returns the exact plan/grant and no lease/fence; lifecycle has no lease write. | Authorization cannot allocate execution ownership. |
| A4-M02 | In lease acquisition identity validation, accept caller/authorizer in place of the plan-bound executor. | A4 tests 2/4: substituted executor is rejected before lease/lifecycle/audit writes. | Executor identity binding. |
| A4-M03 | Remove plan version/hash from grant-to-acquisition equality while retaining IDs. | A4 test 3: changed version/hash is rejected with no lease or lifecycle mutation. | Immutable plan version/hash binding. |
| A4-M04 | Export/inject a low-level lease/fence mutation capability from the production boundary. | A4 F02/test 22: runtime exported capability surface contains only the composite provider; no partial setter resolves or invokes. | No partial mutation authority. |
| A4-M05 | Bypass committed authoritative lease validation before begin/target execution. | A4 test 8: pre-lease execution is denied before target call or lifecycle mutation. | Lease-before-execution ordering. |
| A4-M06 | Bypass only the lower stale-fence rejection in the target classifier. | A4 test 10: a lower submitted fence cannot become first owner or commit any target relation. | Lower-fence exclusion. |
| A4-M07 | On successful result lookup or uncertain acknowledgement, call target execution instead of reconciling the stored result. | A4 tests 14/15: result read count one, target mutation count zero, original committed result retained. | Reconciliation is read-only. |
| A4-M08 | Treat a conflicting acquisition/execution digest as exact replay while retaining request identity. | A4 tests 6/17: same ID/changed digest conflicts, returns no replay payload, and performs zero writes. | Retained A4 replay digest binding. |
| A4-M09 | Permit local state/business completion when its audit/outbox append reports failure. | A4 tests 13/18: append failure yields zero commit and value-identical state/business/receipt relations. | Local audit/outbox atomicity. |
| A4-M10 | Invoke the authoritative reader before descriptor, metadata, and cardinality preflight succeeds. | A4 tests 19/20: mismatch gives exact denial, reader count zero, and no lifecycle/provider calls. | Reader preflight ordering. |
| A5-M11 | In `SelectProposedTransitionAsync`, route non-null A4 operation through the legacy controller first. | A5-02: every A4 kind gets the synthetic transition, legacy count zero, composite provider receives the operation. | A4 never enters legacy ownership. |
| A5-M12 | At the sole composite request construction, replace the exact `A4Operation` argument with `null`. | A5-01: captured durable request contains the same non-null canonical operation once; no legacy call or durable mutation. | Canonical operation reaches composite boundary. |
| A5-M13 | Export a public/injectable grant, lease, fence, lifecycle, nonce, audit, or outbox setter. | A5-08: exact runtime metadata/DI capability surface has no partial method; only the composite atomic method is invocable. | A5 exposes no partial authority. |
| A5-M14 | Commit grant reservation/fence/lease before control audit/outbox, creating two control commits. | A5-06 transaction seam: one transaction ID and one commit after all stages; second-stage failure commits nothing. | One control commit. |
| A5-M15 | Replace database advisory/row winner locking with a process-local winner lock. | A5-06 command seam: independent provider/connections require database-lock event before locked read/write and yield one winner. | Winner ownership is database-scoped. |
| A5-M16 | Commit target business/history before terminal result/watermark staging. | A5-09/A5-10 seam: business/history, terminal/fence/idempotency, security evidence, then one commit; failure rolls all back. | Terminal/fence share target commit. |
| A5-M17 | Move target audit/outbox staging after target commit. | A5-09/A5-10 seam: both security writes precede sole commit; evidence failure leaves every relation unchanged. | Security evidence shares target commit. |
| A5-M18 | Make both declared lower/equal rejection and strict first-owner predicates unconditionally successful. | A5-11: lower 41/42 is stale rejection; equal replay/collision/incomplete have exact outcomes and zero handler/writes. | Exhaustive structured fence classification. |
| A5-M19 | In locked durable replay, make stored-to-incoming request digest equality always true while retaining request ID. | A5-20: exact replay returns original; same ID/changed digest conflicts with identical state and zero writes. | Durable control replay digest. |
| A5-M20 | Remove `RequireGrantValid(grant, now)` only from lease acquisition. | A5-12 not-before subcase: exact validity failure and zero lease/lifecycle/audit/downstream calls. | Grant validity at acquisition. |
| A5-M21 | Make renewal's `RequestedExpiresAt <= grant.ExpiresAt` predicate always true. | A5-13: one-tick-over-grant renewal is rejected with identical grant/lease/lifecycle and zero writes. | Renewal cannot outlive grant. |
| A5-M22 | Make the no-target-result proof predicate always true while retaining expiry/greater-fence checks. | A5-13: absent proof cannot reactivate grant or allocate fence; state/audit/outbox remain identical. | Reacquisition requires no-result proof. |
| A5-M23 | Build dispatch using current lifecycle version instead of committed immutable plan version. | A5-15: after lifecycle advancement, emitted grant/lease/job plan bytes equal the committed plan. | Dispatch plan immutability. |
| A5-M24 | Substitute executor transport identity for stored management authorizer in dispatch provenance. | A5-15/A5-16: emitted authorizer equals committed authorizer; substitution fails before target mutation. | Management authorizer provenance. |
| A5-M25 | Substitute authorization request ID/SHA for committed acquisition request ID/SHA in dispatch. | A5-15/A5-16: acquisition pair equals locked lease receipt exactly; zero target mutation on substitution. | Lease-acquisition provenance. |
| A5-M26 | Synthesize terminal result from control dispatch instead of calling pinned authoritative reader. | A5-17/A5-18: pinned reader count one; returned receipt bytes/digest are sole accepted result; missing/conflict quarantines. | Terminal source authenticity. |
| A5-M27 | On pinned reader 404, POST stored dispatch to execute instead of returning missing. | A5-17/A5-18: one GET, zero POST/body/execution; missing remains unavailable and quarantined. | Reconciliation cannot re-execute. |
| A5-M28 | Move reconciler workload/role/scope authorization below terminal replay return. | A5-19: wrong reconciler first-owner/replay is denied before read/disclosure; zero state/audit consumption. | Authorization precedes replay disclosure. |
| A5-M29 | Replace pinned durable-provider DI factory with a null-returning same-interface factory. | A5-21: actual offline production graph has one pinned descriptor and resolves/validates non-null concrete provider. | Concrete DI ownership. |
| A5-M30 | Make recomputed signed-job digest equality always true before locked facts/actor/handler. | A5-25: changed signed field under original digest yields exact payload-hash denial and zero relational/handler calls. | Signed target-job authenticity. |
| A5-M31 | Map `purchase.comparison.approve` to existing reject method with same typed signature. | A5-23/A5-27: approve count one; reject/all others zero; manifest cardinality stays 19. | Fixed action-method mapping. |
| A5-M32 | In unknown-action default, reflect caller-carried handler identity into existing Purchase method. | A5-24/A5-27: unknown action/method-like handler returns `OPERATION_MISMATCH`; reflection/handler/database zero. | No caller-selected execution. |
| A5-M33 | Make known-action parameter-schema SHA equality true while retaining version/schema ID/handler pins. | A5-23/A5-25: wrong schema hash fails before database and handler calls. | Manifest schema-hash pin. |
| A5-M34 | Omit only `QuoteDueAt` from canonical `purchase.rfq.create` bytes/hash. | A5-24/A5-25: payloads differing only in due time have different bytes/hash; changed payload cannot replay. | Complete canonical binding. |
| A5-M35 | Make plan resource-key to typed Purchase source-key equality true while retaining organization/version. | A5-26: signed resource A/typed B fails before actor/handler/database mutation. | Resource-key binding. |
| A5-M36 | Accept resolver success without comparing resolved employee/role to signed plan actor. | A5-26: resolver returns different employee/role; baseline rejects before handler, mutant reaches it. | Server-resolved actor binding. |
| A5-M37 | Derive Purchase idempotency as `a4:<AuthorizationDecisionId>` instead of target identity SHA. | A5-24/A5-27: captured key equals server target-identity derivation and is stable on exact replay. | Server-derived Purchase idempotency. |
| A5-M38 | In enlisted Purchase scope, call `CommitAsync` on outer target transaction. | A5-09/A5-10/A5-28: real seam outer commit/rollback/dispose attempts zero; target owner commits once. | Outer transaction ownership. |
| A5-M39 | For one action, issue direct business EF/raw DML instead of `IRev869BPurchaseService`. | A5-27/A5-29: service method count one, direct-business-DML count zero, public Purchase unchanged. | No target-provider business DML. |
| A5-M40 | Set successful terminal `TargetTransactionId` empty before persistence. | A5-30: ID nonempty, equals committed transaction, and canonical digest binds it with evidence. | Terminal transaction binding. |

## 8. Semantic validity and non-duplication

All 40 definitions mutate production behavior or the exact exported authority surface, name a real decision point, require a unique production diff, and have a non-vacuous behavioral/runtime-capability assertion. Test-only, message-only, SQL-token-only, line-order-only, missing-marker, and generic-exception kills are prohibited.

Shared tests do not make mutations duplicates:

- A4-M06 removes only lower-fence exclusion; A5-M18 corrupts the exhaustive structured lower/equal/first-owner classifier with two predicates.
- A4-M08 covers retained acquisition/execution replay; A5-M19 covers the new locked durable control receipt.
- A4-M07 mutates reconciliation service behavior; A5-M27 mutates the pinned 404 reader branch.
- A5-M16 splits terminal/fence; A5-M17 splits audit/outbox.
- A5-M24, M25, and M36 bind authorizer, acquisition, and business-actor provenance respectively.
- A5-M30, M33, M34, and M35 bind job digest, schema manifest, canonical parameter, and resource key respectively.

The future campaign must still prove `duplicate_production_diff_hashes=0`; static reconciliation does not waive execution evidence.

## 9. Required arithmetic and checkpoint evidence

- retained A4 mutants: `10`;
- A5-M11 through M30: `20`;
- A5-M31 through M40: `10`;
- compiled: `40`;
- killed by intended assertions: `40`;
- survivors: `0`;
- invalid: `0`;
- duplicate production diff hashes: `0`.

For every mutant the checkpoint records candidate commit/tree, file blob OID, pre/mutated/restored SHA-256 and size, patch/diff SHA-256, compiler/test identities, baseline killer pass artifact, mutant TRX/console hashes, failed assertion signature, restoration results, `diff --check`, clean tracked status, and worktree deletion.

It also retains the 20-row/2,668-byte manifest SHA-256
`EDAF648EFF4BD77158EF3A18A780D7B0DAD634FFB90CDBA8564A27D4DCFC95CB`
and zero counters for PostgreSQL, migrations, services, deployment, production access, Phase B, and Correction 2.

## 10. Stop conditions

Stop and revert if the candidate is not one immutable clean commit; a mutant does not start from a fresh worktree; blob/worktree/EOL evidence differs; a selector is absent, ambiguous, fuzzy-only, or changes an undeclared decision; a mutant does not compile, survives, or is killed for an unrelated reason; a reverse patch is used; restored blob/SHA/size differs; `git diff --check` fails; tracked residue remains; a diff duplicates another; an unnamed source/test/helper is needed; or a prohibited operation becomes necessary.

No mutant may be retried with an alternate transformation after a stop.

## 11. Authorization state

This report validates the deterministic harness and all 40 semantic definitions. It does not start implementation or execute the harness.

`A5_REVISED_MUTANT_HARNESS_GATE=GO`

`A5_CORRECTION_IMPLEMENTATION_STATE=NOT_STARTED`

`POSTGRESQL_EXECUTION_STATE=NOT_AUTHORIZED_NOT_RUN`

`PHASE_B_STATE=NO_GO`

`CORRECTION_2_STATE=NO_GO`

The next gate may authorize one bounded source-only revised A5 correction from the commit containing this report. It must use this harness, rerun all 40 mutants, create exactly one source-only correction commit on success or one report-only blocker commit on failure, and stop for fresh independent review.
