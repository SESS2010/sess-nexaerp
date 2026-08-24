START TRANSACTION;
ALTER TABLE advance.audit_logs DROP CONSTRAINT "FK_audit_logs_companies_CompanyId";

ALTER TABLE advance.commercial_comparison_lines DROP CONSTRAINT "FK_commercial_comparison_lines_companies_CompanyId";

ALTER TABLE advance.commercial_comparisons DROP CONSTRAINT "FK_commercial_comparisons_companies_CompanyId_OrganizationId";

ALTER TABLE advance.controlled_configuration_histories DROP CONSTRAINT "FK_controlled_configuration_histories_companies_CompanyId_Orga~";

ALTER TABLE advance.department_approval_mappings DROP CONSTRAINT "FK_department_approval_mappings_companies_CompanyId";

ALTER TABLE advance.departments DROP CONSTRAINT "FK_departments_departments_ParentDepartmentId";

ALTER TABLE advance.employee_department_history DROP CONSTRAINT "FK_employee_department_history_companies_CompanyId";

ALTER TABLE advance.employee_identity_mappings DROP CONSTRAINT "FK_employee_identity_mappings_companies_CompanyId_Organization~";

ALTER TABLE advance.employee_operational_scopes DROP CONSTRAINT "FK_employee_operational_scopes_companies_CompanyId_Organizatio~";

ALTER TABLE advance.employee_role_assignments DROP CONSTRAINT "FK_employee_role_assignments_companies_CompanyId";

ALTER TABLE advance.material_followup_handoffs DROP CONSTRAINT "FK_material_followup_handoffs_companies_CompanyId";

ALTER TABLE advance.organization_policies DROP CONSTRAINT "FK_organization_policies_companies_CompanyId_OrganizationId";

ALTER TABLE advance.purchase_approval_route_settings DROP CONSTRAINT "FK_purchase_approval_route_settings_companies_CompanyId";

ALTER TABLE advance.purchase_approval_workflow_steps DROP CONSTRAINT "FK_purchase_approval_workflow_steps_companies_CompanyId";

ALTER TABLE advance.purchase_number_sequences DROP CONSTRAINT "FK_purchase_number_sequences_companies_CompanyId_OrganizationId";

ALTER TABLE advance.purchase_order_history DROP CONSTRAINT "FK_purchase_order_history_companies_CompanyId";

ALTER TABLE advance.purchase_order_lines DROP CONSTRAINT "FK_purchase_order_lines_companies_CompanyId";

ALTER TABLE advance.purchase_orders DROP CONSTRAINT "FK_purchase_orders_companies_CompanyId_OrganizationId";

ALTER TABLE advance.purchase_requirement_handoffs DROP CONSTRAINT "FK_purchase_requirement_handoffs_companies_CompanyId";

ALTER TABLE advance.purchase_requisition_approval_history DROP CONSTRAINT "FK_purchase_requisition_approval_history_companies_CompanyId";

ALTER TABLE advance.purchase_requisition_attachments DROP CONSTRAINT "FK_purchase_requisition_attachments_companies_CompanyId";

ALTER TABLE advance.purchase_requisition_lines DROP CONSTRAINT "FK_purchase_requisition_lines_assets_AssetId";

ALTER TABLE advance.purchase_requisition_lines DROP CONSTRAINT "FK_purchase_requisition_lines_companies_CompanyId";

ALTER TABLE advance.purchase_requisition_status_history DROP CONSTRAINT "FK_purchase_requisition_status_history_companies_CompanyId";

ALTER TABLE advance.purchase_requisitions DROP CONSTRAINT "FK_purchase_requisitions_companies_CompanyId_OrganizationId";

ALTER TABLE advance.purchase_transaction_approval_history DROP CONSTRAINT "FK_purchase_transaction_approval_history_companies_CompanyId";

ALTER TABLE advance.purchase_transaction_approval_policies DROP CONSTRAINT "FK_purchase_transaction_approval_policies_companies_CompanyId_~";

ALTER TABLE advance.purchase_transaction_status_history DROP CONSTRAINT "FK_purchase_transaction_status_history_companies_CompanyId_Org~";

ALTER TABLE advance.qc_inspection_policies DROP CONSTRAINT "FK_qc_inspection_policies_companies_CompanyId_OrganizationId";

ALTER TABLE advance.quotation_technical_verifications DROP CONSTRAINT "FK_quotation_technical_verifications_companies_CompanyId";

ALTER TABLE advance.rack_bins DROP CONSTRAINT "FK_rack_bins_companies_CompanyId";

ALTER TABLE advance.reporting_relationships DROP CONSTRAINT "FK_reporting_relationships_companies_CompanyId";

ALTER TABLE advance.request_for_quotation_lines DROP CONSTRAINT "FK_request_for_quotation_lines_companies_CompanyId";

ALTER TABLE advance.request_for_quotations DROP CONSTRAINT "FK_request_for_quotations_companies_CompanyId_OrganizationId";

ALTER TABLE advance.rfq_vendor_invitations DROP CONSTRAINT "FK_rfq_vendor_invitations_companies_CompanyId";

ALTER TABLE advance.stock_availability_check_lines DROP CONSTRAINT "FK_stock_availability_check_lines_companies_CompanyId";

ALTER TABLE advance.stock_availability_checks DROP CONSTRAINT "FK_stock_availability_checks_companies_CompanyId";

ALTER TABLE advance.stock_movements DROP CONSTRAINT "FK_stock_movements_companies_CompanyId";

ALTER TABLE advance.stock_reservation_history DROP CONSTRAINT "FK_stock_reservation_history_companies_CompanyId";

ALTER TABLE advance.stock_reservations DROP CONSTRAINT "FK_stock_reservations_companies_CompanyId";

ALTER TABLE advance.tax_gst_settings DROP CONSTRAINT "FK_tax_gst_settings_companies_CompanyId_OrganizationId";

ALTER TABLE advance.vendor_qualifications DROP CONSTRAINT "FK_vendor_qualifications_companies_CompanyId_OrganizationId";

ALTER TABLE advance.vendor_quotation_lines DROP CONSTRAINT "FK_vendor_quotation_lines_companies_CompanyId";

