CREATE TABLE advance.intercompany_purchase_publications(
 "Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
 "SellerCompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),"RouteId" uuid NOT NULL,
 "RouteApprovalId" uuid NOT NULL REFERENCES advance.intercompany_route_decisions("Id"),
 "PurchaseOrderId" uuid NOT NULL,"PurchaseOrderVersion" bigint NOT NULL CHECK("PurchaseOrderVersion">=0),
 "CommercialHeader" jsonb NOT NULL CHECK(jsonb_typeof("CommercialHeader")='object'),
 "Remarks" text NOT NULL CHECK(length(btrim("Remarks")) BETWEEN 1 AND 2000),
 "ActorEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 "RoleAssignmentId" uuid NOT NULL REFERENCES advance.employee_role_assignments("Id"),
 "RecordedBy" text NOT NULL,"PublishedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),
 UNIQUE("CompanyId","Id"),UNIQUE("CompanyId","RouteId","PurchaseOrderId","PurchaseOrderVersion"),
 FOREIGN KEY("SellerCompanyId","RouteId") REFERENCES advance.intercompany_routes("CompanyId","Id"),
 FOREIGN KEY("CompanyId","PurchaseOrderId") REFERENCES advance.purchase_orders("CompanyId","Id"),
 CHECK("CompanyId"<>"SellerCompanyId"));
CREATE TABLE advance.intercompany_purchase_publication_lines(
 "Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL,"PublicationId" uuid NOT NULL,"PurchaseOrderLineId" uuid NOT NULL,
 "LineNumber" integer NOT NULL CHECK("LineNumber">0),"ItemId" uuid NOT NULL REFERENCES advance.items("Id"),
 "Quantity" numeric(24,6) NOT NULL CHECK("Quantity">0),"UnitRate" numeric(24,6) NOT NULL CHECK("UnitRate">=0),
 "CommercialLine" jsonb NOT NULL CHECK(jsonb_typeof("CommercialLine")='object'),
 UNIQUE("PublicationId","LineNumber"),UNIQUE("PublicationId","PurchaseOrderLineId"),
 FOREIGN KEY("CompanyId","PublicationId") REFERENCES advance.intercompany_purchase_publications("CompanyId","Id"),
 FOREIGN KEY("CompanyId","PurchaseOrderLineId") REFERENCES advance.purchase_order_lines("CompanyId","Id"));
CREATE INDEX ON advance.intercompany_purchase_publications("SellerCompanyId","PublishedAt","Id");
CREATE FUNCTION advance.guard_intercompany_purchase_evidence() RETURNS trigger LANGUAGE plpgsql
 SET search_path=pg_catalog,advance AS $ic$
BEGIN
 IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Published intercompany purchase evidence is immutable.'; END IF;
 IF current_setting('sess.intercompany_purchase_write',true) IS DISTINCT FROM txid_current()::text
 OR current_user IS DISTINCT FROM (SELECT pg_get_userbyid(relowner) FROM pg_class WHERE oid=TG_RELID)
 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Purchase publication requires its governed command.'; END IF;
 RETURN NEW;
END $ic$;
CREATE TRIGGER trg_intercompany_purchase_publications BEFORE INSERT OR UPDATE OR DELETE ON advance.intercompany_purchase_publications
 FOR EACH ROW EXECUTE FUNCTION advance.guard_intercompany_purchase_evidence();
CREATE TRIGGER trg_intercompany_purchase_publication_lines BEFORE INSERT OR UPDATE OR DELETE ON advance.intercompany_purchase_publication_lines
 FOR EACH ROW EXECUTE FUNCTION advance.guard_intercompany_purchase_evidence();

CREATE FUNCTION advance.intercompany_purchase_scope(p_company uuid,p_actor uuid,p_po advance.purchase_orders) RETURNS boolean
 LANGUAGE sql STABLE SET search_path=pg_catalog,advance AS $ic$
 SELECT p_po."CompanyId"=p_company AND EXISTS(SELECT 1 FROM advance.employee_operational_scopes s
  WHERE s."CompanyId"=p_company AND s."OrganizationId"=p_po."OrganizationId" AND s."EmployeeId"=p_actor
   AND s."IsActive" AND s."EffectiveFrom"<=current_date AND (s."EffectiveTo" IS NULL OR s."EffectiveTo">=current_date)
   AND (s."DepartmentId" IS NULL OR s."DepartmentId"=p_po."RequestingDepartmentId")
   AND (s."WarehouseId" IS NULL OR s."WarehouseId"=p_po."DeliveryWarehouseId")
   AND s."RackBinId" IS NULL AND (NOT s."OwnRecordsOnly" OR s."EmployeeId"=p_po."OwnerEmployeeId"));
