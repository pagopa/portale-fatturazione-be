using Microsoft.Data.SqlClient;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Test SP-LEVEL sulla stored procedure reale pfd.EliminaFattura (script autorevole in
/// tests/Data/sp/05_pfdEliminaFattura.sql), invocata direttamente via EXEC contro il DB seeded locale
/// (LocalTestDb, container tests/docker-compose.yml). Coprono il comportamento che lo stub RETURN 1 non
/// poteva esercitare: lo SPOSTAMENTO fisico della fattura (e delle temporanee collegate via MesiFatture)
/// nelle tabelle *_Eliminate e la cancellazione dalle tabelle sorgente.
///
/// Ogni test usa id/periodi DEDICATI (range 9xxxx, anno 2027) e ripulisce in [TearDown] tutte le tabelle
/// toccate: nessuna interferenza con gli altri seed di Gestione Fatture. Se il container e' giu' i test si
/// auto-ignorano (TestDb.SkipIfUnavailable).
///
/// NB: si invoca pfd.EliminaFattura DIRETTAMENTE (non tramite be.spGestioneFattureElimina) per isolare la
/// SP e poter costruire lo scenario 1:N (una emessa che consolida piu' temporanee) senza dipendere dai
/// gate di tipologia del wrapper.
/// </summary>
public class GestioneFattureEliminaFatturaSpIntegrationTests
{
    private const string Ente3 = "33333333-3333-3333-3333-333333333333";
    private const string EnteInps = "53b40136-65f2-424b-acfb-7fae17e35c60";

    private string Conn => LocalTestDb.ConnectionString;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        CleanupAll(); // difensivo: rimuove eventuali residui di un run precedente interrotto
    }

    [TearDown]
    public void TearDown() => CleanupAll();

    // ---------------------------------------------------------------------------------------------
    // 1) Fattura semplice (ANTICIPO): spostamento testata+righe in *_Eliminate e rimozione da sorgente.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public void EliminaFattura_ExistingAnticipo_MovesTestataAndRigheToEliminate_AndRemovesFromSource()
    {
        SeedEmessa(91001, Ente3, "ANTICIPO", 2027, 1);
        SeedRiga(91001, 1, "MAT-A");
        SeedRiga(91001, 2, "MAT-B");

        var result = ExecEliminaFattura(91001);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(1), "pfd.EliminaFattura deve restituire Result = 1.");
            Assert.That(Count("pfd.FattureTestata_Eliminate", "IdFattura", 91001), Is.EqualTo(1),
                "La testata deve essere spostata in pfd.FattureTestata_Eliminate.");
            Assert.That(Count("pfd.FattureTestata", "IdFattura", 91001), Is.EqualTo(0),
                "La testata deve essere rimossa da pfd.FattureTestata.");
            Assert.That(Count("pfd.FattureRighe_Eliminate", "FkIdFattura", 91001), Is.EqualTo(2),
                "Le 2 righe devono essere spostate in pfd.FattureRighe_Eliminate.");
            Assert.That(Count("pfd.FattureRighe", "FkIdFattura", 91001), Is.EqualTo(0),
                "Le righe devono essere rimosse da pfd.FattureRighe.");
        });
    }

    // ---------------------------------------------------------------------------------------------
    // 2) Fattura emessa con UNA temporanea collegata (via MesiFatture) sullo stesso periodo: la tmp va in
    //    *_Eliminate, e la riga MesiFatture viene cancellata. Valida le tabelle tmp*_Eliminate del seed.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public void EliminaFattura_WithLinkedTmpSamePeriod_MovesTmpToEliminate_AndDeletesMesiFatture()
    {
        SeedEmessa(91002, EnteInps, "PRIMO SALDO", 2027, 2);   // INPS PRIMO SALDO: eleggibile all'eliminazione
        SeedTmp(91502, EnteInps, "PRIMO SALDO", 2027, 2, flagFatturata: 1);
        SeedTmpRiga(91502, 1, "MAT-A");
        SeedMesiFatture(EnteInps, 2027, 2, "PRIMO SALDO", idFattura: 91002, idTmp: 91502);

        var result = ExecEliminaFattura(91002);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(1), "pfd.EliminaFattura deve restituire Result = 1.");
            Assert.That(Count("pfd.tmpFattureTestata_Eliminate", "IdFattura", 91502), Is.EqualTo(1),
                "La temporanea collegata deve essere spostata in pfd.tmpFattureTestata_Eliminate.");
            Assert.That(Count("pfd.tmpFattureTestata", "IdFattura", 91502), Is.EqualTo(0),
                "La temporanea deve essere rimossa da pfd.tmpFattureTestata.");
            Assert.That(Count("pfd.MesiFatture", "FkIdFattura", 91002), Is.EqualTo(0),
                "Le righe MesiFatture della fattura emessa devono essere cancellate.");
        });
    }

    // ---------------------------------------------------------------------------------------------
    // 3) CARATTERIZZAZIONE del BUG "scalar": una emessa che consolida PIU' temporanee (scenario 1:N del
    //    primo saldo) collegate via piu' righe MesiFatture. La SP cattura le tmp con
    //    'SELECT @tmpFkIdFattura = mf.FkIdFatturaTmp ...' in una variabile SCALARE: se le righe sono >1 ne
    //    resta solo l'ULTIMA, quindi viene spostata/cancellata UNA SOLA temporanea e l'altra resta ORFANA
    //    in pfd.tmpFattureTestata (MesiFatture invece le cancella tutte, con DELETE per FkIdFattura).
    //
    //    Questo test asserisce il comportamento ATTUALE (difettoso). Quando la SP verra' corretta (gestione
    //    a set delle temporanee) diventera' ROSSO: e' il segnale che il fix e' arrivato -> aggiornarlo alla
    //    forma corretta (entrambe le tmp spostate, orfane = 0). Difetto tracciato in
    //    coverage/segnalazione-sp-gestione-fatture.md.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public void EliminaFattura_WithMultipleLinkedTmp_OnlyOneMoved_ScalarBugCharacterization()
    {
        SeedEmessa(91003, EnteInps, "PRIMO SALDO", 2027, 3);
        // due temporanee su periodi diversi (vincolo UNIQUE ente/anno/mese/tipologia su tmpFattureTestata)
        SeedTmp(91503, EnteInps, "PRIMO SALDO", 2027, 3, flagFatturata: 1);
        SeedTmp(91504, EnteInps, "PRIMO SALDO", 2027, 4, flagFatturata: 1);
        SeedTmpRiga(91503, 1, "MAT-A");
        SeedTmpRiga(91504, 1, "MAT-B");
        SeedMesiFatture(EnteInps, 2027, 3, "PRIMO SALDO", idFattura: 91003, idTmp: 91503);
        SeedMesiFatture(EnteInps, 2027, 4, "PRIMO SALDO", idFattura: 91003, idTmp: 91504);

        var result = ExecEliminaFattura(91003);

        var tmpRimaste = Count("pfd.tmpFattureTestata", "IdFattura", 91503)
                       + Count("pfd.tmpFattureTestata", "IdFattura", 91504);
        var tmpSpostate = Count("pfd.tmpFattureTestata_Eliminate", "IdFattura", 91503)
                        + Count("pfd.tmpFattureTestata_Eliminate", "IdFattura", 91504);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(1), "pfd.EliminaFattura deve restituire Result = 1.");
            // BUG scalar: solo UNA delle due temporanee viene gestita (spostata+cancellata), l'altra resta orfana.
            Assert.That(tmpSpostate, Is.EqualTo(1),
                "Comportamento attuale (bug scalar): una sola temporanea viene spostata in _Eliminate.");
            Assert.That(tmpRimaste, Is.EqualTo(1),
                "Comportamento attuale (bug scalar): l'altra temporanea resta ORFANA in tmpFattureTestata. "
              + "Corretto sarebbe 0 (entrambe spostate): quando la SP sara' corretta questo assert diventera' rosso.");
            // MesiFatture viene invece cancellata per FkIdFattura (tutte le righe), non in modo scalare.
            Assert.That(Count("pfd.MesiFatture", "FkIdFattura", 91003), Is.EqualTo(0),
                "Tutte le righe MesiFatture della emessa vengono cancellate (DELETE per FkIdFattura).");
        });
    }

    // ---- invocazione SP ----

    private int ExecEliminaFattura(long idFattura)
    {
        using var conn = new SqlConnection(Conn);
        conn.Open();
        using var cmd = new SqlCommand("EXEC pfd.EliminaFattura @id", conn);
        cmd.Parameters.AddWithValue("@id", idFattura);
        var v = cmd.ExecuteScalar(); // la SP emette SELECT 0/1 as Result come primo result set
        return v is null or DBNull ? -1 : Convert.ToInt32(v);
    }

    // ---- seed helper (id espliciti -> IDENTITY_INSERT sulle tabelle con IDENTITY) ----

    private void SeedEmessa(long id, string ente, string tip, int anno, int mese) => Exec($@"
        SET IDENTITY_INSERT pfd.FattureTestata ON;
        INSERT INTO pfd.FattureTestata
            (IdFattura, FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, DataFattura,
             IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento, AnnoRiferimento, MeseRiferimento, FatturaInviata, Progressivo)
        VALUES (@id, 'prod-pn', 'TD01', @t, @e, '2027-01-01', CONCAT('IT-', @id), 100, 'EUR', 'MP5', @a, @m, 0, @id);
        SET IDENTITY_INSERT pfd.FattureTestata OFF;",
        ("@id", id), ("@t", tip), ("@e", ente), ("@a", anno), ("@m", mese));

    private void SeedRiga(long idFattura, int linea, string materiale) => Exec(@"
        INSERT INTO pfd.FattureRighe
            (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo, PeriodoRiferimento)
        VALUES (@id, @l, 'riga', @mat, 1, 50, 50, 0, '01/2027')",
        ("@id", idFattura), ("@l", linea), ("@mat", materiale));

    private void SeedTmp(long id, string ente, string tip, int anno, int mese, int flagFatturata) => Exec($@"
        SET IDENTITY_INSERT pfd.tmpFattureTestata ON;
        INSERT INTO pfd.tmpFattureTestata
            (IdFattura, FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, DataFattura,
             IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento, AnnoRiferimento, MeseRiferimento, FatturaInviata, Progressivo, FlagFatturata)
        VALUES (@id, 'prod-pn', 'TD01', @t, @e, '2027-01-01', CONCAT('TMP-', @id), 100, 'EUR', 'MP5', @a, @m, 0, @id, @flag);
        SET IDENTITY_INSERT pfd.tmpFattureTestata OFF;",
        ("@id", id), ("@t", tip), ("@e", ente), ("@a", anno), ("@m", mese), ("@flag", flagFatturata));

    private void SeedTmpRiga(long idFattura, int linea, string materiale) => Exec(@"
        INSERT INTO pfd.tmpFattureRighe
            (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo, PeriodoRiferimento)
        VALUES (@id, @l, 'riga tmp', @mat, 1, 50, 50, 0, '01/2027')",
        ("@id", idFattura), ("@l", linea), ("@mat", materiale));

    private void SeedMesiFatture(string ente, int anno, int mese, string tip, long idFattura, long idTmp) => Exec(@"
        INSERT INTO pfd.MesiFatture (FkIdEnte, AnnoRiferimento, MeseRiferimento, FKTipologiaFattura, FkIdFattura, FkIdFatturaTmp, FlagEliminata)
        VALUES (@e, @a, @m, @t, @idf, @idt, 0)",
        ("@e", ente), ("@a", anno), ("@m", mese), ("@t", tip), ("@idf", idFattura), ("@idt", idTmp));

    // ---- cleanup (per gli id/periodi dedicati di questa fixture) ----

    private void CleanupAll()
    {
        long[] emesse = { 91001, 91002, 91003 };
        long[] tmp = { 91502, 91503, 91504 };
        foreach (var id in emesse)
        {
            Exec("DELETE FROM pfd.FattureRighe_Eliminate WHERE FkIdFattura=@id", ("@id", id));
            Exec("DELETE FROM pfd.FattureTestata_Eliminate WHERE IdFattura=@id", ("@id", id));
            Exec("DELETE FROM pfd.FattureRighe WHERE FkIdFattura=@id", ("@id", id));
            Exec("DELETE FROM pfd.FattureTestata WHERE IdFattura=@id", ("@id", id));
            Exec("DELETE FROM pfd.MesiFatture WHERE FkIdFattura=@id", ("@id", id));
            Exec("DELETE FROM pfd.CreditoSospesoStorico WHERE FkIdFattura=@id", ("@id", id));
        }
        foreach (var id in tmp)
        {
            Exec("DELETE FROM pfd.tmpFattureRighe_Eliminate WHERE FkIdFattura=@id", ("@id", id));
            Exec("DELETE FROM pfd.tmpFattureTestata_Eliminate WHERE IdFattura=@id", ("@id", id));
            Exec("DELETE FROM pfd.tmpFattureRighe WHERE FkIdFattura=@id", ("@id", id));
            Exec("DELETE FROM pfd.tmpFattureTestata WHERE IdFattura=@id", ("@id", id));
        }
    }

    // ---- SQL grezzo ----

    private int Count(string table, string col, long value)
    {
        using var conn = new SqlConnection(Conn);
        conn.Open();
        // table/col sono costanti del test (non input esterno): interpolazione sicura qui.
        using var cmd = new SqlCommand($"SELECT COUNT(*) FROM {table} WHERE {col}=@v", conn);
        cmd.Parameters.AddWithValue("@v", value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private void Exec(string sql, params (string, object)[] ps)
    {
        try
        {
            using var conn = new SqlConnection(Conn);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            foreach (var p in ps) cmd.Parameters.AddWithValue(p.Item1, p.Item2);
            cmd.ExecuteNonQuery();
        }
        catch (SqlException) { /* best-effort: cleanup/seed non deve mascherare il vero esito del test */ }
    }
}
