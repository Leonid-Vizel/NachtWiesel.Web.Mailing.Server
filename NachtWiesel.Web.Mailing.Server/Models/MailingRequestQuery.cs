namespace NachtWiesel.Web.Mailing.Server.Models;

public sealed record MailingRequestQuery
{
    public List<EmailRecipient> Recepients { get; init; } = null!;
    public string Subject { get; init; } = null!;
    public string Body { get; init; } = null!;
    public DateTimeOffset? Offset { get; init; }
}