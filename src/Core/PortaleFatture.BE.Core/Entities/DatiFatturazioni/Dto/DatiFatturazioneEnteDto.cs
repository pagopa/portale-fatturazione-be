using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Core.Entities.DatiFatturazioni.Dto;

[Table("DatiFatturazione")]
public class DatiFatturazioneEnteDto
{
    [Column("Description")]
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
    public string? RagioneSociale { get; set; }

    [Column("FkIdEnte")]
    [JsonPropertyOrder(-4)]
    public string? IdEnte { get; set; }

    [Column("internalistitutionid")]
    [JsonIgnore]
    public string? InternalInstituitionId { get; set; }

    [Column("product")]
    [JsonIgnore]
    public string? InternalProduct { get; set; }

    [Column("institutionType")]
    [JsonPropertyOrder(-2)]
    public string? Profilo { get; set; }

    [Column("IdDatiFatturazione")]
    [JsonPropertyOrder(-1)]
    public long? Id { get; set; }

    [Column("FkTipoCommessa")]
    public string? TipoCommessa { get; set; }

    [Column("FkProdotto")]
    [JsonPropertyOrder(-3)]
    public string? Prodotto { get; set; }
    public string? Cup { get; set; }
    public string? CodCommessa { get; set; }
    public DateTime? DataDocumento { get; set; }
    public bool? SplitPayment { get; set; }
    public string? IdDocumento { get; set; }
    public string? Map { get; set; }
    public string? Pec { get; set; }
    public bool? NotaLegale { get; set; }
    public DateTimeOffset? DataCreazione { get; set; }
    public DateTimeOffset? DataModifica { get; set; }
}