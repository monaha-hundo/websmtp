using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using websmtp.Database;
using websmtp.Startup;

CommandLine.ParseStartupArgs(args);

var builder = WebApplication.CreateBuilder(args);

Startup.InitAppJsonConfig(builder);
Startup.ConfigureWebHost(builder);
Startup.ConfigureServices(builder);

var app = builder.Build();

Startup.ConfigureAppPipeline(app);
Startup.MapEndpoints(app);

CommandLine.ParseModifiersArgs(args, app);

if (builder.Environment.IsEnvironment("Test"))
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

        if (dbContext.Database.GetDbConnection() is not SqliteConnection sqliteConnection)
        {
            throw new Exception("Test environement, but database does not appear to be Sqlite... Aborting");
        }

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Test Environement, recreating TEST database.");
        dbContext.Database.EnsureDeleted();
        if (!dbContext.Database.EnsureCreated())
        {
            throw new Exception("Database not created");
        }

        var q = dbContext.RawMessages.ToList();
    }
}

app.Run();

return 0;

public partial class Program { } // To enable testing