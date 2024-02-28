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
