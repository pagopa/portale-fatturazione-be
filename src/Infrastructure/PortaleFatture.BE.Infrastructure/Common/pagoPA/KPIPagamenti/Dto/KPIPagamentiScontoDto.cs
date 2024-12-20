using System.Text.Json.Serialization;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Dto;

public sealed class KPIPagamentiScontoDto
{ 
    public string? RecipientName { get; set; }

    [HeaderPagoPA(caption: "psp_name", Order = 0)]
    public string? PSPName { get; set; }

    [HeaderPagoPA(caption: "psp_id", Order = 1)]  
    public string? PspId { get; set; } 
 
    public string? RecipientId { get; set; }

    [HeaderPagoPA(caption: "year_quarter", Order = 2)]
    public string? YearQuarter { get; set; }

    [HeaderPagoPA(caption: "trx_total", Order = 3)]
    public long TrxTotal { get; set; }

    [HeaderPagoPA(caption: "value_total", Order = 4)]
    public decimal ValueTotal { get; set; }

    [HeaderPagoPA(caption: "KpiOk", Order = 5)]
    public int KpiOk { get; set; }

    [HeaderPagoPA(caption: "PercSconto", Order = 5)]
    public decimal PercSconto { get; set; }

    [HeaderPagoPA(caption: "value_discount", Order = 6)]
    public decimal ValueDiscount { get; set; }

    [JsonIgnore]
    [HeaderPagoPA(caption: "LinkReport", Order = 7)]
    public string? LinkReport { get; set; }

    [JsonIgnore]
    [HeaderPagoPA(caption: "KpiList", Order = 8)]
    public string? KpiList { get; set; }

    [HeaderPagoPA(caption: "FlagMQ", Order = 9)]
    public bool? FlagMQ { get; set; }
}