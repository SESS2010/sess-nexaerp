using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task R1_vendor_owner_submit_upload_are_PM_only_in_both_companies_and_rollback_is_guarded()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        const string previous = "20261003113000_MirRollbackJournalProtection";
        const string target = "20261006150000_R1VendorOwnerWorkflowGrants";
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("vendor-owner-baseline.sql", migrator.GenerateScript("0", previous));
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql(server.ConnectionString).Options);
        const string snapshot = "SELECT jsonb_agg(to_jsonb(g) ORDER BY g.\"Id\")::text AS \"Value\" FROM advance.role_page_permissions g";
        var before = await db.Database.SqlQueryRaw<string>(snapshot).SingleAsync();
        server.Execute("vendor-owner-before.sql", "CREATE TABLE public.vendor_owner_before AS SELECT \"Id\",to_jsonb(g) body FROM advance.role_page_permissions g;");
        var service = new EfPagePermissionService(db);
        var roles = await db.Roles.Select(x => x.Code).ToListAsync();
        var beforeActions = new Dictionary<string, (bool Submit, bool Upload)>();
        foreach (var role in roles)
            beforeActions[role] = (await service.HasPermissionAsync([role], "masters.vendors", "submit", default),
                await service.HasPermissionAsync([role], "masters.vendors", "upload-attachment", default));
        Assert.Equal((false, false), beforeActions["PURCHASE_MANAGER"]);
        Assert.Equal((false, false), beforeActions["PURCHASE_EXECUTIVE"]);
        var up = migrator.GenerateScript(previous, target);
        var down = migrator.GenerateScript(target, previous);
        server.Execute("vendor-owner-up.sql", up);
        foreach (var role in roles)
        {
            var actual = (await service.HasPermissionAsync([role], "masters.vendors", "submit", default),
                await service.HasPermissionAsync([role], "masters.vendors", "upload-attachment", default));
            Assert.Equal(role == "PURCHASE_MANAGER" ? (true, true) : beforeActions[role], actual);
        }
        foreach (var company in new[] { "SESS_PVT_LTD", "SESS_PROPRIETORSHIP" })
        {
            var employee = await db.Employees.Where(x => x.EmployeeCode == "SESS-15").Select(x => x.Id).SingleAsync();
            var companyId = await db.Companies.Where(x => x.Code == company).Select(x => x.Id).SingleAsync();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            // PROP has no seeded final-approver policy: configure the same independent MD prerequisite in this disposable fixture only.
            if (!await db.OrganizationPolicies.AnyAsync(x => x.OrganizationId == company && x.PolicyCode == Rev869APolicyCodes.VendorFinalApprover))
            {
                db.OrganizationPolicies.Add(new OrganizationPolicy
                {
                    CompanyId = companyId, OrganizationId = company, PolicyCode = Rev869APolicyCodes.VendorFinalApprover,
                    PolicyValue = "MANAGING_DIRECTOR", EffectiveFrom = today, CreatedBy = "vendor-owner-disposable-fixture"
                });
                await db.SaveChangesAsync();
            }
            var assignments = await db.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
                .Where(x => x.EmployeeId == employee && x.CompanyId == companyId && x.EffectiveFrom <= today &&
                    (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= today) &&
                    (x.ApprovalStatus == "Approved" || x.ApprovalStatus == "SeedApproved"))
                .Select(x => new EffectiveRoleAssignment(x.Id, x.Role!.Code, x.AssignmentType)).ToListAsync();
            Assert.Contains(assignments, x => x.RoleCode == "PURCHASE_MANAGER" && x.AssignmentType == "FULL");
            var permissions = await service.ResolveEffectivePermissionsAsync(assignments, company, employee, default);
            Assert.Contains("masters.vendors:submit", permissions);
            Assert.Contains("masters.vendors:upload-attachment", permissions);
            Assert.DoesNotContain("masters.vendors:verify", permissions);
            Assert.DoesNotContain("masters.vendors:approve", permissions);
            var known = (await db.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
                .Where(x => x.CompanyId == companyId && x.EffectiveFrom <= today &&
                    (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= today) &&
                    (x.ApprovalStatus == "Approved" || x.ApprovalStatus == "SeedApproved")).ToListAsync())
                .ToDictionary(x => TaxWorkflowUser.AssignmentKey(x.EmployeeId, x.Role!.Code),
                    x => new EffectiveRoleAssignment(x.Id, x.Role!.Code, x.AssignmentType));
            var actorIds = new Dictionary<string, Guid>();
            foreach (var code in new[] { "SESS-15", "SESS-14", "SESS-02" })
            {
                var person = await db.Employees.SingleAsync(x => x.EmployeeCode == code);
                person.LoginEnabled = true;
                actorIds[code] = person.Id;
                var identity = Mapping(companyId, person.Id, "vendor-owner-" + company + "-" + code);
                identity.OrganizationId = company;
                db.EmployeeIdentityMappings.Add(identity);
            }
            await db.SaveChangesAsync();
            var actor = new TaxWorkflowUser(employee, "vendor-owner-" + company + "-SESS-15", "PURCHASE_MANAGER", known);
            actor.SetOrganization(company);
            await using var host = await PurchaseFlowHost.StartAsync(server.ConnectionString, actor, useRealPagePermissions: true);
            var client = host.Client;
            using var form = new MultipartFormDataContent();
            var pdf = new ByteArrayContent(SupplierInvoiceFixturePdf("VENDOR OWNER GST CERTIFICATE"));
            pdf.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            form.Add(pdf, "file", "vendor-owner-certificate.pdf");
            form.Add(new StringContent("GST_CERTIFICATE"), "kind");
            using var upload = await client.PostAsync("/api/v1/masters/vendors/attachments", form);
            var uploadBody = await upload.Content.ReadAsStringAsync();
            Assert.True(upload.StatusCode == HttpStatusCode.Created, uploadBody);
            var attachmentId = JsonDocument.Parse(uploadBody).RootElement.GetProperty("Id").GetGuid();
            var vendorCode = company == "SESS_PVT_LTD" ? "OWNER-UAT-PVT" : "OWNER-UAT-PROP";
            var request = new UpsertVendorRequest(vendorCode, "Owner witness " + company, null, "MANUFACTURER", null, null,
                false, null, null, null, null, null, null, "Tamil Nadu", "33", "India", null, null, null, null, null, null,
                JsonSerializer.Serialize(new { gstCertificate = new { id = attachmentId } }), null);
            var vendor = await Post<JsonElement>(client, "/api/v1/masters/vendors", request);
            await Post<JsonElement>(client, "/api/v1/masters/vendors/" + vendorCode + "/submit",
                new MasterActionRequest("PRIYA submits imported-master workflow witness", vendor.GetProperty("Version").GetUInt32()));
            var submitted = await Get<JsonElement>(client, "/api/v1/masters/vendors/" + vendorCode);
            Assert.Equal("Pending Approval", submitted.GetProperty("ApprovalStatus").GetString());
            using (var denied = await client.PostAsJsonAsync("/api/v1/masters/vendors/" + vendorCode + "/verify-commercial",
                new MasterActionRequest("Purchase must not commercially verify", submitted.GetProperty("Version").GetUInt32())))
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            using (var denied = await client.PostAsJsonAsync("/api/v1/masters/vendors/" + vendorCode + "/approve",
                new MasterActionRequest("Purchase must not approve", submitted.GetProperty("Version").GetUInt32())))
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            actor.Set(actorIds["SESS-02"], "vendor-owner-" + company + "-SESS-02", "MANAGING_DIRECTOR");
            using (var premature = await client.PostAsJsonAsync("/api/v1/masters/vendors/" + vendorCode + "/approve",
                new MasterActionRequest("No approval before Accounts verification", submitted.GetProperty("Version").GetUInt32())))
                Assert.Equal(HttpStatusCode.Conflict, premature.StatusCode);
            actor.Set(actorIds["SESS-14"], "vendor-owner-" + company + "-SESS-14", "ACCOUNTS_MANAGER");
            await Post<JsonElement>(client, "/api/v1/masters/vendors/" + vendorCode + "/verify-commercial",
                new MasterActionRequest("ALFATHIMA commercial verification", submitted.GetProperty("Version").GetUInt32()));
            var verified = await Get<JsonElement>(client, "/api/v1/masters/vendors/" + vendorCode);
            actor.Set(actorIds["SESS-02"], "vendor-owner-" + company + "-SESS-02", "MANAGING_DIRECTOR");
            await Post<JsonElement>(client, "/api/v1/masters/vendors/" + vendorCode + "/approve",
                new MasterActionRequest("MD independent final approval", verified.GetProperty("Version").GetUInt32()));
            var approved = await Get<JsonElement>(client, "/api/v1/masters/vendors/" + vendorCode);
            Assert.Equal("Approved", approved.GetProperty("ApprovalStatus").GetString());
            Assert.Equal("Active", approved.GetProperty("VendorStatus").GetString());
        }
        Assert.True(await service.HasPermissionAsync(["ACCOUNTS_MANAGER"], "masters.vendors", "verify", default));
        Assert.True(await service.HasPermissionAsync(["MANAGING_DIRECTOR"], "masters.vendors", "approve", default));
        server.Execute("vendor-owner-preserves.sql", """
            DO $check$ BEGIN
              IF (SELECT count(*) FROM public.vendor_owner_before) <> (SELECT count(*) FROM advance.role_page_permissions)
                OR EXISTS(SELECT 1 FROM public.vendor_owner_before b LEFT JOIN advance.role_page_permissions g ON g."Id"=b."Id"
                  LEFT JOIN advance.r1_vendor_owner_workflow_backup j ON j.permission_id=g."Id"
                  WHERE g."Id" IS NULL OR CASE WHEN j.permission_id IS NULL THEN b.body IS DISTINCT FROM to_jsonb(g)
                    ELSE (b.body-'CanSubmit'-'CanUploadAttachment'-'UpdatedAt'-'UpdatedBy'-'Version') IS DISTINCT FROM
                      (to_jsonb(g)-'CanSubmit'-'CanUploadAttachment'-'UpdatedAt'-'UpdatedBy'-'Version') END)
              THEN RAISE EXCEPTION 'Unapproved permission change.'; END IF;
              IF (SELECT count(*) FROM advance.r1_vendor_owner_workflow_backup) <> 1 THEN
                RAISE EXCEPTION 'Expected exactly one owner permission.'; END IF;
            END $check$;
            """);
        const string edit = """
            UPDATE advance.role_page_permissions g SET "CanExport"=NOT g."CanExport"
            FROM advance.r1_vendor_owner_workflow_backup b WHERE g."Id"=b.permission_id;
            """;
        server.Execute("vendor-owner-drift.sql", edit);
        server.AssertRejected("vendor-owner-refuse-down.sql", down, "refuses changed or removed permissions");
        server.Execute("vendor-owner-restore-drift.sql", edit);
        server.Execute("vendor-owner-down.sql", down);
        Assert.Equal(before, await db.Database.SqlQueryRaw<string>(snapshot).SingleAsync());
        server.Execute("vendor-owner-reapply.sql", up);
        Assert.Contains(target, await db.Database.GetAppliedMigrationsAsync());
        // The approved governed DEMO grant may already exist before the packaged migration arrives.
        server.Execute("vendor-owner-down-for-existing.sql", down);
        server.Execute("vendor-owner-site-grant.sql", """
            UPDATE advance.role_page_permissions g SET "CanSubmit"=true,"CanUploadAttachment"=true,
              "UpdatedBy"='TD_APPROVED_DEMO_SETUP',"Version"="Version"+1
            FROM advance.roles r,advance.page_definitions p WHERE r."Id"=g."RoleId" AND p."Id"=g."PageDefinitionId"
              AND r."Code"='PURCHASE_MANAGER' AND p."PageKey"='masters.vendors';
            """);
        var site = await db.Database.SqlQueryRaw<string>(snapshot).SingleAsync();
        server.Execute("vendor-owner-preserve-site-up.sql", up);
        server.Execute("vendor-owner-preserve-site-down.sql", down);
        Assert.Equal(site, await db.Database.SqlQueryRaw<string>(snapshot).SingleAsync());
        server.Execute("vendor-owner-final-up.sql", up);
    }
}