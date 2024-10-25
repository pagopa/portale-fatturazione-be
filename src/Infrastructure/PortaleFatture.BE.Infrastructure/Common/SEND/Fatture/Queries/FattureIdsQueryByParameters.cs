using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class FattureIdsQueryByParameters(IAuthenticationInfo authenticationInfo,
    int anno,
    int mese,
    string? tipologiaFattura,
    int? fatturaInviata,
    int statoAtteso) : IRequest<bool?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int Anno { get; set; } = anno;
    public int Mese { get; set; } = mese;
    public string? TipologiaFattura { get; set; } = tipologiaFattura;
    public int? FatturaInviata { get; set; } = fatturaInviata;
    public int StatoAtteso { get; set; } = statoAtteso;
    public IEnumerable<long>? IdFatture { get; set; }
}