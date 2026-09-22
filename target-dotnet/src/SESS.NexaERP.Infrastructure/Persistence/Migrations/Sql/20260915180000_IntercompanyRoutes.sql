CREATE TABLE advance.intercompany_routes(
 "Id" uuid PRIMARY KEY,
 "CompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
 "BuyerCompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
 "RouteCode" text NOT NULL CHECK(length(btrim("RouteCode")) BETWEEN 1 AND 50),
 "SellerSiteId" uuid NOT NULL REFERENCES advance.company_sites("Id"),
 "BuyerSiteId" uuid NOT NULL REFERENCES advance.company_sites("Id"),
 "SellerWarehouseId" uuid NOT NULL REFERENCES advance.warehouses("Id"),
 "BuyerWarehouseId" uuid NOT NULL REFERENCES advance.warehouses("Id"),
 "SellerGstRegistrationId" uuid NOT NULL REFERENCES advance.company_gst_registrations("Id"),
 "BuyerGstRegistrationId" uuid NOT NULL REFERENCES advance.company_gst_registrations("Id"),
 "SellerVendorId" uuid NOT NULL REFERENCES advance.vendors("Id"),
 "BuyerCustomerId" uuid NOT NULL REFERENCES advance.customers("Id"),
 "EffectiveFrom" date NOT NULL,"EffectiveTo" date,
 "Definition" jsonb NOT NULL CHECK(jsonb_typeof("Definition")='object'),
 "Remarks" text NOT NULL CHECK(length(btrim("Remarks")) BETWEEN 1 AND 2000),
 "ActorEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 "RoleAssignmentId" uuid NOT NULL REFERENCES advance.employee_role_assignments("Id"),
 "RecordedBy" text NOT NULL,"RecordedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),
 UNIQUE("CompanyId","Id"),UNIQUE("CompanyId","RouteCode"),
 CHECK("CompanyId"<>"BuyerCompanyId"),
 CHECK("EffectiveTo" IS NULL OR "EffectiveTo">="EffectiveFrom"));
CREATE TABLE advance.intercompany_route_decisions(
 "Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL,"RouteId" uuid NOT NULL,
 "Version" bigint NOT NULL CHECK("Version" IN(2,3)),
 "Decision" text NOT NULL CHECK("Decision" IN('APPROVED','REJECTED','REVOKED')),
 "Remarks" text NOT NULL CHECK(length(btrim("Remarks")) BETWEEN 1 AND 2000),
 "ActorEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 "RoleAssignmentId" uuid NOT NULL REFERENCES advance.employee_role_assignments("Id"),
 "ActorRoleCode" text NOT NULL,"RecordedBy" text NOT NULL,"RecordedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),
 UNIQUE("RouteId","Version"),
 FOREIGN KEY("CompanyId","RouteId") REFERENCES advance.intercompany_routes("CompanyId","Id"),
 CHECK(("Version"=2 AND "Decision" IN('APPROVED','REJECTED')) OR ("Version"=3 AND "Decision"='REVOKED')));
CREATE INDEX ON advance.intercompany_routes("BuyerCompanyId","EffectiveFrom");
CREATE FUNCTION advance.guard_intercompany_route_evidence() RETURNS trigger LANGUAGE plpgsql
 SET search_path=pg_catalog,advance AS $ic$
BEGIN
 IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Intercompany route definitions and decisions are immutable.'; END IF;
 IF current_setting('sess.intercompany_route_write',true) IS DISTINCT FROM txid_current()::text
 OR current_user IS DISTINCT FROM (SELECT pg_get_userbyid(relowner) FROM pg_class WHERE oid=TG_RELID)
 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Intercompany routes require the governed command.'; END IF;
 RETURN NEW;
END $ic$;
CREATE TRIGGER trg_intercompany_routes BEFORE INSERT OR UPDATE OR DELETE ON advance.intercompany_routes
 FOR EACH ROW EXECUTE FUNCTION advance.guard_intercompany_route_evidence();
