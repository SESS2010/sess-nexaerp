using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Theory]
    [InlineData("RETURNABLE")]
    [InlineData("NON_RETURNABLE")]
    public Task R1_machine_DC_selection_and_job_number_survive_dispatch_signature_and_rollback(string nature) =>
        RunCompletePurchaseFlow(machineDelivery: async context =>
        {
            await using var host = await PurchaseFlowHost.StartAsync(context.RuntimeConnection, context.User, useRealPagePermissions: true);
            await using var db = new NexaErpDbContext(context.Options);
            var job = await db.JobOrders.SingleAsync(j => j.MachineSerial == "WITNESS-MACHINE-001-CORRECTED");
            context.User.Set(context.StoresId, "SESS-35", "STORES_EXECUTIVE");
            const string route = "/api/v1/stores/machine-deliveries/";
            var search = route + "job-orders?search=" + Uri.EscapeDataString(job.JobOrderNumber) + "&pageSize=1";
            var before = await Get<PagedResponse<MachineDeliveryJobOrderCandidate>>(host.Client, route + "job-orders?pageSize=1");
            Assert.Equal(job.Id, Assert.Single((await Get<PagedResponse<MachineDeliveryJobOrderCandidate>>(host.Client, search)).Items).JobOrderId);
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Asia/Kolkata").DateTime);
            var request = new DispatchMachineRequest(job.Id, "R1-DC-SELECTION", nature,
                nature == "RETURNABLE" ? "DEMO" : "CUSTOMER_PO_BASED", today,
                nature == "RETURNABLE" ? today.AddDays(7) : null, "Test customer gate", "r1-dc-selection");
            var dc = await Post<JsonElement>(host.Client, route, request);
            var id = dc.GetProperty("Id").GetGuid();
            Assert.Equal(job.JobOrderNumber, dc.GetProperty("JobOrderNumber").GetString());
            var replay = await Post<JsonElement>(host.Client, route, request);
            Assert.Equal(dc.GetRawText(), replay.GetRawText());
            var after = await Get<PagedResponse<MachineDeliveryJobOrderCandidate>>(host.Client, route + "job-orders?pageSize=1");
            Assert.Equal(before.TotalCount - 1, after.TotalCount);
            var excluded = await Get<PagedResponse<MachineDeliveryJobOrderCandidate>>(host.Client, search);
            Assert.Empty(excluded.Items);
            Assert.Equal(0, excluded.TotalCount);
            using (var duplicate = await host.Client.PostAsJsonAsync(route, request with { DcNumber = "R1-DC-DUPLICATE", IdempotencyKey = "r1-dc-duplicate" }))
                Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            var detail = await Get<JsonElement>(host.Client, route + id);
            Assert.Equal(job.JobOrderNumber, detail.GetProperty("JobOrderNumber").GetString());
            var signed = await Post<JsonElement>(host.Client, route + id + "/signature",
                new SignMachineDeliveryRequest(DateTimeOffset.UtcNow, "Test recipient",
                    new("signed.pdf", "application/pdf", Encoding.ASCII.GetBytes("%PDF-1.7\nTest\n%%EOF")), "r1-dc-signature"));
            Assert.Equal(job.JobOrderNumber, signed.GetProperty("JobOrderNumber").GetString());
            Assert.Empty((await Get<PagedResponse<MachineDeliveryJobOrderCandidate>>(host.Client, search)).Items);

            // The migration changes projections only. Down/up must preserve signed DC evidence and receipts.
            var migrator = db.GetService<IMigrator>();
            var retained = await db.Database.SqlQueryRaw<string>("""
                SELECT (to_jsonb(d)::text || to_jsonb(s)::text) AS "Value"
                FROM advance.machine_delivery_challans d JOIN advance.machine_delivery_signatures s ON s."DeliveryChallanId"=d."Id"
                WHERE d."DcNumber"='R1-DC-SELECTION'
                """).SingleAsync();
            await db.Database.ExecuteSqlRawAsync(migrator.GenerateScript("20260928100000_MachineDeliverySelection", "20260928090000_TrackingLite"));
            Assert.False((await Get<JsonElement>(host.Client, route + id)).TryGetProperty("JobOrderNumber", out _));
            await db.Database.ExecuteSqlRawAsync(migrator.GenerateScript("20260928090000_TrackingLite", "20260928100000_MachineDeliverySelection"));
            Assert.Equal(retained, await db.Database.SqlQueryRaw<string>("""
                SELECT (to_jsonb(d)::text || to_jsonb(s)::text) AS "Value"
                FROM advance.machine_delivery_challans d JOIN advance.machine_delivery_signatures s ON s."DeliveryChallanId"=d."Id"
                WHERE d."DcNumber"='R1-DC-SELECTION'
                """).SingleAsync());
            Assert.Equal(job.JobOrderNumber, (await Get<JsonElement>(host.Client, route + id)).GetProperty("JobOrderNumber").GetString());
            Assert.Empty((await Get<PagedResponse<MachineDeliveryJobOrderCandidate>>(host.Client, search)).Items);
            Assert.Equal(dc.GetRawText(), (await Post<JsonElement>(host.Client, route, request)).GetRawText());
            context.User.SetOrganization("SESS_PROPRIETORSHIP");
            using var otherCompany = await host.Client.GetAsync(route + id);
            Assert.True(otherCompany.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden);
            context.User.SetOrganization("SESS_PVT_LTD");
        });

    [Fact]
    public void R1_machine_DC_selection_migration_restores_the_installed_view()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        const string previous = "20260928090000_TrackingLite";
        const string target = "20260928100000_MachineDeliverySelection";
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("dc-selection-baseline.sql", migrator.GenerateScript("0", previous));
        server.Execute("dc-selection-before.sql", """
            CREATE TABLE public.dc_selection_baseline AS SELECT replace(prosrc,E'\r\n',E'\n') body
              FROM pg_proc WHERE oid='advance.machine_delivery_json(uuid,uuid)'::regprocedure;
            """);
        server.Execute("dc-selection-up.sql", migrator.GenerateScript(previous, target));
        server.Execute("dc-selection-down.sql", migrator.GenerateScript(target, previous));
        server.Execute("dc-selection-check.sql", """
            DO $check$ BEGIN
              IF (SELECT replace(prosrc,E'\r\n',E'\n') FROM pg_proc WHERE oid='advance.machine_delivery_json(uuid,uuid)'::regprocedure)
                  IS DISTINCT FROM (SELECT body FROM public.dc_selection_baseline)
                OR to_regprocedure('advance.machine_delivery_job_ids(uuid)') IS NOT NULL THEN
                RAISE EXCEPTION 'DC selection rollback failed to restore the installed view.';
              END IF;
            END $check$;
            """);
        server.Execute("dc-selection-up-again.sql", migrator.GenerateScript(previous, target));
    }
}
