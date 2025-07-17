namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

public sealed class ReportNotificheDto
{
    public int ReportId { get; set; }
    public string? UniqueId { get; set; }
    public string? Json { get; set; }
    public int Anno { get; set; }
    public int Mese { get; set; }
    public string? InternalOrganizationId { get; set; }
    public string? ContractId { get; set; }
    public string? UtenteId { get; set; }
    public string? Prodotto { get; set; }
    public int Stato { get; set; }
    public DateTime DataInserimento { get; set; }
    public DateTime DataFine { get; set; }
    public string? Storage { get; set; }
    public string? NomeDocumento { get; set; }
    public string? Link { get; set; }
    public string? ContentLanguage { get; set; }
    public string? ContentType { get; set; }
    public int FkIdTipologiaReport { get; set; }
    public string? Hash { get; set; } 
    public bool? Letto { get; set; } 
    public DateTime? DataLettura { get; set; }
}