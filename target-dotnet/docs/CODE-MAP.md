# R1 code map

Source inventory prepared 2 October 2026 (IST) from `31517ad51b0be70ff05fccb24597978edbe4ed92`. This maps the checked-out .NET tree and frontend, not installed-system readback or a release-candidate claim. Frontend release selection follows the TD's named-SHA process. No code, schema, filenames or business decisions change here.

Start at [App.tsx](../src/SESS.NexaERP.Web/src/App.tsx) for routes, [Program.cs](../src/SESS.NexaERP.Api/Program.cs) for endpoint registration and [DependencyInjection.cs](../src/SESS.NexaERP.Infrastructure/DependencyInjection.cs) for wiring. Frontend `src/api/` contains bindings; `src/types/` mirrors contracts. Application interfaces live in `src/SESS.NexaERP.Application/`; domain rules live in `src/SESS.NexaERP.Domain/`.

## Reading the map

Entries list actual files, primary tables, entry migrations and focused tests, not every dependency. Tables normally use `advance`; exact mappings and constraints are in `Infrastructure/Persistence/NexaErpDbContext*.cs` and migration SQL. All modules inherit [AdvanceInitialBaseline](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260824032638_AdvanceInitialBaseline.cs); later migrations are additive corrections. Historical REV migrations mentioned in reports are not necessarily in today's EF chain.

Shared proof: [AdvanceMigrationSqlSyntaxTests.cs](../tests/SESS.NexaERP.Tests/AdvanceMigrationSqlSyntaxTests.cs), [FreshCompanyStoresFoundationTests.cs](../tests/SESS.NexaERP.Tests/FreshCompanyStoresFoundationTests.cs), [FreshCompanyOperationsRehearsalTests.cs](../tests/SESS.NexaERP.Tests/FreshCompanyOperationsRehearsalTests.cs) and [ApiWireContractTests.cs](../tests/SESS.NexaERP.Tests/ApiWireContractTests.cs). These identify evidence for code changes; they do not assert new runtime results for this docs change. PostgreSQL tests use disposable clusters only.

## Screens and modules

### Employee master

Maintain employees and effective assignments. CRUD is in the endpoints; the resolver handles identity.

| Layer | Files / tables |
|---|---|
| Frontend | [EmployeeDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/employees/EmployeeDetailPage.tsx), [EmployeeFormModal.tsx](../src/SESS.NexaERP.Web/src/features/employees/EmployeeFormModal.tsx), [EmployeeListPage.tsx](../src/SESS.NexaERP.Web/src/features/employees/EmployeeListPage.tsx), [StatusBadge.tsx](../src/SESS.NexaERP.Web/src/features/employees/StatusBadge.tsx) |
| API endpoint | [EmployeeEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/EmployeeEndpoints.cs), [EmployeeRoleGovernanceEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/EmployeeRoleGovernanceEndpoints.cs) |
| Service / implementation | [EfEmployeeIdentityResolver.cs](../src/SESS.NexaERP.Infrastructure/Identity/EfEmployeeIdentityResolver.cs) |
| Main DB tables | `employees, departments, designations, employee_role_assignments` |
| Main migrations | [20260825063221_EmployeeMasterRebuild42.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260825063221_EmployeeMasterRebuild42.cs), [20260825125621_MultiCompanyEmployeeAuthorizationPart1.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260825125621_MultiCompanyEmployeeAuthorizationPart1.cs) |
| Focused tests | [EmployeeMasterRebuild42MigrationTests.cs](../tests/SESS.NexaERP.Tests/EmployeeMasterRebuild42MigrationTests.cs), [EmployeeRoleGovernancePhase2Tests.cs](../tests/SESS.NexaERP.Tests/EmployeeRoleGovernancePhase2Tests.cs) |

### Vendor master

Maintain supplier details, qualification and commercial approval. Customer and vendor share endpoint partials; imports use separate service entry points.

| Layer | Files / tables |
|---|---|
| Frontend | [VendorDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/vendors/VendorDetailPage.tsx), [VendorFormModal.tsx](../src/SESS.NexaERP.Web/src/features/vendors/VendorFormModal.tsx), [VendorListPage.tsx](../src/SESS.NexaERP.Web/src/features/vendors/VendorListPage.tsx), [VerifyCommercialModal.tsx](../src/SESS.NexaERP.Web/src/features/vendors/VerifyCommercialModal.tsx) |
| API endpoint | [MasterEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.cs), [MasterEndpoints.CustomerAttachments.cs](../src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.CustomerAttachments.cs), [MasterEndpoints.Rev869A.cs](../src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.Rev869A.cs), [MasterEndpoints.VendorAttachments.cs](../src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.VendorAttachments.cs), [VendorManualAssessmentEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/VendorManualAssessmentEndpoints.cs) |
| Service / implementation | [EfPartyMasterDataServices.cs](../src/SESS.NexaERP.Infrastructure/MasterData/EfPartyMasterDataServices.cs), [EfRev869AFoundationServices.cs](../src/SESS.NexaERP.Infrastructure/Masters/EfRev869AFoundationServices.cs) |
| Main DB tables | `vendors, vendor_contacts, vendor_addresses, vendor_qualifications, vendor_attachments` |
| Main migrations | [20260829091754_VendorAttachments.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260829091754_VendorAttachments.cs), [20260929143000_VendorImportSourceApproval.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260929143000_VendorImportSourceApproval.cs) |
| Focused tests | [CustomerVendorMasterDataAdapterTests.cs](../tests/SESS.NexaERP.Tests/CustomerVendorMasterDataAdapterTests.cs), [Rev867MasterFoundationTests.cs](../tests/SESS.NexaERP.Tests/Rev867MasterFoundationTests.cs) |

### Customer master

Maintain customer identity, addresses and attachments.

| Layer | Files / tables |
|---|---|
| Frontend | [CustomerDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/customers/CustomerDetailPage.tsx), [CustomerFormModal.tsx](../src/SESS.NexaERP.Web/src/features/customers/CustomerFormModal.tsx), [CustomerListPage.tsx](../src/SESS.NexaERP.Web/src/features/customers/CustomerListPage.tsx) |
| API endpoint | [MasterEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.cs), [MasterEndpoints.CustomerAttachments.cs](../src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.CustomerAttachments.cs) |
| Service / implementation | [EfPartyMasterDataServices.cs](../src/SESS.NexaERP.Infrastructure/MasterData/EfPartyMasterDataServices.cs) |
| Main DB tables | `customers, customer_contacts, customer_addresses, customer_attachments` |
| Main migrations | [20260829045502_CompanyRelationshipExternalCodes.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260829045502_CompanyRelationshipExternalCodes.cs), [20260829102434_CustomerAttachmentsAndBank.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260829102434_CustomerAttachmentsAndBank.cs) |
| Focused tests | [CompanyRelationshipExternalCodesTests.cs](../tests/SESS.NexaERP.Tests/CompanyRelationshipExternalCodesTests.cs), [CustomerVendorMasterDataAdapterTests.cs](../tests/SESS.NexaERP.Tests/CustomerVendorMasterDataAdapterTests.cs) |

### Item master

Maintain stock items, units and tracking attributes. Interactive CRUD is in InventoryEndpoints; adapters implement import.

| Layer | Files / tables |
|---|---|
| Frontend | [ItemDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/items/ItemDetailPage.tsx), [ItemFormModal.tsx](../src/SESS.NexaERP.Web/src/features/items/ItemFormModal.tsx), [ItemListPage.tsx](../src/SESS.NexaERP.Web/src/features/items/ItemListPage.tsx) |
| API endpoint | [InventoryEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/InventoryEndpoints.cs), [ItemVendorEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/ItemVendorEndpoints.cs), [ReferenceMasterEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/ReferenceMasterEndpoints.cs) |
| Service / implementation | [ItemImportValues.cs](../src/SESS.NexaERP.Infrastructure/MasterData/ItemImportValues.cs), [OperationalMasterDataAdapters.cs](../src/SESS.NexaERP.Infrastructure/MasterData/OperationalMasterDataAdapters.cs) |
| Main DB tables | `items, item_categories, item_subcategories, uoms, manufacturers, item_vendors, item_images, item_merge_aliases` |
| Main migrations | [20260828121759_ItemReferenceMasterFrontendReadiness.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260828121759_ItemReferenceMasterFrontendReadiness.cs), [20260829115858_ItemVendorsAndImages.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260829115858_ItemVendorsAndImages.cs), [20260920130000_ItemMasterApprovalAuthority.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260920130000_ItemMasterApprovalAuthority.cs), [20260920140000_ItemMergeDirectorAuthority.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260920140000_ItemMergeDirectorAuthority.cs) |
| Focused tests | [ItemImportV2Tests.cs](../tests/SESS.NexaERP.Tests/ItemImportV2Tests.cs), [ItemMasterFrontendReadinessTests.cs](../tests/SESS.NexaERP.Tests/ItemMasterFrontendReadinessTests.cs), [ItemUomRuntimeReachabilityTests.cs](../tests/SESS.NexaERP.Tests/ItemUomRuntimeReachabilityTests.cs) |

### Warehouse / Rack-Bin / reference masters

Define storage locations and form references. App.tsx shows Warehouse / Rack-Bin as disabled; no dedicated page exists here.

