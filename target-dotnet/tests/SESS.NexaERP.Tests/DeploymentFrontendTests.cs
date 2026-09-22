using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SESS.NexaERP.Api.Hosting;

namespace SESS.NexaERP.Tests;

public sealed class DeploymentFrontendTests
{
    [Fact]
    public async Task StaticFrontendAndDeepLinksWorkWithoutMaskingMissingApiOrHealthRoutes()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexa-spa-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "index.html"), "<html>deployment-spa</html>");
        await File.WriteAllTextAsync(Path.Combine(root, "app.js"), "window.packaged=true;");
        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            { ContentRootPath = root, WebRootPath = root });
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            await using var app = builder.Build();
            DeploymentFrontend.Configure(app);
            app.MapGet("/api/v1/ping", () => "api-response");
            await app.StartAsync();
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            foreach (var path in new[] { "/", "/login", "/items/ITEM-1", "/oidc/callback" })
                Assert.Contains("deployment-spa", await client.GetStringAsync(path));
            Assert.Equal("window.packaged=true;", await client.GetStringAsync("/app.js"));
            Assert.Equal("api-response", await client.GetStringAsync("/api/v1/ping"));
            foreach (var path in new[] { "/api", "/api/missing", "/API/missing", "/health/missing", "/missing.js" })
            {
                using var response = await client.GetAsync(path);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
            await app.StopAsync();
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
