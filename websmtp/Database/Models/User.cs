using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace websmtp.Database.Models;

[Index("Username", IsUnique = true)]
public class User
{
    public int Id { get; set; }
    [StringLength(1000)] public string Username { get; set; } = string.Empty;
    [StringLength(1000)] public string PasswordHash { get; set; } = string.Empty;
    [StringLength(20)] public string OtpSecret { get; set; } = string.Empty;
    public bool OtpEnabled { get; set; }
    [StringLength(100)] public string Roles { get; set; } = string.Empty;

    public ICollection<UserMailbox> Mailboxes { get; set; } = [];
    public ICollection<UserIdentity> Identities { get; set; } = [];

    public bool Deleted { get; set; }
}
