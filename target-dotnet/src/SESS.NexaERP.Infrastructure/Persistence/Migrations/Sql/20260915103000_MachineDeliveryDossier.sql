-- Job-order machine DCs deliberately have no ItemId and create no inventory posting.
CREATE TABLE advance.machine_delivery_challans(
 "Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
 "JobOrderId" uuid NOT NULL REFERENCES advance.job_orders("Id"),
 "CustomerId" uuid NOT NULL REFERENCES advance.customers("Id"),
 "CustomerPurchaseOrderId" uuid NOT NULL REFERENCES advance.customer_purchase_orders("Id"),
 "CustomerPoNumber" text NOT NULL,"DcNumber" text NOT NULL CHECK(length(btrim("DcNumber")) BETWEEN 1 AND 100),
 "MaterialType" text NOT NULL DEFAULT 'MACHINE' CHECK("MaterialType"='MACHINE'),
 "Nature" text NOT NULL CHECK("Nature" IN('RETURNABLE','NON_RETURNABLE')),
 "Purpose" text NOT NULL,"DispatchDate" date NOT NULL,"ExpectedReturnDate" date,
 "Destination" text NOT NULL CHECK(length(btrim("Destination")) BETWEEN 1 AND 500),
 "MachineSerial" text NOT NULL,"MachineModel" text NOT NULL,"CustomerName" text NOT NULL,
 "FatReconciliationId" uuid NOT NULL REFERENCES advance.job_order_fat_reconciliations("Id"),
 "RecordedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),
 "ActorEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 "RoleAssignmentId" uuid NOT NULL REFERENCES advance.employee_role_assignments("Id"),"RecordedBy" text NOT NULL,
 UNIQUE("CompanyId","Id"),UNIQUE("CompanyId","DcNumber"),UNIQUE("CompanyId","JobOrderId"),
 CHECK(("Nature"='RETURNABLE' AND "Purpose" IN('DEMO','TRIAL','JOB_WORK','SITE_WORK') AND "ExpectedReturnDate">="DispatchDate")
    OR ("Nature"='NON_RETURNABLE' AND "Purpose"='CUSTOMER_PO_BASED' AND "ExpectedReturnDate" IS NULL)));
CREATE TABLE advance.machine_delivery_signatures(
 "Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL,"DeliveryChallanId" uuid NOT NULL UNIQUE,
 "DeliveredAt" timestamptz NOT NULL,"CustomerSignatory" text NOT NULL CHECK(length(btrim("CustomerSignatory")) BETWEEN 1 AND 200),
 "FileName" text NOT NULL,"ContentType" text NOT NULL CHECK("ContentType" IN('application/pdf','image/jpeg','image/png')),
 "Content" bytea NOT NULL CHECK(octet_length("Content") BETWEEN 1 AND 5242880),
 "ContentSha256" text GENERATED ALWAYS AS (encode(sha256("Content"),'hex')) STORED,
 "RecordedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),"ActorEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 "RoleAssignmentId" uuid NOT NULL REFERENCES advance.employee_role_assignments("Id"),"RecordedBy" text NOT NULL,
 FOREIGN KEY("CompanyId","DeliveryChallanId") REFERENCES advance.machine_delivery_challans("CompanyId","Id"));
CREATE TABLE advance.machine_delivery_bom_entries(
 "CompanyId" uuid NOT NULL,"DeliveryChallanId" uuid NOT NULL,"ActualBomEntryId" uuid NOT NULL REFERENCES advance.actual_bom_entries("Id"),
 PRIMARY KEY("DeliveryChallanId","ActualBomEntryId"),
 FOREIGN KEY("CompanyId","DeliveryChallanId") REFERENCES advance.machine_delivery_challans("CompanyId","Id"));
CREATE FUNCTION advance.guard_machine_delivery_evidence() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $md$
BEGIN
 IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Machine delivery evidence is immutable.'; END IF;
 IF current_setting('sess.machine_delivery_write',true) IS DISTINCT FROM txid_current()::text
 OR current_user IS DISTINCT FROM (SELECT pg_get_userbyid(relowner) FROM pg_class WHERE oid=TG_RELID)
 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Machine delivery requires its governed command.'; END IF;
 RETURN NEW;
