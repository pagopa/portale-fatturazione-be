using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.UnitTest;

public static class ServiceProvider
{
    private static IServiceProvider Provider()
    {
        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets("d0dd11cf-4318-484a-844d-058470676f42")
            .AddEnvironmentVariables()
            .Build();

        services.AddSingleton<IConfiguration>(configurationBuilder);

        var configuration = services
            .BuildServiceProvider()
            .GetRequiredService<IConfiguration>();

        var sconf = configuration!.GetSection(nameof(PortaleFattureOptions));
        services.Configure<PortaleFattureOptions>(o => { o.SelfCareCertEndpoint = configuration.GetSection("PortaleFattureOptions:SelfCareCertEndpoint").Value;
            o.ConnectionString = configuration.GetSection("PortaleFattureOptions:ConnectionString").Value;
            o.SelfCareUri = configuration.GetSection("PortaleFattureOptions:SelfCareUri").Value;
            });

        var options = services
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<PortaleFattureOptions>>();

        var dbConnectionString = options.CurrentValue.ConnectionString ??
                      throw new ConfigurationException("Db connection string not configured");

        services.AddSingleton<IFattureDbContextFactory>(new DbContextFactory(dbConnectionString, "pfw"));

        services.AddMediatR(x => x.RegisterServicesFromAssembly(typeof(RootInfrastructure).Assembly));

        services.AddLogging();

        services.AddLocalization(options => options.ResourcesPath = "Resources");

        return services.BuildServiceProvider();
    }

    public static T GetRequiredService<T>() where T : class
    {
        var provider = Provider();
        return provider.GetRequiredService<T>();
    }
}