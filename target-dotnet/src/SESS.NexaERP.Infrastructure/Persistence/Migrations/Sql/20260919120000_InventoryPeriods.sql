-- Governed inventory-period commands, installed by GovernedInventoryPeriods.
-- Use the existing financial_periods calendar with PeriodType = INVENTORY.
-- Installation creates no period or other business row.
CREATE TABLE advance.inventory_period_events (
 "Id" uuid PRIMARY KEY,
 "CompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
 "FinancialPeriodId" uuid NOT NULL,
 "Action" text NOT NULL CHECK ("Action" IN ('OPENED','CLOSED')),
 "PeriodVersion" bigint NOT NULL CHECK ("PeriodVersion">=0),
 "ActorEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 "RoleAssignmentId" uuid NOT NULL REFERENCES advance.employee_role_assignments("Id"),
 "RoleAssignmentType" text NOT NULL CHECK ("RoleAssignmentType" IN ('FULL','TEMPORARY')),
 "Reason" text NOT NULL CHECK (length(btrim("Reason")) BETWEEN 1 AND 2000),
 "RecordedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),
 "RecordedBy" text NOT NULL CHECK (length(btrim("RecordedBy"))>0),
 FOREIGN KEY ("CompanyId","FinancialPeriodId") REFERENCES advance.financial_periods("CompanyId","Id"),
 UNIQUE ("FinancialPeriodId","Action"),
 UNIQUE ("FinancialPeriodId","PeriodVersion")
);

CREATE FUNCTION advance.guard_inventory_period_evidence() RETURNS trigger
 LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $period$
BEGIN
 IF TG_TABLE_NAME='inventory_period_events' THEN
  IF TG_OP<>'INSERT' OR current_setting('sess.inventory_period_write',true) IS DISTINCT FROM txid_current()::text
   OR current_user IS DISTINCT FROM (SELECT pg_get_userbyid(relowner) FROM pg_class WHERE oid=TG_RELID)
  THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Inventory period events require a controlled append.'; END IF;
  RETURN NEW;
 END IF;
 IF (TG_OP<>'INSERT' AND upper(btrim(OLD."PeriodType"))='INVENTORY') OR (TG_OP<>'DELETE' AND upper(btrim(NEW."PeriodType"))='INVENTORY') THEN
  IF TG_OP='DELETE' OR current_setting('sess.inventory_period_write',true) IS DISTINCT FROM txid_current()::text
   OR current_user IS DISTINCT FROM (SELECT pg_get_userbyid(relowner) FROM pg_class WHERE oid=TG_RELID)
  THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Inventory periods require the governed CFO command.'; END IF;
  IF TG_OP='UPDATE' AND (OLD."Status"<>'OPEN' OR NEW."Status"<>'CLOSED'
   OR NEW."PeriodType" IS DISTINCT FROM OLD."PeriodType"
   OR NEW."CompanyId" IS DISTINCT FROM OLD."CompanyId" OR NEW."Id" IS DISTINCT FROM OLD."Id"
   OR NEW."StartDate" IS DISTINCT FROM OLD."StartDate" OR NEW."EndDate" IS DISTINCT FROM OLD."EndDate"
   OR NEW."Code" IS DISTINCT FROM OLD."Code" OR NEW."Name" IS DISTINCT FROM OLD."Name"
   OR NEW."IsActive" IS DISTINCT FROM OLD."IsActive" OR NEW."Version"<>OLD."Version"+1)
  THEN RAISE EXCEPTION 'An inventory period may only close once; dates, identity and closed periods are immutable.'; END IF;
 END IF;
 IF TG_OP='DELETE' THEN RETURN OLD; END IF;
 RETURN NEW;
END $period$;
CREATE TRIGGER trg_inventory_period_event_guard BEFORE INSERT OR UPDATE OR DELETE
 ON advance.inventory_period_events FOR EACH ROW EXECUTE FUNCTION advance.guard_inventory_period_evidence();
CREATE TRIGGER trg_inventory_period_guard BEFORE INSERT OR UPDATE OR DELETE
 ON advance.financial_periods FOR EACH ROW EXECUTE FUNCTION advance.guard_inventory_period_evidence();

