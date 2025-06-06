﻿using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Commands;

public class RelUploadCreateCommand(IAuthenticationInfo? authenticationInfo) : IRequest<bool?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? IdEnte { get; set; }
    public string? IdContratto { get; set; }
    public string? TipologiaFattura { get; set; }
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public DateTime? DataEvento { get; set; }
    public string? IdUtente { get; set; }
    public string? Azione { get; set; }
    public string? Hash { get; set; }
}