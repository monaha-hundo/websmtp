namespace websmtp;

public class ChangeUsernameRequest
{
    public int UserId { get; set; }
    public string NewUsername { get; set; } = string.Empty;
}