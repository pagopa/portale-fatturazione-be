using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

public class SimpleWhiteListFatturaEnteDto
{
    public int Id { get; set; }

    [HeaderAttributev2(caption: "Ragione Sociale", Order = 1)]
    public string? RagioneSociale { get; set; }

    [HeaderAttributev2(caption: "IdEnte", Order = 2)]
    public string? IdEnte { get; set; }

    [HeaderAttributev2(caption: "Anno", Order = 3)]
    public int Anno { get; set; }

    public int Mese { get; set; }

    [HeaderAttributev2(caption: "Mese", Order = 4)]
    public string MeseDescrizione
    {
        get
        {
            return Mese.GetMonth();
        }
    }

    [HeaderAttributev2(caption: "Data Inizio", Order = 7)]
    public DateTime? DataInizio { get; set; }
    public DateTime? DataFine { get; set; }
    [HeaderAttributev2(caption: "Tipologia Fattura", Order = 5)]
    public string? TipologiaFattura { get; set; }
    public int IdTipoContratto { get; set; }
    [HeaderAttributev2(caption: "Tipo Contratto", Order = 6)]
    public string? TipoContratto { get; set; }

    public bool cancella = true;
}

public sealed class WhiteListFatturaEnteDto
{
    [JsonPropertyOrder(-1)]
    public IEnumerable<SimpleWhiteListFatturaEnteDto>? Whitelist { get; set; }
    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}