using websmtp.Startup;

var smtpServerOnly = CommandLine.ParseStartupArgs(args);

if (smtpServerOnly)
{
    var cliBuilder = Host.CreateApplicationBuilder(args);
    Startup.InitAppConfig(cliBuilder);
    Startup.ConfigureSqlite(cliBuilder);
    Startup.ConfigureSmtpServices(cliBuilder);
    var cliApp = cliBuilder.Build();
    cliApp.Run();
    return 0;
}

var webBuilder = WebApplication.CreateSlimBuilder(args);

Startup.InitAppConfig(webBuilder);
Startup.ConfigureWebHost(webBuilder);
Startup.ConfigureWebServices(webBuilder);

var webApp = webBuilder.Build();

Startup.ConfigureSecurity(webApp);
Startup.ConfigureAppPipeline(webApp);
Startup.MapEndpoints(webApp);

CommandLine.ParseModifiersArgs(args, webApp);

webApp.Run();
return 0;

public partial class Program { } // To enable testing