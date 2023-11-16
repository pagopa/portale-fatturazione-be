namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload;

public class DatiModuloCommessaCreateRequest
{
    public List<DatiModuloCommessaCreateSimpleRequest>? ModuliCommessa { get; set; }

}
public class DatiModuloCommessaCreateSimpleRequest
{
    public int NumeroNotificheNazionali { get; set; }
    public int NumeroNotificheInternazionali { get; set; }  
    public int IdTipoSpedizione { get; set; }
}