using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Application.Employees;
using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed record MixedRunContext(DbContextOptions<NexaErpDbContext> Options,
        string RuntimeConnection, Func<string> ReadPostgresLog);
    private sealed record MixedCommand(string EmployeeCode, string Role, string Organization,
        string Operation, string Path, object Body, string Key, Guid EntityId, string Method = "POST");
    private sealed record MixedPrefix(Guid ItemId, string ItemCode, Rev869BDocumentResult? Quote,
        Rev869BDocumentResult? Po, GoodsReceiptResult? Grn);

#if CONCURRENCY_WITNESS
    [Fact]
    public Task ElevenUsersRunSupportedCommandsOnOneApiHost() =>
        RunCompletePurchaseFlow(mixedRun: async context =>
        {
            try { await RunElevenUserCommands(context); }
            catch
            {
                var directory = Path.Combine(FindRepositoryRoot(),"local-evidence","item25");
                Directory.CreateDirectory(directory);
                await File.WriteAllTextAsync(Path.Combine(directory,"eleven-user-failure-postgresql.log"),context.ReadPostgresLog());
                throw;
            }
        });

#endif
    private static async Task RunElevenUserCommands(MixedRunContext context)
    {
        // Copy only the disposable random-port fixture. Its completed three-band
        // witness remains untouched; all pending business documents below use APIs.
        await using var sourceDb = new NexaErpDbContext(context.Options);
        var source = new NpgsqlConnectionStringBuilder(sourceDb.Database.GetConnectionString());
        Assert.Equal("127.0.0.1",source.Host);
        Assert.Equal("advance_parser",source.Database);
        Assert.Equal("postgres",source.Username);
        Assert.False(source.Pooling);
        Assert.InRange(source.Port,1025,65535);
        Assert.NotEqual(5432,source.Port);
        var cloneName = "mixed_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(source.ConnectionString)
            { Database = "postgres" }.ConnectionString))
        {
            await admin.OpenAsync();
            await using (var count = new NpgsqlCommand("SELECT count(*) FROM pg_stat_activity WHERE datname='advance_parser'",admin))
                Assert.Equal(0L,(long)(await count.ExecuteScalarAsync())!);
            await using (var clone = new NpgsqlCommand($"CREATE DATABASE \"{cloneName}\" WITH TEMPLATE advance_parser OWNER nexa_erp_owner",admin))
            {
                clone.CommandTimeout = 60;
                await clone.ExecuteNonQueryAsync();
            }
            await using var grants = new NpgsqlCommand($"REVOKE CONNECT,TEMPORARY ON DATABASE \"{cloneName}\" FROM PUBLIC; GRANT CONNECT ON DATABASE \"{cloneName}\" TO nexa_erp_runtime;",admin);
            await grants.ExecuteNonQueryAsync();
        }
        var options = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(
            new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = cloneName }.ConnectionString).Options;
        var runtime = new NpgsqlConnectionStringBuilder(context.RuntimeConnection)
            { Database = cloneName, ApplicationName = "eleven-user-api", Pooling = false }.ConnectionString;
        var company = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var company2 = await Query(options,db => db.Companies.Where(x => x.Code == "SESS_PROPRIETORSHIP").Select(x => x.Id).SingleAsync());
        var codes = new[] { "SESS-12","SESS-14","SESS-01","SESS-02","SESS-05","SESS-15","SESS-35","SESS-33","SESS-25","SESS-41","SESS-16" };
        var employees = await Query(options,async db => await db.Employees.Where(x => codes.Contains(x.EmployeeCode))
            .ToDictionaryAsync(x => x.EmployeeCode,x => x.Id));
        Assert.Equal(11,employees.Count);
        await Query(options,async db =>
        {
            foreach (var code in new[] { "SESS-41","SESS-14","SESS-01" })
                if (!await db.EmployeeIdentityMappings.AnyAsync(x => x.CompanyId == company2 && x.EmployeeId == employees[code] && x.IsActive))
                {
                    var mapping = Mapping(company2,employees[code],code);
                    mapping.OrganizationId = "SESS_PROPRIETORSHIP";
                    db.EmployeeIdentityMappings.Add(mapping);
                }
            await db.SaveChangesAsync();
            return 0;
        });
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var assignments = await Query(options,async db => (await db.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
            .Where(x => (x.CompanyId == company || x.CompanyId == company2) && x.EffectiveFrom <= today
                && (!x.EffectiveTo.HasValue || x.EffectiveTo >= today)).ToListAsync())
            .GroupBy(x => x.CompanyId).ToDictionary(g => g.Key,g => (IReadOnlyDictionary<string,EffectiveRoleAssignment>)g.ToDictionary(
                x => TaxWorkflowUser.AssignmentKey(x.EmployeeId,x.Role!.Code),
                x => new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType))));
        var subjects = await Query(options,async db => (await db.EmployeeIdentityMappings.Where(x => x.IsActive
            && (x.CompanyId == company || x.CompanyId == company2)).ToListAsync())
            .ToDictionary(x => (x.CompanyId,x.EmployeeId),x => x.Subject));
        TaxWorkflowUser Actor(string code,string role,string organization = "SESS_PVT_LTD")
        {
            var id = employees[code];
            var companyId = organization == "SESS_PVT_LTD" ? company : company2;
            var key = TaxWorkflowUser.AssignmentKey(id,role);
            Assert.True(assignments[companyId].ContainsKey(key),$"Missing real assignment: {code} {role} {organization}");
            var actor = new TaxWorkflowUser(id,subjects[(companyId,id)],role,assignments[companyId]);
            actor.SetOrganization(organization);
            Assert.NotEqual(Guid.Empty,Assert.Single(actor.EffectiveRoleAssignments).AssignmentId);
            return actor;
        }
        var commands = new List<MixedCommand>();
        var measuring = false;
        var activeRequests = 0;
        var maxActiveRequests = 0;
        var observationLock = new object();
        var observations = new List<object>();
        var observedUsers = new List<TaxWorkflowUser>();
        void Observe(ICurrentUser current, bool entering)
        {
            if (!measuring) return;
            lock (observationLock)
            {
                activeRequests += entering ? 1 : -1;
                maxActiveRequests = Math.Max(maxActiveRequests,activeRequests);
                observations.Add(new { Entering = entering, At = DateTimeOffset.UtcNow,
                    current.EmployeeId,current.IdentitySubject,current.OrganizationId,current.RoleCode,
                    current.ResolvedRoleAssignmentId,current.ResolvedRoleAssignmentType });
                if (entering) observedUsers.Add((TaxWorkflowUser)current);
            }
        }
        // This routing is confined to the test host. Production OIDC is separately
        // witnessed against Keycloak; these headers never enter the application.
        await using var host = await PurchaseFlowHost.StartAsync(runtime,Actor("SESS-12","IT_MANAGER"),true,true,
            requestUser: http => Actor(http.Request.Headers["X-Witness-Employee"].ToString(),
                http.Request.Headers["X-Witness-Role"].ToString(),http.Request.Headers["X-Witness-Company"].ToString()), observeRequest: Observe);
        using var client = new HttpClient { BaseAddress = host.Client.BaseAddress, Timeout = TimeSpan.FromMinutes(3) };
        client.DefaultRequestHeaders.Authorization = new("PurchaseFlow");
        void Select(string code,string role,string organization = "SESS_PVT_LTD")
        {
            foreach (var name in new[] { "X-Witness-Employee","X-Witness-Role","X-Witness-Company" })
                client.DefaultRequestHeaders.Remove(name);
            client.DefaultRequestHeaders.Add("X-Witness-Employee",code);
            client.DefaultRequestHeaders.Add("X-Witness-Role",role);
            client.DefaultRequestHeaders.Add("X-Witness-Company",organization);
        }
        void Add(string code,string role,string operation,string path,object body,string key,Guid entityId,
            string organization = "SESS_PVT_LTD") =>
            commands.Add(new(code,role,organization,operation,path,body,key,entityId));


        // The copied trial route maps IT approvals to SESS-14, while that employee's
        // seeded operational scope is Accounts. Configure this disposable scenario
        // explicitly; do not weaken scope checks or imply this is the site's setup.
        var scopeFixture = await Query(options,async db =>
        {
            var it = await db.Departments.Where(x => x.Code == "IT").Select(x => x.Id).SingleAsync();
            var membership = await db.EmployeeCompanyAssignments.SingleAsync(x => x.CompanyId == company
                && x.EmployeeId == employees["SESS-14"] && x.IsActive && x.Status == "ACTIVE"
                && x.EffectiveFrom <= today && (!x.EffectiveTo.HasValue || x.EffectiveTo >= today));
            var assignment = await db.EmployeeDepartmentAssignments.SingleOrDefaultAsync(x => x.CompanyId == company
                && x.EmployeeCompanyAssignmentId == membership.Id && x.DepartmentId == it && x.IsActive
                && x.EffectiveFrom <= today && (!x.EffectiveTo.HasValue || x.EffectiveTo >= today));
            var introduced = assignment is null;
            if (assignment is null)
            {
                var designation = await db.Employees.Where(x => x.Id == employees["SESS-14"]).Select(x => x.DesignationId).SingleAsync();
                assignment = new SESS.NexaERP.Domain.Foundation.EmployeeDepartmentAssignment
                {
                    CompanyId = company, EmployeeCompanyAssignmentId = membership.Id, DepartmentId = it,
                    DesignationId = designation, AssignmentType = "SECONDARY", IsPrimary = false,
                    EffectiveFrom = today, Status = "ACTIVE", IsActive = true, CreatedBy = "MIXED_REFERENCE_FIXTURE"
                };
                db.EmployeeDepartmentAssignments.Add(assignment);
                await db.SaveChangesAsync();
            }
            return new { assignment.Id, IntroducedDepartmentFixture = introduced };
        });
        Select("SESS-12","IT_MANAGER");
        var scopeGrant = await Post<JsonElement>(client,"/api/v1/rev869a/configuration/operational-scopes",
            new CreateOperationalScopeRequest("SESS_PVT_LTD","SESS-14","IT","TRIAL-WH-C01",null,false,false,today,null,
                "Disposable eleven-user scenario: mapped IT approver"));
        var scopeEvidenceDirectory = Path.Combine(FindRepositoryRoot(),"local-evidence","item25");
        Directory.CreateDirectory(scopeEvidenceDirectory);
        await File.WriteAllTextAsync(Path.Combine(scopeEvidenceDirectory,"eleven-user-scope-fixture.json"),
            JsonSerializer.Serialize(new { Department = scopeFixture, GovernedScope = scopeGrant }));
        var required = today.AddDays(30);
        CreatePurchaseRequisitionRequest PrBody(string code,decimal amount) =>
            new("SESS_PVT_LTD","IT","SESS-15",required,"NORMAL","Eleven-user " + code,"TRIAL-WH-C01",
                null,null,null,null,null,[new(code,1,amount,required,"TRIAL-WH-C01",null,null,null)]);
        async Task<PurchaseRequisitionDetail> PreparePr(string itemCode,string key,decimal amount,bool md = false)
        {
            Select("SESS-15","PURCHASE_EXECUTIVE");
            var pr = await Post<PurchaseRequisitionDetail>(client,"/api/v1/purchase/requisitions",PrBody(itemCode,amount));
            pr = await Post<PurchaseRequisitionDetail>(client,$"/api/v1/purchase/requisitions/{pr.PrNumber}/submit",
                new PurchaseRequisitionActionRequest("Mixed preparation",pr.Version,key+"-submit"));
            Select("SESS-14","ACCOUNTS_MANAGER");
            pr = await Post<PurchaseRequisitionDetail>(client,$"/api/v1/purchase/requisitions/{pr.PrNumber}/verify",
                new PurchaseRequisitionActionRequest("Mixed preparation",pr.Version,key+"-verify"));
            pr = await Post<PurchaseRequisitionDetail>(client,$"/api/v1/purchase/requisitions/{pr.PrNumber}/approve",
                new PurchaseRequisitionActionRequest("Mixed preparation",pr.Version,key+"-approve"));
            Assert.Equal(md ? PurchaseRequisitionStatuses.PendingApproval : PurchaseRequisitionStatuses.StockCheckPending,pr.Status);
            return pr;
        }
        var vendorIds = await Query(options,db => db.Vendors.Where(x => x.VendorCode == "TRIAL-VEN-001" || x.VendorCode == "TRIAL-VEN-002")
            .OrderBy(x => x.VendorCode).Select(x => x.Id).ToArrayAsync());
        async Task<MixedPrefix> Prefix(string suffix,string stop)
        {
            var key = "MIX-" + suffix;
            var itemId = await Query(options,async db =>
            {
                var original = await db.Items.SingleAsync(x => x.ItemCode == "TRIAL-ITEM-001");
                var item = (Item)db.Entry(original).CurrentValues.ToObject();
                item.Id = Guid.NewGuid(); item.ItemCode = key; item.Barcode = key;
                item.Name = "Mixed workload fixture " + suffix; item.CreatedBy = "MIXED_REFERENCE_FIXTURE";
                db.Items.Add(item);
                var originalPolicy = await db.QcInspectionPolicies.SingleAsync(x => x.ItemId == original.Id && x.IsActive);
                var policy = (QcInspectionPolicy)db.Entry(originalPolicy).CurrentValues.ToObject();
                policy.Id = Guid.NewGuid(); policy.ItemId = item.Id; policy.CreatedBy = "MIXED_REFERENCE_FIXTURE";
                db.QcInspectionPolicies.Add(policy);
                await db.SaveChangesAsync();
                return item.Id;
            });
            var pr = await PreparePr(key,key,4000);
            Select("SESS-15","STORES_EXECUTIVE");
            await PostNoResult(client,$"/api/v1/purchase/requisitions/{pr.PrNumber}/stock-check",
                new StockCheckRequest("New item has no stock",pr.Version,key+"-stock",
                    [new(1,"TRIAL-WH-C01","TRIAL-C01-GEN-01")]),key+"-stock");
            var handoff = await Query(options,db => db.PurchaseRequirementHandoffs.Where(x => x.PurchaseRequisitionId == pr.Id)
                .Select(x => new { x.Id,x.HandoffQuantity }).SingleAsync());
            Assert.Equal(1m,handoff.HandoffQuantity);
            Select("SESS-15","PURCHASE_EXECUTIVE");
            var rfq = await Post<Rev869BDocumentResult>(client,"/api/v1/purchase/rfqs",
                new Rev869BCreateRfqRequest(DateTimeOffset.UtcNow.AddDays(7),"INR",false,null,key+"-rfq",[new(handoff.Id,1)]));
            var quotes = new List<Rev869BDocumentResult>();
            var lineId = await Query(options,db => db.RequestForQuotationLines.Where(x => x.RequestForQuotationId == rfq.Id).Select(x => x.Id).SingleAsync());
            for (var i = 0; i < vendorIds.Length; i++)
            {
                var version = await Query(options,db => db.RequestForQuotations.Where(x => x.Id == rfq.Id).Select(x => x.Version).SingleAsync());
                var invite = await Post<Rev869BDocumentResult>(client,$"/api/v1/purchase/rfqs/{rfq.Number}/vendors",
                    new Rev869BInviteVendorRequest(vendorIds[i],"Qualified vendor",version,key+"-invite-"+i));
                quotes.Add(await Post<Rev869BDocumentResult>(client,$"/api/v1/purchase/rfq-invitations/{invite.Id}/quotations",
                    new Rev869BSubmitQuotationRequest(key+"-Q"+i,"INR","30 days","Delivered","12 months",false,null,
                        "EMAIL_RECEIVED",DateTimeOffset.UtcNow.AddMinutes(-1),"mixed/"+key+i+".pdf",new string('A',64),
                        "Synthetic supplier evidence",0,null,key+"-quote-"+i,
                        [new(lineId,1,4000+i,0,0,0,0,0,required,"9025","33","33",VendorRegistrationType.REGULAR.ToCanonicalValue(),0)])));
            }
            if (stop == "QUOTE") return new(itemId,key,quotes[0],null,null);
            Select("SESS-05","TECHNICAL_SUPPORT_MANAGER");
            foreach (var quote in quotes)
            {
                var quoteLine = await Query(options,db => db.VendorQuotationLines.Where(x => x.VendorQuotationId == quote.Id).Select(x => x.Id).SingleAsync());
                await Post<Rev869BDocumentResult>(client,$"/api/v1/purchase/quotations/{quote.Number}/technical-verifications",
                    new Rev869BTechnicalVerificationRequest(quoteLine,true,"{\"trial\":true}","Compliant",quote.Version,key+"-tech-"+quote.Id));
            }
            Select("SESS-15","PURCHASE_MANAGER");
            var rfqVersion = await Query(options,db => db.RequestForQuotations.Where(x => x.Id == rfq.Id).Select(x => x.Version).SingleAsync());
            var comparison = await Post<Rev869BDocumentResult>(client,"/api/v1/purchase/comparisons",
                new Rev869BCreateComparisonRequest(rfq.Number,rfqVersion,key+"-cmp"));
            comparison = await Post<Rev869BDocumentResult>(client,$"/api/v1/purchase/comparisons/{comparison.Number}/recommend",
                new Rev869BRecommendComparisonRequest(quotes[0].Id,"Lowest compliant",null,comparison.Version,key+"-recommend"));
            Select("SESS-14","ACCOUNTS_MANAGER");
            comparison = await Post<Rev869BDocumentResult>(client,$"/api/v1/purchase/comparisons/{comparison.Number}/approve",
                new Rev869BApprovalActionRequest("Approve",comparison.Version,key+"-cmp-approve"));
            Select("SESS-15","PURCHASE_MANAGER");
            var po = await Post<Rev869BDocumentResult>(client,"/api/v1/purchase/purchase-orders",
                new Rev869BCreatePurchaseOrderRequest(comparison.Number,comparison.Version,key+"-po"));
            po = await Post<Rev869BDocumentResult>(client,$"/api/v1/purchase/purchase-orders/{po.Number}/submit",
                new Rev869BSubmitPurchaseOrderRequest("Submit",po.Version,key+"-po-submit"));
            Select("SESS-14","ACCOUNTS_MANAGER");
            po = await Post<Rev869BDocumentResult>(client,$"/api/v1/purchase/purchase-orders/{po.Number}/approve",
                new Rev869BPoApprovalActionRequest("Approve",po.Version,null,key+"-po-approve"));
            if (stop == "PO") return new(itemId,key,null,po,null);
            Select("SESS-15","PURCHASE_MANAGER");
            po = await Post<Rev869BDocumentResult>(client,$"/api/v1/purchase/purchase-orders/{po.Number}/issue",
                new Rev869BIssuePurchaseOrderRequest("Issue",po.Version,key+"-po-issue"));
            Select("SESS-35","STORES_EXECUTIVE");
            var poLine = await Query(options,db => db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == po.Id).Select(x => x.Id).SingleAsync());
            var gate = await Post<GateEntryResult>(client,"/api/v1/stores/gate-entries/",
                new CreateGateEntryRequest(po.Number,key+"-DC","TRIAL-VEHICLE","ROAD",DateTimeOffset.UtcNow,
                    "{\"packagesChecked\":true}",[new(poLine,1)]),key+"-gate");
            gate = await Post<GateEntryResult>(client,$"/api/v1/stores/gate-entries/{gate.Id}/finalize",
                new FinalizeGateEntryRequest(gate.Version,key+"-gate-final"));
            var grn = await Post<GoodsReceiptResult>(client,"/api/v1/stores/goods-receipts/",
                new CreateGoodsReceiptRequest(gate.GateEntryNumber,key+"-BILL",today,DateTimeOffset.UtcNow,
                    "{\"billChecked\":true}",[new(gate.Lines.Single().Id,[new(1,1,key+"-LOT",null,today.AddMonths(-1),today.AddYears(2))],[])]),key+"-grn");
            if (stop != "GRN")
                grn = await Post<GoodsReceiptResult>(client,$"/api/v1/stores/goods-receipts/{grn.Id}/finalize",
                    new FinalizeGoodsReceiptRequest(grn.Version,key+"-grn-final"));
            return new(itemId,key,null,po,grn);
        }
        async Task<FinalizeQcInspectionRequest> QcBody(GoodsReceiptResult grn)
        {
            var policy = await Query(options,db => db.QcInspectionPolicies.Where(x => x.ItemId == grn.Lines.Single().ItemId && x.IsActive).Select(x => x.Id).SingleAsync());
            var available = await Query(options,db => db.WarehouseConditionLocations.Where(x => x.CompanyId == company && x.ConditionCode == "AVAILABLE" && x.IsActive).OrderBy(x => x.Id).Select(x => x.Id).FirstAsync());
            return new(grn.Lines.Single().Lots.Single().Id,DateTimeOffset.UtcNow,1,0,0,available,[new(policy,1,5,null,"PASS",null)],[]);
        }
        var stock = await Prefix("ISSUE","QC");
        Select("SESS-33","QC_MANAGER");
        await Post<QcInspectionResult>(client,"/api/v1/qc/inspections",await QcBody(stock.Grn!),"mixed-stock-qc");
        var department = await Query(options,db => db.Departments.Where(x => x.Code == "IT").Select(x => x.Id).SingleAsync());
        var uom = await Query(options,db => db.Items.Where(x => x.Id == stock.ItemId).Select(x => x.BaseUomId).SingleAsync());
        async Task<MaterialIssueRequestView> Mir(string key)
        {
            Select("SESS-15","PURCHASE_MANAGER");
            var mir = await Post<MaterialIssueRequestView>(client,"/api/v1/stores/material-issue-requests",
                new CreateMaterialIssueRequest("FACTORY_ASSEMBLY","CONSUMABLE_OFFICE","DEPARTMENT",null,null,null,department,
                    "Mixed workload consumables",department,required,[new MaterialIssueRequestLineInput(stock.ItemId,uom,1,null,null)],key+"-create"));
            return await Post<MaterialIssueRequestView>(client,$"/api/v1/stores/material-issue-requests/{mir.Id}/submit",
                new MaterialIssueTransitionRequest(mir.Version,"Submit",key+"-submit"));
        }
        var issuedMir = await Mir("mixed-issue");
        Select("SESS-25","PRODUCTION_MANAGER");
        issuedMir = await Post<MaterialIssueRequestView>(client,$"/api/v1/stores/material-issue-requests/{issuedMir.Id}/approve",
            new MaterialIssueTransitionRequest(issuedMir.Version,"Approve","mixed-issue-approve"));
        Add("SESS-16","STORES_ASSISTANT","Issue",$"/api/v1/stores/material-issues/from-request/{issuedMir.Id}",
            new CreateMaterialIssue("mixed-issue-final",employees["SESS-05"],DateTimeOffset.UtcNow,
                [new MaterialIssueScan(issuedMir.Lines.Single().Id,stock.ItemCode,null,1)]),"mixed-issue-final",issuedMir.Id);
        foreach (var actor in new[] { ("SESS-25","PRODUCTION_MANAGER"),("SESS-41","STORES_MANAGER") })
        {
            var mir = await Mir("mixed-mir-"+actor.Item1);
            Add(actor.Item1,actor.Item2,"MIR approve",$"/api/v1/stores/material-issue-requests/{mir.Id}/approve",
                new MaterialIssueTransitionRequest(mir.Version,"Approve","mixed-mir-final-"+actor.Item1),"mixed-mir-final-"+actor.Item1,mir.Id);
        }
        Select("SESS-14","ACCOUNTS_MANAGER");
        var stockGrn = stock.Grn!;
        var bill = await Post<VendorBillView>(client,$"/api/v1/accounts/vendor-bills/from-grn/{stockGrn.Id}",
            new CreateVendorBillRequest(stockGrn.VendorBillNumber,stockGrn.VendorBillDate,
                [new(stockGrn.Lines.Single().Id,1,4000,4720)],"mixed-bill"));
        bill = await Post<VendorBillView>(client,$"/api/v1/accounts/vendor-bills/{bill.Id}/accept",
            new VendorBillDecisionRequest(bill.Version,"Accept","mixed-bill-accept"));
        Add("SESS-14","ACCOUNTS_MANAGER","Payment","/api/v1/accounts/vendor-financial-evidence/payments",
            new RecordVendorPaymentRequest(vendorIds[0],today,1,"INR","MIXED-PAYMENT","mixed/bank.pdf",[new(bill.Id,1)],"mixed-payment"),
            "mixed-payment",bill.Id);
        var grnPending = (await Prefix("GRN","GRN")).Grn!;
        Add("SESS-35","STORES_EXECUTIVE","GRN finalize",$"/api/v1/stores/goods-receipts/{grnPending.Id}/finalize",
            new FinalizeGoodsReceiptRequest(grnPending.Version,"mixed-grn-final"),"mixed-grn-final",grnPending.Id);
        var qcPending = (await Prefix("QC","QC")).Grn!;
        Add("SESS-33","QC_MANAGER","QC finalize","/api/v1/qc/inspections",await QcBody(qcPending),"mixed-qc-final",qcPending.Id);
        var poPending = (await Prefix("PO","PO")).Po!;
        Add("SESS-15","PURCHASE_MANAGER","PO issue",$"/api/v1/purchase/purchase-orders/{poPending.Number}/issue",
            new Rev869BIssuePurchaseOrderRequest("Issue",poPending.Version,"mixed-po-final"),"mixed-po-final",poPending.Id);
        var quotePending = (await Prefix("QUOTE","QUOTE")).Quote!;
        var quoteLineId = await Query(options,db => db.VendorQuotationLines.Where(x => x.VendorQuotationId == quotePending.Id).Select(x => x.Id).SingleAsync());
        Add("SESS-05","TECHNICAL_SUPPORT_MANAGER","Technical verify",$"/api/v1/purchase/quotations/{quotePending.Number}/technical-verifications",
            new Rev869BTechnicalVerificationRequest(quoteLineId,true,"{\"trial\":true}","Compliant",quotePending.Version,"mixed-tech-final"),
            "mixed-tech-final",quotePending.Id);
        var mdPr = await PreparePr(stock.ItemCode,"mixed-md",100000.01m,true);
        Add("SESS-02","MANAGING_DIRECTOR","PR final approve",$"/api/v1/purchase/requisitions/{mdPr.PrNumber}/approve",
            new PurchaseRequisitionActionRequest("Approve",mdPr.Version,"mixed-md-final"),"mixed-md-final",mdPr.Id);
        var contact = await Query(options,async db =>
        {
            var employee = await db.Employees.Include(x => x.Department).Include(x => x.Designation)
                .SingleAsync(x => x.Id == employees["SESS-12"]);
            var skill = await db.EmployeeSkills.Include(x => x.Skill).SingleAsync(x => x.EmployeeId == employee.Id);
            return new UpdateEmployeeRequest(employee.EmployeeName,employee.EmployeeType,employee.Grade,
                employee.Department!.Code,skill.Skill!.Code,employee.Designation!.Code,employee.DateOfJoining,
                "mixed-suranther@example.invalid",employee.MobileNumber,"Disposable mixed workload contact update",employee.Version);
        });
        commands.Add(new("SESS-12","IT_MANAGER","SESS_PVT_LTD","Employee contact update","/api/v1/employees/SESS-12",
            contact,"mixed-contact",employees["SESS-12"],"PUT"));

        // Controlled completed-import fixture, not an Excel upload claim. Count,
        // valuation and final authorization still use their governed APIs.
        var stage = WitnessSql[..WitnessSql.IndexOf("DO $count$",StringComparison.Ordinal)]
            .Replace("SESS_PVT_LTD","SESS_PROPRIETORSHIP",StringComparison.Ordinal)
            .Replace("https://opening.test","https://issuer.purchase-flow.test",StringComparison.Ordinal)
            + "\nRESET SESSION AUTHORIZATION;";
        await using (var staging = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(source.ConnectionString)
            { Database = cloneName }.ConnectionString))
        {
            await staging.OpenAsync();
            await using var command = new NpgsqlCommand(stage,staging);
            await command.ExecuteNonQueryAsync();
        }
        Select("SESS-41","STORES_MANAGER","SESS_PROPRIETORSHIP");
        var opening = await Post<OpeningStockView>(client,"/api/v1/stores/opening-stock/from-import",
            new CreateOpeningStockFromImportRequest(Guid.Parse("f1300000-0000-0000-0000-000000000001"),
                new DateOnly(2026,4,1),new DateOnly(2027,3,31),"Physical count","mixed-opening-count"));
        Select("SESS-14","ACCOUNTS_MANAGER","SESS_PROPRIETORSHIP");
        opening = await Post<OpeningStockView>(client,$"/api/v1/stores/opening-stock/{opening.Id}/confirm-value",
            new OpeningStockTransitionRequest(opening.Version,"Confirmed","mixed-opening-value"));
        Add("SESS-01","TECHNICAL_DIRECTOR","Opening authorize",$"/api/v1/stores/opening-stock/{opening.Id}/authorize",
            new OpeningStockTransitionRequest(opening.Version,"Authorize","mixed-opening-final"),"mixed-opening-final",opening.Id,"SESS_PROPRIETORSHIP");
        Assert.Equal(11,commands.Count);
        Assert.Equal(11,commands.Select(x => x.EmployeeCode).Distinct().Count());

        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;
        async Task<RaceHttpResult> Execute(MixedCommand command, bool synchronize = true)
        {
            using var request = new HttpRequestMessage(new HttpMethod(command.Method),command.Path) { Content = JsonContent.Create(command.Body) };
            request.Headers.Authorization = new("PurchaseFlow");
            request.Headers.Add("X-Witness-Employee",command.EmployeeCode);
            request.Headers.Add("X-Witness-Role",command.Role);
            request.Headers.Add("X-Witness-Company",command.Organization);
            request.Headers.Add("Idempotency-Key",command.Key);
            if (synchronize)
            {
                if (Interlocked.Increment(ref started) == 11) ready.SetResult();
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
            }
            var watch = Stopwatch.StartNew();
            using var response = await host.Client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            return new(response.StatusCode,watch.Elapsed.TotalSeconds,body);
        }
        var directory = Path.Combine(FindRepositoryRoot(),"local-evidence","item25");
        Directory.CreateDirectory(directory);
        // Deterministic EF-wrapped 40001 probes run BEFORE the ungated simultaneous batch.
        // Rollback leaves the same commands and keys available for that batch's successful retry.
        var boundaryFailures = new List<(string Operation, RaceHttpResult Response)>();
        await using (var admin = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(source.ConnectionString)
            { Database = cloneName }.ConnectionString))
        {
            await admin.OpenAsync();
            async Task<string> Snapshot() => await Query(options,async db => JsonSerializer.Serialize(new {
                Audits = await db.AuditLogs.CountAsync(), Batches = await db.StockPostingBatches.CountAsync(),
                Moves = await db.StockMovements.CountAsync(), Fifo = await db.FifoInventoryCostLayers.CountAsync(),
                Revisions = await db.QcInspectionRevisions.CountAsync(),
                Grn = await db.GoodsReceipts.Where(x => x.Id == grnPending.Id).Select(x => new { x.Status,x.Version }).SingleAsync(),
                Opening = await db.OpeningStocks.Where(x => x.Id == opening.Id).Select(x => new { x.Status,x.Version }).SingleAsync()
            }));
            foreach (var operation in new[] { "GRN finalize", "Opening authorize", "QC finalize" })
            {
                var command = commands.Single(x => x.Operation == operation);
                var table = operation == "QC finalize" ? "qc_inspection_revisions" : "audit_logs";
                var before = await Snapshot();
                await using (var install = new NpgsqlCommand($$"""
                    CREATE FUNCTION advance.mixed_ef_failure() RETURNS trigger LANGUAGE plpgsql AS $f$
                    BEGIN RAISE EXCEPTION USING ERRCODE='40001', MESSAGE='Witness EF save serialization failure'; END $f$;
                    CREATE TRIGGER mixed_ef_failure BEFORE INSERT ON advance.{{table}}
                    FOR EACH ROW EXECUTE FUNCTION advance.mixed_ef_failure();
                    """,admin))
                    await install.ExecuteNonQueryAsync();
                RaceHttpResult refused;
                try { refused = await Execute(command,synchronize:false); }
                finally
                {
                    await using var remove = new NpgsqlCommand($$"""
                        DROP TRIGGER mixed_ef_failure ON advance.{{table}};
                        DROP FUNCTION advance.mixed_ef_failure();
                        """,admin);
                    await remove.ExecuteNonQueryAsync();
                }
                boundaryFailures.Add((operation,refused));
                var after = await Snapshot();
                Assert.Equal(before,after);
                await using var requests = new NpgsqlCommand("SELECT count(*) FROM advance.command_requests WHERE \"IdempotencyKeySha256\"=@hash",admin);
                requests.Parameters.AddWithValue("hash",System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(command.Key)));
                Assert.Equal(0L,(long)(await requests.ExecuteScalarAsync())!);
                await File.WriteAllTextAsync(Path.Combine(directory,operation.Replace(' ','-')+"-ef-failure.json"),
                    JsonSerializer.Serialize(new { Operation=operation,Table=table,Refused=refused,Before=before,After=after,
                        RolledBackRegistrationCount=0 },new JsonSerializerOptions { WriteIndented=true }));
            }
        }
        Assert.All(boundaryFailures,failure => {
            Assert.Equal(HttpStatusCode.Conflict,failure.Response.Status);
            using var envelope = JsonDocument.Parse(failure.Response.Body);
            Assert.Equal("CONCURRENCY_CONFLICT",envelope.RootElement.GetProperty("Code").GetString());
        });
        var logStart = context.ReadPostgresLog().Length;
        measuring = true;
        RaceHttpResult[] results;
        try { results = await Task.WhenAll(commands.Select(command => Execute(command))); }
        finally { measuring = false; }
        var log = context.ReadPostgresLog()[logStart..];
        await File.WriteAllTextAsync(Path.Combine(directory,"eleven-user-postgresql.log"),log);
        await File.WriteAllTextAsync(Path.Combine(directory,"eleven-user.json"),JsonSerializer.Serialize(new
        {
            ApiBaseAddress = host.Client.BaseAddress, Database = cloneName, MaxActiveRequests = maxActiveRequests, Observations = observations,
            Scope = "One test-authenticated API host; real runtime principal, pages and scopes; no SQL gates; no DC or adjustment workflow",
            Results = commands.Select((command,index) => new { Command = command, Actor = new {
                EmployeeId = employees[command.EmployeeCode], Subject = Actor(command.EmployeeCode,command.Role,command.Organization).IdentitySubject,
                Assignment = Actor(command.EmployeeCode,command.Role,command.Organization).EffectiveRoleAssignments.Single() }, Response = results[index] })
        },new JsonSerializerOptions { WriteIndented = true }));
        Assert.DoesNotContain("40P01",log,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deadlock detected",log,StringComparison.OrdinalIgnoreCase);

        var initialResults = results.ToArray();
        var retries = new List<object>();
        await using (var rollbackDb = new NexaErpDbContext(options))
        await using (var rollbackConnection = new NpgsqlConnection(rollbackDb.Database.GetConnectionString()))
        {
            await rollbackConnection.OpenAsync();
            for (var index = 0; index < commands.Count; index++)
            {
                if (results[index].Status != HttpStatusCode.Conflict) continue;
                var command = commands[index];
                using var refusal = JsonDocument.Parse(results[index].Body);
                Assert.Equal("CONCURRENCY_CONFLICT",refusal.RootElement.GetProperty("Code").GetString());
                // A serialization refusal must roll back the whole command, not
                // leave an unreceipted registration or a partial business posting.
                await using var registrations = new NpgsqlCommand(
                    "SELECT count(*) FROM advance.command_requests WHERE \"IdempotencyKeySha256\"=@hash",rollbackConnection);
                registrations.Parameters.AddWithValue("hash",System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(command.Key)));
                Assert.Equal(0L,(long)(await registrations.ExecuteScalarAsync())!);
                switch (command.Operation)
                {
                    case "Issue":
                        Assert.False(await rollbackDb.MaterialIssues.AnyAsync(x => x.MaterialIssueRequestId == command.EntityId));
                        Assert.Equal("APPROVED",await rollbackDb.MaterialIssueRequests.Where(x => x.Id == command.EntityId).Select(x => x.Status).SingleAsync());
                        break;
                    case "GRN finalize":
                        Assert.Equal("DRAFT",await rollbackDb.GoodsReceipts.Where(x => x.Id == command.EntityId).Select(x => x.Status).SingleAsync());
                        Assert.False(await rollbackDb.StockPostingBatches.AnyAsync(x => x.GoodsReceiptId == command.EntityId));
                        break;
                    case "Opening authorize":
                        Assert.Equal("VALUED",await rollbackDb.OpeningStocks.Where(x => x.Id == command.EntityId).Select(x => x.Status).SingleAsync());
                        Assert.False(await rollbackDb.StockMovements.AnyAsync(x => x.CompanyId == company2));
                        Assert.False(await rollbackDb.FifoInventoryCostLayers.AnyAsync(x => x.CompanyId == company2));
                        break;
                    case "MIR approve":
                        Assert.Equal("SUBMITTED",await rollbackDb.MaterialIssueRequests.Where(x => x.Id == command.EntityId).Select(x => x.Status).SingleAsync());
                        Assert.False(await rollbackDb.MaterialIssueHistories.AnyAsync(x => x.MaterialIssueRequestId == command.EntityId && x.Action == "APPROVE"));
                        break;
                    case "QC finalize":
                        Assert.False(await rollbackDb.QcInspections.AnyAsync(x => x.GoodsReceiptLineId == qcPending.Lines.Single().Id));
                        break;
                    case "PO issue":
                        Assert.Equal("Approved",await rollbackDb.PurchaseOrders.Where(x => x.Id == command.EntityId).Select(x => x.Status).SingleAsync());
                        break;
                    case "PR final approve":
                        Assert.Equal(PurchaseRequisitionStatuses.PendingApproval,await rollbackDb.PurchaseRequisitions.Where(x => x.Id == command.EntityId).Select(x => x.Status).SingleAsync());
                        break;
                    case "Technical verify":
                        Assert.False(await rollbackDb.QuotationTechnicalVerifications.AnyAsync(x => x.VendorQuotationLine!.VendorQuotationId == command.EntityId));
                        break;
                    case "Payment":
                        await using (var allocations = new NpgsqlCommand("SELECT count(*) FROM advance.vendor_payment_allocations WHERE \"VendorBillId\"=@bill",rollbackConnection))
                        {
                            allocations.Parameters.AddWithValue("bill",command.EntityId);
                            Assert.Equal(0L,(long)(await allocations.ExecuteScalarAsync())!);
                        }
                        break;
                    case "Employee contact update":
                        Assert.Equal(contact.Version,await rollbackDb.Employees.Where(x => x.Id == command.EntityId).Select(x => x.Version).SingleAsync());
                        break;
                    default: throw new InvalidOperationException("No rollback assertion for " + command.Operation);
                }
                var retry = await Execute(command);
                retries.Add(new { command.EmployeeCode,command.Operation, RolledBackRegistrationCount = 0, Retry = retry });
                results[index] = retry;
            }
        }
        await File.WriteAllTextAsync(Path.Combine(directory,"eleven-user.json"),JsonSerializer.Serialize(new
        {
            ApiBaseAddress = host.Client.BaseAddress, Database = cloneName, MaxActiveRequests = maxActiveRequests, Observations = observations,
            Scope = "Warm test-authenticated API; ordinary runtime, real pages and configured scopes; no SQL gates; DC/adjustment absent. Retries follow the initial simultaneous batch.",
            Results = commands.Select((command,index) => new { Command = command, Response = initialResults[index], Final = results[index] }),
            Retries = retries
        },new JsonSerializerOptions { WriteIndented = true }));
        var completeLog = context.ReadPostgresLog()[logStart..];
        await File.WriteAllTextAsync(Path.Combine(directory,"eleven-user-postgresql.log"),completeLog);
        Assert.DoesNotContain("40P01",completeLog,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deadlock detected",completeLog,StringComparison.OrdinalIgnoreCase);
        foreach (var (command,result) in commands.Zip(results))
            Assert.True((int)result.Status is >= 200 and < 300,$"{command.EmployeeCode} {command.Operation}: {result.Status} {result.Body}");
        Assert.Equal(0,activeRequests);
        Assert.Equal(11,observedUsers.Count);
        Assert.Equal(11,observedUsers.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.True(maxActiveRequests >= 2,$"Only {maxActiveRequests} requests overlapped.");
        Assert.Equal(11,observedUsers.Select(x => x.EmployeeId).Distinct().Count());
        await AssertPrEvidence(options,mdPr.Id,"Approve",5,5);
        await AssertApprovalActors(options,"PR",mdPr.Id,2,employees["SESS-14"],employees["SESS-02"]);
        await AssertTechnicalEvidence(options,quotePending.Id,employees["SESS-05"]);
        await AssertPoEvidence(options,poPending.Id,"IssuePO");
        Assert.Equal("POSTED",await Query(options,db => db.OpeningStocks.Where(x => x.Id == opening.Id).Select(x => x.Status).SingleAsync()));
        Assert.Equal(10m,await Query(options,db => db.StockMovements.Where(x => x.CompanyId == company2).SumAsync(x => x.QuantityIn-x.QuantityOut)));
        Assert.Equal(1,await Query(options,db => db.MaterialIssues.CountAsync(x => x.MaterialIssueRequestId == issuedMir.Id)));
        await using var state = new NexaErpDbContext(options);
        var issue = await state.MaterialIssues.SingleAsync(x => x.MaterialIssueRequestId == issuedMir.Id);
        Assert.Equal(employees["SESS-16"],issue.IssuedByEmployeeId);
        Assert.Equal("STORES_ASSISTANT",issue.ActorRoleCode);
        Assert.Equal(Actor("SESS-16","STORES_ASSISTANT").EffectiveRoleAssignments.Single().AssignmentId,issue.ResolvedRoleAssignmentId);
        var issueLine = await state.MaterialIssueLines.SingleAsync(x => x.MaterialIssueId == issue.Id);
        Assert.Equal(1m,issueLine.QuantityBase);
        var consumption = await state.FifoCostConsumptions.SingleAsync(x => x.MaterialIssueLineId == issueLine.Id);
        Assert.Equal(1m,consumption.Quantity);
        Assert.Equal(4720m,consumption.ConsumedValue);
        var issueMoves = await state.StockMovements.Where(x => x.StockPostingBatchId == issue.StockPostingBatchId).ToListAsync();
        Assert.Equal(2,issueMoves.Count);
        Assert.Equal(1m,issueMoves.Sum(x => x.QuantityIn));
        Assert.Equal(1m,issueMoves.Sum(x => x.QuantityOut));
        foreach (var command in commands.Where(x => x.Operation == "MIR approve"))
        {
            var mir = await state.MaterialIssueRequests.SingleAsync(x => x.Id == command.EntityId);
            Assert.Equal("APPROVED",mir.Status);
            Assert.Equal(2u,mir.Version);
            Assert.Equal(employees[command.EmployeeCode],mir.ApprovedByEmployeeId);
            var history = await state.MaterialIssueHistories.SingleAsync(x => x.MaterialIssueRequestId == mir.Id && x.Action == "APPROVE");
            Assert.Equal(employees[command.EmployeeCode],history.ActorEmployeeId);
            Assert.Equal(command.Role,history.ActorRoleCode);
            Assert.Equal(Actor(command.EmployeeCode,command.Role).EffectiveRoleAssignments.Single().AssignmentId,history.ResolvedRoleAssignmentId);
        }
        var finalizedGrn = await state.GoodsReceipts.SingleAsync(x => x.Id == grnPending.Id);
        Assert.Equal("FINALIZED",finalizedGrn.Status);
        Assert.Equal(grnPending.Version+1,finalizedGrn.Version);
        Assert.Equal(employees["SESS-35"],finalizedGrn.FinalizedByEmployeeId);
        Assert.Equal(1,await state.StockPostingBatches.CountAsync(x => x.GoodsReceiptId == grnPending.Id && x.PostingKind == "GRN_CUSTODY"));
        var qcLotId = qcPending.Lines.Single().Lots.Single().Id;
        var inspection = await state.QcInspections.SingleAsync(x => x.GoodsReceiptLineLotAllocationId == qcLotId);
        var revision = await state.QcInspectionRevisions.SingleAsync(x => x.QcInspectionId == inspection.Id);
        Assert.Equal(1,revision.RevisionNumber);
        Assert.Equal(employees["SESS-33"],revision.FinalizedByEmployeeId);
        Assert.Equal(1m,revision.AcceptedQuantity);
        Assert.Equal(0m,revision.RejectedQuantity);
        var postedOpening = await state.OpeningStocks.SingleAsync(x => x.Id == opening.Id);
        Assert.Equal(2u,postedOpening.Version);
        Assert.Equal(employees["SESS-41"],postedOpening.CountedByEmployeeId);
        Assert.Equal(employees["SESS-14"],postedOpening.ValuedByEmployeeId);
        Assert.Equal(employees["SESS-01"],postedOpening.AuthorizedByEmployeeId);
        Assert.Equal(3,await state.OpeningStockEvents.CountAsync(x => x.CompanyId == company2));
        var newItemIds = await state.Items.Where(x => x.ItemCode.StartsWith("MIX-")).Select(x => x.Id).ToArrayAsync();
        var balances = await state.StockMovements.Where(x => newItemIds.Contains(x.ItemId))
            .GroupBy(x => new { x.CompanyId,x.ItemId,x.OwnershipAccountId,x.CustodyAssignmentId,x.ConditionCode,
                x.InventoryProvenanceLayerId,x.InventoryLotId,x.InventorySerialId,x.WarehouseConditionLocationId })
            .Select(g => new { g.Key, Quantity = g.Sum(x => x.QuantityIn-x.QuantityOut) }).ToListAsync();
        Assert.All(balances,x => Assert.True(x.Quantity >= 0,JsonSerializer.Serialize(x)));
        var fifo = await state.FifoInventoryCostLayers.Where(x => newItemIds.Contains(x.ItemId))
            .Select(x => new { x.Id,x.ItemId,x.UnitCost,Remaining = x.QuantityReceived -
                (state.FifoCostConsumptions.Where(c => c.FifoInventoryCostLayerId == x.Id).Sum(c => (decimal?)c.Quantity) ?? 0m) }).ToListAsync();
        Assert.All(fifo,x => Assert.True(x.Remaining >= 0));
        Assert.Equal(0m,Assert.Single(fifo,x => x.ItemId == stock.ItemId).Remaining);

        async Task AssertEmployeeContactRace(UpdateEmployeeRequest current)
        {
            var beforeHistory = await Query(options,db => db.EmployeeApprovalHistories.CountAsync(x => x.EmployeeId == employees["SESS-12"] && x.Action == "Update"));
            await using var gate = new NpgsqlConnection(sourceDb.Database.GetConnectionString() is null ? throw new InvalidOperationException() :
                new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = cloneName }.ConnectionString);
            await gate.OpenAsync();
            await using var gateTransaction = await gate.BeginTransactionAsync();
            await using (var hold = new NpgsqlCommand("SELECT \"Id\" FROM advance.employees WHERE \"Id\"=@employee FOR NO KEY UPDATE",gate,gateTransaction))
            {
                hold.Parameters.AddWithValue("employee",employees["SESS-12"]);
                await hold.ExecuteScalarAsync();
            }
            var firstCommand = new MixedCommand("SESS-12","IT_MANAGER","SESS_PVT_LTD","Contact race",
                "/api/v1/employees/SESS-12",current with { OfficialEmail = "first-editor@example.invalid",Reason = "First concurrent contact edit" },"contact-race-first",employees["SESS-12"],"PUT");
            var secondCommand = firstCommand with { EmployeeCode = "SESS-01",Role = "TECHNICAL_DIRECTOR",
                Body = current with { OfficialEmail = "second-editor@example.invalid",Reason = "Second concurrent contact edit" },Key = "contact-race-second" };
            var raceLogStart = context.ReadPostgresLog().Length;
            var first = Execute(firstCommand);
            var second = Execute(secondCommand);
            var blocked = false;
            try
            {
                var deadline = Stopwatch.StartNew();
                while (deadline.Elapsed < TimeSpan.FromSeconds(20))
                {
                    await using (var refresh = new NpgsqlCommand("SELECT pg_stat_clear_snapshot()",gate,gateTransaction))
                        await refresh.ExecuteNonQueryAsync();
                    await using var observe = new NpgsqlCommand(
                        "SELECT count(*) FROM pg_stat_activity WHERE application_name='eleven-user-api' AND state='active' AND wait_event_type='Lock' AND cardinality(pg_blocking_pids(pid))>0",gate,gateTransaction);
                    if ((long)(await observe.ExecuteScalarAsync())! >= 2) { blocked = true; break; }
                    await Task.Delay(25);
                }
            }
            finally { await gateTransaction.CommitAsync(); }
            var calls = await Task.WhenAll(first,second);
            var after = await Query(options,db => db.Employees.AsNoTracking().Where(x => x.Id == employees["SESS-12"])
                .Select(x => new { x.Version,x.OfficialEmail }).SingleAsync());
            var afterHistory = await Query(options,db => db.EmployeeApprovalHistories.CountAsync(x => x.EmployeeId == employees["SESS-12"] && x.Action == "Update"));
            var raceLog = context.ReadPostgresLog()[raceLogStart..];
            await File.WriteAllTextAsync(Path.Combine(directory,"employee-contact-race.json"),JsonSerializer.Serialize(new
            {
                BeforeVersion = current.Version,Blocked = blocked,Calls = calls,After = after,
                BeforeHistory = beforeHistory,AfterHistory = afterHistory,
                TimingNote = "Controlled employee-row lock wait is included; separate from eleven-user timings."
            },new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(directory,"employee-contact-race-postgresql.log"),raceLog);
            Assert.True(blocked,"Both editors must reach a blocked database write.");
            Assert.DoesNotContain("40P01",raceLog,StringComparison.OrdinalIgnoreCase);
            Assert.Single(calls,x => x.Status == HttpStatusCode.OK);
            Assert.Single(calls,x => x.Status == HttpStatusCode.Conflict);
            Assert.Equal(current.Version+1,after.Version);
            Assert.Equal(beforeHistory+1,afterHistory);
            var winner = calls[0].Status == HttpStatusCode.OK ? firstCommand : secondCommand;
            Assert.Equal(((UpdateEmployeeRequest)winner.Body).OfficialEmail,after.OfficialEmail);
        }

        async Task AssertEmployeeTransitions()
        {
            var employeeId = employees["SESS-12"];
            var before = await Query(options,db => db.Employees.AsNoTracking().SingleAsync(x => x.Id == employeeId));
            var approvalCount = await Query(options,db => db.EmployeeApprovalHistories.CountAsync(x => x.EmployeeId == employeeId));
            var statusCount = await Query(options,db => db.EmployeeStatusHistories.CountAsync(x => x.EmployeeId == employeeId));
            var command = new MixedCommand("SESS-01","TECHNICAL_DIRECTOR","SESS_PVT_LTD","Employee approval",
                "/api/v1/employees/SESS-12/approve",new EmployeeApprovalRequest("Version witness",before.Version),
                "employee-approve-version",employeeId);
            var approved = await Execute(command);
            Assert.Equal(HttpStatusCode.OK,approved.Status);
            using (var body = JsonDocument.Parse(approved.Body))
                Assert.Equal(before.Version+1,body.RootElement.GetProperty("Version").GetUInt32());
            var staleApproval = await Execute(command with { Path = "/api/v1/employees/SESS-12/reject",
                Body = new EmployeeApprovalRequest("Stale approval witness",before.Version),Key = "employee-stale-reject" });
            Assert.Equal(HttpStatusCode.Conflict,staleApproval.Status);
            var afterApproval = await Query(options,db => db.Employees.AsNoTracking().SingleAsync(x => x.Id == employeeId));
            Assert.Equal("Approved",afterApproval.ApprovalStatus);
            Assert.Equal(before.Version+1,afterApproval.Version);
            Assert.Equal(approvalCount+1,await Query(options,db => db.EmployeeApprovalHistories.CountAsync(x => x.EmployeeId == employeeId)));
            var disabled = await Execute(command with { Path = "/api/v1/employees/SESS-12/deactivate-login",
                Body = new LoginStatusRequest("Login version witness",afterApproval.Version),Key = "employee-disable-version" });
            Assert.Equal(HttpStatusCode.OK,disabled.Status);
            using (var body = JsonDocument.Parse(disabled.Body))
                Assert.Equal(afterApproval.Version+1,body.RootElement.GetProperty("Version").GetUInt32());
            var staleLogin = await Execute(command with { Path = "/api/v1/employees/SESS-12/activate-login",
                Body = new LoginStatusRequest("Stale login witness",afterApproval.Version),Key = "employee-stale-enable" });
            Assert.Equal(HttpStatusCode.Conflict,staleLogin.Status);
            var afterLogin = await Query(options,db => db.Employees.AsNoTracking().SingleAsync(x => x.Id == employeeId));
            Assert.False(afterLogin.LoginEnabled);
            Assert.Equal("Inactive",afterLogin.Status);
            Assert.Equal(afterApproval.Version+1,afterLogin.Version);
            Assert.Equal(statusCount+1,await Query(options,db => db.EmployeeStatusHistories.CountAsync(x => x.EmployeeId == employeeId)));
            var enabled = await Execute(command with { Path = "/api/v1/employees/SESS-12/activate-login",
                Body = new LoginStatusRequest("Restore login with current version",afterLogin.Version),Key = "employee-enable-version" });
            Assert.Equal(HttpStatusCode.OK,enabled.Status);
            Assert.Equal(afterLogin.Version+1,await Query(options,db => db.Employees.Where(x => x.Id == employeeId).Select(x => x.Version).SingleAsync()));
            await File.WriteAllTextAsync(Path.Combine(directory,"employee-status-version.json"),JsonSerializer.Serialize(new {
                BeforeVersion = before.Version,Approved = approved,StaleApproval = staleApproval,
                Disabled = disabled,StaleLogin = staleLogin,Enabled = enabled
            },new JsonSerializerOptions { WriteIndented = true }));
        }

        var changedEmployee = await state.Employees.SingleAsync(x => x.Id == employees["SESS-12"]);
        await AssertEmployeeContactRace(contact with { Version = changedEmployee.Version });
        await AssertEmployeeTransitions();
        Assert.Equal(contact.Version+1,changedEmployee.Version);
        Assert.Equal(contact.OfficialEmail,changedEmployee.OfficialEmail);
        var contactAudit = await state.AuditLogs.Where(x => x.EntityId == changedEmployee.Id.ToString() && x.Action == "Update").OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).FirstAsync();
        Assert.Equal(Actor("SESS-12","IT_MANAGER").IdentitySubject,contactAudit.UserLoginId);
        var paymentIndex = commands.FindIndex(x => x.Operation == "Payment");
        var payment = JsonSerializer.Deserialize<VendorPaymentView>(results[paymentIndex].Body)!;
        Assert.Equal(1m,payment.Amount);
        Assert.Equal(bill.Id,Assert.Single(payment.Allocations).VendorBillId);
        Assert.Equal(1m,payment.Allocations.Single().Amount);
        await using var financial = new NpgsqlConnection(state.Database.GetConnectionString());
        await financial.OpenAsync();
        await using var paid = new NpgsqlCommand("SELECT sum(\"Amount\") FROM advance.vendor_payment_allocations WHERE \"VendorBillId\"=@bill",financial);
        paid.Parameters.AddWithValue("bill",bill.Id);
        Assert.Equal(1m,(decimal)(await paid.ExecuteScalarAsync())!);

        async Task AssertPaymentSerializationFailure(string boundary, decimal paidBefore)
        {
            var original = commands.Single(x => x.Operation == "Payment");
            var body = ((RecordVendorPaymentRequest)original.Body) with {
                PaymentReference = $"{boundary}-FAILURE-WITNESS",IdempotencyKey = $"mixed-payment-{boundary}-failure"
            };
            var command = original with { Body = body,Key = body.IdempotencyKey };
            var auditBefore = await Query(options,db => db.AuditLogs.CountAsync(x => x.Action == "VendorPayment.Record"));
            var startLog = context.ReadPostgresLog().Length;
            await using var admin = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(source.ConnectionString)
                { Database = cloneName }.ConnectionString);
            await admin.OpenAsync();
            // Reproduce 40001 both inside the controlled payment function and later
            // at receipt insertion, after the payment and audit have written.
            var failureTable = boundary == "function" ? "vendor_payments" : "command_receipts";
            await using (var install = new NpgsqlCommand($$"""
                CREATE FUNCTION advance.mixed_receipt_failure() RETURNS trigger LANGUAGE plpgsql AS $f$
                BEGIN RAISE EXCEPTION USING ERRCODE='40001', MESSAGE='Witness serialization failure at payment {{boundary}}'; END $f$;
                CREATE TRIGGER mixed_receipt_failure BEFORE INSERT ON advance.{{failureTable}}
                FOR EACH ROW EXECUTE FUNCTION advance.mixed_receipt_failure();
                """,admin))
                await install.ExecuteNonQueryAsync();
            RaceHttpResult refused;
            try { refused = await Execute(command); }
            finally
            {
                await using var remove = new NpgsqlCommand($$"""
                    DROP TRIGGER mixed_receipt_failure ON advance.{{failureTable}};
                    DROP FUNCTION advance.mixed_receipt_failure();
                    """,admin);
                await remove.ExecuteNonQueryAsync();
            }
            Assert.Equal(HttpStatusCode.Conflict,refused.Status);
            using (var envelope = JsonDocument.Parse(refused.Body))
                Assert.Equal("CONCURRENCY_CONFLICT",envelope.RootElement.GetProperty("Code").GetString());
            await using (var check = new NpgsqlCommand("""
                SELECT (SELECT count(*) FROM advance.command_requests WHERE "IdempotencyKeySha256"=@hash),
                       (SELECT count(*) FROM advance.vendor_payments WHERE "PaymentReference"=@reference),
                       (SELECT sum("Amount") FROM advance.vendor_payment_allocations WHERE "VendorBillId"=@bill)
                """,admin))
            {
                check.Parameters.AddWithValue("hash",System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(command.Key)));
                check.Parameters.AddWithValue("reference",body.PaymentReference);
                check.Parameters.AddWithValue("bill",bill.Id);
                await using var rows = await check.ExecuteReaderAsync();
                Assert.True(await rows.ReadAsync());
                Assert.Equal(0L,rows.GetInt64(0));
                Assert.Equal(0L,rows.GetInt64(1));
                Assert.Equal(paidBefore,rows.GetDecimal(2));
            }
            Assert.Equal(auditBefore,await Query(options,db => db.AuditLogs.CountAsync(x => x.Action == "VendorPayment.Record")));
            var retry = await Execute(command);
            Assert.Equal(HttpStatusCode.Created,retry.Status);
            var retried = JsonSerializer.Deserialize<VendorPaymentView>(retry.Body)!;
            Assert.False(retried.Replayed);
            var replay = await Execute(command);
            Assert.Equal(HttpStatusCode.Created,replay.Status);
            var replayed = JsonSerializer.Deserialize<VendorPaymentView>(replay.Body)!;
            Assert.True(replayed.Replayed);
            Assert.Equal(retried.Id,replayed.Id);
            await using (var total = new NpgsqlCommand("SELECT sum(\"Amount\") FROM advance.vendor_payment_allocations WHERE \"VendorBillId\"=@bill",admin))
            {
                total.Parameters.AddWithValue("bill",bill.Id);
                Assert.Equal(paidBefore+1m,(decimal)(await total.ExecuteScalarAsync())!);
            }
            Assert.Equal(auditBefore+1,await Query(options,db => db.AuditLogs.CountAsync(x => x.Action == "VendorPayment.Record")));
            var log = context.ReadPostgresLog()[startLog..];
            Assert.Contains("40001",log,StringComparison.Ordinal);
            Assert.DoesNotContain("40P01",log,StringComparison.OrdinalIgnoreCase);
            await File.WriteAllTextAsync(Path.Combine(directory,$"payment-{boundary}-failure.json"),JsonSerializer.Serialize(new {
                Boundary = boundary,Refused = refused,Retry = retry,Replay = replay,TotalPaidAfterRollback = paidBefore,TotalPaidAfterRetryAndReplay = paidBefore+1m,
                Note = "Deterministic test-only trigger reproduces SQLSTATE 40001 at the recorded boundary; separate from the eleven-user workload."
            },new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(directory,$"payment-{boundary}-failure-postgresql.log"),log);
        }
        await AssertPaymentSerializationFailure("function",1m);
        await AssertPaymentSerializationFailure("receipt",2m);

        await File.WriteAllTextAsync(Path.Combine(directory,"eleven-user-state.json"),JsonSerializer.Serialize(new {
            IssueId = issue.Id, IssueActor = issue.IssuedByEmployeeId, IssueAssignment = issue.ResolvedRoleAssignmentId,
            Consumption = new { consumption.Quantity,consumption.ConsumedValue },
            GrnVersion = finalizedGrn.Version, QcRevision = revision.RevisionNumber,
            OpeningVersion = postedOpening.Version, Payment = payment.Id, Balances = balances, Fifo = fifo
        },new JsonSerializerOptions { WriteIndented = true }));

    }
}