ALTER TABLE advance.vendor_quotations DROP CONSTRAINT "FK_vendor_quotations_companies_CompanyId_OrganizationId";

ALTER TABLE advance.warehouse_condition_locations DROP CONSTRAINT "FK_warehouse_condition_locations_companies_CompanyId_Organizat~";

ALTER TABLE advance.warehouses DROP CONSTRAINT "FK_warehouses_companies_CompanyId";

ALTER TABLE advance.document_revisions DROP CONSTRAINT "FK_document_revisions_documents_DocumentId";

DROP TABLE advance.assets;

DROP TABLE advance.company_gst_registrations;

DROP TABLE advance.currencies;

DROP TABLE advance.customer_company_relationships;

DROP TABLE advance.customer_user_bindings;

DROP TABLE advance.document_number_sequences;

DROP TABLE advance.employee_department_assignments;

DROP TABLE advance.employee_user_bindings;

DROP TABLE advance.projects;

DROP TABLE advance.user_identity_mappings;

DROP TABLE advance.user_role_assignments;

DROP TABLE advance.vendor_company_relationships;

DROP TABLE advance.vendor_user_bindings;

DROP TABLE advance.financial_periods;

DROP TABLE advance.employee_company_assignments;

DROP TABLE advance.cost_centres;

DROP TABLE advance.payment_terms;

DROP TABLE advance.company_sites;

DROP TABLE advance.companies;

DROP TABLE advance.documents;

DROP TABLE advance.document_revisions;

ALTER TABLE advance.warehouses DROP CONSTRAINT "AK_warehouses_CompanyId_Id";

DROP INDEX advance."IX_warehouses_CompanyId";

ALTER TABLE advance.warehouse_condition_locations DROP CONSTRAINT "AK_warehouse_condition_locations_CompanyId_Id";

DROP INDEX advance."IX_warehouse_condition_locations_CompanyId";

DROP INDEX advance."IX_warehouse_condition_locations_CompanyId_OrganizationId";

ALTER TABLE advance.vendor_quotations DROP CONSTRAINT "AK_vendor_quotations_CompanyId_Id";

DROP INDEX advance."IX_vendor_quotations_CompanyId";

DROP INDEX advance."IX_vendor_quotations_CompanyId_OrganizationId";

ALTER TABLE advance.vendor_quotation_lines DROP CONSTRAINT "AK_vendor_quotation_lines_CompanyId_Id";

DROP INDEX advance."IX_vendor_quotation_lines_CompanyId";

ALTER TABLE advance.vendor_qualifications DROP CONSTRAINT "AK_vendor_qualifications_CompanyId_Id";

DROP INDEX advance."IX_vendor_qualifications_CompanyId";

DROP INDEX advance."IX_vendor_qualifications_CompanyId_OrganizationId";

DROP INDEX advance."IX_user_accounts_PrincipalType_IsActive";

ALTER TABLE advance.user_accounts DROP CONSTRAINT "CK_user_accounts_principal_type";

ALTER TABLE advance.tax_gst_settings DROP CONSTRAINT "AK_tax_gst_settings_CompanyId_Id";

DROP INDEX advance."IX_tax_gst_settings_CompanyId";

DROP INDEX advance."IX_tax_gst_settings_CompanyId_OrganizationId";

ALTER TABLE advance.stock_reservations DROP CONSTRAINT "AK_stock_reservations_CompanyId_Id";

DROP INDEX advance."IX_stock_reservations_CompanyId";

ALTER TABLE advance.stock_reservation_history DROP CONSTRAINT "AK_stock_reservation_history_CompanyId_Id";

DROP INDEX advance."IX_stock_reservation_history_CompanyId";

ALTER TABLE advance.stock_movements DROP CONSTRAINT "AK_stock_movements_CompanyId_Id";

DROP INDEX advance."IX_stock_movements_CompanyId";

ALTER TABLE advance.stock_availability_checks DROP CONSTRAINT "AK_stock_availability_checks_CompanyId_Id";

DROP INDEX advance."IX_stock_availability_checks_CompanyId";

ALTER TABLE advance.stock_availability_check_lines DROP CONSTRAINT "AK_stock_availability_check_lines_CompanyId_Id";

DROP INDEX advance."IX_stock_availability_check_lines_CompanyId";

ALTER TABLE advance.rfq_vendor_invitations DROP CONSTRAINT "AK_rfq_vendor_invitations_CompanyId_Id";

DROP INDEX advance."IX_rfq_vendor_invitations_CompanyId";

ALTER TABLE advance.request_for_quotations DROP CONSTRAINT "AK_request_for_quotations_CompanyId_Id";

DROP INDEX advance."IX_request_for_quotations_CompanyId";

DROP INDEX advance."IX_request_for_quotations_CompanyId_OrganizationId";

ALTER TABLE advance.request_for_quotation_lines DROP CONSTRAINT "AK_request_for_quotation_lines_CompanyId_Id";

DROP INDEX advance."IX_request_for_quotation_lines_CompanyId";

ALTER TABLE advance.reporting_relationships DROP CONSTRAINT "AK_reporting_relationships_CompanyId_Id";

DROP INDEX advance."IX_reporting_relationships_CompanyId";

ALTER TABLE advance.rack_bins DROP CONSTRAINT "AK_rack_bins_CompanyId_Id";

DROP INDEX advance."IX_rack_bins_CompanyId";

ALTER TABLE advance.quotation_technical_verifications DROP CONSTRAINT "AK_quotation_technical_verifications_CompanyId_Id";

DROP INDEX advance."IX_quotation_technical_verifications_CompanyId";

ALTER TABLE advance.qc_inspection_policies DROP CONSTRAINT "AK_qc_inspection_policies_CompanyId_Id";

DROP INDEX advance."IX_qc_inspection_policies_CompanyId";

DROP INDEX advance."IX_qc_inspection_policies_CompanyId_OrganizationId";

ALTER TABLE advance.purchase_transaction_status_history DROP CONSTRAINT "AK_purchase_transaction_status_history_CompanyId_Id";

DROP INDEX advance."IX_purchase_transaction_status_history_CompanyId";

