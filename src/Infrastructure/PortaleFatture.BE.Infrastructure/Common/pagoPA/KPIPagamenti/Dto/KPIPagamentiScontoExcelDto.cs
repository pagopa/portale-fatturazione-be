using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;
using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Dto;

public sealed class KPIPagamentiScontoExcelDto
{
    [HeaderPagoPA(caption: "Name", Order = 1)]
    public string? Name { get; set; }

    [HeaderPagoPA(caption: "RecipientId", Order = 2)]
    public string? RecipientId { get; set; } 

    [HeaderPagoPA(caption: "YearQuarter", Order = 3)]
    public string? YearQuarter { get; set; } 

    [HeaderPagoPA(caption: "Totale ", Order = 7)]
    public decimal? Totale { get; set; }

    [HeaderPagoPA(caption: "TotaleSconto", Order = 10)]
    public decimal? TotaleSconto { get; set; }

    [HeaderPagoPA(caption: "Link", Order = 11)]
    public string? Link { get; set; }

    [HeaderPagoPA(caption: "KpiList", Order = 12)]
    public string? KpiList { get; set; } 

    [HeaderPagoPA(caption: "psp_name", Order = 4)]
    public string? PSPName { get; set; }

    [HeaderPagoPA(caption: "psp_id", Order = 5)]
    public string? PspId { get; set; } 
 
    [HeaderPagoPA(caption: "trx_total", Order = 6)]
    public long? TrxTotal { get; set; }
 
    [HeaderPagoPA(caption: "KpiOk", Order = 8)]
    public int? KpiOk { get; set; }

    [HeaderPagoPA(caption: "PercSconto", Order = 9)]
    public decimal? PercSconto { get; set; }

    [HeaderPagoPA(caption: "FlagMQ", Order = 10)]
    public bool? FlagMQ { get; set; }
} 