using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

public class FattureDaNonInviareSapRipristinoCommand(IAuthenticationInfo? authenticationInfo, FatturaKey[] Fatture) : IRequest<int?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public FatturaKey[] Fatture { get; set; } = Fatture;
    public DateTime? DataRipristino { get; set; } = DateTime.UtcNow.ItalianTime();
    public string? IdUtenteRipristino { get; set; } = authenticationInfo!.Id;
}

