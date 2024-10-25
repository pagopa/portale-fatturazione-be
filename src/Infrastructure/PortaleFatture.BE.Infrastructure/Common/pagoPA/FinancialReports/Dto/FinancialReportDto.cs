using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;

public sealed class FinancialReportDto
{
    [HeaderPagoPA(caption: "abi", Order = 1)]
    public string? Abi { get; set; }

    [HeaderPagoPA(caption: "recipient_id", Order = 2)]
    public string? RecipientId { get; set; }

    [HeaderPagoPA(caption: "name", Order = 3)]
    public string? Name { get; set; }

    [HeaderPagoPA(caption: "category", Order = 4)]
    public string? Category { get; set; }

    [HeaderPagoPA(caption: "current_trx", Order = 5)]
    public int CurrentTrx { get; set; }


    [HeaderPagoPA(caption: "value", Order = 6)]
    public decimal Value { get; set; }

    [HeaderPagoPA(caption: "Codice_articolo", Order = 7)]
    public string? CodiceArticolo { get; set; } 

    public string? YearQuarter { get; set; }
}
