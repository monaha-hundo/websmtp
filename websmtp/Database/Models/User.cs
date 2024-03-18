using System.ComponentModel.DataAnnotations;

namespace websmtp.Database.Models;

public class User
{
    public int Id { get; set; }
    [StringLength(1000)] public string Username { get; set; } = string.Empty;
    [StringLength(1000)] public string PasswordHash { get; set; } = string.Empty;
    [StringLength(20)] public string OtpSecret { get; set; } = string.Empty;
    [StringLength(100)] public string Roles { get; set; } = string.Empty; // reader, sender, admin

    public ICollection<UserMailbox> Mailboxes { get; set; }
}
