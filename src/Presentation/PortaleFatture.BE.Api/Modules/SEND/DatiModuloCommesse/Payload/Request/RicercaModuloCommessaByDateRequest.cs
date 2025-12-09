
using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Extensions; 

namespace PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Request;

public sealed class RicercaModuloCommessaByDateRequest
{
    [JsonPropertyName("dataInizioModulo")]
    public DateTime? DataInizioModulo { get; set; }

    [JsonPropertyName("dataFineModulo")]
    public DateTime? DataFineModulo { get; set; }

    private string[]? _idEnti;
    [JsonPropertyName("idEnti")]
    public string[]? IdEnti
    {
        get { return _idEnti; }
        set { _idEnti = value!.IsNullNotAny() ? null : value; }
    }

    [JsonPropertyName("idTipoContratto")]
    public int? IdTipoContratto { get; set; }

    [JsonPropertyName("dataInizioContratto")]
    public DateTime? DataInizioContratto { get; set; }

    [JsonPropertyName("dataFineContratto")]
    public DateTime? DataFineContratto { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("size")]
    public int? Size { get; set; }
}