using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Entities.Utenti;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

public class FattureDaNonInviareSapAggiungiCommand(IAuthenticationInfo? authenticationInfo) : IRequest<bool?>
{

    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int Anno { get; set; }
    public int[]? Mesi { get; set; }
    public string? TipologiaFattura { get; set; }
    public string? IdEnte { get; set; }
    public DateTime DataInserimento { get; set; } = DateTime.UtcNow.ItalianTime();
    public int Stato { get; set; } = 0;
    public string? IdUtente { get; set; } = authenticationInfo!.Id;

}
