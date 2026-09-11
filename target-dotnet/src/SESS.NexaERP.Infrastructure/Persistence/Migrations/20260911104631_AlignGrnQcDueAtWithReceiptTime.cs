using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignGrnQcDueAtWithReceiptTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(ChangeDeadlineBasis(useReceiptTime: true));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(ChangeDeadlineBasis(useReceiptTime: false));
        }

        private static string ChangeDeadlineBasis(bool useReceiptTime)
        {
            var oldBasis = useReceiptTime ? "FinalizedAt" : "ReceivedAt";
            var newBasis = useReceiptTime ? "ReceivedAt" : "FinalizedAt";
            var oldMessage = useReceiptTime
                ? "GRN QC due time must use the snapshotted limit from finalisation."
                : "GRN QC due time must use the snapshotted limit from receipt.";
            var newMessage = useReceiptTime
                ? "GRN QC due time must use the snapshotted limit from receipt."
                : "GRN QC due time must use the snapshotted limit from finalisation.";
            var oldFinalizerDeadline = useReceiptTime
                ? @"""QcDueAt""=finalized_at+make_interval(days=>""QcCompletionDaysSnapshot"")"
                : @"""QcDueAt""=receipt.""ReceivedAt""+make_interval(days=>""QcCompletionDaysSnapshot"")";
            var newFinalizerDeadline = useReceiptTime
                ? @"""QcDueAt""=receipt.""ReceivedAt""+make_interval(days=>""QcCompletionDaysSnapshot"")"
                : @"""QcDueAt""=finalized_at+make_interval(days=>""QcCompletionDaysSnapshot"")";

            return $"""
            DO $migration$
            DECLARE
              guard_definition text;
              finalizer_definition text;
              immutable_clause text := $clause$IF TG_OP='UPDATE' AND OLD."Status"='FINALIZED' THEN RAISE EXCEPTION 'Finalised GRNs are immutable.'; END IF;$clause$;
              repair_clause text := $clause$IF TG_OP='UPDATE' AND OLD."Status"='FINALIZED' THEN
                IF (to_jsonb(NEW)-'QcDueAt')=(to_jsonb(OLD)-'QcDueAt')
                   AND NEW."QcDueAt"=NEW."{newBasis}"+make_interval(days=>NEW."QcCompletionDaysSnapshot") THEN
                  RETURN NEW;
                END IF;
                RAISE EXCEPTION 'Finalised GRNs are immutable.';
              END IF;$clause$;
              old_guard_deadline text := $clause$IF NEW."QcDueAt"<>NEW."{oldBasis}"+make_interval(days=>NEW."QcCompletionDaysSnapshot") THEN
                  RAISE EXCEPTION '{oldMessage}';
                END IF;$clause$;
              new_guard_deadline text := $clause$IF NEW."QcDueAt"<>NEW."{newBasis}"+make_interval(days=>NEW."QcCompletionDaysSnapshot") THEN
                  RAISE EXCEPTION '{newMessage}';
                END IF;$clause$;
              old_finalizer_deadline text := $clause${oldFinalizerDeadline}$clause$;
              new_finalizer_deadline text := $clause${newFinalizerDeadline}$clause$;
            BEGIN
              IF to_regclass('advance.goods_receipts') IS NULL
                 OR to_regprocedure('advance.stores_p2_goods_receipt_guard()') IS NULL
                 OR to_regprocedure('advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text)') IS NULL
                 OR NOT EXISTS (
                   SELECT 1 FROM pg_trigger
                   WHERE tgrelid='advance.goods_receipts'::regclass
                     AND tgfoid='advance.stores_p2_goods_receipt_guard()'::regprocedure
                     AND NOT tgisinternal) THEN
                RAISE EXCEPTION 'GRN QC deadline controls are absent or partially installed.';
              END IF;

              LOCK TABLE advance.goods_receipts IN ACCESS EXCLUSIVE MODE;
              SELECT pg_get_functiondef('advance.stores_p2_goods_receipt_guard()'::regprocedure)
                INTO guard_definition;
              SELECT pg_get_functiondef('advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text)'::regprocedure)
                INTO finalizer_definition;

              IF (length(guard_definition)-length(replace(guard_definition,immutable_clause,'')))/length(immutable_clause)<>1
                 OR (length(guard_definition)-length(replace(guard_definition,old_guard_deadline,'')))/length(old_guard_deadline)<>1
                 OR (length(finalizer_definition)-length(replace(finalizer_definition,old_finalizer_deadline,'')))/length(old_finalizer_deadline)<>1 THEN
                RAISE EXCEPTION 'GRN QC deadline controls do not match the expected predecessor contract (immutable %, guard %, finalizer %).', position(immutable_clause in guard_definition), position(old_guard_deadline in guard_definition), position(old_finalizer_deadline in finalizer_definition);
              END IF;

              -- Keep finalised GRNs immutable except for this exact derived-field repair.
              EXECUTE replace(guard_definition,immutable_clause,repair_clause);
              UPDATE advance.goods_receipts
                 SET "QcDueAt"="{newBasis}"+make_interval(days=>"QcCompletionDaysSnapshot")
               WHERE "Status"='FINALIZED'
                 AND "QcDueAt" IS DISTINCT FROM "{newBasis}"+make_interval(days=>"QcCompletionDaysSnapshot");

              EXECUTE replace(guard_definition,old_guard_deadline,new_guard_deadline);
              EXECUTE replace(finalizer_definition,old_finalizer_deadline,new_finalizer_deadline);
            END $migration$;
            """;
        }
    }
}
