using MimeKit;

namespace websmtp;

public static class MimeMessageExtentions
{
    public static Tuple<string,string> ExtractNameAndAddress(
        this InternetAddressList addr)
    {
        var ogFrom = addr[0] ?? throw new Exception("Missing from");
        var ogFromMailbox = ogFrom as MailboxAddress ?? throw new Exception("Not a valid mailbox address");
        return new Tuple<string, string>(
            ogFromMailbox.Name,
            ogFromMailbox.Address
        );
    }
    public static MailboxAddress AsMailboxAddress(this InternetAddressList addr)
    {
        var internetAddress = addr[0] ?? throw new Exception("Missing from");
        var mailbox = internetAddress as MailboxAddress ?? throw new Exception("Not a valid mailbox address");
        return mailbox;
    }

    public static string GetName(this InternetAddressList addr)
    {
        return addr.AsMailboxAddress().Name;
    }

    public static string GetAddress(this InternetAddressList addr)
    {
        return addr.AsMailboxAddress().Address;
    }
}
