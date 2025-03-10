using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

public class FatturaRiptristinoSAPCommand(IAuthenticationInfo? authenticationInfo, int anno, int mese, string? tipologiaFattura) : IRequest<bool>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int Anno { get; set; } = anno;
    public int Mese { get; set; } = mese;
    public string? TipologiaFattura { get; set; } = tipologiaFattura;
    public int? FatturaInviata { get; set; } = 0;
    public int StatoAtteso { get; set; } = 1;
    public bool Invio { get; set; } = false;
}

public class FatturaRiptristinoSAPCommandList(List<FatturaRiptristinoSAPCommand> commands) : IRequest<bool>
{
    public List<FatturaRiptristinoSAPCommand> Commands { get; } = commands;
}