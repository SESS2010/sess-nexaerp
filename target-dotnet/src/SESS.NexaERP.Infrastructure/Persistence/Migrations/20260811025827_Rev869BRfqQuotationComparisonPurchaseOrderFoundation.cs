using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Rev869BRfqQuotationComparisonPurchaseOrderFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "CanIssue", schema: "nexa", table: "role_page_permissions", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<Guid?>(name: "VerifiedByEmployeeId", schema: "nexa", table: "vendor_qualifications", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<Guid?>(name: "ApprovedByEmployeeId", schema: "nexa", table: "vendor_qualifications", type: "uuid", nullable: true);
            migrationBuilder.CreateIndex(name: "IX_vendor_qualifications_VerifiedByEmployeeId", schema: "nexa", table: "vendor_qualifications", column: "VerifiedByEmployeeId");
            migrationBuilder.CreateIndex(name: "IX_vendor_qualifications_ApprovedByEmployeeId", schema: "nexa", table: "vendor_qualifications", column: "ApprovedByEmployeeId");
            migrationBuilder.AddForeignKey(name: "FK_vendor_qualifications_employees_VerifiedByEmployeeId", schema: "nexa", table: "vendor_qualifications", column: "VerifiedByEmployeeId", principalSchema: "nexa", principalTable: "employees", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_vendor_qualifications_employees_ApprovedByEmployeeId", schema: "nexa", table: "vendor_qualifications", column: "ApprovedByEmployeeId", principalSchema: "nexa", principalTable: "employees", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.CreateTable(
                name: "purchase_transaction_approval_policies",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RouteCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    MaximumAmount = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: true),
                    ApproverRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_transaction_approval_policies", x => x.Id);
                    table.CheckConstraint("CK_purchase_transaction_policy_amounts", "\"MinimumAmount\" >= 0 AND (\"MaximumAmount\" IS NULL OR \"MaximumAmount\" >= \"MinimumAmount\")");
                    table.CheckConstraint("CK_purchase_transaction_policy_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom");
                });

            migrationBuilder.CreateTable(
                name: "purchase_transaction_status_history",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorLoginId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_transaction_status_history", x => x.Id);
                    table.CheckConstraint("CK_purchase_transaction_history_remarks", "length(trim(\"Remarks\")) > 0");
                    table.ForeignKey(
                        name: "FK_purchase_transaction_status_history_employees_ActorEmployee~",
                        column: x => x.ActorEmployeeId,
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "request_for_quotations",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RfqNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestingDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteDueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsSingleSource = table.Column<bool>(type: "boolean", nullable: false),
                    SingleSourceJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TransitionCorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_for_quotations", x => x.Id);
                    table.CheckConstraint("CK_rfqs_sequence_positive", "\"SequenceNumber\" > 0");
                    table.CheckConstraint("CK_rfqs_single_source_reason", "NOT \"IsSingleSource\" OR length(trim(coalesce(\"SingleSourceJustification\", ''))) > 0");
                    table.CheckConstraint("CK_rfqs_status", "\"Status\" IN ('Draft','Issued','Closed','Cancelled')");
                    table.ForeignKey(
                        name: "FK_request_for_quotations_departments_RequestingDepartmentId",
                        column: x => x.RequestingDepartmentId,
                        principalSchema: "nexa",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_for_quotations_employees_OwnerEmployeeId",
                        column: x => x.OwnerEmployeeId,
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_for_quotations_purchase_requisitions_PurchaseRequis~",
                        column: x => x.PurchaseRequisitionId,
                        principalSchema: "nexa",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_for_quotations_warehouses_DeliveryWarehouseId",
                        column: x => x.DeliveryWarehouseId,
                        principalSchema: "nexa",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "request_for_quotation_lines",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestForQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequirementHandoffId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    PrNumberSnapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PrLineNumberSnapshot = table.Column<int>(type: "integer", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UomSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SpecificationSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ApprovedQuantitySnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    AlreadyOrderedQuantitySnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    OutstandingQuantitySnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    RfqQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    RequiredDateSnapshot = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_for_quotation_lines", x => x.Id);
                    table.CheckConstraint("CK_rfq_lines_quantities", "\"ApprovedQuantitySnapshot\" > 0 AND \"AlreadyOrderedQuantitySnapshot\" >= 0 AND \"OutstandingQuantitySnapshot\" >= 0 AND \"RfqQuantity\" > 0 AND \"RfqQuantity\" <= \"OutstandingQuantitySnapshot");
                    table.ForeignKey(
                        name: "FK_request_for_quotation_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "nexa",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_for_quotation_lines_purchase_requirement_handoffs_P~",
                        column: x => x.PurchaseRequirementHandoffId,
                        principalSchema: "nexa",
                        principalTable: "purchase_requirement_handoffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_for_quotation_lines_purchase_requisition_lines_Purc~",
                        column: x => x.PurchaseRequisitionLineId,
                        principalSchema: "nexa",
                        principalTable: "purchase_requisition_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_for_quotation_lines_request_for_quotations_RequestF~",
                        column: x => x.RequestForQuotationId,
                        principalSchema: "nexa",
                        principalTable: "request_for_quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rfq_vendor_invitations",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestForQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    InvitedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    QuoteDueAtSnapshot = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VendorQualificationSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TransitionCorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfq_vendor_invitations", x => x.Id);
                    table.CheckConstraint("CK_rfq_invitation_status", "\"Status\" IN ('Issued','Submitted','Withdrawn','Cancelled')");
                    table.ForeignKey(
                        name: "FK_rfq_vendor_invitations_request_for_quotations_RequestForQuo~",
                        column: x => x.RequestForQuotationId,
                        principalSchema: "nexa",
                        principalTable: "request_for_quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rfq_vendor_invitations_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "nexa",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_quotations",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    QuotationNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    RfqVendorInvitationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RootQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    IsCurrentRevision = table.Column<bool>(type: "boolean", nullable: false),
                    VendorQuoteReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SubmissionSource = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AttachmentObjectKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AttachmentSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VendorAttestation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsLateSubmission = table.Column<bool>(type: "boolean", nullable: false),
                    LateAuthorizedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    LateAuthorizationRemarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PaymentTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DeliveryTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    WarrantyTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    HeaderDiscountValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    TotalPayableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TransitionCorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_quotations", x => x.Id);
                    table.CheckConstraint("CK_vendor_quotation_late_authorization", "NOT \"IsLateSubmission\" OR (\"LateAuthorizedByEmployeeId\" IS NOT NULL AND length(trim(coalesce(\"LateAuthorizationRemarks\", ''))) > 0)");
                    table.CheckConstraint("CK_vendor_quotation_revision", "\"RevisionNumber\" > 0 AND \"SequenceNumber\" > 0");
                    table.CheckConstraint("CK_vendor_quotation_provenance", "\"SubmissionSource\" IN ('EMAIL_RECEIVED','PHYSICAL_RECEIVED') AND length(trim(\"AttachmentObjectKey\")) > 0 AND \"AttachmentSha256\" ~ '^[0-9A-Fa-f]{64}$' AND length(trim(\"VendorAttestation\")) > 0 AND \"ReceivedAt\" <= \"SubmittedAt\"");
                    table.CheckConstraint("CK_vendor_quotation_status", "\"Status\" IN ('Draft','Submitted','TechnicallyCompliant','TechnicallyRejected','Superseded','Withdrawn','Rejected')");
                    table.CheckConstraint("CK_vendor_quotation_total", "\"HeaderDiscountValue\" >= 0 AND \"TotalPayableValue\" >= 0");
                    table.ForeignKey(
                        name: "FK_vendor_quotations_employees_LateAuthorizedByEmployeeId",
                        column: x => x.LateAuthorizedByEmployeeId,
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_quotations_rfq_vendor_invitations_RfqVendorInvitatio~",
                        column: x => x.RfqVendorInvitationId,
                        principalSchema: "nexa",
                        principalTable: "rfq_vendor_invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_quotations_vendor_quotations_PreviousRevisionId",
                        column: x => x.PreviousRevisionId,
                        principalSchema: "nexa",
                        principalTable: "vendor_quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_quotations_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "nexa",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_comparisons",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ComparisonNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    RequestForQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecommendedVendorQuotationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SelectedVendorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalPayableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ApprovalRoute = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsSingleSource = table.Column<bool>(type: "boolean", nullable: false),
                    SingleSourceJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RecommendationRemarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TransitionCorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_comparisons", x => x.Id);
                    table.CheckConstraint("CK_comparison_sequence_total", "\"SequenceNumber\" > 0 AND \"TotalPayableValue\" >= 0");
                    table.CheckConstraint("CK_comparison_single_source_reason", "NOT \"IsSingleSource\" OR length(trim(coalesce(\"SingleSourceJustification\", ''))) > 0");
                    table.CheckConstraint("CK_comparison_status", "\"Status\" IN ('Draft','PendingApproval','Approved','Rejected','RevisionRequested','Cancelled')");
                    table.ForeignKey(
                        name: "FK_commercial_comparisons_employees_OwnerEmployeeId",
                        column: x => x.OwnerEmployeeId,
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_comparisons_request_for_quotations_RequestForQuo~",
                        column: x => x.RequestForQuotationId,
                        principalSchema: "nexa",
                        principalTable: "request_for_quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_comparisons_vendor_quotations_RecommendedVendorQ~",
                        column: x => x.RecommendedVendorQuotationId,
                        principalSchema: "nexa",
                        principalTable: "vendor_quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_comparisons_vendors_SelectedVendorId",
                        column: x => x.SelectedVendorId,
                        principalSchema: "nexa",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_quotation_lines",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestForQuotationLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    HsnSacCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SupplierStateCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PlaceOfSupplyStateCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    VendorRegistrationType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    UnitRate = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    HeaderDiscountValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    PackingForwarding = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Freight = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Insurance = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    OtherCharges = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    TaxableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    TaxGstSettingId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxRuleSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    CgstValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    SgstValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    IgstValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CessValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    RoundOff = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    TotalPayableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    PromisedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_quotation_lines", x => x.Id);
                    table.CheckConstraint("CK_vendor_quotation_line_quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_vendor_quotation_line_values", "\"UnitRate\" >= 0 AND \"DiscountValue\" >= 0 AND \"HeaderDiscountValue\" >= 0 AND \"PackingForwarding\" >= 0 AND \"Freight\" >= 0 AND \"Insurance\" >= 0 AND \"OtherCharges\" >= 0 AND \"TaxableValue\" >= 0 AND \"CgstValue\" >= 0 AND \"SgstValue\" >= 0 AND \"IgstValue\" >= 0 AND \"CessValue\" >= 0 AND \"TotalPayableValue\" >= 0");
                    table.ForeignKey(
                        name: "FK_vendor_quotation_lines_request_for_quotation_lines_RequestF~",
                        column: x => x.RequestForQuotationLineId,
                        principalSchema: "nexa",
                        principalTable: "request_for_quotation_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_quotation_lines_tax_gst_settings_TaxGstSettingId",
                        column: x => x.TaxGstSettingId,
                        principalSchema: "nexa",
                        principalTable: "tax_gst_settings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_quotation_lines_vendor_quotations_VendorQuotationId",
                        column: x => x.VendorQuotationId,
                        principalSchema: "nexa",
                        principalTable: "vendor_quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PoNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    RootPurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "boolean", nullable: false),
                    CommercialComparisonId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestingDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalRoute = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TaxableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    HeaderDiscountValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    TaxValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    PackingForwarding = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Freight = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Insurance = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    OtherCharges = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    RoundOff = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    TotalPayableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ApprovalPolicySnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    PaymentTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DeliveryTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    WarrantyTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AmendmentReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TransitionCorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_orders", x => x.Id);
                    table.CheckConstraint("CK_purchase_order_cancel_reason", "\"Status\" <> 'Cancelled' OR (\"CancelledAt\" IS NOT NULL AND length(trim(coalesce(\"CancellationReason\", ''))) > 0)");
                    table.CheckConstraint("CK_purchase_order_revision", "\"RevisionNumber\" > 0 AND \"SequenceNumber\" > 0");
                    table.CheckConstraint("CK_purchase_order_current_lifecycle", "\"Status\" <> 'Superseded' OR NOT \"IsCurrentVersion\"");
                    table.CheckConstraint("CK_purchase_order_status", "\"Status\" IN ('Draft','PendingApproval','Approved','Issued','Rejected','RevisionDraft','Resubmitted','Superseded','Cancelled')");
                    table.CheckConstraint("CK_purchase_order_values", "\"TaxableValue\" >= 0 AND \"DiscountValue\" >= 0 AND \"HeaderDiscountValue\" >= 0 AND \"TaxValue\" >= 0 AND \"PackingForwarding\" >= 0 AND \"Freight\" >= 0 AND \"Insurance\" >= 0 AND \"OtherCharges\" >= 0 AND \"TotalPayableValue\" >= 0 AND \"ApprovalPolicySnapshotJson\" <> '{}'::jsonb");
                    table.ForeignKey(
                        name: "FK_purchase_orders_commercial_comparisons_CommercialComparison~",
                        column: x => x.CommercialComparisonId,
                        principalSchema: "nexa",
                        principalTable: "commercial_comparisons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_departments_RequestingDepartmentId",
                        column: x => x.RequestingDepartmentId,
                        principalSchema: "nexa",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_employees_OwnerEmployeeId",
                        column: x => x.OwnerEmployeeId,
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_purchase_orders_PreviousVersionId",
                        column: x => x.PreviousVersionId,
                        principalSchema: "nexa",
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "nexa",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_warehouses_DeliveryWarehouseId",
                        column: x => x.DeliveryWarehouseId,
                        principalSchema: "nexa",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_transaction_approval_history",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommercialComparisonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ApprovalRoute = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorLoginId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_transaction_approval_history", x => x.Id);
                    table.CheckConstraint("CK_purchase_approval_history_remarks", "length(trim(\"Remarks\")) > 0");
                    table.ForeignKey(
                        name: "FK_purchase_transaction_approval_history_commercial_comparison~",
                        column: x => x.CommercialComparisonId,
                        principalSchema: "nexa",
                        principalTable: "commercial_comparisons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_transaction_approval_history_employees_ActorEmploy~",
                        column: x => x.ActorEmployeeId,
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_comparison_lines",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommercialComparisonId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorQuotationLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalComplianceSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CommercialSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    DeliverySnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    WarrantySnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PaymentTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TotalPayableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    IsRecommended = table.Column<bool>(type: "boolean", nullable: false),
                    RecommendationReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_comparison_lines", x => x.Id);
                    table.CheckConstraint("CK_comparison_line_total", "\"TotalPayableValue\" >= 0 AND (NOT \"IsRecommended\" OR length(trim(coalesce(\"RecommendationReason\", ''))) > 0)");
                    table.ForeignKey(
                        name: "FK_commercial_comparison_lines_commercial_comparisons_Commerci~",
                        column: x => x.CommercialComparisonId,
                        principalSchema: "nexa",
                        principalTable: "commercial_comparisons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_comparison_lines_vendor_quotation_lines_VendorQu~",
                        column: x => x.VendorQuotationLineId,
                        principalSchema: "nexa",
                        principalTable: "vendor_quotation_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_comparison_lines_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "nexa",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quotation_technical_verifications",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorQuotationLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerifierEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ComplianceStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ComplianceSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_technical_verifications", x => x.Id);
                    table.CheckConstraint("CK_quote_technical_status", "\"ComplianceStatus\" IN ('TechnicallyCompliant','TechnicallyRejected') AND length(trim(\"Remarks\")) > 0");
                    table.ForeignKey(
                        name: "FK_quotation_technical_verifications_employees_VerifierEmploye~",
                        column: x => x.VerifierEmployeeId,
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotation_technical_verifications_vendor_quotation_lines_Ve~",
                        column: x => x.VendorQuotationLineId,
                        principalSchema: "nexa",
                        principalTable: "vendor_quotation_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_history",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorLoginId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_history", x => x.Id);
                    table.CheckConstraint("CK_purchase_order_history_reason", "length(trim(\"Reason\")) > 0");
                    table.ForeignKey(
                        name: "FK_purchase_order_history_employees_ActorEmployeeId",
                        column: x => x.ActorEmployeeId,
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_history_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "nexa",
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_lines",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommercialComparisonLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequirementHandoffId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UomSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ApprovedOutstandingQuantitySnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    UnitRate = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CommercialSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    TaxRuleSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    TotalPayableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_lines", x => x.Id);
                    table.CheckConstraint("CK_purchase_order_line_quantity", "\"OrderedQuantity\" > 0 AND \"ApprovedOutstandingQuantitySnapshot\" > 0 AND \"OrderedQuantity\" <= \"ApprovedOutstandingQuantitySnapshot\" AND \"UnitRate\" >= 0 AND \"TotalPayableValue\" >= 0");
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_commercial_comparison_lines_Commercial~",
                        column: x => x.CommercialComparisonLineId,
                        principalSchema: "nexa",
                        principalTable: "commercial_comparison_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "nexa",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "nexa",
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_purchase_requirement_handoffs_Purchase~",
                        column: x => x.PurchaseRequirementHandoffId,
                        principalSchema: "nexa",
                        principalTable: "purchase_requirement_handoffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_purchase_requisition_lines_PurchaseReq~",
                        column: x => x.PurchaseRequisitionLineId,
                        principalSchema: "nexa",
                        principalTable: "purchase_requisition_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_followup_handoffs",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandoffNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OrderedQuantitySnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    HandoffAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_followup_handoffs", x => x.Id);
                    table.CheckConstraint("CK_material_followup_quantity", "\"OrderedQuantitySnapshot\" > 0 AND \"Status\" IN ('PendingFollowUp','InProgress','Completed')");
                    table.ForeignKey(
                        name: "FK_material_followup_handoffs_purchase_order_lines_PurchaseOrd~",
                        column: x => x.PurchaseOrderLineId,
                        principalSchema: "nexa",
                        principalTable: "purchase_order_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_followup_handoffs_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "nexa",
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "nexa",
                table: "page_definitions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("21231666-baa1-d0fb-60c4-aff55813333f"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, "Purchase", "purchase.vendor-quotations", "/purchase/vendor-quotations", "Vendor Quotations", null, null, 0L },
                    { new Guid("2be06aa9-fb95-c4b6-1f26-c27f71cd58e9"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, "Purchase", "purchase.material-followup", "/purchase/material-followup", "Material Follow-up", null, null, 0L },
                    { new Guid("45ee1d9e-ac42-f959-ed98-df00b9c20bb0"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, "Purchase", "purchase.commercial-comparisons", "/purchase/commercial-comparisons", "Commercial Comparisons", null, null, 0L },
                    { new Guid("e6c278a8-9f01-4a07-845a-1aba37ca0e46"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, "Purchase", "purchase.technical-verification", "/purchase/technical-verification", "Technical Verification", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "nexa",
                table: "purchase_transaction_approval_policies",
                columns: new[] { "Id", "ApproverRoleCode", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "IsActive", "MaximumAmount", "MinimumAmount", "OrganizationId", "RouteCode", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("0e6e49ea-95c5-86dd-1c23-18d61c50f4c1"), "MANAGING_DIRECTOR", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", new DateOnly(2026, 8, 11), null, true, 999999999999999999.999999m, 500000.000001m, "SESS", "MANAGING_DIRECTOR", null, null, 0L },
                    { new Guid("d7b12d20-a4be-c916-9f5e-de2245510b91"), "PURCHASE_MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", new DateOnly(2026, 8, 11), null, true, 50000m, 0m, "SESS", "MANAGER", null, null, 0L },
                    { new Guid("f9505a0c-182b-7627-52f4-1197a29e4c16"), "TECHNICAL_DIRECTOR", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", new DateOnly(2026, 8, 11), null, true, 500000m, 50000.000001m, "SESS", "TECHNICAL_DIRECTOR", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "nexa",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("01a0c648-2bdc-3643-465f-d013309c37be"), false, true, true, false, true, true, true, false, false, false, false, true, true, true, true, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("20a822a5-146b-b1ab-b849-5b19b42d053b"), false, true, false, false, true, true, true, false, false, true, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("4235c07c-e564-bbf7-e475-eda92f8f8a15"), true, true, false, false, true, true, true, true, false, false, true, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("4e10adcf-6248-dceb-cb7c-5ce74abcec69"), true, true, false, false, true, true, true, true, false, false, true, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("7802b358-eab1-01d0-24b6-2ea8f479222a"), false, false, false, false, true, false, true, false, false, true, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("84b35820-207b-51a5-bf29-205365672b1d"), false, true, false, false, true, true, true, false, false, true, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("9d745645-4715-debc-7899-f8f307dea12e"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("a915b601-7adc-9bdc-9f57-38f51929fd64"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("c81d042a-8e3b-c955-e28d-688c09fa5e55"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("d5caa783-5666-ecf5-aa06-6d2b302c30c7"), false, false, true, false, true, false, true, false, false, true, false, false, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("fd4c02e7-4bcf-cf41-bc84-0c4025efae03"), false, true, true, false, true, true, true, false, false, true, false, false, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("35794bca-a9b2-9e06-d997-a7031ebd5a24"), true, false, false, false, true, true, true, true, false, true, true, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("45ee1d9e-ac42-f959-ed98-df00b9c20bb0"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("4527d01a-d157-1d9f-fc8e-77bc2e2fdd00"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("2be06aa9-fb95-c4b6-1f26-c27f71cd58e9"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("478a8966-7033-50b4-591e-18765aa441bb"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("21231666-baa1-d0fb-60c4-aff55813333f"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("47e11a52-579e-038e-8fd3-f4c25dd72bb5"), true, false, false, false, true, true, true, true, false, true, true, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, new Guid("45ee1d9e-ac42-f959-ed98-df00b9c20bb0"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("4d6fc2ba-2e1b-bdd4-d132-c87b77102ccb"), false, true, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("21231666-baa1-d0fb-60c4-aff55813333f"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("6ae6814b-2691-8ca4-9fcb-a280f5a0abaa"), false, true, true, false, true, true, true, false, false, false, false, false, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("21231666-baa1-d0fb-60c4-aff55813333f"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("7e2b6cda-fef5-6891-4b5a-9dda984ea76c"), false, false, false, false, true, true, true, false, false, true, false, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("e6c278a8-9f01-4a07-845a-1aba37ca0e46"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("822a0e7d-4b77-a6f5-bd9b-2117b6675e7e"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("2be06aa9-fb95-c4b6-1f26-c27f71cd58e9"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("88fe402d-de4f-37cb-8d43-b187d534e57d"), false, false, true, false, true, false, true, false, false, true, false, false, true, true, true, true, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("e6c278a8-9f01-4a07-845a-1aba37ca0e46"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("899d4333-60f8-7e14-4ee3-52edb1baa52c"), false, false, true, false, true, false, true, false, false, false, false, false, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("21231666-baa1-d0fb-60c4-aff55813333f"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("9e371f64-5812-c791-9e35-41c95554f945"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, new Guid("2be06aa9-fb95-c4b6-1f26-c27f71cd58e9"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("a103fb11-2d03-59f1-2d58-c823ade55568"), false, true, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, new Guid("21231666-baa1-d0fb-60c4-aff55813333f"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("a9e03f47-cc6e-9cf1-c48c-6b2a8f14b3e6"), false, false, false, false, true, true, true, false, false, true, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("45ee1d9e-ac42-f959-ed98-df00b9c20bb0"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("acd3edac-34a5-b17d-13c6-91b098ce03bb"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("2be06aa9-fb95-c4b6-1f26-c27f71cd58e9"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("b5a0a82a-8bf9-abdb-c594-8185fa6de8d2"), true, false, true, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("45ee1d9e-ac42-f959-ed98-df00b9c20bb0"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("c80614d6-27dc-22a0-b1b9-0a9f5b1536ea"), false, false, false, false, true, true, true, false, false, true, false, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, new Guid("e6c278a8-9f01-4a07-845a-1aba37ca0e46"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("e68927ed-7502-fd94-30dc-08fcdc435577"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("2be06aa9-fb95-c4b6-1f26-c27f71cd58e9"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("e818913d-251c-5c1c-8395-3ea116a3c0b2"), false, false, false, false, true, true, true, false, false, true, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("e6c278a8-9f01-4a07-845a-1aba37ca0e46"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L }
                });

            migrationBuilder.Sql("""UPDATE nexa.role_page_permissions SET "CanIssue" = TRUE WHERE "Id" = '01a0c648-2bdc-3643-465f-d013309c37be'::uuid AND "CreatedBy" = 'migration-rev869b';""");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparison_lines_CommercialComparisonId_VendorQu~",
                schema: "nexa",
                table: "commercial_comparison_lines",
                columns: new[] { "CommercialComparisonId", "VendorQuotationLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparison_lines_VendorId",
                schema: "nexa",
                table: "commercial_comparison_lines",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparison_lines_VendorQuotationLineId",
                schema: "nexa",
                table: "commercial_comparison_lines",
                column: "VendorQuotationLineId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparisons_OrganizationId_ComparisonNumber",
                schema: "nexa",
                table: "commercial_comparisons",
                columns: new[] { "OrganizationId", "ComparisonNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparisons_OrganizationId_IdempotencyKey",
                schema: "nexa",
                table: "commercial_comparisons",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparisons_OwnerEmployeeId",
                schema: "nexa",
                table: "commercial_comparisons",
                column: "OwnerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparisons_RecommendedVendorQuotationId",
                schema: "nexa",
                table: "commercial_comparisons",
                column: "RecommendedVendorQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparisons_RequestForQuotationId",
                schema: "nexa",
                table: "commercial_comparisons",
                column: "RequestForQuotationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparisons_SelectedVendorId",
                schema: "nexa",
                table: "commercial_comparisons",
                column: "SelectedVendorId");

            migrationBuilder.CreateIndex(
                name: "IX_material_followup_handoffs_HandoffNumber",
                schema: "nexa",
                table: "material_followup_handoffs",
                column: "HandoffNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_followup_handoffs_PurchaseOrderId",
                schema: "nexa",
                table: "material_followup_handoffs",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_material_followup_handoffs_PurchaseOrderLineId",
                schema: "nexa",
                table: "material_followup_handoffs",
                column: "PurchaseOrderLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_history_ActorEmployeeId",
                schema: "nexa",
                table: "purchase_order_history",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_history_PurchaseOrderId_CorrelationId",
                schema: "nexa",
                table: "purchase_order_history",
                columns: new[] { "PurchaseOrderId", "CorrelationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_history_PurchaseOrderId_CreatedAt",
                schema: "nexa",
                table: "purchase_order_history",
                columns: new[] { "PurchaseOrderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_CommercialComparisonLineId",
                schema: "nexa",
                table: "purchase_order_lines",
                column: "CommercialComparisonLineId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_ItemId",
                schema: "nexa",
                table: "purchase_order_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_PurchaseOrderId_CommercialComparisonLi~",
                schema: "nexa",
                table: "purchase_order_lines",
                columns: new[] { "PurchaseOrderId", "CommercialComparisonLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_PurchaseOrderId_LineNumber",
                schema: "nexa",
                table: "purchase_order_lines",
                columns: new[] { "PurchaseOrderId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_PurchaseRequirementHandoffId",
                schema: "nexa",
                table: "purchase_order_lines",
                column: "PurchaseRequirementHandoffId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_PurchaseRequisitionLineId_PurchaseOrde~",
                schema: "nexa",
                table: "purchase_order_lines",
                columns: new[] { "PurchaseRequisitionLineId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_CommercialComparisonId_RevisionNumber",
                schema: "nexa",
                table: "purchase_orders",
                columns: new[] { "CommercialComparisonId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_DeliveryWarehouseId",
                schema: "nexa",
                table: "purchase_orders",
                column: "DeliveryWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_OrganizationId_IdempotencyKey",
                schema: "nexa",
                table: "purchase_orders",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_OrganizationId_PoNumber_RevisionNumber",
                schema: "nexa",
                table: "purchase_orders",
                columns: new[] { "OrganizationId", "PoNumber", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_OwnerEmployeeId",
                schema: "nexa",
                table: "purchase_orders",
                column: "OwnerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_PreviousVersionId",
                schema: "nexa",
                table: "purchase_orders",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_RequestingDepartmentId",
                schema: "nexa",
                table: "purchase_orders",
                column: "RequestingDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_RootPurchaseOrderId_IsCurrentVersion",
                schema: "nexa",
                table: "purchase_orders",
                columns: new[] { "RootPurchaseOrderId", "IsCurrentVersion" },
                unique: true,
                filter: "\"IsCurrentVersion\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_RootPurchaseOrderId_RevisionNumber",
                schema: "nexa",
                table: "purchase_orders",
                columns: new[] { "RootPurchaseOrderId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_VendorId",
                schema: "nexa",
                table: "purchase_orders",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_transaction_approval_history_ActorEmployeeId",
                schema: "nexa",
                table: "purchase_transaction_approval_history",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_transaction_approval_history_CommercialComparison~1",
                schema: "nexa",
                table: "purchase_transaction_approval_history",
                columns: new[] { "CommercialComparisonId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_transaction_approval_history_CommercialComparisonI~",
                schema: "nexa",
                table: "purchase_transaction_approval_history",
                columns: new[] { "CommercialComparisonId", "CorrelationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_transaction_approval_policies_OrganizationId_Route~",
                schema: "nexa",
                table: "purchase_transaction_approval_policies",
                columns: new[] { "OrganizationId", "RouteCode", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_transaction_status_history_ActorEmployeeId",
                schema: "nexa",
                table: "purchase_transaction_status_history",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_transaction_status_history_EntityType_EntityId_Cor~",
                schema: "nexa",
                table: "purchase_transaction_status_history",
                columns: new[] { "EntityType", "EntityId", "CorrelationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_transaction_status_history_EntityType_EntityId_Cre~",
                schema: "nexa",
                table: "purchase_transaction_status_history",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_quotation_technical_verifications_VendorQuotationLineId",
                schema: "nexa",
                table: "quotation_technical_verifications",
                column: "VendorQuotationLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotation_technical_verifications_VerifierEmployeeId",
                schema: "nexa",
                table: "quotation_technical_verifications",
                column: "VerifierEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotation_lines_ItemId",
                schema: "nexa",
                table: "request_for_quotation_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotation_lines_PurchaseRequirementHandoffId",
                schema: "nexa",
                table: "request_for_quotation_lines",
                column: "PurchaseRequirementHandoffId");

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotation_lines_PurchaseRequisitionLineId",
                schema: "nexa",
                table: "request_for_quotation_lines",
                column: "PurchaseRequisitionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotation_lines_RequestForQuotationId_LineNumber",
                schema: "nexa",
                table: "request_for_quotation_lines",
                columns: new[] { "RequestForQuotationId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotation_lines_RequestForQuotationId_PurchaseR~",
                schema: "nexa",
                table: "request_for_quotation_lines",
                columns: new[] { "RequestForQuotationId", "PurchaseRequirementHandoffId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotations_DeliveryWarehouseId",
                schema: "nexa",
                table: "request_for_quotations",
                column: "DeliveryWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotations_OrganizationId_FinancialYear_Sequenc~",
                schema: "nexa",
                table: "request_for_quotations",
                columns: new[] { "OrganizationId", "FinancialYear", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotations_OrganizationId_IdempotencyKey",
                schema: "nexa",
                table: "request_for_quotations",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotations_OrganizationId_RfqNumber",
                schema: "nexa",
                table: "request_for_quotations",
                columns: new[] { "OrganizationId", "RfqNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotations_OwnerEmployeeId",
                schema: "nexa",
                table: "request_for_quotations",
                column: "OwnerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotations_PurchaseRequisitionId_Status",
                schema: "nexa",
                table: "request_for_quotations",
                columns: new[] { "PurchaseRequisitionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotations_RequestingDepartmentId",
                schema: "nexa",
                table: "request_for_quotations",
                column: "RequestingDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_rfq_vendor_invitations_RequestForQuotationId_IdempotencyKey",
                schema: "nexa",
                table: "rfq_vendor_invitations",
                columns: new[] { "RequestForQuotationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rfq_vendor_invitations_RequestForQuotationId_VendorId",
                schema: "nexa",
                table: "rfq_vendor_invitations",
                columns: new[] { "RequestForQuotationId", "VendorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rfq_vendor_invitations_VendorId",
                schema: "nexa",
                table: "rfq_vendor_invitations",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotation_lines_RequestForQuotationLineId",
                schema: "nexa",
                table: "vendor_quotation_lines",
                column: "RequestForQuotationLineId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotation_lines_TaxGstSettingId",
                schema: "nexa",
                table: "vendor_quotation_lines",
                column: "TaxGstSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotation_lines_VendorQuotationId_LineNumber",
                schema: "nexa",
                table: "vendor_quotation_lines",
                columns: new[] { "VendorQuotationId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotation_lines_VendorQuotationId_RequestForQuotatio~",
                schema: "nexa",
                table: "vendor_quotation_lines",
                columns: new[] { "VendorQuotationId", "RequestForQuotationLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotations_LateAuthorizedByEmployeeId",
                schema: "nexa",
                table: "vendor_quotations",
                column: "LateAuthorizedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotations_OrganizationId_IdempotencyKey",
                schema: "nexa",
                table: "vendor_quotations",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotations_OrganizationId_QuotationNumber",
                schema: "nexa",
                table: "vendor_quotations",
                columns: new[] { "OrganizationId", "QuotationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotations_PreviousRevisionId",
                schema: "nexa",
                table: "vendor_quotations",
                column: "PreviousRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotations_RfqVendorInvitationId_RevisionNumber",
                schema: "nexa",
                table: "vendor_quotations",
                columns: new[] { "RfqVendorInvitationId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotations_RootQuotationId_IsCurrentRevision",
                schema: "nexa",
                table: "vendor_quotations",
                columns: new[] { "RootQuotationId", "IsCurrentRevision" },
                unique: true,
                filter: "\"IsCurrentRevision\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotations_VendorId",
                schema: "nexa",
                table: "vendor_quotations",
                column: "VendorId");

            migrationBuilder.Sql("""
                DO $rev869b$
                DECLARE department_manager_role_id uuid;
                BEGIN
                    SELECT "Id" INTO department_manager_role_id FROM nexa.roles WHERE upper(trim("Code")) = 'DEPARTMENT_MANAGER' AND "IsActive" = TRUE;
                    IF department_manager_role_id IS NULL THEN RAISE EXCEPTION 'REV869B requires the existing active DEPARTMENT_MANAGER role.'; END IF;
                    IF (SELECT count(*) FROM nexa.page_definitions WHERE "PageKey" IN ('purchase.rfq','purchase.commercial-comparisons','purchase.po') AND "IsActive" = TRUE) <> 3 THEN RAISE EXCEPTION 'REV869B Department Manager pages are missing or ambiguous.'; END IF;
                    INSERT INTO nexa.role_page_permissions
                    ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
                    SELECT permission_id,department_manager_role_id,p."Id",TRUE,FALSE,FALSE,FALSE,FALSE,
                           p."PageKey" = 'purchase.po',p."PageKey" = 'purchase.po',TRUE,p."PageKey" = 'purchase.po',FALSE,FALSE,FALSE,TRUE,TRUE,FALSE,FALSE,FALSE,FALSE,TRUE,FALSE,
                           TIMESTAMPTZ '1970-01-01 00:00:00+00','migration-rev869b',0
                    FROM (VALUES
                        ('918d0634-ff61-c756-98f4-a17290d04110'::uuid,'purchase.rfq'),
                        ('062fd69d-1356-6930-ecdb-1c13ae5a01d5'::uuid,'purchase.commercial-comparisons'),
                        ('c9ec48cd-024d-128e-697f-389458e12c97'::uuid,'purchase.po')
                    ) expected(permission_id,page_key) JOIN nexa.page_definitions p ON p."PageKey" = expected.page_key;
                END $rev869b$;

                CREATE OR REPLACE FUNCTION nexa.rev869b_reject_immutable_mutation() RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, nexa AS $rev869b$
                BEGIN RAISE EXCEPTION 'REV869B controlled history/snapshot relation % is immutable.', TG_TABLE_NAME; END $rev869b$;

                CREATE OR REPLACE FUNCTION nexa.rev869b_guard_controlled_snapshot() RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, nexa AS $rev869b$
                BEGIN
                    IF TG_TABLE_NAME = 'vendor_quotations' AND
                       to_jsonb(NEW) - ARRAY['Status','Version','IsCurrentRevision','UpdatedAt','UpdatedBy'] <> to_jsonb(OLD) - ARRAY['Status','Version','IsCurrentRevision','UpdatedAt','UpdatedBy'] THEN
                        RAISE EXCEPTION 'Submitted quotation provenance and commercial terms are immutable.';
                    ELSIF TG_TABLE_NAME = 'commercial_comparisons' AND OLD."Status" NOT IN ('Draft','RevisionRequested') AND
                       to_jsonb(NEW) - ARRAY['Status','Version','UpdatedAt','UpdatedBy'] <> to_jsonb(OLD) - ARRAY['Status','Version','UpdatedAt','UpdatedBy'] THEN
                        RAISE EXCEPTION 'Submitted comparison snapshot is immutable.';
                    ELSIF TG_TABLE_NAME = 'commercial_comparison_lines' AND
                       EXISTS (SELECT 1 FROM nexa.commercial_comparisons c WHERE c."Id" = OLD."CommercialComparisonId" AND c."Status" NOT IN ('Draft','RevisionRequested')) THEN
                        RAISE EXCEPTION 'Submitted comparison line snapshot is immutable.';
                    ELSIF TG_TABLE_NAME = 'purchase_orders' AND
                       to_jsonb(NEW) - ARRAY['Status','Version','IsCurrentVersion','IssuedAt','CancelledAt','CancellationReason','UpdatedAt','UpdatedBy'] <> to_jsonb(OLD) - ARRAY['Status','Version','IsCurrentVersion','IssuedAt','CancelledAt','CancellationReason','UpdatedAt','UpdatedBy'] THEN
                        RAISE EXCEPTION 'Purchase order commercial and provenance snapshot is immutable.';
                    END IF;
                    RETURN NEW;
                END $rev869b$;

                CREATE OR REPLACE FUNCTION nexa.rev869b_enforce_transition() RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, nexa AS $rev869b$
                DECLARE allowed boolean := false;
                BEGIN
                    IF TG_OP = 'INSERT' THEN
                        IF TG_TABLE_NAME = 'request_for_quotations' AND NEW."Status" <> 'Draft' THEN
                            RAISE EXCEPTION 'RFQ must be inserted in Draft status.';
                        ELSIF TG_TABLE_NAME = 'rfq_vendor_invitations' AND NEW."Status" <> 'Issued' THEN
                            RAISE EXCEPTION 'RFQ invitation must be inserted in Issued status.';
                        ELSIF TG_TABLE_NAME = 'vendor_quotations' AND NEW."Status" <> 'Submitted' THEN
                            RAISE EXCEPTION 'Quotation must be inserted in Submitted status.';
                        ELSIF TG_TABLE_NAME = 'commercial_comparisons' AND NEW."Status" <> 'Draft' THEN
                            RAISE EXCEPTION 'Comparison must be inserted in Draft status.';
                        ELSIF TG_TABLE_NAME = 'purchase_orders' AND NEW."Status" NOT IN ('Draft','RevisionDraft') THEN
                            RAISE EXCEPTION 'Purchase order must be inserted in a controlled draft status.';
                        ELSIF TG_TABLE_NAME = 'purchase_orders' AND NEW."Status" = 'RevisionDraft' AND NOT EXISTS (
                            SELECT 1 FROM nexa.purchase_orders p
                            WHERE p."Id" = NEW."PreviousVersionId" AND p."OrganizationId" = NEW."OrganizationId"
                              AND p."RootPurchaseOrderId" = NEW."RootPurchaseOrderId" AND p."PoNumber" = NEW."PoNumber"
                              AND p."RevisionNumber" + 1 = NEW."RevisionNumber" AND p."Status" = 'Rejected' AND NOT p."IsCurrentVersion") THEN
                            RAISE EXCEPTION 'RevisionDraft requires an immutable rejected predecessor.';
                        END IF;
                        RETURN NEW;
                    END IF;
                    IF NEW."Version" <> OLD."Version" + 1 THEN RAISE EXCEPTION 'REV869B aggregate version must increment by exactly one.'; END IF;
                    IF TG_TABLE_NAME = 'request_for_quotations' AND
                       (NEW."OrganizationId",NEW."PurchaseRequisitionId") IS DISTINCT FROM (OLD."OrganizationId",OLD."PurchaseRequisitionId") THEN
                        RAISE EXCEPTION 'RFQ organization and parent are immutable.';
                    ELSIF TG_TABLE_NAME = 'rfq_vendor_invitations' AND
                       (NEW."RequestForQuotationId",NEW."VendorId") IS DISTINCT FROM (OLD."RequestForQuotationId",OLD."VendorId") THEN
                        RAISE EXCEPTION 'RFQ invitation parents are immutable.';
                    ELSIF TG_TABLE_NAME = 'vendor_quotations' AND
                       (NEW."OrganizationId",NEW."RfqVendorInvitationId",NEW."VendorId") IS DISTINCT FROM (OLD."OrganizationId",OLD."RfqVendorInvitationId",OLD."VendorId") THEN
                        RAISE EXCEPTION 'Quotation organization and parents are immutable.';
                    ELSIF TG_TABLE_NAME = 'commercial_comparisons' AND
                       (NEW."OrganizationId",NEW."RequestForQuotationId") IS DISTINCT FROM (OLD."OrganizationId",OLD."RequestForQuotationId") THEN
                        RAISE EXCEPTION 'Comparison organization and parent are immutable.';
                    ELSIF TG_TABLE_NAME = 'purchase_orders' AND
                       (NEW."OrganizationId",NEW."CommercialComparisonId",NEW."VendorId",NEW."RootPurchaseOrderId",NEW."PreviousVersionId") IS DISTINCT FROM
                       (OLD."OrganizationId",OLD."CommercialComparisonId",OLD."VendorId",OLD."RootPurchaseOrderId",OLD."PreviousVersionId") THEN
                        RAISE EXCEPTION 'Purchase order organization, provenance and version ancestry are immutable.';
                    END IF;
                    IF TG_TABLE_NAME = 'commercial_comparisons' AND NEW."Status" IN ('PendingApproval','Approved') AND (
                        NEW."RecommendedVendorQuotationId" IS NULL OR NEW."SelectedVendorId" IS NULL OR
                        length(trim(coalesce(NEW."ApprovalRoute", ''))) = 0 OR
                        NOT EXISTS (SELECT 1 FROM nexa.commercial_comparison_lines cl WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended") OR
                        EXISTS (
                            SELECT 1 FROM nexa.commercial_comparison_lines cl
                            JOIN nexa.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId"
                            JOIN nexa.vendor_quotations q ON q."Id"=ql."VendorQuotationId"
                            JOIN nexa.request_for_quotation_lines rl ON rl."Id"=ql."RequestForQuotationLineId"
                            WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended" AND (
                                cl."VendorId" <> NEW."SelectedVendorId" OR q."Id" <> NEW."RecommendedVendorQuotationId" OR
                                q."OrganizationId" <> NEW."OrganizationId" OR q."VendorId" <> NEW."SelectedVendorId" OR
                                q."RevisionNumber" <= 0 OR NOT q."IsCurrentRevision" OR q."Status" <> 'TechnicallyCompliant' OR
                                coalesce(cl."CommercialSnapshotJson"->>'organizationId','') <> NEW."OrganizationId" OR
                                coalesce((cl."CommercialSnapshotJson"->>'commercialComparisonId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> NEW."Id" OR
                                coalesce((cl."CommercialSnapshotJson"->>'requestForQuotationId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> NEW."RequestForQuotationId" OR
                                coalesce((cl."CommercialSnapshotJson"->>'vendorId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> NEW."SelectedVendorId" OR
                                coalesce((cl."CommercialSnapshotJson"->>'vendorQuotationId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> q."Id" OR
                                coalesce((cl."CommercialSnapshotJson"->>'quotationRevision')::integer,0) <> q."RevisionNumber" OR
                                coalesce((cl."CommercialSnapshotJson"->>'vendorQuotationLineId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> ql."Id" OR
                                coalesce((cl."CommercialSnapshotJson"->>'itemId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> rl."ItemId" OR
                                coalesce((cl."CommercialSnapshotJson"->>'quantity')::numeric,-1) <> ql."Quantity" OR
                                coalesce(cl."CommercialSnapshotJson"->>'uom','') <> rl."UomSnapshot" OR
                                coalesce(cl."CommercialSnapshotJson"->>'currencyCode','') <> q."CurrencyCode" OR
                                coalesce((cl."CommercialSnapshotJson"->>'exchangeRate')::numeric,0) <> 1 OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'grossAmount')::numeric,-1) <> round(ql."Quantity"*ql."UnitRate",(ql."TaxRuleSnapshotJson"->>'roundingScale')::integer) OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'discountValue')::numeric,-1) <> ql."DiscountValue" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'headerDiscountValue')::numeric,-1) <> ql."HeaderDiscountValue" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'packingForwarding')::numeric,-1) <> ql."PackingForwarding" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'freight')::numeric,-1) <> ql."Freight" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'insurance')::numeric,-1) <> ql."Insurance" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'otherCharges')::numeric,-1) <> ql."OtherCharges" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'taxableValue')::numeric,-1) <> ql."TaxableValue" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'cgstValue')::numeric,-1) <> ql."CgstValue" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'sgstValue')::numeric,-1) <> ql."SgstValue" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'igstValue')::numeric,-1) <> ql."IgstValue" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'cessValue')::numeric,-1) <> ql."CessValue" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'roundOff')::numeric,-999999) <> ql."RoundOff" OR
                                coalesce((cl."CommercialSnapshotJson"->'result'->>'totalPayableValue')::numeric,-1) <> ql."TotalPayableValue" OR
                                cl."TotalPayableValue" <> ql."TotalPayableValue" OR
                                cl."CommercialSnapshotJson"->'taxRule' IS NULL OR
                                cl."CommercialSnapshotJson"->'taxRule' IS DISTINCT FROM ql."TaxRuleSnapshotJson"
                            )) OR
                        (SELECT count(*) FROM nexa.commercial_comparison_lines cl WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended") <>
                        (SELECT count(*) FROM nexa.vendor_quotation_lines ql WHERE ql."VendorQuotationId"=NEW."RecommendedVendorQuotationId") OR
                        NEW."TotalPayableValue" <> (SELECT coalesce(sum(cl."TotalPayableValue"),0) FROM nexa.commercial_comparison_lines cl WHERE cl."CommercialComparisonId"=NEW."Id" AND cl."IsRecommended")
                    ) THEN RAISE EXCEPTION 'Comparison snapshot is incomplete or does not exactly reconcile.'; END IF;
                    IF NEW."Status" = OLD."Status" THEN RETURN NEW; END IF;
                    IF TG_TABLE_NAME = 'request_for_quotations' THEN
                        allowed := (OLD."Status", NEW."Status") IN (('Draft','Issued'),('Draft','Cancelled'),('Issued','Closed'),('Issued','Cancelled'));
                    ELSIF TG_TABLE_NAME = 'rfq_vendor_invitations' THEN
                        allowed := (OLD."Status", NEW."Status") IN (('Issued','Submitted'),('Issued','Withdrawn'),('Issued','Cancelled'));
                    ELSIF TG_TABLE_NAME = 'vendor_quotations' THEN
                        allowed := (OLD."Status", NEW."Status") IN (
                            ('Submitted','TechnicallyCompliant'),('Submitted','TechnicallyRejected'),('Submitted','Superseded'),('Submitted','Withdrawn'),
                            ('TechnicallyCompliant','Superseded'),('TechnicallyCompliant','Withdrawn'),
                            ('TechnicallyRejected','Superseded'),('TechnicallyRejected','Withdrawn'),('TechnicallyRejected','Rejected'));
                    ELSIF TG_TABLE_NAME = 'commercial_comparisons' THEN
                        allowed := (OLD."Status", NEW."Status") IN (
                            ('Draft','PendingApproval'),('Draft','Cancelled'),('PendingApproval','Approved'),('PendingApproval','Rejected'),
                            ('PendingApproval','RevisionRequested'),('RevisionRequested','PendingApproval'),('RevisionRequested','Cancelled'));
                    ELSIF TG_TABLE_NAME = 'purchase_orders' THEN
                        allowed := (OLD."Status", NEW."Status") IN (
                            ('Draft','PendingApproval'),('Draft','Cancelled'),('RevisionDraft','Resubmitted'),('RevisionDraft','Cancelled'),
                            ('PendingApproval','Approved'),('PendingApproval','Rejected'),('PendingApproval','Cancelled'),
                            ('Resubmitted','Approved'),('Resubmitted','Rejected'),('Resubmitted','Cancelled'),
                            ('Approved','Issued'),('Approved','Cancelled'),('Issued','Superseded'),('Issued','Cancelled'));
                        IF OLD."Status" = 'Approved' AND NEW."Status" = 'Issued' AND (
                            length(trim(coalesce(NEW."PaymentTermsSnapshot", ''))) = 0 OR
                            length(trim(coalesce(NEW."DeliveryTermsSnapshot", ''))) = 0 OR
                            length(trim(coalesce(NEW."WarrantyTermsSnapshot", ''))) = 0 OR
                            length(trim(coalesce(NEW."ApprovalRoute", ''))) = 0 OR
                            NOT EXISTS (SELECT 1 FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            EXISTS (SELECT 1 FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id" AND
                                (l."OrderedQuantity" <= 0 OR l."ApprovedOutstandingQuantitySnapshot" <= 0 OR
                                 l."CommercialSnapshotJson" IS NULL OR l."CommercialSnapshotJson" = '{}'::jsonb OR
                                 l."TaxRuleSnapshotJson" IS NULL OR l."TaxRuleSnapshotJson" = '{}'::jsonb OR
                                 coalesce(l."CommercialSnapshotJson"->>'organizationId','') <> NEW."OrganizationId" OR
                                 coalesce((l."CommercialSnapshotJson"->>'vendorQuotationId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) = '00000000-0000-0000-0000-000000000000'::uuid OR
                                 coalesce((l."CommercialSnapshotJson"->>'vendorQuotationLineId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) = '00000000-0000-0000-0000-000000000000'::uuid OR
                                 coalesce((l."CommercialSnapshotJson"->>'requestForQuotationId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) = '00000000-0000-0000-0000-000000000000'::uuid OR
                                 coalesce((l."CommercialSnapshotJson"->>'quotationRevision')::integer,0) <= 0 OR
                                 coalesce((l."CommercialSnapshotJson"->>'commercialComparisonId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> NEW."CommercialComparisonId" OR
                                 coalesce((l."CommercialSnapshotJson"->>'vendorId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> NEW."VendorId" OR
                                 coalesce((l."CommercialSnapshotJson"->>'itemId')::uuid,'00000000-0000-0000-0000-000000000000'::uuid) <> l."ItemId" OR
                                 coalesce((l."CommercialSnapshotJson"->>'quantity')::numeric,-1) <> l."OrderedQuantity" OR
                                 coalesce(l."CommercialSnapshotJson"->>'uom','') <> l."UomSnapshot" OR
                                 coalesce(l."CommercialSnapshotJson"->>'currencyCode','') <> NEW."CurrencyCode" OR
                                 coalesce((l."CommercialSnapshotJson"->>'exchangeRate')::numeric,0) <> 1 OR
                                 coalesce(l."CommercialSnapshotJson"->>'vendorQualificationSnapshotJson','{}') = '{}' OR
                                 length(trim(coalesce(l."CommercialSnapshotJson"->>'attachmentObjectKey',''))) = 0 OR
                                 length(coalesce(l."CommercialSnapshotJson"->>'attachmentSha256','')) <> 64 OR
                                 coalesce(l."TaxRuleSnapshotJson"->>'organizationId','') <> NEW."OrganizationId" OR
                                 coalesce(l."TaxRuleSnapshotJson"->>'approvalStatus','') <> 'Approved' OR
                                 coalesce((l."TaxRuleSnapshotJson"->>'isActive')::boolean,FALSE) IS NOT TRUE OR
                                 length(trim(coalesce(l."TaxRuleSnapshotJson"->>'hsnSacCode',''))) = 0 OR
                                 coalesce(l."CommercialSnapshotJson"->>'comparisonApprovalRoute','') <> NEW."ApprovalRoute" OR
                                 coalesce((l."CommercialSnapshotJson"->'result'->>'totalPayableValue')::numeric,-1) <> l."TotalPayableValue")) OR
                            NEW."TaxableValue" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'taxableValue')::numeric), 0) FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."DiscountValue" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'discountValue')::numeric), 0) FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."HeaderDiscountValue" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'headerDiscountValue')::numeric), 0) FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."TaxValue" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'cgstValue')::numeric + (l."CommercialSnapshotJson"->'result'->>'sgstValue')::numeric + (l."CommercialSnapshotJson"->'result'->>'igstValue')::numeric + (l."CommercialSnapshotJson"->'result'->>'cessValue')::numeric), 0) FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."PackingForwarding" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'packingForwarding')::numeric), 0) FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."Freight" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'freight')::numeric), 0) FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."Insurance" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'insurance')::numeric), 0) FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."OtherCharges" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'otherCharges')::numeric), 0) FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."RoundOff" <> (SELECT coalesce(sum((l."CommercialSnapshotJson"->'result'->>'roundOff')::numeric), 0) FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."TotalPayableValue" <> (SELECT coalesce(sum(l."TotalPayableValue"), 0) FROM nexa.purchase_order_lines l WHERE l."PurchaseOrderId" = NEW."Id") OR
                            NEW."ApprovalPolicySnapshotJson" IS NULL OR NEW."ApprovalPolicySnapshotJson" = '{}'::jsonb OR
                            coalesce(NEW."ApprovalPolicySnapshotJson"->>'organizationId','') <> NEW."OrganizationId" OR
                            coalesce(NEW."ApprovalPolicySnapshotJson"->>'routeCode','') <> NEW."ApprovalRoute" OR
                            coalesce((NEW."ApprovalPolicySnapshotJson"->>'approvalValue')::numeric,-1) <> NEW."TotalPayableValue" OR
                            length(trim(coalesce(NEW."ApprovalPolicySnapshotJson"->>'effectiveOn',''))) = 0 OR
                            NOT EXISTS (SELECT 1 FROM nexa.purchase_order_history h WHERE h."PurchaseOrderId" = NEW."Id" AND h."ToStatus" = 'Approved' AND length(trim(h."Reason")) > 0) OR
                            NOT EXISTS (SELECT 1 FROM nexa.purchase_transaction_approval_policies p WHERE p."OrganizationId" = NEW."OrganizationId" AND p."RouteCode" = NEW."ApprovalRoute" AND p."IsActive" AND NEW."TotalPayableValue" >= p."MinimumAmount" AND (p."MaximumAmount" IS NULL OR NEW."TotalPayableValue" <= p."MaximumAmount"))
                        ) THEN RAISE EXCEPTION 'Purchase order pre-issue snapshot is incomplete or does not reconcile.'; END IF;
                    END IF;
                    IF NOT allowed THEN RAISE EXCEPTION 'Illegal REV869B % status transition: % to %.', TG_TABLE_NAME, OLD."Status", NEW."Status"; END IF;
                    RETURN NEW;
                END $rev869b$;

                CREATE OR REPLACE FUNCTION nexa.rev869b_reject_overlapping_approval_policy() RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, nexa AS $rev869b$
                BEGIN
                    IF NEW."IsActive" AND EXISTS (
                        SELECT 1 FROM nexa.purchase_transaction_approval_policies p
                        WHERE p."Id" <> NEW."Id" AND p."OrganizationId" = NEW."OrganizationId" AND p."IsActive"
                          AND daterange(p."EffectiveFrom", coalesce(p."EffectiveTo", 'infinity'::date), '[]') &&
                              daterange(NEW."EffectiveFrom", coalesce(NEW."EffectiveTo", 'infinity'::date), '[]')
                          AND numrange(p."MinimumAmount", p."MaximumAmount", '[]') &&
                              numrange(NEW."MinimumAmount", NEW."MaximumAmount", '[]')) THEN
                        RAISE EXCEPTION 'Overlapping active purchase approval policies are prohibited.';
                    END IF;
                    RETURN NEW;
                END $rev869b$;

                CREATE OR REPLACE FUNCTION nexa.rev869b_validate_parent_contract() RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog, nexa AS $rev869b$
                BEGIN
                    IF TG_TABLE_NAME = 'vendor_quotation_lines' AND NOT EXISTS (
                        SELECT 1 FROM nexa.vendor_quotations q
                        JOIN nexa.rfq_vendor_invitations i ON i."Id" = q."RfqVendorInvitationId" AND i."VendorId" = q."VendorId"
                        JOIN nexa.request_for_quotations r ON r."Id" = i."RequestForQuotationId" AND r."OrganizationId" = q."OrganizationId"
                        JOIN nexa.request_for_quotation_lines rl ON rl."Id" = NEW."RequestForQuotationLineId" AND rl."RequestForQuotationId" = r."Id"
                        WHERE q."Id" = NEW."VendorQuotationId") THEN
                        RAISE EXCEPTION 'Quotation line parent contract mismatch.';
                    ELSIF TG_TABLE_NAME = 'quotation_technical_verifications' AND NOT EXISTS (
                        SELECT 1 FROM nexa.vendor_quotation_lines ql JOIN nexa.vendor_quotations q ON q."Id" = ql."VendorQuotationId" WHERE ql."Id" = NEW."VendorQuotationLineId") THEN
                        RAISE EXCEPTION 'Technical verification parent contract mismatch.';
                    ELSIF TG_TABLE_NAME = 'commercial_comparison_lines' AND NOT EXISTS (
                        SELECT 1 FROM nexa.commercial_comparisons c
                        JOIN nexa.vendor_quotation_lines ql ON ql."Id" = NEW."VendorQuotationLineId"
                        JOIN nexa.vendor_quotations q ON q."Id" = ql."VendorQuotationId" AND q."VendorId" = NEW."VendorId"
                        JOIN nexa.rfq_vendor_invitations i ON i."Id" = q."RfqVendorInvitationId" AND i."RequestForQuotationId" = c."RequestForQuotationId"
                        WHERE c."Id" = NEW."CommercialComparisonId" AND c."OrganizationId" = q."OrganizationId") THEN
                        RAISE EXCEPTION 'Comparison line parent contract mismatch.';
                    ELSIF TG_TABLE_NAME = 'purchase_orders' AND NOT EXISTS (
                        SELECT 1 FROM nexa.commercial_comparisons c
                        JOIN nexa.vendor_quotations q ON q."Id" = c."RecommendedVendorQuotationId" AND q."VendorId" = c."SelectedVendorId"
                        JOIN nexa.rfq_vendor_invitations i ON i."Id" = q."RfqVendorInvitationId" AND i."RequestForQuotationId" = c."RequestForQuotationId"
                        WHERE c."Id" = NEW."CommercialComparisonId" AND c."OrganizationId" = NEW."OrganizationId" AND c."SelectedVendorId" = NEW."VendorId") THEN
                        RAISE EXCEPTION 'Purchase order parent contract mismatch.';
                    ELSIF TG_TABLE_NAME = 'purchase_order_lines' AND NOT EXISTS (
                        SELECT 1 FROM nexa.purchase_orders p
                        JOIN nexa.commercial_comparison_lines cl ON cl."Id" = NEW."CommercialComparisonLineId" AND cl."CommercialComparisonId" = p."CommercialComparisonId"
                        JOIN nexa.vendor_quotation_lines ql ON ql."Id" = cl."VendorQuotationLineId"
                        JOIN nexa.request_for_quotation_lines rl ON rl."Id" = ql."RequestForQuotationLineId" AND rl."ItemId" = NEW."ItemId"
                        JOIN nexa.purchase_requirement_handoffs h ON h."Id" = NEW."PurchaseRequirementHandoffId" AND h."PurchaseRequisitionLineId" = NEW."PurchaseRequisitionLineId"
                        WHERE p."Id" = NEW."PurchaseOrderId" AND rl."PurchaseRequirementHandoffId" = h."Id") THEN
                        RAISE EXCEPTION 'Purchase order line parent contract mismatch.';
                    ELSIF TG_TABLE_NAME = 'material_followup_handoffs' AND NOT EXISTS (
                        SELECT 1 FROM nexa.purchase_order_lines pl WHERE pl."Id" = NEW."PurchaseOrderLineId" AND pl."PurchaseOrderId" = NEW."PurchaseOrderId") THEN
                        RAISE EXCEPTION 'Material follow-up parent contract mismatch.';
                    END IF;
                    RETURN NEW;
                END $rev869b$;

                CREATE TRIGGER trg_rev869b_vendor_quotation_snapshot_guard BEFORE UPDATE ON nexa.vendor_quotations FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_controlled_snapshot();
                CREATE TRIGGER trg_rev869b_comparison_snapshot_guard BEFORE UPDATE ON nexa.commercial_comparisons FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_controlled_snapshot();
                CREATE TRIGGER trg_rev869b_comparison_line_snapshot_guard BEFORE UPDATE ON nexa.commercial_comparison_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_controlled_snapshot();
                CREATE TRIGGER trg_rev869b_purchase_order_snapshot_guard BEFORE UPDATE ON nexa.purchase_orders FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_controlled_snapshot();
                CREATE TRIGGER trg_rev869b_rfq_transition_guard BEFORE INSERT OR UPDATE ON nexa.request_for_quotations FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_enforce_transition();
                CREATE TRIGGER trg_rev869b_invitation_transition_guard BEFORE INSERT OR UPDATE ON nexa.rfq_vendor_invitations FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_enforce_transition();
                CREATE TRIGGER trg_rev869b_comparison_transition_guard BEFORE INSERT OR UPDATE ON nexa.commercial_comparisons FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_enforce_transition();
                CREATE TRIGGER trg_rev869b_purchase_order_transition_guard BEFORE INSERT OR UPDATE ON nexa.purchase_orders FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_enforce_transition();
                CREATE TRIGGER trg_rev869b_quotation_line_parent_guard BEFORE INSERT OR UPDATE ON nexa.vendor_quotation_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_validate_parent_contract();
                CREATE TRIGGER trg_rev869b_technical_parent_guard BEFORE INSERT OR UPDATE ON nexa.quotation_technical_verifications FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_validate_parent_contract();
                CREATE TRIGGER trg_rev869b_comparison_line_parent_guard BEFORE INSERT OR UPDATE ON nexa.commercial_comparison_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_validate_parent_contract();
                CREATE TRIGGER trg_rev869b_purchase_order_parent_guard BEFORE INSERT OR UPDATE ON nexa.purchase_orders FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_validate_parent_contract();
                CREATE TRIGGER trg_rev869b_purchase_order_line_parent_guard BEFORE INSERT OR UPDATE ON nexa.purchase_order_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_validate_parent_contract();
                CREATE TRIGGER trg_rev869b_followup_parent_guard BEFORE INSERT OR UPDATE ON nexa.material_followup_handoffs FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_validate_parent_contract();
                CREATE TRIGGER trg_rev869b_vendor_quotation_lines_immutable BEFORE UPDATE OR DELETE ON nexa.vendor_quotation_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_immutable_mutation();
                CREATE TRIGGER trg_rev869b_technical_verifications_immutable BEFORE UPDATE OR DELETE ON nexa.quotation_technical_verifications FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_immutable_mutation();
                CREATE TRIGGER trg_rev869b_purchase_approval_history_immutable BEFORE UPDATE OR DELETE ON nexa.purchase_transaction_approval_history FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_immutable_mutation();
                CREATE TRIGGER trg_rev869b_purchase_order_lines_immutable BEFORE UPDATE OR DELETE ON nexa.purchase_order_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_immutable_mutation();
                CREATE TRIGGER trg_rev869b_purchase_order_history_immutable BEFORE UPDATE OR DELETE ON nexa.purchase_order_history FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_immutable_mutation();
                CREATE TRIGGER trg_rev869b_purchase_status_history_immutable BEFORE UPDATE OR DELETE ON nexa.purchase_transaction_status_history FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_immutable_mutation();
                CREATE TRIGGER trg_rev869b_approval_policy_overlap_guard BEFORE INSERT OR UPDATE ON nexa.purchase_transaction_approval_policies FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_overlapping_approval_policy();

                """);
            migrationBuilder.Sql(Rev869BDatabaseSafetySql.Install);
            migrationBuilder.Sql(Rev869BDatabaseLifecycleSql.Install);
            migrationBuilder.Sql(Rev869BCommandContextSql.Install);
            migrationBuilder.Sql(Rev869BControlledMutationSql.Install);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Rev869BControlledMutationSql.Remove);
            migrationBuilder.Sql(Rev869BCommandContextSql.Remove);
            migrationBuilder.Sql(Rev869BDatabaseLifecycleSql.Remove);
            migrationBuilder.Sql(Rev869BDatabaseSafetySql.Remove);
            migrationBuilder.DropForeignKey(name: "FK_vendor_qualifications_employees_VerifiedByEmployeeId", schema: "nexa", table: "vendor_qualifications");
            migrationBuilder.DropForeignKey(name: "FK_vendor_qualifications_employees_ApprovedByEmployeeId", schema: "nexa", table: "vendor_qualifications");
            migrationBuilder.DropIndex(name: "IX_vendor_qualifications_VerifiedByEmployeeId", schema: "nexa", table: "vendor_qualifications");
            migrationBuilder.DropIndex(name: "IX_vendor_qualifications_ApprovedByEmployeeId", schema: "nexa", table: "vendor_qualifications");
            migrationBuilder.DropColumn(name: "VerifiedByEmployeeId", schema: "nexa", table: "vendor_qualifications");
            migrationBuilder.DropColumn(name: "ApprovedByEmployeeId", schema: "nexa", table: "vendor_qualifications");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS nexa.rev869b_validate_parent_contract() CASCADE; DROP FUNCTION IF EXISTS nexa.rev869b_enforce_transition() CASCADE; DROP FUNCTION IF EXISTS nexa.rev869b_guard_controlled_snapshot() CASCADE; DROP FUNCTION IF EXISTS nexa.rev869b_reject_immutable_mutation() CASCADE; DROP FUNCTION IF EXISTS nexa.rev869b_reject_overlapping_approval_policy() CASCADE;");
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION nexa.rev869b_down_owned_seed_guard() RETURNS trigger LANGUAGE plpgsql AS $rev869b$
                BEGIN
                    IF OLD."CreatedBy" <> 'migration-rev869b' THEN
                        RAISE EXCEPTION 'REV869B rollback refused to delete a seed row not owned by this migration.';
                    END IF;
                    RETURN OLD;
                END $rev869b$;
                CREATE TRIGGER trg_rev869b_down_permission_owner BEFORE DELETE ON nexa.role_page_permissions FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_down_owned_seed_guard();
                CREATE TRIGGER trg_rev869b_down_page_owner BEFORE DELETE ON nexa.page_definitions FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_down_owned_seed_guard();
                """);
            migrationBuilder.Sql("""
                DELETE FROM nexa.role_page_permissions
                WHERE "Id" IN ('918d0634-ff61-c756-98f4-a17290d04110'::uuid,'062fd69d-1356-6930-ecdb-1c13ae5a01d5'::uuid,'c9ec48cd-024d-128e-697f-389458e12c97'::uuid)
                  AND "CreatedBy" = 'migration-rev869b';
                """);
            migrationBuilder.DropTable(
                name: "material_followup_handoffs",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "purchase_order_history",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "purchase_transaction_approval_history",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "purchase_transaction_approval_policies",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "purchase_transaction_status_history",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "quotation_technical_verifications",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "purchase_order_lines",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "commercial_comparison_lines",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "purchase_orders",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "vendor_quotation_lines",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "commercial_comparisons",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "request_for_quotation_lines",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "vendor_quotations",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "rfq_vendor_invitations",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "request_for_quotations",
                schema: "nexa");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS nexa.rev869b_validate_parent_contract(); DROP FUNCTION IF EXISTS nexa.rev869b_enforce_transition(); DROP FUNCTION IF EXISTS nexa.rev869b_guard_controlled_snapshot(); DROP FUNCTION IF EXISTS nexa.rev869b_reject_immutable_mutation(); DROP FUNCTION IF EXISTS nexa.rev869b_reject_overlapping_approval_policy();");

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("01a0c648-2bdc-3643-465f-d013309c37be"));


            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("20a822a5-146b-b1ab-b849-5b19b42d053b"));



            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("35794bca-a9b2-9e06-d997-a7031ebd5a24"));


            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4235c07c-e564-bbf7-e475-eda92f8f8a15"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4527d01a-d157-1d9f-fc8e-77bc2e2fdd00"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("478a8966-7033-50b4-591e-18765aa441bb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("47e11a52-579e-038e-8fd3-f4c25dd72bb5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4d6fc2ba-2e1b-bdd4-d132-c87b77102ccb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4e10adcf-6248-dceb-cb7c-5ce74abcec69"));




            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6ae6814b-2691-8ca4-9fcb-a280f5a0abaa"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7802b358-eab1-01d0-24b6-2ea8f479222a"));


            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7e2b6cda-fef5-6891-4b5a-9dda984ea76c"));


            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("822a0e7d-4b77-a6f5-bd9b-2117b6675e7e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("84b35820-207b-51a5-bf29-205365672b1d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("88fe402d-de4f-37cb-8d43-b187d534e57d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("899d4333-60f8-7e14-4ee3-52edb1baa52c"));




            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9d745645-4715-debc-7899-f8f307dea12e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9e371f64-5812-c791-9e35-41c95554f945"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a103fb11-2d03-59f1-2d58-c823ade55568"));



            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a915b601-7adc-9bdc-9f57-38f51929fd64"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a9e03f47-cc6e-9cf1-c48c-6b2a8f14b3e6"));


            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("acd3edac-34a5-b17d-13c6-91b098ce03bb"));


            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b5a0a82a-8bf9-abdb-c594-8185fa6de8d2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c80614d6-27dc-22a0-b1b9-0a9f5b1536ea"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c81d042a-8e3b-c955-e28d-688c09fa5e55"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d5caa783-5666-ecf5-aa06-6d2b302c30c7"));




            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e68927ed-7502-fd94-30dc-08fcdc435577"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e818913d-251c-5c1c-8395-3ea116a3c0b2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fd4c02e7-4bcf-cf41-bc84-0c4025efae03"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("21231666-baa1-d0fb-60c4-aff55813333f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("2be06aa9-fb95-c4b6-1f26-c27f71cd58e9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("45ee1d9e-ac42-f959-ed98-df00b9c20bb0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("e6c278a8-9f01-4a07-845a-1aba37ca0e46"));

            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_rev869b_down_permission_owner ON nexa.role_page_permissions; DROP TRIGGER IF EXISTS trg_rev869b_down_page_owner ON nexa.page_definitions; DROP FUNCTION IF EXISTS nexa.rev869b_down_owned_seed_guard();");
            migrationBuilder.DropColumn(name: "CanIssue", schema: "nexa", table: "role_page_permissions");
        }
    }
}
