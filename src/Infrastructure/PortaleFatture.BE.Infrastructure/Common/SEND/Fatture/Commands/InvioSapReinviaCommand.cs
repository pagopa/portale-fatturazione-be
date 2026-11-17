using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

/// <summary>
/// Re-invio a SAP (<c>PUT api/fatture/invio-sap/reinvia</c>): fatture gia' inviate di un solo rigo (anno, mese,
/// tipologia), ognuna con la propria nota e il proprio documento di storno. Stesso esito e stesse scritture del
/// Genera; cambia solo il dettaglio, che porta nota e storno per riga.
/// </summary>
public sealed class InvioSapReinviaCommand(IAuthenticationInfo authenticationInfo) : IRequest<InvioSapGeneraEsito>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? Anno { get; init; }
    public int? Mese { get; init; }
    public string? TipologiaFattura { get; init; }
    public IReadOnlyList<InvioSapFatturaDaReinviare?>? Fatture { get; init; }

    public InvioSapRichiestaReinvio ToRichiesta() => new()
    {
        Anno = Anno,
        Mese = Mese,
        TipologiaFattura = TipologiaFattura,
        Fatture = Fatture
    };
}
