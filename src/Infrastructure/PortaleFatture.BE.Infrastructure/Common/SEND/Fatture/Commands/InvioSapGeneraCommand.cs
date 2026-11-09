using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

/// <summary>
/// "Genera JSON" (PUT api/fatture/invio-sap/genera). Senza <see cref="IdFatture"/> = primo invio del rigo
/// <c>DA_INVIARE</c>; con <see cref="IdFatture"/> = re-invio di fatture gia' inviate (D7).
/// </summary>
public sealed class InvioSapGeneraCommand(IAuthenticationInfo authenticationInfo) : IRequest<InvioSapGeneraEsito>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? Anno { get; init; }
    public int? Mese { get; init; }
    public string? TipologiaFattura { get; init; }
    public IReadOnlyList<long>? IdFatture { get; init; }
    public string? Nota { get; init; }
    public string? NumeroDocumentoStorno { get; init; }

    public InvioSapRichiestaGenerazione ToRichiesta() => new()
    {
        Anno = Anno,
        Mese = Mese,
        TipologiaFattura = TipologiaFattura,
        IdFatture = IdFatture,
        Nota = Nota,
        NumeroDocumentoStorno = NumeroDocumentoStorno
    };
}
