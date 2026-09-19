CREATE TABLE advance.intercompany_invoice_evidence (
 "Id" uuid PRIMARY KEY,
 "CompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
 "BuyerCompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
 "PublicationId" uuid NOT NULL REFERENCES advance.intercompany_purchase_publications("Id"),
 "InvoiceNumber" text NOT NULL CHECK(length(btrim("InvoiceNumber")) BETWEEN 1 AND 100 AND "InvoiceNumber" !~ '[[:cntrl:]]'),
 "InvoiceDate" date NOT NULL,
 "FinancialYearStart" date GENERATED ALWAYS AS
  (make_date(extract(year FROM "InvoiceDate")::integer-CASE WHEN extract(month FROM "InvoiceDate")<4 THEN 1 ELSE 0 END,4,1)) STORED,
 "NormalizedInvoiceNumber" text GENERATED ALWAYS AS (upper(btrim("InvoiceNumber"))) STORED,
 "OrderSnapshot" jsonb NOT NULL CHECK(jsonb_typeof("OrderSnapshot")='object'),
 "FileName" text NOT NULL CHECK(length(btrim("FileName")) BETWEEN 1 AND 255 AND "FileName" !~ '[[:cntrl:]]'),
 "ContentType" text NOT NULL CHECK("ContentType" IN('application/pdf','image/jpeg','image/png')),
 "Content" bytea NOT NULL CHECK(octet_length("Content") BETWEEN 1 AND 5242880),
 "Sha256" text NOT NULL CHECK("Sha256" ~ '^[0-9a-f]{64}$'),
 "RecordedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),
 "ActorEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 "RoleAssignmentId" uuid NOT NULL REFERENCES advance.employee_role_assignments("Id"),
 "RecordedBy" text NOT NULL,
 UNIQUE("CompanyId","Id"),
 UNIQUE("CompanyId","FinancialYearStart","NormalizedInvoiceNumber"),
 CHECK("CompanyId"<>"BuyerCompanyId")
);
CREATE INDEX ON advance.intercompany_invoice_evidence("PublicationId","RecordedAt","Id");

CREATE FUNCTION advance.guard_intercompany_invoice_evidence() RETURNS trigger
 LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $ic$
BEGIN
 IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Intercompany invoice evidence is immutable.'; END IF;
 IF current_setting('sess.intercompany_invoice_write',true) IS DISTINCT FROM txid_current()::text
  OR current_user IS DISTINCT FROM (SELECT pg_get_userbyid(relowner) FROM pg_class WHERE oid=TG_RELID)
 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Intercompany invoice evidence requires its governed command.'; END IF;
 IF NEW."Sha256"<>encode(pg_catalog.sha256(NEW."Content"),'hex')
  OR NOT ((NEW."ContentType"='application/pdf' AND substring(NEW."Content",1,5)=decode('255044462d','hex'))
   OR (NEW."ContentType"='image/jpeg' AND substring(NEW."Content",1,3)=decode('ffd8ff','hex'))
   OR (NEW."ContentType"='image/png' AND substring(NEW."Content",1,8)=decode('89504e470d0a1a0a','hex')))
 THEN RAISE EXCEPTION 'Invoice evidence type or hash is invalid.'; END IF;
 RETURN NEW;
END $ic$;
CREATE TRIGGER trg_intercompany_invoice_evidence BEFORE INSERT OR UPDATE OR DELETE
 ON advance.intercompany_invoice_evidence FOR EACH ROW EXECUTE FUNCTION advance.guard_intercompany_invoice_evidence();

CREATE FUNCTION advance.intercompany_invoice_json(p_company uuid,p_id uuid) RETURNS jsonb
 LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $ic$
BEGIN
 IF session_user<>'nexa_erp_runtime' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Invoice reads require runtime.'; END IF;
 RETURN (SELECT jsonb_build_object('Id',i."Id",'CompanyId',p_company,'SellerCompanyId',i."CompanyId",
  'BuyerCompanyId',i."BuyerCompanyId",'CorrelationId',i."PublicationId",'InvoiceNumber',i."InvoiceNumber",
  'InvoiceDate',i."InvoiceDate",'OrderSnapshot',i."OrderSnapshot",'Replayed',false,
  'Evidence',jsonb_build_object('FileName',i."FileName",'ContentType',i."ContentType",'SizeBytes',octet_length(i."Content"),
   'Sha256',i."Sha256",'RecordedAt',i."RecordedAt",'RecordedByEmployeeId',i."ActorEmployeeId"))
 FROM advance.intercompany_invoice_evidence i WHERE i."Id"=p_id AND p_company IN(i."CompanyId",i."BuyerCompanyId"));
END $ic$;

CREATE FUNCTION advance.intercompany_invoice_content(p_company uuid,p_id uuid)
 RETURNS TABLE("FileName" text,"ContentType" text,"Content" bytea,"Sha256" text)
 LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $ic$
BEGIN
 IF session_user<>'nexa_erp_runtime' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Invoice downloads require runtime.'; END IF;
 RETURN QUERY SELECT i."FileName",i."ContentType",i."Content",i."Sha256"
 FROM advance.intercompany_invoice_evidence i WHERE i."Id"=p_id AND p_company IN(i."CompanyId",i."BuyerCompanyId");
END $ic$;