END $md$;
DO $md$ DECLARE relation text; BEGIN
 FOREACH relation IN ARRAY ARRAY['machine_delivery_challans','machine_delivery_signatures','machine_delivery_bom_entries'] LOOP
 EXECUTE format('CREATE TRIGGER %I BEFORE INSERT OR UPDATE OR DELETE ON advance.%I FOR EACH ROW EXECUTE FUNCTION advance.guard_machine_delivery_evidence()', 'trg_'||relation,relation);
 END LOOP;
END $md$;
CREATE FUNCTION advance.machine_delivery_json(p_company uuid,p_id uuid) RETURNS jsonb LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $md$
BEGIN
 IF session_user<>'nexa_erp_runtime' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Machine delivery reads require runtime.'; END IF;
 RETURN (SELECT to_jsonb(d)||jsonb_build_object('MachineState',CASE WHEN s."Id" IS NULL THEN 'DISPATCHED' ELSE 'DELIVERED' END,
  'DcState',CASE WHEN s."Id" IS NULL THEN 'DISPATCHED' WHEN d."Nature"='RETURNABLE' THEN 'OUTSTANDING' ELSE 'CLOSED' END,
  'Signature',CASE WHEN s."Id" IS NULL THEN NULL ELSE to_jsonb(s)-'Content' END)
 FROM advance.machine_delivery_challans d LEFT JOIN advance.machine_delivery_signatures s ON s."CompanyId"=d."CompanyId" AND s."DeliveryChallanId"=d."Id"
 WHERE d."CompanyId"=p_company AND d."Id"=p_id);
