using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// A2 stock adjustment: the record, its decisions and the physical/FIFO posting. The ledger
/// admits a new document kind (STOCK_ADJUSTMENT batches, a StockAdjustmentLineId document
/// reference and an OriginStockAdjustmentLineId origin on movements, an ADJUSTMENT_STATED FIFO
/// layer and an adjustment-referenced FIFO consumption); the batch, movement and reconcile
/// guards, the issue FIFO consumer and the FIFO valuation report are rewritten from their
/// installed bodies with exact-occurrence guards. Approval bands, exclusions, backdating and the
/// period rule are the domain policies of the A2 foundation; posting locks the inventory period
/// row exactly as period close does. Serialized identity change is not posted by this migration.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260920210000_StockAdjustmentPosting")]
public sealed class StockAdjustmentPosting : Migration
{
    private const string BatchGuard = "advance.stores_p3b_batch_insert_guard()";
    private const string MovementGuard = "advance.stores_p3b_movement_guard()";
    private const string ReconcileGuard = "advance.stores_p3b_reconcile_batch()";
    private const string IssueConsumer = "advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text)";
    private const string FifoReport = "advance.company_report_fifo_valuation(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)";

    private const string BatchBefore = "  ELSIF NEW.\"PostingKind\"='OPENING_BALANCE' THEN\n    expected_type:='OPENING_BALANCE';";
    private const string BatchAfter = "  ELSIF NEW.\"PostingKind\"='STOCK_ADJUSTMENT' THEN\n    expected_type:='STOCK_ADJUSTMENT';\n    SELECT \"CompanyId\",\"AdjustmentNumber\" INTO source_company,expected_number FROM advance.stock_adjustments WHERE \"Id\"=NEW.\"StockAdjustmentId\";\n  ELSIF NEW.\"PostingKind\"='OPENING_BALANCE' THEN\n    expected_type:='OPENING_BALANCE';";
    private const string MovementBefore = "  ELSIF NEW.\"OpeningStockLineId\" IS NOT NULL THEN\n";
    private const string MovementAfter = "  ELSIF NEW.\"StockAdjustmentLineId\" IS NOT NULL THEN\n    SELECT a.\"CompanyId\",a.\"Id\",l.\"ItemId\" INTO source_company,source_header,source_item FROM advance.stock_adjustment_lines l JOIN advance.stock_adjustments a ON a.\"Id\"=l.\"StockAdjustmentId\" WHERE l.\"Id\"=NEW.\"StockAdjustmentLineId\";\n    IF b.\"PostingKind\"<>'STOCK_ADJUSTMENT' OR source_header<>b.\"StockAdjustmentId\" THEN RAISE EXCEPTION 'Stock adjustment movement source does not match its batch.'; END IF;\n    IF NEW.\"ConditionCode\"<>'AVAILABLE' OR NEW.\"MovementLeg\" NOT IN ('RECEIPT_IN','CONSUMPTION_OUT') OR (NEW.\"MovementLeg\"='RECEIPT_IN' AND (NEW.\"QuantityIn\"<=0 OR NEW.\"QuantityOut\"<>0)) OR (NEW.\"MovementLeg\"='CONSUMPTION_OUT' AND (NEW.\"QuantityOut\"<=0 OR NEW.\"QuantityIn\"<>0)) THEN RAISE EXCEPTION 'Stock adjustment moves AVAILABLE stock in or out only.'; END IF;\n  ELSIF NEW.\"OpeningStockLineId\" IS NOT NULL THEN\n";
    private const string ReconcileBefore = "  ELSIF b.\"PostingKind\"='OPENING_BALANCE' THEN\n";
    private const string ReconcileAfter = "  ELSIF b.\"PostingKind\"='STOCK_ADJUSTMENT' THEN\n    IF (SELECT coalesce(sum(\"QuantityIn\"-\"QuantityOut\"),0) FROM advance.stock_movements WHERE \"StockPostingBatchId\"=b.\"Id\")<>(SELECT coalesce(sum(l.\"QuantityChange\"),0) FROM advance.stock_adjustment_lines l JOIN advance.stock_adjustments a ON a.\"Id\"=l.\"StockAdjustmentId\" WHERE a.\"Id\"=b.\"StockAdjustmentId\" AND l.\"RevisionNumber\"=a.\"CurrentRevisionNumber\") THEN RAISE EXCEPTION 'Stock adjustment posting does not reconcile to its approved lines.'; END IF;\n  ELSIF b.\"PostingKind\"='OPENING_BALANCE' THEN\n";
    private const string OriginBefore = "(f.\"OpeningStockLineId\" IS NOT NULL AND m.\"OpeningStockLineId\"=f.\"OpeningStockLineId\")";
    // IN-list constraints are replaced whole (PostgreSQL re-renders the ARRAY literal on re-add).
    private const string BatchKindsBefore = "\"PostingKind\" IN ('GRN_CUSTODY','QC_DISPOSITION','CONCESSION_ACCEPTANCE','MATERIAL_ISSUE','MATERIAL_RETURN','FITMENT_CONSUMPTION','OPENING_BALANCE','DC_DISPATCH','DC_RETURN_CUSTODY','REVERSAL')";
    private const string BatchKindsAfter = "\"PostingKind\" IN ('GRN_CUSTODY','QC_DISPOSITION','CONCESSION_ACCEPTANCE','MATERIAL_ISSUE','MATERIAL_RETURN','FITMENT_CONSUMPTION','OPENING_BALANCE','STOCK_ADJUSTMENT','DC_DISPATCH','DC_RETURN_CUSTODY','REVERSAL')";
    private const string FifoLayerBefore = "\"QuantityReceived\">0 AND \"UnitCost\">=0 AND \"LayerValue\">=0 AND \"CostBasis\" IN ('PO_PROVISIONAL_IDENTICAL','OPENING_LANDED') AND num_nonnulls(\"GoodsReceiptLineId\",\"OpeningStockLineId\")=1";
    private const string FifoLayerAfter = "\"QuantityReceived\">0 AND \"UnitCost\">=0 AND \"LayerValue\">=0 AND \"CostBasis\" IN ('PO_PROVISIONAL_IDENTICAL','OPENING_LANDED','ADJUSTMENT_STATED') AND num_nonnulls(\"GoodsReceiptLineId\",\"OpeningStockLineId\",\"StockAdjustmentLineId\")=1";
    // Layers with no GRN and no opening line are adjustment-stated; both allocators must admit them.
    private const string EligibilityBefore = "(f.\"OpeningStockLineId\" IS NOT NULL OR (g.\"DocumentKind\"='NORMAL'";
    private const string EligibilityAfter = "(f.\"OpeningStockLineId\" IS NOT NULL OR f.\"StockAdjustmentLineId\" IS NOT NULL OR (g.\"DocumentKind\"='NORMAL'";
    private const string OriginAfter ="(f.\"OpeningStockLineId\" IS NOT NULL AND m.\"OpeningStockLineId\"=f.\"OpeningStockLineId\") OR (f.\"StockAdjustmentLineId\" IS NOT NULL AND m.\"StockAdjustmentLineId\"=f.\"StockAdjustmentLineId\")";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf(Tables));
        migrationBuilder.Sql(MigrationText.Lf(LedgerColumns));
        migrationBuilder.Sql(InstalledFunctionSql.ReplaceCheck("advance.stock_posting_batches", "CK_stock_posting_batch_kind",
            "OPENING_BALANCE", "STOCK_ADJUSTMENT", BatchKindsAfter));
        migrationBuilder.Sql(InstalledFunctionSql.RewriteCheck("advance.stock_posting_batches", "CK_stock_posting_batch_source",
            "num_nonnulls(\"GoodsReceiptId\", ", "num_nonnulls(\"StockAdjustmentId\", \"GoodsReceiptId\", ", 2));
        migrationBuilder.Sql(InstalledFunctionSql.RewriteCheck("advance.stock_movements", "CK_stock_movement_v2_contract",
            "num_nonnulls(\"GoodsReceiptLineId\", ", "num_nonnulls(\"StockAdjustmentLineId\", \"GoodsReceiptLineId\", "));
        migrationBuilder.Sql(InstalledFunctionSql.RewriteCheck("advance.stock_movements", "CK_stock_movement_outbound_origin",
            "(\"OriginOpeningStockLineId\" IS NOT NULL)", "(\"OriginOpeningStockLineId\" IS NOT NULL) OR (\"OriginStockAdjustmentLineId\" IS NOT NULL)"));
        migrationBuilder.Sql(InstalledFunctionSql.ReplaceCheck("advance.fifo_inventory_cost_layers", "CK_fifo_cost_layer",
            "OPENING_LANDED", "ADJUSTMENT_STATED", FifoLayerAfter));
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(BatchGuard, (BatchBefore, BatchAfter)));
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(MovementGuard, (MovementBefore, MovementAfter)));
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(ReconcileGuard, (ReconcileBefore, ReconcileAfter)));
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(IssueConsumer, (OriginBefore, OriginAfter, 1), (EligibilityBefore, EligibilityAfter, 1)));
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(FifoReport, (OriginBefore, OriginAfter, 2), (EligibilityBefore, EligibilityAfter, 2)));
        migrationBuilder.Sql(MigrationText.Lf(PostingFunction));
        migrationBuilder.Sql(MigrationText.Lf(Page));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf("""
            DO $guard$
            BEGIN
              IF EXISTS (SELECT 1 FROM advance.stock_adjustments) THEN
                RAISE EXCEPTION 'Refusing rollback: stock adjustment evidence exists.';
              END IF;
            END $guard$;
            DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('stores.stock-adjustments')::uuid AND "CreatedBy"='StockAdjustmentPosting';
            DELETE FROM advance.page_definitions WHERE "PageKey"='stores.stock-adjustments' AND "CreatedBy"='StockAdjustmentPosting';
            DROP FUNCTION advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text);
            DROP FUNCTION advance.fifo_carrying_value_preview(uuid,uuid,numeric);
            DROP FUNCTION advance.guard_stock_adjustment_evidence() CASCADE;
            """));
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(FifoReport, (OriginAfter, OriginBefore, 2), (EligibilityAfter, EligibilityBefore, 2)));
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(IssueConsumer, (OriginAfter, OriginBefore, 1), (EligibilityAfter, EligibilityBefore, 1)));
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(ReconcileGuard, (ReconcileAfter, ReconcileBefore)));
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(MovementGuard, (MovementAfter, MovementBefore)));
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(BatchGuard, (BatchAfter, BatchBefore)));
        migrationBuilder.Sql(InstalledFunctionSql.ReplaceCheck("advance.fifo_inventory_cost_layers", "CK_fifo_cost_layer",
            "ADJUSTMENT_STATED", "NEVER_PRESENT", FifoLayerBefore));
        migrationBuilder.Sql(InstalledFunctionSql.RewriteCheck("advance.stock_movements", "CK_stock_movement_outbound_origin",
            "(\"OriginOpeningStockLineId\" IS NOT NULL) OR (\"OriginStockAdjustmentLineId\" IS NOT NULL)", "(\"OriginOpeningStockLineId\" IS NOT NULL)"));
        migrationBuilder.Sql(InstalledFunctionSql.RewriteCheck("advance.stock_movements", "CK_stock_movement_v2_contract",
            "num_nonnulls(\"StockAdjustmentLineId\", \"GoodsReceiptLineId\", ", "num_nonnulls(\"GoodsReceiptLineId\", "));
        migrationBuilder.Sql(InstalledFunctionSql.RewriteCheck("advance.stock_posting_batches", "CK_stock_posting_batch_source",
            "num_nonnulls(\"StockAdjustmentId\", \"GoodsReceiptId\", ", "num_nonnulls(\"GoodsReceiptId\", ", 2));
        migrationBuilder.Sql(InstalledFunctionSql.ReplaceCheck("advance.stock_posting_batches", "CK_stock_posting_batch_kind",
            "STOCK_ADJUSTMENT", "NEVER_PRESENT", BatchKindsBefore));
        migrationBuilder.Sql(MigrationText.Lf("""
            ALTER TABLE advance.fifo_cost_consumptions DROP CONSTRAINT "CK_fifo_cost_consumption_source";
            ALTER TABLE advance.fifo_cost_consumptions ALTER COLUMN "MaterialIssueLineId" SET NOT NULL;
            ALTER TABLE advance.fifo_cost_consumptions DROP COLUMN "StockAdjustmentLineId";
            ALTER TABLE advance.fifo_inventory_cost_layers DROP COLUMN "StockAdjustmentLineId";
            ALTER TABLE advance.stock_movements DROP COLUMN "OriginStockAdjustmentLineId";
            ALTER TABLE advance.stock_movements DROP COLUMN "StockAdjustmentLineId";
            ALTER TABLE advance.stock_posting_batches DROP COLUMN "StockAdjustmentId";
            DROP TABLE advance.stock_adjustment_decisions;
            DROP TABLE advance.stock_adjustment_lines;
            DROP TABLE advance.stock_adjustments;
            """));
    }

    private const string Tables = """
        CREATE TABLE advance.stock_adjustments(
          "Id" uuid PRIMARY KEY,
          "CompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
          "AdjustmentNumber" varchar(40) NOT NULL,
          "WarehouseId" uuid NOT NULL,
          "ReasonKind" varchar(30) NOT NULL CHECK ("ReasonKind" IN ('COUNT_VARIANCE','DAMAGE_LOSS','CORRECTION')),
          "EffectiveDate" date NOT NULL,
          "InventoryPeriodId" uuid NOT NULL REFERENCES advance.financial_periods("Id"),
          "Status" varchar(20) NOT NULL CHECK ("Status" IN ('DRAFT','SUBMITTED','APPROVED','POSTED','REJECTED')),
          "CurrentRevisionNumber" integer NOT NULL CHECK ("CurrentRevisionNumber">0),
          "Remarks" varchar(1000) NOT NULL,
          "RecordedByEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
          "RecordedRoleAssignmentId" uuid NOT NULL,
          "CounterEmployeeIdsJson" jsonb NOT NULL DEFAULT '[]'::jsonb,
          "BackdateReason" varchar(1000) NULL,
          "BackdateEvidenceId" uuid NULL,
          "DaysBackdated" integer NOT NULL DEFAULT 0,
          "ApprovalSnapshotJson" jsonb NULL,
          "ReversesStockAdjustmentId" uuid NULL REFERENCES advance.stock_adjustments("Id"),
          "StockPostingBatchId" uuid NULL,
          "PostedAt" timestamptz NULL,
          "PostedByEmployeeId" uuid NULL,
          "PostingIdempotencyKey" varchar(100) NULL,
          "PostingRequestFingerprint" character(64) NULL,
          "IdempotencyKey" varchar(100) NOT NULL,
          "RequestFingerprint" character(64) NOT NULL,
          "CreatedAt" timestamptz NOT NULL,
          "CreatedBy" varchar(160) NOT NULL,
          "UpdatedAt" timestamptz NULL,
          "UpdatedBy" varchar(160) NULL,
          "Version" bigint NOT NULL DEFAULT 0,
          CONSTRAINT "AK_stock_adjustments_CompanyId_Id" UNIQUE ("CompanyId","Id"),
          CONSTRAINT "UQ_stock_adjustments_number" UNIQUE ("CompanyId","AdjustmentNumber"),
          CONSTRAINT "UQ_stock_adjustments_key" UNIQUE ("CompanyId","IdempotencyKey"),
          CONSTRAINT "FK_stock_adjustments_warehouses" FOREIGN KEY ("CompanyId","WarehouseId") REFERENCES advance.warehouses("CompanyId","Id"),
          CONSTRAINT "CK_stock_adjustments_posted" CHECK (("Status"='POSTED')=("StockPostingBatchId" IS NOT NULL))
        );
        CREATE TABLE advance.stock_adjustment_lines(
          "Id" uuid PRIMARY KEY,
          "CompanyId" uuid NOT NULL,
          "StockAdjustmentId" uuid NOT NULL,
          "RevisionNumber" integer NOT NULL,
          "LineNumber" integer NOT NULL,
          "ItemId" uuid NOT NULL REFERENCES advance.items("Id"),
          "WarehouseConditionLocationId" uuid NOT NULL,
          "LotNumber" varchar(160) NULL,
          "SerialNumber" varchar(300) NULL,
          "QuantityChange" numeric(24,6) NOT NULL,
          "UnitValue" numeric(24,6) NULL,
          "AcceptedLineValue" numeric(24,6) NOT NULL,
          "Remarks" varchar(500) NULL,
          "CreatedAt" timestamptz NOT NULL,
          "CreatedBy" varchar(160) NOT NULL,
          CONSTRAINT "AK_stock_adjustment_lines_CompanyId_Id" UNIQUE ("CompanyId","Id"),
          CONSTRAINT "UQ_stock_adjustment_lines_revision_line" UNIQUE ("CompanyId","StockAdjustmentId","RevisionNumber","LineNumber"),
          CONSTRAINT "FK_stock_adjustment_lines_adjustment" FOREIGN KEY ("CompanyId","StockAdjustmentId") REFERENCES advance.stock_adjustments("CompanyId","Id"),
          CONSTRAINT "FK_stock_adjustment_lines_location" FOREIGN KEY ("CompanyId","WarehouseConditionLocationId") REFERENCES advance.warehouse_condition_locations("CompanyId","Id"),
          CONSTRAINT "CK_stock_adjustment_lines_values" CHECK ("QuantityChange"<>0 AND "AcceptedLineValue">=0 AND ("QuantityChange"<0 OR ("UnitValue" IS NOT NULL AND "UnitValue">=0)))
        );
        CREATE TABLE advance.stock_adjustment_decisions(
          "Id" uuid PRIMARY KEY,
          "CompanyId" uuid NOT NULL,
          "StockAdjustmentId" uuid NOT NULL,
          "RevisionNumber" integer NOT NULL,
          "Decision" varchar(20) NOT NULL CHECK ("Decision" IN ('APPROVE','REJECT')),
          "EmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
          "RoleCode" varchar(100) NOT NULL,
          "RoleAssignmentId" uuid NOT NULL,
          "RoleAssignmentType" varchar(20) NOT NULL,
          "DecidedAt" timestamptz NOT NULL,
          "Reason" varchar(1000) NOT NULL,
          "IdempotencyKey" varchar(100) NOT NULL,
          "CreatedBy" varchar(160) NOT NULL,
          CONSTRAINT "UQ_stock_adjustment_decisions_key" UNIQUE ("CompanyId","IdempotencyKey"),
          CONSTRAINT "UQ_stock_adjustment_decisions_role" UNIQUE ("CompanyId","StockAdjustmentId","RevisionNumber","RoleCode"),
          CONSTRAINT "FK_stock_adjustment_decisions_adjustment" FOREIGN KEY ("CompanyId","StockAdjustmentId") REFERENCES advance.stock_adjustments("CompanyId","Id")
        );
        CREATE INDEX "IX_stock_adjustments_CompanyId_Status" ON advance.stock_adjustments("CompanyId","Status");
        CREATE INDEX "IX_stock_adjustment_lines_adjustment" ON advance.stock_adjustment_lines("CompanyId","StockAdjustmentId","RevisionNumber");
        CREATE FUNCTION advance.guard_stock_adjustment_evidence() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $f$
        BEGIN
          IF TG_TABLE_NAME IN ('stock_adjustment_lines','stock_adjustment_decisions') AND TG_OP<>'INSERT' THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Stock adjustment lines and decisions are immutable.';
          END IF;
          IF TG_TABLE_NAME='stock_adjustments' AND TG_OP='DELETE' THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Stock adjustment evidence is immutable.';
          END IF;
          IF TG_TABLE_NAME='stock_adjustments' AND TG_OP='UPDATE' AND OLD."Status"='POSTED' THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='A posted stock adjustment is immutable; post a reversing adjustment.';
          END IF;
          RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
        END $f$;
        CREATE TRIGGER trg_stock_adjustment_guard BEFORE UPDATE OR DELETE ON advance.stock_adjustments FOR EACH ROW EXECUTE FUNCTION advance.guard_stock_adjustment_evidence();
        CREATE TRIGGER trg_stock_adjustment_line_guard BEFORE UPDATE OR DELETE ON advance.stock_adjustment_lines FOR EACH ROW EXECUTE FUNCTION advance.guard_stock_adjustment_evidence();
        CREATE TRIGGER trg_stock_adjustment_decision_guard BEFORE UPDATE OR DELETE ON advance.stock_adjustment_decisions FOR EACH ROW EXECUTE FUNCTION advance.guard_stock_adjustment_evidence();
        """;

    private const string LedgerColumns = """
        ALTER TABLE advance.stock_posting_batches ADD COLUMN "StockAdjustmentId" uuid NULL;
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "FK_stock_posting_batches_stock_adjustments" FOREIGN KEY ("CompanyId","StockAdjustmentId") REFERENCES advance.stock_adjustments("CompanyId","Id");
        ALTER TABLE advance.stock_movements ADD COLUMN "StockAdjustmentLineId" uuid NULL;
        ALTER TABLE advance.stock_movements ADD COLUMN "OriginStockAdjustmentLineId" uuid NULL;
        ALTER TABLE advance.stock_movements ADD CONSTRAINT "FK_stock_movements_StockAdjustmentLineId" FOREIGN KEY ("CompanyId","StockAdjustmentLineId") REFERENCES advance.stock_adjustment_lines("CompanyId","Id");
        ALTER TABLE advance.stock_movements ADD CONSTRAINT "FK_stock_movements_OriginStockAdjustmentLineId" FOREIGN KEY ("CompanyId","OriginStockAdjustmentLineId") REFERENCES advance.stock_adjustment_lines("CompanyId","Id");
        CREATE INDEX "IX_stock_movements_StockAdjustmentLineId" ON advance.stock_movements("CompanyId","StockAdjustmentLineId");
        ALTER TABLE advance.fifo_inventory_cost_layers ADD COLUMN "StockAdjustmentLineId" uuid NULL REFERENCES advance.stock_adjustment_lines("Id");
        ALTER TABLE advance.fifo_cost_consumptions ADD COLUMN "StockAdjustmentLineId" uuid NULL REFERENCES advance.stock_adjustment_lines("Id");
        ALTER TABLE advance.fifo_cost_consumptions ALTER COLUMN "MaterialIssueLineId" DROP NOT NULL;
        ALTER TABLE advance.fifo_cost_consumptions ADD CONSTRAINT "CK_fifo_cost_consumption_source" CHECK (num_nonnulls("MaterialIssueLineId","StockAdjustmentLineId")=1);
        """;

    private const string PostingFunction = """
        CREATE FUNCTION advance.post_stock_adjustment(p_company uuid,p_adjustment uuid,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text,p_key text,p_hash text)
        RETURNS TABLE("StockPostingBatchId" uuid,"Replayed" boolean) LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
        DECLARE adj advance.stock_adjustments%ROWTYPE; period advance.financial_periods%ROWTYPE; line advance.stock_adjustment_lines%ROWTYPE;
                loc advance.warehouse_condition_locations%ROWTYPE; organization text;
                holder_id uuid; ownership_id uuid; custody_id uuid; custody_assignment_id uuid; lot_id uuid; serial_id uuid;
                provenance_id uuid; batch_id uuid:=gen_random_uuid(); account_code text; identity_hash text; normalized text;
                ordinal integer:=0; remaining numeric; take_quantity numeric; balance record; layer record; pool_currency text;
        BEGIN
          SELECT "Code" INTO organization FROM advance.companies WHERE "Id"=p_company AND "IsActive" AND "Status"='ACTIVE';
          IF session_user<>'nexa_erp_runtime' OR organization IS NULL OR p_type NOT IN ('FULL','TEMPORARY')
             OR NOT advance.ordinary_command_context_valid(organization,p_actor,current_setting('advance.ordinary_identity_issuer',true),current_setting('advance.ordinary_identity_subject',true),p_role)
             OR NOT EXISTS(SELECT 1 FROM advance.command_requests r WHERE r."CommandId"=nullif(current_setting('advance.ordinary_command_id',true),'')::uuid AND r."Operation"='StockAdjustment.Decide' AND r."ResolvedRoleAssignmentId"=p_assignment)
             OR NOT EXISTS(SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,'approve',ARRAY[p_role]) a WHERE a."AssignmentId"=p_assignment AND a."AssignmentType"=p_type) THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Stock adjustment posting requires the current authorized decision command.';
          END IF;
          SELECT * INTO adj FROM advance.stock_adjustments WHERE "CompanyId"=p_company AND "Id"=p_adjustment FOR UPDATE;
          IF NOT FOUND THEN RAISE EXCEPTION 'Stock adjustment was not found.'; END IF;
          IF adj."PostingIdempotencyKey"=p_key THEN
            IF adj."PostingRequestFingerprint"<>p_hash THEN RAISE EXCEPTION 'Stock adjustment posting idempotency mismatch.'; END IF;
            RETURN QUERY SELECT adj."StockPostingBatchId",true; RETURN;
          END IF;
          IF adj."Status"<>'APPROVED' THEN RAISE EXCEPTION 'Only an approved stock adjustment can be posted.'; END IF;
          -- The inventory period row is locked exactly as period close locks it; a closed period refuses.
          SELECT * INTO period FROM advance.financial_periods WHERE "Id"=adj."InventoryPeriodId" AND "CompanyId"=p_company AND "PeriodType"='INVENTORY' FOR UPDATE;
          IF NOT FOUND OR period."Status"<>'OPEN' OR NOT period."IsActive" THEN RAISE EXCEPTION 'The inventory period is closed or missing; the adjustment cannot be posted.'; END IF;
          IF adj."EffectiveDate"<period."StartDate" OR adj."EffectiveDate">period."EndDate" OR adj."EffectiveDate">current_date THEN RAISE EXCEPTION 'The effective date is outside the open inventory period or in the future.'; END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended('STOCK-ADJUSTMENT:'||p_company,0));
          PERFORM set_config('sess.vendor_bill_write',txid_current()::text,true);
          INSERT INTO advance.inventory_account_holders
            ("Id","CompanyId","HolderType","HolderCompanyId","HolderCode","HolderNameSnapshot","IsActive","CreatedAt","CreatedBy","Version")
          SELECT gen_random_uuid(),p_company,'COMPANY',p_company,'COMPANY-INVENTORY',c."LegalName",true,clock_timestamp(),p_login,0
          FROM advance.companies c WHERE c."Id"=p_company ON CONFLICT ("CompanyId","HolderCode") DO NOTHING;
          SELECT "Id" INTO holder_id FROM advance.inventory_account_holders WHERE "CompanyId"=p_company AND "HolderCode"='COMPANY-INVENTORY' AND "HolderType"='COMPANY' AND "HolderCompanyId"=p_company AND "IsActive";
          INSERT INTO advance.inventory_ownership_accounts
            ("Id","CompanyId","AccountHolderId","AccountCode","OwnershipType","InventoryValuationBasis","CurrencyCode","IsActive","CreatedAt","CreatedBy","Version")
          VALUES(gen_random_uuid(),p_company,holder_id,'SESS-INVENTORY','SESS_INVENTORY','FIFO','INR',true,clock_timestamp(),p_login,0)
          ON CONFLICT ("CompanyId","AccountCode") DO NOTHING;
          SELECT "Id","CurrencyCode" INTO ownership_id,pool_currency FROM advance.inventory_ownership_accounts WHERE "CompanyId"=p_company AND "AccountCode"='SESS-INVENTORY' AND "AccountHolderId"=holder_id AND "IsActive";
          IF holder_id IS NULL OR ownership_id IS NULL THEN RAISE EXCEPTION 'SESS inventory ownership account is missing or incompatible.'; END IF;
          INSERT INTO advance.stock_posting_batches
            ("Id","CompanyId","PostingKind","StockAdjustmentId","ReferenceType","ReferenceNumber","PostingDate","PostedAt","PostedByEmployeeId","IdempotencyKey","RequestFingerprint","CorrelationId","CreatedAt","CreatedBy","Version")
          VALUES(batch_id,p_company,'STOCK_ADJUSTMENT',adj."Id",'STOCK_ADJUSTMENT',adj."AdjustmentNumber",adj."EffectiveDate",clock_timestamp(),p_actor,p_key,p_hash,p_hash,clock_timestamp(),p_login,0);
          FOR line IN SELECT l.* FROM advance.stock_adjustment_lines l WHERE l."StockAdjustmentId"=adj."Id" AND l."RevisionNumber"=adj."CurrentRevisionNumber" ORDER BY l."LineNumber" LOOP
            SELECT * INTO loc FROM advance.warehouse_condition_locations WHERE "CompanyId"=p_company AND "Id"=line."WarehouseConditionLocationId" AND "ConditionCode"='AVAILABLE' AND "IsActive" AND "WarehouseId"=adj."WarehouseId";
            IF NOT FOUND THEN RAISE EXCEPTION 'Adjustment line % needs an effective AVAILABLE location in the adjustment warehouse.',line."LineNumber"; END IF;
            IF line."SerialNumber" IS NOT NULL AND abs(line."QuantityChange")<>1 THEN RAISE EXCEPTION 'Adjustment line % names a serial and must change exactly one unit.',line."LineNumber"; END IF;
            PERFORM pg_advisory_xact_lock(hashtextextended('FIFO:'||p_company||':'||line."ItemId",0));
            account_code:='WH-'||left(replace(loc."WarehouseId"::text,'-',''),12)||'-'||left(replace(loc."RackBinId"::text,'-',''),12);
            INSERT INTO advance.inventory_custody_accounts
              ("Id","CompanyId","AccountHolderId","AccountCode","CustodyType","WarehouseId","RackBinId","IsActive","CreatedAt","CreatedBy","Version")
            VALUES(gen_random_uuid(),p_company,holder_id,account_code,'WAREHOUSE',loc."WarehouseId",loc."RackBinId",true,clock_timestamp(),p_login,0)
            ON CONFLICT ("CompanyId","AccountCode") DO NOTHING;
            SELECT "Id" INTO custody_id FROM advance.inventory_custody_accounts WHERE "CompanyId"=p_company AND "AccountCode"=account_code AND "CustodyType"='WAREHOUSE' AND "IsActive";
            IF line."QuantityChange">0 THEN
              lot_id:=NULL; serial_id:=NULL;
              IF line."LotNumber" IS NOT NULL THEN
                normalized:=upper(btrim(regexp_replace(line."LotNumber",'[[:space:]]+',' ','g')));
                INSERT INTO advance.inventory_lots("Id","CompanyId","ItemId","SupplierLotNumber","NormalizedSupplierLotNumber","CreatedAt","CreatedBy")
                VALUES(gen_random_uuid(),p_company,line."ItemId",line."LotNumber",normalized,clock_timestamp(),p_login) RETURNING "Id" INTO lot_id;
              END IF;
              IF line."SerialNumber" IS NOT NULL THEN
                normalized:=upper(regexp_replace(btrim(line."SerialNumber"),'[^A-Z0-9]','','g'));
                SELECT s."Id" INTO serial_id FROM advance.inventory_serials s WHERE s."CompanyId"=p_company AND s."NormalizedStoredSerialNumber"=normalized;
                IF serial_id IS NOT NULL AND NOT EXISTS(SELECT 1 FROM advance.inventory_serials s WHERE s."Id"=serial_id AND s."ItemId"=line."ItemId") THEN
                  RAISE EXCEPTION 'Adjustment line % names serial % which belongs to another item.',line."LineNumber",line."SerialNumber";
                END IF;
                IF serial_id IS NOT NULL AND EXISTS(SELECT 1 FROM advance.stock_movements m WHERE m."CompanyId"=p_company AND m."InventorySerialId"=serial_id GROUP BY m."InventorySerialId" HAVING sum(m."QuantityIn"-m."QuantityOut")>0) THEN
                  RAISE EXCEPTION 'Adjustment line % names serial % which is already in stock.',line."LineNumber",line."SerialNumber";
                END IF;
                IF serial_id IS NULL THEN
                  INSERT INTO advance.inventory_serials("Id","CompanyId","ItemId","StoredSerialNumber","NormalizedStoredSerialNumber","FirstCapturedAt","FirstCapturedByEmployeeId","CreatedAt","CreatedBy")
                  VALUES(gen_random_uuid(),p_company,line."ItemId",line."SerialNumber",normalized,clock_timestamp(),p_actor,clock_timestamp(),p_login) RETURNING "Id" INTO serial_id;
                END IF;
              END IF;
              custody_assignment_id:=md5('ADJ:CUST:'||p_company||':'||line."Id")::uuid;
              INSERT INTO advance.inventory_custody_assignments
                ("Id","CompanyId","CustodyAccountId","WarehouseId","RackBinId","AssignedQuantity","EffectiveFrom","IsCurrent","AssignmentReason","CreatedAt","CreatedBy","Version")
              VALUES(custody_assignment_id,p_company,custody_id,loc."WarehouseId",loc."RackBinId",line."QuantityChange",adj."EffectiveDate"::timestamp AT TIME ZONE 'UTC',true,'Stock adjustment '||adj."AdjustmentNumber",clock_timestamp(),p_login,0);
              identity_hash:=encode(pg_catalog.sha256(convert_to('ADJUSTMENT:'||p_company||':'||line."Id",'UTF8')),'hex');
              INSERT INTO advance.inventory_provenance_layers
                ("Id","CompanyId","ItemId","InventoryLotId","InventorySerialId","LayerType","QuantityCreated","UomId","Status","IdentityHash","CreatedAt","CreatedBy")
              SELECT gen_random_uuid(),p_company,line."ItemId",lot_id,serial_id,'ADJUSTMENT',line."QuantityChange",i."BaseUomId",'ACTIVE',identity_hash,clock_timestamp(),p_login FROM advance.items i WHERE i."Id"=line."ItemId"
              RETURNING "Id" INTO provenance_id;
              INSERT INTO advance.fifo_inventory_cost_layers
                ("Id","CompanyId","StockAdjustmentLineId","ItemId","QuantityReceived","UnitCost","LayerValue","ReceivedAt","CostBasis","CreatedAt","CreatedBy")
              VALUES(gen_random_uuid(),p_company,line."Id",line."ItemId",line."QuantityChange",line."UnitValue",round(line."QuantityChange"*line."UnitValue",6),adj."EffectiveDate"::timestamp AT TIME ZONE 'UTC','ADJUSTMENT_STATED',clock_timestamp(),p_login);
              ordinal:=ordinal+1;
              INSERT INTO advance.stock_movements
                ("Id","CompanyId","ItemId","WarehouseId","RackBinId","MovementType","ReferenceType","ReferenceNumber","QuantityIn","QuantityOut","PostingDate","LedgerSchemaVersion","WarehouseConditionLocationId","ConditionCode","StockPostingBatchId","BatchLineOrdinal","MovementLeg","StockAdjustmentLineId","OriginStockAdjustmentLineId","OwnershipAccountId","CustodyAssignmentId","InventoryProvenanceLayerId","InventoryLotId","InventorySerialId","PostingIdentity","CreatedAt","CreatedBy","Version")
              VALUES(gen_random_uuid(),p_company,line."ItemId",loc."WarehouseId",loc."RackBinId",'STOCK_ADJUSTMENT','STOCK_ADJUSTMENT',adj."AdjustmentNumber",line."QuantityChange",0,adj."EffectiveDate",2,loc."Id",'AVAILABLE',batch_id,ordinal,'RECEIPT_IN',line."Id",line."Id",ownership_id,custody_assignment_id,provenance_id,lot_id,serial_id,'STOCK_ADJUSTMENT:'||line."Id"||':IN',clock_timestamp(),p_login,0);
            ELSE
              remaining:=-line."QuantityChange";
              FOR balance IN
                SELECT m."OwnershipAccountId",m."CustodyAssignmentId",m."InventoryProvenanceLayerId",m."CustodyCaseLineId",m."InventoryLotId",m."InventorySerialId",
                       m."OriginGoodsReceiptLineId",m."OriginOpeningStockLineId",m."OriginStockAdjustmentLineId",m."GoodsReceiptLineLotAllocationId",
                       sum(m."QuantityIn"-m."QuantityOut") AS available,min(p."CreatedAt") AS provenance_at
                FROM advance.stock_movements m
                JOIN advance.inventory_custody_assignments ca ON ca."Id"=m."CustodyAssignmentId"
                JOIN advance.inventory_custody_accounts cc ON cc."Id"=ca."CustodyAccountId" AND cc."CustodyType"='WAREHOUSE'
                JOIN advance.inventory_provenance_layers p ON p."Id"=m."InventoryProvenanceLayerId"
                WHERE m."CompanyId"=p_company AND m."ItemId"=line."ItemId" AND m."WarehouseConditionLocationId"=loc."Id" AND m."ConditionCode"='AVAILABLE'
                  AND (line."SerialNumber" IS NULL OR m."InventorySerialId" IN (SELECT s."Id" FROM advance.inventory_serials s WHERE s."CompanyId"=p_company AND s."ItemId"=line."ItemId" AND s."NormalizedStoredSerialNumber"=upper(regexp_replace(btrim(line."SerialNumber"),'[^A-Z0-9]','','g'))))
                GROUP BY m."OwnershipAccountId",m."CustodyAssignmentId",m."InventoryProvenanceLayerId",m."CustodyCaseLineId",m."InventoryLotId",m."InventorySerialId",
                         m."OriginGoodsReceiptLineId",m."OriginOpeningStockLineId",m."OriginStockAdjustmentLineId",m."GoodsReceiptLineLotAllocationId"
                HAVING sum(m."QuantityIn"-m."QuantityOut")>0
                ORDER BY provenance_at,m."InventoryProvenanceLayerId"
              LOOP
                EXIT WHEN remaining<=0;
                take_quantity:=least(remaining,balance.available);
                ordinal:=ordinal+1;
                INSERT INTO advance.stock_movements
                  ("Id","CompanyId","ItemId","WarehouseId","RackBinId","MovementType","ReferenceType","ReferenceNumber","QuantityIn","QuantityOut","PostingDate","LedgerSchemaVersion","WarehouseConditionLocationId","ConditionCode","StockPostingBatchId","BatchLineOrdinal","MovementLeg","StockAdjustmentLineId","OriginGoodsReceiptLineId","OriginOpeningStockLineId","OriginStockAdjustmentLineId","GoodsReceiptLineLotAllocationId","OwnershipAccountId","CustodyAssignmentId","InventoryProvenanceLayerId","CustodyCaseLineId","InventoryLotId","InventorySerialId","PostingIdentity","CreatedAt","CreatedBy","Version")
                VALUES(gen_random_uuid(),p_company,line."ItemId",loc."WarehouseId",loc."RackBinId",'STOCK_ADJUSTMENT','STOCK_ADJUSTMENT',adj."AdjustmentNumber",0,take_quantity,adj."EffectiveDate",2,loc."Id",'AVAILABLE',batch_id,ordinal,'CONSUMPTION_OUT',line."Id",balance."OriginGoodsReceiptLineId",balance."OriginOpeningStockLineId",balance."OriginStockAdjustmentLineId",balance."GoodsReceiptLineLotAllocationId",balance."OwnershipAccountId",balance."CustodyAssignmentId",balance."InventoryProvenanceLayerId",balance."CustodyCaseLineId",balance."InventoryLotId",balance."InventorySerialId",'STOCK_ADJUSTMENT:'||line."Id"||':OUT:'||ordinal,clock_timestamp(),p_login,0);
                remaining:=remaining-take_quantity;
              END LOOP;
              IF remaining>0 THEN RAISE EXCEPTION 'Adjustment line % removes more than the AVAILABLE Stores stock at its location.',line."LineNumber"; END IF;
              -- FIFO: the removed quantity consumes the oldest layers of the item in the company pool.
              remaining:=-line."QuantityChange";
              FOR layer IN
                SELECT f."Id",f."UnitCost",f."QuantityReceived"-coalesce((SELECT sum(c."Quantity"-coalesce((SELECT sum(r."Quantity") FROM advance.fifo_cost_restorations r WHERE r."CompanyId"=p_company AND r."FifoCostConsumptionId"=c."Id"),0))
                  FROM advance.fifo_cost_consumptions c WHERE c."CompanyId"=p_company AND c."FifoInventoryCostLayerId"=f."Id"),0) AS available
                FROM advance.fifo_inventory_cost_layers f WHERE f."CompanyId"=p_company AND f."ItemId"=line."ItemId"
                ORDER BY f."ReceivedAt",f."Id" FOR UPDATE OF f
              LOOP
                EXIT WHEN remaining<=0;
                IF layer.available<=0 THEN CONTINUE; END IF;
                take_quantity:=least(remaining,layer.available);
                INSERT INTO advance.fifo_cost_consumptions
                  ("Id","CompanyId","FifoInventoryCostLayerId","StockAdjustmentLineId","Quantity","UnitCost","ConsumedValue","ConsumedAt","CreatedBy")
                VALUES(gen_random_uuid(),p_company,layer."Id",line."Id",take_quantity,layer."UnitCost",round(take_quantity*layer."UnitCost",6),clock_timestamp(),p_login);
                remaining:=remaining-take_quantity;
              END LOOP;
              IF remaining>0 THEN RAISE EXCEPTION 'Adjustment line % removes more than the FIFO cost layers hold.',line."LineNumber"; END IF;
            END IF;
          END LOOP;
          IF ordinal=0 THEN RAISE EXCEPTION 'The approved revision has no lines to post.'; END IF;
          UPDATE advance.stock_adjustments SET "Status"='POSTED',"StockPostingBatchId"=batch_id,"PostedAt"=clock_timestamp(),"PostedByEmployeeId"=p_actor,
            "PostingIdempotencyKey"=p_key,"PostingRequestFingerprint"=p_hash,"UpdatedAt"=clock_timestamp(),"UpdatedBy"=p_login,"Version"="Version"+1 WHERE "Id"=adj."Id";
          RETURN QUERY SELECT batch_id,false;
        END $f$;
        -- The carrying value a removal would consume, oldest layer first, net of restorations: the
        -- service records it as the accepted line value so the approval band is a FIFO fact, not an
        -- average. NULL when the layers hold less than the quantity.
        CREATE FUNCTION advance.fifo_carrying_value_preview(p_company uuid,p_item uuid,p_quantity numeric)
        RETURNS numeric LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
        DECLARE remaining numeric:=p_quantity; total numeric:=0; layer record; take_quantity numeric;
        BEGIN
          IF session_user<>'nexa_erp_runtime' OR p_quantity IS NULL OR p_quantity<=0 THEN RETURN NULL; END IF;
          FOR layer IN
            SELECT f."UnitCost",f."QuantityReceived"-coalesce((SELECT sum(c."Quantity"-coalesce((SELECT sum(r."Quantity") FROM advance.fifo_cost_restorations r WHERE r."CompanyId"=p_company AND r."FifoCostConsumptionId"=c."Id"),0))
              FROM advance.fifo_cost_consumptions c WHERE c."CompanyId"=p_company AND c."FifoInventoryCostLayerId"=f."Id"),0) AS available
            FROM advance.fifo_inventory_cost_layers f WHERE f."CompanyId"=p_company AND f."ItemId"=p_item
            ORDER BY f."ReceivedAt",f."Id"
          LOOP
            EXIT WHEN remaining<=0;
            IF layer.available<=0 THEN CONTINUE; END IF;
            take_quantity:=least(remaining,layer.available);
            total:=total+round(take_quantity*layer."UnitCost",6);
            remaining:=remaining-take_quantity;
          END LOOP;
          RETURN CASE WHEN remaining>0 THEN NULL ELSE total END;
        END $f$;
        REVOKE ALL ON FUNCTION advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text),advance.fifo_carrying_value_preview(uuid,uuid,numeric) FROM PUBLIC;
        DO $roles$ BEGIN
          IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            ALTER FUNCTION advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.fifo_carrying_value_preview(uuid,uuid,numeric) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.guard_stock_adjustment_evidence() OWNER TO nexa_erp_owner;
            ALTER TABLE advance.stock_adjustments OWNER TO nexa_erp_owner;
            ALTER TABLE advance.stock_adjustment_lines OWNER TO nexa_erp_owner;
            ALTER TABLE advance.stock_adjustment_decisions OWNER TO nexa_erp_owner;
            REVOKE ALL ON FUNCTION advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text),advance.fifo_carrying_value_preview(uuid,uuid,numeric) FROM nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text),advance.fifo_carrying_value_preview(uuid,uuid,numeric) TO nexa_erp_runtime;
            GRANT SELECT,INSERT,UPDATE ON advance.stock_adjustments,advance.stock_adjustment_lines,advance.stock_adjustment_decisions TO nexa_erp_runtime;
          END IF;
        END $roles$;
        """;

    private const string Page = """
        INSERT INTO advance.page_definitions("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
        VALUES(md5('stores.stock-adjustments')::uuid,'stores.stock-adjustments','Stores','Stock adjustments','/stores/stock-adjustments',true,now(),'StockAdjustmentPosting',0);
        INSERT INTO advance.role_page_permissions
          ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit",
           "CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
        SELECT md5('stores.stock-adjustments:'||r."Code")::uuid,r."Id",md5('stores.stock-adjustments')::uuid,
          true, r."Code" IN ('STORES_EXECUTIVE','STORES_MANAGER'), r."Code" IN ('STORES_EXECUTIVE','STORES_MANAGER'), r."Code" IN ('STORES_EXECUTIVE','STORES_MANAGER'), false, false,
          r."Code" IN ('STORES_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR','ACCOUNTS_MANAGER'), r."Code" IN ('STORES_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR','ACCOUNTS_MANAGER'), false, false, false,
          false, false, true, true, true, false, false, r."Code" IN ('STORES_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR','ACCOUNTS_MANAGER'), true, false, now(), 'StockAdjustmentPosting', 0
        FROM advance.roles r WHERE r."IsActive" AND r."Code" IN ('STORES_EXECUTIVE','STORES_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR','ACCOUNTS_MANAGER');
        """;
}
