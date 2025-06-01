using MailKit.Security;

namespace NachtWiesel.Web.Mailing.Server.Models;

public sealed class EmailServiceConfig
{
    public string Host { get; set; } = null!;
    public int Port { get; set; }
    public string Login { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FromName { get; set; } = null!;
    public string FromAddress { get; set; } = null!;
    public string LocalDomain { get; set; } = null!;
    public string Security { get; set; } = "None";
    public bool IgnoreCertificateValidation { get; set; } = false;
    private SecureSocketOptions? _security = null;
    public SecureSocketOptions SecurityMode
    {
        get
        {
            _security ??= Enum.Parse<SecureSocketOptions>(Security);
            return _security.Value;
        }
    }
    public bool Disabled { get; set; }
}
