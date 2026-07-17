
using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

public class SimpleGestioneFattureDto
{
    [HeaderAttributev2(caption: "Ragione Sociale", Order = 1)]
    public string? RagioneSociale { get; set; }

    [HeaderAttributev2(caption: "IdEnte", Order = 2)]
    public string? Ente { get; set; }


    [HeaderAttributev2(caption: "Anno", Order = 4)]
    public int Anno { get; set; }

    public int Mese { get; set; }

    [HeaderAttributev2(caption: "Mese", Order = 5)]
    public string MeseDescrizione
    {
        get
        {
            return Mese.GetMonth();
        }
    }

 
    public DateTime? DataCancellazione { get; set; }

    [HeaderAttributev2(caption: "DataRipristino", Order = 7)]
    public DateTime? DataRipristino { get; set; }

    [HeaderAttributev2(caption: "DataInseriemnto", Order = 8)]

    public DateTime? DataInserimento { get; set; }

    [HeaderAttributev2(caption: "Tipologia Fattura", Order = 6)]
    public string? TipologiaFattura { get; set; }
    public int IdTipoContratto { get; set; }
  
    public string? TipoContratto { get; set; }
    [HeaderAttributev2(caption: "Note", Order = 10)]
    public string? Note { get; set; }

    [HeaderAttributev2(caption: "Azione", Order = 3)]
    public string? Azione { get; set; }


}

public sealed class GestioneFattureListDto
{
    [JsonPropertyOrder(-1)]
    public IEnumerable<SimpleGestioneFattureDto>? GestioneFatture { get; set; }
    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}
