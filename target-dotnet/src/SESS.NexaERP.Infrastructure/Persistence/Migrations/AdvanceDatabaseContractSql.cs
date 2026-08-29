namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class AdvanceDatabaseContractSql
{
    internal static string InstallRev869A => AdvanceSchemaSql.Expand(InstallRev869ATemplate);
    internal static string InstallRev869B => AdvanceSchemaSql.Expand(InstallRev869BTemplate);
    internal static string ReconcileRev869BTransitionGuard => AdvanceSchemaSql.Expand(
        ExtractFunction(InstallRev869BTemplate, "CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_enforce_transition()")
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace(
                "ELSIF TG_TABLE_NAME = 'purchase_orders' AND NEW.\"Status\" NOT IN ('Draft','RevisionDraft') THEN",
                "ELSIF TG_TABLE_NAME = 'purchase_orders' THEN\n                            IF NEW.\"Status\" NOT IN ('Draft','RevisionDraft') THEN")
            .Replace(
                "ELSIF TG_TABLE_NAME = 'purchase_orders' AND NEW.\"Status\" = 'RevisionDraft' AND NOT EXISTS (",
                "ELSIF NEW.\"Status\" = 'RevisionDraft' AND NOT EXISTS (")
            .Replace(
                "RAISE EXCEPTION 'RevisionDraft requires an immutable rejected predecessor.';",
                "RAISE EXCEPTION 'RevisionDraft requires an immutable rejected predecessor.';\n                            END IF;")
            .Replace("IF TG_TABLE_NAME = 'request_for_quotations' AND\n", "IF TG_TABLE_NAME = 'request_for_quotations' THEN\n                        IF ")
            .Replace("RAISE EXCEPTION 'RFQ organization and parent are immutable.';", "RAISE EXCEPTION 'RFQ organization and parent are immutable.';\n                        END IF;")
            .Replace("ELSIF TG_TABLE_NAME = 'rfq_vendor_invitations' AND\n", "ELSIF TG_TABLE_NAME = 'rfq_vendor_invitations' THEN\n                        IF ")
            .Replace("RAISE EXCEPTION 'RFQ invitation parents are immutable.';", "RAISE EXCEPTION 'RFQ invitation parents are immutable.';\n                        END IF;")
            .Replace("ELSIF TG_TABLE_NAME = 'vendor_quotations' AND\n", "ELSIF TG_TABLE_NAME = 'vendor_quotations' THEN\n                        IF ")
            .Replace("RAISE EXCEPTION 'Quotation organization and parents are immutable.';", "RAISE EXCEPTION 'Quotation organization and parents are immutable.';\n                        END IF;")
            .Replace("ELSIF TG_TABLE_NAME = 'commercial_comparisons' AND\n", "ELSIF TG_TABLE_NAME = 'commercial_comparisons' THEN\n                        IF ")
            .Replace("RAISE EXCEPTION 'Comparison organization and parent are immutable.';", "RAISE EXCEPTION 'Comparison organization and parent are immutable.';\n                        END IF;")
            .Replace("ELSIF TG_TABLE_NAME = 'purchase_orders' AND\n", "ELSIF TG_TABLE_NAME = 'purchase_orders' THEN\n                        IF ")
            .Replace("RAISE EXCEPTION 'Purchase order organization, provenance and version ancestry are immutable.';", "RAISE EXCEPTION 'Purchase order organization, provenance and version ancestry are immutable.';\n                        END IF;")
            .Replace("IF TG_TABLE_NAME = 'commercial_comparisons' AND NEW.\"Status\" IN ('PendingApproval','Approved') AND (", "IF TG_TABLE_NAME = 'commercial_comparisons' THEN\n                    IF NEW.\"Status\" IN ('PendingApproval','Approved') AND (")
            .Replace("THEN RAISE EXCEPTION 'Comparison snapshot is incomplete or does not exactly reconcile.'; END IF;", "THEN RAISE EXCEPTION 'Comparison snapshot is incomplete or does not exactly reconcile.'; END IF;\n                    END IF;"));
    internal static string ReconcileRev869BParentGuard => AdvanceSchemaSql.Expand(
        new[]
        {
            ("vendor_quotation_lines", "Quotation line parent contract mismatch."),
            ("quotation_technical_verifications", "Technical verification parent contract mismatch."),
            ("commercial_comparison_lines", "Comparison line parent contract mismatch."),
            ("purchase_orders", "Purchase order parent contract mismatch."),
            ("purchase_order_lines", "Purchase order line parent contract mismatch."),
            ("material_followup_handoffs", "Material follow-up parent contract mismatch.")
        }.Aggregate(
            ExtractFunction(InstallRev869BTemplate, "CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_validate_parent_contract()")
                .Replace("\r\n", "\n", StringComparison.Ordinal),
            static (sql, guard) => NestTableGuard(sql, guard.Item1, guard.Item2)));
    internal static string ReconcileRev869BSnapshotGuard => AdvanceSchemaSql.Expand(
        ExtractFunction(InstallRev869BTemplate, "CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_guard_controlled_snapshot()")
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("ARRAY['Status','Version','IsCurrentRevision','UpdatedAt','UpdatedBy']", "ARRAY['Status','Version','IsCurrentRevision','TransitionCorrelationId','UpdatedAt','UpdatedBy']", StringComparison.Ordinal)
            .Replace("ARRAY['Status','Version','UpdatedAt','UpdatedBy']", "ARRAY['Status','CompletedApprovalStepCount','Version','TransitionCorrelationId','UpdatedAt','UpdatedBy']", StringComparison.Ordinal)
            .Replace("ARRAY['Status','Version','IsCurrentVersion','IssuedAt','CancelledAt','CancellationReason','UpdatedAt','UpdatedBy']", "ARRAY['Status','ApprovalRoute','ApprovalCycle','RequiredApprovalStepCount','CompletedApprovalStepCount','ApprovalWorkflowSnapshotJson','Version','IsCurrentVersion','IssuedAt','CancelledAt','CancellationReason','TransitionCorrelationId','UpdatedAt','UpdatedBy']", StringComparison.Ordinal)
            .Replace("IF TG_TABLE_NAME = 'vendor_quotations' AND\n", "IF TG_TABLE_NAME = 'vendor_quotations' THEN\n                        IF ", StringComparison.Ordinal)
            .Replace("RAISE EXCEPTION 'Submitted quotation provenance and commercial terms are immutable.';", "RAISE EXCEPTION 'Submitted quotation provenance and commercial terms are immutable.';\n                        END IF;", StringComparison.Ordinal)
            .Replace("ELSIF TG_TABLE_NAME = 'commercial_comparisons' AND OLD.\"Status\" NOT IN ('Draft','RevisionRequested') AND\n", "ELSIF TG_TABLE_NAME = 'commercial_comparisons' THEN\n                        IF OLD.\"Status\" NOT IN ('Draft','RevisionRequested') AND\n", StringComparison.Ordinal)
            .Replace("RAISE EXCEPTION 'Submitted comparison snapshot is immutable.';", "RAISE EXCEPTION 'Submitted comparison snapshot is immutable.';\n                        END IF;", StringComparison.Ordinal)
            .Replace("ELSIF TG_TABLE_NAME = 'commercial_comparison_lines' AND\n", "ELSIF TG_TABLE_NAME = 'commercial_comparison_lines' THEN\n                        IF ", StringComparison.Ordinal)
            .Replace("RAISE EXCEPTION 'Submitted comparison line snapshot is immutable.';", "RAISE EXCEPTION 'Submitted comparison line snapshot is immutable.';\n                        END IF;", StringComparison.Ordinal)
            .Replace("ELSIF TG_TABLE_NAME = 'purchase_orders' AND\n", "ELSIF TG_TABLE_NAME = 'purchase_orders' THEN\n                        IF ", StringComparison.Ordinal)
            .Replace("RAISE EXCEPTION 'Purchase order commercial and provenance snapshot is immutable.';", "RAISE EXCEPTION 'Purchase order commercial and provenance snapshot is immutable.';\n                        END IF;", StringComparison.Ordinal));
    internal static string RestoreRev869BGuards => AdvanceSchemaSql.Expand(
        ExtractFunction(InstallRev869BTemplate, "CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_guard_controlled_snapshot()") + Environment.NewLine +
        ExtractFunction(InstallRev869BTemplate, "CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_enforce_transition()") + Environment.NewLine +
        ExtractFunction(InstallRev869BTemplate, "CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_validate_parent_contract()"));
    internal static string RemoveRev869B => AdvanceSchemaSql.Expand(RemoveRev869BTemplate);
    internal static string RemoveRev869A => AdvanceSchemaSql.Expand(RemoveRev869ATemplate);

    private static string ExtractFunction(string template, string marker)
    {
        var start = template.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException($"SQL function marker was not found: {marker}");
        const string terminator = "END $rev869b$;";
        var end = template.IndexOf(terminator, start, StringComparison.Ordinal);
        if (end < 0) throw new InvalidOperationException($"SQL function terminator was not found after: {marker}");
        return template.Substring(start, end + terminator.Length - start);
    }

    private static string NestTableGuard(string sql, string table, string message)
    {
        sql = sql.Replace($"IF TG_TABLE_NAME = '{table}' AND NOT EXISTS (", $"IF TG_TABLE_NAME = '{table}' THEN\n                        IF NOT EXISTS (", StringComparison.Ordinal);
        sql = sql.Replace($"ELSIF TG_TABLE_NAME = '{table}' AND NOT EXISTS (", $"ELSIF TG_TABLE_NAME = '{table}' THEN\n                        IF NOT EXISTS (", StringComparison.Ordinal);
        return sql.Replace($"RAISE EXCEPTION '{message}';", $"RAISE EXCEPTION '{message}';\n                        END IF;", StringComparison.Ordinal);
    }

    private const string InstallRev869ATemplate = """
                CREATE FUNCTION __advance_schema__.rev869a_block_history_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Controlled configuration history is append-only'; END $$;
                CREATE TRIGGER trg_rev869a_history_append_only BEFORE UPDATE OR DELETE ON __advance_schema__.controlled_configuration_histories FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869a_block_history_mutation();

                CREATE FUNCTION __advance_schema__.rev869a_guard_controlled_version() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN RAISE EXCEPTION 'Controlled configuration versions cannot be deleted'; END IF;
                    IF (to_jsonb(NEW) - ARRAY['EffectiveTo','IsActive','UpdatedAt','UpdatedBy','Version']) <> (to_jsonb(OLD) - ARRAY['EffectiveTo','IsActive','UpdatedAt','UpdatedBy','Version']) THEN
                        RAISE EXCEPTION 'Historical effective records are immutable; close the old version and insert a corrected version';
                    END IF;
                    IF OLD."EffectiveTo" IS NOT NULL OR NEW."EffectiveTo" IS NULL OR NEW."EffectiveTo" < OLD."EffectiveFrom" THEN RAISE EXCEPTION 'Only a valid first close of an open effective version is allowed'; END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER trg_rev869a_identity_version_guard BEFORE UPDATE OR DELETE ON __advance_schema__.employee_identity_mappings FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869a_guard_controlled_version();
                CREATE TRIGGER trg_rev869a_scope_version_guard BEFORE UPDATE OR DELETE ON __advance_schema__.employee_operational_scopes FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869a_guard_controlled_version();
                CREATE TRIGGER trg_rev869a_policy_version_guard BEFORE UPDATE OR DELETE ON __advance_schema__.organization_policies FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869a_guard_controlled_version();
                CREATE TRIGGER trg_rev869a_qc_version_guard BEFORE UPDATE OR DELETE ON __advance_schema__.qc_inspection_policies FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869a_guard_controlled_version();
                CREATE TRIGGER trg_rev869a_tax_version_guard BEFORE UPDATE OR DELETE ON __advance_schema__.tax_gst_settings FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869a_guard_controlled_version();
                CREATE TRIGGER trg_rev869a_vendor_qualification_version_guard BEFORE UPDATE OR DELETE ON __advance_schema__.vendor_qualifications FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869a_guard_controlled_version();
                CREATE TRIGGER trg_rev869a_warehouse_condition_version_guard BEFORE UPDATE OR DELETE ON __advance_schema__.warehouse_condition_locations FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869a_guard_controlled_version();
                CREATE TRIGGER trg_rev869a_uom_conversion_version_guard BEFORE UPDATE OR DELETE ON __advance_schema__.uom_conversions FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869a_guard_controlled_version();

                CREATE FUNCTION __advance_schema__.rev869a_guard_used_uom_conversion() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN IF OLD."FirstUsedAt" IS NOT NULL THEN RAISE EXCEPTION 'A used UOM conversion is immutable'; END IF; RETURN NEW; END $$;
                CREATE TRIGGER trg_rev869a_used_uom_conversion BEFORE UPDATE OR DELETE ON __advance_schema__.uom_conversions FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869a_guard_used_uom_conversion();

        """;

    private const string InstallRev869BTemplate = """

                CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_reject_immutable_mutation() RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, __advance_schema__ AS $rev869b$
                BEGIN RAISE EXCEPTION USING ERRCODE='P0001',SCHEMA='__advance_schema__',TABLE=TG_TABLE_NAME,
                    CONSTRAINT='rev869b_reject_immutable_mutation',
                    MESSAGE=format('REV869B controlled history/snapshot relation %s is immutable.',TG_TABLE_NAME); END $rev869b$;

                CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_guard_controlled_snapshot() RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, __advance_schema__ AS $rev869b$
                BEGIN
                    IF TG_TABLE_NAME = 'vendor_quotations' AND
                       to_jsonb(NEW) - ARRAY['Status','Version','IsCurrentRevision','UpdatedAt','UpdatedBy'] <> to_jsonb(OLD) - ARRAY['Status','Version','IsCurrentRevision','UpdatedAt','UpdatedBy'] THEN
                        RAISE EXCEPTION 'Submitted quotation provenance and commercial terms are immutable.';
                    ELSIF TG_TABLE_NAME = 'commercial_comparisons' AND OLD."Status" NOT IN ('Draft','RevisionRequested') AND
                       to_jsonb(NEW) - ARRAY['Status','Version','UpdatedAt','UpdatedBy'] <> to_jsonb(OLD) - ARRAY['Status','Version','UpdatedAt','UpdatedBy'] THEN
                        RAISE EXCEPTION 'Submitted comparison snapshot is immutable.';
                    ELSIF TG_TABLE_NAME = 'commercial_comparison_lines' AND
                       EXISTS (SELECT 1 FROM __advance_schema__.commercial_comparisons c WHERE c."Id" = OLD."CommercialComparisonId" AND c."Status" NOT IN ('Draft','RevisionRequested')) THEN
                        RAISE EXCEPTION 'Submitted comparison line snapshot is immutable.';
                    ELSIF TG_TABLE_NAME = 'purchase_orders' AND
                       to_jsonb(NEW) - ARRAY['Status','Version','IsCurrentVersion','IssuedAt','CancelledAt','CancellationReason','UpdatedAt','UpdatedBy'] <> to_jsonb(OLD) - ARRAY['Status','Version','IsCurrentVersion','IssuedAt','CancelledAt','CancellationReason','UpdatedAt','UpdatedBy'] THEN
                        RAISE EXCEPTION 'Purchase order commercial and provenance snapshot is immutable.';
                    END IF;
                    RETURN NEW;
                END $rev869b$;

                CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_enforce_transition() RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, __advance_schema__ AS $rev869b$
                DECLARE allowed boolean := false;
                BEGIN
                    IF TG_OP = 'INSERT' THEN
                        IF TG_TABLE_NAME = 'request_for_quotations' AND NEW."Status" <> 'Draft' THEN
                            RAISE EXCEPTION USING ERRCODE='P0001',SCHEMA='__advance_schema__',TABLE=TG_TABLE_NAME,CONSTRAINT='rev869b_enforce_transition',MESSAGE='RFQ must be inserted in Draft status.';
                        ELSIF TG_TABLE_NAME = 'rfq_vendor_invitations' AND NEW."Status" <> 'Issued' THEN
                            RAISE EXCEPTION USING ERRCODE='P0001',SCHEMA='__advance_schema__',TABLE=TG_TABLE_NAME,CONSTRAINT='rev869b_enforce_transition',MESSAGE='RFQ invitation must be inserted in Issued status.';
                        ELSIF TG_TABLE_NAME = 'vendor_quotations' AND NEW."Status" <> 'Submitted' THEN
                            RAISE EXCEPTION USING ERRCODE='P0001',SCHEMA='__advance_schema__',TABLE=TG_TABLE_NAME,CONSTRAINT='rev869b_enforce_transition',MESSAGE='Quotation must be inserted in Submitted status.';
                        ELSIF TG_TABLE_NAME = 'commercial_comparisons' AND NEW."Status" <> 'Draft' THEN
                            RAISE EXCEPTION USING ERRCODE='P0001',SCHEMA='__advance_schema__',TABLE=TG_TABLE_NAME,CONSTRAINT='rev869b_enforce_transition',MESSAGE='Comparison must be inserted in Draft status.';
                        ELSIF TG_TABLE_NAME = 'purchase_orders' AND NEW."Status" NOT IN ('Draft','RevisionDraft') THEN
                            RAISE EXCEPTION USING ERRCODE='P0001',SCHEMA='__advance_schema__',TABLE=TG_TABLE_NAME,CONSTRAINT='rev869b_enforce_transition',MESSAGE='Purchase order must be inserted in a controlled draft status.';
                        ELSIF TG_TABLE_NAME = 'purchase_orders' AND NEW."Status" = 'RevisionDraft' AND NOT EXISTS (
                            SELECT 1 FROM __advance_schema__.purchase_orders p
                            WHERE p."Id" = NEW."PreviousVersionId" AND p."OrganizationId" = NEW."OrganizationId"
                              AND p."RootPurchaseOrderId" = NEW."RootPurchaseOrderId" AND p."PoNumber" = NEW."PoNumber"
                              AND p."RevisionNumber" + 1 = NEW."RevisionNumber" AND p."Status" = 'Rejected' AND NOT p."IsCurrentVersion") THEN
                            RAISE EXCEPTION 'RevisionDraft requires an immutable rejected predecessor.';
                        END IF;
                        RETURN NEW;
                    END IF;
                    IF NEW."Version" <> OLD."Version" + 1 THEN RAISE EXCEPTION 'REV869B aggregate version must increment by exactly one.'; END IF;
                    IF TG_TABLE_NAME = 'request_for_quotations' AND
                       (NEW."OrganizationId",NEW."PurchaseRequisitionId") IS DISTINCT FROM (OLD."OrganizationId",OLD."PurchaseRequisitionId") THEN
                        RAISE EXCEPTION 'RFQ organization and parent are immutable.';
                    ELSIF TG_TABLE_NAME = 'rfq_vendor_invitations' AND
                       (NEW."RequestForQuotationId",NEW."VendorId") IS DISTINCT FROM (OLD."RequestForQuotationId",OLD."VendorId") THEN
                        RAISE EXCEPTION 'RFQ invitation parents are immutable.';
                    ELSIF TG_TABLE_NAME = 'vendor_quotations' AND
                       (NEW."OrganizationId",NEW."RfqVendorInvitationId",NEW."VendorId") IS DISTINCT FROM (OLD."OrganizationId",OLD."RfqVendorInvitationId",OLD."VendorId") THEN
                        RAISE EXCEPTION 'Quotation organization and parents are immutable.';
                    ELSIF TG_TABLE_NAME = 'commercial_comparisons' AND
                       (NEW."OrganizationId",NEW."RequestForQuotationId") IS DISTINCT FROM (OLD."OrganizationId",OLD."RequestForQuotationId") THEN
                        RAISE EXCEPTION 'Comparison organization and parent are immutable.';
                    ELSIF TG_TABLE_NAME = 'purchase_orders' AND
                       (NEW."OrganizationId",NEW."CommercialComparisonId",NEW."VendorId",NEW."RootPurchaseOrderId",NEW."PreviousVersionId") IS DISTINCT FROM
                       (OLD."OrganizationId",OLD."CommercialComparisonId",OLD."VendorId",OLD."RootPurchaseOrderId",OLD."PreviousVersionId") THEN
                        RAISE EXCEPTION 'Purchase order organization, provenance and version ancestry are immutable.';
                    END IF;
                    IF TG_TABLE_NAME = 'commercial_comparisons' AND NEW."Status" IN ('PendingApproval','Approved') AND (
                        NEW."RecommendedVendorQuotationId" IS NULL OR NEW."SelectedVendorId" IS NULL OR
                        length(trim(coalesce(NEW."ApprovalRoute", ''))) = 0 OR
                        NOT EXISTS (SELECT 1 FROM __advance_schema__.commercial_comparison_lines cl WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended") OR
                        EXISTS (
                            SELECT 1 FROM __advance_schema__.commercial_comparison_lines cl
                            JOIN __advance_schema__.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId"
                            JOIN __advance_schema__.vendor_quotations q ON q."Id"=ql."VendorQuotationId"
                            JOIN __advance_schema__.request_for_quotation_lines rl ON rl."Id"=ql."RequestForQuotationLineId"
                            WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended" AND (
                                cl."VendorId" <> NEW."SelectedVendorId" OR q."Id" <> NEW."RecommendedVendorQuotationId" OR
                                q."OrganizationId" <> NEW."OrganizationId" OR q."VendorId" <> NEW."SelectedVendorId" OR
                                q."RevisionNumber" <= 0 OR NOT q."IsCurrentRevision" OR q."Status" <> 'TechnicallyCompliant' OR
                                coalesce(cl."CommercialSnapshotJson"->>'organizationId','') <> NEW."OrganizationId" OR
                                coalesce((cl."CommercialSnapshotJson"->>'commercialComparisonId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> NEW."Id" OR
                                coalesce((cl."CommercialSnapshotJson"->>'requestForQuotationId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> NEW."RequestForQuotationId" OR
                                coalesce((cl."CommercialSnapshotJson"->>'vendorId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> NEW."SelectedVendorId" OR
                                coalesce((cl."CommercialSnapshotJson"->>'vendorQuotationId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> q."Id" OR
                                coalesce((cl."CommercialSnapshotJson"->>'quotationRevision')::integer,0) <> q."RevisionNumber" OR
                                coalesce((cl."CommercialSnapshotJson"->>'vendorQuotationLineId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> ql."Id" OR
                                coalesce((cl."CommercialSnapshotJson"->>'itemId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> rl."ItemId" OR
                                coalesce((cl."CommercialSnapshotJson"->>'quantity')::numeric,-1) <> ql."Quantity" OR
                                coalesce(cl."CommercialSnapshotJson"->>'uom','') <> rl."UomSnapshot" OR
                                coalesce(cl."CommercialSnapshotJson"->>'currencyCode','') <> q."CurrencyCode" OR
                                coalesce((cl."CommercialSnapshotJson"->>'exchangeRate')::numeric,0) <> 1 OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'grossAmount')::numeric,-1) <> round(ql."Quantity"*ql."UnitRate",(ql."TaxRuleSnapshotJson"->>'roundingScale')::integer) OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'discountValue')::numeric,-1) <> ql."DiscountValue" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'headerDiscountValue')::numeric,-1) <> ql."HeaderDiscountValue" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'packingForwarding')::numeric,-1) <> ql."PackingForwarding" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'freight')::numeric,-1) <> ql."Freight" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'insurance')::numeric,-1) <> ql."Insurance" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'otherCharges')::numeric,-1) <> ql."OtherCharges" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'taxableValue')::numeric,-1) <> ql."TaxableValue" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'cgstValue')::numeric,-1) <> ql."CgstValue" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'sgstValue')::numeric,-1) <> ql."SgstValue" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'igstValue')::numeric,-1) <> ql."IgstValue" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'cessValue')::numeric,-1) <> ql."CessValue" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'roundOff')::numeric,-999999) <> ql."RoundOff" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'totalPayableValue')::numeric,-1) <> ql."TotalPayableValue" OR
                                cl."TotalPayableValue" <> ql."TotalPayableValue" OR
                                cl."CommercialSnapshotJson"->'taxRule' IS NULL OR
                                cl."CommercialSnapshotJson"->'taxRule' IS DISTINCT FROM ql."TaxRuleSnapshotJson"
                            )) OR
                        (SELECT count(*) FROM __advance_schema__.commercial_comparison_lines cl WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended") <>
                        (SELECT count(*) FROM __advance_schema__.vendor_quotation_lines ql WHERE ql."VendorQuotationId"=NEW."RecommendedVendorQuotationId") OR
                        NEW."TotalPayableValue" <> (SELECT coalesce(sum(cl."TotalPayableValue"),0) FROM __advance_schema__.commercial_comparison_lines cl WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended")
                    ) THEN RAISE EXCEPTION 'Comparison snapshot is incomplete or does not exactly reconcile.'; END IF;
                    IF NEW."Status" = OLD."Status" THEN RETURN NEW; END IF;
                    IF TG_TABLE_NAME = 'request_for_quotations' THEN
                        allowed := (OLD."Status", NEW."Status") IN (('Draft','Issued'),('Draft','Cancelled'),('Issued','Closed'),('Issued','Cancelled'));
                    ELSIF TG_TABLE_NAME = 'rfq_vendor_invitations' THEN
                        allowed := (OLD."Status", NEW."Status") IN (('Issued','Submitted'),('Issued','Withdrawn'),('Issued','Cancelled'));
                    ELSIF TG_TABLE_NAME = 'vendor_quotations' THEN
                        allowed := (OLD."Status", NEW."Status") IN (
                            ('Submitted','TechnicallyCompliant'),('Submitted','TechnicallyRejected'),('Submitted','Superseded'),('Submitted','Withdrawn'),
                            ('TechnicallyCompliant','Superseded'),('TechnicallyCompliant','Withdrawn'),
                            ('TechnicallyRejected','Superseded'),('TechnicallyRejected','Withdrawn'),('TechnicallyRejected','Rejected'));
                    ELSIF TG_TABLE_NAME = 'commercial_comparisons' THEN
                        allowed := (OLD."Status", NEW."Status") IN (
                            ('Draft','PendingApproval'),('Draft','Cancelled'),('PendingApproval','Approved'),('PendingApproval','Rejected'),
                            ('PendingApproval','RevisionRequested'),('RevisionRequested','PendingApproval'),('RevisionRequested','Cancelled'));
                    ELSIF TG_TABLE_NAME = 'purchase_orders' THEN
                        allowed := (OLD."Status", NEW."Status") IN (
                            ('Draft','PendingApproval'),('Draft','Cancelled'),('RevisionDraft','Resubmitted'),('RevisionDraft','Cancelled'),
                            ('PendingApproval','Approved'),('PendingApproval','Rejected'),('PendingApproval','Cancelled'),
                            ('Resubmitted','Approved'),('Resubmitted','Rejected'),('Resubmitted','Cancelled'),
                            ('Approved','Issued'),('Approved','Cancelled'),('Issued','Superseded'),('Issued','Cancelled'));
                        IF OLD."Status" = 'Approved' AND NEW."Status" = 'Issued' AND (
                            length(trim(coalesce(NEW."PaymentTermsSnapshot", ''))) = 0 OR
                            length(trim(coalesce(NEW."DeliveryTermsSnapshot", ''))) = 0 OR
                            length(trim(coalesce(NEW."WarrantyTermsSnapshot", ''))) = 0 OR
                            length(trim(coalesce(NEW."ApprovalRoute", ''))) = 0 OR
                            NOT EXISTS (SELECT 1 FROM __advance_schema__.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            EXISTS (SELECT 1 FROM __advance_schema__.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id" AND
                                (l."OrderedQuantity" <= 0 OR l."ApprovedOutstandingQuantitySnapshot" <= 0 OR
                                 l."CommercialSnapshotJson" IS NULL OR l."CommercialSnapshotJson" = '{}'::jsonb OR
                                 l."TaxRuleSnapshotJson" IS NULL OR l."TaxRuleSnapshotJson" = '{}'::jsonb OR
                                 coalesce(l."CommercialSnapshotJson"->>'organizationId','') <> NEW."OrganizationId" OR
                                 coalesce((l."CommercialSnapshotJson"->>'vendorQuotationId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) = '00000000-0000-0000-0000-000000000000'::uuid OR
                                 coalesce((l."CommercialSnapshotJson"->>'vendorQuotationLineId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) = '00000000-0000-0000-0000-000000000000'::uuid OR
                                 coalesce((l."CommercialSnapshotJson"->>'requestForQuotationId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) = '00000000-0000-0000-0000-000000000000'::uuid OR
                                 coalesce((l."CommercialSnapshotJson"->>'quotationRevision')::integer,0) <= 0 OR
                                 coalesce((l."CommercialSnapshotJson"->>'commercialComparisonId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> NEW."CommercialComparisonId" OR
                                 coalesce((l."CommercialSnapshotJson"->>'vendorId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> NEW."VendorId" OR
                                 coalesce((l."CommercialSnapshotJson"->>'itemId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> l."ItemId" OR
                                 coalesce((l."CommercialSnapshotJson"->>'quantity')::numeric,-1) <> l."OrderedQuantity" OR
                                 coalesce(l."CommercialSnapshotJson"->>'uom','') <> l."UomSnapshot" OR
                                 coalesce(l."CommercialSnapshotJson"->>'currencyCode','') <> NEW."CurrencyCode" OR
                                 coalesce((l."CommercialSnapshotJson"->>'exchangeRate')::numeric,0) <> 1 OR
                                 coalesce(l."CommercialSnapshotJson"->>'vendorQualificationSnapshotJson','{}') = '{}' OR
                                 length(trim(coalesce(l."CommercialSnapshotJson"->>'attachmentObjectKey',''))) = 0 OR
                                 length(coalesce(l."CommercialSnapshotJson"->>'attachmentSha256','')) <> 64 OR
                                 coalesce(l."TaxRuleSnapshotJson"->>'organizationId','') <> NEW."OrganizationId" OR
                                 coalesce(l."TaxRuleSnapshotJson"->>'approvalStatus','') <> 'Approved' OR
                                 coalesce((l."TaxRuleSnapshotJson"->>'isActive')::boolean,FALSE) IS NOT TRUE OR
                                 length(trim(coalesce(l."TaxRuleSnapshotJson"->>'hsnSacCode',''))) = 0 OR
                                 coalesce(l."CommercialSnapshotJson"->>'comparisonApprovalRoute','') <> NEW."ApprovalRoute" OR
                                 coalesce((l."CommercialSnapshotJson"->'result'->>'totalPayableValue')::numeric,-1) <> l."TotalPayableValue")) OR
                            NEW."TaxableValue" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'taxableValue')::numeric), 0) FROM __advance_schema__.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."DiscountValue" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'discountValue')::numeric), 0) FROM __advance_schema__.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."HeaderDiscountValue" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'headerDiscountValue')::numeric), 0) FROM __advance_schema__.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."TaxValue" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'cgstValue')::numeric + (l."CommercialSnapshotJson"->'result'->>'sgstValue')::numeric + (l."CommercialSnapshotJson"->'result'->>'igstValue')::numeric + (l."CommercialSnapshotJson"->'result'->>'cessValue')::numeric), 0) FROM __advance_schema__.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."PackingForwarding" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'packingForwarding')::numeric), 0) FROM __advance_schema__.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."Freight" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'freight')::numeric), 0) FROM __advance_schema__.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."Insurance" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'insurance')::numeric), 0) FROM __advance_schema__.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."OtherCharges" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'otherCharges')::numeric), 0) FROM __advance_schema__.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."RoundOff" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'roundOff')::numeric), 0) FROM __advance_schema__.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."TotalPayableValue" <> (SELECT coalesce(sum(l."TotalPayableValue"), 0) FROM __advance_schema__.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."ApprovalPolicySnapshotJson" IS NULL OR NEW."ApprovalPolicySnapshotJson" = '{}'::jsonb OR
                            coalesce(NEW."ApprovalPolicySnapshotJson"->>'organizationId','') <> NEW."OrganizationId" OR
                            coalesce(NEW."ApprovalPolicySnapshotJson"->>'routeCode','') <> NEW."ApprovalRoute" OR
                            coalesce((NEW."ApprovalPolicySnapshotJson"->>'approvalValue')::numeric,-1) <> NEW."TotalPayableValue" OR
                            length(trim(coalesce(NEW."ApprovalPolicySnapshotJson"->>'effectiveOn',''))) = 0 OR
                            NOT EXISTS (SELECT 1 FROM __advance_schema__.purchase_order_history h WHERE h."PurchaseOrderId" = NEW."Id" AND h."ToStatus" = 'Approved' AND length(trim(h."Reason")) > 0) OR
                            NOT EXISTS (SELECT 1 FROM __advance_schema__.purchase_transaction_approval_policies p WHERE p."OrganizationId" = NEW."OrganizationId" AND p."RouteCode" = NEW."ApprovalRoute" AND p."IsActive" AND NEW."TotalPayableValue" >= p."MinimumAmount" AND (p."MaximumAmount" IS NULL OR NEW."TotalPayableValue" <= p."MaximumAmount"))
                        ) THEN RAISE EXCEPTION 'Purchase order pre-issue snapshot is incomplete or does not reconcile.'; END IF;
                    END IF;
                    IF NOT allowed THEN RAISE EXCEPTION 'Illegal REV869B % status transition: % to %.', TG_TABLE_NAME, OLD."Status", NEW."Status"; END IF;
                    RETURN NEW;
                END $rev869b$;

                CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_reject_overlapping_approval_policy() RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, __advance_schema__ AS $rev869b$
                BEGIN
                    IF NEW."IsActive" AND EXISTS (
                        SELECT 1 FROM __advance_schema__.purchase_transaction_approval_policies p
                        WHERE p."Id" <> NEW."Id" AND p."OrganizationId" = NEW."OrganizationId" AND p."IsActive"
                          AND daterange(p."EffectiveFrom", coalesce(p."EffectiveTo", 'infinity'::date), '[]') &&
                              daterange(NEW."EffectiveFrom", coalesce(NEW."EffectiveTo", 'infinity'::date), '[]')
                          AND numrange(p."MinimumAmount", p."MaximumAmount", '[]') &&
                              numrange(NEW."MinimumAmount", NEW."MaximumAmount", '[]')) THEN
                        RAISE EXCEPTION 'Overlapping active purchase approval policies are prohibited.';
                    END IF;
                    RETURN NEW;
                END $rev869b$;

                CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_validate_parent_contract() RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, __advance_schema__ AS $rev869b$
                BEGIN
                    IF TG_TABLE_NAME = 'vendor_quotation_lines' AND NOT EXISTS (
                        SELECT 1 FROM __advance_schema__.vendor_quotations q
                        JOIN __advance_schema__.rfq_vendor_invitations i ON i."Id" = q."RfqVendorInvitationId" AND i."VendorId" = q."VendorId"
                        JOIN __advance_schema__.request_for_quotations r ON r."Id" = i."RequestForQuotationId" AND r."OrganizationId" = q."OrganizationId"
                        JOIN __advance_schema__.request_for_quotation_lines rl ON rl."Id" = NEW."RequestForQuotationLineId" AND rl."RequestForQuotationId" = r."Id"
                        WHERE q."Id" = NEW."VendorQuotationId") THEN
                        RAISE EXCEPTION 'Quotation line parent contract mismatch.';
                    ELSIF TG_TABLE_NAME = 'quotation_technical_verifications' AND NOT EXISTS (
                        SELECT 1 FROM __advance_schema__.vendor_quotation_lines ql JOIN __advance_schema__.vendor_quotations q ON q."Id" = ql."VendorQuotationId" WHERE ql."Id" = NEW."VendorQuotationLineId") THEN
                        RAISE EXCEPTION 'Technical verification parent contract mismatch.';
                    ELSIF TG_TABLE_NAME = 'commercial_comparison_lines' AND NOT EXISTS (
                        SELECT 1 FROM __advance_schema__.commercial_comparisons c
                        JOIN __advance_schema__.vendor_quotation_lines ql ON ql."Id" = NEW."VendorQuotationLineId"
                        JOIN __advance_schema__.vendor_quotations q ON q."Id" = ql."VendorQuotationId" AND q."VendorId" = NEW."VendorId"
                        JOIN __advance_schema__.rfq_vendor_invitations i ON i."Id" = q."RfqVendorInvitationId" AND i."RequestForQuotationId" = c."RequestForQuotationId"
                        WHERE c."Id" = NEW."CommercialComparisonId" AND c."OrganizationId" = q."OrganizationId") THEN
                        RAISE EXCEPTION 'Comparison line parent contract mismatch.';
                    ELSIF TG_TABLE_NAME = 'purchase_orders' AND NOT EXISTS (
                        SELECT 1 FROM __advance_schema__.commercial_comparisons c
                        JOIN __advance_schema__.vendor_quotations q ON q."Id" = c."RecommendedVendorQuotationId" AND q."VendorId" = c."SelectedVendorId"
                        JOIN __advance_schema__.rfq_vendor_invitations i ON i."Id" = q."RfqVendorInvitationId" AND i."RequestForQuotationId" = c."RequestForQuotationId"
                        WHERE c."Id" = NEW."CommercialComparisonId" AND c."OrganizationId" = NEW."OrganizationId" AND c."SelectedVendorId" = NEW."VendorId") THEN
                        RAISE EXCEPTION 'Purchase order parent contract mismatch.';
                    ELSIF TG_TABLE_NAME = 'purchase_order_lines' AND NOT EXISTS (
                        SELECT 1 FROM __advance_schema__.purchase_orders p
                        JOIN __advance_schema__.commercial_comparison_lines cl ON cl."Id" = NEW."CommercialComparisonLineId" AND cl."CommercialComparisonId" = p."CommercialComparisonId"
                        JOIN __advance_schema__.vendor_quotation_lines ql ON ql."Id" = cl."VendorQuotationLineId"
                        JOIN __advance_schema__.request_for_quotation_lines rl ON rl."Id" = ql."RequestForQuotationLineId" AND rl."ItemId" = NEW."ItemId"
                        JOIN __advance_schema__.purchase_requirement_handoffs h ON h."Id" = NEW."PurchaseRequirementHandoffId" AND h."PurchaseRequisitionLineId" = NEW."PurchaseRequisitionLineId"
                        WHERE p."Id" = NEW."PurchaseOrderId" AND rl."PurchaseRequirementHandoffId" = h."Id") THEN
                        RAISE EXCEPTION 'Purchase order line parent contract mismatch.';
                    ELSIF TG_TABLE_NAME = 'material_followup_handoffs' AND NOT EXISTS (
                        SELECT 1 FROM __advance_schema__.purchase_order_lines pl WHERE pl."Id" = NEW."PurchaseOrderLineId" AND pl."PurchaseOrderId" = NEW."PurchaseOrderId") THEN
                        RAISE EXCEPTION 'Material follow-up parent contract mismatch.';
                    END IF;
                    RETURN NEW;
                END $rev869b$;

                CREATE TRIGGER trg_rev869b_vendor_quotation_snapshot_guard BEFORE UPDATE ON __advance_schema__.vendor_quotations FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_controlled_snapshot();
                CREATE TRIGGER trg_rev869b_comparison_snapshot_guard BEFORE UPDATE ON __advance_schema__.commercial_comparisons FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_controlled_snapshot();
                CREATE TRIGGER trg_rev869b_comparison_line_snapshot_guard BEFORE UPDATE ON __advance_schema__.commercial_comparison_lines FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_controlled_snapshot();
                CREATE TRIGGER trg_rev869b_purchase_order_snapshot_guard BEFORE UPDATE ON __advance_schema__.purchase_orders FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_controlled_snapshot();
                CREATE TRIGGER trg_rev869b_rfq_transition_guard BEFORE INSERT OR UPDATE ON __advance_schema__.request_for_quotations FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_enforce_transition();
                CREATE TRIGGER trg_rev869b_invitation_transition_guard BEFORE INSERT OR UPDATE ON __advance_schema__.rfq_vendor_invitations FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_enforce_transition();
                CREATE TRIGGER trg_rev869b_comparison_transition_guard BEFORE INSERT OR UPDATE ON __advance_schema__.commercial_comparisons FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_enforce_transition();
                CREATE TRIGGER trg_rev869b_purchase_order_transition_guard BEFORE INSERT OR UPDATE ON __advance_schema__.purchase_orders FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_enforce_transition();
                CREATE TRIGGER trg_rev869b_quotation_line_parent_guard BEFORE INSERT OR UPDATE ON __advance_schema__.vendor_quotation_lines FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_validate_parent_contract();
                CREATE TRIGGER trg_rev869b_technical_parent_guard BEFORE INSERT OR UPDATE ON __advance_schema__.quotation_technical_verifications FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_validate_parent_contract();
                CREATE TRIGGER trg_rev869b_comparison_line_parent_guard BEFORE INSERT OR UPDATE ON __advance_schema__.commercial_comparison_lines FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_validate_parent_contract();
                CREATE TRIGGER trg_rev869b_purchase_order_parent_guard BEFORE INSERT OR UPDATE ON __advance_schema__.purchase_orders FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_validate_parent_contract();
                CREATE TRIGGER trg_rev869b_purchase_order_line_parent_guard BEFORE INSERT OR UPDATE ON __advance_schema__.purchase_order_lines FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_validate_parent_contract();
                CREATE TRIGGER trg_rev869b_followup_parent_guard BEFORE INSERT OR UPDATE ON __advance_schema__.material_followup_handoffs FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_validate_parent_contract();
                CREATE TRIGGER trg_rev869b_vendor_quotation_lines_immutable BEFORE UPDATE OR DELETE ON __advance_schema__.vendor_quotation_lines FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_immutable_mutation();
                CREATE TRIGGER trg_rev869b_technical_verifications_immutable BEFORE UPDATE OR DELETE ON __advance_schema__.quotation_technical_verifications FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_immutable_mutation();
                CREATE TRIGGER trg_rev869b_purchase_approval_history_immutable BEFORE UPDATE OR DELETE ON __advance_schema__.purchase_transaction_approval_history FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_immutable_mutation();
                CREATE TRIGGER trg_rev869b_purchase_order_lines_immutable BEFORE UPDATE OR DELETE ON __advance_schema__.purchase_order_lines FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_immutable_mutation();
                CREATE TRIGGER trg_rev869b_purchase_order_history_immutable BEFORE UPDATE OR DELETE ON __advance_schema__.purchase_order_history FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_immutable_mutation();
                CREATE TRIGGER trg_rev869b_purchase_status_history_immutable BEFORE UPDATE OR DELETE ON __advance_schema__.purchase_transaction_status_history FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_immutable_mutation();
                CREATE TRIGGER trg_rev869b_approval_policy_overlap_guard BEFORE INSERT OR UPDATE ON __advance_schema__.purchase_transaction_approval_policies FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_overlapping_approval_policy();


        """;

    private const string RemoveRev869BTemplate = """
        DROP FUNCTION IF EXISTS __advance_schema__.rev869b_validate_parent_contract() CASCADE;
        DROP FUNCTION IF EXISTS __advance_schema__.rev869b_enforce_transition() CASCADE;
        DROP FUNCTION IF EXISTS __advance_schema__.rev869b_guard_controlled_snapshot() CASCADE;
        DROP FUNCTION IF EXISTS __advance_schema__.rev869b_reject_immutable_mutation() CASCADE;
        DROP FUNCTION IF EXISTS __advance_schema__.rev869b_reject_overlapping_approval_policy() CASCADE;
        """;

    private const string RemoveRev869ATemplate = """
        DROP FUNCTION IF EXISTS __advance_schema__.rev869a_guard_used_uom_conversion() CASCADE;
        DROP FUNCTION IF EXISTS __advance_schema__.rev869a_guard_controlled_version() CASCADE;
        DROP FUNCTION IF EXISTS __advance_schema__.rev869a_block_history_mutation() CASCADE;
        """;
}
