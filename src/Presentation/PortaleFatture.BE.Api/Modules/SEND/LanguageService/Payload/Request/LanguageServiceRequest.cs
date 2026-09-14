using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Api.Modules.SEND.LanguageService.Payload.Request;

public class LanguageServiceRequest
{
    /// <summary>
    /// Il testo da analizzare. Nel JSON viaggia minuscolo (`{ "testo": "..." }`): a legarlo
    /// e' la naming policy camelCase impostata in `ConfigurationExtensions`, come per ogni
    /// altro request del progetto. Era un **campo pubblico** fino al 04/09/2026, e in quella
    /// forma legava solo grazie a `SerializerOptions.IncludeFields = true` — una dipendenza
    /// silenziosa da una riga di configurazione lontana.
    /// </summary>
    public string? Testo { get; set; }
}