namespace PortaleFatture.BE.Api.Modules.AppIO.Contratti.Response;

/// <summary>
/// Voce del filtro "Nome Ente" dei contratti APP IO.
/// </summary>
public class AppIoContrattoNomeResponse
{
    /// <summary>
    /// Identificativo del contratto, da rimandare nel filtro ContractIds della griglia.
    /// </summary>
    public string? ContractId { get; set; }

    /// <summary>
    /// Nome dell'ente titolare del contratto.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Anno e trimestre di riferimento, nel formato 'AAAA_T'.
    /// </summary>
    public string? YearQuarter { get; set; }
}