CREATE TRIGGER trg_intercompany_route_decisions BEFORE INSERT OR UPDATE OR DELETE ON advance.intercompany_route_decisions
 FOR EACH ROW EXECUTE FUNCTION advance.guard_intercompany_route_evidence();

-- Revalidate exact master identities at proposal/approval and, later, before dispatch.
CREATE FUNCTION advance.validate_intercompany_route(p_route advance.intercompany_routes) RETURNS jsonb
 LANGUAGE plpgsql STABLE SET search_path=pg_catalog,advance AS $ic$
DECLARE seller advance.companies%ROWTYPE; buyer advance.companies%ROWTYPE;
 seller_site advance.company_sites%ROWTYPE; buyer_site advance.company_sites%ROWTYPE;
 seller_gst advance.company_gst_registrations%ROWTYPE; buyer_gst advance.company_gst_registrations%ROWTYPE;
 seller_vendor advance.vendors%ROWTYPE; buyer_customer advance.customers%ROWTYPE;
 seller_store advance.warehouses%ROWTYPE; buyer_store advance.warehouses%ROWTYPE;
BEGIN
 IF p_route."CompanyId"=p_route."BuyerCompanyId" THEN RAISE EXCEPTION 'Intercompany sale requires two distinct companies.'; END IF;
 SELECT * INTO seller FROM advance.companies WHERE "Id"=p_route."CompanyId" AND "IsActive" AND "Status"='ACTIVE';
 SELECT * INTO buyer FROM advance.companies WHERE "Id"=p_route."BuyerCompanyId" AND "IsActive" AND "Status"='ACTIVE';
 IF seller."Id" IS NULL OR buyer."Id" IS NULL THEN RAISE EXCEPTION 'Both route companies must be active.'; END IF;
 SELECT * INTO seller_site FROM advance.company_sites WHERE "Id"=p_route."SellerSiteId" AND "CompanyId"=seller."Id" AND "IsActive";
 SELECT * INTO buyer_site FROM advance.company_sites WHERE "Id"=p_route."BuyerSiteId" AND "CompanyId"=buyer."Id" AND "IsActive";
 IF seller_site."Id" IS NULL OR buyer_site."Id" IS NULL THEN RAISE EXCEPTION 'Route sites must belong to their respective active companies.'; END IF;
 SELECT * INTO seller_store FROM advance.warehouses WHERE "Id"=p_route."SellerWarehouseId" AND "CompanyId"=seller."Id" AND "IsActive" AND "ApprovalStatus"='Approved';
 SELECT * INTO buyer_store FROM advance.warehouses WHERE "Id"=p_route."BuyerWarehouseId" AND "CompanyId"=buyer."Id" AND "IsActive" AND "ApprovalStatus"='Approved';
 IF seller_store."Id" IS NULL OR buyer_store."Id" IS NULL THEN RAISE EXCEPTION 'Route warehouses must be approved in their respective companies.'; END IF;
 SELECT * INTO seller_gst FROM advance.company_gst_registrations WHERE "Id"=p_route."SellerGstRegistrationId" AND "CompanyId"=seller."Id" AND "IsActive"
  AND ("CompanySiteId" IS NULL OR "CompanySiteId"=seller_site."Id") AND "StateCode"=seller_site."StateCode"
  AND "EffectiveFrom"<=p_route."EffectiveFrom" AND ("EffectiveTo" IS NULL OR "EffectiveTo">=p_route."EffectiveFrom");
 SELECT * INTO buyer_gst FROM advance.company_gst_registrations WHERE "Id"=p_route."BuyerGstRegistrationId" AND "CompanyId"=buyer."Id" AND "IsActive"
  AND ("CompanySiteId" IS NULL OR "CompanySiteId"=buyer_site."Id") AND "StateCode"=buyer_site."StateCode"
  AND "EffectiveFrom"<=p_route."EffectiveFrom" AND ("EffectiveTo" IS NULL OR "EffectiveTo">=p_route."EffectiveFrom");
 IF seller_gst."Id" IS NULL OR buyer_gst."Id" IS NULL OR seller_gst."Gstin"=buyer_gst."Gstin"
 THEN RAISE EXCEPTION 'Route requires distinct effective GST registrations matching the selected sites.'; END IF;
 SELECT * INTO seller_vendor FROM advance.vendors WHERE "Id"=p_route."SellerVendorId" AND "IsActive" AND "ApprovalStatus"='Approved'
  AND upper(btrim("GstNumber"))=upper(btrim(seller_gst."Gstin"));
 SELECT * INTO buyer_customer FROM advance.customers WHERE "Id"=p_route."BuyerCustomerId" AND "IsActive" AND "ApprovalStatus"='Approved'
  AND upper(btrim("GstNumber"))=upper(btrim(buyer_gst."Gstin"));
 IF seller_vendor."Id" IS NULL OR buyer_customer."Id" IS NULL
 THEN RAISE EXCEPTION 'Vendor and customer must identify the actual seller and buyer GST registrations.'; END IF;
 IF NOT EXISTS(SELECT 1 FROM advance.vendor_company_relationships WHERE "CompanyId"=buyer."Id" AND "VendorId"=seller_vendor."Id"
  AND "IsActive" AND "RelationshipStatus"='ACTIVE' AND "ApprovedByEmployeeId" IS NOT NULL AND "ApprovedAt" IS NOT NULL
  AND "EffectiveFrom"<=p_route."EffectiveFrom" AND ("EffectiveTo" IS NULL OR "EffectiveTo">=p_route."EffectiveFrom"))
 OR NOT EXISTS(SELECT 1 FROM advance.customer_company_relationships WHERE "CompanyId"=seller."Id" AND "CustomerId"=buyer_customer."Id"
  AND "IsActive" AND "RelationshipStatus"='ACTIVE' AND "ApprovedByEmployeeId" IS NOT NULL AND "ApprovedAt" IS NOT NULL
  AND "EffectiveFrom"<=p_route."EffectiveFrom" AND ("EffectiveTo" IS NULL OR "EffectiveTo">=p_route."EffectiveFrom"))
 THEN RAISE EXCEPTION 'Both companies require approved effective commercial relationships.'; END IF;
 RETURN jsonb_build_object('sellerCompany',jsonb_build_object('id',seller."Id",'code',seller."Code",'legalName',seller."LegalName"),
  'buyerCompany',jsonb_build_object('id',buyer."Id",'code',buyer."Code",'legalName',buyer."LegalName"),
  'sellerSite',jsonb_build_object('id',seller_site."Id",'code',seller_site."Code",'stateCode',seller_site."StateCode"),
  'buyerSite',jsonb_build_object('id',buyer_site."Id",'code',buyer_site."Code",'stateCode',buyer_site."StateCode"),
  'sellerWarehouse',jsonb_build_object('id',seller_store."Id",'code',seller_store."WarehouseCode"),
  'buyerWarehouse',jsonb_build_object('id',buyer_store."Id",'code',buyer_store."WarehouseCode"),
  'sellerGstin',seller_gst."Gstin",'buyerGstin',buyer_gst."Gstin",
  'sellerVendorCode',seller_vendor."VendorCode",'buyerCustomerCode',buyer_customer."CustomerCode");
