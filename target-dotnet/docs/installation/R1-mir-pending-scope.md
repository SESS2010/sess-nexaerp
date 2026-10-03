# R1 MIR pending scope alignment

MIR list/detail and approve/issue commands already operate in the selected company.
Operational department, warehouse and own-record filters previously hid MIRs from
Pending, MIR tracking history and Stores workload even when the same user could
open them in the MIR module.

Migration 147, `20261003083000_MirPendingScopeAlignment`, aligns these reads with
that existing module behavior. It changes the two MIR tracking queues to COMPANY
scope and corrects the installed MIR history and Stores workload SQL functions.
Every read still requires a resolved active employee, selected-company membership,
effective assignments, an active operational scope and the applicable page grants.
Other document queues retain their existing scope rules. This does not grant any
new page or command action; independent MIR approval remains unchanged.

The migration preserves installed function signatures, owners, execution grants,
SECURITY DEFINER and search_path. It introduces no function requiring an installer
grant. The existing InstalledFunctionSql.Rewrite helper supports PL/pgSQL only;
these SQL-language functions are instead rewritten from pg_get_functiondef with
an exact fragment-count guard, retaining the complete installed header. Down
restores exact saved queue/function contracts and refuses later edits.

Focused verification covers approval and issue lifecycle states under a mismatched
department/warehouse and own-record scope, source-page denial, selected-company
isolation, unchanged function security, rollback refusal and Up/Down/Up. The fast
suite and nightly are separate release gates; a source change is not deployment proof.
