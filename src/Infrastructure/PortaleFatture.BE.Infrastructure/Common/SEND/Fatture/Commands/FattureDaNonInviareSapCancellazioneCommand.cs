using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;


public class FatturaKey
{
    [Required]
    public string IdEnte { get; set; }
    [Required]
    public string TipologiaFattura { get; set; }
    [Range(2000, 2100)]
    public int Anno { get; set; }
    [Range(1, 12)]
    public int Mese { get; set; }
    [Required]
    public int Stato { get; set; }
}



public class FattureDaNonInviareSapCancellazioneCommand(IAuthenticationInfo? authenticationInfo, FatturaKey[] Fatture) : IRequest<int?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public FatturaKey[] Fatture { get; set; } = Fatture;
    public DateTime? DataCancellazione { get; set; } = DateTime.UtcNow.ItalianTime();
    public string? IdUtenteCancellazione { get; set; } = authenticationInfo!.Id;

}