CREATE FUNCTION advance.record_intercompany_invoice(p_company uuid,p_command uuid,p_publication uuid,
 p_number text,p_date date,p_filename text,p_mime text,p_content bytea,
 p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text) RETURNS jsonb
 LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $ic$
DECLARE pub advance.intercompany_purchase_publications%ROWTYPE;
 route advance.intercompany_routes%ROWTYPE; po advance.purchase_orders%ROWTYPE; published jsonb;
BEGIN
 IF session_user<>'nexa_erp_runtime' OR p_role IS DISTINCT FROM 'ACCOUNTS_MANAGER'
  OR p_type IS NULL OR p_type NOT IN('FULL','TEMPORARY','SUPPORT')
  OR NOT EXISTS(SELECT 1 FROM advance.companies c WHERE c."Id"=p_company AND c."IsActive" AND c."Status"='ACTIVE'
   AND advance.ordinary_command_context_valid(c."Code",p_actor,current_setting('advance.ordinary_identity_issuer',true),
    current_setting('advance.ordinary_identity_subject',true),p_role))
  OR NOT EXISTS(SELECT 1 FROM advance.command_requests r WHERE r."CommandId"=p_command
   AND p_command=nullif(current_setting('advance.ordinary_command_id',true),'')::uuid
   AND r."Operation"='IntercompanyInvoice.Record' AND r."ResolvedRoleAssignmentId"=p_assignment)
  OR NOT EXISTS(SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,'create',ARRAY[p_role]) a
   WHERE a."AssignmentId"=p_assignment AND a."AssignmentType"=p_type)
 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Invoice registration requires the exact seller Accounts command.'; END IF;
 IF p_date IS NULL OR p_date IN('infinity'::date,'-infinity'::date)
  OR length(btrim(coalesce(p_number,''))) NOT BETWEEN 1 AND 100
  OR length(btrim(coalesce(p_filename,''))) NOT BETWEEN 1 AND 255
  OR p_content IS NULL OR octet_length(p_content) NOT BETWEEN 1 AND 5242880
 THEN RAISE EXCEPTION 'Invoice date, number and retained evidence are required.'; END IF;
 SELECT * INTO pub FROM advance.intercompany_purchase_publications
  WHERE "Id"=p_publication AND "SellerCompanyId"=p_company FOR SHARE;
 IF NOT FOUND THEN RAISE EXCEPTION 'Published purchase does not belong to this seller.'; END IF;
 SELECT * INTO po FROM advance.purchase_orders WHERE "Id"=pub."PurchaseOrderId" AND "CompanyId"=pub."CompanyId" FOR SHARE;
 SELECT * INTO route FROM advance.intercompany_routes WHERE "Id"=pub."RouteId" AND "CompanyId"=p_company FOR SHARE;
 IF po."Status"<>'Issued' OR NOT po."IsCurrentVersion" OR po."Version"<>pub."PurchaseOrderVersion"
  OR route."EffectiveFrom">current_date OR (route."EffectiveTo" IS NOT NULL AND route."EffectiveTo"<current_date)
  OR (SELECT d."Decision" FROM advance.intercompany_route_decisions d WHERE d."RouteId"=route."Id" ORDER BY d."Version" DESC LIMIT 1) IS DISTINCT FROM 'APPROVED'
 THEN RAISE EXCEPTION 'Invoice requires a current published PO and approved effective route.'; END IF;
 IF advance.validate_intercompany_route(route) IS DISTINCT FROM route."Definition" THEN
  RAISE EXCEPTION 'Route master identities changed; a new approved route and publication are required.';
 END IF;
 published:=advance.intercompany_purchase_json(p_company,p_publication,p_actor,p_role);
 IF published IS NULL OR published->>'Eligibility' IS DISTINCT FROM 'CURRENT'
  OR nullif(published->'CommercialOrder'->>'sellerGstin','') IS NULL
  OR nullif(published->'CommercialOrder'->>'buyerGstin','') IS NULL
 THEN RAISE EXCEPTION 'Published GST identities and current commercial snapshot are required.'; END IF;
 PERFORM set_config('sess.intercompany_invoice_write',txid_current()::text,true);
 INSERT INTO advance.intercompany_invoice_evidence("Id","CompanyId","BuyerCompanyId","PublicationId","InvoiceNumber",
  "InvoiceDate","OrderSnapshot","FileName","ContentType","Content","Sha256","ActorEmployeeId","RoleAssignmentId","RecordedBy")
 VALUES(p_command,p_company,pub."CompanyId",pub."Id",btrim(p_number),p_date,published->'CommercialOrder',
  btrim(p_filename),p_mime,p_content,encode(pg_catalog.sha256(p_content),'hex'),p_actor,p_assignment,p_login);
 RETURN advance.intercompany_invoice_json(p_company,p_command);
END $ic$;

CREATE FUNCTION advance.intercompany_invoices_for_purchase(p_company uuid,p_publication uuid) RETURNS jsonb
 LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $ic$
BEGIN
 IF session_user<>'nexa_erp_runtime' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Invoice reads require runtime.'; END IF;
 RETURN (SELECT coalesce(jsonb_agg(advance.intercompany_invoice_json(p_company,i."Id") ORDER BY i."RecordedAt",i."Id"),'[]'::jsonb)
  FROM advance.intercompany_invoice_evidence i WHERE i."PublicationId"=p_publication
   AND p_company IN(i."CompanyId",i."BuyerCompanyId"));
END $ic$;
