using System.Runtime.Serialization;

namespace PortaleFatture.BE.Core.Entities.DatiCommesse;

public class DatiCommessaContatto
{ 
    [DataMember(Name = "FkIdDatiCommessa")]
    public long IdDatiCommessa { get; set; }
    public string? Email { get; set; }
    public int Tipo { get; set; }
} 