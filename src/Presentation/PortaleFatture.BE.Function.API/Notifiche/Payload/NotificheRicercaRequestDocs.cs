using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;

namespace PortaleFatture.BE.Function.API.Notifiche.Payload;

public class NotificheRicercaRequestDocs
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
} 