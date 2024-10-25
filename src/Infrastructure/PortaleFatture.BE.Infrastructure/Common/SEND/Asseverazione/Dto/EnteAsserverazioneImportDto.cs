namespace PortaleFatture.BE.Infrastructure.Common.SEND.Asseverazione.Dto;

public class EnteAsserverazioneImportDto
{
    public string? IdEnte { get; set; }
    public string? RagioneSociale { get; set; }
    public DateTime? DataAsseverazione { get; set; }
    public bool? TipoAsseverazione { get; set; }
}