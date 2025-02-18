using System.Data;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

public class WhiteListFatturaEnteAnniInserimentoDto
{
    public int AnnoRiferimento { get; set; } 
    public int MeseRiferimento { get; set; }
    public string? TipologiaFattura { get; set; } 
} 