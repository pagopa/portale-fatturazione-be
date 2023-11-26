using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.IntegrationTest;

public static class ServiceProvider
{
    private static IServiceProvider Provider()
    {
        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets("27e0801e-8863-4e92-af93-631a5685fed4")
            .AddEnvironmentVariables()
            .Build();
        services.AddSingleton<IConfiguration>(configurationBuilder);

        var configuration = services
            .BuildServiceProvider()
            .GetRequiredService<IConfiguration>(); 

        PortaleFattureOptions options = new();
        configuration.GetSection(nameof(PortaleFattureOptions)).Bind(options);

        services.AddSingleton<IPortaleFattureOptions>(options);

        var sconf = configuration!.GetSection(nameof(PortaleFattureOptions));
        services.Configure<PortaleFattureOptions>(o =>
        {
            o.SelfCareCertEndpoint = configuration.GetSection("PortaleFattureOptions:SelfCareCertEndpoint").Value;
            o.ConnectionString = configuration.GetSection("PortaleFattureOptions:ConnectionString").Value;
            o.SelfCareUri = configuration.GetSection("PortaleFattureOptions:SelfCareUri").Value;
        });
 
        var dbConnectionString = options.ConnectionString ??
                      throw new ConfigurationException("Db connection string not configured");

        services.AddSingleton<IDbContextFactory>(new DbContextFactory(dbConnectionString, "pfw"));

        services.AddMediatR(x => x.RegisterServicesFromAssembly(typeof(RootInfrastructure).Assembly));

        services.AddLogging();

        services.AddLocalization(options => options.ResourcesPath = "Resources");

        services.AddHttpClient();

        services.AddSingleton<ISelfCareHttpClient, SelfCareHttpClient>();
        services.AddSingleton<ISelfCareTokenService, SelfCareTokenService>();

        return services.BuildServiceProvider();
    }

    public static T GetRequiredService<T>() where T : class
    {
        var provider = Provider();
        return provider.GetRequiredService<T>();
    }
}