namespace websmtp;

public class InitialisationTasks
{
    public static async Task InitMessageStore(WebApplication app)
    {
        var scope = app.Services.CreateScope();
        var log = scope.ServiceProvider.GetService<ILogger<InitialisationTasks>>();
        if (!Directory.Exists("messages"))
        {
            Directory.CreateDirectory("messages");
        }
        var smtpBgSrv = scope.ServiceProvider.GetRequiredService<IReadableMessageStore>();
        await smtpBgSrv.LoadMessages();
    }

}