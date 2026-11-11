namespace PortaleFatture.BE.Api.Modules.AppIO.Contratti.Request;

/// <summary>
/// Filtri della griglia e del download dei contratti APP IO (api/appio/contracts e api/appio/contracts/download).
/// </summary>
public class AppIoContrattiRequest
{
    /// <summary>
    /// Enti da cercare (institutionId del filtro "Nome Ente"); null o vuoto = nessun filtro.
    /// </summary>
    public string[]? InstitutionIds { get; set; }

    /// <summary>
    /// Tipi ente da cercare (valori di api/appio/contracts/tipoente); null o vuoto = nessun filtro.
    /// </summary>
    public string[]? TipiEnte { get; set; }
}
