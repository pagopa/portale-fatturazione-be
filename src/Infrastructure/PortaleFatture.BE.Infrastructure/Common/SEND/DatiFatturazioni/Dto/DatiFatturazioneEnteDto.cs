using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

[Table("DatiFatturazione")]
public class DatiFatturazioneEnteDto
{
    [JsonPropertyOrder(-6)]
    public string? Key
    {
        get
        {
            return $"{IdEnte}_{Prodotto}_{Profilo}";
        }
    }

    [Column("Description")]
    [JsonPropertyOrder(-5)]
    [Header(caption: "Ragione Sociale", Order = 1)]
    public string? RagioneSociale { get; set; }

    [Column("FkIdEnte")]
    [JsonPropertyOrder(-4)]
    public string? IdEnte { get; set; }

    [Column("internalistitutionid")]
    [JsonIgnore]
    public string? InternalInstituitionId { get; set; }

    [Column("product")]
    [JsonIgnore]
    [Header(caption: "Prodotto", Order = 2)]
    public string? InternalProduct { get; set; }

    [Column("institutionType")]
    [JsonPropertyOrder(-2)]
    [Header(caption: "Profilo", Order = 3)]
    public string? Profilo { get; set; }

    [Column("IdDatiFatturazione")]
    [JsonPropertyOrder(-1)]
    public long? Id { get; set; }

    [Column("FkTipoCommessa")]
    public string? TipoCommessa { get; set; }

    [Column("FkProdotto")]
    [JsonPropertyOrder(-3)]
    public string? Prodotto { get; set; }

    [Header(caption: "Cup", Order = 4)]
    public string? Cup { get; set; }

    [Header(caption: "Cod. Commessa", Order = 5)]
    public string? CodCommessa { get; set; }
    public DateTime? DataDocumento { get; set; }
    public bool? SplitPayment { get; set; }

    [Header(caption: "Id Documento", Order = 6)]
    public string? IdDocumento { get; set; }
    public string? Map { get; set; }

    [Header(caption: "Pec", Order = 9)]
    public string? Pec { get; set; }
    public bool? NotaLegale { get; set; }
    public DateTimeOffset? DataCreazione { get; set; }
    public DateTimeOffset? DataModifica { get; set; }


    [JsonIgnore]
    [Header(caption: "Data Documento", Order = 7)]
    public string? SDataDocumento { get { return DataDocumento?.ToString("d"); } }

    [Header(caption: "Split Payment", Order = 8)]
    [JsonIgnore]
    public string? SSplitPayment { get { return SplitPayment.To(); } }

    [Header(caption: "Nota Legale", Order = 10)]
    [JsonIgnore]
    public string? SNotaLegale { get { return NotaLegale.To(); } }

    [Header(caption: "Data Creazione", Order = 11)]
    [JsonIgnore]
    public string? SDataCreazione { get { return DataCreazione?.ToString("d"); } }

    [Header(caption: "Data Modifica", Order = 12)]
    [JsonIgnore]
    public string? SDataModifica { get { return DataModifica?.ToString("d"); } }

    [Header(caption: "Codice SDI Confermato", Order = 13)]
    [JsonPropertyName("codiceSDI")]
    public string? CodiceSDI { get; set; }

    [Header(caption: "Codice SDI Contratto", Order = 14)]
    [JsonPropertyName("contrattoCodiceSDI")]
    public string? ContrattoCodiceSDI { get; set; }

    [Header(caption: "Tipo Contratto", Order = 15)]
    [JsonPropertyName("tipocontratto")]
    public string? TipoContratto { get; set; }
}


public class DatiFatturazioneEnteWithCountDto
{
    [JsonPropertyOrder(-2)]
    [JsonPropertyName("datiFatturazione")]
    public IEnumerable<DatiFatturazioneEnteDto>? DatiFatturazioneEnte { get; set; }

    [JsonPropertyOrder(-1)]

    [JsonPropertyName("count")]
    public int Count { get; set; }
}