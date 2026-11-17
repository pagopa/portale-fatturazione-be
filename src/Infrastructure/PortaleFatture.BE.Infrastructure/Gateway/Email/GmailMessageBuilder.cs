using MimeKit;

namespace PortaleFatture.BE.Infrastructure.Gateway.Email;

/// <summary>
/// Composizione del messaggio per l'API Gmail, condivisa da <see cref="PspEmailSender"/> e
/// <see cref="AlertSender"/>: il messaggio MIME e la sua codifica "raw" (base64url senza padding)
/// richiesta da <c>users.messages.send</c>. E' la sola parte dei due sender che non chiama Google,
/// quindi l'unica verificabile senza rete.
/// </summary>
public static class GmailMessageBuilder
{
    public static MimeMessage Componi(
        string? fromName,
        string? from,
        string? toName,
        string? to,
        string? subject,
        string? body)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(fromName, from));
        mimeMessage.To.Add(new MailboxAddress(toName, to));
        mimeMessage.Subject = subject;
        mimeMessage.Body = new TextPart("html")
        {
            Text = body
        };
        return mimeMessage;
    }

    public static string ToRaw(MimeMessage mimeMessage)
    {
        using var memoryStream = new MemoryStream();
        mimeMessage.WriteTo(memoryStream);
        return Convert.ToBase64String(memoryStream.ToArray())
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }
}
