using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Entities.Messaggi;
using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;
using PortaleFatture.BE.Core.Entities.Storici;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Service;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.CommandHandlers;

/// <summary>
/// "Genera JSON", passo per passo come in be-invio-sap.md §8.5:
/// 1. lock sulla vista (409 senza altre letture ne' scritture);
/// 2. validazione formale (400);
/// 3. letture dalle viste: <c>NumeroFatture</c> del rigo (primo invio del rigo intero), stato delle fatture scelte
///    (elenco) oppure, con "seleziona tutto", controllo delle escluse e numero delle fatture del rigo che restano.
///    Elenco e "seleziona tutto" valgono sia per il re-invio (fatture inviate) sia, con <c>statoInvio</c>
///    <c>DA_INVIARE</c>, per il primo invio (fatture da inviare);
/// 4. transazione (ordine deciso con DATA il 10/11/2026): Lock per primo, Registro, Dettaglio,
///    <c>FatturaInviata = NULL</c> sulle fatture del batch, Log, Messaggi; poi COMMIT;
/// 5. <c>createRun</c> DOPO il commit: la pipeline legge sempre righe confermate.
///
/// Se <c>createRun</c> fallisce le righe restano scritte (lock, batch IN_ELABORAZIONE, fatture a NULL): per ora
/// nessuna compensazione, il punto e' aperto con DATA (D-m). Un errore PRIMA del commit fa rollback completo.
/// </summary>
public sealed class InvioSapGeneraCommandHandler(
    IFattureDbContextFactory factory,
    IMediator mediator,
    ISynapseService synapse,
    IPortaleFattureOptions options,
    ILogger<InvioSapGeneraCommandHandler> logger) : IRequestHandler<InvioSapGeneraCommand, InvioSapGeneraEsito>
{
    private const int ViolazionePrimaryKey = 2627;
    private const int ViolazioneIndiceUnivoco = 2601;

    public async Task<InvioSapGeneraEsito> Handle(InvioSapGeneraCommand command, CancellationToken ct)
    {
        var authInfo = command.AuthenticationInfo;

        // 1. Lock sulla vista, prima di qualunque altra cosa (richiesta di DATA).
        var lockAttivo = await mediator.Send(new InvioSapLockQuery(authInfo), ct);
        if (lockAttivo is not null)
            return InvioSapGeneraEsito.Conflitto(InvioSapMotivoConflitto.LOCK_ATTIVO, lockAttivo.BatchId, lockAttivo.LockTimeoutUTC);

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

        // 4. Transazione: il lock per primo, poi il batch e le sue righe.
        var notaJson = new InvioSapNota(DateTime.UtcNow, richiesta.Nota!.Trim(), InvioSapRegole.DescrizioneAzione).ToJson();
        InvioSapBatchCreato batch;
        IReadOnlyList<long> inseriti;
        long idMessaggio;
        using var uow = await factory.Create(true, IsolationLevel.ReadCommitted, ct);
        try
        {
            int righeLock;
            try
            {
                righeLock = await uow.Execute(new InvioSapInserisciLockCommandPersistence(InvioSapRegole.IdAzioneGeneraJson), ct);
            }
            catch (SqlException ex) when (ex.Number is ViolazionePrimaryKey or ViolazioneIndiceUnivoco)
            {
                uow.Rollback();
                return await ConflittoSulLock(authInfo, ct);
            }
            if (righeLock == 0)
            {
                uow.Rollback();
                throw new InvalidOperationException("Configurazione mancante: pfw.TipoAzione non contiene l'azione GeneraJsonFatture.");
            }

            batch = await uow.Execute(new InvioSapCreaBatchCommandPersistence(authInfo.Id!, numeroAtteso), ct);
            await uow.Execute(new InvioSapAssegnaBatchAlLockCommandPersistence(InvioSapRegole.IdAzioneGeneraJson, batch.BatchId), ct);

            if (rigoIntero)
                inseriti = await uow.Execute(new InvioSapDettaglioPrimoInvioCommandPersistence(batch.BatchId, anno, mese, tipologia, notaJson), ct);
            else if (selezioneTotale)
                inseriti = await uow.Execute(new InvioSapDettaglioSelezioneTotaleCommandPersistence(
                    batch.BatchId, anno, mese, tipologia, idEsclusi, inviate, storno, notaJson), ct);
            else
                inseriti = await uow.Execute(new InvioSapDettaglioSelezioneCommandPersistence(batch.BatchId, idSelezionati, inviate, storno, notaJson), ct);

            if (inseriti.Count == 0 && rigoIntero)
            {
                uow.Rollback();
                return InvioSapGeneraEsito.NonTrovate();
            }
            if (inseriti.Count != numeroAtteso)
            {
                uow.Rollback();
                logger.LogInformation("InvioSap Genera: rigo cambiato fra lettura e INSERT (attese {Attese}, inserite {Inserite})", numeroAtteso, inseriti.Count);
                return InvioSapGeneraEsito.Conflitto(InvioSapMotivoConflitto.GRUPPO_CAMBIATO);
            }

            // Le fatture del batch passano a "in elaborazione": in griglia il rigo cambia stato subito.
            var marcate = await uow.Execute(new InvioSapMarcaInElaborazioneCommandPersistence(batch.BatchId), ct);
            if (marcate != inseriti.Count)
            {
                uow.Rollback();
                throw new InvalidOperationException(
                    $"InvioSap Genera: fatture portate in elaborazione ({marcate}) diverse dalle righe del dettaglio ({inseriti.Count}).");
            }

            // SerializeAlsoReadOnly: le proprieta' dei tipi anonimi sono read-only, e Serialize() le ignora (scriverebbe "{}").
            var evento = new
            {
                evento = "GENERAZIONE_JSON",
                batchId = batch.BatchId,
                anno,
                mese,
                tipologiaFattura = tipologia,
                modalita = modalita.ToString(),
                numeroFatture = inseriti.Count
            };
            await uow.Execute(new StoricoCreateCommandPersistence(new StoricoCreateCommand(
                authInfo, DateTime.UtcNow.ItalianTime(), TipoStorico.InvioSAP, evento.SerializeAlsoReadOnly())), ct);

            idMessaggio = await uow.Execute(new InvioSapMessaggioCreateCommandPersistence(new InvioSapMessaggio
            {
                IdEnte = string.IsNullOrWhiteSpace(authInfo.IdEnte) ? null : authInfo.IdEnte,
                IdUtente = authInfo.Id!,
                Json = new { batchId = batch.BatchId, tipologiaFattura = tipologia, anno, mese, numeroFatture = inseriti.Count }.SerializeAlsoReadOnly(),
                Anno = anno,
                Mese = mese,
                Prodotto = string.IsNullOrWhiteSpace(authInfo.Prodotto) ? Product.ProdPN : authInfo.Prodotto,
                GruppoRuolo = authInfo.GruppoRuolo,
                Auth = authInfo.Auth ?? string.Empty,
                Stato = TipologiaStatoMessaggio.Elaborazione,
                DataInserimento = DateTime.UtcNow.ItalianTime(),
                TipologiaDocumento = TipologiaDocumento.InvioSap,
                CategoriaDocumento = "GENERAZIONE JSON",
                Hash = batch.BatchId.ToString()
            }), ct);

            uow.Commit();
        }
        catch
        {
            TryRollback(uow);
            throw;
        }

        // 5. createRun DOPO il commit (richiesta di DATA): da qui non c'e' piu' nulla da annullare con un rollback.
        SynapseRunResult run;
        try
        {
            run = await synapse.AvviaGenerazioneJsonSap(options.Synapse?.PipelineNameSAP,
                InvioSapPipelineRequest.Crea(batch.BatchId, idMessaggio, tipologia, anno, mese), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, MessaggioPipelineNonAvviata, batch.BatchId, idMessaggio, "eccezione");
            return InvioSapGeneraEsito.PipelineFallita();
        }
        if (!run.Success)
        {
            logger.LogError(MessaggioPipelineNonAvviata, batch.BatchId, idMessaggio, run.StatusCode);
            return InvioSapGeneraEsito.PipelineFallita();
        }

        logger.LogInformation("InvioSap Genera: batch {BatchId}, messaggio {IdMessaggio}, {Numero} fatture, run {RunId}",
            batch.BatchId, idMessaggio, inseriti.Count, run.RunId);
        return new InvioSapGeneraEsito
        {
            Tipo = InvioSapGeneraEsitoTipo.Ok,
            BatchId = batch.BatchId,
            DataAvvioUtc = DateTime.SpecifyKind(batch.DataAvvio, DateTimeKind.Utc),
            NumeroFatture = inseriti.Count,
            IdFatture = inseriti
        };
    }

    /// <summary>
    /// Le righe del batch sono gia' confermate e il BE non le annulla (punto D-m, aperto con DATA): il lock resta
    /// attivo fino alla scadenza e le fatture restano "in elaborazione" finche' DATA non interviene.
    /// </summary>
    private const string MessaggioPipelineNonAvviata =
        "InvioSap Genera: pipeline non avviata per il batch {BatchId} (messaggio {IdMessaggio}, esito {Esito}). " +
        "Le righe sono confermate: lock, batch IN_ELABORAZIONE e FatturaInviata = NULL restano finche' DATA non li ripulisce";

    /// <summary>
    /// PK violata: o un altro Genera ha appena inserito il lock (la vista lo mostra), o la riga e' scaduta e
    /// DATA non l'ha ancora cancellata (la vista e' vuota). Il BE non cancella nulla: la pulizia e' di DATA.
    /// </summary>
    private async Task<InvioSapGeneraEsito> ConflittoSulLock(IAuthenticationInfo authInfo, CancellationToken ct)
    {
        var attivo = await mediator.Send(new InvioSapLockQuery(authInfo), ct);
        if (attivo is not null)
            return InvioSapGeneraEsito.Conflitto(InvioSapMotivoConflitto.LOCK_ATTIVO, attivo.BatchId, attivo.LockTimeoutUTC);

        logger.LogWarning("InvioSap Genera: riga di lock scaduta e non cancellata in pfw.LockAzionePF (azione {Azione}); la pulizia e' di DATA",
            InvioSapRegole.DescrizioneAzione);
        return InvioSapGeneraEsito.Conflitto(InvioSapMotivoConflitto.LOCK_NON_RILASCIATO);
    }

    private static void TryRollback(PortaleFatture.BE.Infrastructure.Common.Persistence.IDbContext uow)
    {
        try { uow.Rollback(); } catch (InvalidOperationException) { /* gia' chiusa */ }
    }
}
