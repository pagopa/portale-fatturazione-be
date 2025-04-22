using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.EntiPrivati.Dto;

// Removed parentheses from the static class declaration to fix CS0710
public static class TipologiaPubblicoPrivato
{
    public const string PUBBLICO = "PUB"; 
    public const string PRIVATO = "PRIV";   
}
public sealed class CheckFinanceVBSDto
{
    [HeaderPagoPA(caption: "Numero", Order = 1)]
    public string? Numero { get; set; }

    [HeaderPagoPA(caption: "Codice_articolo", Order = 2)]
    public string? CodiceArticolo { get; set; }

    [HeaderPagoPA(caption: "Q_ta_tot", Order = 3)]
    public int? Quantità { get; set; }

    [HeaderPagoPA(caption: "Tipologia", Order = 4)]
    public string? Tipologia { get; set; }

    [HeaderPagoPA(caption: "Q_ta_tipologia", Order = 5)]
    public int? QuantitàTipologia { get; set; }

    [HeaderPagoPA(caption: "Totale Imponibile (senza sconto)", Order = 6)]
    public decimal? Importo { get; set; }

    [HeaderPagoPA(caption: "ABI", Order = 7)]
    public string? ABI { get; set; }

    [HeaderPagoPA(caption: "Ragione Sociale", Order = 8)]
    public string? RagioneSociale { get; set; }

    [HeaderPagoPA(caption: "Sconto", Order = 9)]
    public decimal? Sconti { get; set; }

    [HeaderPagoPA(caption: "Totale Imponibile (senza sconto) con bollo", Order = 10)]
    public decimal? Totale { get; set; }

    [HeaderPagoPA(caption: "Totale Imponibile con sconto", Order = 11)]
    public decimal? TotaleScontato { get; set; }
    public string? ContractId { get; set; }
}
