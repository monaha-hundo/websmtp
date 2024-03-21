using System.ComponentModel.DataAnnotations.Schema;
using websmtp.Database.Models;

namespace websmtp;

public class UserIdentity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    [ForeignKey("UserId")] public User User { get; set; } = null!;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