| Layer | Files / tables |
|---|---|
| Frontend | No dedicated file; see purpose / note. |
| API endpoint | [MasterDataTransferEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/MasterDataTransferEndpoints.cs), [ReferenceMasterEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/ReferenceMasterEndpoints.cs) |
| Service / implementation | [WarehouseRackMasterDataAdapters.cs](../src/SESS.NexaERP.Infrastructure/MasterData/WarehouseRackMasterDataAdapters.cs), [EfUomMasterService.cs](../src/SESS.NexaERP.Infrastructure/Masters/EfUomMasterService.cs), [EfWarehouseRackMasterDataServices.cs](../src/SESS.NexaERP.Infrastructure/Masters/EfWarehouseRackMasterDataServices.cs) |
| Main DB tables | `warehouses, rack_bins, warehouse_condition_locations, uoms, item_categories, manufacturers` |
| Main migrations | [20260831075228_WarehouseAndRackMaster.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260831075228_WarehouseAndRackMaster.cs), [20260920090000_StoresWarehouseRackGrants.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260920090000_StoresWarehouseRackGrants.cs) |
| Focused tests | [OperationalMasterDataAdapterTests.cs](../tests/SESS.NexaERP.Tests/OperationalMasterDataAdapterTests.cs), [WarehouseRackMasterTests.cs](../tests/SESS.NexaERP.Tests/WarehouseRackMasterTests.cs) |

### Company profile

Provide company address, tax identity and print headers.

| Layer | Files / tables |
|---|---|
| Frontend | [CompanyProfilePage.tsx](../src/SESS.NexaERP.Web/src/features/company/CompanyProfilePage.tsx) |
| API endpoint | [CompanyProfileEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/CompanyProfileEndpoints.cs) |
| Service / implementation | [CompanyPrintBlock.cs](../src/SESS.NexaERP.Infrastructure/Masters/CompanyPrintBlock.cs), [EfCompanyProfileService.cs](../src/SESS.NexaERP.Infrastructure/Masters/EfCompanyProfileService.cs) |
| Main DB tables | `companies, warehouses` |
| Main migrations | [20260926110000_CompanyProfileAndWarehouseState.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260926110000_CompanyProfileAndWarehouseState.cs) |
| Focused tests | [CompanyProfileTests.cs](../tests/SESS.NexaERP.Tests/CompanyProfileTests.cs), [PrintContractTests.cs](../tests/SESS.NexaERP.Tests/PrintContractTests.cs) |

### Configuration / GST / approval routes

Configure effective rules used by purchasing, stores and access. No general configuration screen exists; QC policy has its own screen.

| Layer | Files / tables |
|---|---|
| Frontend | No dedicated file; see purpose / note. |
| API endpoint | [MasterEndpoints.Rev869A.cs](../src/SESS.NexaERP.Api/Endpoints/MasterEndpoints.Rev869A.cs), [Rev869AConfigurationEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.cs), [Rev869AConfigurationEndpoints.QcPolicies.cs](../src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.QcPolicies.cs), [Rev869AConfigurationEndpoints.StoreCategoryRoutes.cs](../src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.StoreCategoryRoutes.cs), [Rev869AConfigurationReadEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationReadEndpoints.cs) |
| Service / implementation | [EfRev869AFoundationServices.cs](../src/SESS.NexaERP.Infrastructure/Masters/EfRev869AFoundationServices.cs), [EfTaxGstWorkflowService.cs](../src/SESS.NexaERP.Infrastructure/Masters/EfTaxGstWorkflowService.cs) |
| Main DB tables | `tax_gst_settings, uom_conversions, organization_policies, employee_operational_scopes, controlled_configuration_histories, store_category_routes, purchase_transaction_approval_policies` |
| Main migrations | [20260825135023_ApprovalConfigurationAndPermissionsPart2.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260825135023_ApprovalConfigurationAndPermissionsPart2.cs), [20260829114544_ControlledTaxGstWorkflow.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260829114544_ControlledTaxGstWorkflow.cs), [20260920100000_StoreCategoryRoutePage.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260920100000_StoreCategoryRoutePage.cs) |
| Focused tests | [ApprovalConfigurationPart2Tests.cs](../tests/SESS.NexaERP.Tests/ApprovalConfigurationPart2Tests.cs), [ConfigurationReadReachabilityTests.cs](../tests/SESS.NexaERP.Tests/ConfigurationReadReachabilityTests.cs), [Rev869AFoundationTests.cs](../tests/SESS.NexaERP.Tests/Rev869AFoundationTests.cs) |

### Purchase PR

Request material and route it through approval and stock checks.

| Layer | Files / tables |
|---|---|
| Frontend | [PurchaseRequisitionDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/purchase/PurchaseRequisitionDetailPage.tsx), [PurchaseRequisitionFormModal.tsx](../src/SESS.NexaERP.Web/src/features/purchase/PurchaseRequisitionFormModal.tsx), [PurchaseRequisitionListPage.tsx](../src/SESS.NexaERP.Web/src/features/purchase/PurchaseRequisitionListPage.tsx) |
| API endpoint | [PurchaseRequisitionEndpointHelpers.cs](../src/SESS.NexaERP.Api/Endpoints/PurchaseRequisitionEndpointHelpers.cs), [PurchaseRequisitionEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/PurchaseRequisitionEndpoints.cs), [PurchaseRequisitionLookupEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/PurchaseRequisitionLookupEndpoints.cs), [PurchaseRequisitionSupport.cs](../src/SESS.NexaERP.Api/Endpoints/PurchaseRequisitionSupport.cs) |
| Service / implementation | [EfPurchaseApprovalWorkflowService.cs](../src/SESS.NexaERP.Infrastructure/Purchase/EfPurchaseApprovalWorkflowService.cs), [EfPurchaseRequisitionWorkflowService.cs](../src/SESS.NexaERP.Infrastructure/Purchase/EfPurchaseRequisitionWorkflowService.cs) |
| Main DB tables | `purchase_requisitions, purchase_requisition_lines, purchase_requisition_approval_history` |
| Main migrations | [20260826054057_TwoLevelPurchaseApprovalEnginePart3.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260826054057_TwoLevelPurchaseApprovalEnginePart3.cs), [20260903075214_ApprovalChainReachabilityAndVisibilityCorrections.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260903075214_ApprovalChainReachabilityAndVisibilityCorrections.cs) |
| Focused tests | [PurchaseApprovalEnginePart3Tests.cs](../tests/SESS.NexaERP.Tests/PurchaseApprovalEnginePart3Tests.cs), [PurchaseRequisitionCreationFailureTests.cs](../tests/SESS.NexaERP.Tests/PurchaseRequisitionCreationFailureTests.cs), [Rev868PurchaseRequisitionTests.cs](../tests/SESS.NexaERP.Tests/Rev868PurchaseRequisitionTests.cs) |

### RFQ

Invite eligible suppliers to quote for approved demand.

| Layer | Files / tables |
|---|---|
| Frontend | [RfqCreateModal.tsx](../src/SESS.NexaERP.Web/src/features/purchase/RfqCreateModal.tsx), [RfqDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/purchase/RfqDetailPage.tsx), [RfqListPage.tsx](../src/SESS.NexaERP.Web/src/features/purchase/RfqListPage.tsx) |
| API endpoint | [Rev869BPurchaseEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs), [Rev869BPurchaseReadEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseReadEndpoints.cs) |
| Service / implementation | [EfRev869BPurchaseService.cs](../src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs), [EfRev869BPurchaseService.RfqQuotation.cs](../src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs) |
| Main DB tables | `request_for_quotations, request_for_quotation_lines, rfq_vendor_invitations` |
| Main migrations | [20260824032638_AdvanceInitialBaseline.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260824032638_AdvanceInitialBaseline.cs) |
| Focused tests | [PurchaseFlowEndToEndPostgreSqlTests.cs](../tests/SESS.NexaERP.Tests/PurchaseFlowEndToEndPostgreSqlTests.cs), [Rev869BPurchaseBehaviorTests.cs](../tests/SESS.NexaERP.Tests/Rev869BPurchaseBehaviorTests.cs), [Rev869BPurchaseCorrectionTests.cs](../tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs), [Rev869BPurchaseFoundationTests.cs](../tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs) |

### Quotation

Record supplier offers and technical verification.

| Layer | Files / tables |
|---|---|
| Frontend | [QuotationListPage.tsx](../src/SESS.NexaERP.Web/src/features/purchase/QuotationListPage.tsx), [QuotationPage.tsx](../src/SESS.NexaERP.Web/src/features/purchase/QuotationPage.tsx) |
| API endpoint | [Rev869BPurchaseEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs), [Rev869BPurchaseReadEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseReadEndpoints.cs) |
| Service / implementation | [EfRev869BPurchaseService.cs](../src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs), [EfRev869BPurchaseService.RfqQuotation.cs](../src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.RfqQuotation.cs) |
| Main DB tables | `vendor_quotations, vendor_quotation_lines, quotation_technical_verifications` |
| Main migrations | [20260824032638_AdvanceInitialBaseline.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260824032638_AdvanceInitialBaseline.cs), [20260918093000_TechnicalVerifierQuotationRead.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260918093000_TechnicalVerifierQuotationRead.cs) |
| Focused tests | [PurchaseFlowEndToEndPostgreSqlTests.cs](../tests/SESS.NexaERP.Tests/PurchaseFlowEndToEndPostgreSqlTests.cs), [Rev869BPurchaseBehaviorTests.cs](../tests/SESS.NexaERP.Tests/Rev869BPurchaseBehaviorTests.cs), [Rev869BPurchaseCorrectionTests.cs](../tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs), [Rev869BPurchaseFoundationTests.cs](../tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs) |

