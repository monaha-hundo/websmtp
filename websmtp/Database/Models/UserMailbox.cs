using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using websmtp.Database.Models;

namespace websmtp;

public class UserMailbox
{
    public int Id { get; set; }
    public int UserId { get; set; }
    [ForeignKey("UserId")] public User User { get; set; } = null!;
    [StringLength(1000)] public string Identity { get; set; } = string.Empty;
    [StringLength(1000)] public string Host { get; set; } = string.Empty;
    [StringLength(256)] public string DisplayName { get; set; } = string.Empty;
}
