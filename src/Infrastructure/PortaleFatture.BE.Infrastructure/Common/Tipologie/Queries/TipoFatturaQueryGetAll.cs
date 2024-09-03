using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;

public class TipoFatturaQueryGetAll(AuthenticationInfo? authenticationInfo, int anno, int mese, bool cancellata = false) : IRequest<IEnumerable<string>>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int Anno { get; internal set; } = anno;
    public int Mese { get; internal set; } = mese;
    public bool? Cancellata { get; set; } = cancellata;
}