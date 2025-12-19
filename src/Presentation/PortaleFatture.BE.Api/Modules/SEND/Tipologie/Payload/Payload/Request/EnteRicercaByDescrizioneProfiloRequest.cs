using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Api.Modules.SEND.Tipologie.Payload.Payload.Request;

public sealed class EnteRicercaByDescrizioneProfiloRequest
{

    private string[]? _idEnti;

    [JsonPropertyName("idEnti")]
    public string[]? IdEnti
    {
        get { return _idEnti; }
        set { _idEnti = value!.IsNullNotAny() ? null : value; }
    }

    [JsonPropertyName("prodotto")]
    public string? Prodotto { get; set; }

    [JsonPropertyName("profilo")]
    public string? Profilo { get; set; }

    [JsonPropertyName("idTipoContratto")]
    public int? IdTipoContratto { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("size")]
    public int? Size { get; set; }
}

public sealed class EnteRicercaByRequest
{
    public string? Descrizione { get; set; }
    public string? Prodotto { get; set; }
    public string? Profilo { get; set; }
}