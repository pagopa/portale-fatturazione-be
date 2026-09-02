using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;


/// <summary>
/// DTO per la gestione del report delle fatture. 
/// </summary>
public sealed class GestioneFattureReportDto
{
    public string? IdEnte { get; set; }

    // /!\ corretta Ragione Sociale nella SELECT della view, in quanto il nome della colonna nella vista è "Ragione Sociale" e non "RagioneSociale"
    //[Column("Ragione Sociale")] public string? RagioneSociale { get; set; }
    public string? RagioneSociale { get; set; }

    public string? IdContratto { get; set; }
    public string? TipologiaFattura { get; set; }
    public long? NumeroFattura { get; set; } // ft.Progressivo è bigint
    public string? TipoDocumento { get; set; }
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public int? TotaleNotificheAnalogiche { get; set; }
    public int? TotaleNotificheDigitali { get; set; }
    public int? TotaleNotifiche { get; set; }
    public decimal? TotaleImponibileAnalogico { get; set; }
    public decimal? TotaleImponibileDigitale { get; set; }
    public decimal? TotaleImponibile { get; set; }
    public decimal? TotaleIvatoAnalogico { get; set; }
    public decimal? TotaleIvatoDigitale { get; set; }
    public decimal? TotaleIvato { get; set; }
    public string? Firmata { get; set; } // 'Firmata' / 'Non Caricata' (CASE nella vista)

    //public double? TotaleFatturaImponibile { get; set; } /!\ verificare se è corretto usare decimal o double, in quanto float può portare a problemi di precisione
    public decimal? TotaleFatturaImponibile { get; set; } // ft.TotaleFattura è float nel DDL reale 

    public string? TipoContratto { get; set; }
    public string? Stato { get; set; }
}