### Comparison

Compare technically qualified offers and approve a recommendation.

| Layer | Files / tables |
|---|---|
| Frontend | [ComparisonDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/purchase/ComparisonDetailPage.tsx), [ComparisonListPage.tsx](../src/SESS.NexaERP.Web/src/features/purchase/ComparisonListPage.tsx) |
| API endpoint | [Rev869BPurchaseEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs), [Rev869BPurchaseReadEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseReadEndpoints.cs) |
| Service / implementation | [EfRev869BPurchaseService.ComparisonPo.cs](../src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs), [EfRev869BPurchaseService.cs](../src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs) |
| Main DB tables | `commercial_comparisons, commercial_comparison_lines, purchase_transaction_approval_history` |
| Main migrations | [20260824032638_AdvanceInitialBaseline.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260824032638_AdvanceInitialBaseline.cs) |
| Focused tests | [PurchaseFlowEndToEndPostgreSqlTests.cs](../tests/SESS.NexaERP.Tests/PurchaseFlowEndToEndPostgreSqlTests.cs), [Rev869BPurchaseBehaviorTests.cs](../tests/SESS.NexaERP.Tests/Rev869BPurchaseBehaviorTests.cs), [Rev869BPurchaseCorrectionTests.cs](../tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs), [Rev869BPurchaseFoundationTests.cs](../tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs) |

### Purchase PO

Issue approved supplier orders and retain revisions and cancellations.

| Layer | Files / tables |
|---|---|
| Frontend | [PurchaseOrderDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/purchase/PurchaseOrderDetailPage.tsx), [PurchaseOrderListPage.tsx](../src/SESS.NexaERP.Web/src/features/purchase/PurchaseOrderListPage.tsx) |
| API endpoint | [Rev869BPurchaseEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.cs), [Rev869BPurchaseReadEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseReadEndpoints.cs) |
| Service / implementation | [EfRev869BPurchaseService.ComparisonPo.cs](../src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.ComparisonPo.cs), [EfRev869BPurchaseService.cs](../src/SESS.NexaERP.Infrastructure/Purchase/EfRev869BPurchaseService.cs) |
| Main DB tables | `purchase_orders, purchase_order_lines, purchase_transaction_status_history` |
| Main migrations | [20260824032638_AdvanceInitialBaseline.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260824032638_AdvanceInitialBaseline.cs), [20260913095000_PurchaseOrderSupersedeHistory.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260913095000_PurchaseOrderSupersedeHistory.cs), [20260914045000_PurchaseOrderCancellationHistory.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260914045000_PurchaseOrderCancellationHistory.cs) |
| Focused tests | [PurchaseOrderCancellationTests.cs](../tests/SESS.NexaERP.Tests/PurchaseOrderCancellationTests.cs), [PurchaseOrderReceiptRevisionTests.cs](../tests/SESS.NexaERP.Tests/PurchaseOrderReceiptRevisionTests.cs), [Rev869BPurchaseBehaviorTests.cs](../tests/SESS.NexaERP.Tests/Rev869BPurchaseBehaviorTests.cs), [Rev869BPurchaseCorrectionTests.cs](../tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs), [Rev869BPurchaseFoundationTests.cs](../tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs) |

### PO print

Render purchase orders from authorized print data.

| Layer | Files / tables |
|---|---|
| Frontend | [PrintDocumentPage.tsx](../src/SESS.NexaERP.Web/src/print/PrintDocumentPage.tsx), [PurchaseOrderPrint.tsx](../src/SESS.NexaERP.Web/src/print/PurchaseOrderPrint.tsx) |
| API endpoint | [Rev869BPurchaseEndpoints.Print.cs](../src/SESS.NexaERP.Api/Endpoints/Rev869BPurchaseEndpoints.Print.cs) |
| Service / implementation | [EfPurchaseOrderPrintQuery.cs](../src/SESS.NexaERP.Infrastructure/Purchase/EfPurchaseOrderPrintQuery.cs) |
| Main DB tables | `purchase_orders, purchase_order_lines, companies, vendors` |
| Main migrations | [20260926140000_PrintPermissionGrants.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260926140000_PrintPermissionGrants.cs) |
| Focused tests | [PrintContractTests.cs](../tests/SESS.NexaERP.Tests/PrintContractTests.cs) |

### Gate Entry

Record material arriving at the gate before stores receipt.

| Layer | Files / tables |
|---|---|
| Frontend | [GateEntryDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/GateEntryDetailPage.tsx), [GateEntryFormModal.tsx](../src/SESS.NexaERP.Web/src/features/stores/GateEntryFormModal.tsx), [GateEntryListPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/GateEntryListPage.tsx) |
| API endpoint | [StoresGateEntryEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/StoresGateEntryEndpoints.cs) |
| Service / implementation | [EfGateEntryService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfGateEntryService.cs), [EfGateEntryService.Queries.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfGateEntryService.Queries.cs) |
| Main DB tables | `gate_entries, gate_entry_lines, stores_document_status_history` |
| Main migrations | [20260827093952_FirstStoresPart1FoundationInboundNotifications.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260827093952_FirstStoresPart1FoundationInboundNotifications.cs), [20260831052559_StoresSlice0ControlledPostingAndGateApi.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260831052559_StoresSlice0ControlledPostingAndGateApi.cs) |
| Focused tests | [FirstStoresPart1MigrationTests.cs](../tests/SESS.NexaERP.Tests/FirstStoresPart1MigrationTests.cs), [FreshCompanyOperationsRehearsalTests.cs](../tests/SESS.NexaERP.Tests/FreshCompanyOperationsRehearsalTests.cs) |

### GRN

Receive goods into controlled lot and serial custody.

| Layer | Files / tables |
|---|---|
| Frontend | [GoodsReceiptDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/GoodsReceiptDetailPage.tsx), [GoodsReceiptFormModal.tsx](../src/SESS.NexaERP.Web/src/features/stores/GoodsReceiptFormModal.tsx), [GoodsReceiptListPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/GoodsReceiptListPage.tsx) |
| API endpoint | [StoresGoodsReceiptEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/StoresGoodsReceiptEndpoints.cs) |
| Service / implementation | [EfGoodsReceiptService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfGoodsReceiptService.cs) |
| Main DB tables | `goods_receipts, goods_receipt_lines, inventory_lots, inventory_serials, goods_receipt_line_lot_allocations, goods_receipt_line_serials` |
| Main migrations | [20260827110550_FirstStoresPart2GrnAndSerials.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260827110550_FirstStoresPart2GrnAndSerials.cs), [20260901042749_StoresSlice2GrnCustodyPosting.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260901042749_StoresSlice2GrnCustodyPosting.cs), [20260901140623_StoresReceiptAuthorizationCorrections.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260901140623_StoresReceiptAuthorizationCorrections.cs) |
| Focused tests | [ConcurrentGoodsReceiptTests.cs](../tests/SESS.NexaERP.Tests/ConcurrentGoodsReceiptTests.cs), [FirstStoresPart2MigrationTests.cs](../tests/SESS.NexaERP.Tests/FirstStoresPart2MigrationTests.cs), [QcGrnSegregationTests.cs](../tests/SESS.NexaERP.Tests/QcGrnSegregationTests.cs) |

### QC inspection

Inspect received goods and retain lot or serial disposition evidence.

| Layer | Files / tables |
|---|---|
| Frontend | [QcDispositionForm.tsx](../src/SESS.NexaERP.Web/src/features/qc/QcDispositionForm.tsx), [QcInspectionPage.tsx](../src/SESS.NexaERP.Web/src/features/qc/QcInspectionPage.tsx), [QcInspectPage.tsx](../src/SESS.NexaERP.Web/src/features/qc/QcInspectPage.tsx), [QcQueuePage.tsx](../src/SESS.NexaERP.Web/src/features/qc/QcQueuePage.tsx) |
| API endpoint | [QcEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/QcEndpoints.cs) |
| Service / implementation | [EfQcWorkflowService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfQcWorkflowService.cs) |
| Main DB tables | `qc_inspections, qc_inspection_revisions, qc_inspection_parameter_results, qc_inspection_serial_dispositions, qc_inspection_lot_dispositions` |
| Main migrations | [20260827115729_FirstStoresPart3AQcOutboundDocuments.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260827115729_FirstStoresPart3AQcOutboundDocuments.cs), [20260902114019_StoresSlice3QcConcessionActivation.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260902114019_StoresSlice3QcConcessionActivation.cs), [20260911104631_AlignGrnQcDueAtWithReceiptTime.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260911104631_AlignGrnQcDueAtWithReceiptTime.cs) |
| Focused tests | [FirstStoresPart3AMigrationTests.cs](../tests/SESS.NexaERP.Tests/FirstStoresPart3AMigrationTests.cs), [QcCorrectionRuntimeTests.cs](../tests/SESS.NexaERP.Tests/QcCorrectionRuntimeTests.cs), [QcDueAtReceiptTimeCorrectionTests.cs](../tests/SESS.NexaERP.Tests/QcDueAtReceiptTimeCorrectionTests.cs), [QcFrontendApiCorrectionTests.cs](../tests/SESS.NexaERP.Tests/QcFrontendApiCorrectionTests.cs), [QcGrnSegregationTests.cs](../tests/SESS.NexaERP.Tests/QcGrnSegregationTests.cs), [QcMultiSerialReachabilityTests.cs](../tests/SESS.NexaERP.Tests/QcMultiSerialReachabilityTests.cs), [QcPolicyReachabilityRuntimeTests.cs](../tests/SESS.NexaERP.Tests/QcPolicyReachabilityRuntimeTests.cs) |

