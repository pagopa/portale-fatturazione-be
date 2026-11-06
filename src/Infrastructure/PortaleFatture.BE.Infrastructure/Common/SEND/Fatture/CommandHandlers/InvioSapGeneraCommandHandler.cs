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
/// 3. letture dalle viste: <c>NumeroFatture</c> del rigo (primo invio) o stato delle fatture (re-invio);
/// 4. transazione di soli INSERT: Registro, Lock, Dettaglio, Log, Messaggi; poi <c>createRun</c>, e commit
///    solo se la pipeline e' partita. Altrimenti rollback: nulla resta scritto.
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
        if (modalita == InvioSapModalita.PrimoInvio)
        {
            var numero = await mediator.Send(new InvioSapRigoDaInviareQuery(authInfo)
            {
                Anno = anno, Mese = mese, TipologiaFattura = tipologia
            }, ct);
            if (numero is null or <= 0)
                return InvioSapGeneraEsito.NonTrovate();
            numeroAtteso = numero.Value;
        }
        else
        {
            idSelezionati = richiesta.IdFatture!;
            var correnti = await mediator.Send(new InvioSapStatoFattureQuery(authInfo) { IdFatture = idSelezionati }, ct);
            var esitoReInvio = InvioSapRegole.ValidaReInvio(richiesta, correnti.Select(c => c.ToCorrente()).ToList());
            if (esitoReInvio is not null)
                return esitoReInvio;
            numeroAtteso = idSelezionati.Count;
        }

        // 4. Transazione di soli INSERT.
        var notaJson = new InvioSapNota(DateTime.UtcNow, richiesta.Nota!.Trim(), InvioSapRegole.DescrizioneAzione).ToJson();
        using var uow = await factory.Create(true, IsolationLevel.ReadCommitted, ct);
        try
        {
            var batch = await uow.Execute(new InvioSapCreaBatchCommandPersistence(authInfo.Id!, numeroAtteso), ct);

            int righeLock;
            try
            {
                righeLock = await uow.Execute(new InvioSapInserisciLockCommandPersistence(InvioSapRegole.IdAzioneGeneraJson, batch.BatchId), ct);
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

            IReadOnlyList<long> inseriti = modalita == InvioSapModalita.PrimoInvio
                ? await uow.Execute(new InvioSapDettaglioPrimoInvioCommandPersistence(batch.BatchId, anno, mese, tipologia, notaJson), ct)
                : await uow.Execute(new InvioSapDettaglioReInvioCommandPersistence(batch.BatchId, idSelezionati, richiesta.NumeroDocumentoStorno!.Trim(), notaJson), ct);

            if (inseriti.Count == 0 && modalita == InvioSapModalita.PrimoInvio)
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

            var idMessaggio = await uow.Execute(new InvioSapMessaggioCreateCommandPersistence(new InvioSapMessaggio
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

            var run = await synapse.AvviaGenerazioneJsonSap(options.Synapse?.PipelineNameSAP,
                InvioSapPipelineRequest.Crea(batch.BatchId, idMessaggio, tipologia, anno, mese), ct);
            if (!run.Success)
            {
                uow.Rollback();
                logger.LogError("InvioSap Genera: pipeline non avviata per il batch {BatchId} (status {Status})", batch.BatchId, run.StatusCode);
                return InvioSapGeneraEsito.PipelineFallita();
            }

            uow.Commit();
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
        catch
        {
            TryRollback(uow);
            throw;
        }
    }

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
