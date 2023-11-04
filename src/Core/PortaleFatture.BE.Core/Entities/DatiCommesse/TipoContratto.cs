using System.Runtime.Serialization;

namespace PortaleFatture.BE.Core.Entities.DatiCommesse;

public class TipoContratto
{
    [DataMember(Name = "IdTipoContratto")]
    public long Id { get; set; } 
    public string? Descrizione { get; set; }
} 