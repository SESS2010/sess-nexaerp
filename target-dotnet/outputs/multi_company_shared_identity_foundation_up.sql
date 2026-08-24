START TRANSACTION;
DELETE FROM advance.employee_role_assignments
WHERE "Id" = '02702296-3863-8644-c306-ddc2f49e5cca';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '068427ee-6fc5-8182-b61c-24b2b3187867';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '157d94ff-a39e-3fa4-3a54-f6f8d05cab62';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '18da9f7c-3049-52e3-b76c-c4238cedb213';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '1b5c6764-7dcd-6f19-0097-61b87603b5eb';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '205cd7e9-b79c-4600-f9c9-561e15e2be9f';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '25c10527-28a2-e600-82d2-3b1b767af269';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '261e0ee9-c1a4-6f18-a3fc-461add06916b';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '270a811f-0564-a4b0-8f4f-0b47118d3134';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '2e2b854a-f965-2a71-21c3-96738e3cb840';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '30e7eac7-1101-ffde-70c0-6edd20ed4c01';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '3b51f513-0e8e-7677-b138-19bc0d9c4150';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '3b6fe413-e8d3-3c0e-52a0-2425db151f48';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '4a1b90a5-9797-0fd0-0e6d-58785e981854';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '53f3f0b9-de8b-4119-3668-01c751a3d52a';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '5554a0f5-85f0-d477-ea7b-f3a6cd1ed121';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '67461916-89e1-fe39-e460-39d2d341d242';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '6c56b8eb-3f8a-4940-df22-5e8002b262da';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '6d4b74b6-5611-c8f5-0ba5-48be51fd6996';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '87dd003b-f6f7-fb19-9f89-c395683c8fa0';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '8b4828cc-bbf0-05df-0f27-a3d789052b82';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '8b8c5e6b-cc4d-4386-50a3-32fb3d776860';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '8c3e4b9b-6be9-9fa3-9c81-fa47f23b5818';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '8c7733c4-1a45-970b-a81b-dbf5aa781ef0';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '8ee5108f-6a19-af67-0562-ee708ebd6a05';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '98804443-54b0-2474-7acb-ffc54410e33e';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '9ac81cf0-423b-97a8-08e7-d3797a7410c7';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = '9e1e368d-3c82-60cf-f522-7758004d3e88';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = 'a260b451-c377-907d-ba80-fb03af55ebc0';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = 'a2bc7e87-56b4-0478-d29d-c329f7eb060a';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = 'a7552ac8-23f1-9ed4-6de8-669d08054e0a';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = 'a79e4f09-112d-57e5-4f17-00066b3e6d22';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = 'ad9892ac-7d0f-89fc-8aec-be5f65860079';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = 'ae3c6d06-5d8c-fa88-ae24-4dcf2ddbfacb';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = 'babde2dc-2cd6-83b4-eea4-84c5886b436e';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = 'c3aa8842-31de-0d93-71b8-ba5e8895a534';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = 'd278c271-c2e2-00a7-a70b-ca058dc2af0e';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = 'e6cf6f13-4f3a-56c8-dbed-608f3b596b6e';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = 'ec95b2c0-4bb6-9b59-3e5e-6fd16ce97ba3';

DELETE FROM advance.employee_role_assignments
WHERE "Id" = 'f03cb56e-0797-3443-b51a-d28205fcdfa7';

ALTER TABLE advance.warehouses ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.warehouse_condition_locations ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.vendor_quotations ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.vendor_quotation_lines ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.vendor_qualifications ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.user_accounts ADD "PrincipalType" character varying(20) NOT NULL DEFAULT 'INTERNAL';

ALTER TABLE advance.tax_gst_settings ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.stock_reservations ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.stock_reservation_history ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.stock_movements ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.stock_availability_checks ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.stock_availability_check_lines ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.rfq_vendor_invitations ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.request_for_quotations ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.request_for_quotation_lines ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.reporting_relationships ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.rack_bins ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.quotation_technical_verifications ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.qc_inspection_policies ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.purchase_transaction_status_history ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.purchase_transaction_approval_policies ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.purchase_transaction_approval_history ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.purchase_requisitions ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.purchase_requisition_status_history ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.purchase_requisition_lines ADD "AssetId" uuid;

ALTER TABLE advance.purchase_requisition_lines ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.purchase_requisition_attachments ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.purchase_requisition_approval_history ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.purchase_requirement_handoffs ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.purchase_orders ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.purchase_order_lines ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.purchase_order_history ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.purchase_number_sequences ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.purchase_approval_workflow_steps ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.purchase_approval_route_settings ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.organization_policies ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.material_followup_handoffs ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.employee_role_assignments ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.employee_operational_scopes ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.employee_identity_mappings ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.employee_department_history ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.departments ADD "ParentDepartmentId" uuid;

ALTER TABLE advance.department_approval_mappings ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.controlled_configuration_histories ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.commercial_comparisons ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.commercial_comparison_lines ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE advance.audit_logs ADD "CompanyId" uuid;

ALTER TABLE advance.audit_logs ADD "Scope" character varying(20) NOT NULL DEFAULT 'GLOBAL';

ALTER TABLE advance.warehouses ADD CONSTRAINT "AK_warehouses_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.warehouse_condition_locations ADD CONSTRAINT "AK_warehouse_condition_locations_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.vendor_quotations ADD CONSTRAINT "AK_vendor_quotations_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.vendor_quotation_lines ADD CONSTRAINT "AK_vendor_quotation_lines_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.vendor_qualifications ADD CONSTRAINT "AK_vendor_qualifications_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.tax_gst_settings ADD CONSTRAINT "AK_tax_gst_settings_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.stock_reservations ADD CONSTRAINT "AK_stock_reservations_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.stock_reservation_history ADD CONSTRAINT "AK_stock_reservation_history_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.stock_movements ADD CONSTRAINT "AK_stock_movements_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.stock_availability_checks ADD CONSTRAINT "AK_stock_availability_checks_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.stock_availability_check_lines ADD CONSTRAINT "AK_stock_availability_check_lines_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.rfq_vendor_invitations ADD CONSTRAINT "AK_rfq_vendor_invitations_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.request_for_quotations ADD CONSTRAINT "AK_request_for_quotations_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.request_for_quotation_lines ADD CONSTRAINT "AK_request_for_quotation_lines_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.reporting_relationships ADD CONSTRAINT "AK_reporting_relationships_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.rack_bins ADD CONSTRAINT "AK_rack_bins_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.quotation_technical_verifications ADD CONSTRAINT "AK_quotation_technical_verifications_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.qc_inspection_policies ADD CONSTRAINT "AK_qc_inspection_policies_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.purchase_transaction_status_history ADD CONSTRAINT "AK_purchase_transaction_status_history_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.purchase_transaction_approval_policies ADD CONSTRAINT "AK_purchase_transaction_approval_policies_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.purchase_transaction_approval_history ADD CONSTRAINT "AK_purchase_transaction_approval_history_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.purchase_requisitions ADD CONSTRAINT "AK_purchase_requisitions_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.purchase_requisition_status_history ADD CONSTRAINT "AK_purchase_requisition_status_history_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.purchase_requisition_lines ADD CONSTRAINT "AK_purchase_requisition_lines_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.purchase_requisition_attachments ADD CONSTRAINT "AK_purchase_requisition_attachments_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.purchase_requisition_approval_history ADD CONSTRAINT "AK_purchase_requisition_approval_history_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.purchase_requirement_handoffs ADD CONSTRAINT "AK_purchase_requirement_handoffs_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.purchase_orders ADD CONSTRAINT "AK_purchase_orders_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.purchase_order_lines ADD CONSTRAINT "AK_purchase_order_lines_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.purchase_order_history ADD CONSTRAINT "AK_purchase_order_history_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.purchase_number_sequences ADD CONSTRAINT "AK_purchase_number_sequences_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.purchase_approval_workflow_steps ADD CONSTRAINT "AK_purchase_approval_workflow_steps_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.purchase_approval_route_settings ADD CONSTRAINT "AK_purchase_approval_route_settings_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.organization_policies ADD CONSTRAINT "AK_organization_policies_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.material_followup_handoffs ADD CONSTRAINT "AK_material_followup_handoffs_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.employee_role_assignments ADD CONSTRAINT "AK_employee_role_assignments_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.employee_operational_scopes ADD CONSTRAINT "AK_employee_operational_scopes_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.employee_identity_mappings ADD CONSTRAINT "AK_employee_identity_mappings_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.employee_department_history ADD CONSTRAINT "AK_employee_department_history_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.department_approval_mappings ADD CONSTRAINT "AK_department_approval_mappings_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.controlled_configuration_histories ADD CONSTRAINT "AK_controlled_configuration_histories_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.commercial_comparisons ADD CONSTRAINT "AK_commercial_comparisons_CompanyId_Id" UNIQUE ("CompanyId", "Id");

ALTER TABLE advance.commercial_comparison_lines ADD CONSTRAINT "AK_commercial_comparison_lines_CompanyId_Id" UNIQUE ("CompanyId", "Id");

CREATE TABLE advance.companies (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "LegalName" character varying(300) NOT NULL,
    "EntityType" character varying(30) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_companies" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_companies_Id_Code" UNIQUE ("Id", "Code"),
    CONSTRAINT "CK_companies_entity_type" CHECK ("EntityType" IN ('PROPRIETORSHIP','PRIVATE_LIMITED')),
    CONSTRAINT "CK_companies_status" CHECK ("Status" IN ('ACTIVE','INACTIVE'))
);

CREATE TABLE advance.currencies (
    "Id" uuid NOT NULL,
    "Code" character varying(3) NOT NULL,
    "Name" character varying(100) NOT NULL,
    "NumericCode" character varying(3),
    "MinorUnitDigits" integer NOT NULL,
    "Symbol" character varying(10),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_currencies" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_currencies_code" CHECK (char_length("Code") = 3),
    CONSTRAINT "CK_currencies_minor_units" CHECK ("MinorUnitDigits" BETWEEN 0 AND 6)
);