### QC concession

Authorize retained exceptions to failed inspection dispositions.

| Layer | Files / tables |
|---|---|
| Frontend | [ConcessionPage.tsx](../src/SESS.NexaERP.Web/src/features/qc/ConcessionPage.tsx) |
| API endpoint | [QcEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/QcEndpoints.cs) |
| Service / implementation | [EfQcWorkflowService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfQcWorkflowService.cs) |
| Main DB tables | `inventory_concessions, inventory_concession_allocations, inventory_concession_allocation_serials` |
| Main migrations | [20260903103611_CorrectQcConcessionAuthority.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260903103611_CorrectQcConcessionAuthority.cs), [20260913050000_ConcessionSerialDecisionHistory.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260913050000_ConcessionSerialDecisionHistory.cs) |
| Focused tests | [ConcessionSerialHistoryTests.cs](../tests/SESS.NexaERP.Tests/ConcessionSerialHistoryTests.cs), [ConcurrentQcConcessionTests.cs](../tests/SESS.NexaERP.Tests/ConcurrentQcConcessionTests.cs) |

### QC policy

Maintain effective inspection parameters and sampling decisions.

| Layer | Files / tables |
|---|---|
| Frontend | [QcPolicyPage.tsx](../src/SESS.NexaERP.Web/src/features/qc/QcPolicyPage.tsx) |
| API endpoint | [Rev869AConfigurationEndpoints.QcPolicies.cs](../src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationEndpoints.QcPolicies.cs), [Rev869AConfigurationReadEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/Rev869AConfigurationReadEndpoints.cs) |
| Service / implementation | [EfRev869AFoundationServices.cs](../src/SESS.NexaERP.Infrastructure/Masters/EfRev869AFoundationServices.cs) |
| Main DB tables | `qc_inspection_policies, controlled_configuration_histories` |
| Main migrations | [20260824032638_AdvanceInitialBaseline.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260824032638_AdvanceInitialBaseline.cs), [20260916170000_QcPolicyDecisions.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260916170000_QcPolicyDecisions.cs) |
| Focused tests | [QcPolicyReachabilityRuntimeTests.cs](../tests/SESS.NexaERP.Tests/QcPolicyReachabilityRuntimeTests.cs), [Rev869AFoundationTests.cs](../tests/SESS.NexaERP.Tests/Rev869AFoundationTests.cs) |

### Stock Check

Check available stock before advancing purchase demand.

| Layer | Files / tables |
|---|---|
| Frontend | [StockCheckPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/StockCheckPage.tsx) |
| API endpoint | [InventoryEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/InventoryEndpoints.cs), [PurchaseRequisitionEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/PurchaseRequisitionEndpoints.cs) |
| Service / implementation | [EfPurchaseRequisitionWorkflowService.cs](../src/SESS.NexaERP.Infrastructure/Purchase/EfPurchaseRequisitionWorkflowService.cs) |
| Main DB tables | `stock_movements, purchase_requisitions, purchase_requisition_lines` |
| Main migrations | [20260903125259_StockCheckAndPriyaPurchaseScopeV2.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260903125259_StockCheckAndPriyaPurchaseScopeV2.cs), [20260904075003_MultiCompanyPrAndStoresStockCheckCorrection.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260904075003_MultiCompanyPrAndStoresStockCheckCorrection.cs) |
| Focused tests | [MultiCompanyNumberAndStockCheckTests.cs](../tests/SESS.NexaERP.Tests/MultiCompanyNumberAndStockCheckTests.cs), [Rev868PurchaseRequisitionTests.cs](../tests/SESS.NexaERP.Tests/Rev868PurchaseRequisitionTests.cs) |

### MIR

Request and approve material for an operational need.

| Layer | Files / tables |
|---|---|
| Frontend | [MaterialIssueRequestDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/MaterialIssueRequestDetailPage.tsx), [MaterialIssueRequestFormModal.tsx](../src/SESS.NexaERP.Web/src/features/stores/MaterialIssueRequestFormModal.tsx), [MaterialIssueRequestListPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/MaterialIssueRequestListPage.tsx) |
| API endpoint | [MaterialIssueEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/MaterialIssueEndpoints.cs) |
| Service / implementation | [EfMaterialIssueService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfMaterialIssueService.cs), [EfMaterialIssueService.Read.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfMaterialIssueService.Read.cs), [EfMaterialIssueService.RequestCommands.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfMaterialIssueService.RequestCommands.cs) |
| Main DB tables | `material_issue_requests, material_issue_request_lines, stores_approval_history` |
| Main migrations | [20260907134726_MaterialIssueRequestAndCustodyIssue.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260907134726_MaterialIssueRequestAndCustodyIssue.cs), [20260910115815_CorrectMaterialIssueSituationBaselines.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260910115815_CorrectMaterialIssueSituationBaselines.cs) |
| Focused tests | [ConcurrentMirApprovalTests.cs](../tests/SESS.NexaERP.Tests/ConcurrentMirApprovalTests.cs), [MaterialIssueMigrationTests.cs](../tests/SESS.NexaERP.Tests/MaterialIssueMigrationTests.cs) |

### Material Issue

Issue available stock into custody with retained posting evidence.

| Layer | Files / tables |
|---|---|
| Frontend | [MaterialIssueDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/MaterialIssueDetailPage.tsx), [MaterialIssueFormModal.tsx](../src/SESS.NexaERP.Web/src/features/stores/MaterialIssueFormModal.tsx), [MaterialIssueListPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/MaterialIssueListPage.tsx) |
| API endpoint | [MaterialIssueEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/MaterialIssueEndpoints.cs) |
| Service / implementation | [EfMaterialIssueService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfMaterialIssueService.cs), [EfMaterialIssueService.Issue.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfMaterialIssueService.Issue.cs) |
| Main DB tables | `material_issues, material_issue_lines, material_issue_history, stock_movements, fifo_cost_consumptions` |
| Main migrations | [20260907134726_MaterialIssueRequestAndCustodyIssue.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260907134726_MaterialIssueRequestAndCustodyIssue.cs), [20260910115815_CorrectMaterialIssueSituationBaselines.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260910115815_CorrectMaterialIssueSituationBaselines.cs) |
| Focused tests | [ConcurrentSerialIssueTests.cs](../tests/SESS.NexaERP.Tests/ConcurrentSerialIssueTests.cs), [MaterialIssueMigrationTests.cs](../tests/SESS.NexaERP.Tests/MaterialIssueMigrationTests.cs), [MaterialIssueSituationBaselineMigrationTests.cs](../tests/SESS.NexaERP.Tests/MaterialIssueSituationBaselineMigrationTests.cs) |

### Material Return

Return issued material while preserving source and costing history.

| Layer | Files / tables |
|---|---|
| Frontend | [MaterialReturnFormModal.tsx](../src/SESS.NexaERP.Web/src/features/stores/MaterialReturnFormModal.tsx), [MaterialReturnListPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/MaterialReturnListPage.tsx) |
| API endpoint | [MaterialIssueEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/MaterialIssueEndpoints.cs) |
| Service / implementation | [EfMaterialIssueService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfMaterialIssueService.cs), [EfMaterialIssueService.Return.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfMaterialIssueService.Return.cs) |
| Main DB tables | `material_returns, material_return_lines, material_return_history` |
| Main migrations | [20260907182204_MaterialReturnToStores.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260907182204_MaterialReturnToStores.cs), [20260914080000_FifoReturnRestorations.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260914080000_FifoReturnRestorations.cs) |
| Focused tests | [FifoReturnRestorationTests.cs](../tests/SESS.NexaERP.Tests/FifoReturnRestorationTests.cs), [MaterialReturnMigrationTests.cs](../tests/SESS.NexaERP.Tests/MaterialReturnMigrationTests.cs) |

### Machine DC

Prepare and dispatch machine delivery challans with their dossier.