CREATE FUNCTION advance.require_inventory_period_authority(p_company uuid,p_command uuid,p_actor uuid,
 p_role text,p_assignment uuid,p_type text,p_operation text) RETURNS void
 LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $period$
BEGIN
 IF session_user<>'nexa_erp_runtime' OR p_role IS DISTINCT FROM 'CHIEF_FINANCIAL_OFFICER'
  OR p_type IS NULL OR p_type NOT IN ('FULL','TEMPORARY')
  OR p_operation NOT IN ('InventoryPeriod.Open','InventoryPeriod.Close')
  OR NOT EXISTS(SELECT 1 FROM advance.companies c WHERE c."Id"=p_company AND c."IsActive" AND c."Status"='ACTIVE'
   AND advance.ordinary_command_context_valid(c."Code",p_actor,current_setting('advance.ordinary_identity_issuer',true),
    current_setting('advance.ordinary_identity_subject',true),p_role))
  OR NOT EXISTS(SELECT 1 FROM advance.command_requests r WHERE r."CommandId"=p_command
   AND p_command=nullif(current_setting('advance.ordinary_command_id',true),'')::uuid
   AND r."Operation"=p_operation AND r."ResolvedRoleAssignmentId"=p_assignment)
  OR NOT EXISTS(SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,'approve',ARRAY[p_role]) a
   WHERE a."AssignmentId"=p_assignment AND a."AssignmentType"=p_type)
 THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Inventory period decisions require the exact governed CFO command.'; END IF;
END $period$;

