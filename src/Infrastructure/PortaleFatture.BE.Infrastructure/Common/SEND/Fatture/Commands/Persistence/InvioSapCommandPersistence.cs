using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

// Scritture dell'Invio a SAP (be-invio-sap.md §2.1 e §8.4). Elenco chiuso, concordato con DATA il 10/11/2026:
// INSERT in Lock, Registro, Dettaglio, Log e Messaggi, piu' due UPDATE sulle sole righe del proprio batch
// (FkBatchId della riga di lock appena inserita; FatturaInviata = NULL delle fatture del batch).
// Il BE non cancella mai nulla: stato finale del batch, valore finale di FatturaInviata, messaggio e
// cancellazione del lock sono di DATA. Tutte richiedono la transazione del Genera.
// OUTPUT sempre con INTO in una variabile tabella: un OUTPUT senza INTO fallisce (errore 334) se la tabella
// ha un trigger abilitato, e le tabelle sono di DATA, che potrebbe aggiungerne uno senza avvisare.

public sealed class InvioSapBatchCreato
{
    public int BatchId { get; set; }
    public DateTime DataAvvio { get; set; }
}

/// <summary>Nuova riga del Registro in <c>IN_ELABORAZIONE</c>; restituisce <c>BatchId</c> e <c>DataAvvio</c> (UTC).</summary>
public sealed class InvioSapCreaBatchCommandPersistence(string idUtente, int countFatture) : DapperBase, ICommand<InvioSapBatchCreato>
{
    public bool RequiresTransaction => true;

    private const string Sql = @"
        DECLARE @Creato TABLE (BatchId INT, DataAvvio DATETIME2(7));
        INSERT INTO [pfw].[RegistroInvioSAP] (Stato, IdUtente, DataAvvio, CountFatture)
        OUTPUT INSERTED.BatchId, INSERTED.DataAvvio INTO @Creato
        VALUES (@Stato, @IdUtente, SYSUTCDATETIME(), @CountFatture);
        SELECT BatchId, DataAvvio FROM @Creato;";

    public async Task<InvioSapBatchCreato> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
        => (await ((IDatabase)this).ExecuteAsync<InvioSapBatchCreato>(connection!, Sql,
            new { Stato = StatoBatchSap.IN_ELABORAZIONE, IdUtente = idUtente, CountFatture = countFatture }, transaction))!;
}

/// <summary>
/// Riga di lock con il timeout di <c>pfw.TipoAzione</c>: e' la PRIMA scrittura del Genera (richiesta di DATA,
/// 10/11/2026), quindi il batch non esiste ancora e <c>FkBatchId</c> nasce NULL. La PK su <c>FkIdAzione</c>
/// risolve la gara fra due Genera (errore 2627 al secondo), prima che il perdente stacchi un <c>BatchId</c>.
/// Restituisce le righe inserite: 0 se manca la riga di <c>TipoAzione</c>.
/// </summary>
public sealed class InvioSapInserisciLockCommandPersistence(Guid idAzione) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => true;

    private const string Sql = @"
        INSERT INTO [pfw].[LockAzionePF] (FkIdAzione, lockStart, lockTimeout, FkBatchId)
        SELECT ta.IdAzione, SYSUTCDATETIME(), DATEADD(MILLISECOND, ta.Timeout_millis, SYSUTCDATETIME()), NULL
          FROM [pfw].[TipoAzione] ta
         WHERE ta.IdAzione = @IdAzione;";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
        => await ((IDatabase)this).ExecuteAsync(connection!, Sql, new { IdAzione = idAzione }, transaction);
}

/// <summary>
/// Lega la riga di lock appena inserita al batch: senza, <c>GET lock</c> e il 409 <c>LOCK_ATTIVO</c> non
/// riporterebbero il <c>batchId</c>. Tocca solo la riga inserita dallo stesso Genera, nella stessa transazione.
/// </summary>
public sealed class InvioSapAssegnaBatchAlLockCommandPersistence(Guid idAzione, int batchId) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => true;

    private const string Sql = @"
        UPDATE [pfw].[LockAzionePF] SET FkBatchId = @BatchId WHERE FkIdAzione = @IdAzione;";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
        => await ((IDatabase)this).ExecuteAsync(connection!, Sql, new { IdAzione = idAzione, BatchId = batchId }, transaction);
}

