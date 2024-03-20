using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OtpNet;
using websmtp;
using websmtp.Database;

namespace MyApp.Namespace;

[AllowAnonymous]
[ValidateAntiForgeryToken]
public class MfaModel : PageModel
{
    public IHttpContextAccessor _http { get; set; }
    public IConfiguration _conf { get; set; }
    public ILogger<MfaModel> _logger;
    private readonly DataContext _data;
    [FromForm] public string OTP { get; set; } = string.Empty;
    [FromQuery] public string? ReturnUrl { get; set; }
    public bool Error { get; set; }

    public MfaModel(IHttpContextAccessor http, IConfiguration conf, DataContext data, ILogger<MfaModel> logger)
    {
        _http = http;
        _conf = conf;
        _data = data;
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        if (!TempData.ContainsKey("userId"))
        {
            return Redirect("/error");
        }
        TempData.Keep("userId");
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        try
        {
            if (!TempData.ContainsKey("userId"))
            {
                return Redirect("/error");
            }

            if (string.IsNullOrWhiteSpace(OTP) || string.IsNullOrWhiteSpace(OTP))
            {
                Error = true;
                return Page();
            }

            var isOtpValid = CheckOtp();

            var userId = (int?)TempData["userId"] ?? throw new Exception("Invalid user id in temp data.");
            var user = _data.Users.Single(u => !u.Deleted && u.Id == userId);

            if (isOtpValid)
            {
                await _http.SignInAsync(user);
                return Redirect("/inbox");
            }

            Error = true;
            TempData.Keep("userId");
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Exception during OTP validation: " + ex.Message);
            Error = true;
            TempData.Keep("userId");
            return Page();
        }
    }

    private bool CheckOtp()
    {
        var userId = (int?)TempData["userId"] ?? throw new Exception("Could not get password authenticatd user id.");
        var user = _data.Users.Single(u => !u.Deleted && u.Id == userId);

        if (user.OtpEnabled)
        {
            var secretBytes = Base32Encoding.ToBytes(user.OtpSecret);
            var totp = new Totp(secretBytes);
            var result = totp.VerifyTotp(OTP, out var timeSteps);
            return result;
        }

        return true;
    }

}