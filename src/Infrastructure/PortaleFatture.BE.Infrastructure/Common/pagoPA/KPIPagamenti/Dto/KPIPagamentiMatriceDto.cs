using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Dto;

public sealed class KPIPagamentiMatriceDto
{
    [HeaderPagoPA(caption: "kpi_id", Order = 1)]
    public string? KpiId { get; set; }

    [HeaderPagoPA(caption: "kpi_threshold", Order = 2)]
    public decimal? KpiThreshold { get; set; }

    [HeaderPagoPA(caption: "start", Order = 3)]
    public DateTime? Start { get; set; }

    [HeaderPagoPA(caption: "end", Order = 4)]
    public DateTime? End { get; set; }

    [HeaderPagoPA(caption: "psp_id", Order = 5)]
    public string? PspId { get; set; }

    [HeaderPagoPA(caption: "psp_company_name", Order = 6)]
    public string? PspCompanyName { get; set; }

    [HeaderPagoPA(caption: "psp_broker_id", Order = 7)]
    public string? PspBrokerId { get; set; }

    [HeaderPagoPA(caption: "psp_broker_company_name", Order = 8)]
    public string? PspBrokerCompanyName { get; set; }

    [HeaderPagoPA(caption: "perc_kpi", Order = 9)]
    public string? PercKpi { get; set; }   

    [HeaderPagoPA(caption: "kpi_outcome", Order = 10)]
    public string? KpiOutcome { get; set; }

    [HeaderPagoPA(caption: "dl_event_tms", Order = 11)]
    public long? DlEventTms { get; set; }   

    [HeaderPagoPA(caption: "dl_ingestion_tms", Order = 11)]
    public long? DlIngestionTms { get; set; } 
}
