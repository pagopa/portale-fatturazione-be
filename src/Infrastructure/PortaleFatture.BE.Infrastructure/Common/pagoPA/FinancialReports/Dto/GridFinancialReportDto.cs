using System.Text.Json.Serialization;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;

public sealed class GridFinancialReportDto 
{
    [HeaderPagoPA(caption: "Name", Order = 1)]
    public string? Name { get; set; }

    [JsonIgnore]
    [HeaderPagoPA(caption: "Category", Order = 11)]
    public string? Category { get; set; }

    [HeaderPagoPA(caption: "ContractId", Order = 2)]
    public string? ContractId { get; set; }

    [HeaderPagoPA(caption: "TipoDoc", Order = 3)]
    public string? TipoDoc { get; set; }

    [HeaderPagoPA(caption: "CodiceAggiuntivo", Order = 4)]
    public string? CodiceAggiuntivo { get; set; }

    [HeaderPagoPA(caption: "VatCode", Order = 5)]
    public string? VatCode { get; set; }

    [HeaderPagoPA(caption: "Valuta", Order = 6)]
    public string? Valuta { get; set; }

    [HeaderPagoPA(caption: "Id", Order = 7)]
    public int? Id { get; set; }

    [HeaderPagoPA(caption: "Numero", Order = 8)]
    public string? Numero { get; set; }

    [HeaderPagoPA(caption: "Data", Order = 9)]
    public DateTime? Data { get; set; }

    [HeaderPagoPA(caption: "Bollo", Order = 10)]
    public string? Bollo { get; set; }

    [JsonIgnore]
    [HeaderPagoPA(caption: "ProgressivoRiga", Order = 12)]
    public int ProgressivoRiga { get; set; }

    [JsonIgnore]
    [HeaderPagoPA(caption: "CodiceArticolo", Order = 13)]
    public string? CodiceArticolo { get; set; }

    [JsonIgnore] 
    public string? DescrizioneRiga { get; set; }

    [JsonIgnore]
    [HeaderPagoPA(caption: "Quantita", Order = 19)]
    public int? Quantita { get; set; }

    [JsonIgnore]
    [HeaderPagoPA(caption: "Importo", Order = 20)]
    public decimal? Importo { get; set; }

    [JsonIgnore]
    [HeaderPagoPA(caption: "CodIva", Order = 15)]
    public string? CodIva { get; set; }

    [JsonIgnore]
    [HeaderPagoPA(caption: "Condizioni", Order = 16)]
    public string? Condizioni { get; set; }

    [JsonIgnore]
    [HeaderPagoPA(caption: "Causale", Order = 17)]
    public string? Causale { get; set; }

    [JsonIgnore]
    [HeaderPagoPA(caption: "IndTipoRiga", Order = 18)]
    public string? IndTipoRiga { get; set; }


    [HeaderPagoPA(caption: "RiferimentoData", Order = 11)]
    public DateTime? RiferimentoData { get; set; }

    [HeaderPagoPA(caption: "YearQuarter", Order = 12)]
    public string? YearQuarter { get; set; }

    [JsonIgnore] 
    //[HeaderPagoPA(caption: "DetailedReport", Order = 21)]
    public string? DetailedReport { get; set; }

    [JsonIgnore]
    //[HeaderPagoPA(caption: "AgentQuarterReport", Order = 22)]
    public string? AgentQuarterReport { get; set; }

    public List<GridFinancialReportPosizioniDto>? Posizioni { get; set; } = [];
    public List<string>? Reports { get; set; } = [];
}
