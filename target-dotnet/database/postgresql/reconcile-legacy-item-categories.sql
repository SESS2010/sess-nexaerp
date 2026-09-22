-- Finding 4: reconcile only legacy EXCEL_IMPORT item category references.
-- Reviewed maintenance artifact; run separately from the original import after backup.
-- Does not rewrite GRN snapshots, QC evidence, qualifications, routes or category IDs.
\set ON_ERROR_STOP on
BEGIN ISOLATION LEVEL SERIALIZABLE;
DO $guard$
BEGIN
  IF current_database() IN ('postgres','template0','template1') THEN
    RAISE EXCEPTION 'Category reconciliation refuses a system database';
  END IF;
END $guard$;
CREATE TEMP TABLE category_reconciliation_map (legacy_code text PRIMARY KEY, canonical_code text NOT NULL) ON COMMIT DROP;
INSERT INTO category_reconciliation_map VALUES ('ELECTRICALS','ELE'),('FABRICATION','FAB'),('REFRIGERATION','REF');
DO $guard$
BEGIN
  IF EXISTS (SELECT 1 FROM category_reconciliation_map m
    JOIN advance.item_categories old ON old."Code"=m.legacy_code
    JOIN advance.items i ON i."CategoryId"=old."Id" AND i."CreatedBy"='EXCEL_IMPORT'
    WHERE NOT EXISTS (SELECT 1 FROM advance.item_categories target
      WHERE target."Code"=m.canonical_code AND target."IsActive")) THEN
    RAISE EXCEPTION 'Create or activate canonical ELE/FAB/REF categories before reconciliation';
  END IF;
  IF EXISTS (SELECT 1 FROM advance.qc_inspection_policies p
    JOIN advance.item_categories c ON c."Id"=p."ItemCategoryId"
    JOIN category_reconciliation_map m ON m.legacy_code=c."Code" WHERE p."IsActive")
    OR EXISTS (SELECT 1 FROM advance.vendor_qualifications p
    JOIN advance.item_categories c ON c."Id"=p."ItemCategoryId"
    JOIN category_reconciliation_map m ON m.legacy_code=c."Code" WHERE p."IsActive")
    OR EXISTS (SELECT 1 FROM advance.store_category_routes p
    JOIN advance.item_categories c ON c."Id"=p."ItemCategoryId"
    JOIN category_reconciliation_map m ON m.legacy_code=c."Code" WHERE p."IsActive") THEN
    RAISE EXCEPTION 'Active legacy-category QC policy, qualification or Stores route requires governed reconciliation first';
  END IF;
  IF EXISTS (SELECT 1 FROM advance.items i
    JOIN advance.item_categories old ON old."Id"=i."CategoryId"
    JOIN category_reconciliation_map m ON m.legacy_code=old."Code"
    WHERE i."CreatedBy"='EXCEL_IMPORT' AND i."SubcategoryId" IS NOT NULL) THEN
    RAISE EXCEPTION 'Legacy imported item has a subcategory; reconcile that governed classification before proceeding';
  END IF;
END $guard$;
DO $guard$
BEGIN
  IF EXISTS (SELECT 1 FROM advance.item_categories old
    JOIN category_reconciliation_map m ON m.legacy_code=old."Code"
    JOIN advance.items i ON i."CategoryId"=old."Id"
    WHERE old."CreatedBy"='EXCEL_IMPORT' AND old."IsActive" AND i."IsActive"
      AND i."CreatedBy" IS DISTINCT FROM 'EXCEL_IMPORT')
    OR EXISTS (SELECT 1 FROM advance.item_categories old
    JOIN category_reconciliation_map m ON m.legacy_code=old."Code"
    JOIN advance.item_subcategories sub ON sub."CategoryId"=old."Id"
    WHERE old."CreatedBy"='EXCEL_IMPORT' AND old."IsActive" AND sub."IsActive") THEN
    RAISE EXCEPTION 'Import-owned legacy category has an active non-import item or subcategory; reconcile that reference before retiring the alias';
  END IF;
