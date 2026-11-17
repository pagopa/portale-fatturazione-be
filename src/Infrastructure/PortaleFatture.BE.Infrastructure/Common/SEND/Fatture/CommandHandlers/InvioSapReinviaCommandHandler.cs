using MediatR;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Service;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.CommandHandlers;

/// <summary>
/// Re-invio a SAP con nota e storno per fattura. Stessa sequenza del Genera:
/// 1. lock sulla vista (409 senza altre letture ne' scritture);
/// 2. validazione formale (400), in <see cref="InvioSapRegoleReinvio"/>;
/// 3. stato delle fatture scelte dalla vista dettaglio, con le regole del Genera: non trovate 404, fuori rigo 400,
///    non inviate 400;
/// 4. e 5. scritture, commit e <c>createRun</c> in <see cref="InvioSapBatchWriter"/>: cambia solo il Dettaglio.
/// </summary>
public sealed class InvioSapReinviaCommandHandler(
    IFattureDbContextFactory factory,
    IMediator mediator,
    ISynapseService synapse,
    IPortaleFattureOptions options,
    ILogger<InvioSapReinviaCommandHandler> logger) : IRequestHandler<InvioSapReinviaCommand, InvioSapGeneraEsito>
{
    public async Task<InvioSapGeneraEsito> Handle(InvioSapReinviaCommand command, CancellationToken ct)
    {
        var authInfo = command.AuthenticationInfo;

        // 1. Lock sulla vista, prima di qualunque altra cosa (richiesta di DATA).
        if (await InvioSapBatchWriter.LockAttivo(mediator, authInfo, ct) is { } conflitto)
            return conflitto;

        // 2. Validazione formale.
        var richiesta = command.ToRichiesta();
        var errori = InvioSapRegoleReinvio.ValidaFormale(richiesta, authInfo.Id);
        if (errori.Count > 0)
            return InvioSapGeneraEsito.NonValida(errori);

        var anno = richiesta.Anno!.Value;
        var mese = richiesta.Mese!.Value;
        var tipologia = richiesta.TipologiaFattura!.Trim();

        // 3. Le fatture scelte devono esistere, stare nel rigo ed essere inviate: stesse regole del Genera.
        var comeGenerazione = richiesta.ComeGenerazione();
        var correnti = await mediator.Send(new InvioSapStatoFattureQuery(authInfo) { IdFatture = comeGenerazione.IdFatture! }, ct);
        var esitoSelezione = InvioSapRegole.ValidaSelezione(comeGenerazione, correnti.Select(c => c.ToCorrente()).ToList());
        if (esitoSelezione is not null)
            return esitoSelezione;

        // 4. e 5. Transazione, commit e createRun. La data della nota e' la stessa per tutte le fatture del batch.
        var adesso = DateTime.UtcNow;
        var righe = richiesta.Fatture!
            .Select(f => new InvioSapRigaReinvio(
                f!.IdFattura!.Value,
                f.NumeroDocumentoStorno!.Trim(),
                new InvioSapNota(adesso, f.Nota!.Trim(), InvioSapRegole.DescrizioneAzione).ToJson()))
            .ToList();

        return await InvioSapBatchWriter.Scrivi(factory, mediator, synapse, options, logger, authInfo,
            new InvioSapBatchDaScrivere(anno, mese, tipologia, InvioSapModalita.ReInvio, righe.Count, VuotoComeNonTrovate: false, Rotta: "reinvia"),
            (uow, batchId, token) => uow.Execute(new InvioSapDettaglioReinvioCommandPersistence(batchId, righe), token),
            ct);
    }
}