| Layer | Files / tables |
|---|---|
| Frontend | [MachineDeliveryDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/MachineDeliveryDetailPage.tsx), [MachineDeliveryDispatchModal.tsx](../src/SESS.NexaERP.Web/src/features/stores/MachineDeliveryDispatchModal.tsx), [MachineDeliveryListPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/MachineDeliveryListPage.tsx), [MachineDeliveryChallanPrint.tsx](../src/SESS.NexaERP.Web/src/print/MachineDeliveryChallanPrint.tsx) |
| API endpoint | [MachineDeliveryEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/MachineDeliveryEndpoints.cs) |
| Service / implementation | [EfMachineDeliveryService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfMachineDeliveryService.cs) |
| Main DB tables | `delivery_challans, delivery_challan_lines, job_orders, stores_approval_history` |
| Main migrations | [20260915103000_MachineDeliveryDossier.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260915103000_MachineDeliveryDossier.cs), [20260926120000_MachineDeliveryDispatchDetails.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260926120000_MachineDeliveryDispatchDetails.cs), [20260928100000_MachineDeliverySelection.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260928100000_MachineDeliverySelection.cs) |
| Focused tests | [MachineDeliveryRequestValidationTests.cs](../tests/SESS.NexaERP.Tests/MachineDeliveryRequestValidationTests.cs), [MachineDeliverySelectionTests.cs](../tests/SESS.NexaERP.Tests/MachineDeliverySelectionTests.cs), [MachineDeliveryWitnessTests.cs](../tests/SESS.NexaERP.Tests/MachineDeliveryWitnessTests.cs), [PrintContractTests.cs](../tests/SESS.NexaERP.Tests/PrintContractTests.cs) |

### Opening Stock

Load and authorize opening inventory through the three-actor ceremony.

| Layer | Files / tables |
|---|---|
| Frontend | [OpeningStockDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/OpeningStockDetailPage.tsx), [OpeningStockListPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/OpeningStockListPage.tsx) |
| API endpoint | [OpeningStockEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/OpeningStockEndpoints.cs) |
| Service / implementation | [EfOpeningStockService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfOpeningStockService.cs) |
| Main DB tables | `opening_stock_import_staging_lines, opening_stocks, opening_stock_lines, opening_stock_events, fifo_inventory_cost_layers` |
| Main migrations | [20260912064200_GovernedOpeningStockThreeActorCeremony.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260912064200_GovernedOpeningStockThreeActorCeremony.cs), [20260920160000_OpeningStockProvenance.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260920160000_OpeningStockProvenance.cs), [20260926130000_OpeningStockWithdraw.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260926130000_OpeningStockWithdraw.cs) |
| Focused tests | [ConcurrentOpeningStockTests.cs](../tests/SESS.NexaERP.Tests/ConcurrentOpeningStockTests.cs), [OpeningStockGovernanceTests.cs](../tests/SESS.NexaERP.Tests/OpeningStockGovernanceTests.cs), [OpeningStockPrivilegeRefusalTests.cs](../tests/SESS.NexaERP.Tests/OpeningStockPrivilegeRefusalTests.cs), [OpeningStockTemplateTests.cs](../tests/SESS.NexaERP.Tests/OpeningStockTemplateTests.cs), [OpeningStockWithdrawTests.cs](../tests/SESS.NexaERP.Tests/OpeningStockWithdrawTests.cs) |

### Stock Adjustment

Review and post controlled inventory corrections.

| Layer | Files / tables |
|---|---|
| Frontend | [StockAdjustmentDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/StockAdjustmentDetailPage.tsx), [StockAdjustmentFormModal.tsx](../src/SESS.NexaERP.Web/src/features/stores/StockAdjustmentFormModal.tsx), [StockAdjustmentListPage.tsx](../src/SESS.NexaERP.Web/src/features/stores/StockAdjustmentListPage.tsx) |
| API endpoint | [StockAdjustmentEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/StockAdjustmentEndpoints.cs) |
| Service / implementation | [EfStockAdjustmentService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfStockAdjustmentService.cs), [EfStockAdjustmentService.Helpers.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfStockAdjustmentService.Helpers.cs) |
| Main DB tables | `stock_adjustments, stock_adjustment_lines, stock_movements` |
| Main migrations | [20260920210000_StockAdjustmentPosting.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260920210000_StockAdjustmentPosting.cs) |
| Focused tests | [StockAdjustmentApprovalPolicyTests.cs](../tests/SESS.NexaERP.Tests/StockAdjustmentApprovalPolicyTests.cs), [StockAdjustmentApprovalSnapshotTests.cs](../tests/SESS.NexaERP.Tests/StockAdjustmentApprovalSnapshotTests.cs), [StockAdjustmentMigrationTests.cs](../tests/SESS.NexaERP.Tests/StockAdjustmentMigrationTests.cs), [StockAdjustmentPostingDatePolicyTests.cs](../tests/SESS.NexaERP.Tests/StockAdjustmentPostingDatePolicyTests.cs), [StockAdjustmentReviewTests.cs](../tests/SESS.NexaERP.Tests/StockAdjustmentReviewTests.cs) |

### Inventory Periods

Open and close the periods in which posting is permitted.

| Layer | Files / tables |
|---|---|
| Frontend | [InventoryPeriodCloseModal.tsx](../src/SESS.NexaERP.Web/src/features/accounts/InventoryPeriodCloseModal.tsx), [InventoryPeriodOpenModal.tsx](../src/SESS.NexaERP.Web/src/features/accounts/InventoryPeriodOpenModal.tsx), [InventoryPeriodsPage.tsx](../src/SESS.NexaERP.Web/src/features/accounts/InventoryPeriodsPage.tsx) |
| API endpoint | [InventoryPeriodEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/InventoryPeriodEndpoints.cs) |
| Service / implementation | [EfInventoryPeriodService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfInventoryPeriodService.cs) |
| Main DB tables | `inventory_periods` |
| Main migrations | [20260919110000_DocumentedCfoAuthorityBaseline.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260919110000_DocumentedCfoAuthorityBaseline.cs), [20260919120000_GovernedInventoryPeriods.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260919120000_GovernedInventoryPeriods.cs) |
| Focused tests | [InventoryPeriodMigrationTests.cs](../tests/SESS.NexaERP.Tests/InventoryPeriodMigrationTests.cs), [InventoryPeriodWorkflowTests.cs](../tests/SESS.NexaERP.Tests/InventoryPeriodWorkflowTests.cs) |

### Pending / History

Show documents waiting for action and retained timelines. Tracking reads existing modules rather than duplicating their documents.

| Layer | Files / tables |
|---|---|
| Frontend | [HistoryPanel.tsx](../src/SESS.NexaERP.Web/src/components/HistoryPanel.tsx), [PendingPage.tsx](../src/SESS.NexaERP.Web/src/features/tracking/PendingPage.tsx), [trackingLinks.test.ts](../src/SESS.NexaERP.Web/src/features/tracking/trackingLinks.test.ts), [TrackingTiles.tsx](../src/SESS.NexaERP.Web/src/features/tracking/TrackingTiles.tsx) |
| API endpoint | [TrackingEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/TrackingEndpoints.cs) |
| Service / implementation | [EfTrackingDigestQuery.cs](../src/SESS.NexaERP.Infrastructure/Tracking/EfTrackingDigestQuery.cs), [EfTrackingService.cs](../src/SESS.NexaERP.Infrastructure/Tracking/EfTrackingService.cs) |
| Main DB tables | `tracking_queues; existing module document and history tables` |
| Main migrations | [20260928090000_TrackingLite.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260928090000_TrackingLite.cs) |
| Focused tests | [TrackingDocumentLinkTests.cs](../tests/SESS.NexaERP.Tests/TrackingDocumentLinkTests.cs), [TrackingLiteMigrationTests.cs](../tests/SESS.NexaERP.Tests/TrackingLiteMigrationTests.cs) |

### Home

Show shortcuts allowed by session grants; Home has no separate table.

| Layer | Files / tables |
|---|---|
| Frontend | [HomePage.tsx](../src/SESS.NexaERP.Web/src/features/home/HomePage.tsx), [pageCatalog.test.ts](../src/SESS.NexaERP.Web/src/features/home/pageCatalog.test.ts), [pageCatalog.ts](../src/SESS.NexaERP.Web/src/features/home/pageCatalog.ts) |
| API endpoint | [SessionEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/SessionEndpoints.cs) |
| Service / implementation | [EfPagePermissionService.cs](../src/SESS.NexaERP.Infrastructure/Authorization/EfPagePermissionService.cs), [EfSessionService.cs](../src/SESS.NexaERP.Infrastructure/Identity/EfSessionService.cs) |
| Main DB tables | `page_definitions, role_page_permissions, employee_role_assignments, employee_operational_scopes` |
| Main migrations | [20260929110000_R1DirectorPageGrants.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260929110000_R1DirectorPageGrants.cs), [20260929130000_R1RemainingDirectorViews.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260929130000_R1RemainingDirectorViews.cs), [20260929181000_R1PurchaseProductionGrants.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260929181000_R1PurchaseProductionGrants.cs) |
| Focused tests | [EffectiveRoleAuthorizationTests.cs](../tests/SESS.NexaERP.Tests/EffectiveRoleAuthorizationTests.cs), [SessionPermissionParityTests.cs](../tests/SESS.NexaERP.Tests/SessionPermissionParityTests.cs) |

### Dashboards

Summarize purchase workload and stores receipt or QC attention.

