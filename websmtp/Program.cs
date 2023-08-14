
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

app.Run();

public partial class Program {}