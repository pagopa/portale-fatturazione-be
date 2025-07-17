namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

public class ReportNotificheListDto
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
    public string DescrizioneStato =>
        Stato switch
        {
            0 => "Presa in carico",
            1 => "Elaborato",
            2 => "Elaborato no data",
            _ => "Errore"
        };
    public DateTime DataInserimento { get; set; }
    public DateTime? DataFine { get; set; }
    public string? Storage { get; set; }
    public string? NomeDocumento { get; set; }
    public string? Link { get; set; }
    public string? ContentLanguage { get; set; }
    public string? ContentType { get; set; }
    public int FkIdTipologiaReport { get; set; }
    public string? Hash { get; set; }
    public bool? Letto { get; set; }
    public DateTime? DataLettura { get; set; } 
    public string? RagioneSociale { get; set; } 
    public string? CategoriaDocumento { get; set; } 
    public string? TipologiaDocumento { get; set; } 
    public long? Count { get; set; }
} 