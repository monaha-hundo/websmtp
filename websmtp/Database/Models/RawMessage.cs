namespace websmtp.Database.Models;

public class RawMessage
{
    public Guid Id { get; set; }
    public byte[] Content { get; set; }
}