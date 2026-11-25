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
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Service;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.CommandHandlers;

/// <summary>Cosa scrive un Genera o un re-invio, una volta superate validazioni e letture.</summary>
internal sealed record InvioSapBatchDaScrivere(
    int Anno,
    int Mese,
    string Tipologia,
    InvioSapModalita Modalita,
    int NumeroAtteso,
    bool VuotoComeNonTrovate,
    string Rotta);

/// <summary>
/// Le scritture comuni a <c>genera</c> e <c>reinvia</c>, nell'ordine deciso con DATA il 10/11/2026
/// (be-invio-sap.md §8.5): lock per primo, Registro, aggancio del lock al batch, Dettaglio (l'unico passo che
/// cambia fra le due rotte, passato come delegato), <c>FatturaInviata = NULL</c> sulle fatture del batch, Log,
/// Messaggi; COMMIT; <c>createRun</c> DOPO il commit. Una sola copia di ordine, rollback e gestione degli errori.
///
/// Se <c>createRun</c> fallisce le righe restano scritte (lock, batch IN_ELABORAZIONE, fatture a NULL): per ora
/// nessuna compensazione, il punto e' aperto con DATA (D-m). Un errore PRIMA del commit fa rollback completo.
/// </summary>
internal static class InvioSapBatchWriter
{
    private const int ViolazionePrimaryKey = 2627;
    private const int ViolazioneIndiceUnivoco = 2601;

    /// <summary>
    /// Le righe del batch sono gia' confermate e il BE non le annulla (punto D-m, aperto con DATA): il lock resta
    /// attivo fino alla scadenza e le fatture restano "in elaborazione" finche' DATA non interviene.
    /// </summary>
    private const string MessaggioPipelineNonAvviata =
        "InvioSap {Rotta}: pipeline non avviata per il batch {BatchId} (messaggio {IdMessaggio}, esito {Esito}). " +
        "Le righe sono confermate: lock, batch IN_ELABORAZIONE e FatturaInviata = NULL restano finche' DATA non li ripulisce";

    public static async Task<InvioSapGeneraEsito> Scrivi(
        IFattureDbContextFactory factory,
        IMediator mediator,
        ISynapseService synapse,
        IPortaleFattureOptions options,
        ILogger logger,
        IAuthenticationInfo authInfo,
        InvioSapBatchDaScrivere batchDaScrivere,
        Func<IDbContext, int, CancellationToken, Task<IReadOnlyList<long>>> inserisciDettaglio,
        CancellationToken ct)
    {
        var (anno, mese, tipologia, modalita, numeroAtteso, vuotoComeNonTrovate, rotta) = batchDaScrivere;

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
                return await ConflittoSulLock(mediator, logger, authInfo, ct);
            }
            if (righeLock == 0)
            {
                uow.Rollback();
                throw new InvalidOperationException("Configurazione mancante: pfw.TipoAzione non contiene l'azione GeneraJsonFatture.");
            }

            batch = await uow.Execute(new InvioSapCreaBatchCommandPersistence(authInfo.Id!, numeroAtteso), ct);
            await uow.Execute(new InvioSapAssegnaBatchAlLockCommandPersistence(InvioSapRegole.IdAzioneGeneraJson, batch.BatchId), ct);

            inseriti = await inserisciDettaglio(uow, batch.BatchId, ct);

            if (inseriti.Count == 0 && vuotoComeNonTrovate)
            {
                uow.Rollback();
                return InvioSapGeneraEsito.NonTrovate();
            }
            if (inseriti.Count != numeroAtteso)
            {
                uow.Rollback();
                logger.LogInformation("InvioSap {Rotta}: rigo cambiato fra lettura e INSERT (attese {Attese}, inserite {Inserite})",
                    rotta, numeroAtteso, inseriti.Count);
                return InvioSapGeneraEsito.Conflitto(InvioSapMotivoConflitto.GRUPPO_CAMBIATO);
            }

