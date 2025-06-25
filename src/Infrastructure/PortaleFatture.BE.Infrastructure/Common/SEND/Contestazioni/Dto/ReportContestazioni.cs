using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

public class ReportContestazioni
{
    public string? ReportId { get; set; } 
    public string? UniqueId { get; set; }
    public string? Json { get; set; }
    public int Anno { get; set; }
    public int? Mese { get; set; }
    public string? InternalOrganizationId { get; set; }
    public string? ContractId { get; set; }
    public string? ActualContractId { get; set; }
    public string? UtenteId { get; set; }
    public string? Prodotto { get; set; }
    public short Stato { get; set; } 
    public string? DescrizioneStato { get; set; }
    public DateTime DataInserimento { get; set; }
    public DateTime? DataStepCorrente { get; set; } 
    public string? NomeDocumento { get; set; }
    [JsonIgnore]
    public string? LinkDocumento { get; set; }
    [JsonIgnore]
    public string? Storage { get; set; }
    public string? Hash { get; set; }
    public string? ContentType { get; set; }
    public string? ContentLanguage { get; set; } 
    public string? TipologiaDocumento { get; set; }
    public string? CategoriaDocumento { get; set; } 
    public string? RagioneSociale { get; set; }
}