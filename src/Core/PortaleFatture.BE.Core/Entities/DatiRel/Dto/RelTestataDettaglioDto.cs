using System.ComponentModel.DataAnnotations.Schema;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Core.Entities.DatiRel;

public class RelTestataDettaglioDto
{
    public string? IdTestata
    {
        get
        {
            return
                $"{IdEnte}_{IdContratto}_{TipologiaFattura!.Replace(" ", "-")}_{Anno}_{Mese}";
        }
    }

    [Column("internal_organization_id")]
    public string? IdEnte { get; set; }

    [Column("description")]
    public string? RagioneSociale { get; set; }

    [Column("DataDocumento")]
    public DateTime? DataDocumento { get; set; }

    [Column("IdDocumento")]
    public string? IdDocumento { get; set; }

    [Column("Cup")]
    public string? Cup { get; set; }

    [Column("contract_id")]
    public string? IdContratto { get; set; }

    [Column("TipologiaFattura")]
    public string? TipologiaFattura { get; set; }

    [Column("year")]
    public string? Anno { get; set; }

    [Column("month")]
    public string? Mese { get; set; }

    [Column("TotaleAnalogico")]
    public decimal TotaleAnalogico { get; set; }

    [Column("TotaleDigitale")]
    public decimal TotaleDigitale { get; set; }

    [Column("TotaleNotificheAnalogiche")]
    public int TotaleNotificheAnalogiche { get; set; }

    [Column("TotaleNotificheDigitali")]
    public int TotaleNotificheDigitali { get; set; }

    [Column("Totale")]
    public decimal Totale { get; set; }

    public bool DatiFatturazione { get; set; } 

    [Column("Iva")]
    public decimal Iva { get; set; } 

    [Column("TotaleAnalogicoIva")]
    public decimal TotaleAnalogicoIva { get; set; } 

    [Column("TotaleDigitaleIva")]
    public decimal TotaleDigitaleIva { get; set; } 

    [Column("TotaleIva")]
    public decimal TotaleIva { get; set; }
 
    public string Firmata
    {
        get
        {
            return Caricata.MapRelTestata();
        }
    }

    [Column("Caricata")]
    public byte Caricata { get; set; }
}