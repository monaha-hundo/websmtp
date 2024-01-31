
using SmtpServer.Storage;
using websmtp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<IMessageStore, MessageStore>();
builder.Services.AddTransient<IReadableMessageStore, MessageStore>();
builder.Services.AddHostedService<SmtpBackgroundServerService>();
builder.Services.AddHostedService<MailAiReplyService>();

var app = builder.Build();

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

app.MapGet("/api/messages/{msgId}/attachements/{filename}", MessagesEndpoints.GetMessageAttachement);
app.MapGet("/api/messages/{msgId}.html", MessagesEndpoints.GetMessage);

await Task.Run(async () => await InitialisationTasks.InitMessageStore(app));

app.Run();

public partial class Program { } // To enable testing