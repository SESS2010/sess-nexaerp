# REV869B Option-A Phase-A revised Correction A5 blocker checkpoint

## Decision

`A5_REVISED_CORRECTION=BLOCKED`

The authorized source-only implementation was reverted in full. No source correction is retained. The stop was mandatory because disposable mutant A5-M12 did not restore its production file to the exact pre-mutant SHA-256 after its reverse patch.

## Entry and boundary

- Entry commit: `89ded58e591ff0a9fb6d9b615c2c50d173d4ebf4`.
- Governing reconciliation SHA-256: `6CE986BC221D09FE5DE071DA1D5660D6DE7454E434B7B149B080950E5E682FDB`.
- Frozen 20-row manifest: `2,668` UTF-8 bytes, SHA-256 `EDAF648EFF4BD77158EF3A18A780D7B0DAD634FFB90CDBA8564A27D4DCFC95CB`.
- Baseline warning-as-error solution build: passed with zero warnings and zero errors.
- Baseline A5 aggregate: `30/30` passed.
- Retained A4 aggregate: `23/23` passed.
- PostgreSQL connections, migrations, provisioning, deployments, production operations, service starts, Phase B, and Correction 2: `0`.
- `../legacy-reference/` was not accessed or changed.

## Mutant evidence and stop

### A5-M11

- Production file: `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`.
- Patch SHA-256: `E38BEC63520FCBCD7FB78D96F567400D2C8D35AA7ED2CD8DA5AF5B65D66E74FF`.
- Pre-mutant file SHA-256: `D9490745BBB6B519241FB22707B2BFBBFC149530DDC18E282F6DA80687C26C8C`.
- Mutated file SHA-256: `BF4EA669D81E72967C6E672FCA0DD00878F600E3984C6A2331463A9FD9D592AB`.
- Production compilation: passed.
- Decisive killer: `A5_EachNewOperationReachesCompositeProviderWithoutLegacyLifecycleRejection`.
- Result: killed.
- TRX SHA-256: `A9C7A3EFE86B13AD20A4850A91B6CD381C0FD152AB6E182270AAD5DAE41D9949`.
- Restoration: exact.

### A5-M12

- Production file: `src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs`.
- Patch SHA-256: `F74F4CDF1D85453C428CA70721FB98B5BEF3EA46FAC20C06122BE1BCEFA7DE2E`.
- Pre-mutant file SHA-256: `D9490745BBB6B519241FB22707B2BFBBFC149530DDC18E282F6DA80687C26C8C`.
- Mutated file SHA-256: `F4B2DCC6A9ABB13E091A362A463BD40A9B839A60ABA9304DCDAE3025621E88F7`.
- Production compilation: passed.
- Decisive killer: `A5_RawAuthorizeAcquireBeginTargetReconcileUsesOneCanonicalProductionPath`.
- Result: killed.
- TRX SHA-256: `5528067BFEC42CE075E6BC32F1555673F6506747F01571E2BB86BFEB2067D4A1`.
- Post-reverse file SHA-256: `9726B2E11E35222CAA4BE4ACC209CC4D34385F52F2DC4C09A08945FBEF6D9714`.
- Restoration: failed exact SHA comparison; mutant classified invalid.

The campaign stopped immediately. A5-M13 through A5-M40 were not executed. No alternate mutant was substituted and no retry or diagnosis was performed.

## Rollback result

All tracked and newly created implementation changes were removed, returning the target source tree to entry commit `89ded58`. This blocker checkpoint is the only retained target-scoped change.

No revised A5 source correction commit may be created from this run. A fresh management decision and independent reconciliation are required before any further implementation or mutant execution.
