using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
public class GestioneFattureModificaMesiQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<int>?>
{

    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public string Anno { get; set; }
    public string TipologiaFattura { get; set; }

    public string Azione { get; set; }


}