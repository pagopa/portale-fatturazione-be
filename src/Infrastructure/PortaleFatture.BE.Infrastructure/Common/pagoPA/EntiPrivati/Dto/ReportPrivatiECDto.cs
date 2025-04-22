using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.EntiPrivati.Dto;

public class ReportPrivatiECDto  
{
    [HeaderPagoPA(caption: "InternalInstitutionId", Order = 2)]
    public string? InternalInstitutionId { get; set; }

    [HeaderPagoPA(caption: "P. IVA", Order = 3)]
    public string? TaxCode { get; set; }

    [HeaderPagoPA(caption: "Ragione Sociale", Order = 1)]
    public string? RagioneSociale { get; set; }
    [HeaderPagoPA(caption: "Codice Articolo", Order = 3)]
    public string? CodiceArticolo { get; set; }

    [HeaderPagoPA(caption: "Totale Async", Order = 4)]
    public int? TotaleAsync { get; set; } // SUM(ASYNC_numero_tot)

    [HeaderPagoPA(caption: "Imponibile Async", Order = 5)]
    public decimal? ValoreAsync { get; set; } // SUM(ASYNC_valore_tot) 

    [HeaderPagoPA(caption: "Totale Sync", Order = 6)]
    public int? TotaleSync { get; set; } // SUM(SYNC_numero_tot)

    [HeaderPagoPA(caption: "Imponibile Sync", Order = 7)]
    public decimal? ValoreSync { get; set; } // SUM(SYNC_valore_tot) 

    public string? YearQuarter { get; set; }

    [HeaderPagoPA(caption: "Imponibile", Order = 8)]
    public decimal? Imponibile { get; set; }

    public ReportPrivatiECDto Clone()
    {
        return (ReportPrivatiECDto)this.MemberwiseClone();
    }
}