DROP INDEX advance."IX_purchase_transaction_status_history_CompanyId_OrganizationId";

ALTER TABLE advance.purchase_transaction_approval_policies DROP CONSTRAINT "AK_purchase_transaction_approval_policies_CompanyId_Id";

DROP INDEX advance."IX_purchase_transaction_approval_policies_CompanyId";

DROP INDEX advance."IX_purchase_transaction_approval_policies_CompanyId_Organizati~";

ALTER TABLE advance.purchase_transaction_approval_history DROP CONSTRAINT "AK_purchase_transaction_approval_history_CompanyId_Id";

DROP INDEX advance."IX_purchase_transaction_approval_history_CompanyId";

ALTER TABLE advance.purchase_requisitions DROP CONSTRAINT "AK_purchase_requisitions_CompanyId_Id";

DROP INDEX advance."IX_purchase_requisitions_CompanyId";

DROP INDEX advance."IX_purchase_requisitions_CompanyId_OrganizationId";

ALTER TABLE advance.purchase_requisition_status_history DROP CONSTRAINT "AK_purchase_requisition_status_history_CompanyId_Id";

DROP INDEX advance."IX_purchase_requisition_status_history_CompanyId";

ALTER TABLE advance.purchase_requisition_lines DROP CONSTRAINT "AK_purchase_requisition_lines_CompanyId_Id";

DROP INDEX advance."IX_purchase_requisition_lines_AssetId";

DROP INDEX advance."IX_purchase_requisition_lines_CompanyId";

ALTER TABLE advance.purchase_requisition_attachments DROP CONSTRAINT "AK_purchase_requisition_attachments_CompanyId_Id";

DROP INDEX advance."IX_purchase_requisition_attachments_CompanyId";

ALTER TABLE advance.purchase_requisition_approval_history DROP CONSTRAINT "AK_purchase_requisition_approval_history_CompanyId_Id";

DROP INDEX advance."IX_purchase_requisition_approval_history_CompanyId";

ALTER TABLE advance.purchase_requirement_handoffs DROP CONSTRAINT "AK_purchase_requirement_handoffs_CompanyId_Id";

DROP INDEX advance."IX_purchase_requirement_handoffs_CompanyId";

ALTER TABLE advance.purchase_orders DROP CONSTRAINT "AK_purchase_orders_CompanyId_Id";

DROP INDEX advance."IX_purchase_orders_CompanyId";

DROP INDEX advance."IX_purchase_orders_CompanyId_OrganizationId";

ALTER TABLE advance.purchase_order_lines DROP CONSTRAINT "AK_purchase_order_lines_CompanyId_Id";

DROP INDEX advance."IX_purchase_order_lines_CompanyId";

ALTER TABLE advance.purchase_order_history DROP CONSTRAINT "AK_purchase_order_history_CompanyId_Id";

DROP INDEX advance."IX_purchase_order_history_CompanyId";

ALTER TABLE advance.purchase_number_sequences DROP CONSTRAINT "AK_purchase_number_sequences_CompanyId_Id";

DROP INDEX advance."IX_purchase_number_sequences_CompanyId";

DROP INDEX advance."IX_purchase_number_sequences_CompanyId_OrganizationId";

ALTER TABLE advance.purchase_approval_workflow_steps DROP CONSTRAINT "AK_purchase_approval_workflow_steps_CompanyId_Id";

DROP INDEX advance."IX_purchase_approval_workflow_steps_CompanyId";

ALTER TABLE advance.purchase_approval_route_settings DROP CONSTRAINT "AK_purchase_approval_route_settings_CompanyId_Id";

DROP INDEX advance."IX_purchase_approval_route_settings_CompanyId";

ALTER TABLE advance.organization_policies DROP CONSTRAINT "AK_organization_policies_CompanyId_Id";

DROP INDEX advance."IX_organization_policies_CompanyId";

DROP INDEX advance."IX_organization_policies_CompanyId_OrganizationId";

ALTER TABLE advance.material_followup_handoffs DROP CONSTRAINT "AK_material_followup_handoffs_CompanyId_Id";

DROP INDEX advance."IX_material_followup_handoffs_CompanyId";

ALTER TABLE advance.employee_role_assignments DROP CONSTRAINT "AK_employee_role_assignments_CompanyId_Id";

DROP INDEX advance."IX_employee_role_assignments_CompanyId";

ALTER TABLE advance.employee_operational_scopes DROP CONSTRAINT "AK_employee_operational_scopes_CompanyId_Id";

DROP INDEX advance."IX_employee_operational_scopes_CompanyId";

DROP INDEX advance."IX_employee_operational_scopes_CompanyId_OrganizationId";

ALTER TABLE advance.employee_identity_mappings DROP CONSTRAINT "AK_employee_identity_mappings_CompanyId_Id";

DROP INDEX advance."IX_employee_identity_mappings_CompanyId";

DROP INDEX advance."IX_employee_identity_mappings_CompanyId_OrganizationId";

ALTER TABLE advance.employee_department_history DROP CONSTRAINT "AK_employee_department_history_CompanyId_Id";

DROP INDEX advance."IX_employee_department_history_CompanyId";

DROP INDEX advance."IX_departments_ParentDepartmentId";

ALTER TABLE advance.department_approval_mappings DROP CONSTRAINT "AK_department_approval_mappings_CompanyId_Id";

DROP INDEX advance."IX_department_approval_mappings_CompanyId";

ALTER TABLE advance.controlled_configuration_histories DROP CONSTRAINT "AK_controlled_configuration_histories_CompanyId_Id";

DROP INDEX advance."IX_controlled_configuration_histories_CompanyId";

DROP INDEX advance."IX_controlled_configuration_histories_CompanyId_OrganizationId";

ALTER TABLE advance.commercial_comparisons DROP CONSTRAINT "AK_commercial_comparisons_CompanyId_Id";

DROP INDEX advance."IX_commercial_comparisons_CompanyId";

DROP INDEX advance."IX_commercial_comparisons_CompanyId_OrganizationId";

ALTER TABLE advance.commercial_comparison_lines DROP CONSTRAINT "AK_commercial_comparison_lines_CompanyId_Id";

