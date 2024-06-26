﻿using Microsoft.AspNetCore.Mvc;

namespace websmtp;

public static partial class MessagesEndpoints
{
    public static IResult Stats(
    [FromServices] IReadableMessageStore messages,
    [FromServices] IConfiguration config
    )
    {
        var stats = messages.Stats();
        return Results.Json(stats);
    }

    public static IResult GetMessage(
    [FromRoute] Guid msgId,
    [FromServices] IReadableMessageStore messages,
    [FromServices] IConfiguration config
    )
    {
        var message = messages.Single(msgId) ?? throw new Exception("Could not find message");
        var displayHtml = config.GetValue<bool>("Security:EnableHtmlDisplay");
        if (!string.IsNullOrWhiteSpace(message.HtmlContent))
        {
            var contentBytes = Convert.FromBase64String(message.HtmlContent);
            var html = System.Text.Encoding.Default.GetString(contentBytes);
            var mimeType = displayHtml ? "text/html" : "text/plain";
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
        [FromBody] List<Guid> msgIds,
        [FromServices] IReadableMessageStore messages
    )
    {
        messages.MarkAsRead(msgIds);
        return Results.Ok();
    }

    public static IResult MarkAsUnread(
        [FromBody] List<Guid> msgIds,
        [FromServices] IReadableMessageStore messages
    )
    {
        messages.MarkAsUnread(msgIds);
        return Results.Ok();
    }

    public static IResult Delete(
        [FromServices] IReadableMessageStore messages,
        [FromBody] List<Guid> msgIds
    )
    {
        messages.Delete(msgIds);
        return Results.Ok();
    }

    public static IResult Undelete(
        [FromServices] IReadableMessageStore messages,
        [FromBody] List<Guid> msgIds
    )
    {
        messages.Undelete(msgIds);
        return Results.Ok();
    }

    public static IResult Star(
        [FromBody] List<Guid> msgIds,
        [FromServices] IReadableMessageStore messages
    )
    {
        messages.Star(msgIds);
        return Results.Ok();
    }
    public static IResult Unstar(
        [FromBody] List<Guid> msgIds,
        [FromServices] IReadableMessageStore messages
    )
    {
        messages.Unstar(msgIds);
        return Results.Ok();
    }

    public static IResult Spam(
        [FromBody] List<Guid> msgIds,
        [FromServices] IReadableMessageStore messages
    )
    {
        messages.Spam(msgIds);
        return Results.Ok();
    }
    public static IResult NotSpam(
        [FromBody] List<Guid> msgIds,
        [FromServices] IReadableMessageStore messages
    )
    {
        messages.NotSpam(msgIds);
        return Results.Ok();
    }

    public static async Task<IResult> Train(
        [FromBody] TrainRequest trainRequest,
        [FromServices] IReadableMessageStore messages
    )
    {
        await messages.TrainSpam(trainRequest.MsgsIds, trainRequest.Spam);
        return Results.Ok();
    }

}
