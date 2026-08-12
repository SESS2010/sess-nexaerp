using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

internal static partial class Rev869BCompleteGraphSeeder
{
    private static object[] CreateTransactions(FixtureIds ids, SupportGraph s)
    {
        var q = s.Qualification;
        var qJson = JsonSerializer.Serialize(new { snapshotAt=At,qualifications=new[] { new { vendorQualificationId=q.Id,
            vendorId=s.Vendor.Id,organizationId=Organization,itemCategoryId=s.Item.CategoryId,qualificationType="APPROVED",
            qualificationVersion=2,effectiveFrom=On,effectiveTo=(DateOnly?)null,verificationStatus="Approved",
            verifiedByEmployeeId=s.Verifier,approvalStatus="Approved",approvedByEmployeeId=s.Approver,isActive=true } } });
        var rfqKey="rev869b-pg-owned:rfq";
        var rfq = new RequestForQuotation { Id=ids.Id("rfq"),OrganizationId=Organization,RfqNumber=ids.Code("RFQ"),FinancialYear="2026-27",
            SequenceNumber=1,PurchaseRequisitionId=s.Pr.Id,DeliveryWarehouseId=s.Warehouse.Id,OwnerEmployeeId=s.Actor,QuoteDueAt=At.AddDays(7),
            CurrencyCode="INR",Status=Rev869BStatuses.Draft,IdempotencyKey=rfqKey,TransitionCorrelationId=rfqKey,CreatedAt=At,CreatedBy=Login };
        var rfqLine = new RequestForQuotationLine { Id=ids.Id("rfq-line"),RequestForQuotationId=rfq.Id,PurchaseRequirementHandoffId=s.Requirement.Id,
            PurchaseRequisitionLineId=s.PrLine.Id,ItemId=s.Item.Id,LineNumber=1,PrNumberSnapshot=s.Pr.PrNumber,PrLineNumberSnapshot=1,
            ItemCodeSnapshot=s.Item.ItemCode,ItemNameSnapshot=s.Item.Name,UomSnapshot=s.Uom.Code,ApprovedQuantitySnapshot=1,
            OutstandingQuantitySnapshot=1,RfqQuantity=1,RequiredDateSnapshot=On.AddMonths(1),CreatedAt=At,CreatedBy=Login };
        var terminalRfqKey="rev869b-pg-owned:terminal-rfq";
        var terminalRfq = new RequestForQuotation { Id=ids.Id("terminal-rfq"),OrganizationId=Organization,RfqNumber=ids.Code("RFQT"),FinancialYear="2026-27",
            SequenceNumber=2,PurchaseRequisitionId=s.Pr.Id,DeliveryWarehouseId=s.Warehouse.Id,OwnerEmployeeId=s.Actor,QuoteDueAt=At.AddDays(7),
            CurrencyCode="INR",Status=Rev869BStatuses.Closed,IdempotencyKey=terminalRfqKey,TransitionCorrelationId=terminalRfqKey,
            Version=2,CreatedAt=At,CreatedBy=Login };
        var terminalRfqLine = new RequestForQuotationLine { Id=ids.Id("terminal-rfq-line"),RequestForQuotationId=terminalRfq.Id,
            PurchaseRequirementHandoffId=s.Requirement.Id,PurchaseRequisitionLineId=s.PrLine.Id,ItemId=s.Item.Id,LineNumber=1,
            PrNumberSnapshot=s.Pr.PrNumber,PrLineNumberSnapshot=1,ItemCodeSnapshot=s.Item.ItemCode,ItemNameSnapshot=s.Item.Name,
            UomSnapshot=s.Uom.Code,ApprovedQuantitySnapshot=1,OutstandingQuantitySnapshot=1,RfqQuantity=1,
            RequiredDateSnapshot=On.AddMonths(1),CreatedAt=At,CreatedBy=Login };
        var invitationKey="rev869b-pg-owned:invitation";
        var invitation = new RfqVendorInvitation { Id=ids.Id("invitation"),RequestForQuotationId=rfq.Id,VendorId=s.Vendor.Id,
            Status=Rev869BStatuses.Issued,InvitedAt=At,QuoteDueAtSnapshot=rfq.QuoteDueAt,VendorQualificationSnapshotJson=qJson,
            IdempotencyKey=invitationKey,TransitionCorrelationId=invitationKey,CreatedAt=At,CreatedBy=Login };
        var terminalInvitationKey="rev869b-pg-owned:terminal-invitation";
        var terminalInvitation = new RfqVendorInvitation { Id=ids.Id("terminal-invitation"),RequestForQuotationId=terminalRfq.Id,VendorId=s.Vendor.Id,
            Status=Rev869BStatuses.Cancelled,InvitedAt=At,QuoteDueAtSnapshot=terminalRfq.QuoteDueAt,VendorQualificationSnapshotJson=qJson,
            IdempotencyKey=terminalInvitationKey,TransitionCorrelationId=terminalInvitationKey,Version=1,CreatedAt=At,CreatedBy=Login };
        var quoteKey="rev869b-pg-owned:quotation";
        var quote = new VendorQuotation { Id=ids.Id("quotation"),OrganizationId=Organization,QuotationNumber=ids.Code("QUOTE"),FinancialYear="2026-27",
            SequenceNumber=1,RfqVendorInvitationId=invitation.Id,VendorId=s.Vendor.Id,RootQuotationId=ids.Id("quotation"),RevisionNumber=1,
            IsCurrentRevision=true,VendorQuoteReference=ids.Code("VREF"),SubmissionSource="Portal",ReceivedAt=At,AttachmentObjectKey="owned/quote.pdf",
            AttachmentSha256=new string('A',64),VendorAttestation="Attested",CurrencyCode="INR",Status=Rev869BStatuses.TechnicallyCompliant,
            SubmittedAt=At,PaymentTermsSnapshot="30 days",DeliveryTermsSnapshot="Delivered",WarrantyTermsSnapshot="12 months",
            TotalPayableValue=100,IdempotencyKey=quoteKey,TransitionCorrelationId=quoteKey,CreatedAt=At,CreatedBy=Login };
        var quoteLine = new VendorQuotationLine { Id=ids.Id("quotation-line"),VendorQuotationId=quote.Id,RequestForQuotationLineId=rfqLine.Id,
            LineNumber=1,Quantity=1,UnitRate=100,TaxableValue=100,TaxGstSettingId=s.Tax.Id,TaxRuleSnapshotJson="{}",HsnSacCode="0000",
            SupplierStateCode="TN",PlaceOfSupplyStateCode="TN",VendorRegistrationType="REGULAR",TotalPayableValue=100,
            PromisedDeliveryDate=On.AddMonths(1),CreatedAt=At,CreatedBy=Login };
        var technical = new QuotationTechnicalVerification { Id=ids.Id("technical"),VendorQuotationLineId=quoteLine.Id,VerifierEmployeeId=s.Verifier,
            ComplianceStatus=Rev869BStatuses.TechnicallyCompliant,ComplianceSnapshotJson="{}",Remarks=s.Marker,VerifiedAt=At,
            CorrelationId="rev869b-pg-owned:technical",CreatedAt=At,CreatedBy=Login };
        var comparisonKey="rev869b-pg-owned:comparison";
        var comparison = new CommercialComparison { Id=ids.Id("comparison"),OrganizationId=Organization,ComparisonNumber=ids.Code("CMP"),
            FinancialYear="2026-27",SequenceNumber=1,RequestForQuotationId=rfq.Id,RecommendedVendorQuotationId=quote.Id,SelectedVendorId=s.Vendor.Id,
            OwnerEmployeeId=s.Actor,CurrencyCode="INR",TotalPayableValue=100,ApprovalRoute=Rev869BApprovalRoutes.Manager,
            Status=Rev869BStatuses.Approved,RecommendationRemarks=s.Marker,IdempotencyKey=comparisonKey,TransitionCorrelationId=comparisonKey,
            Version=2,CreatedAt=At,CreatedBy=Login };
        var comparisonLine = new CommercialComparisonLine { Id=ids.Id("comparison-line"),CommercialComparisonId=comparison.Id,
            VendorQuotationLineId=quoteLine.Id,VendorId=s.Vendor.Id,TechnicalComplianceSnapshot=Rev869BStatuses.TechnicallyCompliant,
            CommercialSnapshotJson="{}",DeliverySnapshot="Delivered",WarrantySnapshot="12 months",PaymentTermsSnapshot="30 days",
            TotalPayableValue=100,IsRecommended=true,RecommendationReason=s.Marker,CreatedAt=At,CreatedBy=Login };
        var policy = new PurchaseTransactionApprovalPolicy { Id=ids.Id("policy"),OrganizationId=Organization,RouteCode=Rev869BApprovalRoutes.Manager,
            MinimumAmount=0,MaximumAmount=50000,ApproverRoleCode=Rev869ARoleCodes.PurchaseManager,EffectiveFrom=On,IsActive=true,CreatedAt=At,CreatedBy=Login };
        var approved = Po(ids,"po-approved","POA",Rev869BStatuses.Approved,true,comparison,s);
        var rejected = Po(ids,"po-rejected","POR",Rev869BStatuses.Rejected,false,comparison,s);
        var approvedLine = PoLine(ids,"po-line-approved",approved.Id,comparisonLine,s);
        var rejectedLine = PoLine(ids,"po-line-rejected",rejected.Id,comparisonLine,s);
        var followup = new MaterialFollowUpHandoff { Id=ids.Id("followup"),PurchaseOrderId=approved.Id,PurchaseOrderLineId=approvedLine.Id,
            HandoffNumber=ids.Code("MFU"),OrderedQuantitySnapshot=1,Status=Rev869BStatuses.PendingFollowUp,HandoffAt=At,
            CorrelationId="rev869b-pg-owned:followup",CreatedAt=At,CreatedBy=Login };
        var terminalFollowup = new MaterialFollowUpHandoff { Id=ids.Id("terminal-followup"),PurchaseOrderId=rejected.Id,PurchaseOrderLineId=rejectedLine.Id,
            HandoffNumber=ids.Code("MFUT"),OrderedQuantitySnapshot=1,Status=Rev869BStatuses.PendingFollowUp,HandoffAt=At,
            CorrelationId="rev869b-pg-owned:terminal-followup",CreatedAt=At,CreatedBy=Login };
        var status = new PurchaseTransactionStatusHistory { Id=ids.Id("status-history"),OrganizationId=Organization,EntityType="RFQ",EntityId=rfq.Id,
            DocumentNumber=rfq.RfqNumber,Action="Create",ToStatus=rfq.Status,ActorEmployeeId=s.Actor,ActorLoginId=Login,
            ActorRoleCode=Rev869ARoleCodes.PurchaseExecutive,Remarks=s.Marker,CorrelationId=rfqKey,CreatedAt=At,CreatedBy=Login };
        var approval = new PurchaseTransactionApprovalHistory { Id=ids.Id("approval-history"),CommercialComparisonId=comparison.Id,Action="Approve",
            FromStatus=Rev869BStatuses.PendingApproval,ToStatus=Rev869BStatuses.Approved,ApprovalRoute=comparison.ApprovalRoute,
            ActorEmployeeId=s.Approver,ActorLoginId="REV869B-APPROVER",ActorRoleCode=Rev869ARoleCodes.ManagingDirector,Remarks=s.Marker,
            CorrelationId=comparisonKey,Version=2,CreatedAt=At,CreatedBy="REV869B-APPROVER" };
        var poHistory = new PurchaseOrderHistory { Id=ids.Id("po-history"),PurchaseOrderId=approved.Id,Action="Approve",
            FromStatus=Rev869BStatuses.PendingApproval,ToStatus=Rev869BStatuses.Approved,RevisionNumber=1,ActorEmployeeId=s.Approver,
            ActorLoginId="REV869B-APPROVER",ActorRoleCode=Rev869ARoleCodes.ManagingDirector,Reason=s.Marker,
            CorrelationId=approved.TransitionCorrelationId,Version=2,CreatedAt=At,CreatedBy="REV869B-APPROVER" };
        var verifyHistory = QualificationHistory(ids,"qualification-verify",q,s.Verifier,"REV869B-VERIFIER","Verify",1,"VerifiedByEmployeeId");
        var approveHistory = QualificationHistory(ids,"qualification-approve",q,s.Approver,"REV869B-APPROVER","Approve",2,"ApprovedByEmployeeId");
        var sequence = new PurchaseNumberSequence { Id=ids.Id("sequence"),OrganizationId=Organization,FinancialYear="2026-27",Prefix="RFQ",LastNumber=2,CreatedAt=At,CreatedBy=s.Marker };
        return new object[] { verifyHistory,approveHistory,rfq,rfqLine,terminalRfq,terminalRfqLine,invitation,terminalInvitation,
            quote,quoteLine,technical,comparison,comparisonLine,policy,approved,rejected,approvedLine,rejectedLine,followup,
            terminalFollowup,status,approval,poHistory,sequence };
    }