DROP INDEX advance."IX_commercial_comparison_lines_CompanyId";

DROP INDEX advance."IX_audit_logs_CompanyId_CreatedAt";

ALTER TABLE advance.audit_logs DROP CONSTRAINT "CK_audit_logs_scope";

ALTER TABLE advance.warehouses DROP COLUMN "CompanyId";

ALTER TABLE advance.warehouse_condition_locations DROP COLUMN "CompanyId";

ALTER TABLE advance.vendor_quotations DROP COLUMN "CompanyId";

ALTER TABLE advance.vendor_quotation_lines DROP COLUMN "CompanyId";

ALTER TABLE advance.vendor_qualifications DROP COLUMN "CompanyId";

ALTER TABLE advance.user_accounts DROP COLUMN "PrincipalType";

ALTER TABLE advance.tax_gst_settings DROP COLUMN "CompanyId";

ALTER TABLE advance.stock_reservations DROP COLUMN "CompanyId";

ALTER TABLE advance.stock_reservation_history DROP COLUMN "CompanyId";

ALTER TABLE advance.stock_movements DROP COLUMN "CompanyId";

ALTER TABLE advance.stock_availability_checks DROP COLUMN "CompanyId";

ALTER TABLE advance.stock_availability_check_lines DROP COLUMN "CompanyId";

ALTER TABLE advance.rfq_vendor_invitations DROP COLUMN "CompanyId";

ALTER TABLE advance.request_for_quotations DROP COLUMN "CompanyId";

ALTER TABLE advance.request_for_quotation_lines DROP COLUMN "CompanyId";

ALTER TABLE advance.reporting_relationships DROP COLUMN "CompanyId";

ALTER TABLE advance.rack_bins DROP COLUMN "CompanyId";

ALTER TABLE advance.quotation_technical_verifications DROP COLUMN "CompanyId";

ALTER TABLE advance.qc_inspection_policies DROP COLUMN "CompanyId";

ALTER TABLE advance.purchase_transaction_status_history DROP COLUMN "CompanyId";

ALTER TABLE advance.purchase_transaction_approval_policies DROP COLUMN "CompanyId";

ALTER TABLE advance.purchase_transaction_approval_history DROP COLUMN "CompanyId";

ALTER TABLE advance.purchase_requisitions DROP COLUMN "CompanyId";

ALTER TABLE advance.purchase_requisition_status_history DROP COLUMN "CompanyId";

ALTER TABLE advance.purchase_requisition_lines DROP COLUMN "AssetId";

ALTER TABLE advance.purchase_requisition_lines DROP COLUMN "CompanyId";

ALTER TABLE advance.purchase_requisition_attachments DROP COLUMN "CompanyId";

ALTER TABLE advance.purchase_requisition_approval_history DROP COLUMN "CompanyId";

ALTER TABLE advance.purchase_requirement_handoffs DROP COLUMN "CompanyId";

ALTER TABLE advance.purchase_orders DROP COLUMN "CompanyId";

ALTER TABLE advance.purchase_order_lines DROP COLUMN "CompanyId";

ALTER TABLE advance.purchase_order_history DROP COLUMN "CompanyId";

ALTER TABLE advance.purchase_number_sequences DROP COLUMN "CompanyId";

ALTER TABLE advance.purchase_approval_workflow_steps DROP COLUMN "CompanyId";

ALTER TABLE advance.purchase_approval_route_settings DROP COLUMN "CompanyId";

ALTER TABLE advance.organization_policies DROP COLUMN "CompanyId";

ALTER TABLE advance.material_followup_handoffs DROP COLUMN "CompanyId";

ALTER TABLE advance.employee_role_assignments DROP COLUMN "CompanyId";

ALTER TABLE advance.employee_operational_scopes DROP COLUMN "CompanyId";

ALTER TABLE advance.employee_identity_mappings DROP COLUMN "CompanyId";

ALTER TABLE advance.employee_department_history DROP COLUMN "CompanyId";

ALTER TABLE advance.departments DROP COLUMN "ParentDepartmentId";

ALTER TABLE advance.department_approval_mappings DROP COLUMN "CompanyId";

ALTER TABLE advance.controlled_configuration_histories DROP COLUMN "CompanyId";

ALTER TABLE advance.commercial_comparisons DROP COLUMN "CompanyId";

ALTER TABLE advance.commercial_comparison_lines DROP COLUMN "CompanyId";

ALTER TABLE advance.audit_logs DROP COLUMN "CompanyId";

ALTER TABLE advance.audit_logs DROP COLUMN "Scope";

UPDATE advance.departments SET "Code" = 'PRODUCTION_FABRICATION', "Name" = 'Production/Fabrication'
WHERE "Id" = '51d81035-b83e-5452-c5c8-be69b5d3b1b3';

UPDATE advance.departments SET "Code" = 'MANAGER', "IsActive" = TRUE, "Name" = 'Manager'
WHERE "Id" = '6ea3e733-e5e0-9b55-e7de-db94afda2b09';

UPDATE advance.departments SET "Code" = 'JUNIOR_ASSISTANT', "IsActive" = TRUE, "Name" = 'Junior/Assistant'
WHERE "Id" = 'bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e';

UPDATE advance.departments SET "Code" = 'ADMIN_ACCOUNTS_STORES', "IsActive" = TRUE, "Name" = 'Admin/Accounts/Stores'
WHERE "Id" = 'd30a9101-4e01-b19c-bc7c-926feb98e889';

UPDATE advance.departments SET "Code" = 'ENGINEER_TECHNICAL', "IsActive" = TRUE, "Name" = 'Engineer/Technical'
WHERE "Id" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4';

UPDATE advance.designations SET "Name" = 'LABVIEW DEVELOPER'
WHERE "Id" = '075fb64f-355a-ee74-517b-6b9c6da0f8db';

UPDATE advance.designations SET "Name" = 'TECHNICAL DIRECTOR'
WHERE "Id" = '086ab1d4-3404-12b7-c35a-4b77737eb97b';

UPDATE advance.designations SET "Name" = 'ELECTRICAL ENGINEER'
WHERE "Id" = '2f148a82-10ab-5801-9ff1-9f510611e5fd';

