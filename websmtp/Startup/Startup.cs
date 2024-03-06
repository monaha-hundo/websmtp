using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SmtpServer.Storage;
using websmtp.Database;

namespace websmtp.Startup;

public static class Startup
{
    public static void InitAppJsonConfig(WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("appSettings.json", false, true);

        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("appSettings.Development.json", true, true);
        }

        if (builder.Environment.IsProduction())
        {
            builder.Configuration.AddJsonFile("appSettings.Production.json", true, true);
        }

        if (builder.Environment.IsEnvironment("Test"))
        {
            builder.Configuration.AddJsonFile("appSettings.Test.json", true, true);
        }
    }

    public static void ConfigureWebHost(WebApplicationBuilder builder)
    {
        var useSsl = builder.Configuration.GetValue<bool>("SSL:Enabled");
        if (useSsl)
        {
            var sslPort = builder.Configuration.GetValue<int>("SSL:Port");
            var privKeyFilename = builder.Configuration.GetValue<string>("SSL:PrivateKey") ?? throw new Exception("Missing SSL:PrivateKey configuration.");
            var pubKeyFilename = builder.Configuration.GetValue<string>("SSL:PublicKey") ?? throw new Exception("Missing SSL:PublicKey configuration.");
            
            builder.WebHost.ConfigureKestrel((context, serverOptions) =>
            {
                var certPem = File.ReadAllText(pubKeyFilename);
                var keyPem = File.ReadAllText(privKeyFilename);
                var x509 = X509Certificate2.CreateFromPem(certPem, keyPem);

                serverOptions.Listen(IPAddress.Any, sslPort, listenOptions =>
                {
                    listenOptions.UseHttps(x509);
                });
            });
        }
    }

    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        var dbServer = builder.Configuration.GetValue<string>("Database:Server");
        var dbName = builder.Configuration.GetValue<string>("Database:Name");
        var dbUsername = builder.Configuration.GetValue<string>("Database:Username");
        var dbPassword = builder.Configuration.GetValue<string>("Database:Password");
        var cs = $"server={dbServer};database={dbName};user={dbUsername};password={dbPassword}";

        builder.Services.AddResponseCompression(options =>
         {
             options.EnableForHttps = true;
         });
        builder.Services.AddDbContext<DataContext>(dbOpts => dbOpts.UseMySQL(cs), ServiceLifetime.Transient, ServiceLifetime.Transient);
        builder.Services.AddAntiforgery();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAuthentication().AddCookie(ConfigureAuthenticationCookie);
        builder.Services.AddAuthorization();
        builder.Services.AddRazorPages();
        builder.Services.AddTransient<BasicPublicKeyLocator>();
        builder.Services.AddTransient<IncomingEmailValidator>();
        builder.Services.AddTransient<SendMailService>();
        builder.Services.AddSingleton<IMessageStore, MessageStore>();
        builder.Services.AddTransient<IReadableMessageStore, ReadableMessageStore>();
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
        app.UseResponseCompression();
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
        app.MapPost("/api/messages/{msgId}/delete/", MessagesEndpoints.Delete).RequireAuthorization();
        app.MapPost("/api/messages/{msgId}/undelete/", MessagesEndpoints.Undelete).RequireAuthorization();
        app.MapGet("/api/messages/{msgId}/attachements/{filename}", MessagesEndpoints.GetMessageAttachement).RequireAuthorization();
        app.MapGet("/api/messages/{msgId}.html", MessagesEndpoints.GetMessage).RequireAuthorization();
    }
}
