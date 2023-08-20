
using Microsoft.AspNetCore.Mvc;
using SmtpServer;
using SmtpServer.Storage;

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
    var message = messages.Single(msgId);
    var contentBytes = Convert.FromBase64String(message.HtmlContent);
    var html = System.Text.Encoding.Default.GetString(contentBytes);
    var mimeType = "text/html";
    //var filename = $"{msgId}.html";
    return Results.Content(html, mimeType);
});

app.Run();

public partial class Program { }