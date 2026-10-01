using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.AppIO.Contratti.Response;

/// <summary>
/// Risposta della griglia dei contratti APP IO: la pagina richiesta e il conteggio totale.
/// </summary>
public class AppIoContrattiListResponse
{
    /// <summary>
    /// Contratti della pagina richiesta.
    /// </summary>
    [JsonPropertyOrder(-1)]
    public IEnumerable<AppIoContrattoResponse>? Contratti { get; set; }

    /// <summary>
    /// Numero totale dei contratti che soddisfano il filtro, indipendente dalla paginazione.
    /// </summary>
    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}

/// <summary>
/// Riga della griglia dei contratti APP IO.
/// </summary>
public class AppIoContrattoResponse
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
    /// Codice fiscale associato al contratto.
    /// </summary>
    public string? TaxCode { get; set; }

    /// <summary>
    /// Partita IVA associata al contratto.
    /// </summary>
    public string? VatCode { get; set; }

    /// <summary>
    /// Gruppo IVA associato al contratto.
    /// </summary>
    public decimal? VatGroup { get; set; }

    /// <summary>
    /// Codice SDI associato al contratto.
    /// </summary>
    public string? SdiCode { get; set; }

    /// <summary>
    /// Anno e mese di riferimento, nel formato 'AAAA-MM'.
    /// </summary>
    public string? YearMonth { get; set; }

    /// <summary>
    /// Anno e trimestre di riferimento, nel formato 'AAAA_T'.
    /// </summary>
    public string? YearQuarter { get; set; }
}