/// <summary>
/// Porta a NULL ("in elaborazione") <c>FatturaInviata</c> delle fatture del batch: lo fa il BE, che sa quali
/// fatture sono state scelte (decisione di DATA del 10/11/2026). L'elenco e' il Dettaglio del batch, quindi vale
/// per il primo invio e per il re-invio. Restituisce le righe aggiornate: devono essere quante quelle del Dettaglio.
/// </summary>
public sealed class InvioSapMarcaInElaborazioneCommandPersistence(int batchId) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => true;

    private const string Sql = @"
        UPDATE ft SET ft.FatturaInviata = NULL
          FROM [pfd].[FattureTestata] ft
          JOIN [pfw].[DettaglioInvioSAP] d ON d.FkIdFattura = ft.IdFattura
         WHERE d.FkBatchId = @BatchId;";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
        => await ((IDatabase)this).ExecuteAsync(connection!, Sql, new { BatchId = batchId }, transaction);
}

/// <summary>
/// Primo invio del rigo intero: tutte le fatture del rigo <c>DA_INVIARE</c> (chiave D10: Tipologia, Anno, Mese,
/// <c>FatturaInviata = 0</c>), da NON INVIATA (4) a INVIATA (5), senza storno. Restituisce gli id inseriti.
/// Le fatture posticipate (0) o eliminate (3) in Gestione Fatture restano fuori, con la stessa condizione di
/// <c>be.vwInvioSAPGriglia</c> e <c>be.vwInvioSAPDettaglio</c> (versione 1.1 di DATA): il numero inserito deve
/// coincidere con il <c>NumeroFatture</c> del rigo, altrimenti il Genera risponde 409 GRUPPO_CAMBIATO.
/// </summary>
public sealed class InvioSapDettaglioPrimoInvioCommandPersistence(int batchId, int anno, int mese, string tipologia, string notaJson)
    : DapperBase, ICommand<IReadOnlyList<long>>
{
    public bool RequiresTransaction => true;

    private const string Sql = @"
        DECLARE @Inseriti TABLE (IdFattura BIGINT);
        INSERT INTO [pfw].[DettaglioInvioSAP] (FkBatchId, FkIdFattura, FkStatoPrecedente, FkStato, NumeroDocStorno, Nota)
        OUTPUT INSERTED.FkIdFattura INTO @Inseriti
        SELECT @BatchId, ft.IdFattura, @StatoPrecedente, @Stato, NULL, @Nota
          FROM [pfd].[FattureTestata] ft
         WHERE ft.AnnoRiferimento = @Anno
           AND ft.MeseRiferimento = @Mese
           AND ft.FkTipologiaFattura = @TipologiaFattura
           AND ft.FatturaInviata = 0
           AND NOT EXISTS (SELECT 1
                             FROM [cfg].[GestioneFatture] gf
                            WHERE gf.FkIdFattura = ft.IdFattura
                              AND gf.Stato IN (0, 3));
        SELECT IdFattura FROM @Inseriti;";

    public async Task<IReadOnlyList<long>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
        => (await ((IDatabase)this).SelectAsync<long>(connection!, Sql, new
        {
            BatchId = batchId,
            StatoPrecedente = StatoFatturaSap.NonInviata,
            Stato = StatoFatturaSap.Inviata,
            Nota = notaJson,
            Anno = anno,
            Mese = mese,
            TipologiaFattura = tipologia
        }, transaction)).OrderBy(id => id).ToList();
}

/// <summary>
/// Le fatture scelte una per una. Re-invio (<c>inviate = true</c>): da INVIATA (5) a INVIATA (5), con lo storno.
/// Primo invio (<c>inviate = false</c>): da NON INVIATA (4) a INVIATA (5), senza storno. Le righe si prendono dalla
/// vista dettaglio nello stato richiesto: se nel frattempo una fattura e' cambiata, non viene inserita e l'handler
/// se ne accorge dal numero di righe.
/// </summary>
public sealed class InvioSapDettaglioSelezioneCommandPersistence(int batchId, IReadOnlyList<long> idFatture, bool inviate, string? storno, string notaJson)
    : DapperBase, ICommand<IReadOnlyList<long>>
{
    public bool RequiresTransaction => true;

    private const string Sql = @"
        DECLARE @Inseriti TABLE (IdFattura BIGINT);
        INSERT INTO [pfw].[DettaglioInvioSAP] (FkBatchId, FkIdFattura, FkStatoPrecedente, FkStato, NumeroDocStorno, Nota)
        OUTPUT INSERTED.FkIdFattura INTO @Inseriti
        SELECT @BatchId, v.IdFattura, @StatoPrecedente, @Stato, @NumeroDocStorno, @Nota
          FROM [be].[vwInvioSAPDettaglio] v
         WHERE v.IdFattura IN @IdFatture
           AND v.FatturaInviata = @Inviata;
        SELECT IdFattura FROM @Inseriti;";

    public async Task<IReadOnlyList<long>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
        => (await ((IDatabase)this).SelectAsync<long>(connection!, Sql, new
        {
            BatchId = batchId,
            StatoPrecedente = inviate ? StatoFatturaSap.Inviata : StatoFatturaSap.NonInviata,
            Stato = StatoFatturaSap.Inviata,
            NumeroDocStorno = inviate ? storno : null,
            Nota = notaJson,
            Inviata = inviate,
            IdFatture = idFatture.Distinct().ToArray()
        }, transaction)).OrderBy(id => id).ToList();
}