UPDATE advance.designations SET "Name" = 'JUNIOR ACCOUNTS'
WHERE "Id" = '35936fb3-4fc0-4757-268f-c467720e39fa';

UPDATE advance.designations SET "Code" = 'JR._ACCOUNT', "IsActive" = TRUE, "Name" = 'JR. ACCOUNT'
WHERE "Id" = '37ae1390-d60b-28aa-f5f8-43b5549936c8';

UPDATE advance.designations SET "Code" = 'JR._ELECTRICAL___PLC___INSTRUMENTATION_SUPPORT', "IsActive" = TRUE, "Name" = 'JR. ELECTRICAL / PLC / INSTRUMENTATION SUPPORT'
WHERE "Id" = '39f842c4-5688-20a6-2a81-dc0fed68aa0f';

UPDATE advance.designations SET "Name" = 'JUNIOR ENGINEER'
WHERE "Id" = '4c22a815-6a44-3d0b-9bd2-45743fc0a9aa';

UPDATE advance.designations SET "Name" = 'DESIGN ENGINEER'
WHERE "Id" = '4c9baa15-c3d4-6b41-d040-f354c5cff307';

UPDATE advance.designations SET "Code" = 'HR_DEPT', "Name" = 'HR DEPT'
WHERE "Id" = '82783939-c768-2002-5b0e-17db5261eab9';

UPDATE advance.designations SET "Code" = 'ADMIN_MAINTENANCE', "Name" = 'ADMIN MAINTENANCE'
WHERE "Id" = '8e377677-95bb-f0fe-4207-2efaf2b89208';

UPDATE advance.designations SET "Name" = 'SOFTWARE DEVELOPER'
WHERE "Id" = '90c527f8-3ea8-dc72-7283-c80e73a71f5d';

UPDATE advance.designations SET "Code" = 'STORES_AND_PURCHASE', "IsActive" = TRUE, "Name" = 'STORES AND PURCHASE'
WHERE "Id" = '940ac030-8dcf-1575-6545-fea0f75f18f8';

UPDATE advance.designations SET "Name" = 'PRODUCTION COORDINATOR'
WHERE "Id" = '96908ceb-4e96-b670-db7e-59b2237f1dec';

UPDATE advance.designations SET "Code" = 'JR._ENGINEER', "IsActive" = TRUE, "Name" = 'JR. ENGINEER'
WHERE "Id" = 'a2ed4710-4cec-d8dd-097e-e8c7353a66a6';

UPDATE advance.designations SET "Code" = 'MD', "Name" = 'MD'
WHERE "Id" = 'a653c7ab-0b15-c0fc-bdcb-8cb6c64bd830';

UPDATE advance.designations SET "Code" = 'REFRIGERATION___MECHANICAL_ENGINEER', "Name" = 'REFRIGERATION / MECHANICAL ENGINEER'
WHERE "Id" = 'b5b051ca-7d0d-c78a-0e14-9794651490db';

UPDATE advance.designations SET "Name" = 'PLC ENGINEER'
WHERE "Id" = 'b9de3ebc-27c8-8fd6-85b2-d8bc916e6dfa';

UPDATE advance.designations SET "Name" = 'TECHNICAL SUPPORT MANAGER'
WHERE "Id" = 'c7775052-f0a9-27e3-f259-746120a113a6';

UPDATE advance.designations SET "Name" = 'FABRICATOR'
WHERE "Id" = 'e4bec48d-a248-c13d-a71a-00a2dd40e35e';

UPDATE advance.designations SET "Code" = 'PRODUCTION_MECHANICAL_TEAM', "IsActive" = TRUE, "Name" = 'PRODUCTION MECHANICAL TEAM'
WHERE "Id" = 'f38530d3-549c-8fe3-3f75-331795d92bd3';

