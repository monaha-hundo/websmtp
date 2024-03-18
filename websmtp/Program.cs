using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using websmtp.Startup;

CommandLine.ParseStartupArgs(args);

var builder = WebApplication.CreateBuilder(args);

Startup.InitAppJsonConfig(builder);
Startup.ConfigureWebHost(builder);
Startup.ConfigureServices(builder);

var app = builder.Build();

Startup.ConfigureSecurity(app);
Startup.ConfigureAppPipeline(app);
Startup.MapEndpoints(app);

CommandLine.ParseModifiersArgs(args, app);

if (app.Environment.IsEnvironment("Test"))
{
    CommandLine.MigrateDatabase(app);

    app.MapGet("/test-login", ([FromServices] IHttpContextAccessor httpContextAccessor) =>
    {

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString().Replace('0','1')),
            new Claim(ClaimTypes.Name, "tester"),
            new Claim(ClaimTypes.Role, "user"),
        };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(claimsIdentity);

        httpContextAccessor.HttpContext.SignInAsync(principal);
    });
}

app.Run();

return 0;

public partial class Program { } // To enable testing