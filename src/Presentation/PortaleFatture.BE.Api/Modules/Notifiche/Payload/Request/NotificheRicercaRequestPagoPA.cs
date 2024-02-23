using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Core.Extensions;
namespace PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;

public class NotificheRicercaRequestPagoPA
{
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public string? Prodotto { get; set; }
    public string? Cap { get; set; }
    public string? Profilo { get; set; }
    public TipoNotifica? TipoNotifica { get; set; }
    public int[]? StatoContestazione { get; set; }
    public string? Iun { get; set; }
    public string? RecipientId { get; set; }

    private string[]? _idEnti; 
    public string[]? IdEnti
    {
        get { return _idEnti; }
        set { _idEnti = value!.IsNullNotAny() ? null : value; }
    } 
}