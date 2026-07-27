using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.UnitTest;

public static class ServiceProvider
{
    private static IServiceProvider Provider(
        string? connectionStringOverride = null,
        Action<IServiceCollection>? configureOverrides = null)
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

        var dbConnectionString = connectionStringOverride ?? options.ConnectionString ??
                      throw new ConfigurationException("Db connection string not configured");

        services.AddSingleton<ISelfCareDbContextFactory>(new DbContextFactory(dbConnectionString, "pfd"));
        services.AddSingleton<IFattureDbContextFactory>(new DbContextFactory(dbConnectionString, "pfw"));
        services.AddSingleton<IScadenziarioService, ScadenziarioService>();

        // Gateway HTTP esterni: nessuna chiamata reale.
        // NON usare un Mock "nudo": un Mock non configurato restituisce il default della tupla,
        // cioe' (Success: false), e ogni handler che verifica il recipient code (es.
        // DatiFatturazioneCreateCommandHandler) fallirebbe con ValidationException prima di
        // toccare il DB. Usiamo le implementazioni fake gia' presenti nel prodotto, che
        // rispondono "ok" — il comportamento che i test happy-path si aspettano.
        services.AddSingleton<ISelfCareOnBoardingHttpClient, MockSelfCareOnBoardingHttpClient>();
        services.AddSingleton<ISupportAPIServiceHttpClient, MockSupportAPIServiceHttpClient>();

        services.AddMediatR(x => x.RegisterServicesFromAssembly(typeof(RootInfrastructure).Assembly));

        // Console provider: rende visibili nell'output di dotnet test i log degli handler
        // (utile quando un handler cattura l'eccezione vera e rilancia una DomainException generica).
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));

        services.AddLocalization(options => options.ResourcesPath = "Resources");

        // Ultimo, cosi' un test puo' sostituire qualunque registrazione (es. un gateway che
        // risponde "ko" per coprire il ramo di errore).
        configureOverrides?.Invoke(services);

        return services.BuildServiceProvider();
    }

    public static T GetRequiredService<T>() where T : class
    {
        var provider = Provider();
        return provider.GetRequiredService<T>();
    }

    /// <summary>
    /// Come GetRequiredService, ma con una connection string alternativa (es. il DB locale seeded
    /// avviato da tests/docker-compose.yml). Usato dai test che richiedono un DB reale.
    /// </summary>
    public static T GetRequiredService<T>(string connectionString) where T : class
    {
        var provider = Provider(connectionString);
        return provider.GetRequiredService<T>();
    }

    /// <summary>
    /// Come sopra, ma consente di sostituire registrazioni (tipicamente i gateway HTTP) per
    /// coprire i rami di errore senza toccare la configurazione condivisa.
    /// </summary>
    public static T GetRequiredService<T>(string connectionString, Action<IServiceCollection> configureOverrides) where T : class
    {
        var provider = Provider(connectionString, configureOverrides);
        return provider.GetRequiredService<T>();
    }
}