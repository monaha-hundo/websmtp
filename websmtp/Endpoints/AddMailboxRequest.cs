namespace websmtp;

public static partial class MessagesEndpoints
{
    public class AddMailboxRequest
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
