using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using websmtp.Database.Models;

namespace websmtp.Database;

public class DataContext : DbContext
{
    public DbSet<Message> Messages { get; set; }
    public DbSet<RawMessage> RawMessages { get; set; }

    public DbSet<MessageAttachement> MessageAttachements { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserMailbox> Mailboxes { get; set; }

    public DataContext(DbContextOptions<DataContext> options)
        : base(options)
    {
    }
}