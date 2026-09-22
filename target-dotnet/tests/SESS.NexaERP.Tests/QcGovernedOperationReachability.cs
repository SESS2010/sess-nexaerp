using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    // No allow-list of existing routes: adding a QC write route requires a real API witness.
    private sealed class QcReachabilityWitness(Guid qcId, Guid tdId, IReadOnlySet<Guid> seededAssignments)
    {
        private sealed record ActorEvidence(Guid? EmployeeId, string RoleCode, Guid? AssignmentId);
        private readonly ConcurrentDictionary<string, ActorEvidence> reached = new(StringComparer.Ordinal);

        public Action Begin(HttpContext context, ICurrentUser user)
        {

            return () =>
            {
                if (context.Response.StatusCode is < 200 or >= 300 ||
                    context.GetEndpoint() is not RouteEndpoint endpoint ||
                    !IsQc(endpoint.RoutePattern.RawText) || !IsWrite(context.Request.Method)) return;
                // Authority is operation-specific and is resolved during endpoint execution.
                // Capture the actual resolved assignment, never a pre-request role guess.
                reached[context.Request.Method + " " + endpoint.RoutePattern.RawText] =
                    new ActorEvidence(user.EmployeeId, user.RoleCode, user.ResolvedRoleAssignmentId);
            };
        }

        public static IReadOnlyList<string> Routes(IEndpointRouteBuilder builder) =>
            builder.DataSources.SelectMany(x => x.Endpoints).OfType<RouteEndpoint>()
                .Where(x => IsQc(x.RoutePattern.RawText))
                .SelectMany(x => (x.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [])
                    .Where(IsWrite).Select(method => method + " " + x.RoutePattern.RawText))
                .Order(StringComparer.Ordinal).ToArray();

        private static bool IsQc(string? path) => path is not null &&
            (path.StartsWith("/api/v1/qc/", StringComparison.Ordinal) ||
             path.StartsWith("/api/v1/rev869a/configuration/qc-inspection-policies", StringComparison.Ordinal));
        private static bool IsWrite(string method) => method is "POST" or "PUT" or "PATCH" or "DELETE";

        public async Task AssertCompleteAsync(IReadOnlyList<string> operations)
        {
            Assert.NotEmpty(operations);
            for (var attempt = 0; attempt < 20 && operations.Any(x => !reached.ContainsKey(x)); attempt++)
                await Task.Delay(25);
            var missing = operations.Where(x => !reached.ContainsKey(x)).ToArray();
            Assert.True(missing.Length == 0,
                "QC operations without a successful seeded-employee API witness: " + string.Join("; ", missing));
            foreach (var operation in operations)
            {
                var actor = reached[operation];
                Assert.Contains(actor.EmployeeId, new Guid?[] { qcId, tdId });
                Assert.Contains(actor.RoleCode, new[] { "QC_MANAGER", "TECHNICAL_DIRECTOR" });
                Assert.True(actor.AssignmentId.HasValue && seededAssignments.Contains(actor.AssignmentId.Value),
                    operation + " did not use a seeded role assignment.");
            }
            var directory = Path.Combine(FindRepositoryRoot(), "local-evidence", "qc-reachability");
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, "qc-operation-witnesses.json"),
                JsonSerializer.Serialize(operations.Select(x => new { Operation = x, Actor = reached[x] }),
                    new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
