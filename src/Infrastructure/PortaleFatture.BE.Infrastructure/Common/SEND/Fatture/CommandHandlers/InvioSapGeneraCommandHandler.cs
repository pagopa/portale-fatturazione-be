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
/// "Genera JSON", passo per passo come in be-invio-sap.md §8.5:
/// 1. lock sulla vista (409 senza altre letture ne' scritture);
/// 2. validazione formale (400);
/// 3. letture dalle viste: <c>NumeroFatture</c> del rigo (primo invio del rigo intero), stato delle fatture scelte
///    (elenco) oppure, con "seleziona tutto", controllo delle escluse e numero delle fatture del rigo che restano.
///    Elenco e "seleziona tutto" valgono sia per il re-invio (fatture inviate) sia, con <c>statoInvio</c>
///    <c>DA_INVIARE</c>, per il primo invio (fatture da inviare);
/// 4. e 5. scritture, commit e <c>createRun</c>: in <see cref="InvioSapBatchWriter"/>, condiviso con il re-invio
///    (<c>PUT …/reinvia</c>), che scrive lo stesso batch con nota e storno per fattura.
/// </summary>
public sealed class InvioSapGeneraCommandHandler(
    IFattureDbContextFactory factory,
    IMediator mediator,
    ISynapseService synapse,
    IPortaleFattureOptions options,
    ILogger<InvioSapGeneraCommandHandler> logger) : IRequestHandler<InvioSapGeneraCommand, InvioSapGeneraEsito>
{
    public async Task<InvioSapGeneraEsito> Handle(InvioSapGeneraCommand command, CancellationToken ct)
    {
        var authInfo = command.AuthenticationInfo;

        // 1. Lock sulla vista, prima di qualunque altra cosa (richiesta di DATA).
        if (await InvioSapBatchWriter.LockAttivo(mediator, authInfo, ct) is { } conflitto)
            return conflitto;

        // 2. Validazione formale.
        var richiesta = command.ToRichiesta();
        var errori = InvioSapRegole.ValidaFormale(richiesta, authInfo.Id);
        if (errori.Count > 0)
            return InvioSapGeneraEsito.NonValida(errori);

        var modalita = InvioSapRegole.Modalita(richiesta);
        var anno = richiesta.Anno!.Value;
        var mese = richiesta.Mese!.Value;
        var tipologia = richiesta.TipologiaFattura!.Trim();

        // 3. Letture dalle viste (nessuna scrittura fin qui).
        int numeroAtteso;
        IReadOnlyList<long> idSelezionati = [];
        IReadOnlyList<long> idEsclusi = [];
        var selezioneTotale = InvioSapRegole.SelezioneTotale(richiesta);
        var rigoIntero = InvioSapRegole.RigoIntero(richiesta);
        var inviate = modalita == InvioSapModalita.ReInvio; // stato delle fatture da prendere: inviate o da inviare
        var storno = inviate ? richiesta.NumeroDocumentoStorno!.Trim() : null;
        if (rigoIntero)
        {
            var numero = await mediator.Send(new InvioSapRigoDaInviareQuery(authInfo)
            {
                Anno = anno, Mese = mese, TipologiaFattura = tipologia
            }, ct);
            if (numero is null or <= 0)
                return InvioSapGeneraEsito.NonTrovate();
            numeroAtteso = numero.Value;
        }
        else if (selezioneTotale)
        {
            // "Seleziona tutto": il rigo meno le escluse. Le escluse devono esistere e appartenere al rigo.
            idEsclusi = richiesta.IdFattureEscluse ?? [];
            if (idEsclusi.Count > 0)
            {
                var escluse = await mediator.Send(new InvioSapStatoFattureQuery(authInfo) { IdFatture = idEsclusi }, ct);
                var esitoEscluse = InvioSapRegole.ValidaEsclusioni(richiesta, escluse.Select(c => c.ToCorrente()).ToList());
                if (esitoEscluse is not null)
                    return esitoEscluse;
            }
            numeroAtteso = await mediator.Send(new InvioSapConteggioSelezioneTotaleQuery(authInfo)
            {
                Anno = anno, Mese = mese, TipologiaFattura = tipologia, IdFattureEscluse = idEsclusi, Inviata = inviate
            }, ct);
            if (numeroAtteso <= 0)
                return idEsclusi.Count > 0
                    ? InvioSapGeneraEsito.NonValida([InvioSapRegole.ErroreSelezioneVuota])
                    : InvioSapGeneraEsito.NonTrovate();
        }
        else
        {
            idSelezionati = richiesta.IdFatture!;
            var correnti = await mediator.Send(new InvioSapStatoFattureQuery(authInfo) { IdFatture = idSelezionati }, ct);
            var esitoSelezione = InvioSapRegole.ValidaSelezione(richiesta, correnti.Select(c => c.ToCorrente()).ToList());
            if (esitoSelezione is not null)
                return esitoSelezione;
            numeroAtteso = idSelezionati.Count;
        }

        // 4. e 5. Transazione, commit e createRun.
        var notaJson = new InvioSapNota(DateTime.UtcNow, richiesta.Nota!.Trim(), InvioSapRegole.DescrizioneAzione).ToJson();
        return await InvioSapBatchWriter.Scrivi(factory, mediator, synapse, options, logger, authInfo,
            new InvioSapBatchDaScrivere(anno, mese, tipologia, modalita, numeroAtteso, VuotoComeNonTrovate: rigoIntero, Rotta: "genera"),
            async (uow, batchId, token) =>
            {
                if (rigoIntero)
                    return await uow.Execute(new InvioSapDettaglioPrimoInvioCommandPersistence(batchId, anno, mese, tipologia, notaJson), token);
                if (selezioneTotale)
                    return await uow.Execute(new InvioSapDettaglioSelezioneTotaleCommandPersistence(
                        batchId, anno, mese, tipologia, idEsclusi, inviate, storno, notaJson), token);
                return await uow.Execute(new InvioSapDettaglioSelezioneCommandPersistence(batchId, idSelezionati, inviate, storno, notaJson), token);
            },
            ct);
    }
}