UPDATE advance.designations SET "Name" = 'STORES ASSISTANT'
WHERE "Id" = 'f8b60dea-0ce6-fa0b-f56e-a7e01058bfc6';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-012","Name":"PRIYA.E","EmployeeType":"Permanent","Grade":"Executive","Department":"Admin/Accounts/Stores","Skill":"Admin/Accounts/Stores","Designation":"STORES AND PURCHASE","Roles":["PURCHASE_EXECUTIVE","STORES_EXECUTIVE"]}'
WHERE "Id" = '0f56a17e-c040-acb4-6736-1cc168a81c46';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-003","Name":"M. SATHISHKUMAR","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"REFRIGERATION / MECHANICAL ENGINEER","Roles":["TECHNICAL_ENGINEER"]}'
WHERE "Id" = '0f6b42e6-1bab-d372-290a-9057fd7805f6';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-037","Name":"DEVANAND B","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"REFRIGERATION / MECHANICAL ENGINEER","Roles":["TECHNICAL_ENGINEER"]}'
WHERE "Id" = '1963049e-f974-5923-54e3-72af4c92f635';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-032","Name":"PRASANNA.G","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"LABVIEW DEVELOPER","Roles":["SOFTWARE_ENGINEER"]}'
WHERE "Id" = '2bc77b77-1d6d-4279-8d9d-8cf854537ea0';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-002","Name":"ALAGUEASWARI","EmployeeType":"Permanent","Grade":"Executive","Department":"Management","Skill":"Management","Designation":"MD","Roles":["MANAGING_DIRECTOR"]}'
WHERE "Id" = '2d009327-ea1c-2e86-5f13-bc4df67fd6bc';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-007","Name":"A. ALFATHIMA PARVEEN","EmployeeType":"Permanent","Grade":"Executive","Department":"Junior/Assistant","Skill":"Junior/Assistant","Designation":"JR. ACCOUNT","Roles":["ACCOUNTS_ASSISTANT"]}'
WHERE "Id" = '2f40e507-8533-479e-6db2-d696d7cb5807';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-011","Name":"YESWANTH KUMAR.N","EmployeeType":"Permanent","Grade":"Executive","Department":"Junior/Assistant","Skill":"Junior/Assistant","Designation":"JUNIOR ENGINEER","Roles":["JUNIOR_ENGINEER"]}'
WHERE "Id" = '3045b304-1c11-b626-4170-02ed928cfde8';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-035","Name":"VINAYAGAM","EmployeeType":"Permanent","Grade":"Executive","Department":"Production/Fabrication","Skill":"Production/Fabrication","Designation":"FABRICATOR","Roles":["PRODUCTION_OPERATOR"]}'
WHERE "Id" = '360cc0c3-8709-66a2-513c-bff91aed60e0';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-006","Name":"S. NANTHAKUMAR","EmployeeType":"Permanent","Grade":"Executive","Department":"Junior/Assistant","Skill":"Junior/Assistant","Designation":"JR. ELECTRICAL / PLC / INSTRUMENTATION SUPPORT","Roles":["JUNIOR_ENGINEER"]}'
WHERE "Id" = '38f02d97-8ea0-c6a0-6132-cf41067a7af3';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-010","Name":"RAJESHKUMAR.V","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"ELECTRICAL ENGINEER","Roles":["ELECTRICAL_ENGINEER"]}'
WHERE "Id" = '3da0797e-c7ce-8c50-3bcd-a857613a54db';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-020","Name":"RANJEETH.B","EmployeeType":"Permanent","Grade":"Executive","Department":"Admin/Accounts/Stores","Skill":"Admin/Accounts/Stores","Designation":"HR DEPT","Roles":["HR_EXECUTIVE"]}'
WHERE "Id" = '402f96b9-1b0a-2400-183e-987b2b06f2d6';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-008","Name":"SURANTHER P","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"SOFTWARE DEVELOPER","Roles":["SOFTWARE_DEVELOPER"]}'
WHERE "Id" = '433b462b-d44e-0ce4-a6ba-a9373b87e605';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-017","Name":"MOHD ASHIQ","EmployeeType":"Permanent","Grade":"Executive","Department":"Junior/Assistant","Skill":"Junior/Assistant","Designation":"JUNIOR ENGINEER","Roles":["JUNIOR_ENGINEER"]}'
WHERE "Id" = '48023480-faa5-975e-ee67-4ee5854aa96b';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-029","Name":"SRINIVASAN.C","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"REFRIGERATION / MECHANICAL ENGINEER","Roles":["TECHNICAL_ENGINEER"]}'
WHERE "Id" = '55b979e1-f612-de68-1aa0-d6348dd174cd';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-034","Name":"MADHANKUMAR.J","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"REFRIGERATION / MECHANICAL ENGINEER","Roles":["TECHNICAL_ENGINEER"]}'
WHERE "Id" = '59fbbe70-bb14-d466-3bf7-e97a1040c446';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-009","Name":"MANIKANDAN.S","EmployeeType":"Permanent","Grade":"Executive","Department":"Junior/Assistant","Skill":"Junior/Assistant","Designation":"JR. ENGINEER","Roles":["JUNIOR_ENGINEER"]}'
WHERE "Id" = '5d2880b6-6e40-84c4-b982-e4f16b422dd5';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-015","Name":"RANJITH.E","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"DESIGN ENGINEER","Roles":["DESIGN_ENGINEER"]}'
WHERE "Id" = '6695623a-7f5c-4041-00e4-c8d7cde7745e';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-039","Name":"THIRUNAVUKKARASU","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"REFRIGERATION / MECHANICAL ENGINEER","Roles":["TECHNICAL_ENGINEER"]}'
WHERE "Id" = '756caab8-cd36-fe0a-4a9b-2cfc2651549e';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-023","Name":"SARATH BABU.K","EmployeeType":"Permanent","Grade":"Executive","Department":"Production/Fabrication","Skill":"Production/Fabrication","Designation":"PRODUCTION COORDINATOR","Roles":["PRODUCTION_COORDINATOR"]}'
WHERE "Id" = '75cd655f-0c24-89ae-9f3b-11fc83651c0e';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-027","Name":"SANJAY SARAVANAN","EmployeeType":"Permanent","Grade":"Executive","Department":"Junior/Assistant","Skill":"Junior/Assistant","Designation":"JUNIOR ACCOUNTS","Roles":["ACCOUNTS_ASSISTANT"]}'
WHERE "Id" = '91576a97-ed27-5bf5-5ff3-82bf4912a2da';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-024","Name":"PRAKASAM.B","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"ELECTRICAL ENGINEER","Roles":["ELECTRICAL_ENGINEER"]}'
WHERE "Id" = '9a98139e-e3cf-e3a5-efb7-eb276b5b5bf7';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-038","Name":"SYED IJAZUDDIN Z","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"PLC ENGINEER","Roles":["PLC_ENGINEER"]}'
WHERE "Id" = '9c911b33-3733-9d90-307f-c2221e6586b3';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-014","Name":"KAMALI SRINIVASAN","EmployeeType":"Permanent","Grade":"Executive","Department":"Junior/Assistant","Skill":"Junior/Assistant","Designation":"STORES ASSISTANT","Roles":["STORES_ASSISTANT"]}'
WHERE "Id" = 'a0519833-9d8b-dbd7-42aa-df3fb73ab391';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-004","Name":"T. DINESH","EmployeeType":"Permanent","Grade":"Executive","Department":"Manager","Skill":"Manager","Designation":"TECHNICAL SUPPORT MANAGER","Roles":["TECHNICAL_SUPPORT_MANAGER"]}'
WHERE "Id" = 'a16a71a7-1c21-c40b-7fe5-4b76aa13f2d7';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-028","Name":"PRAVEEN KUMAR.M","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"REFRIGERATION / MECHANICAL ENGINEER","Roles":["TECHNICAL_ENGINEER"]}'
WHERE "Id" = 'a9a42d67-1710-9687-2eeb-df48df1adc33';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-016","Name":"KALIDOSS","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"DESIGN ENGINEER","Roles":["DESIGN_ENGINEER"]}'
WHERE "Id" = 'b2e05e24-8e31-871f-a938-4253cfe87be9';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-030","Name":"MANIKANDAN SOKKALINGAM","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"ELECTRICAL ENGINEER","Roles":["ELECTRICAL_ENGINEER"]}'
WHERE "Id" = 'b4c08282-5c80-b7a0-5143-fd5a5bb112a1';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-022","Name":"KARTHICK.B","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"ELECTRICAL ENGINEER","Roles":["ELECTRICAL_ENGINEER"]}'
WHERE "Id" = 'b7dea89e-de29-daa2-4608-72c6734e3aa1';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-018","Name":"A. VINAYA SAGAR ARKATI","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"ELECTRICAL ENGINEER","Roles":["ELECTRICAL_ENGINEER"]}'
WHERE "Id" = 'c02926b7-b69c-f94e-4f98-d3e7e8b304a6';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-031","Name":"VENKAT RAV.S","EmployeeType":"Permanent","Grade":"Executive","Department":"Junior/Assistant","Skill":"Junior/Assistant","Designation":"JUNIOR ACCOUNTS","Roles":["ACCOUNTS_ASSISTANT"]}'
WHERE "Id" = 'c169fe6d-6b2c-33ec-c820-daaebaf58fef';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-021","Name":"KRISHNAVENI","EmployeeType":"Permanent","Grade":"Executive","Department":"Admin/Accounts/Stores","Skill":"Admin/Accounts/Stores","Designation":"ADMIN MAINTENANCE","Roles":["ADMIN_EXECUTIVE"]}'
WHERE "Id" = 'c4c160a6-38ca-fb45-1596-1acde02fef13';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-005","Name":"WASEEM.S","EmployeeType":"Permanent","Grade":"Executive","Department":"Production/Fabrication","Skill":"Production/Fabrication","Designation":"PRODUCTION MECHANICAL TEAM","Roles":["PRODUCTION_OPERATOR"]}'
WHERE "Id" = 'ca1ac22f-c92b-6f0b-6d00-dd686a27adf0';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-036","Name":"FRANCIS XAVIER","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"REFRIGERATION / MECHANICAL ENGINEER","Roles":["TECHNICAL_ENGINEER"]}'
WHERE "Id" = 'cfdc990d-5afd-1b29-bf52-ab5995b174cf';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-025","Name":"KARTHIKEYAN MK","EmployeeType":"Permanent","Grade":"Executive","Department":"Production/Fabrication","Skill":"Production/Fabrication","Designation":"FABRICATOR","Roles":["PRODUCTION_OPERATOR"]}'
WHERE "Id" = 'd181ade1-290a-8ebe-1f57-47b66b4ecdde';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-013","Name":"LALU","EmployeeType":"Permanent","Grade":"Executive","Department":"Production/Fabrication","Skill":"Production/Fabrication","Designation":"FABRICATOR","Roles":["PRODUCTION_OPERATOR"]}'
WHERE "Id" = 'd4bbc4c9-5036-bb52-53bb-2dd1e420b5ed';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-026","Name":"SRINIVASAN.V","EmployeeType":"Permanent","Grade":"Executive","Department":"Production/Fabrication","Skill":"Production/Fabrication","Designation":"FABRICATOR","Roles":["PRODUCTION_OPERATOR"]}'
WHERE "Id" = 'd85900eb-e0a2-9ac2-9298-7bbef29480e7';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-033","Name":"BLESSON PAUL","EmployeeType":"Permanent","Grade":"Executive","Department":"Junior/Assistant","Skill":"Junior/Assistant","Designation":"JR. ENGINEER","Roles":["JUNIOR_ENGINEER"]}'
WHERE "Id" = 'e2bb043e-cfe0-c4a1-1a63-53097f1ebea4';

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-019","Name":"RANJITH. R","EmployeeType":"Permanent","Grade":"Executive","Department":"Engineer/Technical","Skill":"Engineer/Technical","Designation":"DESIGN ENGINEER","Roles":["DESIGN_ENGINEER"]}'
WHERE "Id" = 'f03f9db4-a89a-7d11-960a-43eb702e3439';

