namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class Rev869BDatabaseSafetySql
{
    public const string Install = """
        CREATE OR REPLACE FUNCTION nexa.rev869b_commercial_snapshot_reconciles(
            p_quotation_line_id uuid, p_commercial jsonb, p_tax jsonb)
        RETURNS boolean LANGUAGE plpgsql SET search_path = pg_catalog, nexa AS $rev869b$
        DECLARE
            v record; gross numeric; assessable numeric; taxable numeric;
            cgst numeric; sgst numeric; igst numeric; cess numeric; payable numeric;
            expected_input jsonb; expected_result jsonb; expected_tax jsonb;
            maximum numeric := 999999999999999999.999999;
        BEGIN
            IF p_commercial IS NULL OR jsonb_typeof(p_commercial) <> 'object' OR
               p_tax IS NULL OR jsonb_typeof(p_tax) <> 'object' OR
               jsonb_typeof(p_commercial->'input') <> 'object' OR
               jsonb_typeof(p_commercial->'result') <> 'object' THEN RETURN FALSE; END IF;

            SELECT ql."Quantity" quantity, ql."UnitRate" unit_rate,
                   ql."DiscountValue" discount_value, ql."HeaderDiscountValue" header_discount,
                   ql."PackingForwarding" packing, ql."Freight" freight,
                   ql."Insurance" insurance, ql."OtherCharges" other_charges,
                   ql."RoundOff" round_off, ql."TaxableValue" stored_taxable,
                   ql."CgstValue" stored_cgst, ql."SgstValue" stored_sgst,
                   ql."IgstValue" stored_igst, ql."CessValue" stored_cess,
                   ql."TotalPayableValue" stored_payable, ql."TaxGstSettingId" tax_id,
                   ql."HsnSacCode" hsn, ql."SupplierStateCode" supplier_state,
                   ql."PlaceOfSupplyStateCode" supply_state,
                   ql."VendorRegistrationType" registration_type,
                   q."OrganizationId" organization_id, q."CurrencyCode" currency_code,
                   q."ReceivedAt"::date effective_on,
                   t."JurisdictionCode" jurisdiction, t."SupplyType" supply_type,
                   t."GstRate" gst_rate, t."CgstRate" cgst_rate,
                   t."SgstRate" sgst_rate, t."IgstRate" igst_rate,
                   t."CessRate" cess_rate, t."IsExempt" is_exempt,
                   t."IsReverseCharge" is_reverse_charge,
                   t."CurrencyCode" tax_currency, t."RoundingScale" rounding_scale,
                   t."EffectiveFrom" effective_from, t."EffectiveTo" effective_to,
                   t."ApprovalStatus" approval_status, t."IsActive" tax_active
              INTO STRICT v
              FROM nexa.vendor_quotation_lines ql
              JOIN nexa.vendor_quotations q ON q."Id"=ql."VendorQuotationId"
              JOIN nexa.tax_gst_settings t ON t."Id"=ql."TaxGstSettingId"
             WHERE ql."Id"=p_quotation_line_id;

            IF v.quantity <= 0 OR v.unit_rate < 0 OR v.discount_value < 0 OR
               v.header_discount < 0 OR v.packing < 0 OR v.freight < 0 OR
               v.insurance < 0 OR v.other_charges < 0 OR
               v.cgst_rate NOT BETWEEN 0 AND 100 OR v.sgst_rate NOT BETWEEN 0 AND 100 OR
               v.igst_rate NOT BETWEEN 0 AND 100 OR v.cess_rate NOT BETWEEN 0 AND 100 OR
               v.rounding_scale NOT BETWEEN 0 AND 6 OR v.currency_code IS DISTINCT FROM v.tax_currency OR
               v.approval_status IS DISTINCT FROM 'Approved' OR v.tax_active IS NOT TRUE OR
               v.effective_from > v.effective_on OR
               (v.effective_to IS NOT NULL AND v.effective_to < v.effective_on) OR
               v.hsn IS DISTINCT FROM (p_tax->>'hsnSacCode') OR
               v.organization_id IS DISTINCT FROM (p_tax->>'organizationId') OR
               v.supplier_state IS DISTINCT FROM (p_tax->>'supplierStateCode') OR
               v.supply_state IS DISTINCT FROM (p_tax->>'placeOfSupplyStateCode') OR
               v.registration_type IS DISTINCT FROM (p_tax->>'vendorRegistrationType') OR
               v.supply_type IS DISTINCT FROM CASE WHEN upper(v.supplier_state)=upper(v.supply_state)
                                                   THEN 'INTRASTATE' ELSE 'INTERSTATE' END OR
               (v.supply_type='INTRASTATE' AND (v.igst_rate<>0 OR v.cgst_rate+v.sgst_rate<>v.gst_rate)) OR
               (v.supply_type='INTERSTATE' AND (v.cgst_rate<>0 OR v.sgst_rate<>0 OR v.igst_rate<>v.gst_rate))
            THEN RETURN FALSE; END IF;

            gross := round(v.quantity*v.unit_rate, v.rounding_scale);
            assessable := gross+v.packing+v.freight+v.insurance+v.other_charges;
            IF v.discount_value+v.header_discount > assessable THEN RETURN FALSE; END IF;
            taxable := round(assessable-v.discount_value-v.header_discount, v.rounding_scale);
            cgst := round(taxable*v.cgst_rate/100, v.rounding_scale);
            sgst := round(taxable*v.sgst_rate/100, v.rounding_scale);
            igst := round(taxable*v.igst_rate/100, v.rounding_scale);
            cess := round(taxable*v.cess_rate/100, v.rounding_scale);
            payable := round(taxable+cgst+sgst+igst+cess+v.round_off, v.rounding_scale);

            IF payable < 0 OR EXISTS (SELECT 1 FROM unnest(ARRAY[gross,assessable,taxable,cgst,sgst,igst,cess,payable,
                    v.quantity,v.unit_rate,v.discount_value,v.header_discount,v.packing,v.freight,v.insurance,
                    v.other_charges,v.round_off]) n WHERE abs(n)>maximum) THEN RETURN FALSE; END IF;
            IF (v.stored_taxable,v.stored_cgst,v.stored_sgst,v.stored_igst,v.stored_cess,v.stored_payable)
               IS DISTINCT FROM (taxable,cgst,sgst,igst,cess,payable) THEN RETURN FALSE; END IF;

            expected_input := jsonb_build_object('quantity',v.quantity,'unitRate',v.unit_rate,
                'discountValue',v.discount_value,'packingForwarding',v.packing,'freight',v.freight,
                'insurance',v.insurance,'otherCharges',v.other_charges,'cgstRate',v.cgst_rate,
                'sgstRate',v.sgst_rate,'igstRate',v.igst_rate,'cessRate',v.cess_rate,
                'roundOff',v.round_off,'roundingScale',v.rounding_scale,
                'headerDiscountValue',v.header_discount,'currencyCode',v.currency_code,'exchangeRate',1);
            expected_result := jsonb_build_object('taxableValue',taxable,'discountValue',v.discount_value,
                'cgstValue',cgst,'sgstValue',sgst,'igstValue',igst,'cessValue',cess,
                'packingForwarding',v.packing,'freight',v.freight,'insurance',v.insurance,
                'otherCharges',v.other_charges,'roundOff',v.round_off,'totalPayableValue',payable,
                'grossAmount',gross,'headerDiscountValue',v.header_discount,'assessableValue',assessable,
                'currencyCode',v.currency_code,'exchangeRate',1);
            expected_tax := jsonb_build_object('id',v.tax_id,'organizationId',v.organization_id,
                'jurisdictionCode',v.jurisdiction,'hsnSacCode',v.hsn,'supplyType',v.supply_type,
                'supplierStateCode',v.supplier_state,'placeOfSupplyStateCode',v.supply_state,
                'vendorRegistrationType',v.registration_type,'gstRate',v.gst_rate,
                'cgstRate',v.cgst_rate,'sgstRate',v.sgst_rate,'igstRate',v.igst_rate,
                'cessRate',v.cess_rate,'isExempt',v.is_exempt,'isReverseCharge',v.is_reverse_charge,
                'currencyCode',v.tax_currency,'roundingScale',v.rounding_scale,
                'effectiveFrom',v.effective_from,'effectiveTo',v.effective_to,
                'approvalStatus',v.approval_status,'isActive',v.tax_active);
            RETURN p_commercial->'input' IS NOT DISTINCT FROM expected_input AND
                   p_commercial->'result' IS NOT DISTINCT FROM expected_result AND
                   p_tax IS NOT DISTINCT FROM expected_tax AND
                   (NOT (p_commercial ? 'taxRule') OR p_commercial->'taxRule' IS NOT DISTINCT FROM expected_tax);
        EXCEPTION WHEN OTHERS THEN RETURN FALSE;
        END $rev869b$;

        CREATE OR REPLACE FUNCTION nexa.rev869b_guard_child_insert()
        RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, nexa AS $rev869b$
        DECLARE matched bigint;
        BEGIN
            IF TG_TABLE_NAME='request_for_quotation_lines' THEN
                SELECT count(*) INTO matched FROM nexa.request_for_quotations r
                 WHERE r."Id"=NEW."RequestForQuotationId" AND r."Status"='Draft' AND r."Version"=0
                   AND length(trim(r."OrganizationId"))>0;
            ELSIF TG_TABLE_NAME='rfq_vendor_invitations' THEN
                SELECT count(*) INTO matched FROM nexa.request_for_quotations r
                 WHERE r."Id"=NEW."RequestForQuotationId" AND r."Status"='Issued'
                   AND length(trim(r."OrganizationId"))>0;
            ELSIF TG_TABLE_NAME='vendor_quotation_lines' THEN
                SELECT count(*) INTO matched FROM nexa.vendor_quotations q
                 WHERE q."Id"=NEW."VendorQuotationId" AND q."Status"='Draft' AND q."Version"=0
                   AND q."IsCurrentRevision" AND length(trim(q."OrganizationId"))>0;
            ELSIF TG_TABLE_NAME='commercial_comparison_lines' THEN
                SELECT count(*) INTO matched FROM nexa.commercial_comparisons c
                 WHERE c."Id"=NEW."CommercialComparisonId" AND c."Status"='Draft' AND c."Version"=0
                   AND length(trim(c."OrganizationId"))>0;
            ELSIF TG_TABLE_NAME='purchase_order_lines' THEN
                SELECT count(*) INTO matched FROM nexa.purchase_orders p
                 WHERE p."Id"=NEW."PurchaseOrderId" AND p."Status" IN ('Draft','RevisionDraft')
                   AND p."Version"=0 AND length(trim(p."OrganizationId"))>0;
            ELSIF TG_TABLE_NAME='quotation_technical_verifications' THEN
                SELECT count(*) INTO matched FROM nexa.vendor_quotation_lines ql
                JOIN nexa.vendor_quotations q ON q."Id"=ql."VendorQuotationId"
                 WHERE ql."Id"=NEW."VendorQuotationLineId" AND q."Status"='Submitted';
            ELSIF TG_TABLE_NAME='material_followup_handoffs' THEN
                SELECT count(*) INTO matched FROM nexa.purchase_orders p
                 WHERE p."Id"=NEW."PurchaseOrderId" AND p."Status"='Issued'
                   AND p."IsCurrentVersion" AND length(trim(p."OrganizationId"))>0;
            ELSE RAISE EXCEPTION 'Unsupported REV869B controlled child relation %.',TG_TABLE_NAME;
            END IF;
            IF matched<>1 THEN RAISE EXCEPTION 'REV869B child INSERT requires exactly one editable parent version.'; END IF;
            RETURN NEW;
        END $rev869b$;

        CREATE OR REPLACE FUNCTION nexa.rev869b_qualification_provenance_valid(qualification_id uuid)
        RETURNS boolean LANGUAGE sql STABLE SET search_path=pg_catalog,nexa AS $rev869b$
        SELECT EXISTS (
          SELECT 1 FROM nexa.vendor_qualifications q
          WHERE q."Id"=qualification_id AND q."VerificationStatus"='Approved' AND q."ApprovalStatus"='Approved'
            AND q."VerifiedByEmployeeId" IS NOT NULL AND q."ApprovedByEmployeeId" IS NOT NULL
            AND q."VerifiedByEmployeeId"<>q."ApprovedByEmployeeId"
            AND (SELECT count(*) FROM nexa.controlled_configuration_histories h
                 JOIN nexa.employee_identity_mappings m ON m."OrganizationId"=q."OrganizationId" AND m."Subject"=h."ActorLoginId"
                   AND m."EmployeeId"=q."VerifiedByEmployeeId" AND m."IsActive"
                 WHERE h."EntityType"='VendorQualification' AND h."EntityId"=q."Id" AND h."OrganizationId"=q."OrganizationId"
                   AND h."Action"='Verify' AND h."Version"=q."Version"-1
                   AND (h."AfterJson"->>'VerifiedByEmployeeId')::uuid IS NOT DISTINCT FROM q."VerifiedByEmployeeId")=1
            AND (SELECT count(*) FROM nexa.controlled_configuration_histories h
                 JOIN nexa.employee_identity_mappings m ON m."OrganizationId"=q."OrganizationId" AND m."Subject"=h."ActorLoginId"
                   AND m."EmployeeId"=q."ApprovedByEmployeeId" AND m."IsActive"
                 WHERE h."EntityType"='VendorQualification' AND h."EntityId"=q."Id" AND h."OrganizationId"=q."OrganizationId"
                   AND h."Action"='Approve' AND h."Version"=q."Version"
                   AND (h."AfterJson"->>'ApprovedByEmployeeId')::uuid IS NOT DISTINCT FROM q."ApprovedByEmployeeId")=1
        );
        $rev869b$;

        CREATE OR REPLACE FUNCTION nexa.rev869b_guard_authoritative_transition()
        RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, nexa AS $rev869b$
        DECLARE expected_count bigint; actual_count bigint; matched_count bigint; approval_count bigint;
                missing_count bigint; unexpected_count bigint; duplicate_count bigint; stale_version_count bigint;
                organization_mismatch_count bigint; parent_provenance_mismatch_count bigint;
                commercial_value_mismatch_count bigint; tax_mismatch_count bigint;
                attachment_qualification_mismatch_count bigint; approval_mismatch_count bigint;
        BEGIN
            IF TG_TABLE_NAME='commercial_comparisons' AND NEW."Status" IN ('PendingApproval','Approved') THEN
                SELECT count(*) INTO expected_count FROM nexa.vendor_quotation_lines ql WHERE ql."VendorQuotationId"=NEW."RecommendedVendorQuotationId";
                SELECT count(*) INTO matched_count FROM nexa.commercial_comparison_lines cl
                JOIN nexa.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId"
                JOIN nexa.vendor_quotations q ON q."Id"=ql."VendorQuotationId"
                JOIN nexa.rfq_vendor_invitations i ON i."Id"=q."RfqVendorInvitationId"
                JOIN nexa.request_for_quotation_lines rl ON rl."Id"=ql."RequestForQuotationLineId"
                JOIN nexa.items item ON item."Id"=rl."ItemId"
                JOIN nexa.uoms uom ON uom."Id"=item."BaseUomId"
                JOIN nexa.vendors vendor ON vendor."Id"=q."VendorId"
                JOIN nexa.vendor_qualifications qualification ON qualification."VendorId"=vendor."Id"
                  AND qualification."OrganizationId"=q."OrganizationId"
                  AND qualification."ItemCategoryId"=item."CategoryId"
                 WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended" AND
                       q."Id"=NEW."RecommendedVendorQuotationId" AND q."VendorId"=NEW."SelectedVendorId" AND
                       q."OrganizationId"=NEW."OrganizationId" AND i."RequestForQuotationId"=NEW."RequestForQuotationId" AND
                       item."IsActive" AND item."ApprovalStatus"='Approved' AND uom."IsActive" AND uom."Code"=rl."UomSnapshot" AND
                       vendor."IsActive" AND vendor."VendorStatus"='Active' AND vendor."ApprovalStatus"='Approved' AND
                       vendor."CommercialVerificationStatus"='Approved' AND vendor."EffectiveFrom"<=q."ReceivedAt"::date AND
                       (vendor."EffectiveTo" IS NULL OR vendor."EffectiveTo">=q."ReceivedAt"::date) AND
                       qualification."IsActive" AND qualification."VerificationStatus"='Approved' AND qualification."ApprovalStatus"='Approved' AND
                       qualification."VerifiedByEmployeeId" IS NOT NULL AND qualification."ApprovedByEmployeeId" IS NOT NULL AND
                       qualification."VerifiedByEmployeeId"<>qualification."ApprovedByEmployeeId" AND
                       nexa.rev869b_qualification_provenance_valid(qualification."Id") IS TRUE AND
                       qualification."EffectiveFrom"<=i."InvitedAt"::date AND
                       (qualification."EffectiveTo" IS NULL OR qualification."EffectiveTo">=i."InvitedAt"::date) AND
                       jsonb_typeof(i."VendorQualificationSnapshotJson")='object' AND
                       jsonb_typeof(i."VendorQualificationSnapshotJson"->'qualifications')='array' AND
                       jsonb_array_length(i."VendorQualificationSnapshotJson"->'qualifications')>0 AND
                       (i."VendorQualificationSnapshotJson"->>'snapshotAt')::timestamptz IS NOT DISTINCT FROM i."InvitedAt" AND
                       EXISTS (SELECT 1 FROM jsonb_array_elements(i."VendorQualificationSnapshotJson"->'qualifications') evidence WHERE
                         (evidence->>'vendorQualificationId')::uuid IS NOT DISTINCT FROM qualification."Id" AND
                         (evidence->>'vendorId')::uuid IS NOT DISTINCT FROM qualification."VendorId" AND
                         evidence->>'organizationId' IS NOT DISTINCT FROM qualification."OrganizationId" AND
                         (evidence->>'itemCategoryId')::uuid IS NOT DISTINCT FROM qualification."ItemCategoryId" AND
                         evidence->>'qualificationType' IS NOT DISTINCT FROM qualification."QualificationCode" AND
                         (evidence->>'qualificationVersion')::bigint IS NOT DISTINCT FROM qualification."Version" AND
                         (evidence->>'effectiveFrom')::date IS NOT DISTINCT FROM qualification."EffectiveFrom" AND
                         (evidence->>'effectiveTo')::date IS NOT DISTINCT FROM qualification."EffectiveTo" AND
                         evidence->>'verificationStatus' IS NOT DISTINCT FROM qualification."VerificationStatus" AND
                         (evidence->>'verifiedByEmployeeId')::uuid IS NOT DISTINCT FROM qualification."VerifiedByEmployeeId" AND
                         evidence->>'approvalStatus' IS NOT DISTINCT FROM qualification."ApprovalStatus" AND
                         (evidence->>'approvedByEmployeeId')::uuid IS NOT DISTINCT FROM qualification."ApprovedByEmployeeId" AND
                         (evidence->>'isActive')::boolean IS TRUE) AND
                       length(trim(q."AttachmentObjectKey"))>0 AND q."AttachmentSha256"~'^[0-9A-F]{64}$' AND
                       cl."VendorId"=NEW."SelectedVendorId" AND q."IsCurrentRevision" AND q."Status"='TechnicallyCompliant' AND
                       cl."CommercialSnapshotJson"->'taxRule' IS NOT NULL AND
                       cl."CommercialSnapshotJson"->'taxRule' IS NOT DISTINCT FROM ql."TaxRuleSnapshotJson" AND
                       nexa.rev869b_commercial_snapshot_reconciles(ql."Id",cl."CommercialSnapshotJson",ql."TaxRuleSnapshotJson") AND
                       cl."CommercialSnapshotJson" - ARRAY['input','result','taxRule'] IS NOT DISTINCT FROM
                       jsonb_build_object('organizationId',NEW."OrganizationId",'commercialComparisonId',NEW."Id",
                           'requestForQuotationId',NEW."RequestForQuotationId",'vendorId',q."VendorId",
                           'vendorQuotationId',q."Id",'quotationRevision',q."RevisionNumber",
                           'vendorQuotationLineId',ql."Id",'itemId',rl."ItemId",'quantity',ql."Quantity",
                           'uom',rl."UomSnapshot",'currencyCode',q."CurrencyCode",'exchangeRate',1) AND
                       (SELECT count(*) FROM nexa.quotation_technical_verifications tv
                         WHERE tv."VendorQuotationLineId"=ql."Id" AND tv."ComplianceStatus"='TechnicallyCompliant')=1;
                SELECT count(*) INTO actual_count FROM nexa.commercial_comparison_lines cl
                 WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended";
                SELECT count(*) INTO duplicate_count FROM (SELECT cl."VendorQuotationLineId" FROM nexa.commercial_comparison_lines cl
                 WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended"
                 GROUP BY cl."VendorQuotationLineId" HAVING count(*)<>1) duplicate_rows;
                missing_count:=greatest(expected_count-matched_count,0);
                SELECT count(*) INTO unexpected_count FROM nexa.commercial_comparison_lines cl
                 WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended" AND NOT EXISTS
                       (SELECT 1 FROM nexa.vendor_quotation_lines ql WHERE ql."Id"=cl."VendorQuotationLineId" AND ql."VendorQuotationId"=NEW."RecommendedVendorQuotationId");
                SELECT count(*) INTO stale_version_count FROM nexa.commercial_comparison_lines cl
                 JOIN nexa.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId"
                 JOIN nexa.vendor_quotations q ON q."Id"=ql."VendorQuotationId"
                 WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended" AND NOT q."IsCurrentRevision";
                SELECT count(*) INTO organization_mismatch_count FROM nexa.commercial_comparison_lines cl
                 JOIN nexa.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId"
                 JOIN nexa.vendor_quotations q ON q."Id"=ql."VendorQuotationId"
                 WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended" AND q."OrganizationId" IS DISTINCT FROM NEW."OrganizationId";
                SELECT count(*) INTO parent_provenance_mismatch_count FROM nexa.commercial_comparison_lines cl
                 JOIN nexa.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId"
                 JOIN nexa.vendor_quotations q ON q."Id"=ql."VendorQuotationId"
                 JOIN nexa.rfq_vendor_invitations i ON i."Id"=q."RfqVendorInvitationId"
                 WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended" AND
                       (q."Id" IS DISTINCT FROM NEW."RecommendedVendorQuotationId" OR i."RequestForQuotationId" IS DISTINCT FROM NEW."RequestForQuotationId");
                SELECT count(*) INTO commercial_value_mismatch_count FROM nexa.commercial_comparison_lines cl
                 JOIN nexa.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId"
                 WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended" AND
                       nexa.rev869b_commercial_snapshot_reconciles(ql."Id",cl."CommercialSnapshotJson",ql."TaxRuleSnapshotJson") IS NOT TRUE;
                SELECT count(*) INTO tax_mismatch_count FROM nexa.commercial_comparison_lines cl
                 JOIN nexa.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId"
                 WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended" AND
                       (cl."CommercialSnapshotJson"->'taxRule' IS NULL OR cl."CommercialSnapshotJson"->'taxRule' IS DISTINCT FROM ql."TaxRuleSnapshotJson");
                SELECT count(*) INTO attachment_qualification_mismatch_count FROM nexa.commercial_comparison_lines cl
                 JOIN nexa.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId"
                 JOIN nexa.vendor_quotations q ON q."Id"=ql."VendorQuotationId"
                 JOIN nexa.rfq_vendor_invitations i ON i."Id"=q."RfqVendorInvitationId"
                 WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended" AND
                       (length(trim(q."AttachmentObjectKey"))=0 OR q."AttachmentSha256"!~'^[0-9A-F]{64}$' OR
                        jsonb_typeof(i."VendorQualificationSnapshotJson") IS DISTINCT FROM 'object');
                SELECT count(*) INTO approval_mismatch_count FROM nexa.commercial_comparison_lines cl
                 WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended" AND
                       (SELECT count(*) FROM nexa.quotation_technical_verifications tv
                         WHERE tv."VendorQuotationLineId"=cl."VendorQuotationLineId" AND tv."ComplianceStatus"='TechnicallyCompliant') IS DISTINCT FROM 1;
                IF expected_count=0 OR matched_count<>expected_count OR actual_count<>expected_count OR
                   missing_count<>0 OR unexpected_count<>0 OR duplicate_count<>0 OR stale_version_count<>0 OR
                   organization_mismatch_count<>0 OR parent_provenance_mismatch_count<>0 OR
                   commercial_value_mismatch_count<>0 OR tax_mismatch_count<>0 OR
                   attachment_qualification_mismatch_count<>0 OR approval_mismatch_count<>0 OR
                   NEW."TotalPayableValue" IS DISTINCT FROM (SELECT sum(cl."TotalPayableValue") FROM nexa.commercial_comparison_lines cl WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended")
                THEN RAISE EXCEPTION 'Comparison authoritative joins/cardinality/commercial evidence failed.'; END IF;
            ELSIF TG_TABLE_NAME='purchase_orders' AND NEW."Status" IN ('PendingApproval','Resubmitted','Approved','Issued') THEN
                SELECT count(*) INTO expected_count FROM nexa.vendor_quotation_lines ql
                JOIN nexa.commercial_comparisons c ON c."RecommendedVendorQuotationId"=ql."VendorQuotationId"
                 WHERE c."Id"=NEW."CommercialComparisonId" AND c."Status"='Approved';
                SELECT count(*) INTO matched_count FROM nexa.purchase_order_lines pl
                JOIN nexa.commercial_comparison_lines cl ON cl."Id"=pl."CommercialComparisonLineId"
                JOIN nexa.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId"
                JOIN nexa.vendor_quotations q ON q."Id"=ql."VendorQuotationId"
                JOIN nexa.rfq_vendor_invitations i ON i."Id"=q."RfqVendorInvitationId"
                JOIN nexa.request_for_quotations r ON r."Id"=i."RequestForQuotationId"
                JOIN nexa.request_for_quotation_lines rl ON rl."Id"=ql."RequestForQuotationLineId"
                JOIN nexa.items item ON item."Id"=rl."ItemId"
                JOIN nexa.uoms uom ON uom."Id"=item."BaseUomId"
                JOIN nexa.vendors vendor ON vendor."Id"=q."VendorId"
                JOIN nexa.vendor_qualifications qualification ON qualification."VendorId"=vendor."Id"
                  AND qualification."OrganizationId"=q."OrganizationId"
                  AND qualification."ItemCategoryId"=item."CategoryId"
                JOIN nexa.commercial_comparisons c ON c."Id"=NEW."CommercialComparisonId"
                 WHERE pl."PurchaseOrderId"=NEW."Id" AND cl."CommercialComparisonId"=c."Id" AND cl."IsRecommended" AND
                       c."Status"='Approved' AND c."RecommendedVendorQuotationId"=q."Id" AND c."SelectedVendorId"=q."VendorId" AND
                       c."OrganizationId"=NEW."OrganizationId" AND r."OrganizationId"=NEW."OrganizationId" AND
                       item."IsActive" AND item."ApprovalStatus"='Approved' AND uom."IsActive" AND uom."Code"=rl."UomSnapshot" AND
                       vendor."IsActive" AND vendor."VendorStatus"='Active' AND vendor."ApprovalStatus"='Approved' AND
                       vendor."CommercialVerificationStatus"='Approved' AND vendor."EffectiveFrom"<=q."ReceivedAt"::date AND
                       (vendor."EffectiveTo" IS NULL OR vendor."EffectiveTo">=q."ReceivedAt"::date) AND
                       qualification."IsActive" AND qualification."VerificationStatus"='Approved' AND qualification."ApprovalStatus"='Approved' AND
                       qualification."VerifiedByEmployeeId" IS NOT NULL AND qualification."ApprovedByEmployeeId" IS NOT NULL AND
                       qualification."VerifiedByEmployeeId"<>qualification."ApprovedByEmployeeId" AND
                       nexa.rev869b_qualification_provenance_valid(qualification."Id") IS TRUE AND
                       qualification."EffectiveFrom"<=i."InvitedAt"::date AND
                       (qualification."EffectiveTo" IS NULL OR qualification."EffectiveTo">=i."InvitedAt"::date) AND
                       jsonb_typeof(i."VendorQualificationSnapshotJson")='object' AND
                       jsonb_typeof(i."VendorQualificationSnapshotJson"->'qualifications')='array' AND
                       (i."VendorQualificationSnapshotJson"->>'snapshotAt')::timestamptz IS NOT DISTINCT FROM i."InvitedAt" AND
                       EXISTS (SELECT 1 FROM jsonb_array_elements(i."VendorQualificationSnapshotJson"->'qualifications') evidence WHERE
                         (evidence->>'vendorQualificationId')::uuid IS NOT DISTINCT FROM qualification."Id" AND
                         (evidence->>'vendorId')::uuid IS NOT DISTINCT FROM qualification."VendorId" AND
                         evidence->>'organizationId' IS NOT DISTINCT FROM qualification."OrganizationId" AND
                         (evidence->>'itemCategoryId')::uuid IS NOT DISTINCT FROM qualification."ItemCategoryId" AND
                         evidence->>'qualificationType' IS NOT DISTINCT FROM qualification."QualificationCode" AND
                         (evidence->>'qualificationVersion')::bigint IS NOT DISTINCT FROM qualification."Version" AND
                         (evidence->>'effectiveFrom')::date IS NOT DISTINCT FROM qualification."EffectiveFrom" AND
                         (evidence->>'effectiveTo')::date IS NOT DISTINCT FROM qualification."EffectiveTo" AND
                         evidence->>'verificationStatus' IS NOT DISTINCT FROM qualification."VerificationStatus" AND
                         (evidence->>'verifiedByEmployeeId')::uuid IS NOT DISTINCT FROM qualification."VerifiedByEmployeeId" AND
                         evidence->>'approvalStatus' IS NOT DISTINCT FROM qualification."ApprovalStatus" AND
                         (evidence->>'approvedByEmployeeId')::uuid IS NOT DISTINCT FROM qualification."ApprovedByEmployeeId" AND
                         (evidence->>'isActive')::boolean IS TRUE) AND
                       NEW."VendorId"=q."VendorId" AND pl."ItemId"=rl."ItemId" AND pl."OrderedQuantity"=ql."Quantity" AND
                       pl."UnitRate"=ql."UnitRate" AND pl."UomSnapshot"=rl."UomSnapshot" AND
                       pl."TaxRuleSnapshotJson" IS NOT DISTINCT FROM ql."TaxRuleSnapshotJson" AND
                       jsonb_typeof(pl."CommercialSnapshotJson")='object' AND
                       jsonb_object_length(pl."CommercialSnapshotJson" - ARRAY['input','result'])=18 AND
                       (pl."CommercialSnapshotJson" - ARRAY['input','result']) ?& ARRAY[
                         'vendorQuotationId','vendorQuotationLineId','requestForQuotationId','commercialComparisonId',
                         'vendorId','organizationId','vendorQualificationSnapshotJson','attachmentObjectKey',
                         'attachmentSha256','comparisonApprovalRoute','comparisonApprovedAt','quotationReceivedAt',
                         'quotationRevision','itemId','quantity','uom','currencyCode','exchangeRate'] AND
                       nexa.rev869b_commercial_snapshot_reconciles(ql."Id",pl."CommercialSnapshotJson",pl."TaxRuleSnapshotJson") AND
                       (pl."CommercialSnapshotJson"->>'organizationId') IS NOT DISTINCT FROM NEW."OrganizationId" AND
                       (pl."CommercialSnapshotJson"->>'vendorQuotationId')::uuid IS NOT DISTINCT FROM q."Id" AND
                       (pl."CommercialSnapshotJson"->>'vendorQuotationLineId')::uuid IS NOT DISTINCT FROM ql."Id" AND
                       (pl."CommercialSnapshotJson"->>'requestForQuotationId')::uuid IS NOT DISTINCT FROM r."Id" AND
                       (pl."CommercialSnapshotJson"->>'commercialComparisonId')::uuid IS NOT DISTINCT FROM c."Id" AND
                       (pl."CommercialSnapshotJson"->>'quotationRevision')::integer IS NOT DISTINCT FROM q."RevisionNumber" AND
                       (pl."CommercialSnapshotJson"->>'itemId')::uuid IS NOT DISTINCT FROM rl."ItemId" AND
                       (pl."CommercialSnapshotJson"->>'quantity')::numeric IS NOT DISTINCT FROM ql."Quantity" AND
                       (pl."CommercialSnapshotJson"->>'uom') IS NOT DISTINCT FROM rl."UomSnapshot" AND
                       (pl."CommercialSnapshotJson"->>'currencyCode') IS NOT DISTINCT FROM q."CurrencyCode" AND
                       (pl."CommercialSnapshotJson"->>'exchangeRate')::numeric IS NOT DISTINCT FROM 1 AND
                       (pl."CommercialSnapshotJson"->>'comparisonApprovalRoute') IS NOT DISTINCT FROM c."ApprovalRoute" AND
                       (pl."CommercialSnapshotJson"->>'quotationReceivedAt')::timestamptz IS NOT DISTINCT FROM q."ReceivedAt" AND
                       (SELECT count(*) FROM nexa.purchase_transaction_approval_history ah
                         WHERE ah."CommercialComparisonId"=c."Id" AND ah."Action"='Approve' AND ah."ToStatus"='Approved'
                           AND ah."ApprovalRoute"=c."ApprovalRoute" AND
                           ah."CreatedAt" IS NOT DISTINCT FROM (pl."CommercialSnapshotJson"->>'comparisonApprovedAt')::timestamptz)=1 AND
                       (pl."CommercialSnapshotJson"->>'attachmentObjectKey') IS NOT DISTINCT FROM q."AttachmentObjectKey" AND
                       (pl."CommercialSnapshotJson"->>'attachmentSha256') IS NOT DISTINCT FROM q."AttachmentSha256" AND
                       (pl."CommercialSnapshotJson"->>'vendorQualificationSnapshotJson') IS NOT DISTINCT FROM i."VendorQualificationSnapshotJson"::text;
                SELECT count(*) INTO actual_count FROM nexa.purchase_order_lines pl WHERE pl."PurchaseOrderId"=NEW."Id";
                SELECT count(*) INTO duplicate_count FROM (SELECT pl."CommercialComparisonLineId" FROM nexa.purchase_order_lines pl
                 WHERE pl."PurchaseOrderId"=NEW."Id" GROUP BY pl."CommercialComparisonLineId" HAVING count(*)<>1) duplicate_rows;
                missing_count:=greatest(expected_count-matched_count,0);
                SELECT count(*) INTO unexpected_count FROM nexa.purchase_order_lines pl
                 WHERE pl."PurchaseOrderId"=NEW."Id" AND NOT EXISTS
                       (SELECT 1 FROM nexa.commercial_comparison_lines cl
                         WHERE cl."Id"=pl."CommercialComparisonLineId" AND cl."CommercialComparisonId"=NEW."CommercialComparisonId" AND cl."IsRecommended");
                SELECT count(*) INTO stale_version_count FROM nexa.purchase_order_lines pl
                 JOIN nexa.commercial_comparison_lines cl ON cl."Id"=pl."CommercialComparisonLineId"
                 JOIN nexa.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId"
                 JOIN nexa.vendor_quotations q ON q."Id"=ql."VendorQuotationId"
                 WHERE pl."PurchaseOrderId"=NEW."Id" AND (NOT q."IsCurrentRevision" OR
                       (pl."CommercialSnapshotJson"->>'quotationRevision')::integer IS DISTINCT FROM q."RevisionNumber");
                SELECT count(*) INTO organization_mismatch_count FROM nexa.purchase_order_lines pl
                 WHERE pl."PurchaseOrderId"=NEW."Id" AND
                       (pl."CommercialSnapshotJson"->>'organizationId') IS DISTINCT FROM NEW."OrganizationId";
                SELECT count(*) INTO parent_provenance_mismatch_count FROM nexa.purchase_order_lines pl
                 JOIN nexa.commercial_comparison_lines cl ON cl."Id"=pl."CommercialComparisonLineId"
                 JOIN nexa.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId"
                 WHERE pl."PurchaseOrderId"=NEW."Id" AND
                       ((pl."CommercialSnapshotJson"->>'commercialComparisonId')::uuid IS DISTINCT FROM NEW."CommercialComparisonId" OR
                        (pl."CommercialSnapshotJson"->>'vendorQuotationLineId')::uuid IS DISTINCT FROM ql."Id");
                SELECT count(*) INTO commercial_value_mismatch_count FROM nexa.purchase_order_lines pl
                 JOIN nexa.commercial_comparison_lines cl ON cl."Id"=pl."CommercialComparisonLineId"
                 JOIN nexa.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId"
                 WHERE pl."PurchaseOrderId"=NEW."Id" AND
                       nexa.rev869b_commercial_snapshot_reconciles(ql."Id",pl."CommercialSnapshotJson",pl."TaxRuleSnapshotJson") IS NOT TRUE;
                SELECT count(*) INTO tax_mismatch_count FROM nexa.purchase_order_lines pl
                 JOIN nexa.commercial_comparison_lines cl ON cl."Id"=pl."CommercialComparisonLineId"
                 JOIN nexa.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId"
                 WHERE pl."PurchaseOrderId"=NEW."Id" AND pl."TaxRuleSnapshotJson" IS DISTINCT FROM ql."TaxRuleSnapshotJson";
                SELECT count(*) INTO attachment_qualification_mismatch_count FROM nexa.purchase_order_lines pl
                 WHERE pl."PurchaseOrderId"=NEW."Id" AND
                       (length(trim(coalesce(pl."CommercialSnapshotJson"->>'attachmentObjectKey','')))=0 OR
                        coalesce(pl."CommercialSnapshotJson"->>'attachmentSha256','')!~'^[0-9A-F]{64}$' OR
                        coalesce(pl."CommercialSnapshotJson"->>'vendorQualificationSnapshotJson','{}')='{}');
                SELECT count(*) INTO approval_mismatch_count FROM nexa.purchase_order_lines pl
                 WHERE pl."PurchaseOrderId"=NEW."Id" AND
                       (pl."CommercialSnapshotJson"->>'comparisonApprovalRoute') IS DISTINCT FROM NEW."ApprovalRoute";
                IF expected_count=0 OR matched_count<>expected_count OR actual_count<>expected_count OR
                   missing_count<>0 OR unexpected_count<>0 OR duplicate_count<>0 OR stale_version_count<>0 OR
                   organization_mismatch_count<>0 OR parent_provenance_mismatch_count<>0 OR
                   commercial_value_mismatch_count<>0 OR tax_mismatch_count<>0 OR
                   attachment_qualification_mismatch_count<>0 OR approval_mismatch_count<>0 OR
                   NEW."CurrencyCode" IS DISTINCT FROM
                   (SELECT q."CurrencyCode" FROM nexa.commercial_comparisons c JOIN nexa.vendor_quotations q ON q."Id"=c."RecommendedVendorQuotationId" WHERE c."Id"=NEW."CommercialComparisonId") OR
                   NEW."TotalPayableValue" IS DISTINCT FROM (SELECT sum(pl."TotalPayableValue") FROM nexa.purchase_order_lines pl WHERE pl."PurchaseOrderId"=NEW."Id")
                THEN RAISE EXCEPTION 'PO exact source/version/cardinality/commercial provenance failed.'; END IF;

                SELECT count(*) INTO approval_count FROM nexa.purchase_transaction_approval_policies p
                 WHERE p."OrganizationId"=NEW."OrganizationId" AND p."RouteCode"=NEW."ApprovalRoute" AND p."IsActive" AND
                       NEW."TotalPayableValue">=p."MinimumAmount" AND (p."MaximumAmount" IS NULL OR NEW."TotalPayableValue"<=p."MaximumAmount") AND
                       p."EffectiveFrom"<=(NEW."ApprovalPolicySnapshotJson"->>'effectiveOn')::date AND
                       (p."EffectiveTo" IS NULL OR p."EffectiveTo">=(NEW."ApprovalPolicySnapshotJson"->>'effectiveOn')::date);
                IF approval_count<>1 OR jsonb_typeof(NEW."ApprovalPolicySnapshotJson")<>'object' OR
                   jsonb_object_length(NEW."ApprovalPolicySnapshotJson")<>4 OR
                   jsonb_typeof(NEW."ApprovalPolicySnapshotJson"->'organizationId')<>'string' OR
                   jsonb_typeof(NEW."ApprovalPolicySnapshotJson"->'routeCode')<>'string' OR
                   jsonb_typeof(NEW."ApprovalPolicySnapshotJson"->'approvalValue')<>'number' OR
                   jsonb_typeof(NEW."ApprovalPolicySnapshotJson"->'effectiveOn')<>'string' OR
                   (NEW."ApprovalPolicySnapshotJson"->>'organizationId') IS DISTINCT FROM NEW."OrganizationId" OR
                   (NEW."ApprovalPolicySnapshotJson"->>'routeCode') IS DISTINCT FROM NEW."ApprovalRoute" OR
                   (NEW."ApprovalPolicySnapshotJson"->>'approvalValue')::numeric IS DISTINCT FROM NEW."TotalPayableValue"
                THEN RAISE EXCEPTION 'PO exact approval policy evidence failed.'; END IF;
                IF NEW."Status"='Issued' AND (SELECT count(*) FROM nexa.purchase_order_history h
                    WHERE h."PurchaseOrderId"=NEW."Id" AND h."ToStatus"='Approved' AND h."RevisionNumber"=NEW."RevisionNumber")<>1
                THEN RAISE EXCEPTION 'PO issue requires exactly one approval history for this version.'; END IF;
            END IF;
            RETURN NEW;
        EXCEPTION WHEN invalid_text_representation OR numeric_value_out_of_range OR invalid_datetime_format THEN
            RAISE EXCEPTION 'REV869B snapshot JSON contains malformed typed evidence.';
        END $rev869b$;

        CREATE OR REPLACE FUNCTION nexa.rev869b_guard_history_insert()
        RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, nexa AS $rev869b$
        BEGIN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_history_binding_not_installed',
              MESSAGE='REV869B history binding must be installed before history INSERT is permitted.';
        END $rev869b$;

        CREATE OR REPLACE FUNCTION nexa.rev869b_guard_extended_immutability()
        RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, nexa AS $rev869b$
        BEGIN
            IF TG_TABLE_NAME='rfq_vendor_invitations' AND TG_OP='UPDATE' THEN
                IF to_jsonb(NEW)-ARRAY['Status','Version','UpdatedAt','UpdatedBy']
                   IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','Version','UpdatedAt','UpdatedBy'] THEN
                    RAISE EXCEPTION 'RFQ invitation qualification and provenance snapshot is immutable.';
                END IF;
                RETURN NEW;
            END IF;
            RAISE EXCEPTION 'REV869B controlled relation % rejects unauthorized %.',TG_TABLE_NAME,TG_OP;
        END $rev869b$;

        CREATE TRIGGER trg_rev869b_rfq_line_insert_guard BEFORE INSERT ON nexa.request_for_quotation_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_child_insert();
        CREATE TRIGGER trg_rev869b_invitation_insert_guard BEFORE INSERT ON nexa.rfq_vendor_invitations FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_child_insert();
        CREATE TRIGGER trg_rev869b_quotation_line_insert_guard BEFORE INSERT ON nexa.vendor_quotation_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_child_insert();
        CREATE TRIGGER trg_rev869b_comparison_line_insert_guard BEFORE INSERT ON nexa.commercial_comparison_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_child_insert();
        CREATE TRIGGER trg_rev869b_po_line_insert_guard BEFORE INSERT ON nexa.purchase_order_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_child_insert();
        CREATE TRIGGER trg_rev869b_technical_insert_guard BEFORE INSERT ON nexa.quotation_technical_verifications FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_child_insert();
        CREATE TRIGGER trg_rev869b_followup_insert_guard BEFORE INSERT ON nexa.material_followup_handoffs FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_child_insert();
        CREATE TRIGGER trg_rev869b_comparison_authoritative_guard BEFORE UPDATE ON nexa.commercial_comparisons FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_authoritative_transition();
        CREATE TRIGGER trg_rev869b_po_authoritative_guard BEFORE UPDATE ON nexa.purchase_orders FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_authoritative_transition();
        CREATE TRIGGER trg_rev869b_comparison_history_insert_guard BEFORE INSERT ON nexa.purchase_transaction_approval_history FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_history_insert();
        CREATE TRIGGER trg_rev869b_po_history_insert_guard BEFORE INSERT ON nexa.purchase_order_history FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_history_insert();
        CREATE TRIGGER trg_rev869b_status_history_insert_guard BEFORE INSERT ON nexa.purchase_transaction_status_history FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_history_insert();
        CREATE TRIGGER trg_rev869b_rfq_lines_immutable BEFORE UPDATE OR DELETE ON nexa.request_for_quotation_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_extended_immutability();
        CREATE TRIGGER trg_rev869b_invitation_snapshot_immutable BEFORE UPDATE OR DELETE ON nexa.rfq_vendor_invitations FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_extended_immutability();
        CREATE TRIGGER trg_rev869b_comparison_lines_delete_guard BEFORE DELETE ON nexa.commercial_comparison_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_extended_immutability();
        CREATE TRIGGER trg_rev869b_followup_immutable BEFORE UPDATE OR DELETE ON nexa.material_followup_handoffs FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_extended_immutability();
        """;

    public const string Remove = """
        DROP FUNCTION IF EXISTS nexa.rev869b_qualification_provenance_valid(uuid) CASCADE;
        DROP FUNCTION IF EXISTS nexa.rev869b_guard_history_insert() CASCADE;
        DROP FUNCTION IF EXISTS nexa.rev869b_guard_extended_immutability() CASCADE;
        DROP FUNCTION IF EXISTS nexa.rev869b_guard_authoritative_transition() CASCADE;
        DROP FUNCTION IF EXISTS nexa.rev869b_guard_child_insert() CASCADE;
        DROP FUNCTION IF EXISTS nexa.rev869b_commercial_snapshot_reconciles(uuid,jsonb,jsonb) CASCADE;
        """;
}
