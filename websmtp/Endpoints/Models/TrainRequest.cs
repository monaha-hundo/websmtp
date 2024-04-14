namespace websmtp;

public class TrainRequest
{
    public List<Guid> MsgsIds { get; set; } = [];
    public bool Spam { get; set; }
}