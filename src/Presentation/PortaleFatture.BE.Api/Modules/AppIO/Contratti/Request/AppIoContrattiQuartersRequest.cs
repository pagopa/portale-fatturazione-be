namespace PortaleFatture.BE.Api.Modules.AppIO.Contratti.Request;

/// <summary>
/// Filtro dei trimestri dei contratti APP IO (api/appio/contracts/quarters).
/// </summary>
public class AppIoContrattiQuartersRequest
{
    /// <summary>
    /// Anno di cui restituire i trimestri (es. "2026"); null o vuoto = tutti i trimestri.
    /// </summary>
    public string? Year { get; set; }
}
