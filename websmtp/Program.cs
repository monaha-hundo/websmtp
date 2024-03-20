using websmtp.Startup;

CommandLine.ParseStartupArgs(args);

var builder = WebApplication.CreateSlimBuilder(args);

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
}

app.Run();

return 0;

public partial class Program { } // To enable testing