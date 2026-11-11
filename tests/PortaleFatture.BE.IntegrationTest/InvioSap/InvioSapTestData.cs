using Microsoft.Data.SqlClient;

namespace PortaleFatture.BE.IntegrationTest.InvioSap;

/// <summary>
/// Punto unico di contatto fra i test dell'Invio a SAP (PF-965) e il DB seedato: rotte, nomi degli oggetti
/// di DATA, costanti del seed <c>tests/Data/invio_sap.sql</c> (anno dedicato 2003) e helper che simulano
/// DATA. Dal 10/11/2026 il BE, oltre agli INSERT, porta a NULL <c>FatturaInviata</c> delle fatture del batch e
/// valorizza <c>FkBatchId</c> del proprio lock; non cancella mai nulla. Ogni altro UPDATE o DELETE qui dentro e'
/// "DATA" o pulizia dei test.
/// </summary>
public static class InvioSapTestData
{
    public const string RottaGriglia = "/api/fatture/invio-sap";
    public const string RottaDettaglio = "/api/fatture/invio-sap/dettaglio";
    public const string RottaDownload = "/api/fatture/invio-sap/download";
    public const string RottaDownloadReinviate = "/api/fatture/invio-sap/download/reinviate";
    public const string RottaGenera = "/api/fatture/invio-sap/genera";
    public const string RottaLock = "/api/fatture/invio-sap/lock";
    public const string RottaStati = "/api/fatture/invio-sap/stati";
    public const string RottaAnni = "/api/fatture/invio-sap/anni";
    public const string RottaMesi = "/api/fatture/invio-sap/mesi";
    public const string RottaTipologie = "/api/fatture/invio-sap/tipologie";

    public const string Registro = "pfw.RegistroInvioSAP";
    public const string Dettaglio = "pfw.DettaglioInvioSAP";
    public const string Lock = "pfw.LockAzionePF";
    public const string TipoAzione = "pfw.TipoAzione";
    public const string VistaGriglia = "be.vwInvioSAPGriglia";
    public const string VistaDettaglio = "be.vwInvioSAPDettaglio";
    public const string VistaReinviate = "be.vwInvioSAPReportFattureReInviate";
    public const string VistaLock = "be.vwLockAttiviAzioniPF";

    public static readonly Guid IdAzione = new("0B102032-99F2-4AF9-BC5E-EBE844171DCC");

    /// <summary>Utente emesso da TestAuthHandler: i batch creati dai test hanno questo <c>IdUtente</c>.</summary>
    public const string UtenteTest = "integration-test-user";

    // ---- Seed 2003 (be-invio-sap.md §13.2) ----------------------------------------------------
    public const int Anno = 2003;

    /// <summary>Rigo DA_INVIARE del primo invio: PRIMO SALDO 2003/2, fatture 9505 e 9506.</summary>
    public const string TipologiaPrimoInvio = "PRIMO SALDO";
    public const int MesePrimoInvio = 2;
    public static readonly long[] FattureRigoDaInviare = [9505, 9506];
    public const long FatturaStessoMeseInviata = 9507;
    public const long FatturaStessoMeseInElaborazione = 9508;
    public const long FatturaStessoMeseAltraTipologia = 9509;

    /// <summary>Rigo INVIATE del re-invio: PRIMO SALDO 2003/1, fatture 9502 e 9503.</summary>
    public const string TipologiaReInvio = "PRIMO SALDO";
    public const int MeseReInvio = 1;
    public static readonly long[] FattureRigoInviate = [9502, 9503];
    public const long FatturaMigrataUnaVolta = 9501;
    public const long FatturaVarSemestrale = 9504;
    public const long FatturaEntePagoPA = 9510;
    public const string EntePagoPA = "4a4149af-172e-4950-9cc8-63ccc9a6d865";

    public const int BatchMigrazione = 9001;
    public const int BatchErrore = 9003;
    public const int BatchUltimoDi9502 = 9004;
    public const int UltimoBatchSeed = 9005;

    /// <summary><c>FatturaInviata</c> del seed, per ripristinarla dopo un test che simula DATA.</summary>
    private static readonly (long id, bool? inviata)[] StatoSeed =
    [
        (9501, true), (9502, true), (9503, true), (9504, true), (9505, false),
        (9506, false), (9507, true), (9508, null), (9509, false), (9510, false)
    ];