/// <summary>
/// "Seleziona tutto": tutte le fatture del rigo nello stato richiesto tranne le escluse. Re-invio
/// (<c>inviate = true</c>): 5 → 5 con lo storno; primo invio (<c>inviate = false</c>): 4 → 5 senza storno. Stessa
/// condizione del conteggio fatto prima della transazione (<c>WhereSelezioneTotale</c>): se il rigo e' cambiato nel
/// frattempo l'handler se ne accorge dal numero di righe. Nessun elenco di id da passare, quindi nessun tetto sul
/// numero di fatture.
/// </summary>
public sealed class InvioSapDettaglioSelezioneTotaleCommandPersistence(
    int batchId, int anno, int mese, string tipologia, IReadOnlyList<long> idFattureEscluse, bool inviate, string? storno, string notaJson)
    : DapperBase, ICommand<IReadOnlyList<long>>
{
    public bool RequiresTransaction => true;

    public async Task<IReadOnlyList<long>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        var escluse = idFattureEscluse.Distinct().ToArray();
        var sql = @"
        DECLARE @Inseriti TABLE (IdFattura BIGINT);
        INSERT INTO [pfw].[DettaglioInvioSAP] (FkBatchId, FkIdFattura, FkStatoPrecedente, FkStato, NumeroDocStorno, Nota)
        OUTPUT INSERTED.FkIdFattura INTO @Inseriti
        SELECT @BatchId, v.IdFattura, @StatoPrecedente, @Stato, @NumeroDocStorno, @Nota
          FROM [be].[vwInvioSAPDettaglio] v" + InvioSapQueryBuilder.WhereSelezioneTotale(escluse.Length > 0) + @";
        SELECT IdFattura FROM @Inseriti;";

        return (await ((IDatabase)this).SelectAsync<long>(connection!, sql, new
        {
            BatchId = batchId,
            StatoPrecedente = inviate ? StatoFatturaSap.Inviata : StatoFatturaSap.NonInviata,
            Stato = StatoFatturaSap.Inviata,
            NumeroDocStorno = inviate ? storno : null,
            Nota = notaJson,
            Inviata = inviate,
            Anno = anno,
            Mese = mese,
            TipologiaFattura = tipologia,
            IdFattureEscluse = escluse
        }, transaction)).OrderBy(id => id).ToList();
    }
}

/// <summary>
/// Messaggio della generazione in <c>pfd.Messaggi</c>, con <c>OUTPUT INSERTED.IdMessaggio</c>: l'id va nel
/// body della pipeline, e DATA aggiorna il messaggio tramite quello (D3). <c>Hash</c> (NOT NULL) = BatchId.
/// </summary>
public sealed class InvioSapMessaggioCreateCommandPersistence(InvioSapMessaggio messaggio) : DapperBase, ICommand<long>
{
    public bool RequiresTransaction => true;

    private const string Sql = @"
        DECLARE @Creato TABLE (IdMessaggio BIGINT);
        INSERT INTO [pfd].[Messaggi]
            (IdEnte, IdUtente, Json, Anno, Mese, Prodotto, GruppoRuolo, Auth, Stato, DataInserimento,
             LinkDocumento, ContentLanguage, ContentType, TipologiaDocumento, CategoriaDocumento, Lettura, Hash, FKIdReport)
        OUTPUT INSERTED.IdMessaggio INTO @Creato
        VALUES
            (@IdEnte, @IdUtente, @Json, @Anno, @Mese, @Prodotto, @GruppoRuolo, @Auth, @Stato, @DataInserimento,
             NULL, @ContentLanguage, NULL, @TipologiaDocumento, @CategoriaDocumento, 0, @Hash, NULL);
        SELECT IdMessaggio FROM @Creato;";

    public async Task<long> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
        => await ((IDatabase)this).ExecuteAsync<long>(connection!, Sql, messaggio, transaction);
}

public sealed class InvioSapMessaggio
{
    public string? IdEnte { get; init; }
    public string IdUtente { get; init; } = string.Empty;
    public string? Json { get; init; }
    public int Anno { get; init; }
    public int Mese { get; init; }
    public string Prodotto { get; init; } = string.Empty;
    public string? GruppoRuolo { get; init; }
    public string Auth { get; init; } = string.Empty;
    public short Stato { get; init; }
    public DateTime DataInserimento { get; init; }
    public string ContentLanguage { get; init; } = "it";
    public string TipologiaDocumento { get; init; } = string.Empty;
    public string? CategoriaDocumento { get; init; }
    public string Hash { get; init; } = string.Empty;
}
