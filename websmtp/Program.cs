using websmtp;

Startup.ParseArgs(args);

var builder = WebApplication.CreateBuilder(args);

Startup.InitAppJsonConfig(builder);

Startup.ConfigureServices(builder.Services);

var app = builder.Build();

Startup.ConfigureAppPipeline(app);
Startup.MapEndpoints(app);

app.Run();

return 0;

public partial class Program { } // To enable testing