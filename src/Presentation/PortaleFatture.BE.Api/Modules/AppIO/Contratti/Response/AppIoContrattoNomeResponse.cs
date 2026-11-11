namespace PortaleFatture.BE.Api.Modules.AppIO.Contratti.Response;

/// <summary>
/// Voce del filtro "Nome Ente" APP IO: una per ente.
/// </summary>
public class AppIoContrattoNomeResponse
{
    /// <summary>
    /// Identificativo dell'ente, da rimandare nel filtro InstitutionIds della griglia.
    /// </summary>
    public string? InstitutionId { get; set; }

    /// <summary>
    /// Nome dell'ente.
    /// </summary>
    public string? Name { get; set; }
}