$ic$;

CREATE FUNCTION advance.intercompany_purchase_json(p_company uuid,p_id uuid,p_actor uuid,p_role text) RETURNS jsonb
 LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $ic$
BEGIN
 IF session_user<>'nexa_erp_runtime' OR p_role IS NULL OR p_role NOT IN('PURCHASE_MANAGER','ACCOUNTS_MANAGER')
 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Published orders require the runtime commercial reader.'; END IF;
 RETURN (SELECT jsonb_build_object('CorrelationId',pub."Id",'CompanyId',p_company,'BuyerCompanyId',pub."CompanyId",
  'SellerCompanyId',pub."SellerCompanyId",'RouteId',pub."RouteId",
  'PurchaseOrderId',CASE WHEN p_company=pub."CompanyId" THEN pub."PurchaseOrderId" END,'PurchaseOrderVersion',pub."PurchaseOrderVersion",
  'Eligibility',CASE WHEN po."Status"='Issued' AND po."IsCurrentVersion" AND po."Version"=pub."PurchaseOrderVersion"
    AND route."EffectiveFrom"<=current_date AND (route."EffectiveTo" IS NULL OR route."EffectiveTo">=current_date)
    AND (SELECT d."Decision" FROM advance.intercompany_route_decisions d WHERE d."RouteId"=route."Id" ORDER BY d."Version" DESC LIMIT 1)='APPROVED'
   THEN 'CURRENT' ELSE 'REFRESH_REQUIRED' END,
  'CommercialOrder',pub."CommercialHeader"||jsonb_build_object('lines',coalesce((SELECT jsonb_agg(
    l."CommercialLine"||jsonb_build_object('publicationLineId',l."Id") ORDER BY l."LineNumber")
    FROM advance.intercompany_purchase_publication_lines l WHERE l."PublicationId"=pub."Id"),'[]'::jsonb)),
  'PublishedAt',pub."PublishedAt")
 FROM advance.intercompany_purchase_publications pub JOIN advance.purchase_orders po ON po."CompanyId"=pub."CompanyId" AND po."Id"=pub."PurchaseOrderId"
 JOIN advance.intercompany_routes route ON route."CompanyId"=pub."SellerCompanyId" AND route."Id"=pub."RouteId"
 WHERE pub."Id"=p_id AND (pub."SellerCompanyId"=p_company OR (pub."CompanyId"=p_company
  AND (p_role='ACCOUNTS_MANAGER' OR advance.intercompany_purchase_scope(p_company,p_actor,po)))));
END $ic$;
CREATE FUNCTION advance.intercompany_purchases_page(p_company uuid,p_actor uuid,p_role text,p_offset integer,p_limit integer) RETURNS jsonb
 LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $ic$
DECLARE result jsonb;
BEGIN
 IF session_user<>'nexa_erp_runtime' OR p_role IS NULL OR p_role NOT IN('PURCHASE_MANAGER','ACCOUNTS_MANAGER')
 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Published orders require the runtime commercial reader.'; END IF;
 IF p_offset<0 OR p_limit NOT BETWEEN 1 AND 200 THEN RAISE EXCEPTION 'Invalid published order page.'; END IF;
 WITH visible AS MATERIALIZED(
  SELECT pub."Id",pub."PublishedAt" FROM advance.intercompany_purchase_publications pub
  JOIN advance.purchase_orders po ON po."CompanyId"=pub."CompanyId" AND po."Id"=pub."PurchaseOrderId"
  WHERE pub."SellerCompanyId"=p_company OR (pub."CompanyId"=p_company
    AND (p_role='ACCOUNTS_MANAGER' OR advance.intercompany_purchase_scope(p_company,p_actor,po)))
 ), page AS(SELECT * FROM visible ORDER BY "PublishedAt" DESC,"Id" OFFSET p_offset LIMIT p_limit)
 SELECT jsonb_build_object('totalCount',(SELECT count(*) FROM visible),'items',coalesce(
  (SELECT jsonb_agg(advance.intercompany_purchase_json(p_company,"Id",p_actor,p_role) ORDER BY "PublishedAt" DESC,"Id") FROM page),'[]'::jsonb)) INTO result;
 RETURN result;
END $ic$;

