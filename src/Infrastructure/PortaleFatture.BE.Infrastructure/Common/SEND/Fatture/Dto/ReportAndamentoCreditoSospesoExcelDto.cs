using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

/// <summary>
/// Data Transfer Object (DTO) che rappresenta il report di andamento del credito sospeso, specificamente progettato per l'esportazione in formato Excel.
/// </summary>
public class ReportAndamentoCreditoSospesoExcelDto
{
    [HeaderAttributev2(caption: "Id Ente", Order = 1)]
    public string? IdEnte { get; set; }

    [HeaderAttributev2(caption: "Ragione Sociale", Order = 2)]
    public string? RagioneSociale { get; set; }

    [HeaderAttributev2(caption: "Id Contratto", Order = 3)]
    public string? IdContratto { get; set; }

    [HeaderAttributev2(caption: "Tipo Contratto", Order = 4)]
    public string? TipoContratto { get; set; }

    [HeaderAttributev2(caption: "Tipologia Fattura", Order = 5)]
    public string? TipologiaFattura { get; set; }

    [HeaderAttributev2(caption: "Num. Fattura Sospesa", Order = 6)]
    public int NumFatturaSospesa { get; set; }

    [HeaderAttributev2(caption: "Tipo Documento", Order = 7)]
    public string? TipoDocumento { get; set; }

    [HeaderAttributev2(caption: "Data Fattura", Order = 8)]
    public string? DataFattura { get; set; }

    [HeaderAttributev2(caption: "Anno", Order = 9)]
    public int Anno { get; set; }

    [HeaderAttributev2(caption: "Mese", Order = 10)]
    public int Mese { get; set; }

    [HeaderAttributev2(caption: "Imponibile Fattura €", Order = 11, Style = XCellStyle.StandardNumberDecimal)]
    public decimal ImponibileFattura { get; set; }

    [HeaderAttributev2(caption: "Credito Cumulato €", Order = 12, Style = XCellStyle.StandardNumberDecimal)]
    public decimal CreditoCumulato { get; set; }

    [HeaderAttributev2(caption: "REL Non Firmata", Order = 13)]
    public string? RelNonFirmata { get; set; }

    [HeaderAttributev2(caption: "Tipo REL", Order = 14)]
    public string? TipoREL { get; set; }

    [HeaderAttributev2(caption: "Anno REL", Order = 15)]
    public int? AnnoREL { get; set; }

    [HeaderAttributev2(caption: "Mese REL", Order = 16)]
    public int? MeseREL { get; set; }
}
