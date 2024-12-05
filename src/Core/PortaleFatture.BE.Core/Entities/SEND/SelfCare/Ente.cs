using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.SEND.SelfCare;

[Table("Enti")]
public class Ente
{
    [Column("internalistitutionid")]
    public string? IdEnte { get; set; }

    [Column("institutionType")]
    public string? Profilo { get; set; }

    [Column("description")]
    public string? Descrizione { get; set; }

    [Column("digitalAddress")]
    public string? Email { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("zipCode")]
    public string? Cap { get; set; }

    [Column("istatCode")]
    public string? CodiceIstat { get; set; }

    [Column("originId")]
    public string? CodiceIPA { get; set; }

    [Column("city")]
    public string? Citta { get; set; }

    [Column("county")]
    public string? Provincia { get; set; }

    [Column("country")]
    public string? Nazione { get; set; }

    [Column("vatnumber")]
    public string? PartitaIva { get; set; }

    public string? IndirizzoCompleto
    {
        get
        {
            return $"{Address}, {Cap} {Citta} ({Provincia}) - {Nazione}";
        }
    }
}