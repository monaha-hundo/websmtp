using websmtp.Startup;

CommandLine.ParseStartupArgs(args);

var builder = WebApplication.CreateBuilder(args);

Startup.InitAppJsonConfig(builder);
Startup.ConfigureWebHost(builder);
Startup.ConfigureServices(builder);

var app = builder.Build();

app.Use(async (context, next) =>
{
    var cspHeaderName = "Content-Security-Policy-Report-Only";
    var csp = new Dictionary<string, List<string>>()
    {
        {"default-src", new List<string>{"self"}},
        {"connect-src", new List<string>{"self"}},
        {"script-src", new List<string>{"self"}},
        {"img-src", new List<string>{"self"}},
        {"style-src", new List<string>{"self"}},
        {"frame-src", new List<string>{"self"}}
    };
    var cspHeaderValue = string.Join("; ", csp.Keys.Select(c => $"{c} {string.Join(' ', csp[c].Select(s => "'" + s + "'"))}"));

    context.Response.Headers.Append(cspHeaderName, cspHeaderValue);
    await next.Invoke();
    // Do logging or other work that doesn't write to the Response.
});

Startup.ConfigureAppPipeline(app);
Startup.MapEndpoints(app);

CommandLine.ParseModifiersArgs(args, app);

if (app.Environment.IsEnvironment("Test"))
{
    CommandLine.MigrateDatabase(app);
}

app.Run();

return 0;

public partial class Program { } // To enable testing