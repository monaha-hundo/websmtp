using Microsoft.AspNetCore.Mvc;

namespace websmtp;

public static class MessagesEndpoints
{
    public static IResult GetMessage(
    [FromRoute] Guid msgId,
    [FromServices] IReadableMessageStore messages
)
    {
        var message = messages.Single(msgId) ?? throw new Exception("Could not find message");
        if (!string.IsNullOrWhiteSpace(message.HtmlContent))
        {
            var contentBytes = Convert.FromBase64String(message.HtmlContent);
            var html = System.Text.Encoding.Default.GetString(contentBytes);
            var mimeType = "text/plain";
            return Results.Content(html, mimeType);
        }
        if (!string.IsNullOrWhiteSpace(message.TextContent))
        {
            var mimeType = "text";
            return Results.Content(message.TextContent, mimeType);
        }
        throw new Exception("Message had neither HtmlContent or TextContent.");
    }

    public static IResult GetMessageAttachement(
        [FromRoute] Guid msgId,
        [FromRoute] string filename,
        [FromServices] IReadableMessageStore messages
    )
    {
        var message = messages.Single(msgId);
        var attachement = message.Attachements.Single(a => a.Filename == filename);
        var contentBytes = Convert.FromBase64String(attachement.Content);
        var mimeType = attachement.MimeType;
        return Results.File(contentBytes, mimeType, filename);
    }

    public static IResult MarkAsRead(
        [FromRoute] Guid msgId,
        [FromServices] IReadableMessageStore messages
    )
    {
        messages.MarkAsRead(msgId);
        return Results.Ok();
    }

    public static IResult Delete(
        [FromRoute] Guid msgId,
        [FromServices] IReadableMessageStore messages
    )
    {
        messages.Delete(msgId);
        return Results.Ok();
    }

    public static IResult Undelete(
        [FromRoute] Guid msgId,
        [FromServices] IReadableMessageStore messages
    )
    {
        messages.Undelete(msgId);
        return Results.Ok();
    }
}