UPDATE advance.employees SET "DepartmentId" = 'bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e', "DesignationId" = 'a2ed4710-4cec-d8dd-097e-e8c7353a66a6'
WHERE "Id" = '04a820d0-3213-a6c2-9ea1-9a5180efcf37';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = '1577c211-a6ed-b6ee-d206-5461ad52c428';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = '20f22ccf-a178-a29e-0a35-7671ff2a2bab';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4', "DesignationId" = '90c527f8-3ea8-dc72-7283-c80e73a71f5d'
WHERE "Id" = '22a9f52a-db35-3ab5-0115-5e399bfbf4b2';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = '26c37705-e799-8708-119b-1227908d5e0f';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = '277fd621-865d-2823-1b5c-e13a9c36eb2a';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = '294a2d76-6b76-66d0-76ce-e8d12c02f0c7';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = '348345c2-1342-5b69-a85a-28d878cd75c6';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = '3edaa7c0-f393-cb3e-fb1e-e2071cbf2178';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = '41ff2ffb-081e-4600-7680-eef1ef81c01e';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = '45f0c876-d996-210a-67b3-993b7502d3e5';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = '48f8c731-7101-d7ff-6605-6b8f283718b1';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = '50a5b3a3-aa3a-8269-a283-149d2a69cf8a';

UPDATE advance.employees SET "DepartmentId" = 'bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e', "DesignationId" = '37ae1390-d60b-28aa-f5f8-43b5549936c8'
WHERE "Id" = '5fdedc5a-1740-164c-04e9-3c6f2db5417c';

UPDATE advance.employees SET "DesignationId" = 'f38530d3-549c-8fe3-3f75-331795d92bd3'
WHERE "Id" = '64382325-5125-141e-057e-7ee3f30b2bd3';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = '85b9da5c-cf3b-6217-593f-4b8e206bfa7a';

UPDATE advance.employees SET "DepartmentId" = '6ea3e733-e5e0-9b55-e7de-db94afda2b09'
WHERE "Id" = '889f9bdc-f246-e914-410d-7102ad10e31d';

UPDATE advance.employees SET "DepartmentId" = 'bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e'
WHERE "Id" = '93216afd-a239-3124-c23e-32d1ff8a8cee';

UPDATE advance.employees SET "DepartmentId" = 'bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e'
WHERE "Id" = '9cb99d4e-f1a7-7c9b-62e4-dd838db62c91';

