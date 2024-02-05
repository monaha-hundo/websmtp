using Newtonsoft.Json;
using SmtpServer.Storage;
using websmtp;

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

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", false, true);
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.development.json", false, true);
}
else
{
    builder.Configuration.AddJsonFile("appsettings.production.json", true, true);
}

builder.Services.AddAntiforgery();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication().AddCookie(otps =>
{
    otps.LoginPath = "/login";
    otps.AccessDeniedPath = "/error/";
    otps.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    otps.SlidingExpiration = true;
});
builder.Services.AddAuthorization();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<IMessageStore, MessageStore>();
builder.Services.AddTransient<IReadableMessageStore, MessageStore>();
builder.Services.AddHostedService<SmtpBackgroundServerService>();
//builder.Services.AddHostedService<MailAiReplyService>();

var app = builder.Build();

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

app.MapGet("/api/messages/{msgId}/attachements/{filename}", MessagesEndpoints.GetMessageAttachement).RequireAuthorization();
app.MapGet("/api/messages/{msgId}.html", MessagesEndpoints.GetMessage).RequireAuthorization();

await Task.Run(async () => await InitialisationTasks.InitMessageStore(app));

app.Run();

return 0;

public partial class Program { } // To enable testing