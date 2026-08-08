# REV861 PostgreSQL Object Catalogue

Generated: 2026-08-08T10:27:26.497Z

Tables detected: 20
Indexes detected: 49

## Tables

- deleted_record_logs
- erp_company_ledger_records
- erp_db_state
- erp_json_master_snapshot
- holiday_master
- ops_flow_status
- page_form_records
- project_flow_status
- project_ledger_records
- purchase_flow_status
- sales_flow_status
- service_assets
- service_flow_status
- service_ledger_records
- simple_master_records
- stage_template_lines
- stage_templates
- stock_movements
- vendor_rating_records
- work_register_records

## Indexes

- idx_deleted_record_logs_company_deleted
- idx_deleted_record_logs_payload_gin
- idx_deleted_record_logs_table_key
- idx_erp_company_ledger_company_key_record
- idx_erp_company_ledger_company_key_updated
- idx_erp_company_ledger_payload_gin
- idx_erp_company_ledger_search_trgm
- idx_erp_db_state_payload_gin
- idx_holiday_master_company_date
- idx_json_master_snapshot_payload_gin
- idx_json_master_snapshot_source_ord
- idx_json_master_snapshot_source_search
- idx_ops_flow_status_flow
- idx_ops_flow_status_gate
- idx_ops_flow_status_payload_gin
- idx_page_form_records_page_updated
- idx_page_form_records_payload_gin
- idx_project_flow_status_flow
- idx_project_flow_status_gate
- idx_project_flow_status_payload_gin
- idx_project_ledger_records_key_updated
- idx_project_ledger_records_payload_gin
- idx_purchase_flow_status_flow
- idx_purchase_flow_status_gate
- idx_purchase_flow_status_payload_gin
- idx_sales_flow_status_flow
- idx_sales_flow_status_gate
- idx_sales_flow_status_payload_gin
- idx_service_assets_company_updated
- idx_service_assets_payload_gin
- idx_service_flow_status_flow
- idx_service_flow_status_gate
- idx_service_flow_status_payload_gin
- idx_service_ledger_records_key_updated
- idx_service_ledger_records_payload_gin
- idx_simple_master_records_page_updated
- idx_simple_master_records_payload_gin
- idx_stage_template_lines_company_template
- idx_stage_templates_company_updated
- idx_stock_movements_item
- idx_stock_movements_payload_gin
- idx_stock_movements_reference
- idx_vendor_rating_records_company_fy
- idx_vendor_rating_records_payload_gin
- idx_vendor_rating_records_vendor
- idx_work_register_records_company_engineer
- idx_work_register_records_company_status
- idx_work_register_records_company_updated
- idx_work_register_records_payload_gin

## Tables Touched By ALTER TABLE

- None detected