UPDATE advance.employees SET "DepartmentId" = 'bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e', "DesignationId" = 'a2ed4710-4cec-d8dd-097e-e8c7353a66a6'
WHERE "Id" = 'a8ffe255-91ff-3c05-8f9f-dfa21826f2d5';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = 'b04acf39-5c81-d23c-89e6-9266d39b0be6';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = 'b1d0d0fb-27b8-e8db-1b03-023c32c74dc9';

UPDATE advance.employees SET "DepartmentId" = 'bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e'
WHERE "Id" = 'b42a0911-dc25-c491-e26f-b87a7512a0ed';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = 'bc8570aa-774c-9c38-9b42-ddf8599758f0';

UPDATE advance.employees SET "DepartmentId" = 'd30a9101-4e01-b19c-bc7c-926feb98e889', "DesignationId" = '940ac030-8dcf-1575-6545-fea0f75f18f8'
WHERE "Id" = 'be7613f2-52e8-5537-06b2-3e25de92c230';

UPDATE advance.employees SET "DepartmentId" = 'd30a9101-4e01-b19c-bc7c-926feb98e889'
WHERE "Id" = 'c175d954-417c-1d34-435c-8a5dce05ac78';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = 'e2815b6b-d417-6f86-177b-fb4fc46a6045';

UPDATE advance.employees SET "DepartmentId" = 'bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e'
WHERE "Id" = 'e7bd4851-c9ba-68e9-a21f-e8583cb82642';

UPDATE advance.employees SET "DepartmentId" = 'ee1a1fa1-17d8-623b-d173-ce1efbb11cd4'
WHERE "Id" = 'eca6a631-ef87-cb26-dbd3-5535a950d37f';

UPDATE advance.employees SET "DepartmentId" = 'bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e'
WHERE "Id" = 'f1dbc4aa-d567-616d-5e5c-63fd8f049e68';

UPDATE advance.employees SET "DepartmentId" = 'bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e', "DesignationId" = '39f842c4-5688-20a6-2a81-dc0fed68aa0f'
WHERE "Id" = 'fa72ea80-86c0-5f25-f12c-721e76c1daac';

UPDATE advance.employees SET "DepartmentId" = 'd30a9101-4e01-b19c-bc7c-926feb98e889'
WHERE "Id" = 'ff338b63-0eab-59d7-56b1-525e1bedfffd';

ALTER TABLE advance.organization_policies DISABLE TRIGGER trg_rev869a_policy_version_guard;

UPDATE advance.organization_policies SET "OrganizationId" = 'SESS'
WHERE "Id" = '50000000-0000-0000-0000-000000000001';

UPDATE advance.organization_policies SET "OrganizationId" = 'SESS'
WHERE "Id" = '50000000-0000-0000-0000-000000000002';

DELETE FROM advance.departments
WHERE "Id" = '05f64e3a-d2a5-1c89-cf9e-667e367e8dae';

DELETE FROM advance.departments
WHERE "Id" = '2243fe97-0335-bcf7-af29-8c0d5e0bac25';

DELETE FROM advance.departments
WHERE "Id" = '284d56ef-2605-02b0-5ac6-9697daa4242a';

DELETE FROM advance.departments
WHERE "Id" = '2ed4c8a3-6340-83ae-b149-95e5b8492b11';

DELETE FROM advance.departments
WHERE "Id" = '40e2426e-14ce-bcbf-2b61-bc2ce0f67bfc';

DELETE FROM advance.departments
WHERE "Id" = '45f7f136-f943-7fbf-c4d3-1b588f7faf71';

DELETE FROM advance.departments
WHERE "Id" = '4c7c86e7-b074-2136-4430-41b9c07757ff';

DELETE FROM advance.departments
WHERE "Id" = '621badfa-eecb-34b1-44c5-029bc6447658';

DELETE FROM advance.departments
WHERE "Id" = '6456dcf7-dad1-598a-f332-ad31dbfc907c';

DELETE FROM advance.departments
WHERE "Id" = '6cc581c8-7385-3438-88a4-9ddd9f3552c9';

DELETE FROM advance.departments
WHERE "Id" = '89de4fb3-b401-e463-94f4-d6cef1ee18a4';

DELETE FROM advance.departments
WHERE "Id" = '8d38e3c9-d05b-270c-cec0-448e0020e2dc';

DELETE FROM advance.departments
WHERE "Id" = '92f4dfe8-2c81-11db-0e68-ae27000fd606';

DELETE FROM advance.departments
WHERE "Id" = '97353e2b-c03c-03ad-dad5-07e697b6429f';

DELETE FROM advance.departments
WHERE "Id" = 'dce26b11-c1b4-9be5-f2e9-ca1d50e45d64';

DELETE FROM advance.departments
WHERE "Id" = 'dd6ab604-a58e-4884-7df9-2ceb7456df64';

DELETE FROM advance.departments
WHERE "Id" = 'e7bcb9d9-f7df-5ecc-fd69-54fb437e2f5e';

DELETE FROM advance.designations
WHERE "Id" = '9865dd7b-b329-ccef-54cd-67bdc1cfdd27';

DELETE FROM advance.designations
WHERE "Id" = 'b6bd22cd-d47c-d8b2-daff-19a3d7c33d5d';

DELETE FROM advance.departments
WHERE "Id" = '1c67b401-c1d8-7d42-e3f9-82217720202f';

ALTER TABLE advance.organization_policies ENABLE TRIGGER trg_rev869a_policy_version_guard;

UPDATE advance.purchase_transaction_approval_policies SET "OrganizationId" = 'SESS'
WHERE "Id" = '0e6e49ea-95c5-86dd-1c23-18d61c50f4c1';

UPDATE advance.purchase_transaction_approval_policies SET "OrganizationId" = 'SESS'
WHERE "Id" = 'd7b12d20-a4be-c916-9f5e-de2245510b91';

UPDATE advance.purchase_transaction_approval_policies SET "OrganizationId" = 'SESS'
WHERE "Id" = 'f9505a0c-182b-7627-52f4-1197a29e4c16';

DELETE FROM advance."__EFMigrationsHistory"
WHERE "MigrationId" = '20260824135450_MultiCompanySharedIdentityFoundation';

COMMIT;

