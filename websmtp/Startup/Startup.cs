using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SmtpServer.Storage;
using websmtp.Database;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace websmtp;

public static class Startup
{
    public static int? ParseArgs(string[] args)
    {
        var shouldHashPassword = args.Any(arg => arg.StartsWith("--generate-password"));
        if (shouldHashPassword)
        {
            Console.Write("Enter password to hash: ");
            var passwordToHash = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(passwordToHash) || passwordToHash.Length == 0)
            {
                Console.WriteLine("Invalid password.");
                Environment.Exit(-1);
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
            Environment.Exit(0);
            return 0;
        }

        return null;
    }

    public static void InitAppJsonConfig(WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("appSettings.json", false, true);
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("appSettings.Development.json", false, true);
        }
        else
        {
            builder.Configuration.AddJsonFile("appSettings.Production.json", false, true);
        }
    }

    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        var dbServer = builder.Configuration.GetValue<string>("Database:Server");
        var dbName = builder.Configuration.GetValue<string>("Database:Name");
        var dbUsername = builder.Configuration.GetValue<string>("Database:Username");
        var dbPassword = builder.Configuration.GetValue<string>("Database:Password");
        var cs = $"server={dbServer};database={dbName};user={dbUsername};password={dbPassword}";

        builder.Services.AddDbContext<DataContext>(dbOpts => dbOpts.UseMySQL(cs));
        builder.Services.AddAntiforgery();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAuthentication().AddCookie(ConfigureAuthenticationCookie);
        builder.Services.AddAuthorization();
        builder.Services.AddRazorPages();
        builder.Services.AddTransient<SendMailService>();
        builder.Services.AddSingleton<IMessageStore, MessageStore>();
        builder.Services.AddTransient<IReadableMessageStore, MessageStore>();
        builder.Services.AddHostedService<SmtpServerService>();
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
        app.MapPost("/api/messages/{msgId}/mark-as-read/", MessagesEndpoints.MarkAsRead).RequireAuthorization();
        app.MapGet("/api/messages/{msgId}/attachements/{filename}", MessagesEndpoints.GetMessageAttachement).RequireAuthorization();
        app.MapGet("/api/messages/{msgId}.html", MessagesEndpoints.GetMessage).RequireAuthorization();
    }
}
