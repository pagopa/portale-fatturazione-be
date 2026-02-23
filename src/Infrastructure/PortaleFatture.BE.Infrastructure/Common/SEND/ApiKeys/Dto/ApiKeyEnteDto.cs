namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Dto;

public class ApiKeyEnteDto
{
    public string? IdEnte { get; set; }
    public string? ApiKey { get; set; }
    public DateTime DataCreazione { get; set; }
    public DateTime DataModifica { get; set; }
    public bool Attiva { get; set; }
    public string? RagioneSociale { get; set; }
    public string? IdContratto { get; set; }
    public int IdTipoContratto { get; set; }
    public string? Prodotto { get; set; } 
    public string? Profilo { get; set; }
} 