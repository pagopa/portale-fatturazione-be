namespace PortaleFatture.BE.Api.Modules.AppIO.Contratti.Request;

/// <summary>
/// Filtri della griglia e del download dei contratti APP IO (api/appio/contracts e api/appio/contracts/download).
/// </summary>
public class AppIoContrattiRequest
{
    /// <summary>
    /// Contratti da cercare; null o vuoto = nessun filtro.
    /// </summary>
    public string[]? ContractIds { get; set; }

    /// <summary>
    /// Trimestri da cercare, nel formato 'AAAA_T'; null o vuoto = solo il trimestre più recente.
    /// </summary>
    public string[]? Quarters { get; set; }
}
