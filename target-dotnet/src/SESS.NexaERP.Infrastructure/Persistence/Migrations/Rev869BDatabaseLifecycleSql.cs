namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class Rev869BDatabaseLifecycleSql
{
    public const string Install = """
        ALTER TABLE nexa.vendor_quotations DROP CONSTRAINT "CK_vendor_quotation_status";
        ALTER TABLE nexa.vendor_quotations ADD CONSTRAINT "CK_vendor_quotation_status"
            CHECK ("Status" IN ('Draft','Submitted','TechnicallyCompliant','TechnicallyRejected','Superseded','Withdrawn','Rejected'));

        DROP TRIGGER IF EXISTS trg_rev869b_quotation_transition_guard ON nexa.vendor_quotations;
        CREATE OR REPLACE FUNCTION nexa.rev869b_enforce_quotation_transition()
        RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, nexa AS $rev869b$
        DECLARE allowed boolean := false; expected_count bigint; matched_count bigint;
        BEGIN
            IF TG_OP='INSERT' THEN
                IF NEW."Status" IS DISTINCT FROM 'Draft' OR NEW."Version"<>0 OR NOT NEW."IsCurrentRevision" THEN
                    RAISE EXCEPTION 'Quotation must be inserted as current Draft version zero.';
                END IF;
                RETURN NEW;
            END IF;
            IF NEW."Version"<>OLD."Version"+1 THEN RAISE EXCEPTION 'Quotation version must increment by exactly one.'; END IF;
            IF (NEW."OrganizationId",NEW."RfqVendorInvitationId",NEW."VendorId",NEW."RootQuotationId",NEW."PreviousRevisionId",NEW."RevisionNumber")
               IS DISTINCT FROM
               (OLD."OrganizationId",OLD."RfqVendorInvitationId",OLD."VendorId",OLD."RootQuotationId",OLD."PreviousRevisionId",OLD."RevisionNumber")
            THEN RAISE EXCEPTION 'Quotation organization, parent and revision identity are immutable.'; END IF;
            IF NEW."Status"=OLD."Status" THEN RETURN NEW; END IF;
            allowed := (OLD."Status",NEW."Status") IN (
                ('Draft','Submitted'),('Submitted','TechnicallyCompliant'),('Submitted','TechnicallyRejected'),
                ('Submitted','Superseded'),('Submitted','Withdrawn'),('TechnicallyCompliant','Superseded'),
                ('TechnicallyCompliant','Withdrawn'),('TechnicallyRejected','Superseded'),
                ('TechnicallyRejected','Withdrawn'),('TechnicallyRejected','Rejected'));
            IF NOT allowed THEN RAISE EXCEPTION 'Illegal REV869B quotation transition: % to %.',OLD."Status",NEW."Status"; END IF;
            IF OLD."Status"='Draft' AND NEW."Status"='Submitted' THEN
                SELECT count(*) INTO expected_count
                  FROM nexa.request_for_quotation_lines rl
                  JOIN nexa.rfq_vendor_invitations i ON i."RequestForQuotationId"=rl."RequestForQuotationId"
                 WHERE i."Id"=NEW."RfqVendorInvitationId";
                SELECT count(*) INTO matched_count
                  FROM nexa.vendor_quotation_lines ql
                  JOIN nexa.request_for_quotation_lines rl ON rl."Id"=ql."RequestForQuotationLineId"
                  JOIN nexa.rfq_vendor_invitations i ON i."Id"=NEW."RfqVendorInvitationId" AND i."RequestForQuotationId"=rl."RequestForQuotationId"
                 WHERE ql."VendorQuotationId"=NEW."Id" AND ql."Quantity"<=rl."RfqQuantity" AND
                       nexa.rev869b_commercial_snapshot_reconciles(
                           ql."Id",
                           jsonb_build_object(
                               'input',jsonb_build_object('quantity',ql."Quantity",'unitRate',ql."UnitRate",
                                   'discountValue',ql."DiscountValue",'packingForwarding',ql."PackingForwarding",
                                   'freight',ql."Freight",'insurance',ql."Insurance",'otherCharges',ql."OtherCharges",
                                   'cgstRate',ql."TaxRuleSnapshotJson"->'cgstRate','sgstRate',ql."TaxRuleSnapshotJson"->'sgstRate',
                                   'igstRate',ql."TaxRuleSnapshotJson"->'igstRate','cessRate',ql."TaxRuleSnapshotJson"->'cessRate',
                                   'roundOff',ql."RoundOff",'roundingScale',ql."TaxRuleSnapshotJson"->'roundingScale',
                                   'headerDiscountValue',ql."HeaderDiscountValue",'currencyCode',NEW."CurrencyCode",'exchangeRate',1),
                               'result',jsonb_build_object('taxableValue',ql."TaxableValue",'discountValue',ql."DiscountValue",
                                   'cgstValue',ql."CgstValue",'sgstValue',ql."SgstValue",'igstValue',ql."IgstValue",
                                   'cessValue',ql."CessValue",'packingForwarding',ql."PackingForwarding",'freight',ql."Freight",
                                   'insurance',ql."Insurance",'otherCharges',ql."OtherCharges",'roundOff',ql."RoundOff",
                                   'totalPayableValue',ql."TotalPayableValue",
                                   'grossAmount',round(ql."Quantity"*ql."UnitRate",(ql."TaxRuleSnapshotJson"->>'roundingScale')::integer),
                                   'headerDiscountValue',ql."HeaderDiscountValue",
                                   'assessableValue',round(ql."Quantity"*ql."UnitRate",(ql."TaxRuleSnapshotJson"->>'roundingScale')::integer)+ql."PackingForwarding"+ql."Freight"+ql."Insurance"+ql."OtherCharges",
                                   'currencyCode',NEW."CurrencyCode",'exchangeRate',1)),
                           ql."TaxRuleSnapshotJson") IS TRUE;
                IF expected_count=0 OR matched_count<>expected_count OR
                   NEW."TotalPayableValue" IS DISTINCT FROM (SELECT sum(ql."TotalPayableValue") FROM nexa.vendor_quotation_lines ql WHERE ql."VendorQuotationId"=NEW."Id") OR
                   NEW."HeaderDiscountValue" IS DISTINCT FROM (SELECT sum(ql."HeaderDiscountValue") FROM nexa.vendor_quotation_lines ql WHERE ql."VendorQuotationId"=NEW."Id")
                THEN RAISE EXCEPTION 'Quotation exact set and authoritative commercial values do not reconcile.'; END IF;
            END IF;
            RETURN NEW;
        EXCEPTION WHEN invalid_text_representation OR numeric_value_out_of_range THEN
            RAISE EXCEPTION 'Quotation contains malformed typed snapshot evidence.';
        END $rev869b$;
        CREATE TRIGGER trg_rev869b_quotation_transition_guard
            BEFORE INSERT OR UPDATE ON nexa.vendor_quotations
            FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_enforce_quotation_transition();
        """;

    public const string Remove = """
        DROP FUNCTION IF EXISTS nexa.rev869b_enforce_quotation_transition() CASCADE;
        """;
}