END $ic$;

CREATE FUNCTION advance.intercompany_route_json(p_company uuid,p_id uuid) RETURNS jsonb
 LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $ic$
BEGIN
 IF session_user<>'nexa_erp_runtime' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Intercompany route reads require runtime.'; END IF;
 RETURN (SELECT to_jsonb(r)||jsonb_build_object('Status',coalesce(last_decision."Decision",'PROPOSED'),
  'Version',coalesce(last_decision."Version",1),'History',coalesce((SELECT jsonb_agg(to_jsonb(d) ORDER BY d."Version")
   FROM advance.intercompany_route_decisions d WHERE d."CompanyId"=p_company AND d."RouteId"=r."Id"),'[]'::jsonb))
  FROM advance.intercompany_routes r LEFT JOIN LATERAL(SELECT d."Decision",d."Version"
   FROM advance.intercompany_route_decisions d WHERE d."CompanyId"=p_company AND d."RouteId"=r."Id" ORDER BY d."Version" DESC LIMIT 1) last_decision ON true
  WHERE r."CompanyId"=p_company AND r."Id"=p_id);
END $ic$;
CREATE FUNCTION advance.intercompany_routes_page(p_company uuid,p_offset integer,p_limit integer) RETURNS jsonb
 LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $ic$
BEGIN
 IF session_user<>'nexa_erp_runtime' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Intercompany route reads require runtime.'; END IF;
 IF p_offset<0 OR p_limit NOT BETWEEN 1 AND 200 THEN RAISE EXCEPTION 'Invalid intercompany route page.'; END IF;
 RETURN jsonb_build_object('totalCount',(SELECT count(*) FROM advance.intercompany_routes WHERE "CompanyId"=p_company),
 'items',coalesce((SELECT jsonb_agg(advance.intercompany_route_json(p_company,r."Id") ORDER BY r."RouteCode",r."Id")
  FROM (SELECT "Id","RouteCode" FROM advance.intercompany_routes WHERE "CompanyId"=p_company ORDER BY "RouteCode","Id" OFFSET p_offset LIMIT p_limit) r),'[]'::jsonb));
