# Fitment, reversal and accepted return: routine witness

The real HTTP workflow now runs in ordinary Debug and Release suites, rather than requiring the optional WORKFLOW_WITNESS compile symbol. The frozen combined candidate passed clean builds (zero warnings/errors), full Debug 881/881 and Release 878/878, no failed/skipped tests, and all three focused witnesses in both configurations. Retained TRX and source hashes are under local-evidence/block-a-v2-results and block-a-v2-acceptance.json. The published source/test tree was compared with that tested candidate after checkout line-ending normalization.

The witness fits a component, reverses its fitment, then declares and accepts its return through Stores. It asserts that reversal removes machine cost while leaving FIFO consumption/restoration totals unchanged; only the accepted return restores the recorded FIFO consumption, in reverse creation order. Command replay cannot add another movement, restoration or charge.

| Observation in the .30-unit return fixture | Result |
|---|---:|
| Machine cost before fitment reversal | 1203.60 |
| Machine cost after reversal | 0 |
| Machine cost after accepted return | 0 |
| Restored quantity | .30 |
| Restored immutable base consumption value | 1416.00 |
| FIFO report value increase, including retained cost adjustment | 1419.60 |
| Quantity still held by the engineer | .05 |

The fixture retains historical FIFO costing evidence; it does not rewrite an immutable layer. This is a separate custody fixture from finding #12's one-of-two-unit variance regression. That regression remains 1475 - 2500 = -1025 before correction and 1250 - 2500 = -1250 afterward. The Estimated BOM baseline remains ex-tax.

Production schema, permission and business-row effects of this test-only publication are zero. Frontend contracts are unchanged. The witness used disposable Windows/PostgreSQL17 databases with repository fixtures, not the owner database or the missing exact pre-75 field dump. Field witness remains pending.
