using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests;

public class CustomWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // builder.ConfigureServices(services =>
        // {

        // });
        base.ConfigureWebHost(builder);
    }
    public new HttpClient CreateClient()
    {
        var cookieContainer = new CookieContainer();
        var uri = new Uri("https://localhost:1443/login");
        var httpClientHandler = new HttpClientHandler
        {
            CookieContainer = cookieContainer
        };
        HttpClient httpClient = new HttpClient(httpClientHandler);
        var verificationToken = GetVerificationToken(httpClient, "https://localhost:1443/login");
        var contentToSend = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", "admin"),
            new KeyValuePair<string, string>("password", "cacaCACA123"),
            new KeyValuePair<string, string>("__RequestVerificationToken", verificationToken),
        });
        var response = httpClient.PostAsync("https://localhost:1443/login", contentToSend).Result;
        var cookies = cookieContainer.GetCookies(new Uri("https://localhost:1443/inbox"));
        cookieContainer.Add(cookies);
        var client = new HttpClient(httpClientHandler);
        return client;
    }
    private string GetVerificationToken(HttpClient client, string url)
    {
        HttpResponseMessage response = client.GetAsync(url).Result;
        var verificationToken = response.Content.ReadAsStringAsync().Result;
        if (verificationToken != null && verificationToken.Length > 0)
        {
            verificationToken = verificationToken.Substring(verificationToken.IndexOf("__RequestVerificationToken"));
            verificationToken = verificationToken.Substring(verificationToken.IndexOf("value=\"") + 7);
            verificationToken = verificationToken.Substring(0, verificationToken.IndexOf("\""));
        }
        return verificationToken ?? throw new Exception("Could not get verification token");
    }
}
