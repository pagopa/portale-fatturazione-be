using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class FattureDaNonInviareSapMesiInserisciQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<FattureDaNonInviareAnniInserimentoDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? Anno { get; set; } = DateTime.UtcNow.Year;
    public string? TipologiaFattura { get; set; }
    public string? IdEnte { get; set; }

}

