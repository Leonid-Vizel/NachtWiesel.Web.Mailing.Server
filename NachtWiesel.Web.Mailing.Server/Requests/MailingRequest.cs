using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NachtWiesel.Web.Mailing.Server.Jobs;
using NachtWiesel.Web.Mailing.Server.Models;
using Quartz;

namespace NachtWiesel.Web.Mailing.Server.Requests;

public sealed class MailingRequest
{
    public static async Task<IResult> Execute(IServiceProvider provider, HttpRequest request)
    {
        var query = await request.ReadFromJsonAsync<MailingRequestQuery>();
        if (query == null)
        {
            return Results.BadRequest();
        }
        var schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.TriggerJob(MailJob.CreateKey(), new JobDataMap()
        {
            { nameof(MailingRequestQuery), query }
        });
        return Results.Ok();
    }
}