| Layer | Files / tables |
|---|---|
| Frontend | [PurchaseDashboardPage.tsx](../src/SESS.NexaERP.Web/src/features/dashboards/PurchaseDashboardPage.tsx), [PurchaseObligationsSection.tsx](../src/SESS.NexaERP.Web/src/features/dashboards/PurchaseObligationsSection.tsx), [PurchaseOpenOrdersSection.tsx](../src/SESS.NexaERP.Web/src/features/dashboards/PurchaseOpenOrdersSection.tsx), [PurchaseSpendingSection.tsx](../src/SESS.NexaERP.Web/src/features/dashboards/PurchaseSpendingSection.tsx), [PurchaseWorkloadSection.tsx](../src/SESS.NexaERP.Web/src/features/dashboards/PurchaseWorkloadSection.tsx), [StoresDashboardPage.tsx](../src/SESS.NexaERP.Web/src/features/dashboards/StoresDashboardPage.tsx), [StoresQcStockSection.tsx](../src/SESS.NexaERP.Web/src/features/dashboards/StoresQcStockSection.tsx), [StoresWorkloadSection.tsx](../src/SESS.NexaERP.Web/src/features/dashboards/StoresWorkloadSection.tsx) |
| API endpoint | [PurchaseObligationsEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/PurchaseObligationsEndpoints.cs), [PurchaseOpenOrdersEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/PurchaseOpenOrdersEndpoints.cs), [PurchaseSpendingEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/PurchaseSpendingEndpoints.cs), [PurchaseWorkloadEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/PurchaseWorkloadEndpoints.cs), [StoresQcStockEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/StoresQcStockEndpoints.cs), [StoresWorkloadEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/StoresWorkloadEndpoints.cs) |
| Service / implementation | [EfPurchaseObligationsService.cs](../src/SESS.NexaERP.Infrastructure/Reporting/EfPurchaseObligationsService.cs), [EfPurchaseOpenOrdersService.cs](../src/SESS.NexaERP.Infrastructure/Reporting/EfPurchaseOpenOrdersService.cs), [EfPurchaseSpendingService.cs](../src/SESS.NexaERP.Infrastructure/Reporting/EfPurchaseSpendingService.cs), [EfPurchaseWorkloadService.cs](../src/SESS.NexaERP.Infrastructure/Reporting/EfPurchaseWorkloadService.cs), [EfStoresQcStockService.cs](../src/SESS.NexaERP.Infrastructure/Reporting/EfStoresQcStockService.cs), [EfStoresWorkloadService.cs](../src/SESS.NexaERP.Infrastructure/Reporting/EfStoresWorkloadService.cs) |
| Main DB tables | `purchase and stores source tables; SQL read functions in linked migrations` |
| Main migrations | [20260914010000_PurchaseWorkload.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260914010000_PurchaseWorkload.cs), [20260914020000_PurchaseSpending.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260914020000_PurchaseSpending.cs), [20260914040000_PurchaseObligations.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260914040000_PurchaseObligations.cs), [20260914050000_PurchaseOpenOrders.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260914050000_PurchaseOpenOrders.cs), [20260914060000_StoresWorkload.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260914060000_StoresWorkload.cs), [20260914070000_StoresQcStock.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260914070000_StoresQcStock.cs) |
| Focused tests | [PurchaseObligationsTests.cs](../tests/SESS.NexaERP.Tests/PurchaseObligationsTests.cs), [PurchaseOpenOrdersTests.cs](../tests/SESS.NexaERP.Tests/PurchaseOpenOrdersTests.cs), [PurchaseSpendingTests.cs](../tests/SESS.NexaERP.Tests/PurchaseSpendingTests.cs), [PurchaseWorkloadTests.cs](../tests/SESS.NexaERP.Tests/PurchaseWorkloadTests.cs), [StoresQcStockTests.cs](../tests/SESS.NexaERP.Tests/StoresQcStockTests.cs), [StoresWorkloadTests.cs](../tests/SESS.NexaERP.Tests/StoresWorkloadTests.cs) |

### E-mail log

Display retained mail status with planned retry and test-mail actions. At this SHA EmailEndpoints maps no routes and EmailLiteRegistration registers no sender or worker. UI calls planned endpoints; the store and PO enqueue hook exist, but SMTP delivery is not implemented by these hooks.

| Layer | Files / tables |
|---|---|
| Frontend | [EmailLogPage.tsx](../src/SESS.NexaERP.Web/src/features/admin/EmailLogPage.tsx), [EmailTestModal.tsx](../src/SESS.NexaERP.Web/src/features/admin/EmailTestModal.tsx) |
| API endpoint | [EmailEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/EmailEndpoints.cs) |
| Service / implementation | [EmailLiteRegistration.cs](../src/SESS.NexaERP.Infrastructure/Email/EmailLiteRegistration.cs), [EfEmailOutboxStore.cs](../src/SESS.NexaERP.Infrastructure/Outbox/EfEmailOutboxStore.cs) |
| Main DB tables | `email_outbox` |
| Main migrations | [20260927100000_EmailOutbox.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260927100000_EmailOutbox.cs), [20260929123000_R1EmailPageGrants.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260929123000_R1EmailPageGrants.cs) |
| Focused tests | [EmailOutboxTests.cs](../tests/SESS.NexaERP.Tests/EmailOutboxTests.cs) |

### Reports

Read and export authorized reports from purchase and inventory evidence.

| Layer | Files / tables |
|---|---|
| Frontend | [ReportCataloguePage.tsx](../src/SESS.NexaERP.Web/src/features/reports/ReportCataloguePage.tsx), [ReportViewerPage.tsx](../src/SESS.NexaERP.Web/src/features/reports/ReportViewerPage.tsx) |
| API endpoint | [CompanyReportEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/CompanyReportEndpoints.cs) |
| Service / implementation | [BilledNotReceivedReportSql.cs](../src/SESS.NexaERP.Infrastructure/Reporting/BilledNotReceivedReportSql.cs), [EfCompanyReportService.cs](../src/SESS.NexaERP.Infrastructure/Reporting/EfCompanyReportService.cs), [EngineerCustodyReportSql.cs](../src/SESS.NexaERP.Infrastructure/Reporting/EngineerCustodyReportSql.cs), [FifoValuationReportSql.cs](../src/SESS.NexaERP.Infrastructure/Reporting/FifoValuationReportSql.cs), [GroupedReportSql.cs](../src/SESS.NexaERP.Infrastructure/Reporting/GroupedReportSql.cs), [MachineDossierReportSql.cs](../src/SESS.NexaERP.Infrastructure/Reporting/MachineDossierReportSql.cs), [PendingApprovalReportSql.cs](../src/SESS.NexaERP.Infrastructure/Reporting/PendingApprovalReportSql.cs), [PurchaseFinancialReportSql.cs](../src/SESS.NexaERP.Infrastructure/Reporting/PurchaseFinancialReportSql.cs), [PurchaseRegisterReportSql.cs](../src/SESS.NexaERP.Infrastructure/Reporting/PurchaseRegisterReportSql.cs), [ReportDefinitions.cs](../src/SESS.NexaERP.Infrastructure/Reporting/ReportDefinitions.cs), [StockReportSql.cs](../src/SESS.NexaERP.Infrastructure/Reporting/StockReportSql.cs) |
| Main DB tables | `report_grants and source purchase, stock, custody, costing and dossier tables` |
| Main migrations | [20260913010000_CompanyReportPermissions.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260913010000_CompanyReportPermissions.cs), [20260920180000_ReportExportFollowsView.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260920180000_ReportExportFollowsView.cs) |
| Focused tests | [ActualBomReversalReportWitnessTests.cs](../tests/SESS.NexaERP.Tests/ActualBomReversalReportWitnessTests.cs), [CompanyReportTests.cs](../tests/SESS.NexaERP.Tests/CompanyReportTests.cs) |

### Identity

Resolve Keycloak identity to employees and enforce session access.

| Layer | Files / tables |
|---|---|
| Frontend | [authSession.ts](../src/SESS.NexaERP.Web/src/auth/authSession.ts), [oidcConfig.ts](../src/SESS.NexaERP.Web/src/auth/oidcConfig.ts), [useAuth.ts](../src/SESS.NexaERP.Web/src/auth/useAuth.ts), [CompanySelectPage.tsx](../src/SESS.NexaERP.Web/src/features/auth/CompanySelectPage.tsx), [LoginPage.tsx](../src/SESS.NexaERP.Web/src/features/auth/LoginPage.tsx), [OidcCallbackPage.tsx](../src/SESS.NexaERP.Web/src/features/auth/OidcCallbackPage.tsx), [RequirePage.tsx](../src/SESS.NexaERP.Web/src/features/auth/RequirePage.tsx), [SessionContext.tsx](../src/SESS.NexaERP.Web/src/features/auth/SessionContext.tsx) |
| API endpoint | [DevelopmentAuthEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/DevelopmentAuthEndpoints.cs), [IdentityEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/IdentityEndpoints.cs), [IdentityRoleGovernanceEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/IdentityRoleGovernanceEndpoints.cs), [SessionEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/SessionEndpoints.cs) |
| Service / implementation | [EfPagePermissionService.cs](../src/SESS.NexaERP.Infrastructure/Authorization/EfPagePermissionService.cs), [EfEmployeeIdentityResolver.cs](../src/SESS.NexaERP.Infrastructure/Identity/EfEmployeeIdentityResolver.cs), [EfSessionService.cs](../src/SESS.NexaERP.Infrastructure/Identity/EfSessionService.cs) |
| Main DB tables | `employee_identity_mappings, user_accounts, roles, company_role_activations, authentication_bootstrap_state, role_page_permissions` |
| Main migrations | [20260825092016_AuthenticationBootstrapFoundation.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260825092016_AuthenticationBootstrapFoundation.cs), [20260826065344_AuthenticationBootstrapCeremonySteps7To12.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260826065344_AuthenticationBootstrapCeremonySteps7To12.cs), [20260912130000_GovernedAuthenticationRuntime.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260912130000_GovernedAuthenticationRuntime.cs), [20260926090000_DevelopmentLoginsOffByDefault.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260926090000_DevelopmentLoginsOffByDefault.cs) |
| Focused tests | [AuthenticationBootstrapFoundationTests.cs](../tests/SESS.NexaERP.Tests/AuthenticationBootstrapFoundationTests.cs), [AuthenticationBootstrapSteps7To12Tests.cs](../tests/SESS.NexaERP.Tests/AuthenticationBootstrapSteps7To12Tests.cs), [GovernedAuthenticationRuntimeTests.cs](../tests/SESS.NexaERP.Tests/GovernedAuthenticationRuntimeTests.cs), [IdentityOperationsMonitorTests.cs](../tests/SESS.NexaERP.Tests/IdentityOperationsMonitorTests.cs), [OidcAccessTokenTests.cs](../tests/SESS.NexaERP.Tests/OidcAccessTokenTests.cs) |

