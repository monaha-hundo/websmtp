public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Subject { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string TextContent { get; set; } = string.Empty;
    public string Raw { get; set; } = string.Empty;
    public List<(string, byte[])> Attachements { get; set; } = new();
    public int Size => System.Text.Encoding.Default.GetByteCount(Raw);
}
