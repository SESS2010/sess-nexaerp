using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

/// <summary>
/// R3 (26 Sep): the employee who entered a Vendor Bill cannot also accept or reject it. The purchase
/// flow witness proves the normal path (Accounts Assistant enters, Accounts Manager decides) still
/// works; this proves the refusal and the exact firing condition, and that Down removes it.
/// </summary>
public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string SeparateDeciderTarget = "20260926100000_VendorBillSeparateDecider";

    [Fact]
    public void A_vendor_bill_decided_by_its_creator_is_refused_and_the_guard_rolls_back()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var predecessor = migrations[Array.IndexOf(migrations, SeparateDeciderTarget) - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("separate-decider-up.sql", migrator.GenerateScript("0", SeparateDeciderTarget) + """
            DO $installed$ DECLARE def text; BEGIN
              SELECT pg_get_triggerdef(t.oid) INTO def FROM pg_trigger t
               WHERE t.tgrelid='advance.vendor_bills'::regclass AND t.tgname='trg_vendor_bills_separate_decider';
              IF def IS NULL OR def NOT LIKE '%BEFORE UPDATE OF "Status" ON advance.vendor_bills FOR EACH ROW WHEN%'
                 OR def NOT LIKE '%old."Status")::text = ''DRAFT''%' OR def NOT LIKE '%''ACCEPTED''%' OR def NOT LIKE '%''REJECTED''%'
                 OR def NOT LIKE '%guard_vendor_bill_separate_decider()%' THEN
                RAISE EXCEPTION 'The separate-decider trigger is missing or fires on the wrong condition: %', def;
              END IF;
            END $installed$;
            CREATE TABLE public.vendor_bill_decider_probe("Status" text, "CreatedByEmployeeId" uuid, "DecidedByEmployeeId" uuid);
            CREATE TRIGGER probe_separate_decider BEFORE UPDATE OF "Status" ON public.vendor_bill_decider_probe FOR EACH ROW
              WHEN (OLD."Status"='DRAFT' AND NEW."Status" IN ('ACCEPTED','REJECTED'))
              EXECUTE FUNCTION advance.guard_vendor_bill_separate_decider();
            INSERT INTO public.vendor_bill_decider_probe VALUES
              ('DRAFT','00000000-0000-0000-0000-00000000000a',NULL),
              ('DRAFT','00000000-0000-0000-0000-00000000000b',NULL),
              ('ACCEPTED','00000000-0000-0000-0000-00000000000c','00000000-0000-0000-0000-00000000000d');
            -- A different employee decides: allowed.
            UPDATE public.vendor_bill_decider_probe SET "Status"='ACCEPTED',"DecidedByEmployeeId"='00000000-0000-0000-0000-0000000000ff'
             WHERE "CreatedByEmployeeId"='00000000-0000-0000-0000-00000000000a';
            -- A reversal is not a decision and is not affected, whoever makes it.
            UPDATE public.vendor_bill_decider_probe SET "Status"='REVERSED',"DecidedByEmployeeId"="CreatedByEmployeeId"
             WHERE "CreatedByEmployeeId"='00000000-0000-0000-0000-00000000000c';
            """);
        server.AssertRejected("separate-decider-accept.sql", """
            UPDATE public.vendor_bill_decider_probe SET "Status"='ACCEPTED',"DecidedByEmployeeId"="CreatedByEmployeeId"
             WHERE "CreatedByEmployeeId"='00000000-0000-0000-0000-00000000000b';
            """, "cannot also accept or reject");
        server.AssertRejected("separate-decider-reject.sql", """
            UPDATE public.vendor_bill_decider_probe SET "Status"='REJECTED',"DecidedByEmployeeId"="CreatedByEmployeeId"
             WHERE "CreatedByEmployeeId"='00000000-0000-0000-0000-00000000000b';
            """, "Another Accounts Manager must decide");
        server.Execute("separate-decider-down.sql", "DROP TABLE public.vendor_bill_decider_probe;"
            + migrator.GenerateScript(SeparateDeciderTarget, predecessor) + """
            DO $removed$ BEGIN
              IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_vendor_bills_separate_decider')
                 OR to_regprocedure('advance.guard_vendor_bill_separate_decider()') IS NOT NULL THEN
                RAISE EXCEPTION 'Down must remove the separate-decider guard.';
              END IF;
            END $removed$;
            """);
    }
}
