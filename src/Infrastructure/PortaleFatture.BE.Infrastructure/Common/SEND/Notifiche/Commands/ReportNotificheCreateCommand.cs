using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Messaggi;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands;

public class ReportNotificheCreateCommand(IAuthenticationInfo? authenticationInfo) : IRequest<int?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? UniqueId { get; set; }
    public string? Json { get; set; }
    public int Anno { get; set; }
    public int Mese { get; set; }
    public string? InternalOrganizationId { get; set; } = authenticationInfo!.IdEnte;
    public string? ContractId { get; set; } 
    public string? UtenteId { get; set; } = authenticationInfo!.Id;
    public string? Prodotto { get; set; } = authenticationInfo.Prodotto;
    public int Stato { get; set; } = TipologiaStatoMessaggio.RichiestaNotifiche;
    public DateTime DataInserimento { get; set; } = DateTime.UtcNow.ItalianTime();
    public DateTime DataFine { get; set; }
    public string? Storage { get; set; }
    public string? NomeDocumento { get; set; }
    public string? Link { get; set; }
    public string? ContentLanguage { get; set; } = "it";
    public string? ContentType { get; set; } = "text/csv";
    public int FkIdTipologiaReport { get; set; }
    public string? Hash { get { return Json.GetHashSHA256(); } }
    public bool? Letto { get; set; } = false; 
}