CREATE FUNCTION advance.open_inventory_period(p_company uuid,p_command uuid,p_code text,p_name text,
 p_from date,p_to date,p_reason text,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
 RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $period$
BEGIN
 PERFORM advance.require_inventory_period_authority(p_company,p_command,p_actor,p_role,p_assignment,p_type,'InventoryPeriod.Open');
 IF p_from IS NULL OR p_to IS NULL OR NOT isfinite(p_from) OR NOT isfinite(p_to) OR p_to<p_from
  OR length(btrim(coalesce(p_code,''))) NOT BETWEEN 1 AND 30
  OR length(btrim(coalesce(p_name,''))) NOT BETWEEN 1 AND 150
  OR length(btrim(coalesce(p_reason,''))) NOT BETWEEN 1 AND 2000
  OR length(btrim(coalesce(p_login,'')))=0
 THEN RAISE EXCEPTION 'A finite inventory period, code, name, decision reason and actor are required.'; END IF;
 -- Serializes overlapping-date admission, including a second opening at another code.
 PERFORM pg_advisory_xact_lock(hashtextextended('INVENTORY_PERIOD:'||p_company,0));
 IF EXISTS(SELECT 1 FROM advance.financial_periods f WHERE f."CompanyId"=p_company AND f."PeriodType"='INVENTORY'
  AND f."StartDate"<=p_to AND f."EndDate">=p_from)
 THEN RAISE EXCEPTION 'Inventory periods cannot overlap, including closed or inactive periods.'; END IF;
 PERFORM set_config('sess.inventory_period_write',txid_current()::text,true);
 INSERT INTO advance.financial_periods("Id","CompanyId","Code","Name","PeriodType","StartDate","EndDate","Status",
  "IsActive","CreatedAt","CreatedBy","Version")
 VALUES(p_command,p_company,btrim(p_code),btrim(p_name),'INVENTORY',p_from,p_to,'OPEN',true,clock_timestamp(),p_login,0);
 INSERT INTO advance.inventory_period_events("Id","CompanyId","FinancialPeriodId","Action","PeriodVersion",
  "ActorEmployeeId","RoleAssignmentId","RoleAssignmentType","Reason","RecordedBy")
 VALUES(p_command,p_company,p_command,'OPENED',0,p_actor,p_assignment,p_type,btrim(p_reason),p_login);
 RETURN p_command;
END $period$;

CREATE FUNCTION advance.close_inventory_period(p_company uuid,p_command uuid,p_period uuid,p_version bigint,p_reason text,
 p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
 RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $period$
DECLARE period_row advance.financial_periods%ROWTYPE;
BEGIN
 PERFORM advance.require_inventory_period_authority(p_company,p_command,p_actor,p_role,p_assignment,p_type,'InventoryPeriod.Close');
 IF length(btrim(coalesce(p_reason,''))) NOT BETWEEN 1 AND 2000 OR length(btrim(coalesce(p_login,'')))=0
 THEN RAISE EXCEPTION 'Closing requires a retained decision reason and actor.'; END IF;
 -- Posting must hold the same period row until its atomic stock/FIFO transaction commits.
 SELECT * INTO period_row FROM advance.financial_periods
  WHERE "CompanyId"=p_company AND "Id"=p_period AND "PeriodType"='INVENTORY' FOR UPDATE;
 IF NOT FOUND OR period_row."Status"<>'OPEN' OR NOT period_row."IsActive"
  OR p_version IS NULL OR period_row."Version"<>p_version
 THEN RAISE EXCEPTION 'The selected open inventory period is absent or its version has changed.'; END IF;
 PERFORM set_config('sess.inventory_period_write',txid_current()::text,true);
 UPDATE advance.financial_periods SET "Status"='CLOSED',"ClosedAt"=clock_timestamp(),
  "UpdatedAt"=clock_timestamp(),"UpdatedBy"=p_login,"Version"="Version"+1
 WHERE "CompanyId"=p_company AND "Id"=p_period;
 INSERT INTO advance.inventory_period_events("Id","CompanyId","FinancialPeriodId","Action","PeriodVersion",
  "ActorEmployeeId","RoleAssignmentId","RoleAssignmentType","Reason","RecordedBy")
 VALUES(p_command,p_company,p_period,'CLOSED',p_version+1,p_actor,p_assignment,p_type,btrim(p_reason),p_login);
 RETURN p_period;
END $period$;

-- The migration installs the shared versioned private-table/function ACL contract
-- after function creation; only governed entry points receive runtime grants.
REVOKE ALL ON advance.inventory_period_events FROM PUBLIC;
REVOKE ALL ON FUNCTION advance.require_inventory_period_authority(uuid,uuid,uuid,text,uuid,text,text) FROM PUBLIC;
REVOKE ALL ON FUNCTION advance.open_inventory_period(uuid,uuid,text,text,date,date,text,uuid,text,uuid,text,text) FROM PUBLIC;
REVOKE ALL ON FUNCTION advance.close_inventory_period(uuid,uuid,uuid,bigint,text,uuid,text,uuid,text,text) FROM PUBLIC;

CREATE FUNCTION advance.inventory_period_json(p_company uuid,p_id uuid) RETURNS jsonb
 LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $period$
BEGIN
 IF session_user<>'nexa_erp_runtime' THEN
  RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Inventory period reads require runtime.'; END IF;
 RETURN (SELECT jsonb_build_object('Id',f."Id",'Code',f."Code",'Name',f."Name",
  'StartDate',f."StartDate",'EndDate',f."EndDate",'Status',f."Status",'Version',f."Version",
  'CreatedAt',f."CreatedAt",'CreatedBy',f."CreatedBy",'ClosedAt',f."ClosedAt",
  'Decisions',coalesce((SELECT jsonb_agg(to_jsonb(e) ORDER BY e."PeriodVersion")
   FROM advance.inventory_period_events e WHERE e."CompanyId"=f."CompanyId" AND e."FinancialPeriodId"=f."Id"),'[]'::jsonb))
 FROM advance.financial_periods f WHERE f."CompanyId"=p_company AND f."Id"=p_id AND f."PeriodType"='INVENTORY');
END $period$;
CREATE FUNCTION advance.inventory_periods_json(p_company uuid) RETURNS jsonb
 LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $period$
BEGIN
 IF session_user<>'nexa_erp_runtime' THEN
  RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Inventory period reads require runtime.'; END IF;
 RETURN (SELECT coalesce(jsonb_agg(advance.inventory_period_json(p_company,f."Id")
  ORDER BY f."StartDate" DESC,f."Id"),'[]'::jsonb) FROM advance.financial_periods f
  WHERE f."CompanyId"=p_company AND f."PeriodType"='INVENTORY');
END $period$;
REVOKE ALL ON FUNCTION advance.inventory_period_json(uuid,uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION advance.inventory_periods_json(uuid) FROM PUBLIC;
