namespace NachtWiesel.Web.Mailing.Server.Models;

public sealed record EmailRecipient
{
    public string Email {  get; init; } = null!;
    public string? Name { get; init; } = null!;

    public EmailRecipient(string email, string? name)
    {
        Email = email;
        Name = name;
    }
}