    private static PurchaseOrder Po(FixtureIds ids,string id,string prefix,string status,bool current,CommercialComparison comparison,SupportGraph s)
    {
        var key="rev869b-pg-owned:"+id;
        return new PurchaseOrder { Id=ids.Id(id),OrganizationId=Organization,PoNumber=ids.Code(prefix),FinancialYear="2026-27",
            SequenceNumber=current?1:2,RootPurchaseOrderId=ids.Id(id),RevisionNumber=1,IsCurrentVersion=current,
            CommercialComparisonId=comparison.Id,VendorId=s.Vendor.Id,DeliveryWarehouseId=s.Warehouse.Id,OwnerEmployeeId=s.Actor,
            Status=status,CurrencyCode="INR",ApprovalRoute=Rev869BApprovalRoutes.Manager,TaxableValue=100,TotalPayableValue=100,
            ApprovalPolicySnapshotJson="{}",PaymentTermsSnapshot="30 days",DeliveryTermsSnapshot="Delivered",WarrantyTermsSnapshot="12 months",
            IdempotencyKey=key,TransitionCorrelationId=key,Version=2,CreatedAt=At,CreatedBy=Login };
    }

    private static PurchaseOrderLine PoLine(FixtureIds ids,string id,Guid po,CommercialComparisonLine comparisonLine,SupportGraph s) =>
        new() { Id=ids.Id(id),PurchaseOrderId=po,CommercialComparisonLineId=comparisonLine.Id,PurchaseRequisitionLineId=s.PrLine.Id,
            PurchaseRequirementHandoffId=s.Requirement.Id,ItemId=s.Item.Id,LineNumber=1,ItemCodeSnapshot=s.Item.ItemCode,
            ItemNameSnapshot=s.Item.Name,UomSnapshot=s.Uom.Code,OrderedQuantity=1,ApprovedOutstandingQuantitySnapshot=1,UnitRate=100,
            CommercialSnapshotJson="{}",TaxRuleSnapshotJson="{}",TotalPayableValue=100,CreatedAt=At,CreatedBy=Login };