CREATE TABLE advance.customer_user_bindings (
    "Id" uuid NOT NULL,
    "UserAccountId" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "IsPrimaryContact" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_customer_user_bindings" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_customer_user_binding_dates" CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom"),
    CONSTRAINT "FK_customer_user_bindings_customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES advance.customers ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_customer_user_bindings_user_accounts_UserAccountId" FOREIGN KEY ("UserAccountId") REFERENCES advance.user_accounts ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.employee_user_bindings (
    "Id" uuid NOT NULL,
    "UserAccountId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_employee_user_bindings" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_employee_user_binding_dates" CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom"),
    CONSTRAINT "FK_employee_user_bindings_employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES advance.employees ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_employee_user_bindings_user_accounts_UserAccountId" FOREIGN KEY ("UserAccountId") REFERENCES advance.user_accounts ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.payment_terms (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(150) NOT NULL,
    "Description" character varying(1000),
    "DueDays" integer NOT NULL,
    "AdvancePercentage" numeric(5,2) NOT NULL,
    "DiscountDays" integer,
    "DiscountPercentage" numeric(5,2),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_payment_terms" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_payment_terms_days" CHECK ("DueDays" >= 0 AND ("DiscountDays" IS NULL OR "DiscountDays" >= 0)),
    CONSTRAINT "CK_payment_terms_percentages" CHECK ("AdvancePercentage" BETWEEN 0 AND 100 AND ("DiscountPercentage" IS NULL OR "DiscountPercentage" BETWEEN 0 AND 100))
);

CREATE TABLE advance.user_identity_mappings (
    "Id" uuid NOT NULL,
    "UserAccountId" uuid NOT NULL,
    "Issuer" character varying(500) NOT NULL,
    "Subject" character varying(500) NOT NULL,
    "IdentityKind" character varying(20) NOT NULL,
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_user_identity_mappings" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_user_identity_mapping_dates" CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom"),
    CONSTRAINT "CK_user_identity_mapping_kind" CHECK ("IdentityKind" IN ('HUMAN','SERVICE')),
    CONSTRAINT "FK_user_identity_mappings_user_accounts_UserAccountId" FOREIGN KEY ("UserAccountId") REFERENCES advance.user_accounts ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.vendor_user_bindings (
    "Id" uuid NOT NULL,
    "UserAccountId" uuid NOT NULL,
    "VendorId" uuid NOT NULL,
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "IsPrimaryContact" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_vendor_user_bindings" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_vendor_user_binding_dates" CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom"),
    CONSTRAINT "FK_vendor_user_bindings_user_accounts_UserAccountId" FOREIGN KEY ("UserAccountId") REFERENCES advance.user_accounts ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_vendor_user_bindings_vendors_VendorId" FOREIGN KEY ("VendorId") REFERENCES advance.vendors ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.company_sites (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(200) NOT NULL,
    "SiteType" character varying(30) NOT NULL,
    "AddressLine1" character varying(300) NOT NULL,
    "AddressLine2" character varying(300),
    "City" character varying(100) NOT NULL,
    "District" character varying(100),
    "State" character varying(100) NOT NULL,
    "StateCode" character varying(2) NOT NULL,
    "PostalCode" character varying(10) NOT NULL,
    "CountryCode" character varying(2) NOT NULL,
    "TimeZoneId" character varying(100) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    "CompanyId" uuid NOT NULL,
    CONSTRAINT "PK_company_sites" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_company_sites_CompanyId_Id" UNIQUE ("CompanyId", "Id"),
    CONSTRAINT "CK_company_sites_country" CHECK (char_length("CountryCode") = 2),
    CONSTRAINT "CK_company_sites_state_code" CHECK (char_length("StateCode") = 2),
    CONSTRAINT "FK_company_sites_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.cost_centres (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(200) NOT NULL,
    "ParentCostCentreId" uuid,
    "DepartmentId" uuid,
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    "CompanyId" uuid NOT NULL,
    CONSTRAINT "PK_cost_centres" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_cost_centres_CompanyId_Id" UNIQUE ("CompanyId", "Id"),
    CONSTRAINT "CK_cost_centre_dates" CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom"),
    CONSTRAINT "FK_cost_centres_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_cost_centres_cost_centres_ParentCostCentreId" FOREIGN KEY ("ParentCostCentreId") REFERENCES advance.cost_centres ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_cost_centres_departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES advance.departments ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.financial_periods (
    "Id" uuid NOT NULL,
    "Code" character varying(30) NOT NULL,
    "Name" character varying(150) NOT NULL,
    "PeriodType" character varying(30) NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL,
    "Status" character varying(20) NOT NULL,
    "ClosedAt" timestamp with time zone,
    "ClosedByUserAccountId" uuid,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    "CompanyId" uuid NOT NULL,
    CONSTRAINT "PK_financial_periods" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_financial_periods_CompanyId_Id" UNIQUE ("CompanyId", "Id"),
    CONSTRAINT "CK_financial_period_dates" CHECK ("EndDate" >= "StartDate"),
    CONSTRAINT "CK_financial_period_status" CHECK ("Status" IN ('OPEN','CLOSED','LOCKED')),
    CONSTRAINT "FK_financial_periods_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_financial_periods_user_accounts_ClosedByUserAccountId" FOREIGN KEY ("ClosedByUserAccountId") REFERENCES advance.user_accounts ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.user_role_assignments (
    "Id" uuid NOT NULL,
    "UserAccountId" uuid NOT NULL,
    "RoleId" uuid NOT NULL,
    "CompanyId" uuid,
    "Audience" character varying(20) NOT NULL,
    "Scope" character varying(20) NOT NULL,
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_user_role_assignments" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_user_role_assignment_audience" CHECK ("Audience" IN ('INTERNAL','VENDOR','CUSTOMER')),
    CONSTRAINT "CK_user_role_assignment_dates" CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom"),
    CONSTRAINT "CK_user_role_assignment_scope" CHECK (("Scope"='GLOBAL' AND "CompanyId" IS NULL) OR ("Scope"='COMPANY' AND "CompanyId" IS NOT NULL)),
    CONSTRAINT "FK_user_role_assignments_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_user_role_assignments_roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES advance.roles ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_user_role_assignments_user_accounts_UserAccountId" FOREIGN KEY ("UserAccountId") REFERENCES advance.user_accounts ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.customer_company_relationships (
    "Id" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "RelationshipStatus" character varying(30) NOT NULL,
    "PaymentTermId" uuid,
    "CreditPeriodDays" integer,
    "CreditLimit" numeric(18,2),
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "ApprovedByEmployeeId" uuid,
    "ApprovedAt" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    "CompanyId" uuid NOT NULL,
    CONSTRAINT "PK_customer_company_relationships" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_customer_company_relationships_CompanyId_Id" UNIQUE ("CompanyId", "Id"),
    CONSTRAINT "CK_customer_company_relationship_dates" CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom"),
    CONSTRAINT "FK_customer_company_relationships_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_customer_company_relationships_customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES advance.customers ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_customer_company_relationships_employees_ApprovedByEmployee~" FOREIGN KEY ("ApprovedByEmployeeId") REFERENCES advance.employees ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_customer_company_relationships_payment_terms_PaymentTermId" FOREIGN KEY ("PaymentTermId") REFERENCES advance.payment_terms ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.vendor_company_relationships (
    "Id" uuid NOT NULL,
    "VendorId" uuid NOT NULL,
    "RelationshipStatus" character varying(30) NOT NULL,
    "PaymentTermId" uuid,
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "ApprovedByEmployeeId" uuid,
    "ApprovedAt" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    "CompanyId" uuid NOT NULL,
    CONSTRAINT "PK_vendor_company_relationships" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_vendor_company_relationships_CompanyId_Id" UNIQUE ("CompanyId", "Id"),
    CONSTRAINT "CK_vendor_company_relationship_dates" CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom"),
    CONSTRAINT "FK_vendor_company_relationships_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_vendor_company_relationships_employees_ApprovedByEmployeeId" FOREIGN KEY ("ApprovedByEmployeeId") REFERENCES advance.employees ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_vendor_company_relationships_payment_terms_PaymentTermId" FOREIGN KEY ("PaymentTermId") REFERENCES advance.payment_terms ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_vendor_company_relationships_vendors_VendorId" FOREIGN KEY ("VendorId") REFERENCES advance.vendors ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.assets (
    "Id" uuid NOT NULL,
    "AssetCode" character varying(80) NOT NULL,
    "AssetType" character varying(50) NOT NULL,
    "ItemId" uuid,
    "SerialNumber" character varying(160),
    "CustomerId" uuid,
    "CustomerAddressId" uuid,
    "CompanySiteId" uuid,
    "InstallationDate" date,
    "WarrantyStartDate" date,
    "WarrantyEndDate" date,
    "Status" character varying(30) NOT NULL,
    "Description" character varying(2000),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    "CompanyId" uuid NOT NULL,
    CONSTRAINT "PK_assets" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_assets_CompanyId_Id" UNIQUE ("CompanyId", "Id"),
    CONSTRAINT "CK_asset_installation_warranty" CHECK ("WarrantyStartDate" IS NULL OR "InstallationDate" IS NULL OR "WarrantyStartDate" >= "InstallationDate"),
    CONSTRAINT "CK_asset_warranty_dates" CHECK ("WarrantyEndDate" IS NULL OR "WarrantyStartDate" IS NULL OR "WarrantyEndDate" >= "WarrantyStartDate"),
    CONSTRAINT "FK_assets_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_assets_company_sites_CompanySiteId" FOREIGN KEY ("CompanySiteId") REFERENCES advance.company_sites ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_assets_customer_addresses_CustomerAddressId" FOREIGN KEY ("CustomerAddressId") REFERENCES advance.customer_addresses ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_assets_customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES advance.customers ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_assets_items_ItemId" FOREIGN KEY ("ItemId") REFERENCES advance.items ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.company_gst_registrations (
    "Id" uuid NOT NULL,
    "CompanySiteId" uuid,
    "Gstin" character varying(15) NOT NULL,
    "RegisteredLegalName" character varying(300) NOT NULL,
    "StateCode" character varying(2) NOT NULL,
    "RegistrationType" character varying(30) NOT NULL,
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "IsPrimary" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    "CompanyId" uuid NOT NULL,
    CONSTRAINT "PK_company_gst_registrations" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_company_gst_registrations_CompanyId_Id" UNIQUE ("CompanyId", "Id"),
    CONSTRAINT "CK_company_gst_registration_dates" CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom"),
    CONSTRAINT "CK_company_gst_registrations_gstin" CHECK (char_length("Gstin") = 15),
    CONSTRAINT "FK_company_gst_registrations_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_company_gst_registrations_company_sites_CompanySiteId" FOREIGN KEY ("CompanySiteId") REFERENCES advance.company_sites ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.employee_company_assignments (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "AssignmentType" character varying(20) NOT NULL,
    "EmployeeCode" character varying(50) NOT NULL,
    "PayrollEmployeeId" character varying(50),
    "CompanySiteId" uuid,
    "EmploymentType" character varying(50) NOT NULL,
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "Status" character varying(20) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    "CompanyId" uuid NOT NULL,
    CONSTRAINT "PK_employee_company_assignments" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_employee_company_assignments_CompanyId_Id" UNIQUE ("CompanyId", "Id"),
    CONSTRAINT "CK_employee_company_assignment_dates" CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom"),
    CONSTRAINT "CK_employee_company_assignment_type" CHECK ("AssignmentType" IN ('PAYROLL','WORK')),
    CONSTRAINT "FK_employee_company_assignments_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_employee_company_assignments_company_sites_CompanySiteId" FOREIGN KEY ("CompanySiteId") REFERENCES advance.company_sites ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_employee_company_assignments_employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES advance.employees ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.projects (
    "Id" uuid NOT NULL,
    "ProjectCode" character varying(50) NOT NULL,
    "Name" character varying(250) NOT NULL,
    "ProjectType" character varying(50) NOT NULL,
    "CustomerId" uuid,
    "CompanySiteId" uuid,
    "CostCentreId" uuid,
    "ManagerEmployeeId" uuid,
    "StartDate" date,
    "TargetEndDate" date,
    "ActualEndDate" date,
    "Status" character varying(30) NOT NULL,
    "Description" character varying(2000),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    "CompanyId" uuid NOT NULL,
    CONSTRAINT "PK_projects" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_projects_CompanyId_Id" UNIQUE ("CompanyId", "Id"),
    CONSTRAINT "CK_project_dates" CHECK (("TargetEndDate" IS NULL OR "StartDate" IS NULL OR "TargetEndDate" >= "StartDate") AND ("ActualEndDate" IS NULL OR "StartDate" IS NULL OR "ActualEndDate" >= "StartDate")),
    CONSTRAINT "FK_projects_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_projects_company_sites_CompanySiteId" FOREIGN KEY ("CompanySiteId") REFERENCES advance.company_sites ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_projects_cost_centres_CostCentreId" FOREIGN KEY ("CostCentreId") REFERENCES advance.cost_centres ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_projects_customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES advance.customers ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_projects_employees_ManagerEmployeeId" FOREIGN KEY ("ManagerEmployeeId") REFERENCES advance.employees ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.document_number_sequences (
    "Id" uuid NOT NULL,
    "DocumentType" character varying(50) NOT NULL,
    "FinancialPeriodId" uuid NOT NULL,
    "Prefix" character varying(50) NOT NULL,
    "Suffix" character varying(50),
    "PaddingLength" integer NOT NULL,
    "LastNumber" bigint NOT NULL,
    "FormatPattern" character varying(200),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    "CompanyId" uuid NOT NULL,
    CONSTRAINT "PK_document_number_sequences" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_document_number_sequences_CompanyId_Id" UNIQUE ("CompanyId", "Id"),
    CONSTRAINT "CK_document_number_sequence_last" CHECK ("LastNumber" >= 0),
    CONSTRAINT "CK_document_number_sequence_padding" CHECK ("PaddingLength" BETWEEN 1 AND 18),
    CONSTRAINT "FK_document_number_sequences_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_document_number_sequences_financial_periods_FinancialPeriod~" FOREIGN KEY ("FinancialPeriodId") REFERENCES advance.financial_periods ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.employee_department_assignments (
    "Id" uuid NOT NULL,
    "EmployeeCompanyAssignmentId" uuid NOT NULL,
    "DepartmentId" uuid NOT NULL,
    "DesignationId" uuid NOT NULL,
    "AssignmentType" character varying(20) NOT NULL,
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "IsPrimary" boolean NOT NULL,
    "Status" character varying(20) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    "CompanyId" uuid NOT NULL,
    CONSTRAINT "PK_employee_department_assignments" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_employee_department_assignments_CompanyId_Id" UNIQUE ("CompanyId", "Id"),
    CONSTRAINT "CK_employee_department_assignment_dates" CHECK ("EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom"),
    CONSTRAINT "CK_employee_department_assignment_primary" CHECK (("AssignmentType" = 'PRIMARY') = "IsPrimary"),
    CONSTRAINT "CK_employee_department_assignment_type" CHECK ("AssignmentType" IN ('PRIMARY','SECONDARY')),
    CONSTRAINT "FK_employee_department_assignments_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_employee_department_assignments_departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES advance.departments ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_employee_department_assignments_designations_DesignationId" FOREIGN KEY ("DesignationId") REFERENCES advance.designations ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_employee_department_assignments_employee_company_assignment~" FOREIGN KEY ("EmployeeCompanyAssignmentId") REFERENCES advance.employee_company_assignments ("Id") ON DELETE CASCADE
);

CREATE TABLE advance.document_revisions (
    "Id" uuid NOT NULL,
    "DocumentId" uuid NOT NULL,
    "RevisionCode" character varying(50) NOT NULL,
    "RevisionNumber" integer NOT NULL,
    "Title" character varying(300) NOT NULL,
    "StorageKey" character varying(1000) NOT NULL,
    "FileName" character varying(300) NOT NULL,
    "ContentType" character varying(150) NOT NULL,
    "SizeBytes" bigint NOT NULL,
    "Sha256" bytea NOT NULL,
    "ChangeSummary" character varying(2000),
    "Status" character varying(30) NOT NULL,
    "EffectiveFrom" date,
    "ReleasedAt" timestamp with time zone,
    "ReleasedByUserAccountId" uuid,
    "IsCurrent" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_document_revisions" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_document_revision_number" CHECK ("RevisionNumber" >= 0),
    CONSTRAINT "CK_document_revision_sha256" CHECK (octet_length("Sha256") = 32),
    CONSTRAINT "CK_document_revision_size" CHECK ("SizeBytes" >= 0),
    CONSTRAINT "FK_document_revisions_user_accounts_ReleasedByUserAccountId" FOREIGN KEY ("ReleasedByUserAccountId") REFERENCES advance.user_accounts ("Id") ON DELETE RESTRICT
);

CREATE TABLE advance.documents (
    "Id" uuid NOT NULL,
    "DocumentNumber" character varying(100) NOT NULL,
    "DocumentType" character varying(50) NOT NULL,
    "Title" character varying(300) NOT NULL,
    "Description" character varying(2000),
    "OwnerDepartmentId" uuid,
    "CurrentRevisionId" uuid,
    "Status" character varying(30) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_documents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_documents_departments_OwnerDepartmentId" FOREIGN KEY ("OwnerDepartmentId") REFERENCES advance.departments ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_documents_document_revisions_CurrentRevisionId" FOREIGN KEY ("CurrentRevisionId") REFERENCES advance.document_revisions ("Id") ON DELETE RESTRICT
);

UPDATE advance.audit_logs SET "CompanyId" = NULL, "Scope" = 'GLOBAL'
WHERE "Id" = '11744032-08e9-f364-d36f-c12caeff0b02';

UPDATE advance.audit_logs SET "CompanyId" = NULL, "Scope" = 'GLOBAL'
WHERE "Id" = '2a23e241-204c-4810-46cd-5f1b0f513434';

UPDATE advance.audit_logs SET "CompanyId" = NULL, "Scope" = 'GLOBAL'
WHERE "Id" = '2e2eb9a5-7caa-e157-2099-e3f06e85fbad';

UPDATE advance.audit_logs SET "CompanyId" = NULL, "Scope" = 'GLOBAL'
WHERE "Id" = '51a38ab8-5943-e4f6-6140-76dea2057e8b';

UPDATE advance.audit_logs SET "CompanyId" = NULL, "Scope" = 'GLOBAL'
WHERE "Id" = 'bf16025e-df11-ac0e-785b-4873e1a14af3';

UPDATE advance.audit_logs SET "CompanyId" = NULL, "Scope" = 'GLOBAL'
WHERE "Id" = 'bf6ef4ae-fe3a-2861-28d4-88f7708aba51';

INSERT INTO advance.companies ("Id", "Code", "CreatedAt", "CreatedBy", "EntityType", "IsActive", "LegalName", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('70000000-0000-0000-0000-000000000001', 'SESS_PVT_LTD', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'PRIVATE_LIMITED', TRUE, 'Sri Easwari Scientific Solution Private Limited', 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.companies ("Id", "Code", "CreatedAt", "CreatedBy", "EntityType", "IsActive", "LegalName", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('70000000-0000-0000-0000-000000000002', 'SESS_PROPRIETORSHIP', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'PROPRIETORSHIP', TRUE, 'Sri Easwari Scientific Solution', 'ACTIVE', NULL, NULL, 0);

INSERT INTO advance.currencies ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "MinorUnitDigits", "Name", "NumericCode", "Symbol", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('72000000-0000-0000-0000-000000000001', 'INR', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', TRUE, 2, 'Indian Rupee', '356', '₹', NULL, NULL, 0);

UPDATE advance.departments SET "ParentDepartmentId" = NULL
WHERE "Id" = '0057b580-1cb1-afa2-8328-5afb1162e77e';

UPDATE advance.departments SET "Code" = 'FABRICATION', "Name" = 'Fabrication', "ParentDepartmentId" = '1c67b401-c1d8-7d42-e3f9-82217720202f'
WHERE "Id" = '51d81035-b83e-5452-c5c8-be69b5d3b1b3';

UPDATE advance.departments SET "Code" = 'LEGACY_MANAGER', "IsActive" = FALSE, "Name" = 'Manager (Legacy)', "ParentDepartmentId" = NULL
WHERE "Id" = '6ea3e733-e5e0-9b55-e7de-db94afda2b09';

UPDATE advance.departments SET "Code" = 'LEGACY_JUNIOR_ASSISTANT', "IsActive" = FALSE, "Name" = 'Junior/Assistant (Legacy)', "ParentDepartmentId" = NULL
WHERE "Id" = 'bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e';

UPDATE advance.departments SET "Code" = 'LEGACY_ADMIN_ACCOUNTS_STORES', "IsActive" = FALSE, "Name" = 'Admin/Accounts/Stores (Legacy)', "ParentDepartmentId" = NULL
WHERE "Id" = 'd30a9101-4e01-b19c-bc7c-926feb98e889';

UPDATE advance.departments SET "Code" = 'LEGACY_ENGINEER_TECHNICAL', "IsActive" = FALSE, "Name" = 'Engineer/Technical (Legacy)', "ParentDepartmentId" = NULL
WHERE "Id" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4';

INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('05f64e3a-d2a5-1c89-cf9e-667e367e8dae', 'HR', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'HR', NULL, NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('1c67b401-c1d8-7d42-e3f9-82217720202f', 'PRODUCTION', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Production', NULL, NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('2243fe97-0335-bcf7-af29-8c0d5e0bac25', 'DESIGN', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Design', NULL, NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('284d56ef-2605-02b0-5ac6-9697daa4242a', 'MAINTENANCE', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Maintenance', NULL, NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('2ed4c8a3-6340-83ae-b149-95e5b8492b11', 'MARKETING', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Marketing', NULL, NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('45f7f136-f943-7fbf-c4d3-1b588f7faf71', 'IT', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'IT', NULL, NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('4c7c86e7-b074-2136-4430-41b9c07757ff', 'CAMC', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'CAMC', NULL, NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('621badfa-eecb-34b1-44c5-029bc6447658', 'R_AND_D', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'R&D', NULL, NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('6456dcf7-dad1-598a-f332-ad31dbfc907c', 'ACCOUNTS', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Accounts', NULL, NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('89de4fb3-b401-e463-94f4-d6cef1ee18a4', 'SALES', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Sales', NULL, NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'SERVICE', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Service', NULL, NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('92f4dfe8-2c81-11db-0e68-ae27000fd606', 'AMC', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'AMC', NULL, NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('97353e2b-c03c-03ad-dad5-07e697b6429f', 'STORES', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Stores', NULL, NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('dd6ab604-a58e-4884-7df9-2ceb7456df64', 'PURCHASE', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Purchase', NULL, NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('e7bcb9d9-f7df-5ecc-fd69-54fb437e2f5e', 'QC', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'QC', NULL, NULL, NULL, 0);

UPDATE advance.designations SET "Name" = 'LabVIEW Developer'
WHERE "Id" = '075fb64f-355a-ee74-517b-6b9c6da0f8db';

UPDATE advance.designations SET "Name" = 'Technical Director'
WHERE "Id" = '086ab1d4-3404-12b7-c35a-4b77737eb97b';

UPDATE advance.designations SET "Name" = 'Electrical Engineer'
WHERE "Id" = '2f148a82-10ab-5801-9ff1-9f510611e5fd';

UPDATE advance.designations SET "Name" = 'Junior Accounts'
WHERE "Id" = '35936fb3-4fc0-4757-268f-c467720e39fa';

UPDATE advance.designations SET "Code" = 'LEGACY_JR_ACCOUNT', "IsActive" = FALSE, "Name" = 'Jr. Account (Legacy)'
WHERE "Id" = '37ae1390-d60b-28aa-f5f8-43b5549936c8';

UPDATE advance.designations SET "Code" = 'LEGACY_JR_ELECTRICAL_PLC_SUPPORT', "IsActive" = FALSE, "Name" = 'Jr. Electrical / PLC / Instrumentation Support (Legacy)'
WHERE "Id" = '39f842c4-5688-20a6-2a81-dc0fed68aa0f';

UPDATE advance.designations SET "Name" = 'Junior Engineer'
WHERE "Id" = '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa';

UPDATE advance.designations SET "Name" = 'Design Engineer'
WHERE "Id" = '4c9baa15-c3d4-6b41-d040-f354c5cff307';

UPDATE advance.designations SET "Code" = 'HR_MANAGER', "Name" = 'HR Manager'
WHERE "Id" = '82783939-c768-2002-5b0e-17db5261eab9';

UPDATE advance.designations SET "Code" = 'HOUSEKEEPING_ASSISTANT', "Name" = 'Housekeeping Assistant'
WHERE "Id" = '8e377677-95bb-f0fe-4207-2efaf2b89208';

UPDATE advance.designations SET "Name" = 'Software Developer'
WHERE "Id" = '90c527f8-3ea8-dc72-7283-c80e73a71f5d';

UPDATE advance.designations SET "Code" = 'LEGACY_STORES_AND_PURCHASE', "IsActive" = FALSE, "Name" = 'Stores and Purchase (Legacy)'
WHERE "Id" = '940ac030-8dcf-1575-6545-fea0f75f18f8';

UPDATE advance.designations SET "Name" = 'Production Coordinator'
WHERE "Id" = '96908ceb-4e96-b670-db7e-59b2237f1dec';

UPDATE advance.designations SET "Code" = 'LEGACY_JR_ENGINEER', "IsActive" = FALSE, "Name" = 'Jr. Engineer (Legacy)'
WHERE "Id" = 'a2ed4710-4cec-d8dd-097e-e8c7353a66a6';

UPDATE advance.designations SET "Code" = 'MANAGING_DIRECTOR', "Name" = 'Managing Director'
WHERE "Id" = 'a653c7ab-0b15-c0fc-bdcb-8cb6c64bd830';

UPDATE advance.designations SET "Code" = 'REFRIGERATION_MECHANICAL_ENGINEER', "Name" = 'Refrigeration / Mechanical Engineer'
WHERE "Id" = 'b5b051ca-7d0d-c78a-0e14-9794651490db';

UPDATE advance.designations SET "Name" = 'PLC Engineer'
WHERE "Id" = 'b9de3ebc-27c8-8fd6-85b2-d8bc916e6dfa';

UPDATE advance.designations SET "Name" = 'Technical Support Manager'
WHERE "Id" = 'c7775052-f0a9-27e3-f259-746120a113a6';

UPDATE advance.designations SET "Name" = 'Fabricator'
WHERE "Id" = 'e4bec48d-a248-c13d-a71a-00a2dd40e35e';

UPDATE advance.designations SET "Code" = 'LEGACY_PRODUCTION_MECHANICAL_TEAM', "IsActive" = FALSE, "Name" = 'Production Mechanical Team (Legacy)'
WHERE "Id" = 'f38530d3-549c-8fe3-3f75-331795d92bd3';

UPDATE advance.designations SET "Name" = 'Stores Assistant'
WHERE "Id" = 'f8b60dea-0ce6-fa0b-f56e-a7e01058bfc6';

INSERT INTO advance.designations ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('9865dd7b-b329-ccef-54cd-67bdc1cfdd27', 'IT_MANAGER', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'IT Manager', NULL, NULL, 0);
INSERT INTO advance.designations ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('b6bd22cd-d47c-d8b2-daff-19a3d7c33d5d', 'MAINTENANCE_ENGINEER', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Maintenance Engineer', NULL, NULL, 0);

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-012","Name":"PRIYA.E","EmployeeType":"Permanent","Grade":"Executive","Department":"Stores","Skill":"Admin/Accounts/Stores","Designation":"STORES ASSISTANT","Roles":["PURCHASE_EXECUTIVE","STORES_EXECUTIVE"]}'
WHERE "Id" = '0f56a17e-c040-acb4-6736-1cc168a81c46';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-003","Name":"M. SATHISHKUMAR","EmployeeType":"Permanent","Grade":"Executive","Department":"Refrigeration","Skill":"Engineer/Technical","Designation":"REFRIGERATION / MECHANICAL ENGINEER","Roles":["TECHNICAL_ENGINEER"]}'
WHERE "Id" = '0f6b42e6-1bab-d372-290a-9057fd7805f6';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-037","Name":"DEVANAND B","EmployeeType":"Permanent","Grade":"Executive","Department":"Refrigeration","Skill":"Engineer/Technical","Designation":"REFRIGERATION / MECHANICAL ENGINEER","Roles":["TECHNICAL_ENGINEER"]}'
WHERE "Id" = '1963049e-f974-5923-54e3-72af4c92f635';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-032","Name":"PRASANNA.G","EmployeeType":"Permanent","Grade":"Executive","Department":"PLC/LabVIEW","Skill":"Engineer/Technical","Designation":"LABVIEW DEVELOPER","Roles":["SOFTWARE_ENGINEER"]}'
WHERE "Id" = '2bc77b77-1d6d-4279-8d9d-8cf854537ea0';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-002","Name":"ALAGUEASWARI","EmployeeType":"Permanent","Grade":"Executive","Department":"Management","Skill":"Management","Designation":"MANAGING DIRECTOR","Roles":["MANAGING_DIRECTOR"]}'
WHERE "Id" = '2d009327-ea1c-2e86-5f13-bc4df67fd6bc';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-007","Name":"A. ALFATHIMA PARVEEN","EmployeeType":"Permanent","Grade":"Executive","Department":"Accounts","Skill":"Junior/Assistant","Designation":"JUNIOR ACCOUNTS","Roles":["ACCOUNTS_ASSISTANT"]}'
WHERE "Id" = '2f40e507-8533-479e-6db2-d696d7cb5807';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-011","Name":"YESWANTH KUMAR.N","EmployeeType":"Permanent","Grade":"Executive","Department":"Service","Skill":"Junior/Assistant","Designation":"JUNIOR ENGINEER","Roles":["JUNIOR_ENGINEER"]}'
WHERE "Id" = '3045b304-1c11-b626-4170-02ed928cfde8';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-035","Name":"VINAYAGAM","EmployeeType":"Permanent","Grade":"Executive","Department":"Fabrication","Skill":"Production/Fabrication","Designation":"FABRICATOR","Roles":["PRODUCTION_OPERATOR"]}'
WHERE "Id" = '360cc0c3-8709-66a2-513c-bff91aed60e0';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-006","Name":"S. NANTHAKUMAR","EmployeeType":"Permanent","Grade":"Executive","Department":"Electrical","Skill":"Junior/Assistant","Designation":"JUNIOR ENGINEER","Roles":["JUNIOR_ENGINEER"]}'
WHERE "Id" = '38f02d97-8ea0-c6a0-6132-cf41067a7af3';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-010","Name":"RAJESHKUMAR.V","EmployeeType":"Permanent","Grade":"Executive","Department":"Electrical","Skill":"Engineer/Technical","Designation":"ELECTRICAL ENGINEER","Roles":["ELECTRICAL_ENGINEER"]}'
WHERE "Id" = '3da0797e-c7ce-8c50-3bcd-a857613a54db';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-020","Name":"RANJEETH.B","EmployeeType":"Permanent","Grade":"Executive","Department":"HR","Skill":"Admin/Accounts/Stores","Designation":"HR MANAGER","Roles":["HR_EXECUTIVE"]}'
WHERE "Id" = '402f96b9-1b0a-2400-183e-987b2b06f2d6';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-008","Name":"SURANTHER P","EmployeeType":"Permanent","Grade":"Executive","Department":"IT","Skill":"Engineer/Technical","Designation":"IT MANAGER","Roles":["SOFTWARE_DEVELOPER"]}'
WHERE "Id" = '433b462b-d44e-0ce4-a6ba-a9373b87e605';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-017","Name":"MOHD ASHIQ","EmployeeType":"Permanent","Grade":"Executive","Department":"Electrical","Skill":"Junior/Assistant","Designation":"JUNIOR ENGINEER","Roles":["JUNIOR_ENGINEER"]}'
WHERE "Id" = '48023480-faa5-975e-ee67-4ee5854aa96b';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-029","Name":"SRINIVASAN.C","EmployeeType":"Permanent","Grade":"Executive","Department":"Refrigeration","Skill":"Engineer/Technical","Designation":"REFRIGERATION / MECHANICAL ENGINEER","Roles":["TECHNICAL_ENGINEER"]}'
WHERE "Id" = '55b979e1-f612-de68-1aa0-d6348dd174cd';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-034","Name":"MADHANKUMAR.J","EmployeeType":"Permanent","Grade":"Executive","Department":"Refrigeration","Skill":"Engineer/Technical","Designation":"REFRIGERATION / MECHANICAL ENGINEER","Roles":["TECHNICAL_ENGINEER"]}'
WHERE "Id" = '59fbbe70-bb14-d466-3bf7-e97a1040c446';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-009","Name":"MANIKANDAN.S","EmployeeType":"Permanent","Grade":"Executive","Department":"Maintenance","Skill":"Junior/Assistant","Designation":"MAINTENANCE ENGINEER","Roles":["JUNIOR_ENGINEER"]}'
WHERE "Id" = '5d2880b6-6e40-84c4-b982-e4f16b422dd5';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-015","Name":"RANJITH.E","EmployeeType":"Permanent","Grade":"Executive","Department":"Design","Skill":"Engineer/Technical","Designation":"DESIGN ENGINEER","Roles":["DESIGN_ENGINEER"]}'
WHERE "Id" = '6695623a-7f5c-4041-00e4-c8d7cde7745e';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-039","Name":"THIRUNAVUKKARASU","EmployeeType":"Permanent","Grade":"Executive","Department":"Refrigeration","Skill":"Engineer/Technical","Designation":"REFRIGERATION / MECHANICAL ENGINEER","Roles":["TECHNICAL_ENGINEER"]}'
WHERE "Id" = '756caab8-cd36-fe0a-4a9b-2cfc2651549e';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-023","Name":"SARATH BABU.K","EmployeeType":"Permanent","Grade":"Executive","Department":"Fabrication","Skill":"Production/Fabrication","Designation":"PRODUCTION COORDINATOR","Roles":["PRODUCTION_COORDINATOR"]}'
WHERE "Id" = '75cd655f-0c24-89ae-9f3b-11fc83651c0e';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-027","Name":"SANJAY SARAVANAN","EmployeeType":"Permanent","Grade":"Executive","Department":"Accounts","Skill":"Junior/Assistant","Designation":"JUNIOR ACCOUNTS","Roles":["ACCOUNTS_ASSISTANT"]}'
WHERE "Id" = '91576a97-ed27-5bf5-5ff3-82bf4912a2da';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-024","Name":"PRAKASAM.B","EmployeeType":"Permanent","Grade":"Executive","Department":"Electrical","Skill":"Engineer/Technical","Designation":"ELECTRICAL ENGINEER","Roles":["ELECTRICAL_ENGINEER"]}'
WHERE "Id" = '9a98139e-e3cf-e3a5-efb7-eb276b5b5bf7';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-038","Name":"SYED IJAZUDDIN Z","EmployeeType":"Permanent","Grade":"Executive","Department":"PLC/LabVIEW","Skill":"Engineer/Technical","Designation":"PLC ENGINEER","Roles":["PLC_ENGINEER"]}'
WHERE "Id" = '9c911b33-3733-9d90-307f-c2221e6586b3';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-014","Name":"KAMALI SRINIVASAN","EmployeeType":"Permanent","Grade":"Executive","Department":"Stores","Skill":"Junior/Assistant","Designation":"STORES ASSISTANT","Roles":["STORES_ASSISTANT"]}'
WHERE "Id" = 'a0519833-9d8b-dbd7-42aa-df3fb73ab391';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-004","Name":"T. DINESH","EmployeeType":"Permanent","Grade":"Executive","Department":"Service","Skill":"Manager","Designation":"TECHNICAL SUPPORT MANAGER","Roles":["TECHNICAL_SUPPORT_MANAGER"]}'
WHERE "Id" = 'a16a71a7-1c21-c40b-7fe5-4b76aa13f2d7';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-028","Name":"PRAVEEN KUMAR.M","EmployeeType":"Permanent","Grade":"Executive","Department":"Refrigeration","Skill":"Engineer/Technical","Designation":"REFRIGERATION / MECHANICAL ENGINEER","Roles":["TECHNICAL_ENGINEER"]}'
WHERE "Id" = 'a9a42d67-1710-9687-2eeb-df48df1adc33';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-016","Name":"KALIDOSS","EmployeeType":"Permanent","Grade":"Executive","Department":"Design","Skill":"Engineer/Technical","Designation":"DESIGN ENGINEER","Roles":["DESIGN_ENGINEER"]}'
WHERE "Id" = 'b2e05e24-8e31-871f-a938-4253cfe87be9';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-030","Name":"MANIKANDAN SOKKALINGAM","EmployeeType":"Permanent","Grade":"Executive","Department":"Electrical","Skill":"Engineer/Technical","Designation":"ELECTRICAL ENGINEER","Roles":["ELECTRICAL_ENGINEER"]}'
WHERE "Id" = 'b4c08282-5c80-b7a0-5143-fd5a5bb112a1';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-022","Name":"KARTHICK.B","EmployeeType":"Permanent","Grade":"Executive","Department":"Electrical","Skill":"Engineer/Technical","Designation":"ELECTRICAL ENGINEER","Roles":["ELECTRICAL_ENGINEER"]}'
WHERE "Id" = 'b7dea89e-de29-daa2-4608-72c6734e3aa1';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-018","Name":"A. VINAYA SAGAR ARKATI","EmployeeType":"Permanent","Grade":"Executive","Department":"Electrical","Skill":"Engineer/Technical","Designation":"ELECTRICAL ENGINEER","Roles":["ELECTRICAL_ENGINEER"]}'
WHERE "Id" = 'c02926b7-b69c-f94e-4f98-d3e7e8b304a6';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-031","Name":"VENKAT RAV.S","EmployeeType":"Permanent","Grade":"Executive","Department":"Accounts","Skill":"Junior/Assistant","Designation":"JUNIOR ACCOUNTS","Roles":["ACCOUNTS_ASSISTANT"]}'
WHERE "Id" = 'c169fe6d-6b2c-33ec-c820-daaebaf58fef';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-021","Name":"KRISHNAVENI","EmployeeType":"Permanent","Grade":"Executive","Department":"HR","Skill":"Admin/Accounts/Stores","Designation":"HOUSEKEEPING ASSISTANT","Roles":["ADMIN_EXECUTIVE"]}'
WHERE "Id" = 'c4c160a6-38ca-fb45-1596-1acde02fef13';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-005","Name":"WASEEM.S","EmployeeType":"Permanent","Grade":"Executive","Department":"Fabrication","Skill":"Production/Fabrication","Designation":"FABRICATOR","Roles":["PRODUCTION_OPERATOR"]}'
WHERE "Id" = 'ca1ac22f-c92b-6f0b-6d00-dd686a27adf0';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-036","Name":"FRANCIS XAVIER","EmployeeType":"Permanent","Grade":"Executive","Department":"Refrigeration","Skill":"Engineer/Technical","Designation":"REFRIGERATION / MECHANICAL ENGINEER","Roles":["TECHNICAL_ENGINEER"]}'
WHERE "Id" = 'cfdc990d-5afd-1b29-bf52-ab5995b174cf';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-025","Name":"KARTHIKEYAN MK","EmployeeType":"Permanent","Grade":"Executive","Department":"Fabrication","Skill":"Production/Fabrication","Designation":"FABRICATOR","Roles":["PRODUCTION_OPERATOR"]}'
WHERE "Id" = 'd181ade1-290a-8ebe-1f57-47b66b4ecdde';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-013","Name":"LALU","EmployeeType":"Permanent","Grade":"Executive","Department":"Fabrication","Skill":"Production/Fabrication","Designation":"FABRICATOR","Roles":["PRODUCTION_OPERATOR"]}'
WHERE "Id" = 'd4bbc4c9-5036-bb52-53bb-2dd1e420b5ed';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-026","Name":"SRINIVASAN.V","EmployeeType":"Permanent","Grade":"Executive","Department":"Fabrication","Skill":"Production/Fabrication","Designation":"FABRICATOR","Roles":["PRODUCTION_OPERATOR"]}'
WHERE "Id" = 'd85900eb-e0a2-9ac2-9298-7bbef29480e7';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-033","Name":"BLESSON PAUL","EmployeeType":"Permanent","Grade":"Executive","Department":"Electrical","Skill":"Junior/Assistant","Designation":"JUNIOR ENGINEER","Roles":["JUNIOR_ENGINEER"]}'
WHERE "Id" = 'e2bb043e-cfe0-c4a1-1a63-53097f1ebea4';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-019","Name":"RANJITH. R","EmployeeType":"Permanent","Grade":"Executive","Department":"Design","Skill":"Engineer/Technical","Designation":"DESIGN ENGINEER","Roles":["DESIGN_ENGINEER"]}'
WHERE "Id" = 'f03f9db4-a89a-7d11-960a-43eb702e3439';

INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', 'ELECTRICAL', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Electrical', '1c67b401-c1d8-7d42-e3f9-82217720202f', NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('6cc581c8-7385-3438-88a4-9ddd9f3552c9', 'PLC_LABVIEW', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'PLC/LabVIEW', '1c67b401-c1d8-7d42-e3f9-82217720202f', NULL, NULL, 0);
INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', 'REFRIGERATION', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Refrigeration', '1c67b401-c1d8-7d42-e3f9-82217720202f', NULL, NULL, 0);

UPDATE advance.employees SET "DepartmentId" = '284d56ef-2605-02b0-5ac6-9697daa4242a', "DesignationId" = 'b6bd22cd-d47c-d8b2-daff-19a3d7c33d5d'
WHERE "Id" = '04a820d0-3213-a6c2-9ea1-9a5180efcf37';

UPDATE advance.employees SET "DepartmentId" = '2243fe97-0335-bcf7-af29-8c0d5e0bac25'
WHERE "Id" = '1577c211-a6ed-b6ee-d206-5461ad52c428';

UPDATE advance.employees SET "DepartmentId" = '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc'
WHERE "Id" = '20f22ccf-a178-a29e-0a35-7671ff2a2bab';

UPDATE advance.employees SET "DepartmentId" = '45f7f136-f943-7fbf-c4d3-1b588f7faf71', "DesignationId" = '9865dd7b-b329-ccef-54cd-67bdc1cfdd27'
WHERE "Id" = '22a9f52a-db35-3ab5-0115-5e399bfbf4b2';

UPDATE advance.employees SET "DepartmentId" = '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc'
WHERE "Id" = '26c37705-e799-8708-119b-1227908d5e0f';

UPDATE advance.employees SET "DepartmentId" = '6cc581c8-7385-3438-88a4-9ddd9f3552c9'
WHERE "Id" = '277fd621-865d-2823-1b5c-e13a9c36eb2a';

UPDATE advance.employees SET "DepartmentId" = '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc'
WHERE "Id" = '294a2d76-6b76-66d0-76ce-e8d12c02f0c7';

UPDATE advance.employees SET "DepartmentId" = 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64'
WHERE "Id" = '348345c2-1342-5b69-a85a-28d878cd75c6';

UPDATE advance.employees SET "DepartmentId" = '2243fe97-0335-bcf7-af29-8c0d5e0bac25'
WHERE "Id" = '3edaa7c0-f393-cb3e-fb1e-e2071cbf2178';

UPDATE advance.employees SET "DepartmentId" = '6cc581c8-7385-3438-88a4-9ddd9f3552c9'
WHERE "Id" = '41ff2ffb-081e-4600-7680-eef1ef81c01e';

UPDATE advance.employees SET "DepartmentId" = '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc'
WHERE "Id" = '45f0c876-d996-210a-67b3-993b7502d3e5';

UPDATE advance.employees SET "DepartmentId" = '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc'
WHERE "Id" = '48f8c731-7101-d7ff-6605-6b8f283718b1';

UPDATE advance.employees SET "DepartmentId" = 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64'
WHERE "Id" = '50a5b3a3-aa3a-8269-a283-149d2a69cf8a';

UPDATE advance.employees SET "DepartmentId" = '6456dcf7-dad1-598a-f332-ad31dbfc907c', "DesignationId" = '35936fb3-4fc0-4757-268f-c467720e39fa'
WHERE "Id" = '5fdedc5a-1740-164c-04e9-3c6f2db5417c';

UPDATE advance.employees SET "DesignationId" = 'e4bec48d-a248-c13d-a71a-00a2dd40e35e'
WHERE "Id" = '64382325-5125-141e-057e-7ee3f30b2bd3';

UPDATE advance.employees SET "DepartmentId" = 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64'
WHERE "Id" = '85b9da5c-cf3b-6217-593f-4b8e206bfa7a';

UPDATE advance.employees SET "DepartmentId" = '8d38e3c9-d05b-270c-cec0-448e0020e2dc'
WHERE "Id" = '889f9bdc-f246-e914-410d-7102ad10e31d';

UPDATE advance.employees SET "DepartmentId" = '97353e2b-c03c-03ad-dad5-07e697b6429f'
WHERE "Id" = '93216afd-a239-3124-c23e-32d1ff8a8cee';

UPDATE advance.employees SET "DepartmentId" = '8d38e3c9-d05b-270c-cec0-448e0020e2dc'
WHERE "Id" = '9cb99d4e-f1a7-7c9b-62e4-dd838db62c91';

UPDATE advance.employees SET "DepartmentId" = '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', "DesignationId" = '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa'
WHERE "Id" = 'a8ffe255-91ff-3c05-8f9f-dfa21826f2d5';

UPDATE advance.employees SET "DepartmentId" = 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64'
WHERE "Id" = 'b04acf39-5c81-d23c-89e6-9266d39b0be6';

UPDATE advance.employees SET "DepartmentId" = 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64'
WHERE "Id" = 'b1d0d0fb-27b8-e8db-1b03-023c32c74dc9';

UPDATE advance.employees SET "DepartmentId" = '6456dcf7-dad1-598a-f332-ad31dbfc907c'
WHERE "Id" = 'b42a0911-dc25-c491-e26f-b87a7512a0ed';

UPDATE advance.employees SET "DepartmentId" = 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64'
WHERE "Id" = 'bc8570aa-774c-9c38-9b42-ddf8599758f0';

UPDATE advance.employees SET "DepartmentId" = '97353e2b-c03c-03ad-dad5-07e697b6429f', "DesignationId" = 'f8b60dea-0ce6-fa0b-f56e-a7e01058bfc6'
WHERE "Id" = 'be7613f2-52e8-5537-06b2-3e25de92c230';

UPDATE advance.employees SET "DepartmentId" = '05f64e3a-d2a5-1c89-cf9e-667e367e8dae'
WHERE "Id" = 'c175d954-417c-1d34-435c-8a5dce05ac78';

UPDATE advance.employees SET "DepartmentId" = '2243fe97-0335-bcf7-af29-8c0d5e0bac25'
WHERE "Id" = 'e2815b6b-d417-6f86-177b-fb4fc46a6045';

UPDATE advance.employees SET "DepartmentId" = '6456dcf7-dad1-598a-f332-ad31dbfc907c'
WHERE "Id" = 'e7bd4851-c9ba-68e9-a21f-e8583cb82642';

UPDATE advance.employees SET "DepartmentId" = 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64'
WHERE "Id" = 'eca6a631-ef87-cb26-dbd3-5535a950d37f';

UPDATE advance.employees SET "DepartmentId" = '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc'
WHERE "Id" = 'f1dbc4aa-d567-616d-5e5c-63fd8f049e68';

UPDATE advance.employees SET "DepartmentId" = '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', "DesignationId" = '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa'
WHERE "Id" = 'fa72ea80-86c0-5f25-f12c-721e76c1daac';

UPDATE advance.employees SET "DepartmentId" = '05f64e3a-d2a5-1c89-cf9e-667e367e8dae'
WHERE "Id" = 'ff338b63-0eab-59d7-56b1-525e1bedfffd';

INSERT INTO advance.company_gst_registrations ("Id", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "Gstin", "IsActive", "IsPrimary", "RegisteredLegalName", "RegistrationType", "StateCode", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('71000000-0000-0000-0000-000000000001', '70000000-0000-0000-0000-000000000002', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, '33APRPA5532K1ZU', TRUE, TRUE, 'Sri Easwari Scientific Solution', 'PROPRIETORSHIP', '33', NULL, NULL, 0);
INSERT INTO advance.company_gst_registrations ("Id", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "Gstin", "IsActive", "IsPrimary", "RegisteredLegalName", "RegistrationType", "StateCode", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('71000000-0000-0000-0000-000000000002', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, '33ABACS5491H1ZA', TRUE, TRUE, 'Sri Easwari Scientific Solution Private Limited', 'PRIVATE_LIMITED', '33', NULL, NULL, 0);

INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('01dab17d-9319-cd16-cbdd-46bf6775c26f', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-026', '4f2518af-f9b1-98ce-fa4b-125f1034e56e', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('06a2e1b3-ab9f-111d-e58b-a9a53ce9fa66', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-021', 'c175d954-417c-1d34-435c-8a5dce05ac78', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('0733e0ff-7fbf-ca6c-46c1-3c84c796abd9', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-036', 'eca6a631-ef87-cb26-dbd3-5535a950d37f', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('08659973-5015-fc72-ac84-7779fa1652d4', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-020', 'ff338b63-0eab-59d7-56b1-525e1bedfffd', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('167dd4ef-278a-ab72-c008-0011bca78f90', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-009', '04a820d0-3213-a6c2-9ea1-9a5180efcf37', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('1e8916d1-240f-914a-f27d-45599e1f66af', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-038', '277fd621-865d-2823-1b5c-e13a9c36eb2a', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('203ffe6d-883a-51d5-2a89-0b0746fcdd57', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-029', '50a5b3a3-aa3a-8269-a283-149d2a69cf8a', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('2cc6228b-14e1-2932-7e1e-3d15c943e3ad', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-022', '48f8c731-7101-d7ff-6605-6b8f283718b1', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('2d832e00-8934-5852-dd8c-36f9f8cfe987', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-023', '2cee437e-777d-514a-0fe0-4299dee7df7d', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('37d7669d-ba95-55e0-7eb0-3259ac580089', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-033', 'a8ffe255-91ff-3c05-8f9f-dfa21826f2d5', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('3a607681-a91c-9635-2da4-d511dc6ce4ff', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-001', '3543a705-924a-6599-23be-fb9730a93f06', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('3b958aeb-2bcb-23af-e872-33fd47555240', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-011', '9cb99d4e-f1a7-7c9b-62e4-dd838db62c91', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('3e672d84-b803-74f4-c977-9738a8552abd', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-012', 'be7613f2-52e8-5537-06b2-3e25de92c230', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('4f0ca407-d970-50c5-2fc3-4ed733da00f1', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-008', '22a9f52a-db35-3ab5-0115-5e399bfbf4b2', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('50ee1991-f2fe-8191-3c13-3538a6f505b2', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-004', '889f9bdc-f246-e914-410d-7102ad10e31d', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('52b75cf0-d0ef-8a4f-a011-e0fcd6b9e55b', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-016', '3edaa7c0-f393-cb3e-fb1e-e2071cbf2178', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('5ce8c755-8822-8b4a-7168-ca1add2b5f07', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-005', '64382325-5125-141e-057e-7ee3f30b2bd3', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('7854e746-5fec-ea3e-fa80-5df3cc9fa60e', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-010', '45f0c876-d996-210a-67b3-993b7502d3e5', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('78ab9a1f-3da2-ce4e-712c-35f8b4a5b5b2', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-003', 'bc8570aa-774c-9c38-9b42-ddf8599758f0', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('796e7d12-2446-0107-2d80-cb21840e83b3', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-017', 'f1dbc4aa-d567-616d-5e5c-63fd8f049e68', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('7a419fa7-2d02-d433-5df8-ec0b793043fa', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-014', '93216afd-a239-3124-c23e-32d1ff8a8cee', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('80474b04-39ce-1525-bbd3-d645513b3aec', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-019', '1577c211-a6ed-b6ee-d206-5461ad52c428', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('89ab728e-a4ec-232f-869d-3b8856ff28b4', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-006', 'fa72ea80-86c0-5f25-f12c-721e76c1daac', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('8a8fe8bf-afbd-81bd-bf25-92b297a09ae2', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-028', '348345c2-1342-5b69-a85a-28d878cd75c6', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('8b50547e-3597-3c18-9e5c-3347f2e37d00', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-034', 'b04acf39-5c81-d23c-89e6-9266d39b0be6', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('a14089fc-1ee9-eb82-e719-3828be3d8b9a', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-024', '26c37705-e799-8708-119b-1227908d5e0f', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('a7283fec-5cd5-4eec-212e-7d00f8091d3a', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-037', '85b9da5c-cf3b-6217-593f-4b8e206bfa7a', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('b7a09435-91ea-db50-95b6-75b64e23c0fa', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-025', 'b6292258-84c4-225f-2571-dc1bc204edb7', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('bb221422-6392-8d4e-f59e-5431fbef2333', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-015', 'e2815b6b-d417-6f86-177b-fb4fc46a6045', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('c20dcc9d-b92a-4003-b108-9601f255f809', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-035', '3c926af7-c052-2a69-5cad-b961650d230b', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('c9b7930d-0f6f-e7d7-67d9-3c2b76473e0d', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-018', '20f22ccf-a178-a29e-0a35-7671ff2a2bab', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('d04b56a2-c8ee-ed41-92ce-09bf3c1dc1e0', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-032', '41ff2ffb-081e-4600-7680-eef1ef81c01e', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('de97d579-5c64-379a-ff16-1f40799fab1c', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-030', '294a2d76-6b76-66d0-76ce-e8d12c02f0c7', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('e0ddd259-81c9-718d-9f10-69a1e29dee9c', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-007', '5fdedc5a-1740-164c-04e9-3c6f2db5417c', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('e0e5f9c0-94d3-c3e9-a716-45251b7a2c25', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-002', '73a13e4d-73b6-b86f-738b-71261ad69e71', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('e1b1888f-4a70-24e2-56f8-5c5edea81e18', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-013', '131cf31d-0cc0-9b70-da2e-89463c49619e', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('f3b05270-2438-4480-54a8-7952f6e6f51c', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-039', 'b1d0d0fb-27b8-e8db-1b03-023c32c74dc9', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('f7f06b75-3470-66ad-a0dd-e936ca7fa8bd', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-027', 'e7bd4851-c9ba-68e9-a21f-e8583cb82642', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_company_assignments ("Id", "AssignmentType", "CompanyId", "CompanySiteId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeCode", "EmployeeId", "EmploymentType", "IsActive", "PayrollEmployeeId", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('f83a54c8-8777-6eb0-d642-1f83820c5921', 'PAYROLL', '70000000-0000-0000-0000-000000000001', NULL, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', DATE '2026-08-24', NULL, 'SESS-031', 'b42a0911-dc25-c491-e26f-b87a7512a0ed', 'Permanent', TRUE, NULL, 'ACTIVE', NULL, NULL, 0);

INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('02702296-3863-8644-c306-ddc2f49e5cca', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, 'ff338b63-0eab-59d7-56b1-525e1bedfffd', 'REV866 approved initial mapping', '0a769058-1bab-5087-26b9-d33415b000e5', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('068427ee-6fc5-8182-b61c-24b2b3187867', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, 'b1d0d0fb-27b8-e8db-1b03-023c32c74dc9', 'REV866 approved initial mapping', '80c408fe-3f95-ba8a-54b2-d0eee2374adf', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('157d94ff-a39e-3fa4-3a54-f6f8d05cab62', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '22a9f52a-db35-3ab5-0115-5e399bfbf4b2', 'REV866 approved initial mapping', '327c54ec-84f0-0eca-2123-cb9068b2c13b', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('18da9f7c-3049-52e3-b76c-c4238cedb213', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '2cee437e-777d-514a-0fe0-4299dee7df7d', 'REV866 approved initial mapping', 'e177df2e-c5f3-adb4-fbc9-11973c0d68ac', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('1b5c6764-7dcd-6f19-0097-61b87603b5eb', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '3c926af7-c052-2a69-5cad-b961650d230b', 'REV866 approved initial mapping', '97cf9b49-ae40-a8a5-e20b-acc199601716', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('205cd7e9-b79c-4600-f9c9-561e15e2be9f', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '41ff2ffb-081e-4600-7680-eef1ef81c01e', 'REV866 approved initial mapping', '5108e629-77d1-c7f2-90ee-cca43777210e', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('25c10527-28a2-e600-82d2-3b1b767af269', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, 'f1dbc4aa-d567-616d-5e5c-63fd8f049e68', 'REV866 approved initial mapping', 'c4133420-c386-9452-93a7-484e18105372', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('261e0ee9-c1a4-6f18-a3fc-461add06916b', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, 'b6292258-84c4-225f-2571-dc1bc204edb7', 'REV866 approved initial mapping', '97cf9b49-ae40-a8a5-e20b-acc199601716', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('270a811f-0564-a4b0-8f4f-0b47118d3134', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '9cb99d4e-f1a7-7c9b-62e4-dd838db62c91', 'REV866 approved initial mapping', 'c4133420-c386-9452-93a7-484e18105372', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('2e2b854a-f965-2a71-21c3-96738e3cb840', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '85b9da5c-cf3b-6217-593f-4b8e206bfa7a', 'REV866 approved initial mapping', '80c408fe-3f95-ba8a-54b2-d0eee2374adf', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('30e7eac7-1101-ffde-70c0-6edd20ed4c01', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, 'bc8570aa-774c-9c38-9b42-ddf8599758f0', 'REV866 approved initial mapping', '80c408fe-3f95-ba8a-54b2-d0eee2374adf', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('3b51f513-0e8e-7677-b138-19bc0d9c4150', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, 'b04acf39-5c81-d23c-89e6-9266d39b0be6', 'REV866 approved initial mapping', '80c408fe-3f95-ba8a-54b2-d0eee2374adf', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('3b6fe413-e8d3-3c0e-52a0-2425db151f48', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '64382325-5125-141e-057e-7ee3f30b2bd3', 'REV866 approved initial mapping', '97cf9b49-ae40-a8a5-e20b-acc199601716', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('4a1b90a5-9797-0fd0-0e6d-58785e981854', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '93216afd-a239-3124-c23e-32d1ff8a8cee', 'REV866 approved initial mapping', '23e39915-e02a-82aa-18f9-10ea329fad00', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('53f3f0b9-de8b-4119-3668-01c751a3d52a', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, 'c175d954-417c-1d34-435c-8a5dce05ac78', 'REV866 approved initial mapping', '81701251-a033-5850-5bb4-f4bf1b16920b', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('5554a0f5-85f0-d477-ea7b-f3a6cd1ed121', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, 'b42a0911-dc25-c491-e26f-b87a7512a0ed', 'REV866 approved initial mapping', '003197d6-a07b-a658-1014-0d84c68d2355', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('67461916-89e1-fe39-e460-39d2d341d242', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '4f2518af-f9b1-98ce-fa4b-125f1034e56e', 'REV866 approved initial mapping', '97cf9b49-ae40-a8a5-e20b-acc199601716', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('6c56b8eb-3f8a-4940-df22-5e8002b262da', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '294a2d76-6b76-66d0-76ce-e8d12c02f0c7', 'REV866 approved initial mapping', '1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('6d4b74b6-5611-c8f5-0ba5-48be51fd6996', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '48f8c731-7101-d7ff-6605-6b8f283718b1', 'REV866 approved initial mapping', '1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('87dd003b-f6f7-fb19-9f89-c395683c8fa0', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '131cf31d-0cc0-9b70-da2e-89463c49619e', 'REV866 approved initial mapping', '97cf9b49-ae40-a8a5-e20b-acc199601716', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('8b4828cc-bbf0-05df-0f27-a3d789052b82', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '1577c211-a6ed-b6ee-d206-5461ad52c428', 'REV866 approved initial mapping', 'd52152b0-05b4-18f9-4201-1f7066af4c76', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('8b8c5e6b-cc4d-4386-50a3-32fb3d776860', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '5fdedc5a-1740-164c-04e9-3c6f2db5417c', 'REV866 approved initial mapping', '003197d6-a07b-a658-1014-0d84c68d2355', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('8c3e4b9b-6be9-9fa3-9c81-fa47f23b5818', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, 'fa72ea80-86c0-5f25-f12c-721e76c1daac', 'REV866 approved initial mapping', 'c4133420-c386-9452-93a7-484e18105372', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('8c7733c4-1a45-970b-a81b-dbf5aa781ef0', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '348345c2-1342-5b69-a85a-28d878cd75c6', 'REV866 approved initial mapping', '80c408fe-3f95-ba8a-54b2-d0eee2374adf', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('8ee5108f-6a19-af67-0562-ee708ebd6a05', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, 'e2815b6b-d417-6f86-177b-fb4fc46a6045', 'REV866 approved initial mapping', 'd52152b0-05b4-18f9-4201-1f7066af4c76', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('98804443-54b0-2474-7acb-ffc54410e33e', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '3edaa7c0-f393-cb3e-fb1e-e2071cbf2178', 'REV866 approved initial mapping', 'd52152b0-05b4-18f9-4201-1f7066af4c76', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('9ac81cf0-423b-97a8-08e7-d3797a7410c7', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '04a820d0-3213-a6c2-9ea1-9a5180efcf37', 'REV866 approved initial mapping', 'c4133420-c386-9452-93a7-484e18105372', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('9e1e368d-3c82-60cf-f522-7758004d3e88', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '3543a705-924a-6599-23be-fb9730a93f06', 'REV866 approved initial mapping', '45eb9032-3689-8526-caee-41db0e7e2644', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('a260b451-c377-907d-ba80-fb03af55ebc0', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '277fd621-865d-2823-1b5c-e13a9c36eb2a', 'REV866 approved initial mapping', '4dd5b229-c6a0-e45e-dd6c-ef6529087d05', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('a2bc7e87-56b4-0478-d29d-c329f7eb060a', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '26c37705-e799-8708-119b-1227908d5e0f', 'REV866 approved initial mapping', '1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('a7552ac8-23f1-9ed4-6de8-669d08054e0a', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, 'be7613f2-52e8-5537-06b2-3e25de92c230', 'REV866 approved initial mapping', '8481d263-cb63-6bc1-76ac-b4c2a56fc1c5', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('a79e4f09-112d-57e5-4f17-00066b3e6d22', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, 'be7613f2-52e8-5537-06b2-3e25de92c230', 'REV866 approved initial mapping', '46899b83-f5d7-793d-f008-5b15bcf06b17', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('ad9892ac-7d0f-89fc-8aec-be5f65860079', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, 'a8ffe255-91ff-3c05-8f9f-dfa21826f2d5', 'REV866 approved initial mapping', 'c4133420-c386-9452-93a7-484e18105372', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('ae3c6d06-5d8c-fa88-ae24-4dcf2ddbfacb', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '889f9bdc-f246-e914-410d-7102ad10e31d', 'REV866 approved initial mapping', '07d53aa2-c266-4802-4786-9723d800e29d', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('babde2dc-2cd6-83b4-eea4-84c5886b436e', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '50a5b3a3-aa3a-8269-a283-149d2a69cf8a', 'REV866 approved initial mapping', '80c408fe-3f95-ba8a-54b2-d0eee2374adf', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('c3aa8842-31de-0d93-71b8-ba5e8895a534', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '45f0c876-d996-210a-67b3-993b7502d3e5', 'REV866 approved initial mapping', '1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('d278c271-c2e2-00a7-a70b-ca058dc2af0e', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, 'eca6a631-ef87-cb26-dbd3-5535a950d37f', 'REV866 approved initial mapping', '80c408fe-3f95-ba8a-54b2-d0eee2374adf', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('e6cf6f13-4f3a-56c8-dbed-608f3b596b6e', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, 'e7bd4851-c9ba-68e9-a21f-e8583cb82642', 'REV866 approved initial mapping', '003197d6-a07b-a658-1014-0d84c68d2355', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('ec95b2c0-4bb6-9b59-3e5e-6fd16ce97ba3', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '73a13e4d-73b6-b86f-738b-71261ad69e71', 'REV866 approved initial mapping', '03325f4f-c6d4-b3f3-f4b3-11b728c275da', NULL, NULL, 0);
INSERT INTO advance.employee_role_assignments ("Id", "ApprovalStatus", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('f03cb56e-0797-3443-b51a-d28205fcdfa7', 'SeedApproved', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', DATE '2026-08-08', NULL, '20f22ccf-a178-a29e-0a35-7671ff2a2bab', 'REV866 approved initial mapping', '1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3', NULL, NULL, 0);

ALTER TABLE advance.organization_policies DISABLE TRIGGER trg_rev869a_policy_version_guard;

UPDATE advance.organization_policies SET "CompanyId" = '70000000-0000-0000-0000-000000000001', "OrganizationId" = 'SESS_PVT_LTD'
WHERE "Id" = '50000000-0000-0000-0000-000000000001';

UPDATE advance.organization_policies SET "CompanyId" = '70000000-0000-0000-0000-000000000001', "OrganizationId" = 'SESS_PVT_LTD'
WHERE "Id" = '50000000-0000-0000-0000-000000000002';

ALTER TABLE advance.organization_policies ENABLE TRIGGER trg_rev869a_policy_version_guard;

UPDATE advance.purchase_transaction_approval_policies SET "CompanyId" = '70000000-0000-0000-0000-000000000001', "OrganizationId" = 'SESS_PVT_LTD'
WHERE "Id" = '0e6e49ea-95c5-86dd-1c23-18d61c50f4c1';

UPDATE advance.purchase_transaction_approval_policies SET "CompanyId" = '70000000-0000-0000-0000-000000000001', "OrganizationId" = 'SESS_PVT_LTD'
WHERE "Id" = 'd7b12d20-a4be-c916-9f5e-de2245510b91';

UPDATE advance.purchase_transaction_approval_policies SET "CompanyId" = '70000000-0000-0000-0000-000000000001', "OrganizationId" = 'SESS_PVT_LTD'
WHERE "Id" = 'f9505a0c-182b-7627-52f4-1197a29e4c16';

INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('0022b9f4-a275-a942-4d04-1444e41d49b2', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'c9b7930d-0f6f-e7d7-67d9-3c2b76473e0d', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('00909883-5aba-2b78-3231-41db468b93fa', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, 'a7283fec-5cd5-4eec-212e-7d00f8091d3a', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('02ffb4e3-80b1-a433-adc4-66ad5d124c24', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, '7854e746-5fec-ea3e-fa80-5df3cc9fa60e', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('06c14289-646e-ee4d-66f4-f1a0c8791b4d', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'c20dcc9d-b92a-4003-b108-9601f255f809', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('08671021-7688-cfaa-4864-2c8c0593ffb0', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '203ffe6d-883a-51d5-2a89-0b0746fcdd57', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('099bcda1-ea0d-6593-5d01-2d2112aeea41', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '3b958aeb-2bcb-23af-e872-33fd47555240', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('0ac61e8a-7031-bef7-e752-a116679d653a', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'e1b1888f-4a70-24e2-56f8-5c5edea81e18', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('0d4907c7-98c4-b2c6-9c3e-a6a4cbc0d806', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'c9b7930d-0f6f-e7d7-67d9-3c2b76473e0d', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('0d6d0b4e-d712-5728-046d-a0c4a6fd0917', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, '2cc6228b-14e1-2932-7e1e-3d15c943e3ad', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('0ee6df57-a9ff-ae46-d54c-d8b5e9032406', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, '2cc6228b-14e1-2932-7e1e-3d15c943e3ad', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('0f0c4096-29fa-c516-d7cc-1d24974dd09b', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '37d7669d-ba95-55e0-7eb0-3259ac580089', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('0f43781a-b773-e3d0-ebe8-bf2db1310ce2', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '8b50547e-3597-3c18-9e5c-3347f2e37d00', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('106f312e-faea-f474-8389-32bbddb09886', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '3b958aeb-2bcb-23af-e872-33fd47555240', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('13d60ce1-da99-5491-1e21-745b840c2a1b', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '2243fe97-0335-bcf7-af29-8c0d5e0bac25', '4c9baa15-c3d4-6b41-d040-f354c5cff307', DATE '2026-08-24', NULL, 'bb221422-6392-8d4e-f59e-5431fbef2333', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('14c56b61-ae28-3141-025e-b27c8ba1ba3b', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '78ab9a1f-3da2-ce4e-712c-35f8b4a5b5b2', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('15a415a2-d45c-be42-23ce-94602d83152f', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, '01dab17d-9319-cd16-cbdd-46bf6775c26f', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('182438b5-1927-be9d-f729-8a67be4876f2', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'e1b1888f-4a70-24e2-56f8-5c5edea81e18', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('18ed912c-1d53-97a2-ccd8-7ec32cfb4bc4', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, '2cc6228b-14e1-2932-7e1e-3d15c943e3ad', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('1a740f89-ee33-0339-4d16-50f32a361baa', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'c20dcc9d-b92a-4003-b108-9601f255f809', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('1a869916-4197-722e-713f-eb6cab4262b9', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', '075fb64f-355a-ee74-517b-6b9c6da0f8db', DATE '2026-08-24', NULL, 'd04b56a2-c8ee-ed41-92ce-09bf3c1dc1e0', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('1beef3cd-2c30-7f5b-4abc-ffeb2eab832d', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, '5ce8c755-8822-8b4a-7168-ca1add2b5f07', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('1d11e37f-ed6b-0a0a-0b76-d2b8bb0cbcaa', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, 'f3b05270-2438-4480-54a8-7952f6e6f51c', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('1d37c9ef-86a5-f0dc-7a7c-e125faf4ecc9', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'c9b7930d-0f6f-e7d7-67d9-3c2b76473e0d', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('1f7eb810-1c3d-6f47-6e2f-2a67a1418096', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, '2cc6228b-14e1-2932-7e1e-3d15c943e3ad', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('200b3757-2aa0-49ac-6d3f-5e05fbc4d1ac', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '89ab728e-a4ec-232f-869d-3b8856ff28b4', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('209811a1-1e49-23b4-3268-a1ca3a8166ca', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '8a8fe8bf-afbd-81bd-bf25-92b297a09ae2', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('218e17d5-8efc-4e9b-0ea0-52ea2c568575', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'a14089fc-1ee9-eb82-e719-3828be3d8b9a', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('235b8c76-a7f4-3190-56e6-5ef5d9e5ed0f', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '97353e2b-c03c-03ad-dad5-07e697b6429f', 'f8b60dea-0ce6-fa0b-f56e-a7e01058bfc6', DATE '2026-08-24', NULL, '7a419fa7-2d02-d433-5df8-ec0b793043fa', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('2386aad7-0645-1315-6628-9df592576b7b', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, '7854e746-5fec-ea3e-fa80-5df3cc9fa60e', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('251c2f75-c39a-76ee-ed3b-399767503ec1', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '0733e0ff-7fbf-ca6c-46c1-3c84c796abd9', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('27820f28-b570-1fc7-67c8-a5858f92bf9c', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '3b958aeb-2bcb-23af-e872-33fd47555240', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('2976c383-447d-ad1c-89f8-da6e1066236d', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, '5ce8c755-8822-8b4a-7168-ca1add2b5f07', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('29e77993-9495-40e0-4cc5-7d84055523ec', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'c7775052-f0a9-27e3-f259-746120a113a6', DATE '2026-08-24', NULL, '50ee1991-f2fe-8191-3c13-3538a6f505b2', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('2aee95ce-62c4-6f55-53db-5ec52c043909', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'c20dcc9d-b92a-4003-b108-9601f255f809', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('2b51db23-d122-31bc-e754-9cefdd94ed43', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, '01dab17d-9319-cd16-cbdd-46bf6775c26f', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('2d2d7422-31c9-f67c-afd3-fdb8f3ef901c', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, '01dab17d-9319-cd16-cbdd-46bf6775c26f', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('31f38455-d94b-a29e-b004-b2db4bc79dee', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, '01dab17d-9319-cd16-cbdd-46bf6775c26f', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('31f6f90b-3214-17e1-05e9-60ca8157895e', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, '01dab17d-9319-cd16-cbdd-46bf6775c26f', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('32858370-becf-f784-bfe3-36337d2691c3', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '37d7669d-ba95-55e0-7eb0-3259ac580089', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('334cf3c8-7ad2-15b2-2576-0cabbfc26014', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, 'f3b05270-2438-4480-54a8-7952f6e6f51c', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('33e29f37-7a37-05d8-cdd7-efcf99f3fd2e', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, '2cc6228b-14e1-2932-7e1e-3d15c943e3ad', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('3583bcd7-a95c-7b94-47f5-87b72dd9279b', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '2243fe97-0335-bcf7-af29-8c0d5e0bac25', '4c9baa15-c3d4-6b41-d040-f354c5cff307', DATE '2026-08-24', NULL, '80474b04-39ce-1525-bbd3-d645513b3aec', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('37a45e66-5f57-ff2e-a834-4e441d1f7365', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'c9b7930d-0f6f-e7d7-67d9-3c2b76473e0d', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('37aea340-cc1b-dd18-81bd-d333c0d78b2d', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'de97d579-5c64-379a-ff16-1f40799fab1c', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('39275785-70f9-5a02-7304-68bd941381aa', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '0733e0ff-7fbf-ca6c-46c1-3c84c796abd9', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('392e17ac-a7b6-ed26-a9b0-103b10f8b8a4', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '78ab9a1f-3da2-ce4e-712c-35f8b4a5b5b2', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('3ad917d3-b645-d43b-98e4-88349cf8b545', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '8b50547e-3597-3c18-9e5c-3347f2e37d00', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('3aff07c7-2e44-a3cb-cf70-c17542511884', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', 'b9de3ebc-27c8-8fd6-85b2-d8bc916e6dfa', DATE '2026-08-24', NULL, '1e8916d1-240f-914a-f27d-45599e1f66af', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('3bfc3271-4bc0-d411-1a91-05692ace8a9b', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '796e7d12-2446-0107-2d80-cb21840e83b3', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('3d29039c-643b-094e-b88a-669004ced668', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '97353e2b-c03c-03ad-dad5-07e697b6429f', 'f8b60dea-0ce6-fa0b-f56e-a7e01058bfc6', DATE '2026-08-24', NULL, '3e672d84-b803-74f4-c977-9738a8552abd', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('3ebc756a-15cc-085a-ed8a-77262ff1fc62', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, '7854e746-5fec-ea3e-fa80-5df3cc9fa60e', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('3f5c4f8d-2047-f915-9be4-3b21726b4d85', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'c9b7930d-0f6f-e7d7-67d9-3c2b76473e0d', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('3f8185ee-853e-aeeb-bddc-77f7bf8a310e', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', 'b6bd22cd-d47c-d8b2-daff-19a3d7c33d5d', DATE '2026-08-24', NULL, '167dd4ef-278a-ab72-c008-0011bca78f90', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('41039888-0e90-b82c-3741-068034168588', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, 'a7283fec-5cd5-4eec-212e-7d00f8091d3a', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('417eb4e0-51a9-b655-1e4d-acbaec52a882', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '37d7669d-ba95-55e0-7eb0-3259ac580089', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('42f7916c-479d-b5d1-7468-3a5c6d196638', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '0733e0ff-7fbf-ca6c-46c1-3c84c796abd9', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('42f84275-0e6e-ee75-fe3f-5a854adf8ecd', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'de97d579-5c64-379a-ff16-1f40799fab1c', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('4344c10f-6f3c-f75f-b388-976702c32021', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', 'b9de3ebc-27c8-8fd6-85b2-d8bc916e6dfa', DATE '2026-08-24', NULL, '1e8916d1-240f-914a-f27d-45599e1f66af', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('45d62f82-1ce7-c28d-6bb5-981a3dc0ef66', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '203ffe6d-883a-51d5-2a89-0b0746fcdd57', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('4748b1c2-5644-b98a-2a72-0e2b628632b4', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '8a8fe8bf-afbd-81bd-bf25-92b297a09ae2', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('47e07e35-888d-5028-ad15-9f6a04c1833e', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'c20dcc9d-b92a-4003-b108-9601f255f809', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('49aa21b0-b84b-800a-e738-52179695d36f', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '78ab9a1f-3da2-ce4e-712c-35f8b4a5b5b2', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('4aa21d6f-fb3d-29c7-4f16-569b6a7df441', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'c20dcc9d-b92a-4003-b108-9601f255f809', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('4baf6c0a-ef72-a2bf-1475-3ec886cb4c27', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'a14089fc-1ee9-eb82-e719-3828be3d8b9a', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('4dec21f3-15ad-389c-cf03-6f1cba193edd', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'a14089fc-1ee9-eb82-e719-3828be3d8b9a', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('4f67c576-c957-aa7c-acfc-f030e40208a0', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, '2cc6228b-14e1-2932-7e1e-3d15c943e3ad', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('4fd074af-f9b2-c7cf-4b43-beb471854267', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'de97d579-5c64-379a-ff16-1f40799fab1c', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('50097a28-1d9d-b9a7-95e8-03cc811d288b', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'b7a09435-91ea-db50-95b6-75b64e23c0fa', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('5049777e-ca74-37f0-c7d0-5a7ba509f982', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', '075fb64f-355a-ee74-517b-6b9c6da0f8db', DATE '2026-08-24', NULL, 'd04b56a2-c8ee-ed41-92ce-09bf3c1dc1e0', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('5187d00f-22da-28d5-22fe-e517b3641396', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '05f64e3a-d2a5-1c89-cf9e-667e367e8dae', '82783939-c768-2002-5b0e-17db5261eab9', DATE '2026-08-24', NULL, '08659973-5015-fc72-ac84-7779fa1652d4', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('51dc4bec-6371-afc1-0982-5d6a452dd495', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, 'f3b05270-2438-4480-54a8-7952f6e6f51c', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('526593f3-bd17-a747-e9a0-62c3cfd581a2', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '203ffe6d-883a-51d5-2a89-0b0746fcdd57', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('5307aef4-2b25-0d41-1849-28806ca5508d', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'a14089fc-1ee9-eb82-e719-3828be3d8b9a', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('5508dd29-eb29-4c74-9cdb-aab8db2aeb7e', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '0057b580-1cb1-afa2-8328-5afb1162e77e', '086ab1d4-3404-12b7-c35a-4b77737eb97b', DATE '2026-08-24', NULL, '3a607681-a91c-9635-2da4-d511dc6ce4ff', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('5553b3e4-2106-7ea0-03af-2f733ca9e138', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '8a8fe8bf-afbd-81bd-bf25-92b297a09ae2', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('58d2c34f-3df1-55eb-55b1-e6141f853b2e', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, '5ce8c755-8822-8b4a-7168-ca1add2b5f07', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('5ac5b652-ecb4-cc7e-01c7-2362d858ef2f', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'e1b1888f-4a70-24e2-56f8-5c5edea81e18', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('5b62a304-60f6-133d-7323-71fc5714dac1', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '37d7669d-ba95-55e0-7eb0-3259ac580089', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('5bfa697a-e8c0-88ec-0042-d6e32849cec6', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', '075fb64f-355a-ee74-517b-6b9c6da0f8db', DATE '2026-08-24', NULL, 'd04b56a2-c8ee-ed41-92ce-09bf3c1dc1e0', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('5c6487ff-8080-25ff-2602-2aa19c95e5c2', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', 'b6bd22cd-d47c-d8b2-daff-19a3d7c33d5d', DATE '2026-08-24', NULL, '167dd4ef-278a-ab72-c008-0011bca78f90', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('5c9219c2-cde6-63b7-d8d5-8e1e449cb6d0', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6456dcf7-dad1-598a-f332-ad31dbfc907c', '35936fb3-4fc0-4757-268f-c467720e39fa', DATE '2026-08-24', NULL, 'f7f06b75-3470-66ad-a0dd-e936ca7fa8bd', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('60690f13-1d32-0afc-5d10-e7ff581ba8b7', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', '075fb64f-355a-ee74-517b-6b9c6da0f8db', DATE '2026-08-24', NULL, 'd04b56a2-c8ee-ed41-92ce-09bf3c1dc1e0', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('61b0a15c-e93d-0b1a-6381-4061fd1ee29a', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '8a8fe8bf-afbd-81bd-bf25-92b297a09ae2', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('632796e0-ae54-aa56-cc2a-4e2c7bed16ed', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '89ab728e-a4ec-232f-869d-3b8856ff28b4', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('638ba94e-c620-0008-8ff9-930d2ee88c66', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '203ffe6d-883a-51d5-2a89-0b0746fcdd57', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('64224821-baff-823d-ac63-361973357325', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'e1b1888f-4a70-24e2-56f8-5c5edea81e18', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('6a52a325-c2ec-96cc-3647-e345a43b0f36', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'de97d579-5c64-379a-ff16-1f40799fab1c', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('6bc12308-b068-e485-9aa3-3d9e656757e3', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '2243fe97-0335-bcf7-af29-8c0d5e0bac25', '4c9baa15-c3d4-6b41-d040-f354c5cff307', DATE '2026-08-24', NULL, '52b75cf0-d0ef-8a4f-a011-e0fcd6b9e55b', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('7097163f-82c2-2bbc-9cc6-16573098a5cd', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'de97d579-5c64-379a-ff16-1f40799fab1c', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('716e70a9-1a2c-5c48-7b2f-3cf3e513ac57', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '0733e0ff-7fbf-ca6c-46c1-3c84c796abd9', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('717e943e-6054-ffa2-61fd-acf410da2e9a', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, '01dab17d-9319-cd16-cbdd-46bf6775c26f', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('721bb748-b5cd-28cc-452b-7c3079e0d38a', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '8b50547e-3597-3c18-9e5c-3347f2e37d00', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('72959f31-1d13-10f4-efc9-20603e8310c1', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '8a8fe8bf-afbd-81bd-bf25-92b297a09ae2', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('73ccd010-0493-8bba-c46a-0fbed487c99e', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, 'a7283fec-5cd5-4eec-212e-7d00f8091d3a', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('76060fb5-5a73-59e1-ecc0-e88f77822f2b', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'de97d579-5c64-379a-ff16-1f40799fab1c', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('76524219-41f2-2535-59cb-0a22f6f0cca3', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '89ab728e-a4ec-232f-869d-3b8856ff28b4', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('776cd654-11ae-31e7-e7cd-2dfd063a5b00', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '3b958aeb-2bcb-23af-e872-33fd47555240', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('778088f0-fdce-e1a3-5524-3beecec6c4a3', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, 'f3b05270-2438-4480-54a8-7952f6e6f51c', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('78d2ecd9-d827-9138-e25b-82a622834a75', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'a14089fc-1ee9-eb82-e719-3828be3d8b9a', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('7c25d119-5076-6a17-da90-34013c3d0d26', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '37d7669d-ba95-55e0-7eb0-3259ac580089', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('7ca94d85-fa1d-e85a-1c8c-55976229b332', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, '7854e746-5fec-ea3e-fa80-5df3cc9fa60e', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('7ebdbf3f-0716-dac7-4082-824686b04a1f', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'c9b7930d-0f6f-e7d7-67d9-3c2b76473e0d', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('7fba6dba-5f27-8453-1054-b109845f772d', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '8b50547e-3597-3c18-9e5c-3347f2e37d00', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('818e215c-44cd-9984-2703-b2c68714dce0', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, 'f3b05270-2438-4480-54a8-7952f6e6f51c', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('837dc54a-c650-d791-dde3-6c11cad2aa1e', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, 'f3b05270-2438-4480-54a8-7952f6e6f51c', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('87516f58-944e-30e1-a389-2bb011e2c387', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '203ffe6d-883a-51d5-2a89-0b0746fcdd57', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('88f60fa2-2810-9b3f-8502-da81124046a2', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6456dcf7-dad1-598a-f332-ad31dbfc907c', '35936fb3-4fc0-4757-268f-c467720e39fa', DATE '2026-08-24', NULL, 'e0ddd259-81c9-718d-9f10-69a1e29dee9c', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('8b48a8b2-9ef7-f974-44e4-cbccb196133e', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '78ab9a1f-3da2-ce4e-712c-35f8b4a5b5b2', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('8cdab8f0-6aae-19b7-4e1b-0cde36908091', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'b7a09435-91ea-db50-95b6-75b64e23c0fa', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('8d0d6ddf-e07e-e88c-9eeb-ef82914b735c', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, '5ce8c755-8822-8b4a-7168-ca1add2b5f07', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('8e88a8aa-2304-a466-df5a-ebe322ac68db', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '796e7d12-2446-0107-2d80-cb21840e83b3', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('913c4e72-7fae-2b67-306f-e3a1c83026de', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '0057b580-1cb1-afa2-8328-5afb1162e77e', 'a653c7ab-0b15-c0fc-bdcb-8cb6c64bd830', DATE '2026-08-24', NULL, 'e0e5f9c0-94d3-c3e9-a716-45251b7a2c25', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('91a50219-91ec-9725-cf47-b115abda5fac', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', '075fb64f-355a-ee74-517b-6b9c6da0f8db', DATE '2026-08-24', NULL, 'd04b56a2-c8ee-ed41-92ce-09bf3c1dc1e0', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('97881bb0-0389-90fc-026f-f020387b0969', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '89ab728e-a4ec-232f-869d-3b8856ff28b4', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('989cefac-440d-3f14-188b-bf9bbe0396f8', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'c20dcc9d-b92a-4003-b108-9601f255f809', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('991c20d7-e206-c3dd-57f1-a00b84d941c8', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '89ab728e-a4ec-232f-869d-3b8856ff28b4', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('997e77e8-eaae-33dc-1192-23e41aa651d7', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'b6bd22cd-d47c-d8b2-daff-19a3d7c33d5d', DATE '2026-08-24', NULL, '167dd4ef-278a-ab72-c008-0011bca78f90', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('998ef644-f4ef-913b-c51b-1d729fc55acb', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', '96908ceb-4e96-b670-db7e-59b2237f1dec', DATE '2026-08-24', NULL, '2d832e00-8934-5852-dd8c-36f9f8cfe987', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('9998ac69-5044-1cc0-b31c-454f73aa5901', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, '7854e746-5fec-ea3e-fa80-5df3cc9fa60e', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('99b6eb6e-0657-b686-a3d6-707c96dac7ac', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '05f64e3a-d2a5-1c89-cf9e-667e367e8dae', '8e377677-95bb-f0fe-4207-2efaf2b89208', DATE '2026-08-24', NULL, '06a2e1b3-ab9f-111d-e58b-a9a53ce9fa66', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('9c0b4e95-610d-8040-d2fe-f4700fc8ec33', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', 'b9de3ebc-27c8-8fd6-85b2-d8bc916e6dfa', DATE '2026-08-24', NULL, '1e8916d1-240f-914a-f27d-45599e1f66af', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('9cd01e16-f0c1-93a6-6997-03c56919901d', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6456dcf7-dad1-598a-f332-ad31dbfc907c', '35936fb3-4fc0-4757-268f-c467720e39fa', DATE '2026-08-24', NULL, 'f83a54c8-8777-6eb0-d642-1f83820c5921', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('9d4fbba2-2c14-3545-dc4b-8697f3a557a1', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'c20dcc9d-b92a-4003-b108-9601f255f809', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('9f0a4b9c-2c0a-2536-8409-74237fc77c3a', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '45f7f136-f943-7fbf-c4d3-1b588f7faf71', '9865dd7b-b329-ccef-54cd-67bdc1cfdd27', DATE '2026-08-24', NULL, '4f0ca407-d970-50c5-2fc3-4ed733da00f1', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('a0d38550-1ab1-931e-7a88-68d32c560958', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '0733e0ff-7fbf-ca6c-46c1-3c84c796abd9', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('a1ba5e5d-fba3-369a-c63f-478534c5f7a0', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, '01dab17d-9319-cd16-cbdd-46bf6775c26f', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('a20405b9-afc0-f17c-04d1-bc5135126abc', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '796e7d12-2446-0107-2d80-cb21840e83b3', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('a4285e2f-ba53-9228-8e67-3f3c78f00894', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'b7a09435-91ea-db50-95b6-75b64e23c0fa', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('a4a49855-1f29-d7f1-bcd7-90411f20eecd', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', '075fb64f-355a-ee74-517b-6b9c6da0f8db', DATE '2026-08-24', NULL, 'd04b56a2-c8ee-ed41-92ce-09bf3c1dc1e0', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('a4c500ef-0f5c-031d-c175-4c23923d1c65', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', 'b9de3ebc-27c8-8fd6-85b2-d8bc916e6dfa', DATE '2026-08-24', NULL, '1e8916d1-240f-914a-f27d-45599e1f66af', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('a8f81ebf-368c-d3dc-c156-ab7cf211dab3', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, '5ce8c755-8822-8b4a-7168-ca1add2b5f07', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('af1da839-004b-b8ab-9a89-a2e8b8c7695f', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '796e7d12-2446-0107-2d80-cb21840e83b3', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('b0995386-e2bf-4dc7-c68e-c1ea0cc9731c', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '3b958aeb-2bcb-23af-e872-33fd47555240', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('b1245fb3-0749-55cc-8f79-55dee8783f2e', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', 'b9de3ebc-27c8-8fd6-85b2-d8bc916e6dfa', DATE '2026-08-24', NULL, '1e8916d1-240f-914a-f27d-45599e1f66af', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('b18f9988-ad76-9d96-2f28-848300b53330', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '8b50547e-3597-3c18-9e5c-3347f2e37d00', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('b1c85c0f-e631-69f6-5e02-4914341075fa', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, 'a7283fec-5cd5-4eec-212e-7d00f8091d3a', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('b2b41af0-e2d3-b164-db8c-c053152e53b4', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'e1b1888f-4a70-24e2-56f8-5c5edea81e18', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('b2b51384-e3b2-e58f-54b8-480b91d82340', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, 'a7283fec-5cd5-4eec-212e-7d00f8091d3a', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('b3477a9a-74ae-8ae2-e536-cd6e7da94d9c', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '8b50547e-3597-3c18-9e5c-3347f2e37d00', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('b455fadf-a5c1-932d-5675-f5cac478c2a6', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'b7a09435-91ea-db50-95b6-75b64e23c0fa', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('b468bc60-81f1-1a20-9901-ab98ca651908', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, 'f3b05270-2438-4480-54a8-7952f6e6f51c', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('b842cd1a-ae29-ca3e-9a2d-2254bafec375', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, 'a7283fec-5cd5-4eec-212e-7d00f8091d3a', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('b90a2cba-8de2-4dd8-4e66-87abe263bc6f', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '8a8fe8bf-afbd-81bd-bf25-92b297a09ae2', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('bc152a62-f804-fe5c-f6b4-8f3e7a620931', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', 'b6bd22cd-d47c-d8b2-daff-19a3d7c33d5d', DATE '2026-08-24', NULL, '167dd4ef-278a-ab72-c008-0011bca78f90', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('bef0e90b-2654-d484-e0ac-2be9942399a2', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'e1b1888f-4a70-24e2-56f8-5c5edea81e18', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('c78926f0-d3b8-0b06-577a-34f30f57d167', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '3b958aeb-2bcb-23af-e872-33fd47555240', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('c7e098e6-1846-201a-ea6f-e3a92f36da13', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '78ab9a1f-3da2-ce4e-712c-35f8b4a5b5b2', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('c88a5a4d-50ef-e9a5-0287-25d9045d8895', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', 'b6bd22cd-d47c-d8b2-daff-19a3d7c33d5d', DATE '2026-08-24', NULL, '167dd4ef-278a-ab72-c008-0011bca78f90', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('cbbc6581-dc6d-cb54-5dee-5a2f66c042d2', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '78ab9a1f-3da2-ce4e-712c-35f8b4a5b5b2', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('cd49c19d-cd55-0c4d-c6d4-56f6c302fef8', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '89ab728e-a4ec-232f-869d-3b8856ff28b4', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('cf0cdd8b-cad2-f850-5c28-49489dfa9b49', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', 'b6bd22cd-d47c-d8b2-daff-19a3d7c33d5d', DATE '2026-08-24', NULL, '167dd4ef-278a-ab72-c008-0011bca78f90', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('d1fe6862-d6b3-1980-39c9-8eebe8ec8963', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '4c7c86e7-b074-2136-4430-41b9c07757ff', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'b7a09435-91ea-db50-95b6-75b64e23c0fa', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('d49aa751-d432-80bb-4678-b52c0cc83426', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'e1b1888f-4a70-24e2-56f8-5c5edea81e18', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('d5fd32c5-d233-4921-19b5-7bfab3c292a4', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '8b50547e-3597-3c18-9e5c-3347f2e37d00', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('d9e0eb31-9a5c-2622-5460-47262d39a16d', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'a14089fc-1ee9-eb82-e719-3828be3d8b9a', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('dc3299b3-9f76-cc37-2809-380b19cc63b4', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '796e7d12-2446-0107-2d80-cb21840e83b3', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('dcc9bea5-0cf0-c8cd-ddb5-e433a9957d40', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '796e7d12-2446-0107-2d80-cb21840e83b3', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('e03feb6e-754b-518d-9692-b293fbdebd20', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '78ab9a1f-3da2-ce4e-712c-35f8b4a5b5b2', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('e15fd22a-9894-f3fd-8b25-759f259cae03', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '203ffe6d-883a-51d5-2a89-0b0746fcdd57', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('e19de9be-dc7d-c5cd-17e4-090b7e217580', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'a14089fc-1ee9-eb82-e719-3828be3d8b9a', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('e1e46acd-1cf7-a84c-e6cc-861d2408cfc7', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '0733e0ff-7fbf-ca6c-46c1-3c84c796abd9', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('e27fabf6-2136-a15e-9764-6f2a656cabe0', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, 'a7283fec-5cd5-4eec-212e-7d00f8091d3a', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('e4bbbad0-ae93-0484-2850-987ee15da4d1', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '796e7d12-2446-0107-2d80-cb21840e83b3', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('e53fe2a2-7540-d7e0-866c-31c4e5280f9a', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'b7a09435-91ea-db50-95b6-75b64e23c0fa', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('e78ee74b-3c13-7493-2165-796f6dd958f0', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, 'b7a09435-91ea-db50-95b6-75b64e23c0fa', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('e82c41db-afeb-b1e1-e80f-1b37c9620362', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', 'b9de3ebc-27c8-8fd6-85b2-d8bc916e6dfa', DATE '2026-08-24', NULL, '1e8916d1-240f-914a-f27d-45599e1f66af', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('eb304cce-8759-4aad-7d3b-1aedbcee13d5', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', '075fb64f-355a-ee74-517b-6b9c6da0f8db', DATE '2026-08-24', NULL, 'd04b56a2-c8ee-ed41-92ce-09bf3c1dc1e0', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('ebc987eb-6e20-932f-a565-017faa11e376', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '92f4dfe8-2c81-11db-0e68-ae27000fd606', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '37d7669d-ba95-55e0-7eb0-3259ac580089', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('ed92d2dd-0cba-73d9-d8f2-bbaeb8485c7a', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'c9b7930d-0f6f-e7d7-67d9-3c2b76473e0d', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('ee319aad-5247-4181-7d6e-e55693743117', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', 'b9de3ebc-27c8-8fd6-85b2-d8bc916e6dfa', DATE '2026-08-24', NULL, '1e8916d1-240f-914a-f27d-45599e1f66af', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('f0696612-8ddf-0dd0-d719-072cf1859ef5', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '89ab728e-a4ec-232f-869d-3b8856ff28b4', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('f07212d7-cf8c-9647-f8f6-72ffc18e4ad1', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '3b958aeb-2bcb-23af-e872-33fd47555240', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('f0be05b9-911c-9c52-2377-9af27d41cceb', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, '7854e746-5fec-ea3e-fa80-5df3cc9fa60e', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('f4b7b2a3-1770-4ff2-ed96-083c1089c0d0', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '284d56ef-2605-02b0-5ac6-9697daa4242a', 'b6bd22cd-d47c-d8b2-daff-19a3d7c33d5d', DATE '2026-08-24', NULL, '167dd4ef-278a-ab72-c008-0011bca78f90', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('f5d5f01f-584a-a9de-2e1f-5beafe5168b3', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, '5ce8c755-8822-8b4a-7168-ca1add2b5f07', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('f5f16dbe-5fbe-0e54-d80e-a7afbe8ffefe', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '203ffe6d-883a-51d5-2a89-0b0746fcdd57', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('f609474c-7a92-9ee9-e07c-a663a8e8a4a7', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '6cc581c8-7385-3438-88a4-9ddd9f3552c9', 'b6bd22cd-d47c-d8b2-daff-19a3d7c33d5d', DATE '2026-08-24', NULL, '167dd4ef-278a-ab72-c008-0011bca78f90', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('f86a6582-c8c4-f709-891a-4f9ca7514a15', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa', DATE '2026-08-24', NULL, '37d7669d-ba95-55e0-7eb0-3259ac580089', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('fa1e2ef7-4a8d-6e13-fa6d-351cebc1a0a7', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '8a8fe8bf-afbd-81bd-bf25-92b297a09ae2', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('fa6974e7-0f98-286e-2184-8c69bf23e824', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, 'de97d579-5c64-379a-ff16-1f40799fab1c', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('fb25be31-8e8f-3bff-831b-0de0941eaa14', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '8d38e3c9-d05b-270c-cec0-448e0020e2dc', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, '7854e746-5fec-ea3e-fa80-5df3cc9fa60e', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('fe65f7af-36d9-f2df-973c-2c771b37e44d', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc', '2f148a82-10ab-5801-9ff1-9f510611e5fd', DATE '2026-08-24', NULL, '2cc6228b-14e1-2932-7e1e-3d15c943e3ad', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('ff180680-b53c-c7b8-cb1b-4da2f5f9a5a7', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', 'b5b051ca-7d0d-c78a-0e14-9794651490db', DATE '2026-08-24', NULL, '0733e0ff-7fbf-ca6c-46c1-3c84c796abd9', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('fff03ea3-d611-e3c5-5ab2-7c9ae3cce879', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '51d81035-b83e-5452-c5c8-be69b5d3b1b3', 'e4bec48d-a248-c13d-a71a-00a2dd40e35e', DATE '2026-08-24', NULL, '5ce8c755-8822-8b4a-7168-ca1add2b5f07', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);

CREATE INDEX "IX_warehouses_CompanyId" ON advance.warehouses ("CompanyId");

CREATE INDEX "IX_warehouse_condition_locations_CompanyId" ON advance.warehouse_condition_locations ("CompanyId");

CREATE INDEX "IX_warehouse_condition_locations_CompanyId_OrganizationId" ON advance.warehouse_condition_locations ("CompanyId", "OrganizationId");

CREATE INDEX "IX_vendor_quotations_CompanyId" ON advance.vendor_quotations ("CompanyId");

CREATE INDEX "IX_vendor_quotations_CompanyId_OrganizationId" ON advance.vendor_quotations ("CompanyId", "OrganizationId");

CREATE INDEX "IX_vendor_quotation_lines_CompanyId" ON advance.vendor_quotation_lines ("CompanyId");

CREATE INDEX "IX_vendor_qualifications_CompanyId" ON advance.vendor_qualifications ("CompanyId");

CREATE INDEX "IX_vendor_qualifications_CompanyId_OrganizationId" ON advance.vendor_qualifications ("CompanyId", "OrganizationId");

CREATE INDEX "IX_user_accounts_PrincipalType_IsActive" ON advance.user_accounts ("PrincipalType", "IsActive");

ALTER TABLE advance.user_accounts ADD CONSTRAINT "CK_user_accounts_principal_type" CHECK ("PrincipalType" IN ('INTERNAL','VENDOR','CUSTOMER','SERVICE'));

CREATE INDEX "IX_tax_gst_settings_CompanyId" ON advance.tax_gst_settings ("CompanyId");

CREATE INDEX "IX_tax_gst_settings_CompanyId_OrganizationId" ON advance.tax_gst_settings ("CompanyId", "OrganizationId");

CREATE INDEX "IX_stock_reservations_CompanyId" ON advance.stock_reservations ("CompanyId");

CREATE INDEX "IX_stock_reservation_history_CompanyId" ON advance.stock_reservation_history ("CompanyId");

CREATE INDEX "IX_stock_movements_CompanyId" ON advance.stock_movements ("CompanyId");

CREATE INDEX "IX_stock_availability_checks_CompanyId" ON advance.stock_availability_checks ("CompanyId");

CREATE INDEX "IX_stock_availability_check_lines_CompanyId" ON advance.stock_availability_check_lines ("CompanyId");

CREATE INDEX "IX_rfq_vendor_invitations_CompanyId" ON advance.rfq_vendor_invitations ("CompanyId");

CREATE INDEX "IX_request_for_quotations_CompanyId" ON advance.request_for_quotations ("CompanyId");

CREATE INDEX "IX_request_for_quotations_CompanyId_OrganizationId" ON advance.request_for_quotations ("CompanyId", "OrganizationId");

CREATE INDEX "IX_request_for_quotation_lines_CompanyId" ON advance.request_for_quotation_lines ("CompanyId");

CREATE INDEX "IX_reporting_relationships_CompanyId" ON advance.reporting_relationships ("CompanyId");

CREATE INDEX "IX_rack_bins_CompanyId" ON advance.rack_bins ("CompanyId");

CREATE INDEX "IX_quotation_technical_verifications_CompanyId" ON advance.quotation_technical_verifications ("CompanyId");

CREATE INDEX "IX_qc_inspection_policies_CompanyId" ON advance.qc_inspection_policies ("CompanyId");

CREATE INDEX "IX_qc_inspection_policies_CompanyId_OrganizationId" ON advance.qc_inspection_policies ("CompanyId", "OrganizationId");

CREATE INDEX "IX_purchase_transaction_status_history_CompanyId" ON advance.purchase_transaction_status_history ("CompanyId");

CREATE INDEX "IX_purchase_transaction_status_history_CompanyId_OrganizationId" ON advance.purchase_transaction_status_history ("CompanyId", "OrganizationId");

CREATE INDEX "IX_purchase_transaction_approval_policies_CompanyId" ON advance.purchase_transaction_approval_policies ("CompanyId");

CREATE INDEX "IX_purchase_transaction_approval_policies_CompanyId_Organizati~" ON advance.purchase_transaction_approval_policies ("CompanyId", "OrganizationId");

CREATE INDEX "IX_purchase_transaction_approval_history_CompanyId" ON advance.purchase_transaction_approval_history ("CompanyId");

CREATE INDEX "IX_purchase_requisitions_CompanyId" ON advance.purchase_requisitions ("CompanyId");

CREATE INDEX "IX_purchase_requisitions_CompanyId_OrganizationId" ON advance.purchase_requisitions ("CompanyId", "OrganizationId");

CREATE INDEX "IX_purchase_requisition_status_history_CompanyId" ON advance.purchase_requisition_status_history ("CompanyId");

CREATE INDEX "IX_purchase_requisition_lines_AssetId" ON advance.purchase_requisition_lines ("AssetId");

CREATE INDEX "IX_purchase_requisition_lines_CompanyId" ON advance.purchase_requisition_lines ("CompanyId");

CREATE INDEX "IX_purchase_requisition_attachments_CompanyId" ON advance.purchase_requisition_attachments ("CompanyId");

CREATE INDEX "IX_purchase_requisition_approval_history_CompanyId" ON advance.purchase_requisition_approval_history ("CompanyId");

CREATE INDEX "IX_purchase_requirement_handoffs_CompanyId" ON advance.purchase_requirement_handoffs ("CompanyId");

CREATE INDEX "IX_purchase_orders_CompanyId" ON advance.purchase_orders ("CompanyId");

CREATE INDEX "IX_purchase_orders_CompanyId_OrganizationId" ON advance.purchase_orders ("CompanyId", "OrganizationId");

CREATE INDEX "IX_purchase_order_lines_CompanyId" ON advance.purchase_order_lines ("CompanyId");

CREATE INDEX "IX_purchase_order_history_CompanyId" ON advance.purchase_order_history ("CompanyId");

CREATE INDEX "IX_purchase_number_sequences_CompanyId" ON advance.purchase_number_sequences ("CompanyId");

CREATE INDEX "IX_purchase_number_sequences_CompanyId_OrganizationId" ON advance.purchase_number_sequences ("CompanyId", "OrganizationId");

CREATE INDEX "IX_purchase_approval_workflow_steps_CompanyId" ON advance.purchase_approval_workflow_steps ("CompanyId");

CREATE INDEX "IX_purchase_approval_route_settings_CompanyId" ON advance.purchase_approval_route_settings ("CompanyId");

CREATE INDEX "IX_organization_policies_CompanyId" ON advance.organization_policies ("CompanyId");

CREATE INDEX "IX_organization_policies_CompanyId_OrganizationId" ON advance.organization_policies ("CompanyId", "OrganizationId");

CREATE INDEX "IX_material_followup_handoffs_CompanyId" ON advance.material_followup_handoffs ("CompanyId");

CREATE INDEX "IX_employee_role_assignments_CompanyId" ON advance.employee_role_assignments ("CompanyId");

CREATE INDEX "IX_employee_operational_scopes_CompanyId" ON advance.employee_operational_scopes ("CompanyId");

CREATE INDEX "IX_employee_operational_scopes_CompanyId_OrganizationId" ON advance.employee_operational_scopes ("CompanyId", "OrganizationId");

CREATE INDEX "IX_employee_identity_mappings_CompanyId" ON advance.employee_identity_mappings ("CompanyId");

CREATE INDEX "IX_employee_identity_mappings_CompanyId_OrganizationId" ON advance.employee_identity_mappings ("CompanyId", "OrganizationId");

CREATE INDEX "IX_employee_department_history_CompanyId" ON advance.employee_department_history ("CompanyId");

CREATE INDEX "IX_departments_ParentDepartmentId" ON advance.departments ("ParentDepartmentId");

CREATE INDEX "IX_department_approval_mappings_CompanyId" ON advance.department_approval_mappings ("CompanyId");

CREATE INDEX "IX_controlled_configuration_histories_CompanyId" ON advance.controlled_configuration_histories ("CompanyId");

CREATE INDEX "IX_controlled_configuration_histories_CompanyId_OrganizationId" ON advance.controlled_configuration_histories ("CompanyId", "OrganizationId");

CREATE INDEX "IX_commercial_comparisons_CompanyId" ON advance.commercial_comparisons ("CompanyId");

CREATE INDEX "IX_commercial_comparisons_CompanyId_OrganizationId" ON advance.commercial_comparisons ("CompanyId", "OrganizationId");

CREATE INDEX "IX_commercial_comparison_lines_CompanyId" ON advance.commercial_comparison_lines ("CompanyId");

CREATE INDEX "IX_audit_logs_CompanyId_CreatedAt" ON advance.audit_logs ("CompanyId", "CreatedAt");

ALTER TABLE advance.audit_logs ADD CONSTRAINT "CK_audit_logs_scope" CHECK (("Scope" = 'GLOBAL' AND "CompanyId" IS NULL) OR ("Scope" = 'COMPANY' AND "CompanyId" IS NOT NULL));

CREATE INDEX "IX_assets_CompanyId" ON advance.assets ("CompanyId");

CREATE UNIQUE INDEX "IX_assets_CompanyId_AssetCode" ON advance.assets ("CompanyId", "AssetCode");

CREATE INDEX "IX_assets_CompanyId_CustomerId_Status" ON advance.assets ("CompanyId", "CustomerId", "Status");

CREATE UNIQUE INDEX "IX_assets_CompanyId_SerialNumber" ON advance.assets ("CompanyId", "SerialNumber") WHERE "SerialNumber" IS NOT NULL;

CREATE INDEX "IX_assets_CompanySiteId" ON advance.assets ("CompanySiteId");

CREATE INDEX "IX_assets_CustomerAddressId" ON advance.assets ("CustomerAddressId");

CREATE INDEX "IX_assets_CustomerId" ON advance.assets ("CustomerId");

CREATE INDEX "IX_assets_ItemId" ON advance.assets ("ItemId");

CREATE UNIQUE INDEX "IX_companies_Code" ON advance.companies ("Code");

CREATE INDEX "IX_company_gst_registrations_CompanyId" ON advance.company_gst_registrations ("CompanyId");

CREATE UNIQUE INDEX "IX_company_gst_registrations_CompanyId_StateCode_EffectiveFrom" ON advance.company_gst_registrations ("CompanyId", "StateCode", "EffectiveFrom");

CREATE INDEX "IX_company_gst_registrations_CompanySiteId" ON advance.company_gst_registrations ("CompanySiteId");

CREATE UNIQUE INDEX "IX_company_gst_registrations_Gstin" ON advance.company_gst_registrations ("Gstin");

CREATE INDEX "IX_company_sites_CompanyId" ON advance.company_sites ("CompanyId");

CREATE UNIQUE INDEX "IX_company_sites_CompanyId_Code" ON advance.company_sites ("CompanyId", "Code");

CREATE INDEX "IX_cost_centres_CompanyId" ON advance.cost_centres ("CompanyId");

CREATE UNIQUE INDEX "IX_cost_centres_CompanyId_Code" ON advance.cost_centres ("CompanyId", "Code");

CREATE INDEX "IX_cost_centres_DepartmentId" ON advance.cost_centres ("DepartmentId");

CREATE INDEX "IX_cost_centres_ParentCostCentreId" ON advance.cost_centres ("ParentCostCentreId");

CREATE UNIQUE INDEX "IX_currencies_Code" ON advance.currencies ("Code");

CREATE UNIQUE INDEX "IX_currencies_NumericCode" ON advance.currencies ("NumericCode") WHERE "NumericCode" IS NOT NULL;

CREATE INDEX "IX_customer_company_relationships_ApprovedByEmployeeId" ON advance.customer_company_relationships ("ApprovedByEmployeeId");

CREATE INDEX "IX_customer_company_relationships_CompanyId" ON advance.customer_company_relationships ("CompanyId");

CREATE UNIQUE INDEX "IX_customer_company_relationships_CompanyId_CustomerId_Effecti~" ON advance.customer_company_relationships ("CompanyId", "CustomerId", "EffectiveFrom");

CREATE INDEX "IX_customer_company_relationships_CustomerId" ON advance.customer_company_relationships ("CustomerId");

CREATE INDEX "IX_customer_company_relationships_PaymentTermId" ON advance.customer_company_relationships ("PaymentTermId");

CREATE INDEX "IX_customer_user_bindings_CustomerId_IsActive" ON advance.customer_user_bindings ("CustomerId", "IsActive");

CREATE UNIQUE INDEX "IX_customer_user_bindings_UserAccountId" ON advance.customer_user_bindings ("UserAccountId");

CREATE INDEX "IX_document_number_sequences_CompanyId" ON advance.document_number_sequences ("CompanyId");

CREATE UNIQUE INDEX "IX_document_number_sequences_CompanyId_DocumentType_FinancialP~" ON advance.document_number_sequences ("CompanyId", "DocumentType", "FinancialPeriodId");

CREATE INDEX "IX_document_number_sequences_FinancialPeriodId" ON advance.document_number_sequences ("FinancialPeriodId");

CREATE UNIQUE INDEX "IX_document_revisions_DocumentId" ON advance.document_revisions ("DocumentId") WHERE "IsCurrent";

CREATE UNIQUE INDEX "IX_document_revisions_DocumentId_RevisionCode" ON advance.document_revisions ("DocumentId", "RevisionCode");

CREATE UNIQUE INDEX "IX_document_revisions_DocumentId_RevisionNumber" ON advance.document_revisions ("DocumentId", "RevisionNumber");

CREATE INDEX "IX_document_revisions_ReleasedByUserAccountId" ON advance.document_revisions ("ReleasedByUserAccountId");

CREATE INDEX "IX_documents_CurrentRevisionId" ON advance.documents ("CurrentRevisionId");

CREATE UNIQUE INDEX "IX_documents_DocumentNumber" ON advance.documents ("DocumentNumber");

CREATE INDEX "IX_documents_DocumentType_Status" ON advance.documents ("DocumentType", "Status");

CREATE INDEX "IX_documents_OwnerDepartmentId" ON advance.documents ("OwnerDepartmentId");

CREATE INDEX "IX_employee_company_assignments_CompanyId" ON advance.employee_company_assignments ("CompanyId");

CREATE UNIQUE INDEX "IX_employee_company_assignments_CompanyId_EmployeeCode" ON advance.employee_company_assignments ("CompanyId", "EmployeeCode");

CREATE UNIQUE INDEX "IX_employee_company_assignments_CompanyId_EmployeeId_Assignmen~" ON advance.employee_company_assignments ("CompanyId", "EmployeeId", "AssignmentType", "EffectiveFrom");

CREATE INDEX "IX_employee_company_assignments_CompanySiteId" ON advance.employee_company_assignments ("CompanySiteId");

CREATE UNIQUE INDEX "IX_employee_company_assignments_EmployeeId" ON advance.employee_company_assignments ("EmployeeId") WHERE "AssignmentType" = 'PAYROLL' AND "IsActive";

CREATE INDEX "IX_employee_department_assignments_CompanyId" ON advance.employee_department_assignments ("CompanyId");

CREATE UNIQUE INDEX "IX_employee_department_assignments_CompanyId_EmployeeCompanyAs~" ON advance.employee_department_assignments ("CompanyId", "EmployeeCompanyAssignmentId", "DepartmentId", "EffectiveFrom");

CREATE INDEX "IX_employee_department_assignments_DepartmentId" ON advance.employee_department_assignments ("DepartmentId");

CREATE INDEX "IX_employee_department_assignments_DesignationId" ON advance.employee_department_assignments ("DesignationId");

CREATE UNIQUE INDEX "IX_employee_department_assignments_EmployeeCompanyAssignmentId" ON advance.employee_department_assignments ("EmployeeCompanyAssignmentId") WHERE "IsPrimary" AND "IsActive";

CREATE UNIQUE INDEX "IX_employee_user_bindings_EmployeeId" ON advance.employee_user_bindings ("EmployeeId");

CREATE UNIQUE INDEX "IX_employee_user_bindings_UserAccountId" ON advance.employee_user_bindings ("UserAccountId");

CREATE INDEX "IX_financial_periods_ClosedByUserAccountId" ON advance.financial_periods ("ClosedByUserAccountId");

CREATE INDEX "IX_financial_periods_CompanyId" ON advance.financial_periods ("CompanyId");

CREATE UNIQUE INDEX "IX_financial_periods_CompanyId_Code" ON advance.financial_periods ("CompanyId", "Code");

CREATE UNIQUE INDEX "IX_financial_periods_CompanyId_StartDate_EndDate" ON advance.financial_periods ("CompanyId", "StartDate", "EndDate");

CREATE UNIQUE INDEX "IX_payment_terms_Code" ON advance.payment_terms ("Code");

CREATE INDEX "IX_projects_CompanyId" ON advance.projects ("CompanyId");

CREATE INDEX "IX_projects_CompanyId_CustomerId_Status" ON advance.projects ("CompanyId", "CustomerId", "Status");

CREATE UNIQUE INDEX "IX_projects_CompanyId_ProjectCode" ON advance.projects ("CompanyId", "ProjectCode");

CREATE INDEX "IX_projects_CompanySiteId" ON advance.projects ("CompanySiteId");

CREATE INDEX "IX_projects_CostCentreId" ON advance.projects ("CostCentreId");

CREATE INDEX "IX_projects_CustomerId" ON advance.projects ("CustomerId");

CREATE INDEX "IX_projects_ManagerEmployeeId" ON advance.projects ("ManagerEmployeeId");

CREATE UNIQUE INDEX "IX_user_identity_mappings_Issuer_Subject" ON advance.user_identity_mappings ("Issuer", "Subject");

CREATE INDEX "IX_user_identity_mappings_UserAccountId_IsActive" ON advance.user_identity_mappings ("UserAccountId", "IsActive");

CREATE INDEX "IX_user_role_assignments_CompanyId_UserAccountId_IsActive" ON advance.user_role_assignments ("CompanyId", "UserAccountId", "IsActive");

CREATE INDEX "IX_user_role_assignments_RoleId" ON advance.user_role_assignments ("RoleId");

CREATE INDEX "IX_user_role_assignments_UserAccountId_Audience_IsActive" ON advance.user_role_assignments ("UserAccountId", "Audience", "IsActive");

CREATE UNIQUE INDEX "IX_user_role_assignments_UserAccountId_RoleId_Audience_Company~" ON advance.user_role_assignments ("UserAccountId", "RoleId", "Audience", "CompanyId", "EffectiveFrom") NULLS NOT DISTINCT;

CREATE INDEX "IX_vendor_company_relationships_ApprovedByEmployeeId" ON advance.vendor_company_relationships ("ApprovedByEmployeeId");

CREATE INDEX "IX_vendor_company_relationships_CompanyId" ON advance.vendor_company_relationships ("CompanyId");

CREATE UNIQUE INDEX "IX_vendor_company_relationships_CompanyId_VendorId_EffectiveFr~" ON advance.vendor_company_relationships ("CompanyId", "VendorId", "EffectiveFrom");

CREATE INDEX "IX_vendor_company_relationships_PaymentTermId" ON advance.vendor_company_relationships ("PaymentTermId");

CREATE INDEX "IX_vendor_company_relationships_VendorId" ON advance.vendor_company_relationships ("VendorId");

CREATE UNIQUE INDEX "IX_vendor_user_bindings_UserAccountId" ON advance.vendor_user_bindings ("UserAccountId");

CREATE INDEX "IX_vendor_user_bindings_VendorId_IsActive" ON advance.vendor_user_bindings ("VendorId", "IsActive");

ALTER TABLE advance.audit_logs ADD CONSTRAINT "FK_audit_logs_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.commercial_comparison_lines ADD CONSTRAINT "FK_commercial_comparison_lines_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.commercial_comparisons ADD CONSTRAINT "FK_commercial_comparisons_companies_CompanyId_OrganizationId" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.controlled_configuration_histories ADD CONSTRAINT "FK_controlled_configuration_histories_companies_CompanyId_Orga~" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.department_approval_mappings ADD CONSTRAINT "FK_department_approval_mappings_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.departments ADD CONSTRAINT "FK_departments_departments_ParentDepartmentId" FOREIGN KEY ("ParentDepartmentId") REFERENCES advance.departments ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.employee_department_history ADD CONSTRAINT "FK_employee_department_history_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.employee_identity_mappings ADD CONSTRAINT "FK_employee_identity_mappings_companies_CompanyId_Organization~" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.employee_operational_scopes ADD CONSTRAINT "FK_employee_operational_scopes_companies_CompanyId_Organizatio~" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.employee_role_assignments ADD CONSTRAINT "FK_employee_role_assignments_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.material_followup_handoffs ADD CONSTRAINT "FK_material_followup_handoffs_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.organization_policies ADD CONSTRAINT "FK_organization_policies_companies_CompanyId_OrganizationId" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_approval_route_settings ADD CONSTRAINT "FK_purchase_approval_route_settings_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_approval_workflow_steps ADD CONSTRAINT "FK_purchase_approval_workflow_steps_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_number_sequences ADD CONSTRAINT "FK_purchase_number_sequences_companies_CompanyId_OrganizationId" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_order_history ADD CONSTRAINT "FK_purchase_order_history_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_order_lines ADD CONSTRAINT "FK_purchase_order_lines_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_orders ADD CONSTRAINT "FK_purchase_orders_companies_CompanyId_OrganizationId" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_requirement_handoffs ADD CONSTRAINT "FK_purchase_requirement_handoffs_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_requisition_approval_history ADD CONSTRAINT "FK_purchase_requisition_approval_history_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_requisition_attachments ADD CONSTRAINT "FK_purchase_requisition_attachments_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_requisition_lines ADD CONSTRAINT "FK_purchase_requisition_lines_assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES advance.assets ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_requisition_lines ADD CONSTRAINT "FK_purchase_requisition_lines_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_requisition_status_history ADD CONSTRAINT "FK_purchase_requisition_status_history_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_requisitions ADD CONSTRAINT "FK_purchase_requisitions_companies_CompanyId_OrganizationId" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_transaction_approval_history ADD CONSTRAINT "FK_purchase_transaction_approval_history_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_transaction_approval_policies ADD CONSTRAINT "FK_purchase_transaction_approval_policies_companies_CompanyId_~" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.purchase_transaction_status_history ADD CONSTRAINT "FK_purchase_transaction_status_history_companies_CompanyId_Org~" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.qc_inspection_policies ADD CONSTRAINT "FK_qc_inspection_policies_companies_CompanyId_OrganizationId" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.quotation_technical_verifications ADD CONSTRAINT "FK_quotation_technical_verifications_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.rack_bins ADD CONSTRAINT "FK_rack_bins_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.reporting_relationships ADD CONSTRAINT "FK_reporting_relationships_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.request_for_quotation_lines ADD CONSTRAINT "FK_request_for_quotation_lines_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.request_for_quotations ADD CONSTRAINT "FK_request_for_quotations_companies_CompanyId_OrganizationId" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.rfq_vendor_invitations ADD CONSTRAINT "FK_rfq_vendor_invitations_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.stock_availability_check_lines ADD CONSTRAINT "FK_stock_availability_check_lines_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.stock_availability_checks ADD CONSTRAINT "FK_stock_availability_checks_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.stock_movements ADD CONSTRAINT "FK_stock_movements_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.stock_reservation_history ADD CONSTRAINT "FK_stock_reservation_history_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.stock_reservations ADD CONSTRAINT "FK_stock_reservations_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.tax_gst_settings ADD CONSTRAINT "FK_tax_gst_settings_companies_CompanyId_OrganizationId" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.vendor_qualifications ADD CONSTRAINT "FK_vendor_qualifications_companies_CompanyId_OrganizationId" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.vendor_quotation_lines ADD CONSTRAINT "FK_vendor_quotation_lines_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.vendor_quotations ADD CONSTRAINT "FK_vendor_quotations_companies_CompanyId_OrganizationId" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.warehouse_condition_locations ADD CONSTRAINT "FK_warehouse_condition_locations_companies_CompanyId_Organizat~" FOREIGN KEY ("CompanyId", "OrganizationId") REFERENCES advance.companies ("Id", "Code") ON DELETE RESTRICT;

ALTER TABLE advance.warehouses ADD CONSTRAINT "FK_warehouses_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES advance.companies ("Id") ON DELETE RESTRICT;

ALTER TABLE advance.document_revisions ADD CONSTRAINT "FK_document_revisions_documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES advance.documents ("Id") ON DELETE CASCADE;

INSERT INTO advance."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260824135450_MultiCompanySharedIdentityFoundation', '10.0.10');

COMMIT;

