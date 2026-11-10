using Microsoft.FeatureManagement;

namespace PortaleFatture.BE.Api.Infrastructure;

/// <summary>
/// Feature flag delle funzionalita' nuove (Microsoft.FeatureManagement, sezione "FeatureManagement").
///
/// Regola: <b>flag assente = funzionalita' spenta</b>. Per accenderla in un ambiente serve l'app setting
/// <c>FeatureManagement__&lt;Nome&gt;=true</c> (es. <c>FeatureManagement__AppIO=true</c>).
///
/// Il flag si valuta all'avvio: con il flag spento le rotte NON vengono registrate, quindi non compaiono
/// in Swagger e rispondono 404. Cambiare un app setting riavvia l'App Service, che rilegge i flag.
/// </summary>
public static class FeatureFlags
{
    /// <summary>Prodotto APP IO (PF-550): moduli Modules/AppIO, rotte api/appio/*.</summary>
    public const string AppIO = "AppIO";

    /// <summary>Invio a SAP on-demand (PF-735): rotte api/fatture/invio-sap*.</summary>
    public const string InvioSap = "InvioSap";

    /// <summary>
    /// Invio a SAP legacy (deprecato da PF-735): le 6 rotte api/fatture/resetta/pipeline e api/fatture/invio/*.
    /// Va impostato a true in ogni ambiente finche' il FE non e' passato alle rotte nuove: assente = rotte spente.
    /// </summary>
    public const string InvioSapLegacy = "InvioSapLegacy";

    /// <summary>Tutti i flag noti, nell'ordine in cui il login li restituisce.</summary>
    public static readonly string[] Tutti = [AppIO, InvioSap, InvioSapLegacy];

    /// <summary>Flag accesi fra quelli noti: e' l'elenco restituito al FE nei profili del login admin.</summary>
    public static async Task<List<string>> FeatureAccese(this IFeatureManager featureManager)
    {
        List<string> accese = [];
        foreach (var feature in Tutti)
            if (await featureManager.IsEnabledAsync(feature))
                accese.Add(feature);
        return accese;
    }

    /// <summary>Stato di un flag all'avvio, letto dal provider di configurazione (sincrono di fatto).</summary>
    public static bool IsFeatureEnabled(this IServiceProvider services, string feature)
        => services.GetRequiredService<IFeatureManager>().IsEnabledAsync(feature).GetAwaiter().GetResult();

    /// <inheritdoc cref="IsFeatureEnabled(IServiceProvider, string)"/>
    public static bool IsFeatureEnabled(this IEndpointRouteBuilder endpoints, string feature)
        => endpoints.ServiceProvider.IsFeatureEnabled(feature);
}

/// <summary>
/// Lega un intero modulo a un feature flag: con il flag spento <see cref="ModuleManager"/> non ne mappa
/// le rotte. Per un sottoinsieme di rotte di un modulo usare invece
/// <see cref="FeatureFlags.IsFeatureEnabled(IEndpointRouteBuilder, string)"/> in RegisterEndpoints.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class FeatureFlagAttribute(string feature) : Attribute
{
    public string Feature { get; } = feature;
}
