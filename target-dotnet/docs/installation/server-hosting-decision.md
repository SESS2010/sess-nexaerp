# DESKTOP-SPF5420 hosting decision

> **Protected server rule (21 September, 16:01):** NEVER stop, disable, modify or remove
> SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, their ~77 SOLIDWORKS project databases,
> ewserver, ANY NI or Siemens service, ANY Rockwell FactoryTalk service, IIS Default
> Web Site or /Updater. Leave Wamp stopped/manual. Windows 10 stays; NEVER a clean
> Windows install; NEVER install a .NET SDK on this server. All application here is
> by the server agent, not from the laptop. See C:\SESS-ServerPrep for completed preparation.


Use a Windows service, SCM name `SESSNexaERP`, on HTTPS 8443. IIS owns 80 and 81;
this preserves its sites and avoids a reverse-proxy/site-binding dependency.
The API integrates with Windows Service Control Manager. Delayed automatic start
and service recovery start it after reboot without a Windows login. The frontend
static production build is served from `api/wwwroot` at the same origin, including
BrowserRouter deep links. Unknown API and health routes remain 404, not SPA HTML.
Eleven users enter **https://192.168.68.130:8443**; reserve the address and install a
trusted certificate with IP SAN 192.168.68.130 on all clients. Do not bypass TLS checks.

The selected frontend developer branch (`0c59254f58bd49fc13a8b919387ba0cf5a1d9988`)
contains the Actual BOM pane, but still uses Debug-only identity/token endpoints. Its
Vite production build is not a production authentication implementation. Release
continues to refuse development authentication. Obtain the production frontend and
existing HTTPS OIDC provider configuration before the demo; this package is a
candidate until that is resolved. It does not bundle an identity server.

Service startup, certificate access, reboot without login, another-PC sign-in and
coexistence with SOLIDWORKS must be witnessed on the field machine. The laptop
routing test proves deep links/static assets and API 404 isolation, not those facts.
See [server-deployment.md](server-deployment.md) for the exact installation order.

Reference: [ASP.NET Core Windows services](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-10.0).
