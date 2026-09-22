CREATE TABLE advance.vendor_manual_assessments (
 "Id" uuid PRIMARY KEY,
 "CompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
 "GoodsReceiptId" uuid NOT NULL REFERENCES advance.goods_receipts("Id"),
 "GoodsReceiptVersion" bigint NOT NULL CHECK("GoodsReceiptVersion">=0),
 "VendorId" uuid NOT NULL REFERENCES advance.vendors("Id"),
 "PurchaseOrderId" uuid NOT NULL REFERENCES advance.purchase_orders("Id"),
 "RevisionNumber" integer NOT NULL CHECK("RevisionNumber">0),
 "SupersedesAssessmentId" uuid REFERENCES advance.vendor_manual_assessments("Id"),
 "TechnicalPoints" numeric NOT NULL CHECK("TechnicalPoints">=0 AND "TechnicalPoints"<=15),
 "ResponsePoints" numeric NOT NULL CHECK("ResponsePoints">=0 AND "ResponsePoints"<=5),
 "OverallPoints" numeric NOT NULL CHECK("OverallPoints">=0 AND "OverallPoints"<=5),
 "Reason" text NOT NULL CHECK(length(btrim("Reason")) BETWEEN 1 AND 2000 AND "Reason" !~ '[[:cntrl:]]'),
 "RecordedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),
 "ActorEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 "RoleAssignmentId" uuid NOT NULL REFERENCES advance.employee_role_assignments("Id"),
 "RoleAssignmentType" text NOT NULL,
 "RecordedBy" text NOT NULL CHECK(length(btrim("RecordedBy"))>0),
 UNIQUE("CompanyId","Id"), UNIQUE("CompanyId","GoodsReceiptId","RevisionNumber"),
 CHECK(("RevisionNumber"=1 AND "SupersedesAssessmentId" IS NULL)
    OR ("RevisionNumber">1 AND "SupersedesAssessmentId" IS NOT NULL))
);
CREATE FUNCTION advance.guard_vendor_manual_assessment() RETURNS trigger
 LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $rating$
BEGIN
 IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Manual vendor assessments are immutable; append a reasoned revision.'; END IF;
 IF current_setting('sess.vendor_manual_assessment_write',true) IS DISTINCT FROM txid_current()::text
  OR current_user IS DISTINCT FROM (SELECT pg_get_userbyid(relowner) FROM pg_class WHERE oid=TG_RELID)
 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Manual assessments require the governed QC Manager command.'; END IF;
 RETURN NEW;
END $rating$;
CREATE TRIGGER trg_vendor_manual_assessment BEFORE INSERT OR UPDATE OR DELETE
 ON advance.vendor_manual_assessments FOR EACH ROW EXECUTE FUNCTION advance.guard_vendor_manual_assessment();

CREATE FUNCTION advance.vendor_manual_assessment_json(p_company uuid,p_id uuid) RETURNS jsonb
 LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $rating$
BEGIN
 IF session_user<>'nexa_erp_runtime' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Manual assessment reads require runtime.'; END IF;
 RETURN (SELECT to_jsonb(a)||jsonb_build_object('Replayed',false)
  FROM advance.vendor_manual_assessments a WHERE a."CompanyId"=p_company AND a."Id"=p_id);
END $rating$;
CREATE FUNCTION advance.vendor_manual_assessment_history(p_company uuid,p_receipt uuid) RETURNS jsonb
 LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $rating$
BEGIN
 IF session_user<>'nexa_erp_runtime' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Manual assessment reads require runtime.'; END IF;
 RETURN (SELECT coalesce(jsonb_agg(advance.vendor_manual_assessment_json(p_company,a."Id") ORDER BY a."RevisionNumber"),'[]'::jsonb)
  FROM advance.vendor_manual_assessments a WHERE a."CompanyId"=p_company AND a."GoodsReceiptId"=p_receipt);
END $rating$;

CREATE FUNCTION advance.record_vendor_manual_assessment(p_company uuid,p_command uuid,p_receipt uuid,p_receipt_version bigint,
 p_supersedes uuid,p_technical numeric,p_response numeric,p_overall numeric,p_reason text,
 p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text) RETURNS jsonb
 LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $rating$
