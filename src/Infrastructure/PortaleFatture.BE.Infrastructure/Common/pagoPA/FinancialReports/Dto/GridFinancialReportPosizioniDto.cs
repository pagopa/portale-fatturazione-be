using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;

public sealed class GridFinancialReportPosizioniDto
{ 
    [HeaderPagoPA(caption: "Category", Order = 1)]
    public string? Category { get; set; }

    [HeaderPagoPA(caption: "ProgressivoRiga", Order = 2)]
    public int ProgressivoRiga { get; set; }

    [HeaderPagoPA(caption: "CodiceArticolo", Order = 3)]
    public string? CodiceArticolo { get; set; }

    [HeaderPagoPA(caption: "DescrizioneRiga", Order = 4)]
    public string? DescrizioneRiga { get; set; }

    [HeaderPagoPA(caption: "Quantita", Order = 5)]
    public int Quantita { get; set; }

    [HeaderPagoPA(caption: "Importo", Order = 6)]
    public decimal Importo { get; set; }

    [HeaderPagoPA(caption: "CodIva", Order = 7)]
    public string? CodIva { get; set; }

    [HeaderPagoPA(caption: "Condizioni", Order = 8)]
    public string? Condizioni { get; set; }

    [HeaderPagoPA(caption: "Causale", Order = 9)]
    public string? Causale { get; set; }

    [HeaderPagoPA(caption: "IndTipoRiga", Order = 10)]
    public string? IndTipoRiga { get; set; } 
}