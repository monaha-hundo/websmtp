﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OtpNet;
using QRCoder;
using SmtpServer.Storage;
using System.Diagnostics;
using System.Net.Sockets;
using System.Web;
using websmtp.Database;

namespace websmtp;

public static class Startup
{
    public static int? ParseArgs(string[] args)
    {
        var shouldHashPassword = args.Any(arg => arg.StartsWith("--generate-credentials-config"));
        if (shouldHashPassword)
        {
            return GenerateCredentialConfig();
        }

        var shouldGenerateDkimConfig = args.Any(arg => arg.StartsWith("--generate-dkim-config"));
        if (shouldGenerateDkimConfig)
        {
            return GenerateDkimConfig();
        }

        return null;
    }

    private static int GenerateDkimConfig()
    {
        if(File.Exists("dkim_private.pem"))
        {
            Console.WriteLine("dkim_private.pem already exists, exiting");
            return -1;
        }

        var createPrivKeyProc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "openssl",
                Arguments = "genrsa -out dkim_private.pem 2048",
                UseShellExecute = false,
                //RedirectStandardOutput = false,
                CreateNoWindow = true,
                WorkingDirectory = "."
            }
        };

        if(!createPrivKeyProc.Start())
        {
            Console.WriteLine("could not execute openssl, exiting");
            return -1;
        }

        createPrivKeyProc.WaitForExit();

        if(!File.Exists("dkim_private.pem"))
        {
            Console.WriteLine("dkim_private.pem was not correctly created, exiting");
            return -1;
        }
        //openssl 
        
        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "openssl",
                Arguments = "rsa -in dkim_private.pem -pubout -outform der 2>/dev/null | openssl base64 -A", //> dkim_public_base64.txt
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = "."
            }
        };

        if(!proc.Start())
        {
            Console.WriteLine("could not execute openssl, exiting");
            return -1;
        }

        proc.WaitForExit();

        var base64PubKey = proc.StandardOutput.ToString();

        Console.WriteLine("private/public key pair generated.");
        Console.Write("enter the domain name: ");
        var domain = Console.ReadLine();
        Console.Write("enter a selector for the DKIM process: ");
        var selector = Console.ReadLine();

        Console.WriteLine("Create a TXT record in the DNS like this: ");
        Console.WriteLine($" -Name: {selector}._domainkey.{domain}");
        Console.WriteLine($" -Value: \"{base64PubKey}\"");

        Console.WriteLine("Adjuste the config file and make sure it contains the following: ");
        Console.WriteLine(JsonConvert.SerializeObject(new{
            DKIM = new {
                Selector = selector,
                Domain = domain,
                PrivateKey = "dkim_private.pem"
            }
        }, new JsonSerializerSettings{
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        }));

    }

    private static int GenerateCredentialConfig()
    {
        Console.Write("Enter username: ");
        var username = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(username) || username.Length == 0)
        {
            Console.WriteLine("Invalid username.");
            Environment.Exit(-1);
            return -1;
        }

        Console.Write("Enter password: ");
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

        byte[] raw = new byte[10];
        Random.Shared.NextBytes(raw);
        var otpSecret = Base32Encoding.ToString(raw);

        var qrGenerator = new QRCodeGenerator();
        var totpQrCodeString = new OtpUri(OtpType.Totp, otpSecret, username).ToString();
        var qrCodeData = qrGenerator.CreateQrCode(totpQrCodeString, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new AsciiQRCode(qrCodeData);
        var lines = qrCode.GetLineByLineGraphic(1, drawQuietZones: true);
        Console.WriteLine($"Use the following QR code for your 2FA app:");
        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }

        Console.WriteLine("Add a 'Security' section to the configuration file to set the login credentials, eg.:");
        Console.WriteLine(JsonConvert.SerializeObject(new
        {
            Security = new
            {
                Username = username,
                PasswordHash = hash,
                MfaEnabled = true,
                OTPSecret = otpSecret
            }
        }, Formatting.Indented));
        Environment.Exit(0);
        return 0;
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
