# Finding 3: quotation technical verification scope

The verifier must hold an existing technical role and have an active company
membership and department assignment intersecting an active operational scope.
The department is the verifier's own current department. Warehouse, rack and
own-record restrictions remain enforced. The technical scope predicate does not
use privileged cross-scope flags.

The schema has no governed item-category-to-technical-department competence map.
Inventing one from similar text codes would create an unapproved authorization
rule. This implements the user's expressly selected own-department option.
Consequently it does not claim to enforce item-category-specific competence.

RFQ department follows its originating PR in this code. The customer's failing
RFQ is Purchase-scoped; an RFQ from another department presents the same defect.
Technical review is not membership of that purchasing/requesting department.

Quotation list/detail and technical verification share the predicate. Detail also
returns quoted item identifiers, names and the RFQ specification snapshot, while
retaining commercial-value masking. A verifier can obtain the line ID and version
through the same permitted GET instead of relying on a database-only test lookup.
Other commercial operations retain their existing record-scope checks.

The permission migration supplies the Technical Support Manager quotation view and download grant (one permission receipt and one audit row). It adds no department assignment or operational scope.

The frozen remaining-findings working candidate passed full Debug 878/878 and Release 875/875, with zero failures/skips, after both solution builds succeeded with zero warnings/errors. All 1,013 recorded source hashes matched after testing. TRX evidence is under local-evidence/finding3. These are shared working-candidate results, not clean per-commit checkout runs; ordered commits and their exact staged builds remain pending. No owner database was changed.
