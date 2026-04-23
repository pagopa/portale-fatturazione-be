using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

/// <summary>
/// Rappresenta una query per ottenere i dettagli emessi di un documento contabile di fattura, utilizzando le
/// informazioni di autenticazione e l'identificativo della fattura.
/// </summary>
/// <remarks>Questa query viene utilizzata nell'ambito di richieste MediatR per recuperare una collezione di
/// dettagli di fatture emesse. È necessario fornire le informazioni di autenticazione valide per identificare l'ente e,
/// facoltativamente, l'identificativo della fattura di interesse.</remarks>
public class FattureDocContabileDettaglioEmessoQuery : IRequest<IEnumerable<FatturaDocContabileEmessoDettaglioDto>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; }

    public FattureDocContabileDettaglioEmessoQuery(IAuthenticationInfo authenticationInfo)
    {
        AuthenticationInfo = authenticationInfo;
    }

    public string? IdEnte => AuthenticationInfo.IdEnte;

    public long? IdFattura { get; set; }
}