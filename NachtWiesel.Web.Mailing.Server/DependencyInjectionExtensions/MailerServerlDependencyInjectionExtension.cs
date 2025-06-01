using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NachtWiesel.Web.Mailing.Server.Jobs;
using NachtWiesel.Web.Mailing.Server.Models;
using NachtWiesel.Web.Mailing.Server.Requests;
using NachtWiesel.Web.Mailing.Server.Services;
using Quartz;

namespace NachtWiesel.Web.Mailing.Server.DependencyInjectionExtensions;

public static class MailerServerlDependencyInjectionExtension
{
    public static IHostApplicationBuilder AddMailingServer(this IHostApplicationBuilder builder)
    {
        LoadConfiguration(builder.Services, builder.Configuration, builder.Environment);
        builder.Services.AddTransient<IEmailService, EmailService>();
        builder.Services.AddQuartz(options =>
        {
            MailJob.ConfigureFor(options);
            options.UseDefaultThreadPool(20, x =>
            {
                x.MaxConcurrency = 20;
            });
        });
        builder.Services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = false;
        });
        return builder;
    }

    public static WebApplication UseMailingServer(this WebApplication app)
    {
        app.MapPost("/Send", MailingRequest.Execute);
        return app;
    }

    private static void LoadConfiguration(IServiceCollection services, IConfigurationManager configuration, IHostEnvironment environment)
    {
        string finalSectionName = environment.EnvironmentName;
        var finalSection = configuration.GetSection("Email").GetChildren().FirstOrDefault(x => x.Key == finalSectionName);
        if (finalSection == null)
        {
            throw new Exception($"Section {finalSectionName} not found inside Email section");
        }
        var config = new EmailServiceConfig();
        finalSection.Bind(config);
        services.AddSingleton(config);
    }
}
