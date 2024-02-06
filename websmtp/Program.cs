using websmtp;

// Command line args parsing
var passGenCmdResult = Startup.CheckArgsForPasswordGenerateCommand(args);
if (passGenCmdResult != null)
{
    return passGenCmdResult.Value;
}

// Services Setup
var builder = WebApplication.CreateBuilder(args);
Startup.ConfigureServices(builder.Services);

// App. Pipeline configuration
var app = builder.Build();
Startup.ConfigureAppPipeline(app);
Startup.MapEndpoints(app);

// Initialisation Tasks
await Task.Run(async () => await Startup.InitMessageStore(app));

// Start listening for requests
app.Run();

return 0;

public partial class Program { } // To enable testing