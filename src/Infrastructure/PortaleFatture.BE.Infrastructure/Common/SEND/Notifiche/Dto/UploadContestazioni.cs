using Microsoft.AspNetCore.Http;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

public sealed class UploadContestazioni: UploadContestazioniEnte
{ 
    public string? IdEnte { get; set; }
    public string? ContractId { get; set; }
}

public class UploadContestazioniEnte
{
    public IFormFile? FileChunk { get; set; } 
    public string? FileId { get; set; } //univoco chiave temp upload
    public int TotalChunks { get; set; }
    public int ChunkIndex { get; set; } 
    public string? Anno { get; set; }
    public string? Mese { get; set; }
}