    public static void SkipSeOggettiAssenti() =>
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString,
            Registro, Dettaglio, Lock, TipoAzione, VistaGriglia, VistaDettaglio, VistaReinviate, VistaLock, "pfd.Messaggi");

    public static SqlConnection Apri()
    {
        var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        return conn;
    }

    public static object? Scalar(string sql, params (string nome, object? valore)[] parametri)
    {
        using var conn = Apri();
        using var cmd = Comando(conn, sql, parametri);
        var r = cmd.ExecuteScalar();
        return r is DBNull ? null : r;
    }

    public static int Esegui(string sql, params (string nome, object? valore)[] parametri)
    {
        using var conn = Apri();
        using var cmd = Comando(conn, sql, parametri);
        return cmd.ExecuteNonQuery();
    }

    public static List<T> Lista<T>(string sql, params (string nome, object? valore)[] parametri)
    {
        using var conn = Apri();
        using var cmd = Comando(conn, sql, parametri);
        using var reader = cmd.ExecuteReader();
        var lista = new List<T>();
        while (reader.Read()) lista.Add(reader.IsDBNull(0) ? default! : (T)Convert.ChangeType(reader.GetValue(0), Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)));
        return lista;
    }

    private static SqlCommand Comando(SqlConnection conn, string sql, (string nome, object? valore)[] parametri)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (nome, valore) in parametri) cmd.Parameters.AddWithValue(nome, valore ?? DBNull.Value);
        return cmd;
    }

    // ---- Lock --------------------------------------------------------------------------------

    /// <summary>Come fa DATA a fine batch: cancella la riga di lock dell'azione.</summary>
    public static void RilasciaLockComeData() =>
        Esegui($"DELETE FROM {Lock} WHERE FkIdAzione = @a", ("@a", IdAzione));

    /// <summary>Lock attivo (UTC futuro) di un batch, come se un Genera fosse in corso.</summary>
    public static void AttivaLock(int? batchId = BatchErrore)
    {
        RilasciaLockComeData();
        Esegui($@"INSERT INTO {Lock} (FkIdAzione, lockStart, lockTimeout, FkBatchId)
                  VALUES (@a, SYSUTCDATETIME(), DATEADD(HOUR, 1, SYSUTCDATETIME()), @b)", ("@a", IdAzione), ("@b", batchId));
    }

    /// <summary>Riporta la riga di lock SCADUTA del seed (C9), stato di partenza di ogni test.</summary>
    public static void RipristinaLockSeed()
    {
        RilasciaLockComeData();
        Esegui($@"INSERT INTO {Lock} (FkIdAzione, lockStart, lockTimeout, FkBatchId)
                  VALUES (@a, '2020-01-01T09:00:00', '2020-01-01T10:00:00', @b)", ("@a", IdAzione), ("@b", BatchErrore));
    }

    public static int? BatchDelLock() =>
        Scalar($"SELECT FkBatchId FROM {Lock} WHERE FkIdAzione = @a", ("@a", IdAzione)) as int?;

    // ---- Simulazione di DATA -----------------------------------------------------------------

    /// <summary><c>FatturaInviata</c> di una fattura: false = da inviare, true = inviata, null = in elaborazione.</summary>
    public static bool? FatturaInviata(long idFattura) =>
        Scalar("SELECT FatturaInviata FROM pfd.FattureTestata WHERE IdFattura = @id", ("@id", idFattura)) as bool?;

    public static long UltimoBatchStaccato() =>
        Convert.ToInt64(Scalar($"SELECT IDENT_CURRENT('{Registro}')"));

    /// <summary>
    /// DATA chiude il batch: stato finale, file, valore finale di <c>FatturaInviata</c> e cancellazione del lock.
    /// Con <c>ERRORE</c> (ramo di uscita della pipeline) le fatture tornano allo stato precedente: 4 → da inviare,
    /// 5 → inviata. E' l'assunzione D-o, da confermare con DATA.
    /// </summary>
    public static void ChiudiBatchComeData(int batchId, string stato = "COMPLETATO")
    {
        Esegui($@"UPDATE {Registro} SET Stato = @s,
                    DataCompletamento = CASE WHEN @s = 'COMPLETATO' THEN SYSUTCDATETIME() END,
                    NomeFile = CASE WHEN @s = 'COMPLETATO' THEN 'FatturePagoPa_test.json' END,
                    PathFile = CASE WHEN @s = 'COMPLETATO' THEN CONCAT('https://example.invalid/sap/', @b, '.json') END,
                    DescrizioneErrore = CASE WHEN @s = 'ERRORE' THEN 'Errore simulato dal test' END
                  WHERE BatchId = @b", ("@s", stato), ("@b", batchId));
        if (stato == "COMPLETATO")
            Esegui($@"UPDATE ft SET FatturaInviata = 1 FROM pfd.FattureTestata ft
                      JOIN {Dettaglio} d ON d.FkIdFattura = ft.IdFattura WHERE d.FkBatchId = @b", ("@b", batchId));
        else
            Esegui($@"UPDATE ft SET FatturaInviata = CASE d.FkStatoPrecedente WHEN 5 THEN 1 ELSE 0 END FROM pfd.FattureTestata ft
                      JOIN {Dettaglio} d ON d.FkIdFattura = ft.IdFattura WHERE d.FkBatchId = @b", ("@b", batchId));
        Esegui($"DELETE FROM {Lock} WHERE FkBatchId = @b", ("@b", batchId));
    }

    // ---- Gestione Fatture --------------------------------------------------------------------

    /// <summary>Utente delle righe di <c>cfg.GestioneFatture</c> create dai test: <see cref="Pulisci"/> le toglie.</summary>
    public const string UtenteGestioneFatture = "test-invio-sap";

    /// <summary>
    /// Mette una fattura del seed in Gestione Fatture: 0 posticipata, 1 ripristinata, 2 cancellata, 3 eliminata.
    /// Le viste 1.1 dell'Invio a SAP escludono gli stati 0 e 3. La riga e' del test e non del seed, perche' i test
    /// di lettura di Gestione Fatture non filtrano per anno.
    /// </summary>
    public static void MettiInGestioneFatture(long idFattura, int stato) =>
        Esegui(@"INSERT INTO cfg.GestioneFatture (FkIdFattura, FkIdEnte, FkTipologiaFattura, Anno, Mese, DataInserimento, IdUtenteInserimento, Stato, Azione, Note)
                 SELECT IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, GETDATE(), @u, @s,
                        CASE @s WHEN 0 THEN 'POSTICIPATA' WHEN 1 THEN 'RIPRISTINATA' WHEN 2 THEN 'CANCELLATA' ELSE 'ELIMINATA' END,
                        N'{""Data"":""2003-01-01T00:00:00"",""Testo"":""test invio sap""}'
                   FROM pfd.FattureTestata WHERE IdFattura = @id",
            ("@u", UtenteGestioneFatture), ("@s", stato), ("@id", idFattura));

    // ---- Prova "nessuna riga estranea toccata" -----------------------------------------------

    /// <summary>
    /// Impronta delle righe preesistenti delle tabelle che il Genera tocca. Se il BE aggiornasse o cancellasse
    /// qualcosa che non gli spetta, l'impronta cambierebbe (be-invio-sap.md §13.1 e §13.10).
    /// </summary>
    public sealed record Fotografia(long Registro, long Dettaglio, long Lock, long Fatture, long Messaggi, long Log, long MaxMessaggio);

    /// <param name="sogliaMessaggi">
    /// Per confrontare due fotografie va passata la <c>MaxMessaggio</c> della prima: altrimenti la seconda include
    /// nel checksum anche i messaggi creati nel frattempo, che non sono modifiche di righe preesistenti.
    /// </param>
    /// <param name="fattureDelBatch">
    /// Fatture escluse dall'impronta di <c>pfd.FattureTestata</c>: sono quelle del batch, le uniche su cui il BE
    /// scrive (<c>FatturaInviata = NULL</c>). Vanno passate a ENTRAMBE le fotografie da confrontare, e verificate
    /// a parte con <see cref="FatturaInviata"/>. Senza, l'impronta copre tutta la tabella ("nessuna scrittura").
    /// </param>
    public static Fotografia FotografiaTabelle(long? sogliaMessaggi = null, IReadOnlyCollection<long>? fattureDelBatch = null)
    {
        long C(string sql) => Convert.ToInt64(Scalar(sql) ?? 0L);
        var maxMessaggio = sogliaMessaggi ?? C("SELECT ISNULL(MAX(IdMessaggio), 0) FROM pfd.Messaggi");
        var fuoriDalBatch = fattureDelBatch is { Count: > 0 } ? $" WHERE IdFattura NOT IN ({string.Join(", ", fattureDelBatch)})" : string.Empty;
        return new Fotografia(
            C($"SELECT ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(BatchId, Stato, IdUtente, DataAvvio, DataCompletamento, NomeFile, PathFile, CountFatture, DescrizioneErrore)), 0) FROM {Registro} WHERE BatchId <= {UltimoBatchSeed}"),
            C($"SELECT ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(FkBatchId, FkIdFattura, FkStatoPrecedente, FkStato, NumeroDocStorno, CAST(Nota AS nvarchar(max)))), 0) FROM {Dettaglio} WHERE FkBatchId <= {UltimoBatchSeed}"),
            C($"SELECT ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(FkIdAzione, lockStart, lockTimeout, FkBatchId)), 0) FROM {Lock} WHERE FkBatchId IS NULL OR FkBatchId <= {UltimoBatchSeed}"),
            C("SELECT ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(IdFattura, FatturaInviata, TotaleFattura, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento)), 0) FROM pfd.FattureTestata" + fuoriDalBatch),
            C($"SELECT ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(IdMessaggio, IdUtente, Stato, Hash, LinkDocumento)), 0) FROM pfd.Messaggi WHERE IdMessaggio <= {maxMessaggio}"),
            C("SELECT COUNT(*) FROM pfw.Log WHERE IdUtente IS NULL OR IdUtente <> 'integration-test-user' OR DescrizioneEvento <> 'ISAP'"),
            maxMessaggio);
    }

    // ---- Righe create dai test ---------------------------------------------------------------

    public static List<int> BatchDeiTest() =>
        Lista<int>($"SELECT BatchId FROM {Registro} WHERE IdUtente = @u ORDER BY BatchId", ("@u", UtenteTest));

    public static int ContaRighe(string tabella) => Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM {tabella}"));

    public static int ContaLogDeiTest() =>
        Convert.ToInt32(Scalar("SELECT COUNT(*) FROM pfw.Log WHERE IdUtente = @u AND DescrizioneEvento = 'ISAP'", ("@u", UtenteTest)));

    /// <summary>
    /// Toglie tutto cio' che i test hanno creato e riporta il seed allo stato iniziale: righe dei batch dei
    /// test, log e messaggi, <c>FatturaInviata</c> del seed e riga di lock scaduta.
    /// </summary>
    public static void Pulisci()
    {
        try
        {
            Esegui($@"
                DECLARE @b TABLE (BatchId INT);
                INSERT INTO @b SELECT BatchId FROM {Registro} WHERE IdUtente = @u;
                DELETE FROM pfd.Messaggi WHERE Hash IN (SELECT CONVERT(NVARCHAR(64), BatchId) FROM @b) AND IdUtente = @u;
                DELETE FROM {Dettaglio} WHERE FkBatchId IN (SELECT BatchId FROM @b);
                DELETE FROM {Lock} WHERE FkBatchId IN (SELECT BatchId FROM @b);
                DELETE FROM {Registro} WHERE BatchId IN (SELECT BatchId FROM @b);
                DELETE FROM pfw.Log WHERE IdUtente = @u AND DescrizioneEvento = 'ISAP';",
                ("@u", UtenteTest));
            Esegui("DELETE FROM cfg.GestioneFatture WHERE IdUtenteInserimento = @u", ("@u", UtenteGestioneFatture));
            foreach (var (id, inviata) in StatoSeed)
                Esegui("UPDATE pfd.FattureTestata SET FatturaInviata = @v WHERE IdFattura = @id", ("@v", inviata), ("@id", id));
            Esegui($"IF NOT EXISTS (SELECT 1 FROM {TipoAzione} WHERE IdAzione = @a) INSERT INTO {TipoAzione} VALUES (@a, 'GeneraJsonFatture', 3600000)", ("@a", IdAzione));
            RipristinaLockSeed();
        }
        catch (SqlException) { /* oggetti assenti: i test sono comunque gialli */ }
    }
}
