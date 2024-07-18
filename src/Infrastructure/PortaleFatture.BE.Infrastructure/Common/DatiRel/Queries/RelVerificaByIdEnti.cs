using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries;

public class RelVerificaByIdEnti(IAuthenticationInfo authenticationInfo) : IRequest<RelVerificaDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int Anno { get; set; }
    public int Mese { get; set; } 
} 