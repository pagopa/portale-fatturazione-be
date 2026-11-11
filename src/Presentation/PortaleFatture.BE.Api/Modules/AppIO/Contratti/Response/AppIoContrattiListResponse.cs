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
    /// Identificativo del contratto (onboardingtokenid).
    /// </summary>
    public string? ContractId { get; set; }

    /// <summary>
    /// Identificativo dell'ente.
    /// </summary>
    public string? InstitutionId { get; set; }

    /// <summary>
    /// Nome dell'ente titolare del contratto.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Prodotto del contratto.
    /// </summary>
    public string? Prodotto { get; set; }

    /// <summary>
    /// Tipo dell'ente (es. PA, GSP).
    /// </summary>
    public string? InstitutionType { get; set; }

    /// <summary>
    /// Indirizzo PEC dell'ente; null se assente.
    /// </summary>
    public string? EmailPec { get; set; }

    /// <summary>
    /// Stato del contratto (es. ACTIVE, SUSPENDED).
    /// </summary>
    public string? ContractState { get; set; }
}
