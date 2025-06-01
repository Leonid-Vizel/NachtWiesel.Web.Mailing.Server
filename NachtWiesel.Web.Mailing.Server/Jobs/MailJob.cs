using NachtWiesel.Web.Mailing.Server.Models;
using NachtWiesel.Web.Mailing.Server.Services;
using Quartz;

namespace NachtWiesel.Web.Mailing.Server.Jobs;

public sealed class MailJob : IJob
{
    private readonly IEmailService _mailer;
    public MailJob(IEmailService mailer)
    {
        _mailer = mailer;
    }

    public static JobKey CreateKey()
        => JobKey.Create(nameof(MailJob));

    public static void ConfigureFor(IServiceCollectionQuartzConfigurator options)
    {
        var key = CreateKey();
        options.AddJob<MailJob>(key, x =>
        {
            x.StoreDurably(durability: true);
        });
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var query = context.MergedJobDataMap[nameof(MailingRequestQuery)] as MailingRequestQuery;
        if (query == null)
        {
            return;
        }
        await _mailer.SendEmailAsync(query.Recepients, query.Subject, query.Body, query.Offset);
    }
}
