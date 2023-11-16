namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload.Response;

public class DatiModuloCommessaResponse
{
    public List<DatiModuloCommessaSimpleResponse>? ModuliCommessa { get; set; }
}
public class DatiModuloCommessaSimpleResponse
{
    public int NumeroNotificheNazionali { get; set; }
    public int NumeroNotificheInternazionali { get; set; }
    public int IdTipoSpedizione { get; set; }
}