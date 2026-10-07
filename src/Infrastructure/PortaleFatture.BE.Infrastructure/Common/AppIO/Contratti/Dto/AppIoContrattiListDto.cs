using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;

/// <summary>
/// Lista contratti APP IO con il conteggio totale dei contratti che soddisfano il filtro.
/// </summary>
public sealed class AppIoContrattiListDto
{
    /// <summary>
    /// Contratti della lista richiesta (tutti, se la ricerca non è paginata).
    /// </summary>
    [JsonPropertyOrder(-1)]
    public IEnumerable<AppIoContratto>? Contratti { get; set; }

    /// <summary>
    /// Numero totale dei contratti che soddisfano il filtro, indipendente dalla paginazione.
    /// </summary>
    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}
