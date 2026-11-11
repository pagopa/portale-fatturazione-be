namespace PortaleFatture.BE.Api.Modules.AppIO.Contratti.Request;

/// <summary>
/// Filtro della ricerca degli enti APP IO per nome (api/appio/contracts/name).
/// </summary>
public class AppIoContrattiNameRequest
{
    /// <summary>
    /// Testo da cercare nel nome dell'ente; null = nessun filtro sul nome.
    /// </summary>
    public string? Name { get; set; }
}
