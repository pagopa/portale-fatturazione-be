namespace PortaleFatture.BE.Infrastructure.Common.SEND.Report.Dto;

public class ReportDto
{
    public long IdReport { get; set; }
    public string? Json { get; set; }
    public int Anno { get; set; }
    public int? Mese { get; set; }
    public string? Prodotto { get; set; }
    public string? Stato { get; set; }
    public DateTime DataInserimento { get; set; }
    public DateTime? DataStepCorrente { get; set; }
    public string? LinkDocumento { get; set; }
    public string? TipologiaDocumento { get; set; }
    public string? CategoriaDocumento { get; set; }
    public string? Descrizione { get; set; }
    public string? Hash { get; set; }
    public string? ContentType { get; set; }
    public string? ContentLanguage { get; set; }
}