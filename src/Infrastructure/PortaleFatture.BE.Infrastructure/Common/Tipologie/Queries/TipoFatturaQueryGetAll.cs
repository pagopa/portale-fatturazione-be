using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;

public class TipoFatturaQueryGetAll(AuthenticationInfo? authenticationInfo, int anno, int mese) : IRequest<IEnumerable<string>>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int Anno { get; internal set; } = anno;
    public int Mese { get; internal set; } = mese;
}