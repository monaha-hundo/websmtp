using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OtpNet;
using QRCoder;
using System.Diagnostics;
using websmtp.Database;

namespace websmtp.Startup;

public static class CommandLine
{
    public static int? ParseStartupArgs(string[] args)
    {
        var shouldHashPassword = args.Any(arg => arg.StartsWith("--generate-credentials-config"));
        if (shouldHashPassword)
        {
            return CommandLine.GenerateCredentialConfig();
        }

        var shouldGenerateDkimConfig = args.Any(arg => arg.StartsWith("--generate-dkim-config"));
        if (shouldGenerateDkimConfig)
        {
            return CommandLine.GenerateDkimConfig();
        }

        return null;
    }
    public static void ParseModifiersArgs(string[] args, WebApplication app)
    {
        var shouldMigrate = args.Any(arg => arg.StartsWith("--migrate-database"));
        if (shouldMigrate)
        {
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("Applyging migrations");
                    dbContext.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed migration(s): '{ex.Message}'.");
                Environment.Exit(-1);
            }
        }
    }

    public static int GenerateDkimConfig()
    {
        var iteration = DateTime.Now.Ticks.ToString();

        Console.Write($"Generating a private key... ");
        var createPrivKeyProc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "openssl",
                Arguments = $"genrsa -out dkim_private.{iteration}.pem 2048",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = "."
            }
        };

        if (!createPrivKeyProc.Start())
        {
            Console.WriteLine("could not execute openssl, exiting");
            return -1;
        }

        createPrivKeyProc.WaitForExit();

        if (!File.Exists($"dkim_private.{iteration}.pem"))
        {
            Console.WriteLine($"dkim_private.{iteration}.pem was not correctly created, exiting");
            return -1;
        }
        Console.WriteLine($"Done!");

        Console.Write($"Generating the public key... ");
        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "openssl",
                Arguments = $"rsa -in dkim_private.{iteration}.pem -pubout -outform der -out dkim_pub.{iteration}.der",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = "."
            }
        };

        if (!proc.Start())
        {
            Console.WriteLine("could not execute openssl, exiting");
            return -1;
        }

        proc.WaitForExit();

        if (!File.Exists($"dkim_pub.{iteration}.der"))
        {
            Console.WriteLine($"dkim_pub.{iteration}.der was not correctly created, exiting");
            return -1;
        }
        Console.WriteLine($"Done!");

        var pubKeyBytes = File.ReadAllBytes($"dkim_pub.{iteration}.der");
        var pubKeyBase64 = Convert.ToBase64String(pubKeyBytes);

        Console.WriteLine("private/public key pair generated.");
        Console.Write("enter the domain name: ");
        var domain = Console.ReadLine();
        Console.Write("enter a selector for the DKIM process: ");
        var selector = Console.ReadLine();

        Console.WriteLine("Create a TXT record in the DNS like this: ");
        Console.WriteLine($" -Name: {selector}._domainkey.{domain}");
        Console.WriteLine($" -Value: \"{pubKeyBase64}\"");

        Console.WriteLine("Adjuste the config file and make sure it contains the following: ");
        Console.WriteLine(JsonConvert.SerializeObject(new
        {
            DKIM = new
            {
                Selector = selector,
                Domain = domain,
                PrivateKey = $"dkim_private.{iteration}.pem"
            }
        }, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        }));

        Environment.Exit(0);
        return 0;
    }

    public static int GenerateCredentialConfig()
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

}