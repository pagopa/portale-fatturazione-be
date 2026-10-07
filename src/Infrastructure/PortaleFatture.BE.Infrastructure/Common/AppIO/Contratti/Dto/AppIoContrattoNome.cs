namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;

/// <summary>
/// Esito della ricerca dei contratti APP IO per nome dell'ente: popola il filtro "Nome Ente".
/// </summary>
public sealed class AppIoContrattoNome
{
    /// <summary>
    /// Identificativo del contratto.
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
