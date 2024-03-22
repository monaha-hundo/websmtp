using Microsoft.EntityFrameworkCore;
using MimeKit;
using Newtonsoft.Json;
using OtpNet;
using System.Diagnostics;
using websmtp.Database;
using websmtp.Database.Models;

namespace websmtp.Startup;

public class CommandLine
{
    /// <summary>
    /// These are executed before the APP is configured/launched.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static int? ParseStartupArgs(string[] args)
    {
        var shouldGenerateDkimConfig = args.Any(arg => arg.StartsWith("--generate-dkim-config"));
        if (shouldGenerateDkimConfig)
        {
            return CommandLine.GenerateDkimConfig();
        }

        return null;
    }

    /// <summary>
    /// These are executed after the APP configuration, but before it launches.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="app"></param>
    public static void ParseModifiersArgs(string[] args, WebApplication app)
    {
        var shouldAddUser = args.Any(arg => arg.StartsWith("--add-user"));
        if (shouldAddUser)
        {
            AddUser(app, args);
        }

        var shouldListUsers = args.Any(arg => arg.StartsWith("--list-users"));
        if (shouldListUsers)
        {
            ListUsers(app);
        }

        var shouldMigrate = args.Any(arg => arg.StartsWith("--migrate-database"));
        if (shouldMigrate)
        {
            MigrateDatabase(app);
            Environment.Exit(0);
        }
    }

    public static void MigrateDatabase(WebApplication app)
    {
        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<CommandLine>>();
                logger.LogInformation("Applying migrations");
                dbContext.Database.Migrate();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed migration(s): '{ex.Message}'.");
            Environment.Exit(-1);
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

    public static int AddUser(WebApplication app, string[] args)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CommandLine>>();
        
        var displayName = args
            .Single(arg => arg.StartsWith("--displayName="))
            .Split("=")[1];
        var username = args
            .Single(arg => arg.StartsWith("--username="))
            .Split("=")[1];
        var passwordToHash = args
            .Single(arg => arg.StartsWith("--password="))
            .Split("=")[1];
        var email = args
            .Single(arg => arg.StartsWith("--email="))
            .Split("=")[1];
        var mailbox = args
            .Single(arg => arg.StartsWith("--mailbox="))
            .Split("=")[1];

        var roles = args
            .Single(arg => arg.StartsWith("--roles="))
            .Split("=")[1];

        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword(passwordToHash);

        var mailboxAddress = new MailboxAddress(displayName, email);

        var newUser = new User
        {
            OtpEnabled = false,
            PasswordHash = hash,
            Username = username,
            Roles = roles,
            Mailboxes = new List<UserMailbox>()
            {
                new UserMailbox
                {
                    DisplayName = displayName,
                    Host = mailboxAddress.Domain,
                    Identity = mailboxAddress.Name,
                }
            },
            Identities = new List<UserIdentity>()
            {
                new UserIdentity
                {
                    DisplayName = displayName,
                    Email = email,
                }
            }
        };

        dbContext.Users.Add(newUser);
        dbContext.SaveChanges();

        Environment.Exit(0);
        return 0;
    }

    public static int ListUsers(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CommandLine>>();

        var users = dbContext.Users
            .Include(u => u.Identities)
            .Include(u => u.Mailboxes)
            .ToList();

        foreach (var user in users)
        {
            Console.WriteLine(JsonConvert.SerializeObject(user, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            }));
        }

        Environment.Exit(0);
        return 0;
    }
}