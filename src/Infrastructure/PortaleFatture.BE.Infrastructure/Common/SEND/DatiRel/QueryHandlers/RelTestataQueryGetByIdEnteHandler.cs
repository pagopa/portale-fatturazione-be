using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.QueryHandlers;

public class RelTestataQueryGetByIdEnteHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<RelTestataQueryGetByIdEnteHandler> logger) : IRequestHandler<RelTestataQueryGetByIdEnte, RelTestataDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<RelTestataQueryGetByIdEnteHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<RelTestataDto?> Handle(RelTestataQueryGetByIdEnte request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        var testata = await rs.Query(new RelTestataQueryGetByIdEntePersistence(request), ct);
        var testate = new List<SimpleRelTestata>();
        foreach (var t in testata!.RelTestate!)
        {
            //if (t.AsseverazioneTotaleIva > 0)
            //{
            //    var newTestata = new SimpleRelTestata()
            //    {
            //        Totale = t.AsseverazioneTotale,
            //        TotaleIva = t.AsseverazioneTotaleIva,
            //        TotaleAnalogico = t.AsseverazioneTotaleAnalogico,
            //        TotaleAnalogicoIva = t.AsseverazioneTotaleAnalogicoIva,
            //        TotaleDigitale = t.AsseverazioneTotaleDigitale,
            //        TotaleDigitaleIva = t.AsseverazioneTotaleDigitaleIva,
            //        TotaleNotificheAnalogiche = t.AsseverazioneTotaleNotificheAnalogiche,
            //        TotaleNotificheDigitali = t.AsseverazioneTotaleNotificheDigitali,
            //        TipologiaFattura = TipologiaFattura.ASSEVERAZIONE,
            //        IdContratto = t.IdContratto,
            //        Iva = t.Iva,
            //        IdEnte = t.IdEnte,
            //        Mese = t.Mese,
            //        Anno = t.Anno,
            //        RagioneSociale = t.RagioneSociale 
            //    };
            //    testate.Add(newTestata); 
            //    testata.Count += 1;
            //}
            testate.Add(t);
        }
        testata.RelTestate = testate;
        return testata;
    }
}