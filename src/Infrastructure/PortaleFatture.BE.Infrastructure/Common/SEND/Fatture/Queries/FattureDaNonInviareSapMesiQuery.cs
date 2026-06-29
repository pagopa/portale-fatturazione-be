using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
public class FattureDaNonInviareSapMesiQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<int>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public int? Anno { get; set; }
}
