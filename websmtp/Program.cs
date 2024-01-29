
using Microsoft.AspNetCore.Mvc;
using SmtpServer.Storage;

// Make sure the messages backup directory exists.
if (!Directory.Exists("messages"))
{
    Directory.CreateDirectory("messages");
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<IMessageStore, MessageStore>();
builder.Services.AddTransient<IReadableMessageStore, MessageStore>();
builder.Services.AddHostedService<SmtpBackgroundServerService>();

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.MapGet("/api/messages/{msgId}/attachements/{filename}", (
    [FromRoute] Guid msgId,
    [FromRoute] string filename,
    [FromServices] IReadableMessageStore messages
) =>
{
    var message = messages.Single(msgId);
    var attachement = message.Attachements.Single(a => a.Filename == filename);
    var contentBytes = Convert.FromBase64String(attachement.Content);
    var mimeType = attachement.MimeType;
    return Results.File(contentBytes, mimeType, filename);
});

app.MapGet("/api/messages/{msgId}.html", (
    [FromRoute] Guid msgId,
    [FromServices] IReadableMessageStore messages
) =>
{
    var message = messages.Single(msgId) ?? throw new Exception("Could not find message");
    if (!string.IsNullOrWhiteSpace(message.HtmlContent))
    {
        var contentBytes = Convert.FromBase64String(message.HtmlContent);
        var html = System.Text.Encoding.Default.GetString(contentBytes);
        var mimeType = "text/html";
        return Results.Content(html, mimeType);
    }
    if (!string.IsNullOrWhiteSpace(message.TextContent))
    {
        var mimeType = "text";
        return Results.Content(message.TextContent, mimeType);
    }
    throw new Exception("Message had neither HtmlContent or TextContent.");
});

var appTask = app.RunAsync();

var initTask = Task.Run(async () =>
{
    var scope = app.Services.CreateScope();
    var smtpBgSrv = scope.ServiceProvider.GetRequiredService<IReadableMessageStore>();
    await smtpBgSrv.LoadMessages();
});

Task.WaitAll(appTask, initTask);

public partial class Program { } // To enable testing