END $ic$;

CREATE FUNCTION advance.record_intercompany_route(p_company uuid,p_command uuid,p_id uuid,p_operation text,p_payload jsonb,
 p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text) RETURNS jsonb
 LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $ic$
DECLARE route advance.intercompany_routes%ROWTYPE; last_decision advance.intercompany_route_decisions%ROWTYPE;
 action text; decision text; definition jsonb; expected_version bigint; next_version bigint;
BEGIN
 decision:=p_payload->>'decision';
 action:=CASE WHEN p_operation='IntercompanyRoute.Propose' THEN 'create' WHEN decision='APPROVED' THEN 'approve'
  WHEN decision='REJECTED' THEN 'reject' WHEN decision='REVOKED' THEN 'deactivate' END;
 IF session_user<>'nexa_erp_runtime' OR p_type NOT IN('FULL','TEMPORARY','SUPPORT') OR action IS NULL
 OR (p_operation='IntercompanyRoute.Propose' AND p_role<>'ACCOUNTS_MANAGER')
 OR (p_operation='IntercompanyRoute.Decide' AND p_role NOT IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))
 OR p_operation NOT IN('IntercompanyRoute.Propose','IntercompanyRoute.Decide')
 OR NOT EXISTS(SELECT 1 FROM advance.companies c WHERE c."Id"=p_company AND c."IsActive" AND c."Status"='ACTIVE'
  AND advance.ordinary_command_context_valid(c."Code",p_actor,current_setting('advance.ordinary_identity_issuer',true),current_setting('advance.ordinary_identity_subject',true),p_role))
 OR NOT EXISTS(SELECT 1 FROM advance.command_requests r WHERE r."CommandId"=p_command
  AND p_command=nullif(current_setting('advance.ordinary_command_id',true),'')::uuid
  AND r."Operation"=p_operation AND r."ResolvedRoleAssignmentId"=p_assignment)
 OR NOT EXISTS(SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,action,ARRAY[p_role]) a
  WHERE a."AssignmentId"=p_assignment AND a."AssignmentType"=p_type)
 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Intercompany route requires the exact authorized company command.'; END IF;
 IF length(btrim(coalesce(p_payload->>'remarks',''))) NOT BETWEEN 1 AND 2000 THEN RAISE EXCEPTION 'Route remarks are required.'; END IF;
 PERFORM set_config('sess.intercompany_route_write',txid_current()::text,true);
 IF p_operation='IntercompanyRoute.Propose' THEN
  route:=jsonb_populate_record(NULL::advance.intercompany_routes,jsonb_build_object(
   'Id',p_command,'CompanyId',p_company,'BuyerCompanyId',p_payload->'buyerCompanyId',
   'RouteCode',upper(btrim(p_payload->>'routeCode')),
   'SellerSiteId',p_payload->'sellerSiteId','BuyerSiteId',p_payload->'buyerSiteId',
   'SellerWarehouseId',p_payload->'sellerWarehouseId','BuyerWarehouseId',p_payload->'buyerWarehouseId',
   'SellerGstRegistrationId',p_payload->'sellerGstRegistrationId','BuyerGstRegistrationId',p_payload->'buyerGstRegistrationId',
   'SellerVendorId',p_payload->'sellerVendorId','BuyerCustomerId',p_payload->'buyerCustomerId',
   'EffectiveFrom',p_payload->'effectiveFrom','EffectiveTo',p_payload->'effectiveTo',
   'Remarks',btrim(p_payload->>'remarks'),'ActorEmployeeId',p_actor,'RoleAssignmentId',p_assignment,'RecordedBy',p_login,'RecordedAt',clock_timestamp()));
  route."Definition":=advance.validate_intercompany_route(route);
  INSERT INTO advance.intercompany_routes SELECT route.*;
  p_id:=p_command;
 ELSE
  SELECT * INTO route FROM advance.intercompany_routes WHERE "CompanyId"=p_company AND "Id"=p_id FOR UPDATE;
  IF NOT FOUND THEN RAISE EXCEPTION 'Route not found in the selected company.'; END IF;
  SELECT * INTO last_decision FROM advance.intercompany_route_decisions WHERE "CompanyId"=p_company AND "RouteId"=p_id ORDER BY "Version" DESC LIMIT 1;
  expected_version:=(p_payload->>'version')::bigint;
  IF expected_version IS DISTINCT FROM coalesce(last_decision."Version",1) THEN RAISE EXCEPTION 'Route Version is stale.'; END IF;
  IF decision IN('APPROVED','REJECTED') THEN
   IF last_decision."Id" IS NOT NULL THEN RAISE EXCEPTION 'Only a proposed route can be approved or rejected.'; END IF;
   IF route."ActorEmployeeId"=p_actor THEN RAISE EXCEPTION 'The route proposer cannot approve or reject their own definition.'; END IF;
   next_version:=2;
  ELSIF decision='REVOKED' THEN
   IF last_decision."Decision" IS DISTINCT FROM 'APPROVED' THEN RAISE EXCEPTION 'Only an approved route can be revoked.'; END IF;
   next_version:=3;
  ELSE RAISE EXCEPTION 'Invalid route decision.';
  END IF;
  IF decision='APPROVED' THEN
   definition:=advance.validate_intercompany_route(route);
   IF definition IS DISTINCT FROM route."Definition" THEN RAISE EXCEPTION 'Route master identities changed; reject and propose a fresh definition.'; END IF;
   PERFORM pg_advisory_xact_lock(hashtextextended('INTERCOMPANY:ROUTE:'||p_company||':'||route."BuyerCompanyId"||':'||route."SellerWarehouseId"||':'||route."BuyerWarehouseId",0));
   IF EXISTS(SELECT 1 FROM advance.intercompany_routes other
    WHERE other."CompanyId"=p_company AND other."BuyerCompanyId"=route."BuyerCompanyId" AND other."Id"<>p_id
     AND other."SellerWarehouseId"=route."SellerWarehouseId" AND other."BuyerWarehouseId"=route."BuyerWarehouseId"
     AND daterange(other."EffectiveFrom",other."EffectiveTo",'[]') && daterange(route."EffectiveFrom",route."EffectiveTo",'[]')
     AND (SELECT d."Decision" FROM advance.intercompany_route_decisions d WHERE d."RouteId"=other."Id" ORDER BY d."Version" DESC LIMIT 1)='APPROVED')
   THEN RAISE EXCEPTION 'An approved route already covers this warehouse pair and date range.'; END IF;
  END IF;
  INSERT INTO advance.intercompany_route_decisions("Id","CompanyId","RouteId","Version","Decision","Remarks","ActorEmployeeId","RoleAssignmentId","ActorRoleCode","RecordedBy")
   VALUES(p_command,p_company,p_id,next_version,decision,btrim(p_payload->>'remarks'),p_actor,p_assignment,p_role,p_login);
 END IF;
 RETURN advance.intercompany_route_json(p_company,p_id);
