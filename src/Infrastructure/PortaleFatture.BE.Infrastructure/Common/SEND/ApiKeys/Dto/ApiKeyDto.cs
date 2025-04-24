namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Dto;

 
public class ApiKeyDto
{
    public string? IdEnte { get; set; }
    public string? ApiKey { get; set; }  
    public DateTime DataCreazione { get; set; }
    public DateTime? DataModifica { get; set; }   
    public bool? Attiva { get; set; }
}