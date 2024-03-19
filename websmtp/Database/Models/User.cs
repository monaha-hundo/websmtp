using System.ComponentModel.DataAnnotations;

namespace websmtp.Database.Models;

public class User
{
    public int Id { get; set; }
    [StringLength(1000)] public string Username { get; set; } = string.Empty;
    [StringLength(1000)] public string PasswordHash { get; set; } = string.Empty;
    [StringLength(20)] public string OtpSecret { get; set; } = string.Empty;
    public bool OtpEnabled { get; set; }
    [StringLength(100)] public string Roles { get; set; } = string.Empty;

    public ICollection<UserMailbox> Mailboxes { get; set; } = [];

    public bool Deleted { get; set; }
}