CREATE FUNCTION advance.intercompany_purchase_options(p_company uuid,p_actor uuid) RETURNS jsonb
 LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $ic$
BEGIN
 IF session_user<>'nexa_erp_runtime' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Publication options require runtime.'; END IF;
 RETURN jsonb_build_object('orders',coalesce((SELECT jsonb_agg(jsonb_build_object(
  'routeId',r."Id",'routeCode',r."RouteCode",'sellerCompanyId',r."CompanyId",'seller',r."Definition"->'sellerCompany',
  'purchaseOrderId',po."Id",'poNumber',po."PoNumber",'version',po."Version",'deliveryWarehouseId',po."DeliveryWarehouseId",
  'currency',po."CurrencyCode",'totalPayableValue',po."TotalPayableValue") ORDER BY po."PoNumber",r."RouteCode")
 FROM advance.purchase_orders po JOIN advance.intercompany_routes r ON r."BuyerCompanyId"=po."CompanyId"
  AND r."SellerVendorId"=po."VendorId" AND r."BuyerWarehouseId"=po."DeliveryWarehouseId"
 WHERE po."CompanyId"=p_company AND po."Status"='Issued' AND po."IsCurrentVersion" AND po."IssuedAt" IS NOT NULL
  AND advance.intercompany_purchase_scope(p_company,p_actor,po)
  AND r."EffectiveFrom"<=current_date AND (r."EffectiveTo" IS NULL OR r."EffectiveTo">=current_date)
  AND (SELECT d."Decision" FROM advance.intercompany_route_decisions d WHERE d."RouteId"=r."Id" ORDER BY d."Version" DESC LIMIT 1)='APPROVED'
  AND NOT EXISTS(SELECT 1 FROM advance.intercompany_purchase_publications pub WHERE pub."CompanyId"=p_company
   AND pub."RouteId"=r."Id" AND pub."PurchaseOrderId"=po."Id" AND pub."PurchaseOrderVersion"=po."Version")),'[]'::jsonb));
END $ic$;

CREATE FUNCTION advance.publish_intercompany_purchase(p_company uuid,p_command uuid,p_route uuid,p_po uuid,p_version bigint,p_remarks text,
 p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text) RETURNS jsonb
 LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $ic$
DECLARE po advance.purchase_orders%ROWTYPE; route advance.intercompany_routes%ROWTYPE; approval advance.intercompany_route_decisions%ROWTYPE;
 definition jsonb;
