using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;

public class EnteContrattoDto
{ 
    public string? IdEnte { get; set; }
 
    public string? RagioneSociale { get; set; }
 
    public string? TipoContratto { get; set; }
 
    public string? IdContratto { get; set; }
 
    public string? Prodotto { get; set; }
 
    public string? CodiceIPA { get; set; }
 
    public string? CodiceSDI { get; set; }

    public string? InstitutionType { get; set; } 

    public int IdTipoContratto { get; set; }
} 