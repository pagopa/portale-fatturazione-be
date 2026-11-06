using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

// Scritture dell'Invio a SAP (be-invio-sap.md §8.4). Regola di DATA: il BE fa SOLO INSERT; gli UPDATE e la
// cancellazione del lock li fa DATA. Tutte richiedono la transazione del Genera.
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
/// Riga di lock con il timeout di <c>pfw.TipoAzione</c>. La PK su <c>FkIdAzione</c> risolve la gara fra due
/// Genera (errore 2627 al secondo). Restituisce le righe inserite: 0 se manca la riga di <c>TipoAzione</c>.
/// </summary>
public sealed class InvioSapInserisciLockCommandPersistence(Guid idAzione, int batchId) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => true;

    private const string Sql = @"
        INSERT INTO [pfw].[LockAzionePF] (FkIdAzione, lockStart, lockTimeout, FkBatchId)
        SELECT ta.IdAzione, SYSUTCDATETIME(), DATEADD(MILLISECOND, ta.Timeout_millis, SYSUTCDATETIME()), @BatchId
          FROM [pfw].[TipoAzione] ta
         WHERE ta.IdAzione = @IdAzione;";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
        => await ((IDatabase)this).ExecuteAsync(connection!, Sql, new { IdAzione = idAzione, BatchId = batchId }, transaction);
}

/// <summary>
/// Primo invio: tutte le fatture del rigo <c>DA_INVIARE</c> (chiave D10: Tipologia, Anno, Mese,
/// <c>FatturaInviata = 0</c>), da NON INVIATA (4) a INVIATA (5), senza storno. Restituisce gli id inseriti.
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
           AND ft.FatturaInviata = 0;
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
/// Re-invio: le fatture scelte, da INVIATA (5) a INVIATA (5), con lo storno. Le righe si prendono dalla vista
/// dettaglio con <c>FatturaInviata = 1</c>: se nel frattempo una fattura e' cambiata, non viene inserita e
/// l'handler se ne accorge dal numero di righe.
/// </summary>
public sealed class InvioSapDettaglioReInvioCommandPersistence(int batchId, IReadOnlyList<long> idFatture, string storno, string notaJson)
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
           AND v.FatturaInviata = 1;
        SELECT IdFattura FROM @Inseriti;";

    public async Task<IReadOnlyList<long>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
        => (await ((IDatabase)this).SelectAsync<long>(connection!, Sql, new
        {
            BatchId = batchId,
            StatoPrecedente = StatoFatturaSap.Inviata,
            Stato = StatoFatturaSap.Inviata,
            NumeroDocStorno = storno,
            Nota = notaJson,
            IdFatture = idFatture.Distinct().ToArray()
        }, transaction)).OrderBy(id => id).ToList();
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
