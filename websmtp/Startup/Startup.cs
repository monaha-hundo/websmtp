using Microsoft.AspNetCore.Authentication.Cookies;
using Newtonsoft.Json;
using SmtpServer.Storage;

namespace websmtp;

public static class Startup
{
    public static int? CheckArgsForPasswordGenerateCommand(string[] args)
    {
        var shouldHashPassword = args.Any(arg => arg.StartsWith("--generate-password"));
        if (shouldHashPassword)
        {
            Console.Write("Enter password to hash: ");
            var passwordToHash = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(passwordToHash) || passwordToHash.Length == 0)
            {
                Console.WriteLine("Invalid password.");
                return -1;
            }
            var hasher = new PasswordHasher();
            var hash = hasher.HashPassword(passwordToHash);
            Console.WriteLine($"Hashed password: '{hash}'.");
            Console.WriteLine("Add a 'Security' section to the configuration file to set the login credentials, eg.:");
            Console.WriteLine(JsonConvert.SerializeObject(new
            {
                Security = new
                {
                    Username = "username",
                    Password = hash
                }
            }, Formatting.Indented));
            return 0;
        }

        return null;
    }

    public static void InitAppJsonConfig(WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("appsettings.json", false, true);
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("appsettings.development.json", false, true);
        }
        else
        {
            builder.Configuration.AddJsonFile("appsettings.production.json", true, true);
        }
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddAntiforgery();
        services.AddHttpContextAccessor();
        services.AddAuthentication().AddCookie(ConfigureAuthenticationCookie);

        services.AddAuthorization();
        services.AddRazorPages();
        services.AddTransient<SendMailService>();
        services.AddSingleton<IMessageStore, MessageStore>();
        services.AddTransient<IReadableMessageStore, MessageStore>();
        services.AddHostedService<SmtpServerService>();
    }

    public static void ConfigureAuthenticationCookie(CookieAuthenticationOptions opts)
    {
        opts.LoginPath = "/login";
        opts.AccessDeniedPath = "/error/";
        opts.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        opts.SlidingExpiration = true;
    }

    public static void ConfigureAppPipeline(WebApplication app)
    {
        app.UseAntiforgery();
        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/error");
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();
        app.MapRazorPages();
    }

    public static void MapEndpoints(WebApplication app)
    {
        app.MapGet("/api/messages/{msgId}/attachements/{filename}", MessagesEndpoints.GetMessageAttachement).RequireAuthorization();
        app.MapGet("/api/messages/{msgId}.html", MessagesEndpoints.GetMessage).RequireAuthorization();
    }

    public static async Task InitMessageStore(WebApplication app)
    {
        var scope = app.Services.CreateScope();
        var log = scope.ServiceProvider.GetService<ILogger<WebApplication>>();
        if (!Directory.Exists("messages"))
        {
            Directory.CreateDirectory("messages");
        }
        var smtpBgSrv = scope.ServiceProvider.GetRequiredService<IReadableMessageStore>();
        await smtpBgSrv.LoadMessages();
    }
}
