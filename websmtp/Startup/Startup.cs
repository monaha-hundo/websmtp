using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SmtpServer.Storage;
using websmtp.Database;

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

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<DataContext>(
            options => options.UseSqlite("Data Source=websmtp.db"));

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
}
