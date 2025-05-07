namespace PortaleFatture.BE.Function.Api.Models;

public sealed class RELDataResponse
{
    public Guid InternalOrganizationId { get; set; }
    public Guid ContractId { get; set; }
    public string? TipologiaFattura { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotaleAnalogico { get; set; }
    public decimal TotaleDigitale { get; set; }
    public int TotaleNotificheAnalogiche { get; set; }
    public int TotaleNotificheDigitali { get; set; }
    public decimal Totale { get; set; }
    public decimal Iva { get; set; }
    public decimal TotaleAnalogicoIva { get; set; }
    public decimal TotaleDigitaleIva { get; set; }
    public decimal TotaleIva { get; set; }
    public bool? Caricata { get; set; }
    public decimal? AsseverazioneTotaleAnalogico { get; set; }
    public decimal? AsseverazioneTotaleDigitale { get; set; }
    public int? AsseverazioneTotaleNotificheAnalogiche { get; set; }
    public int? AsseverazioneTotaleNotificheDigitali { get; set; }
    public decimal? AsseverazioneTotale { get; set; }
    public decimal? AsseverazioneTotaleAnalogicoIva { get; set; }
    public decimal? AsseverazioneTotaleDigitaleIva { get; set; }
    public decimal? AsseverazioneTotaleIva { get; set; }
    public bool RelFatturata { get; set; }
} 