END $md$;
CREATE FUNCTION advance.record_machine_delivery(p_company uuid,p_command uuid,p_id uuid,p_operation text,p_payload jsonb,p_content bytea,
 p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
RETURNS jsonb LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $md$
DECLARE job advance.job_orders%ROWTYPE; po advance.customer_purchase_orders%ROWTYPE; dc advance.machine_delivery_challans%ROWTYPE;
 target_id uuid; delivered timestamptz; kind text; purpose text; dispatch_date date; due_date date;
BEGIN
 IF session_user<>'nexa_erp_runtime' OR p_role NOT IN('STORES_ASSISTANT','STORES_EXECUTIVE','STORES_MANAGER') OR p_type NOT IN('FULL','TEMPORARY')
 OR p_operation NOT IN('MachineDelivery.Dispatch','MachineDelivery.Sign')
 OR NOT EXISTS(SELECT 1 FROM advance.companies c WHERE c."Id"=p_company AND c."IsActive" AND c."Status"='ACTIVE'
 AND advance.ordinary_command_context_valid(c."Code",p_actor,current_setting('advance.ordinary_identity_issuer',true),current_setting('advance.ordinary_identity_subject',true),p_role))
 OR NOT EXISTS(SELECT 1 FROM advance.command_requests r WHERE r."CommandId"=p_command
 AND p_command=nullif(current_setting('advance.ordinary_command_id',true),'')::uuid AND r."Operation"=p_operation AND r."ResolvedRoleAssignmentId"=p_assignment)
 OR NOT EXISTS(SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,'issue',ARRAY[p_role]) a
 WHERE a."AssignmentId"=p_assignment AND a."AssignmentType"=p_type)
 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Machine delivery requires the exact authorized Stores command.'; END IF;
 IF p_operation='MachineDelivery.Dispatch' THEN
  SELECT * INTO job FROM advance.job_orders WHERE "CompanyId"=p_company AND "Id"=(p_payload->>'jobOrderId')::uuid FOR UPDATE;
  IF NOT FOUND OR job."FatReadinessStatus"<>'READY' OR job."LatestFatReconciliationId" IS NULL
  THEN RAISE EXCEPTION 'Machine dispatch requires this company job to be FAT READY.'; END IF;
  SELECT * INTO po FROM advance.customer_purchase_orders WHERE "CompanyId"=p_company AND "Id"=job."CustomerPurchaseOrderId";
  IF NOT FOUND OR length(btrim(po."CustomerPoNumber"))=0 THEN RAISE EXCEPTION 'Machine dispatch requires its linked customer PO.'; END IF;
  kind:=p_payload->>'nature'; purpose:=p_payload->>'purpose'; dispatch_date:=(p_payload->>'dispatchDate')::date;
  due_date:=(p_payload->>'expectedReturnDate')::date;
  IF kind IS NULL OR purpose IS NULL OR dispatch_date IS NULL OR dispatch_date>current_date
    OR dispatch_date<(job."FatReconciledAt" AT TIME ZONE 'Asia/Kolkata')::date
    OR (kind='RETURNABLE' AND due_date IS NULL)
  THEN RAISE EXCEPTION 'Nature, purpose, dispatch date and required return date must be valid after FAT readiness.'; END IF;
  PERFORM set_config('sess.machine_delivery_write',txid_current()::text,true);
  INSERT INTO advance.machine_delivery_challans("Id","CompanyId","JobOrderId","CustomerId","CustomerPurchaseOrderId","CustomerPoNumber",
   "DcNumber","Nature","Purpose","DispatchDate","ExpectedReturnDate","Destination","MachineSerial","MachineModel","CustomerName",
   "FatReconciliationId","ActorEmployeeId","RoleAssignmentId","RecordedBy")
  VALUES(p_command,p_company,job."Id",po."CustomerId",po."Id",po."CustomerPoNumber",btrim(p_payload->>'dcNumber'),kind,purpose,
   dispatch_date,due_date,btrim(p_payload->>'destination'),job."MachineSerial",job."MachineModel",job."CustomerName",
   job."LatestFatReconciliationId",p_actor,p_assignment,p_login);
  target_id:=p_command;
 ELSE
  SELECT * INTO dc FROM advance.machine_delivery_challans WHERE "CompanyId"=p_company AND "Id"=p_id FOR UPDATE;
  IF NOT FOUND THEN RAISE EXCEPTION 'Machine DC not found in this company.'; END IF;
  SELECT * INTO job FROM advance.job_orders WHERE "CompanyId"=p_company AND "Id"=dc."JobOrderId" FOR UPDATE;
  delivered:=(p_payload->>'deliveredAt')::timestamptz;
  IF job."FatReadinessStatus"<>'READY' OR job."LatestFatReconciliationId" IS DISTINCT FROM dc."FatReconciliationId"
   OR job."MachineSerial" IS DISTINCT FROM dc."MachineSerial"
   OR delivered IS NULL OR delivered>clock_timestamp() OR (delivered AT TIME ZONE 'Asia/Kolkata')::date<dc."DispatchDate"
   OR p_content IS NULL OR octet_length(p_content)=0
  THEN RAISE EXCEPTION 'Signed delivery requires unchanged FAT-ready machine identity, a valid delivery date and retained signature.'; END IF;
  PERFORM set_config('sess.machine_delivery_write',txid_current()::text,true);
  INSERT INTO advance.machine_delivery_signatures("Id","CompanyId","DeliveryChallanId","DeliveredAt","CustomerSignatory","FileName","ContentType","Content","ActorEmployeeId","RoleAssignmentId","RecordedBy")
  VALUES(p_command,p_company,p_id,delivered,btrim(p_payload->>'customerSignatory'),p_payload->>'fileName',p_payload->>'contentType',p_content,p_actor,p_assignment,p_login);
  INSERT INTO advance.machine_delivery_bom_entries
  SELECT p_company,p_id,e."Id" FROM advance.actual_boms b JOIN advance.actual_bom_entries e ON e."CompanyId"=b."CompanyId" AND e."ActualBomId"=b."Id"
  WHERE b."CompanyId"=p_company AND b."JobOrderId"=job."Id";
  IF NOT FOUND THEN RAISE EXCEPTION 'Machine delivery requires its recorded Actual BOM.'; END IF;
  target_id:=p_id;
 END IF;
 RETURN advance.machine_delivery_json(p_company,target_id);
END $md$;
CREATE FUNCTION advance.machine_delivery_signature_content(p_company uuid,p_id uuid)
RETURNS TABLE("FileName" text,"ContentType" text,"Content" bytea) LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $md$
BEGIN
 IF session_user<>'nexa_erp_runtime' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Signature evidence requires runtime.'; END IF;
 RETURN QUERY SELECT s."FileName",s."ContentType",s."Content" FROM advance.machine_delivery_signatures s WHERE s."CompanyId"=p_company AND s."DeliveryChallanId"=p_id;
END $md$;
