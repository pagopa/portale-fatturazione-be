namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;

public class RelVerificaEmailDto
{
    public string? Messaggio { get; set; }
    public string? RagioneSociale { get; set; } 
    public string? TipologiaFattura { get; set; }
}

public class RelVerificaRelDto
{
    public string? TipologiaFattura { get; set; } 
    public string? RagioneSociale { get; set; }
}

public class RelVerificaDto
{
    public string? IdEnte { get; set; }
    public RelVerificaEmailDto? VerificaEmail { get; set; }

    public RelVerificaRelDto? VerificaRel{ get; set; }
}