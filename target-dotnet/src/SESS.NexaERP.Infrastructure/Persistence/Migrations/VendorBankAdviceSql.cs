namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class VendorBankAdviceSql
{
    internal const string Create = """
CREATE TABLE advance.vendor_bank_advices(
 "Id" uuid PRIMARY KEY,
 "CompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
 "VendorId" uuid NOT NULL REFERENCES advance.vendors("Id"),
 "FileName" varchar(255) NOT NULL CHECK(btrim("FileName")<>''),
 "ContentType" varchar(100) NOT NULL CHECK("ContentType" IN('application/pdf','image/jpeg','image/png')),
 "Content" bytea NOT NULL CHECK(octet_length("Content") BETWEEN 1 AND 5242880),
 "SizeBytes" integer GENERATED ALWAYS AS (octet_length("Content")) STORED,
 "ContentSha256" varchar(64) GENERATED ALWAYS AS (encode(sha256("Content"),'hex')) STORED,
 "EvidenceObjectKey" varchar(48) GENERATED ALWAYS AS ('bank-advice:'||"Id"::text) STORED,
 "RecordedByEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 "ActorRoleCode" varchar(100) NOT NULL,
 "ResolvedRoleAssignmentId" uuid NOT NULL REFERENCES advance.employee_role_assignments("Id"),
 "ResolvedRoleAssignmentType" varchar(20) NOT NULL CHECK("ResolvedRoleAssignmentType" IN('FULL','TEMPORARY')),
 "IdempotencyKey" varchar(100) NOT NULL,"RequestFingerprint" char(64) NOT NULL,
 "CreatedAt" timestamptz NOT NULL,"CreatedBy" varchar(160) NOT NULL,
 UNIQUE("CompanyId","IdempotencyKey"),UNIQUE("EvidenceObjectKey"));
CREATE INDEX "IX_vendor_bank_advices_company_vendor" ON advance.vendor_bank_advices("CompanyId","VendorId","CreatedAt");
CREATE TRIGGER trg_vendor_bank_advices_immutable BEFORE INSERT OR UPDATE OR DELETE
 ON advance.vendor_bank_advices FOR EACH ROW EXECUTE FUNCTION advance.guard_vendor_financial_evidence();

CREATE FUNCTION advance.record_vendor_bank_advice(
 p_company uuid,p_vendor uuid,p_filename text,p_type text,p_content bytea,p_sha text,
 p_key text,p_hash text,p_actor uuid,p_role text,p_assignment uuid,p_assignment_type text,p_login text)
RETURNS TABLE("EvidenceId" uuid,"Replayed" boolean)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
DECLARE old advance.vendor_bank_advices%ROWTYPE; eid uuid;
BEGIN
 IF NOT advance.vendor_financial_command_valid(p_company,p_actor,p_role,p_assignment,p_assignment_type,'VendorBankAdvice.Upload') THEN
  RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Bank advice upload requires current FULL or TEMPORARY ACCOUNTS_MANAGER authority.';
 END IF;
 SELECT * INTO old FROM advance.vendor_bank_advices WHERE "CompanyId"=p_company AND "IdempotencyKey"=p_key;
 IF FOUND THEN
  IF old."RequestFingerprint" IS DISTINCT FROM p_hash THEN RAISE EXCEPTION 'Bank advice idempotency mismatch.'; END IF;
  RETURN QUERY SELECT old."Id",true; RETURN;
 END IF;
 IF btrim(coalesce(p_filename,''))='' OR length(p_filename)>255
  OR btrim(coalesce(p_key,''))='' OR length(p_key)>100
  OR btrim(coalesce(p_login,''))='' OR length(p_login)>160
  OR coalesce(p_hash,'')!~'^[0-9a-f]{64}$'
  OR p_content IS NULL OR octet_length(p_content) NOT BETWEEN 1 AND 5242880
  OR lower(btrim(coalesce(p_type,''))) NOT IN('application/pdf','image/jpeg','image/png') THEN
  RAISE EXCEPTION 'A named PDF, JPEG or PNG bank advice of at most 5 MB and a valid command key are required.';
 END IF;
 IF NOT (
  (lower(btrim(p_type))='application/pdf' AND substring(p_content FROM 1 FOR 5)=decode('255044462d','hex'))
  OR(lower(btrim(p_type))='image/jpeg' AND substring(p_content FROM 1 FOR 3)=decode('ffd8ff','hex'))
  OR(lower(btrim(p_type))='image/png' AND substring(p_content FROM 1 FOR 8)=decode('89504e470d0a1a0a','hex'))) THEN
  RAISE EXCEPTION 'Bank advice bytes do not match the declared PDF, JPEG or PNG content type.';
 END IF;
 IF coalesce(p_sha,'')!~'^[0-9a-f]{64}$' OR p_sha<>encode(sha256(p_content),'hex') THEN
  RAISE EXCEPTION 'Bank advice content hash does not match the supplied file.';
 END IF;
 IF NOT EXISTS(SELECT 1 FROM advance.vendors WHERE "Id"=p_vendor) THEN
  RAISE EXCEPTION 'Bank advice vendor was not found.';
 END IF;
 eid:=gen_random_uuid();
 PERFORM set_config('sess.vendor_financial_write',txid_current()::text,true);
 INSERT INTO advance.vendor_bank_advices(
  "Id","CompanyId","VendorId","FileName","ContentType","Content",
  "RecordedByEmployeeId","ActorRoleCode","ResolvedRoleAssignmentId","ResolvedRoleAssignmentType",
  "IdempotencyKey","RequestFingerprint","CreatedAt","CreatedBy")
 VALUES(eid,p_company,p_vendor,btrim(p_filename),lower(btrim(p_type)),p_content,
  p_actor,p_role,p_assignment,p_assignment_type,p_key,p_hash,clock_timestamp(),p_login);
 RETURN QUERY SELECT eid,false;
END $f$;

CREATE FUNCTION advance.vendor_bank_advice_json(p_company uuid,p_id uuid,p_replay boolean)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
 SELECT jsonb_build_object('id',a."Id",'companyId',a."CompanyId",'vendorId',a."VendorId",
  'vendorCode',v."VendorCode",'vendorName',v."Name",'fileName',a."FileName",
  'contentType',a."ContentType",'sizeBytes',a."SizeBytes",'contentSha256',a."ContentSha256",
  'evidenceObjectKey',a."EvidenceObjectKey",'createdAt',a."CreatedAt",'replayed',p_replay)
 FROM advance.vendor_bank_advices a JOIN advance.vendors v ON v."Id"=a."VendorId"
 WHERE a."CompanyId"=p_company AND a."Id"=p_id;
$f$;

CREATE FUNCTION advance.vendor_bank_advice_content(p_company uuid,p_id uuid)
RETURNS TABLE("FileName" text,"ContentType" text,"Content" bytea,"ContentSha256" text)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
 SELECT a."FileName"::text,a."ContentType"::text,a."Content",a."ContentSha256"::text
 FROM advance.vendor_bank_advices a WHERE a."CompanyId"=p_company AND a."Id"=p_id;
$f$;

CREATE FUNCTION advance.require_vendor_bank_advice(p_company uuid,p_vendor uuid,p_key text)
RETURNS void LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
BEGIN
 IF NOT EXISTS(SELECT 1 FROM advance.vendor_bank_advices a
  WHERE a."CompanyId"=p_company AND a."VendorId"=p_vendor AND a."EvidenceObjectKey"=btrim(p_key)) THEN
  RAISE EXCEPTION 'A retained bank advice for this company and vendor is required.';
 END IF;
END $f$;
""";

    internal const string ConfigureOwnership = """
REVOKE ALL ON TABLE advance.vendor_bank_advices FROM PUBLIC;
REVOKE ALL ON FUNCTION
 advance.record_vendor_bank_advice(uuid,uuid,text,text,bytea,text,text,text,uuid,text,uuid,text,text),
 advance.vendor_bank_advice_json(uuid,uuid,boolean),advance.vendor_bank_advice_content(uuid,uuid),
 advance.require_vendor_bank_advice(uuid,uuid,text) FROM PUBLIC;
DO $owner$
BEGIN
 IF to_regrole('nexa_erp_owner') IS NOT NULL THEN
  ALTER TABLE advance.vendor_bank_advices OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.record_vendor_bank_advice(uuid,uuid,text,text,bytea,text,text,text,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.vendor_bank_advice_json(uuid,uuid,boolean) OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.vendor_bank_advice_content(uuid,uuid) OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.require_vendor_bank_advice(uuid,uuid,text) OWNER TO nexa_erp_owner;
  REVOKE ALL ON TABLE advance.vendor_bank_advices FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
  REVOKE ALL ON FUNCTION
   advance.record_vendor_bank_advice(uuid,uuid,text,text,bytea,text,text,text,uuid,text,uuid,text,text),
   advance.vendor_bank_advice_json(uuid,uuid,boolean),advance.vendor_bank_advice_content(uuid,uuid),
   advance.require_vendor_bank_advice(uuid,uuid,text)
   FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
  GRANT EXECUTE ON FUNCTION
   advance.record_vendor_bank_advice(uuid,uuid,text,text,bytea,text,text,text,uuid,text,uuid,text,text),
   advance.vendor_bank_advice_json(uuid,uuid,boolean),advance.vendor_bank_advice_content(uuid,uuid)
   TO nexa_erp_runtime;
 END IF;
END $owner$;
""";
}
