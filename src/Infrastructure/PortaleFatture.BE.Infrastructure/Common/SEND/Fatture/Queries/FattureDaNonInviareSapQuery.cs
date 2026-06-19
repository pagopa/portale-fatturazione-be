using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class FattureDaNonInviareSapQuery(IAuthenticationInfo authenticationInfo) : IRequest<FattureDaNonInviareSapDto?>
{

    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    private string[]? _idEnti;
    public string[]? IdEnti
    {
        get { return _idEnti; }
        set { _idEnti = value!.IsNullNotAny() ? null : value; }
    }

    public int? TipologiaContratto { get; set; }
    public string? TipologiaFattura { get; set; }

    public int? Anno { get; set; }
    public int[]? Mesi { get; set; }

    public int? Page { get; set; }
    public int? Size { get; set; }

}




