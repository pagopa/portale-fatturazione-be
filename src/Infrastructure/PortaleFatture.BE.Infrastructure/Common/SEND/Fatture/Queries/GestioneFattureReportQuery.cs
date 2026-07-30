using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;


/// <summary>
/// Query per la gestione del report delle fatture.
/// </summary>
/// <param name="authenticationInfo">Informazioni di autenticazione dell'utente.</param>
public class GestioneFattureReportQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<GestioneFattureReportDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public string[]? TipologiaFattura { get; set; }
}



