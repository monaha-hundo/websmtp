namespace websmtp;

public static partial class MessagesEndpoints
{
    public class ChangePasswordViewModel
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

}