DECLARE receipt advance.goods_receipts%ROWTYPE; prior advance.vendor_manual_assessments%ROWTYPE;
BEGIN
 IF session_user<>'nexa_erp_runtime' OR p_role IS DISTINCT FROM 'QC_MANAGER'
  OR NOT EXISTS(SELECT 1 FROM advance.companies c WHERE c."Id"=p_company AND c."IsActive" AND c."Status"='ACTIVE'
   AND advance.ordinary_command_context_valid(c."Code",p_actor,current_setting('advance.ordinary_identity_issuer',true),
    current_setting('advance.ordinary_identity_subject',true),p_role))
  OR NOT EXISTS(SELECT 1 FROM advance.command_requests r WHERE r."CommandId"=p_command
   AND p_command=nullif(current_setting('advance.ordinary_command_id',true),'')::uuid
   AND r."Operation"='VendorManualAssessment.Record' AND r."ResolvedRoleAssignmentId"=p_assignment)
  OR NOT EXISTS(SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,'create',ARRAY[p_role]) a
   WHERE a."AssignmentId"=p_assignment AND a."AssignmentType"=p_type)
 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Manual assessment requires the exact governed QC Manager command.'; END IF;
 IF p_technical IS NULL OR NOT(p_technical BETWEEN 0 AND 15)
  OR p_response IS NULL OR NOT(p_response BETWEEN 0 AND 5)
  OR p_overall IS NULL OR NOT(p_overall BETWEEN 0 AND 5)
  OR length(btrim(coalesce(p_reason,''))) NOT BETWEEN 1 AND 2000 OR p_reason ~ '[[:cntrl:]]'
  OR length(btrim(coalesce(p_login,'')))=0
 THEN RAISE EXCEPTION 'Technical /15, Response /5, Overall /5 and a reason are required.'; END IF;
 -- Lock the actual GRN: this also serializes competing first assessments and revisions.
 SELECT * INTO receipt FROM advance.goods_receipts WHERE "CompanyId"=p_company AND "Id"=p_receipt FOR UPDATE;
 IF NOT FOUND OR receipt."DocumentKind"<>'NORMAL' OR receipt."Status"<>'FINALIZED'
  OR p_receipt_version IS DISTINCT FROM receipt."Version"
  OR EXISTS(SELECT 1 FROM advance.goods_receipts reversed WHERE reversed."CompanyId"=p_company
   AND reversed."ReversesGoodsReceiptId"=p_receipt AND reversed."Status"='FINALIZED')
 THEN RAISE EXCEPTION 'Assessment requires the current finalized, unreversed normal GRN in this company.'; END IF;
 SELECT * INTO prior FROM advance.vendor_manual_assessments
  WHERE "CompanyId"=p_company AND "GoodsReceiptId"=p_receipt ORDER BY "RevisionNumber" DESC LIMIT 1;
 IF p_supersedes IS DISTINCT FROM prior."Id"
 THEN RAISE EXCEPTION 'The assessment changed; refresh and explicitly supersede the latest retained revision.'; END IF;
 PERFORM set_config('sess.vendor_manual_assessment_write',txid_current()::text,true);
 INSERT INTO advance.vendor_manual_assessments("Id","CompanyId","GoodsReceiptId","GoodsReceiptVersion","VendorId",
  "PurchaseOrderId","RevisionNumber","SupersedesAssessmentId","TechnicalPoints","ResponsePoints","OverallPoints",
  "Reason","ActorEmployeeId","RoleAssignmentId","RoleAssignmentType","RecordedBy")
 VALUES(p_command,p_company,p_receipt,receipt."Version",receipt."VendorId",receipt."PurchaseOrderId",
  coalesce(prior."RevisionNumber",0)+1,p_supersedes,p_technical,p_response,p_overall,btrim(p_reason),p_actor,p_assignment,p_type,p_login);
 RETURN advance.vendor_manual_assessment_json(p_company,p_command);
END $rating$;
REVOKE ALL ON advance.vendor_manual_assessments FROM PUBLIC;
REVOKE ALL ON FUNCTION advance.guard_vendor_manual_assessment() FROM PUBLIC;
REVOKE ALL ON FUNCTION advance.vendor_manual_assessment_json(uuid,uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION advance.vendor_manual_assessment_history(uuid,uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION advance.record_vendor_manual_assessment(uuid,uuid,uuid,bigint,uuid,numeric,numeric,numeric,text,uuid,text,uuid,text,text) FROM PUBLIC;
