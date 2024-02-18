using websmtp;

// Command line args parsing
Startup.ParseArgs(args);

// Services Setup
var builder = WebApplication.CreateBuilder(args);
Startup.ConfigureServices(builder.Services);

// App. Pipeline configuration
var app = builder.Build();
Startup.ConfigureAppPipeline(app);
Startup.MapEndpoints(app);

// Start listening for requests
app.Run();

return 0;

public partial class Program { } // To enable testing