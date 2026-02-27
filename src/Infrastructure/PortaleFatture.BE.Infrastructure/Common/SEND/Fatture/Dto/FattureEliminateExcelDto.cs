using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

public sealed class FattureEliminateExcelDto
{
    [HeaderAttributev2(caption: "Id Ente", Order = 1)]
    public string? IstitutioId { get; set; }

    [HeaderAttributev2(caption: "Ragione Sociale", Order = 2)]
    public string? RagioneSociale { get; set; }

    [HeaderAttributev2(caption: "Id Contratto", Order = 3)]
    public string? IdContratto { get; set; }

    [HeaderAttributev2(caption: "Prodotto", Order = 4)]
    public string? Prodotto { get; set; }

    [HeaderAttributev2(caption: "Tipologia Fattura", Order = 5)]
    public string? TipologiaFattura { get; set; }

    [HeaderAttributev2(caption: "Numero Fattura", Order = 6)]
    public long Progressivo { get; set; }

    [HeaderAttributev2(caption: "Data Fattura", Order = 7)]
    public string? DataFattura { get; set; }

    [HeaderAttributev2(caption: "Importo Totale", Order = 8)]
    public decimal? TotaleFattura { get; set; }

    [HeaderAttributev2(caption: "Stato", Order = 9)]
    public string? Stato { get; set; }

    [HeaderAttributev2(caption: "Causale", Order = 10)]
    public string? CausaleFattura { get; set; }

    [HeaderAttributev2(caption: "Tipo Documento", Order = 11)]
    public string? TipoDocumento { get; set; }

    [HeaderAttributev2(caption: "Divisa", Order = 12)]
    public string? Divisa { get; set; }
}
