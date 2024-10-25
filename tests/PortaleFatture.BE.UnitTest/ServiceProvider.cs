using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari;

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

        PortaleFattureOptions options = new();
        configuration.GetSection(nameof(PortaleFattureOptions)).Bind(options);

        services.AddSingleton<IPortaleFattureOptions>(options);

        var sconf = configuration!.GetSection(nameof(PortaleFattureOptions));
        services.Configure<PortaleFattureOptions>(o => { o.SelfCareCertEndpoint = configuration.GetSection("PortaleFattureOptions:SelfCareCertEndpoint").Value;
            o.ConnectionString = configuration.GetSection("PortaleFattureOptions:ConnectionString").Value;
            o.SelfCareUri = configuration.GetSection("PortaleFattureOptions:SelfCareUri").Value;
            }); 

        var dbConnectionString = options.ConnectionString ??
                      throw new ConfigurationException("Db connection string not configured");

        services.AddSingleton<ISelfCareDbContextFactory>(new DbContextFactory(dbConnectionString, "pfd"));
        services.AddSingleton<IFattureDbContextFactory>(new DbContextFactory(dbConnectionString, "pfw"));
        services.AddSingleton<IScadenziarioService, ScadenziarioService>();

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