namespace SESS.NexaERP.Api.Hosting;

public static class DeploymentFrontend
{
    public static void Configure(WebApplication app)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();
        // BrowserRouter deep links resolve to the packaged SPA. Missing API/health
        // routes must keep their real 404 rather than return an HTML success page.
        app.MapFallbackToFile("{*path:nonfile:regex(^(?!(api|health)(/|$)).*$)}", "index.html");
    }
}