### Customer PO

Record customer demand that can feed requisitions. Commands are implemented in CustomerPoEndpoints; the listed persistence file maps the data.

| Layer | Files / tables |
|---|---|
| Frontend | [CustomerPoFormModal.tsx](../src/SESS.NexaERP.Web/src/features/sales/CustomerPoFormModal.tsx), [CustomerPoListPage.tsx](../src/SESS.NexaERP.Web/src/features/sales/CustomerPoListPage.tsx) |
| API endpoint | [CustomerPoEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/CustomerPoEndpoints.cs) |
| Service / implementation | [NexaErpDbContext.Sales.cs](../src/SESS.NexaERP.Infrastructure/Persistence/NexaErpDbContext.Sales.cs) |
| Main DB tables | `customer_purchase_orders, customer_purchase_order_lines` |
| Main migrations | [20260831064159_CustomerPurchaseOrders.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260831064159_CustomerPurchaseOrders.cs), [20260831155638_CorrectCustomerPoIntakeRevisionsAndPrLink.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260831155638_CorrectCustomerPoIntakeRevisionsAndPrLink.cs) |
| Focused tests | [CustomerPoIntakeRegisterCorrectionTests.cs](../tests/SESS.NexaERP.Tests/CustomerPoIntakeRegisterCorrectionTests.cs) |

### Estimated BOM

Maintain the controlled estimate of machine materials.

| Layer | Files / tables |
|---|---|
| Frontend | [BomLinesEditorModal.tsx](../src/SESS.NexaERP.Web/src/features/design/BomLinesEditorModal.tsx), [EstimatedBomDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/design/EstimatedBomDetailPage.tsx), [EstimatedBomListPage.tsx](../src/SESS.NexaERP.Web/src/features/design/EstimatedBomListPage.tsx) |
| API endpoint | [EstimatedBomEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/EstimatedBomEndpoints.cs) |
| Service / implementation | [EfEstimatedBomService.Commands.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfEstimatedBomService.Commands.cs), [EfEstimatedBomService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfEstimatedBomService.cs), [EfEstimatedBomService.Helpers.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfEstimatedBomService.Helpers.cs), [EfEstimatedBomService.ImportMerge.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfEstimatedBomService.ImportMerge.cs) |
| Main DB tables | `estimated_boms, estimated_bom_lines` |
| Main migrations | [20260907034428_EstimatedBomFoundation.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260907034428_EstimatedBomFoundation.cs), [20260907061217_EstimatedBomLifecycleAndItemGovernance.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260907061217_EstimatedBomLifecycleAndItemGovernance.cs) |
| Focused tests | [EstimatedBomLifecycleTests.cs](../tests/SESS.NexaERP.Tests/EstimatedBomLifecycleTests.cs), [EstimatedBomMigrationTests.cs](../tests/SESS.NexaERP.Tests/EstimatedBomMigrationTests.cs) |

### Production BOM / engineering documents

Control production materials and engineering revisions. Engineering Documents has a disabled menu entry; no dedicated page exists.

| Layer | Files / tables |
|---|---|
| Frontend | [ProductionBomDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/production/ProductionBomDetailPage.tsx), [ProductionBomListPage.tsx](../src/SESS.NexaERP.Web/src/features/production/ProductionBomListPage.tsx) |
| API endpoint | [ProductionEngineeringEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/ProductionEngineeringEndpoints.cs) |
| Service / implementation | [EfProductionEngineeringService.CommandSupport.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.CommandSupport.cs), [EfProductionEngineeringService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.cs), [EfProductionEngineeringService.DocumentCreate.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.DocumentCreate.cs), [EfProductionEngineeringService.DocumentRevision.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.DocumentRevision.cs), [EfProductionEngineeringService.DocumentTransition.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.DocumentTransition.cs), [EfProductionEngineeringService.DocumentValidation.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.DocumentValidation.cs), [EfProductionEngineeringService.Identity.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.Identity.cs), [EfProductionEngineeringService.Materials.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.Materials.cs), [EfProductionEngineeringService.ProductionCreate.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.ProductionCreate.cs), [EfProductionEngineeringService.ProductionEdit.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.ProductionEdit.cs), [EfProductionEngineeringService.ProductionPin.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.ProductionPin.cs), [EfProductionEngineeringService.ProductionRevision.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.ProductionRevision.cs), [EfProductionEngineeringService.ProductionTransition.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.ProductionTransition.cs), [EfProductionEngineeringService.Read.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.Read.cs), [EfProductionEngineeringService.Views.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfProductionEngineeringService.Views.cs) |
| Main DB tables | `production_boms, production_bom_lines, engineering_documents, engineering_document_revisions` |
| Main migrations | [20260907082326_ProductionBomAndEngineeringDocuments.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260907082326_ProductionBomAndEngineeringDocuments.cs) |
| Focused tests | [ProductionEngineeringTests.cs](../tests/SESS.NexaERP.Tests/ProductionEngineeringTests.cs) |

### Job Orders / FAT

Create governed machine jobs and assess FAT readiness.

| Layer | Files / tables |
|---|---|
| Frontend | [FatReadinessPanel.tsx](../src/SESS.NexaERP.Web/src/features/production/FatReadinessPanel.tsx), [JobOrderDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/production/JobOrderDetailPage.tsx), [JobOrderFormModal.tsx](../src/SESS.NexaERP.Web/src/features/production/JobOrderFormModal.tsx), [JobOrderListPage.tsx](../src/SESS.NexaERP.Web/src/features/production/JobOrderListPage.tsx) |
| API endpoint | [JobOrderEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/JobOrderEndpoints.cs), [JobOrderFatReadinessEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/JobOrderFatReadinessEndpoints.cs) |
| Service / implementation | [EfJobOrderFatReadinessService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfJobOrderFatReadinessService.cs), [EfJobOrderService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfJobOrderService.cs), [EfJobOrderService.Recovery.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfJobOrderService.Recovery.cs) |
| Main DB tables | `job_orders, job_order_fat_reconciliations, job_order_fat_reconciliation_lines` |
| Main migrations | [20260908095057_GovernedJobOrderCreationWorkflow.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260908095057_GovernedJobOrderCreationWorkflow.cs), [20260908195922_JobOrderFatReadiness.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260908195922_JobOrderFatReadiness.cs) |
| Focused tests | [JobOrderFatReadinessMigrationTests.cs](../tests/SESS.NexaERP.Tests/JobOrderFatReadinessMigrationTests.cs), [JobOrderGovernanceMigrationTests.cs](../tests/SESS.NexaERP.Tests/JobOrderGovernanceMigrationTests.cs) |

### Fitment / Actual BOM

Record fitted components and derive the actual bill of materials.

| Layer | Files / tables |
|---|---|
| Frontend | [ActualBomPanel.tsx](../src/SESS.NexaERP.Web/src/features/production/ActualBomPanel.tsx), [ComponentFitmentFormModal.tsx](../src/SESS.NexaERP.Web/src/features/production/ComponentFitmentFormModal.tsx), [ComponentFitmentListPage.tsx](../src/SESS.NexaERP.Web/src/features/production/ComponentFitmentListPage.tsx) |
| API endpoint | [FitmentActualBomEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/FitmentActualBomEndpoints.cs) |
| Service / implementation | [EfFitmentActualBomService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfFitmentActualBomService.cs) |
| Main DB tables | `component_fitments, actual_boms, actual_bom_entries` |
| Main migrations | [20260908133110_ComponentFitmentAndGeneratedActualBom.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260908133110_ComponentFitmentAndGeneratedActualBom.cs), [20260918090000_ActualBomLandedRateValuation.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260918090000_ActualBomLandedRateValuation.cs) |
| Focused tests | [ActualBomLandedRateTests.cs](../tests/SESS.NexaERP.Tests/ActualBomLandedRateTests.cs), [ActualBomProvenanceTests.cs](../tests/SESS.NexaERP.Tests/ActualBomProvenanceTests.cs), [ActualBomReversalReportWitnessTests.cs](../tests/SESS.NexaERP.Tests/ActualBomReversalReportWitnessTests.cs), [ComponentFitmentMigrationTests.cs](../tests/SESS.NexaERP.Tests/ComponentFitmentMigrationTests.cs) |

