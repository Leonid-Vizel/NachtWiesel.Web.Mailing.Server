using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using NachtWiesel.Web.Mailing.Server.Models;
using System.Net;

namespace NachtWiesel.Web.Mailing.Server.Services;

public interface IEmailService
{
    Task SendEmailAsync(IEnumerable<EmailRecipient> recepients, string subject, string message, DateTimeOffset? offset);
}

public sealed class EmailService : IEmailService
{
    private EmailServiceConfig Config { get; set; }
    private ILogger Logger { get; set; }
    private static int _retryAmount = 50;
    private static int _retryDelay = 10000;
    public EmailService(EmailServiceConfig config, ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger<EmailService>();
        Config = config;
    }

    public async Task SendEmailAsync(IEnumerable<EmailRecipient> recepients, string subject, string message, DateTimeOffset? offset)
    {
        var joinedMails = string.Join(',', recepients.Select(x => x.Email));
        if (string.IsNullOrEmpty(joinedMails))
        {
            Logger.LogWarning($"Requested mail is ignored due to empty recepients (subject:{subject}) (message:{message})");
            return;
        }
        if (Config.Disabled)
        {
            Logger.LogInformation($"[Blocked] Sending email to {joinedMails}");
            return;
        }
        Logger.LogInformation($"Sending email to {joinedMails}");
        var emailMessage = new MimeMessage();
        if (offset != null)
        {
            emailMessage.Date = offset.Value;
        }
        emailMessage.From.Add(new MailboxAddress(Config.FromName, Config.FromAddress));
        foreach (var recepient in recepients)
        {
            emailMessage.To.Add(new MailboxAddress(recepient.Name ?? recepient.Email, recepient.Email));
        }
        emailMessage.Subject = subject;
        emailMessage.Body = new TextPart(TextFormat.Html) { Text = message };
        using (var client = new SmtpClient())
        {
            if (Config.IgnoreCertificateValidation)
            {
                client.ServerCertificateValidationCallback = (s, u, c, k) => true;
            }
            client.LocalDomain = Config.LocalDomain;
            try
            {
                await client.ConnectAsync(Config.Host, Config.Port, Config.SecurityMode).ConfigureAwait(false);
                await client.AuthenticateAsync(new NetworkCredential(Config.Login, Config.Password));
            }
            catch (Exception ex)
            {
                Logger.LogError($"SMTP connection error for {joinedMails}: {ex}");
            }
            int result = await SendInSmtpClient(client, emailMessage, joinedMails, 0);
            try
            {
                await client.DisconnectAsync(true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError($"SMTP disconnection error for {joinedMails}: {ex}");
            }
            if (result > 0)
            {
                Logger.LogWarning($"Letter to {joinedMails} was postponed (Retry amount: {result})");
            }
        }
    }

    private async Task<int> SendInSmtpClient(SmtpClient client, MimeMessage emailMessage, string joinedMailsForLogs, int restartCounter)
    {
        try
        {
            await client.SendAsync(emailMessage).ConfigureAwait(false);
        }
        catch (SmtpCommandException excep)
        {
            if (excep.ErrorCode == SmtpErrorCode.RecipientNotAccepted)
            {
                var needToRemoveAdress = emailMessage.To.Cast<MailboxAddress>().FirstOrDefault(x=>x.Address == excep.Mailbox.Address);
                if (needToRemoveAdress != null)
                {
                    emailMessage.To.Remove(needToRemoveAdress);
                    Logger.LogWarning($"Removed invalid recipient: {needToRemoveAdress.Address}");
                }
                if (emailMessage.To.Count == 0)
                {
                    Logger.LogWarning($"Requested mail is ignored due to empty recepients (subject:{emailMessage.Subject})!");
                    return -1;
                }
            }
            if ((excep.StatusCode == SmtpStatusCode.ErrorInProcessing || excep.StatusCode == SmtpStatusCode.MailboxUnavailable) && restartCounter < _retryAmount)
            {
                Logger.LogWarning($"Letter to {joinedMailsForLogs} is postponed (waiting for {_retryDelay} ms before retry). Reason: {excep}");
                await Task.Delay(_retryDelay);
                return await SendInSmtpClient(client, emailMessage, joinedMailsForLogs, restartCounter + 1);
            }
            else if (restartCounter < _retryAmount)
            {
                Logger.LogError($"Request issue: {excep}");
                return -1;
            }
            else
            {
                Logger.LogError($"Request issue: restart amount > {_retryAmount}; letter ignored");
                return -1;
            }
        }
        return restartCounter;
    }
}
