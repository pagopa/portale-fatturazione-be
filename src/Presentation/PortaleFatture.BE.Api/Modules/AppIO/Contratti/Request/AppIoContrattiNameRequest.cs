namespace PortaleFatture.BE.Api.Modules.AppIO.Contratti.Request;

/// <summary>
/// Filtri della ricerca dei contratti APP IO per nome dell'ente (api/appio/contracts/name).
/// </summary>
public class AppIoContrattiNameRequest
{
    /// <summary>
    /// Testo da cercare nel nome dell'ente; null = nessun filtro sul nome.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Trimestri in cui cercare, nel formato 'AAAA_T'; null o vuoto = solo il trimestre più recente.
    /// </summary>
    public string[]? Quarters { get; set; }
}
