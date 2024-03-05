using websmtp.Startup;

CommandLine.ParseStartupArgs(args);

var builder = WebApplication.CreateBuilder(args);
Startup.InitAppJsonConfig(builder);
Startup.ConfigureServices(builder);

var app = builder.Build();

Startup.ConfigureAppPipeline(app);
Startup.MapEndpoints(app);

CommandLine.ParseModifiersArgs(args, app);

app.Run();

return 0;

public partial class Program { } // To enable testing