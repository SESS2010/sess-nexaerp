using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SESS.NexaERP.Api.Endpoints;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Stores;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Theory]
    [InlineData("42501", typeof(UnauthorizedAccessException))]
    [InlineData("23505", typeof(StoresConflictException))]
    [InlineData("P0001", typeof(StoresConflictException))]
    [InlineData("42601", typeof(PostgresException))]
    public async Task Opening_stock_translates_database_authority_refusals_without_hiding_unrelated_errors(string state, Type expected)
    {
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql(server.ConnectionString).Options);
        await db.Database.OpenConnectionAsync();
        var employee = Guid.NewGuid();
        var role = "STORES_MANAGER";
        var user = new TaxWorkflowUser(employee, "opening-refusal-test", role,
            new Dictionary<string, EffectiveRoleAssignment>
            {
                [TaxWorkflowUser.AssignmentKey(employee, role)] = new(Guid.NewGuid(), role, "FULL")
            });
        user.RequireRole("stores.opening-stock:create", role);
        var service = new EfOpeningStockService(db, user, null!);
        var execute = typeof(EfOpeningStockService).GetMethod("Execute", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var sql = "DO $test$ BEGIN RAISE EXCEPTION USING ERRCODE='" + state + "', MESSAGE='controlled refusal'; END $test$;";
        var task = (Task<(Guid Id, bool Replayed)>)execute.Invoke(service,
            [sql, Guid.NewGuid(), new DateOnly(2026,4,1), new DateOnly(2027,3,31), 0L,
             "refusal regression", "refusal-key", "hash", Guid.NewGuid(), role, CancellationToken.None])!;
        var error = await Record.ExceptionAsync(async () => await task);
        Assert.NotNull(error);
        Assert.Equal(expected, error.GetType());
        if (state == "42501")
        {
            Assert.Equal(state, Assert.IsType<PostgresException>(error.InnerException).SqlState);
            // Exercise the production endpoint wrapper as well as the service mapping.
            // An authenticated authority refusal must become its 403 result, not escape as a 500.
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "opening-refusal-test")], "test"))
            };
            var run = typeof(OpeningStockEndpoints).GetMethod("Run", BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(typeof((Guid, bool)));
            var response = (Task<IResult>)run.Invoke(null,
                [new Func<Task<(Guid Id, bool Replayed)>>(() => task), context, false])!;
            Assert.IsType<ForbidHttpResult>(await response);
        }
    }
}