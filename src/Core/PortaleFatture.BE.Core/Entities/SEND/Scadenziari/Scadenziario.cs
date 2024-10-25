namespace PortaleFatture.BE.Core.Entities.SEND.Scadenziari;

public class Scadenziario
{
    public TipoScadenziario Tipo { get; set; }
    public int GiornoInizio { get; set; }
    public int GiornoFine { get; set; }
    public int Mese { get; set; }
    public int Anno { get; set; }
    public DateTime Adesso { get; set; }
}