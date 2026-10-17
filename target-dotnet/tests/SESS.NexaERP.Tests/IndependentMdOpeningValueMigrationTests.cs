using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void IndependentMdOpeningReviewRoundTripPreservesInstalledAuthorityAndExistingGrants()
    {
        const string before="20261017140000_MirIndependentDecisionAuthority";
        const string after="20261017150000_IndependentMdOpeningValueReview";
        using var model=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator=model.GetService<IMigrator>();
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("independent-review-before.sql",migrator.GenerateScript("0",before));
        server.Execute("independent-review-baseline.sql","""
            CREATE TABLE public.md_review_function_before AS SELECT to_jsonb(p) data FROM pg_proc p
              WHERE p.oid='advance.confirm_opening_stock_value(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)'::regprocedure;
            CREATE TABLE public.md_review_grants_before AS SELECT "Id",to_jsonb(p) data FROM advance.role_page_permissions p;
            CREATE TABLE public.md_review_assignments_before AS SELECT "Id",to_jsonb(a) data FROM advance.employee_role_assignments a;
            """);
        var up=migrator.GenerateScript(before,after);var down=migrator.GenerateScript(after,before);
        server.Execute("independent-review-up.sql",up);
        const string existingUnchanged="""
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.md_review_grants_before b LEFT JOIN advance.role_page_permissions p ON p."Id"=b."Id"
                  WHERE to_jsonb(p) IS DISTINCT FROM b.data) OR
                 EXISTS(SELECT 1 FROM public.md_review_assignments_before b LEFT JOIN advance.employee_role_assignments a ON a."Id"=b."Id"
                  WHERE to_jsonb(a) IS DISTINCT FROM b.data) OR
                 (SELECT count(*) FROM advance.employee_role_assignments)<>(SELECT count(*) FROM public.md_review_assignments_before) THEN
                RAISE EXCEPTION 'Independent review changed a prior grant or an employee role assignment.';
              END IF;
            END $assert$;
            """;
        server.Execute("independent-review-up-proof.sql",existingUnchanged+"""
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.md_review_function_before b CROSS JOIN pg_proc p
                WHERE p.oid='advance.confirm_opening_stock_value(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)'::regprocedure
                AND (to_jsonb(p)-'prosrc') IS DISTINCT FROM (b.data-'prosrc')) OR
                (SELECT count(*) FROM advance.role_page_permissions)<>(SELECT count(*)+1 FROM public.md_review_grants_before) OR
                NOT EXISTS(SELECT 1 FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId"
                  WHERE p."Id"=md5('rc26.independent-md-opening-review')::uuid AND r."Code"='MANAGING_DIRECTOR'
                    AND p."CanVerify" AND NOT p."CanApprove" AND NOT p."CanCreate" AND NOT p."CanIssue" AND NOT p."HasFullControl") THEN
                RAISE EXCEPTION 'Independent MD review widened authority or changed function metadata.';
              END IF;
            END $assert$;
            """);
        server.Execute("independent-review-down.sql",down);
        server.Execute("independent-review-restored.sql",existingUnchanged+"""
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.md_review_function_before b CROSS JOIN pg_proc p
                WHERE p.oid='advance.confirm_opening_stock_value(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)'::regprocedure
                  AND to_jsonb(p) IS DISTINCT FROM b.data) OR
                (SELECT count(*) FROM advance.role_page_permissions)<>(SELECT count(*) FROM public.md_review_grants_before) THEN
                RAISE EXCEPTION 'Independent MD review Down failed to restore the exact function and permission set.';
              END IF;
            END $assert$;
            """);
        server.Execute("independent-review-reapply.sql",up);
        server.Execute("independent-review-role-drift.sql","""
            UPDATE advance.role_page_permissions SET "RoleId"=(SELECT "Id" FROM advance.roles WHERE "Code"='MIR_REQUESTER')
              WHERE "Id"=md5('rc26.independent-md-opening-review')::uuid;
            """);
        server.AssertRejected("independent-review-role-drift-down.sql",down,"refuses permission drift");
        server.Execute("independent-review-role-drift-retained.sql","""
            DO $assert$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId"
                WHERE p."Id"=md5('rc26.independent-md-opening-review')::uuid AND r."Code"='MIR_REQUESTER') THEN
                RAISE EXCEPTION 'Refused rollback erased permission identity drift evidence.';
              END IF;
            END $assert$;
            UPDATE advance.role_page_permissions SET "RoleId"=(SELECT "Id" FROM advance.roles WHERE "Code"='MANAGING_DIRECTOR')
              WHERE "Id"=md5('rc26.independent-md-opening-review')::uuid;
            """);
        server.Execute("independent-review-drift.sql","""
            UPDATE advance.role_page_permissions SET "CanApprove"=true
              WHERE "Id"=md5('rc26.independent-md-opening-review')::uuid;
            """);
        Assert.ThrowsAny<Exception>(()=>server.Execute("independent-review-drift-down-refused.sql",down));
        server.Execute("independent-review-drift-evidence.sql","""
            DO $assert$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM advance.role_page_permissions WHERE "Id"=md5('rc26.independent-md-opening-review')::uuid AND "CanApprove") THEN
                RAISE EXCEPTION 'Refused rollback erased permission drift evidence.';
              END IF;
            END $assert$;
            """);
    }
}