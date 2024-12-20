using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;

public class CheckFinance
{
    [HeaderPagoPA(caption: "Numero", Order = 1)]
    public string? Numero { get; set; }

    [HeaderPagoPA(caption: "Codice_articolo", Order = 2)]
    public string? CodiceArticolo { get; set; }

    [HeaderPagoPA(caption: "Q_ta", Order = 3)]
    public int? Quantità { get; set; }

    [HeaderPagoPA(caption: "Totale Imponibile (senza sconto)", Order = 3)]
    public decimal? Importo { get; set; }

    [HeaderPagoPA(caption: "ABI", Order = 4)]
    public string? ABI { get; set; }

    [HeaderPagoPA(caption: "Ragione Sociale", Order = 5)]
    public string? RagioneSociale { get; set; }

    [HeaderPagoPA(caption: "Sconto", Order = 6)]
    public decimal? Sconti { get; set; }

    [HeaderPagoPA(caption: "Totale Imponibile (senza sconto) con bollo", Order = 7)]
    public decimal? Totale { get; set; }

    [HeaderPagoPA(caption: "Totale Imponibile con sconto", Order = 7)]
    public decimal? TotaleScontato { get; set; }
    public string? ContractId { get; set; }
} 