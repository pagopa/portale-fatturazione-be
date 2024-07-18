using System.ComponentModel.DataAnnotations.Schema;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Core.Entities.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries;

public class RelUploadGetById(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<RelUpload>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
 
    public string? IdEnte { get; set; }
    public string? IdContratto { get; set; } 
    public string? TipologiaFattura { get; set; } 
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public string? Azione { get; set; } = RelAzioneDocumento.Upload;
}