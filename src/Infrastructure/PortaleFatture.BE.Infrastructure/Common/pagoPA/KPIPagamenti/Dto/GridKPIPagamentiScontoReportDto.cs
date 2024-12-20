using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Dto;

public sealed class GridKPIPagamentiScontoReportDto
{
    public string? Key
    {
        get { return $"{RecipientId!}|{YearQuarter}"; }
    }

    [HeaderPagoPA(caption: "Name", Order = 1)]
    public string? Name { get; set; } 

    [HeaderPagoPA(caption: "YearQuarter", Order = 2)]
    public string? YearQuarter { get; set; }

    [HeaderPagoPA(caption: "RecipientId", Order = 3)]
    public string? RecipientId { get; set; }

    [HeaderPagoPA(caption: "Totale ", Order = 4)]
    public decimal Totale  { get; set; }

    [HeaderPagoPA(caption: "TotaleSconto", Order = 5)]
    public decimal TotaleSconto { get; set; }

    [HeaderPagoPA(caption: "Link", Order = 6)]
    public string? Link { get; set; }

    [HeaderPagoPA(caption: "KpiList", Order = 7)]
    public string? KpiList { get; set; }

    [HeaderPagoPA(caption: "FlagMQ", Order = 7)]
    public bool? FlagMQ { get; set; }
    public List<KPIPagamentiScontoDto>? Posizioni { get; set; } = [];
} 