END $guard$;
CREATE TEMP TABLE category_reconciliation_rows ON COMMIT DROP AS
SELECT i."Id", i."ItemCode", i."CategoryId" AS old_id, target."Id" AS new_id,
       i."Version" AS old_version, old."Code" AS old_code, target."Code" AS new_code
FROM advance.items i JOIN advance.item_categories old ON old."Id"=i."CategoryId"
JOIN category_reconciliation_map m ON m.legacy_code=old."Code"
JOIN advance.item_categories target ON target."Code"=m.canonical_code AND target."IsActive"
WHERE i."CreatedBy"='EXCEL_IMPORT';
UPDATE advance.items i SET "CategoryId"=r.new_id, "Version"=i."Version"+1,
  "UpdatedAt"=now(), "UpdatedBy"='IMPORT_CATEGORY_RECONCILIATION'
FROM category_reconciliation_rows r WHERE i."Id"=r."Id" AND i."Version"=r.old_version;
INSERT INTO advance.audit_logs
("Id","Scope","Module","Action","EntityName","EntityId","UserLoginId","ActorRoleCode","Result","CorrelationId",
 "BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
SELECT gen_random_uuid(),'GLOBAL','Masters','ReconcileImportedCategory','Item',r."Id"::text,session_user,'INSTALLER','Success',
 'IMPORT_CATEGORY_RECONCILIATION:'||r."Id"::text,
 jsonb_build_object('ItemCode',r."ItemCode",'CategoryId',r.old_id,'CategoryCode',r.old_code,'Version',r.old_version)::text,
 jsonb_build_object('ItemCode',r."ItemCode",'CategoryId',r.new_id,'CategoryCode',r.new_code,'Version',r.old_version+1)::text,
 now(),'IMPORT_CATEGORY_RECONCILIATION',0 FROM category_reconciliation_rows r;
DO $guard$
BEGIN
  IF EXISTS (SELECT 1 FROM category_reconciliation_rows r JOIN advance.items i ON i."Id"=r."Id"
    WHERE i."CategoryId"<>r.new_id OR i."Version"<>r.old_version+1) THEN
    RAISE EXCEPTION 'Category reconciliation did not update every selected row';
  END IF;
END $guard$;
CREATE TEMP TABLE category_reconciliation_aliases ON COMMIT DROP AS
SELECT old."Id", old."Code", old."Version" AS old_version
FROM advance.item_categories old
JOIN category_reconciliation_map m ON m.legacy_code=old."Code"
WHERE old."CreatedBy"='EXCEL_IMPORT' AND old."IsActive";
UPDATE advance.item_categories c SET "IsActive"=false,"Version"=c."Version"+1,
  "UpdatedAt"=now(),"UpdatedBy"='IMPORT_CATEGORY_RECONCILIATION'
FROM category_reconciliation_aliases r WHERE c."Id"=r."Id" AND c."Version"=r.old_version;
INSERT INTO advance.audit_logs
("Id","Scope","Module","Action","EntityName","EntityId","UserLoginId","ActorRoleCode","Result","CorrelationId",
 "BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
SELECT gen_random_uuid(),'GLOBAL','Masters','RetireImportedCategoryAlias','ItemCategory',r."Id"::text,session_user,'INSTALLER','Success',
 'IMPORT_CATEGORY_ALIAS_RETIREMENT:'||r."Id"::text,
 jsonb_build_object('Code',r."Code",'IsActive',true,'Version',r.old_version)::text,
 jsonb_build_object('Code',r."Code",'IsActive',false,'Version',r.old_version+1)::text,
 now(),'IMPORT_CATEGORY_RECONCILIATION',0 FROM category_reconciliation_aliases r;
SELECT count(*) AS retired_import_aliases FROM category_reconciliation_aliases;
SELECT old_code, new_code, count(*) AS corrected_items FROM category_reconciliation_rows GROUP BY old_code,new_code ORDER BY old_code;
COMMIT;
