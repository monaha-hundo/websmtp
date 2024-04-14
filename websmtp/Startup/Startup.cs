using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmtpServer.Storage;
using websmtp.Database;
using websmtp.Endpoints;
using websmtp.services;

namespace websmtp.Startup;

public static class Startup
{
    public static void InitAppConfig(IHostApplicationBuilder builder)
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

        if (builder.Environment.IsEnvironment("Test"))
        {
            builder.Configuration.AddJsonFile("appSettings.Test.json", false, true);
        }

        builder.Configuration.AddEnvironmentVariables();
    }

    public static void ConfigureWebHost(WebApplicationBuilder builder)
    {
        builder.WebHost.UseKestrel((context, serverOptions) =>
        {
            serverOptions.Listen(IPAddress.Any, 5000);
        });
    }

    public static void ConfigureWebServices(IHostApplicationBuilder builder)
    {
        ConfigureDatabase(builder);
        ConfigureSecurity(builder);
        ConfigureSmtpServices(builder);

        builder.Services.AddRazorPages();
        builder.Services.AddTransient<IReadableMessageStore, ReadableMessageStore>();
    }

    public static void ConfigureSecurity(IHostApplicationBuilder builder)
    {
        builder.Services.Configure<CookieTempDataProviderOptions>(opts =>
        {
            opts.Cookie.Name = "bertrand";// Guid.NewGuid().ToString("N");
            opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            opts.Cookie.SameSite = SameSiteMode.Strict;
        });
        //builder.Services.AddSession();
        builder.Services.AddAntiforgery(opts =>
        {
            opts.Cookie.Name = "jeanguy";// Guid.NewGuid().ToString("N");
            opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            opts.Cookie.SameSite = SameSiteMode.Strict;
        });
        builder.Services.AddAuthentication().AddCookie(opts =>
        {
            opts.LoginPath = "/login";
            opts.AccessDeniedPath = "/error/";
            opts.ExpireTimeSpan = TimeSpan.FromMinutes(20);
            opts.SlidingExpiration = true;
            opts.Cookie.Name = "ronald"; //Guid.NewGuid().ToString("N");
            opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            opts.Cookie.SameSite = SameSiteMode.Strict;
        });
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAuthorization(opts =>
        {
            opts.AddPolicy("admin", pol =>
            {
                pol.RequireRole("admin");
            });
        });
    }

    public static void ConfigureSmtpServices(IHostApplicationBuilder builder)
    {
        builder.Services.AddTransient<SendMail>();
        builder.Services.AddTransient<SpamAssassin>();
        builder.Services.AddSingleton<IMessageStore, WritableMessageStore>();
        builder.Services.AddHostedService<services.BackgroundSmtpServer>();
    }

    public static void ConfigureDatabase(IHostApplicationBuilder builder)
    {
        var dbServer = builder.Configuration.GetValue<string>("Database:Server");
        var dbName = builder.Configuration.GetValue<string>("Database:Name");
        var dbUsername = builder.Configuration.GetValue<string>("Database:Username");
        var dbPassword = builder.Configuration.GetValue<string>("Database:Password");
        var csWithoutPassword = $"server={dbServer};database={dbName};user={dbUsername};";
        Console.WriteLine($"Connection string: '{csWithoutPassword}'.");
        var cs = csWithoutPassword + $"password={dbPassword}";
        var srvVer = ServerVersion.AutoDetect(cs);
        builder.Services.AddDbContext<DataContext>(dbOpts => dbOpts.UseMySql(cs, srvVer), ServiceLifetime.Transient, ServiceLifetime.Transient);
    }


    public static void ConfigureSecurity(WebApplication app)
    {
        if (app.Environment.IsProduction())
        {
            app.UseHsts();
        }

        var enableHtmlMedia = app.Configuration.GetValue<bool>("Security:EnableMediaInHtml");

        var cspHeaderName = "Content-Security-Policy";
        var csp = new Dictionary<string, List<string>>()
        {
            {"default-src", new List<string>{"self"}},
            {"connect-src", new List<string>{"self"}},
            {"script-src", new List<string>{"self"}},
            {"img-src", new List<string>{"self", "blob:"}},
            {"style-src", new List<string>{"self"}},
            {"frame-src", new List<string>{"self"}}
        };

        if (enableHtmlMedia)
        {
            csp["img-src"].Add("data:");
        }

        var cspHeaderValue = string.Join("; ", csp.Keys.Select(c => $"{c} {string.Join(' ', csp[c].Select(s => s.Contains(':') ? s : "'" + s + "'"))}"));

        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments(new PathString("/api")))
            {
                context.Response.Headers.Append(cspHeaderName, cspHeaderValue);
            }
            await next.Invoke();
            // Do logging or other work that doesn't write to the Response.
        });
    }

    public static void ConfigureAppPipeline(WebApplication app)
    {
        // if (app.Environment.IsProduction())
        // {
        //     app.UseResponseCompression();
        // }

        //app.UseSession();
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
        // Messages actions
        app.MapPost("/api/messages/train/", MessagesEndpoints.Train).RequireAuthorization();

        app.MapPost("/api/messages/spam/", MessagesEndpoints.Spam).RequireAuthorization();
        app.MapPost("/api/messages/notspam/", MessagesEndpoints.NotSpam).RequireAuthorization();
        app.MapPost("/api/messages/star/", MessagesEndpoints.Star).RequireAuthorization();
        app.MapPost("/api/messages/unstar/", MessagesEndpoints.Unstar).RequireAuthorization();
        app.MapPost("/api/messages/mark-as-read/", MessagesEndpoints.MarkAsRead).RequireAuthorization();
        app.MapPost("/api/messages/mark-as-unread/", MessagesEndpoints.MarkAsUnread).RequireAuthorization();
        app.MapPost("/api/messages/delete/", MessagesEndpoints.Delete).RequireAuthorization();
        app.MapPost("/api/messages/undelete/", MessagesEndpoints.Undelete).RequireAuthorization();

        // Messages attachements and raw download
        app.MapGet("/api/messages/{msgId}/attachements/{filename}", MessagesEndpoints.GetMessageAttachement).RequireAuthorization();
        app.MapGet("/api/messages/{msgId}.html", MessagesEndpoints.GetMessage).RequireAuthorization();

        // Users accounts
        app.MapPost("/api/settings/administration/add-user", AccountEndpoints.AddUser).RequireAuthorization("admin");
        app.MapGet("/api/settings/otp/initiate", AccountEndpoints.OtpInitiate).RequireAuthorization();
        app.MapPost("/api/settings/otp/validate", AccountEndpoints.OtpValidateAndEnable).RequireAuthorization();
        app.MapPost("/api/settings/pwd/change", AccountEndpoints.ChangePassword).RequireAuthorization();
        app.MapPost("/api/settings/mailboxes/add", AccountEndpoints.AddMailbox).RequireAuthorization();
        app.MapPost("/api/settings/identities/add", AccountEndpoints.AddIdentity).RequireAuthorization();
        app.MapPost("/api/settings/mailboxes/remove", AccountEndpoints.RemoveMailbox).RequireAuthorization("admin");
        app.MapPost("/api/settings/identities/remove", AccountEndpoints.RemoveIdentity).RequireAuthorization("admin");
    }
}