BEGIN
 IF session_user<>'nexa_erp_runtime' OR p_role<>'PURCHASE_MANAGER' OR p_type NOT IN('FULL','TEMPORARY','SUPPORT')
 OR NOT EXISTS(SELECT 1 FROM advance.companies c WHERE c."Id"=p_company AND c."IsActive" AND c."Status"='ACTIVE'
  AND advance.ordinary_command_context_valid(c."Code",p_actor,current_setting('advance.ordinary_identity_issuer',true),current_setting('advance.ordinary_identity_subject',true),p_role))
 OR NOT EXISTS(SELECT 1 FROM advance.command_requests r WHERE r."CommandId"=p_command
  AND p_command=nullif(current_setting('advance.ordinary_command_id',true),'')::uuid
  AND r."Operation"='IntercompanyPurchase.Publish' AND r."ResolvedRoleAssignmentId"=p_assignment)
 OR NOT EXISTS(SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,'issue',ARRAY[p_role]) a
  WHERE a."AssignmentId"=p_assignment AND a."AssignmentType"=p_type)
 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Publication requires the exact buyer Purchase Manager command.'; END IF;
 SELECT * INTO po FROM advance.purchase_orders WHERE "CompanyId"=p_company AND "Id"=p_po FOR SHARE;
 IF NOT FOUND OR po."Status"<>'Issued' OR NOT po."IsCurrentVersion" OR po."Version"<>p_version OR po."IssuedAt" IS NULL
  OR po."RequiredApprovalStepCount"<=0 OR po."CompletedApprovalStepCount"<>po."RequiredApprovalStepCount"
 THEN RAISE EXCEPTION 'Publication requires the current issued and fully approved buyer PO.'; END IF;
 IF NOT advance.intercompany_purchase_scope(p_company,p_actor,po) THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Buyer PO is outside the publishing employee scope.'; END IF;
 IF (SELECT count(*) FROM advance.purchase_order_history h WHERE h."CompanyId"=p_company AND h."PurchaseOrderId"=p_po
   AND h."RevisionNumber"=po."RevisionNumber" AND h."Action"='Issue' AND h."ToStatus"='Issued')<>1
 THEN RAISE EXCEPTION 'Publication requires the normal PO issue history.'; END IF;
 SELECT * INTO route FROM advance.intercompany_routes WHERE "Id"=p_route AND "BuyerCompanyId"=p_company FOR SHARE;
 IF NOT FOUND OR route."SellerVendorId"<>po."VendorId" OR route."BuyerWarehouseId" IS DISTINCT FROM po."DeliveryWarehouseId"
  OR route."EffectiveFrom">current_date OR (route."EffectiveTo" IS NOT NULL AND route."EffectiveTo"<current_date)
 THEN RAISE EXCEPTION 'Buyer PO vendor, warehouse and current route must agree.'; END IF;
 SELECT * INTO approval FROM advance.intercompany_route_decisions WHERE "RouteId"=p_route ORDER BY "Version" DESC LIMIT 1;
 IF approval."Decision" IS DISTINCT FROM 'APPROVED' THEN RAISE EXCEPTION 'The intercompany route is not approved.'; END IF;
 route."EffectiveFrom":=current_date;
 definition:=advance.validate_intercompany_route(route);
 IF definition IS DISTINCT FROM route."Definition" THEN RAISE EXCEPTION 'Route master identities changed; an approved replacement route is required.'; END IF;
 IF length(btrim(coalesce(p_remarks,''))) NOT BETWEEN 1 AND 2000 THEN RAISE EXCEPTION 'Publication remarks are required.'; END IF;
 PERFORM set_config('sess.intercompany_purchase_write',txid_current()::text,true);
 INSERT INTO advance.intercompany_purchase_publications("Id","CompanyId","SellerCompanyId","RouteId","RouteApprovalId","PurchaseOrderId","PurchaseOrderVersion","CommercialHeader","Remarks","ActorEmployeeId","RoleAssignmentId","RecordedBy")
 VALUES(p_command,p_company,route."CompanyId",p_route,approval."Id",p_po,p_version,
  jsonb_build_object('poNumber',po."PoNumber",'revision',po."RevisionNumber",'issuedAt',po."IssuedAt",
   'buyer',route."Definition"->'buyerCompany','seller',route."Definition"->'sellerCompany',
   'buyerGstin',route."Definition"->'buyerGstin','sellerGstin',route."Definition"->'sellerGstin',
   'currency',po."CurrencyCode",'totalPayableValue',po."TotalPayableValue",'paymentTerms',po."PaymentTermsSnapshot",
   'deliveryTerms',po."DeliveryTermsSnapshot",'warrantyTerms',po."WarrantyTermsSnapshot"),
  btrim(p_remarks),p_actor,p_assignment,p_login);
 INSERT INTO advance.intercompany_purchase_publication_lines("Id","CompanyId","PublicationId","PurchaseOrderLineId","LineNumber","ItemId","Quantity","UnitRate","CommercialLine")
 SELECT gen_random_uuid(),p_company,p_command,l."Id",l."LineNumber",l."ItemId",l."OrderedQuantity",l."UnitRate",
  jsonb_build_object('lineNumber',l."LineNumber",'itemId',l."ItemId",'itemCode',l."ItemCodeSnapshot",'itemName',l."ItemNameSnapshot",
   'quantity',l."OrderedQuantity",'uom',l."UomSnapshot",'unitRate',l."UnitRate",'totalPayableValue',l."TotalPayableValue",
   'commercial',(l."CommercialSnapshotJson"::jsonb)->'result','tax',jsonb_build_object(
    'hsnSacCode',(l."TaxRuleSnapshotJson"::jsonb)->'hsnSacCode','cgstRate',(l."TaxRuleSnapshotJson"::jsonb)->'cgstRate',
    'sgstRate',(l."TaxRuleSnapshotJson"::jsonb)->'sgstRate','igstRate',(l."TaxRuleSnapshotJson"::jsonb)->'igstRate',
    'cessRate',(l."TaxRuleSnapshotJson"::jsonb)->'cessRate'))
 FROM advance.purchase_order_lines l WHERE l."CompanyId"=p_company AND l."PurchaseOrderId"=p_po ORDER BY l."LineNumber";
 IF NOT FOUND THEN RAISE EXCEPTION 'Publication requires the normal PO lines.'; END IF;
 RETURN advance.intercompany_purchase_json(p_company,p_command,p_actor,p_role);
END $ic$;