            // Le fatture del batch passano a "in elaborazione": in griglia il rigo cambia stato subito.
            var marcate = await uow.Execute(new InvioSapMarcaInElaborazioneCommandPersistence(batch.BatchId), ct);
            if (marcate != inseriti.Count)
            {
                uow.Rollback();
                throw new InvalidOperationException(
                    $"InvioSap {rotta}: fatture portate in elaborazione ({marcate}) diverse dalle righe del dettaglio ({inseriti.Count}).");
            }

            // SerializeAlsoReadOnly: le proprieta' dei tipi anonimi sono read-only, e Serialize() le ignora (scriverebbe "{}").
            var evento = new
            {
                evento = "GENERAZIONE_JSON",
                rotta,
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

        // createRun DOPO il commit (richiesta di DATA): da qui non c'e' piu' nulla da annullare con un rollback.
        SynapseRunResult run;
        try
        {
            run = await synapse.AvviaGenerazioneJsonSap(options.Synapse?.PipelineNameSAP,
                InvioSapPipelineRequest.Crea(batch.BatchId, idMessaggio, tipologia, anno, mese), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, MessaggioPipelineNonAvviata, rotta, batch.BatchId, idMessaggio, "eccezione");
            return InvioSapGeneraEsito.PipelineFallita();
        }
        if (!run.Success)
        {
            logger.LogError(MessaggioPipelineNonAvviata, rotta, batch.BatchId, idMessaggio, run.StatusCode);
            return InvioSapGeneraEsito.PipelineFallita();
        }

        logger.LogInformation("InvioSap {Rotta}: batch {BatchId}, messaggio {IdMessaggio}, {Numero} fatture, run {RunId}",
            rotta, batch.BatchId, idMessaggio, inseriti.Count, run.RunId);
        return new InvioSapGeneraEsito
        {
            Tipo = InvioSapGeneraEsitoTipo.Ok,
            BatchId = batch.BatchId,
            DataAvvioUtc = DateTime.SpecifyKind(batch.DataAvvio, DateTimeKind.Utc),
            NumeroFatture = inseriti.Count,
            IdFatture = inseriti
        };
    }

    /// <summary>Lock attivo sulla vista: 409 prima di ogni altra lettura o scrittura (richiesta di DATA).</summary>
    public static async Task<InvioSapGeneraEsito?> LockAttivo(IMediator mediator, IAuthenticationInfo authInfo, CancellationToken ct)
    {
        var lockAttivo = await mediator.Send(new InvioSapLockQuery(authInfo), ct);
        return lockAttivo is null
            ? null
            : InvioSapGeneraEsito.Conflitto(InvioSapMotivoConflitto.LOCK_ATTIVO, lockAttivo.BatchId, lockAttivo.LockTimeoutUTC);
    }

    /// <summary>
    /// PK violata: o un altro Genera ha appena inserito il lock (la vista lo mostra), o la riga e' scaduta e
    /// DATA non l'ha ancora cancellata (la vista e' vuota). Il BE non cancella nulla: la pulizia e' di DATA.
    /// </summary>
    private static async Task<InvioSapGeneraEsito> ConflittoSulLock(IMediator mediator, ILogger logger, IAuthenticationInfo authInfo, CancellationToken ct)
    {
        var attivo = await LockAttivo(mediator, authInfo, ct);
        if (attivo is not null)
            return attivo;

        logger.LogWarning("InvioSap: riga di lock scaduta e non cancellata in pfw.LockAzionePF (azione {Azione}); la pulizia e' di DATA",
            InvioSapRegole.DescrizioneAzione);
        return InvioSapGeneraEsito.Conflitto(InvioSapMotivoConflitto.LOCK_NON_RILASCIATO);
    }

    private static void TryRollback(IDbContext uow)
    {
        try { uow.Rollback(); } catch (InvalidOperationException) { /* gia' chiusa */ }
    }
}
