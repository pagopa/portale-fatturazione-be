using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Report.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.Report.Queries;

public class MatriceCostoRecapitisti(IAuthenticationInfo authenticationInfo, DateTime? dataInizioValidita, DateTime? dataFineValidita) : IRequest<IEnumerable<MatriceCostoRecapitistiDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public DateTime? DataInizioValidita { get; set; } = dataInizioValidita;
    public DateTime? DataFineValidita { get; set; } = dataFineValidita;
}