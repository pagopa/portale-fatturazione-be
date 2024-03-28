using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.DatiRel;

public class RelTestata
{ 
    public string? IdTestata
    {
        get
        {
            return new RelTestataKey(IdEnte, IdContratto, TipologiaFattura, Anno, Mese).ToString();
        }
    } 

    [Column("internal_organization_id")]
    public string? IdEnte { get; set; }

    [Column("description")]
    public string? RagioneSociale { get; set; }

    [Column("contract_id")]
    public string? IdContratto { get; set; }

    [Column("TipologiaFattura")]
    public string? TipologiaFattura { get; set; }

    [Column("year")]
    public int? Anno { get; set; }

    [Column("month")]
    public int? Mese { get; set; }

    [Column("TotaleAnalogico")]
    public decimal TotaleAnalogico { get; set; }

    [Column("TotaleDigitale")]
    public decimal? TotaleDigitale { get; set; }

    [Column("TotaleNotificheAnalogiche")]
    public int TotaleNotificheAnalogiche { get; set; }

    [Column("TotaleNotificheDigitali")]
    public int TotaleNotificheDigitali { get; set; }

    [Column("Totale")]
    public decimal Totale { get; set; }

    [Column("Iva")]
    public decimal Iva { get; set; }

    [Column("TotaleAnalogicoIva")]
    public decimal TotaleAnalogicoIva { get; set; }

    [Column("TotaleDigitaleIva")]
    public decimal TotaleDigitaleIva { get; set; }

    [Column("TotaleIva")]
    public decimal TotaleIva { get; set; }
}  