    private static ControlledConfigurationHistory QualificationHistory(FixtureIds ids,string id,VendorQualification q,Guid employee,
        string login,string action,uint version,string jsonProperty) => new() { Id=ids.Id(id),OrganizationId=Organization,
        EntityType=nameof(VendorQualification),EntityId=q.Id,Action=action,
        AfterJson=JsonSerializer.Serialize(new Dictionary<string,Guid> { [jsonProperty]=employee }),
        ActorLoginId=login,ActorRoleCode=action=="Verify"?Rev869ARoleCodes.TechnicalDirector:Rev869ARoleCodes.ManagingDirector,
        Remarks="Deterministic qualification lifecycle",CorrelationId=$"REV869B|QUALIFICATION|{q.Id:N}|{version}|{action.ToUpperInvariant()}",
        Version=version,CreatedAt=At,CreatedBy=login };

    private static async Task VerifyExactOwnedGraphAsync(NexaErpDbContext db, FixtureIds ids)
    {
        var checks = new long[] {
            await db.RequestForQuotations.LongCountAsync(x=>x.OrganizationId==Organization),
            await db.RequestForQuotationLines.LongCountAsync(x=>x.RequestForQuotation!.OrganizationId==Organization),
            await db.RfqVendorInvitations.LongCountAsync(x=>x.RequestForQuotation!.OrganizationId==Organization),
            await db.VendorQuotations.LongCountAsync(x=>x.OrganizationId==Organization),
            await db.VendorQuotationLines.LongCountAsync(x=>x.VendorQuotation!.OrganizationId==Organization),
            await db.QuotationTechnicalVerifications.LongCountAsync(x=>x.VendorQuotationLine!.VendorQuotation!.OrganizationId==Organization),
            await db.CommercialComparisons.LongCountAsync(x=>x.OrganizationId==Organization),
            await db.CommercialComparisonLines.LongCountAsync(x=>x.CommercialComparison!.OrganizationId==Organization),
            await db.PurchaseTransactionApprovalHistories.LongCountAsync(x=>x.CommercialComparison!.OrganizationId==Organization),
            await db.PurchaseOrders.LongCountAsync(x=>x.OrganizationId==Organization),
            await db.PurchaseOrderLines.LongCountAsync(x=>x.PurchaseOrder!.OrganizationId==Organization),
            await db.PurchaseOrderHistories.LongCountAsync(x=>x.PurchaseOrder!.OrganizationId==Organization),
            await db.MaterialFollowUpHandoffs.LongCountAsync(x=>x.PurchaseOrder!.OrganizationId==Organization),
            await db.PurchaseTransactionStatusHistories.LongCountAsync(x=>x.OrganizationId==Organization),
            await db.PurchaseTransactionApprovalPolicies.LongCountAsync(x=>x.OrganizationId==Organization) };
        if (checks.Any(x=>x<1) || !await db.VendorQualifications.AnyAsync(x=>x.Id==ids.Id("qualification")) ||
            !await db.Uoms.AnyAsync(x=>x.Id==ids.Id("uom")) || !await db.Warehouses.AnyAsync(x=>x.Id==ids.Id("warehouse")) ||
            !await db.RackBins.AnyAsync(x=>x.Id==ids.Id("rack")) || await db.EmployeeIdentityMappings.CountAsync(x=>x.OrganizationId==Organization)!=3)
            throw new InvalidOperationException("The complete deterministic direct-test graph was not created exactly.");
    }
}
