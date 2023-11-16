using System.Runtime.Serialization;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

public class DatiModuloCommessaCreateCommand : IRequest<DatiModuloCommessa>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
    public int NumeroNotificheNazionali { get; set; }
    public int NumeroNotificheInternazionali { get; set; }
    public DateTime DataCreazione { get; set; }
    public DateTime DataModifica { get; set; }
    public int AnnoValidita { get; set; }
    public int MeseValidita { get; set; } 
    public string? IdEnte { get; set; } 
    public long IdTipoContratto { get; set; }
    public string? Stato { get; set; } 
    public string? Prodotto { get; set; } 
    public int IdTipoSpedizione { get; set; }
}