### Vendor Bills

Match receipts to bills and retain accepted costing evidence.

| Layer | Files / tables |
|---|---|
| Frontend | [VendorBillDetailPage.tsx](../src/SESS.NexaERP.Web/src/features/accounts/VendorBillDetailPage.tsx), [VendorBillFormModal.tsx](../src/SESS.NexaERP.Web/src/features/accounts/VendorBillFormModal.tsx), [VendorBillListPage.tsx](../src/SESS.NexaERP.Web/src/features/accounts/VendorBillListPage.tsx) |
| API endpoint | [VendorBillEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/VendorBillEndpoints.cs) |
| Service / implementation | [EfVendorBillService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfVendorBillService.cs) |
| Main DB tables | `vendor_bills, vendor_bill_lines, vendor_bill_history, fifo_inventory_cost_layers` |
| Main migrations | [20260908082923_VendorBillAndAcceptedBillCosting.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260908082923_VendorBillAndAcceptedBillCosting.cs), [20260926100000_VendorBillSeparateDecider.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260926100000_VendorBillSeparateDecider.cs) |
| Focused tests | [VendorBillCostingMigrationTests.cs](../tests/SESS.NexaERP.Tests/VendorBillCostingMigrationTests.cs), [VendorBillSeparateDeciderMigrationTests.cs](../tests/SESS.NexaERP.Tests/VendorBillSeparateDeciderMigrationTests.cs) |

### Vendor Payments

Record existing advance and payment evidence; broader tracking remains R2.

| Layer | Files / tables |
|---|---|
| Frontend | [VendorPaymentsPage.tsx](../src/SESS.NexaERP.Web/src/features/accounts/VendorPaymentsPage.tsx) |
| API endpoint | [VendorFinancialEvidenceEndpoints.BankAdvice.cs](../src/SESS.NexaERP.Api/Endpoints/VendorFinancialEvidenceEndpoints.BankAdvice.cs), [VendorFinancialEvidenceEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/VendorFinancialEvidenceEndpoints.cs) |
| Service / implementation | [EfVendorBankAdviceService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfVendorBankAdviceService.cs), [EfVendorFinancialEvidenceService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfVendorFinancialEvidenceService.cs) |
| Main DB tables | `vendor_advances, vendor_advance_reversals, vendor_advance_adjustments, vendor_payments, vendor_payment_allocations` |
| Main migrations | [20260911180000_VendorAdvanceAndPaymentEvidence.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260911180000_VendorAdvanceAndPaymentEvidence.cs), [20260913090000_GovernedVendorBankAdvice.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260913090000_GovernedVendorBankAdvice.cs) |
| Focused tests | [ConcurrentVendorPaymentTests.cs](../tests/SESS.NexaERP.Tests/ConcurrentVendorPaymentTests.cs), [ForeignPaymentAdviceTests.cs](../tests/SESS.NexaERP.Tests/ForeignPaymentAdviceTests.cs) |

### Notifications

Show retained in-app notifications to their recipients.

| Layer | Files / tables |
|---|---|
| Frontend | [NotificationsPage.tsx](../src/SESS.NexaERP.Web/src/features/notifications/NotificationsPage.tsx) |
| API endpoint | [NotificationEndpoints.cs](../src/SESS.NexaERP.Api/Endpoints/NotificationEndpoints.cs) |
| Service / implementation | [EfInAppNotificationService.cs](../src/SESS.NexaERP.Infrastructure/Stores/EfInAppNotificationService.cs) |
| Main DB tables | `notification_events, notification_recipients, notification_delivery_attempts` |
| Main migrations | [20260910094618_InAppNotificationDelivery.cs](../src/SESS.NexaERP.Infrastructure/Persistence/Migrations/20260910094618_InAppNotificationDelivery.cs) |
| Focused tests | [InAppNotificationTests.cs](../tests/SESS.NexaERP.Tests/InAppNotificationTests.cs) |

### Setup tools

Provision the installation and database principals through the documented operator workflow; there is no React setup page.

| Layer | Files / tables |
|---|---|
| Frontend | No setup screen; identity pages above cover login. |
| API | Wrapper calls module endpoints; see `tools/setup/SetupOperator.psm1` for routing. |
| Service / implementation | [Installer Program.cs](../src/SESS.NexaERP.Installer/Program.cs), [DatabasePrincipalProvisioningSql.cs](../src/SESS.NexaERP.Installer/DatabasePrincipalProvisioningSql.cs), `Installer/*BootstrapCommand*.cs`, [Invoke-Setup.ps1](../tools/setup/Invoke-Setup.ps1), [SetupOperator.psm1](../tools/setup/SetupOperator.psm1), [Test-KeycloakRealmImport.ps1](../tools/identity/Test-KeycloakRealmImport.ps1) |
| Main DB tables / migrations | Identity/bootstrap tables and migrations above; principal provisioning grants runtime access to migration-created functions. |
| Tests | [EmptyDatabasePrincipalTests.cs](../tests/SESS.NexaERP.Tests/EmptyDatabasePrincipalTests.cs), [Test-SetupOperator.ps1](../tools/setup/Test-SetupOperator.ps1), [test_runner_safety.py](../tools/setup/test_runner_safety.py), [DeploymentFrontendTests.cs](../tests/SESS.NexaERP.Tests/DeploymentFrontendTests.cs) |

Never execute or package `tools/identity/Repair-ApproverOtpFlow.ps1`. See [tools README](../tools/README.md) and the [fresh database runbook](installation/go-live-fresh-database-runbook.md). No setup, migration or deployment tool runs for this task.

## Why names contain Rev86x / Rev869x

`Rev` is a historical implementation checkpoint inherited from pre-advance work, not a .NET version, release number or schema name. Letters split features; C1/C2/C3 identify correction checkpoints. Names survived in partial classes, seeds, tests and tools after consolidation into the advance baseline. EF timestamps identify migrations and are not interchangeable with REV labels. Dated migrations after the baseline form the current upgrade chain.

| Label | Feature / correction | Source evidence |
|---|---|---|
| REV862 | Phase-1 foundation | `database/postgresql/rev862_phase1_foundation_idempotent.sql` |
| REV863 / REV864 | Early API smoke / persisted and authenticated API smoke checkpoints | Root `api-rev863-smoke-*` and `api-rev864-*` logs survive. Their names alone do not establish a unique business feature; consult `outputs/archive/ef_nexa_migration_history_pre_advance/`. |
| REV865 | Phase-1 authorization seed | `database/postgresql/rev865_phase1_authorization_seed_idempotent.sql` |
| REV866 / REV866C1 | Employee-role/page-permission reconciliation, OIDC decision and correction | `Persistence/Rev866SeedData.cs`, `docs/rev866_role_permission_reconciliation.md`, `docs/rev866_oidc_decision_note.md`, `tools/apply-rev866-correction-secure.ps1` |
| REV867 / REV867C1 | Master foundation and verification/correction checkpoint | `docs/rev867_master_foundation_gap_report.md`, `Rev867MasterFoundationTests.cs`, `Rev867C1PostgresVerificationTests.cs` |
| REV868 | Purchase requisition foundation and workflow | `outputs/rev868_source_checkpoint_report.md`, `Rev868PurchaseRequisitionTests.cs` |
| REV868C1 | Isolated PR workflow verification/preparation | `outputs/rev868c1_source_checkpoint_report.md`, `Rev868C1PreparationTests.cs` |
| REV868C2 | Approval-route correction and department-manager resolution | `outputs/rev868c2_approval_route_source_checkpoint_report.md` |
| REV868C3 | Employee, department and manager reconciliation; legacy department correction | `outputs/rev868c3_source_checkpoint_report.md`, `Rev868C3ImplementationTests.cs`, `Persistence/Rev868C3EmployeeWorkbookData.cs` |
| REV869 | Integrated Purchase / Stores planning umbrella | `outputs/rev869_integrated_purchase_stores_implementation_plan.md` |
| REV869A | Identity/master/scope/configuration: identity mappings, scope, UOM conversions, GST, vendor qualification, QC policies | `outputs/rev869a_source_implementation_checkpoint.md`, `Application/Rev869A/Rev869AContracts.cs`, `NexaErpDbContext.Rev869A.cs` |
| REV869B | RFQ, quotation, comparison, PO, material follow-up; later controlled mutation/security work | `Domain/Purchase/Rev869BPurchaseTransactions.cs`, `Purchase/EfRev869BPurchaseService*.cs`, `NexaErpDbContext.Rev869B.cs`; retirement: `docs/installation/retired-rev869b-project-removal.md` |

Historical reports are evidence, not current business-rule authority. Frozen schema and answers take precedence. [R2 rename proposal](R2-RENAME-PLAN.md) lists semantic filename proposals without executing them. [Root clutter proposal](ROOT-CLUTTER-REVIEW.md) separates generated output from evidence and fixtures.