END $ic$;

-- Configuration lookups expose active master identifiers, never another company's business documents.
CREATE FUNCTION advance.intercompany_route_options(p_company uuid) RETURNS jsonb
 LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $ic$
BEGIN
 IF session_user<>'nexa_erp_runtime' OR NOT EXISTS(SELECT 1 FROM advance.companies WHERE "Id"=p_company AND "IsActive" AND "Status"='ACTIVE')
 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Route options require an active company and runtime.'; END IF;
 RETURN jsonb_build_object(
  'companies',coalesce((SELECT jsonb_agg(jsonb_build_object('id',"Id",'code',"Code",'legalName',"LegalName") ORDER BY "Code")
   FROM advance.companies WHERE "IsActive" AND "Status"='ACTIVE'),'[]'::jsonb),
  'sites',coalesce((SELECT jsonb_agg(jsonb_build_object('id',s."Id",'companyId',s."CompanyId",'code',s."Code",'name',s."Name",'stateCode',s."StateCode") ORDER BY s."CompanyId",s."Code")
   FROM advance.company_sites s JOIN advance.companies c ON c."Id"=s."CompanyId" AND c."IsActive" AND c."Status"='ACTIVE' WHERE s."IsActive"),'[]'::jsonb),
  'warehouses',coalesce((SELECT jsonb_agg(jsonb_build_object('id',w."Id",'companyId',w."CompanyId",'code',w."WarehouseCode",'name',w."Name") ORDER BY w."CompanyId",w."WarehouseCode")
   FROM advance.warehouses w JOIN advance.companies c ON c."Id"=w."CompanyId" AND c."IsActive" AND c."Status"='ACTIVE' WHERE w."IsActive" AND w."ApprovalStatus"='Approved'),'[]'::jsonb),
  'gstRegistrations',coalesce((SELECT jsonb_agg(jsonb_build_object('id',g."Id",'companyId',g."CompanyId",'siteId',g."CompanySiteId",'gstin',g."Gstin",'stateCode',g."StateCode",'effectiveFrom',g."EffectiveFrom",'effectiveTo',g."EffectiveTo") ORDER BY g."CompanyId",g."Gstin")
   FROM advance.company_gst_registrations g JOIN advance.companies c ON c."Id"=g."CompanyId" AND c."IsActive" AND c."Status"='ACTIVE' WHERE g."IsActive"),'[]'::jsonb),
  'vendors',coalesce((SELECT jsonb_agg(jsonb_build_object('id',v."Id",'code',v."VendorCode",'name',v."Name",'gstin',v."GstNumber") ORDER BY v."VendorCode")
   FROM advance.vendors v WHERE v."IsActive" AND v."ApprovalStatus"='Approved'
    AND EXISTS(SELECT 1 FROM advance.company_gst_registrations g WHERE g."CompanyId"=p_company AND g."IsActive" AND upper(btrim(g."Gstin"))=upper(btrim(v."GstNumber")))),'[]'::jsonb),
  'customers',coalesce((SELECT jsonb_agg(jsonb_build_object('id',c."Id",'code',c."CustomerCode",'name',c."Name",'gstin',c."GstNumber") ORDER BY c."CustomerCode")
   FROM advance.customers c WHERE c."IsActive" AND c."ApprovalStatus"='Approved'
    AND EXISTS(SELECT 1 FROM advance.customer_company_relationships r WHERE r."CompanyId"=p_company AND r."CustomerId"=c."Id" AND r."IsActive" AND r."RelationshipStatus"='ACTIVE')
    AND EXISTS(SELECT 1 FROM advance.company_gst_registrations g WHERE g."CompanyId"<>p_company AND g."IsActive" AND upper(btrim(g."Gstin"))=upper(btrim(c."GstNumber")))),'[]'::jsonb));
END $ic$;
