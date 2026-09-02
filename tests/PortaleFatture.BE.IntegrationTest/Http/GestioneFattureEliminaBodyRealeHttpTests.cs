using System.Net;
using System.Text;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Replay end-to-end del body REALE segnalato per POST /api/fatture/pagopa/gestione-fatture/azione
/// (ELIMINA su ANTICIPO, ente 00514501-..., 2026/2, idFattura 62870), inviato VERBATIM — compreso
/// "azione": "Elimina" in case misto e la nota senza campo "azione" (che l'endpoint valorizza server-side).
///
/// Il body e' fisso: a cambiare e' solo lo STATO DEL DB, cioe' i tre casi in cui la SP
/// be.spGestioneFattureElimina puo' trovarsi:
///   1) la fattura esiste ed e' NON inviata      -> ramo "fattura trovata": eliminazione fisica + record ELIMINATA;
///   2) la fattura non esiste ancora (pre-eliminazione, RF06) -> ramo ELSE: solo record ELIMINATA;
///   3) la fattura esiste ma e' GIA' INVIATA     -> caratterizzazione: cade comunque nel ramo ELSE.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build). Se e' giu' i test
/// si auto-ignorano (TestDb.SkipIfUnavailable).
/// </summary>
public class GestioneFattureEliminaBodyRealeHttpTests
{
    private const string Rotta = "/api/fatture/pagopa/gestione-fatture/azione";

    // Chiave del caso segnalato (ente/periodo/tipologia reali del body).
    private const string IdEnte = "00514501-b0f1-4f24-b2b4-fe95dff08f6a";
    private const string Tipologia = "ANTICIPO";
    private const int Anno = 2026;
    private const int Mese = 2;
    private const long IdFattura = 62870;

    /// <summary>Il body cosi' come arriva dal client, senza normalizzazioni.</summary>
    private const string BodyReale = """
    {
        "mese": 2,
        "anno": 2026,
        "tipologiaFattura": "ANTICIPO",
        "azione": "Elimina",
        "idFattura": 62870,
        "idEnte": "00514501-b0f1-4f24-b2b4-fe95dff08f6a",
        "nota": {
            "data": "2026-07-31T12:33:41",
            "testo": "follo test nico 1"
        }
    }
    """;

    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void OneTimeSetup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Pulisci();
        _factory?.Dispose();
    }

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        Pulisci(); // difensivo: residui di un run precedente interrotto occuperebbero la chiave di periodo
    }

    [TearDown]
    public void TearDown() => Pulisci();

    private async Task<(HttpStatusCode Status, string Body)> PostBodyReale()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var content = new StringContent(BodyReale, Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(_factory.WithNonce(Rotta), content);
        var body = await resp.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.Out.WriteLine($"BODY  : {body}");
        return (resp.StatusCode, body);
    }

    // ---------------------------------------------------------------------------------------------
    // 1) La fattura esiste ed e' NON inviata: e' il caso nominale dell'ELIMINA.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Elimina_BodyReale_ConFatturaEsistenteNonInviata_ShouldReturn200_AndRegistraEliminata()
    {
        SeedFattura(fatturaInviata: 0);
        SeedRiga(1, "MAT-A");

        var (status, body) = await PostBodyReale();

        Assert.Multiple(() =>
        {
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK),
                "Azione in case misto ('Elimina') e ANTICIPO sono ammessi: l'endpoint normalizza con "
              + "Trim().ToUpperInvariant() e la SP whitelista ANTICIPO/ACCONTO.");
            Assert.That(body, Is.EqualTo("1"), "La SP deve rispondere Result 1.");

            Assert.That(StatoRegistrato(), Is.EqualTo(3), "cfg.GestioneFatture deve avere Stato 3 (ELIMINATA).");
            Assert.That(AzioneRegistrata(), Is.EqualTo("ELIMINATA"),
                "La colonna Azione usa il vocabolario al PASSATO, non l'imperativo del body.");
            Assert.That(FkIdFatturaRegistrata(), Is.Null,
                "Contratto noto della Elimina: FkIdFattura resta NULL, la chiave logica e' il periodo.");
            Assert.That(NoteRegistrate(), Does.Contain("follo test nico 1"), "La nota del body deve essere persistita.");
            Assert.That(NoteRegistrate(), Does.Contain("ELIMINA"),
                "L'endpoint valorizza server-side Nota.Azione con l'azione normalizzata.");

            Assert.That(Count("pfd.FattureTestata", "IdFattura", IdFattura), Is.EqualTo(0),
                "La fattura deve sparire da pfd.FattureTestata...");
            Assert.That(Count("pfd.FattureTestata_Eliminate", "IdFattura", IdFattura), Is.EqualTo(1),
                "...ed essere spostata in pfd.FattureTestata_Eliminate da pfd.EliminaFattura.");
        });
    }

    // ---------------------------------------------------------------------------------------------
    // 2) Pre-eliminazione (RF06): nessuna fattura a DB per quel periodo -> ramo ELSE della SP.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Elimina_BodyReale_SenzaFatturaADb_ShouldReturn200_AndRegistraSoloLoStato()
    {
        // nessun seed: la fattura 62870 non esiste nel DB seeded

        var (status, body) = await PostBodyReale();

        Assert.Multiple(() =>
        {
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK),
                "L'ELIMINA su un periodo senza fattura ancora generata e' ammessa (ramo ELSE della SP).");
            Assert.That(body, Is.EqualTo("1"));
            Assert.That(StatoRegistrato(), Is.EqualTo(3), "Il periodo resta marcato ELIMINATA anche senza fattura.");
            Assert.That(Count("pfd.FattureTestata_Eliminate", "IdFattura", IdFattura), Is.EqualTo(0),
                "Non essendoci fattura, non c'e' nulla da spostare in _Eliminate.");
        });
    }

    // ---------------------------------------------------------------------------------------------
    // 3) CARATTERIZZAZIONE: fattura GIA' INVIATA a SAP (FatturaInviata = 1).
    //    La SELECT che popola @tmpGestioneFatture filtra 'FatturaInviata IS NULL OR = 0', quindi
    //    @countFatture = 0 e si finisce nel ramo ELSE: la tipologia ANTICIPO passa la whitelist e il
    //    periodo viene comunque marcato ELIMINATA, senza alcuna eliminazione fisica. Il client riceve
    //    200/1 e non distingue questo caso dall'eliminazione vera.
    //    Se un domani la SP rifiutera' esplicitamente le fatture gia' inviate, questo test diventera'
    //    rosso: e' il segnale del cambio di contratto, da aggiornare alla forma nuova.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Elimina_BodyReale_ConFatturaGiaInviata_CadeNelRamoElse_Caratterizzazione()
    {
        SeedFattura(fatturaInviata: 1);

        var (status, body) = await PostBodyReale();

        Assert.Multiple(() =>
        {
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), "Comportamento attuale: nessun rifiuto.");
            Assert.That(body, Is.EqualTo("1"), "Comportamento attuale: Result 1 come per un'eliminazione riuscita.");
            Assert.That(StatoRegistrato(), Is.EqualTo(3), "Il periodo viene marcato ELIMINATA...");
            Assert.That(Count("pfd.FattureTestata", "IdFattura", IdFattura), Is.EqualTo(1),
                "...ma la fattura gia' inviata resta in pfd.FattureTestata: nessuna eliminazione fisica.");
        });
    }

    // ---- seed / lettura / cleanup -----------------------------------------------------------------

    private static void SeedFattura(int fatturaInviata) => Exec($@"
        SET IDENTITY_INSERT pfd.FattureTestata ON;
        INSERT INTO pfd.FattureTestata
            (IdFattura, FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, DataFattura,
             IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento, AnnoRiferimento, MeseRiferimento,
             FatturaInviata, Progressivo)
        VALUES (@id, 'prod-pn', 'TD01', @t, @e, '2026-02-01', CONCAT('IT-', @id), 100, 'EUR', 'MP5', @a, @m, @inv, @id);
        SET IDENTITY_INSERT pfd.FattureTestata OFF;",
        ("@id", IdFattura), ("@t", Tipologia), ("@e", IdEnte), ("@a", Anno), ("@m", Mese), ("@inv", fatturaInviata));

    private static void SeedRiga(int linea, string materiale) => Exec(@"
        INSERT INTO pfd.FattureRighe
            (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo, PeriodoRiferimento)
        VALUES (@id, @l, 'riga', @mat, 1, 100, 100, 0, '02/2026')",
        ("@id", IdFattura), ("@l", linea), ("@mat", materiale));

    private static object? ScalarDelPeriodo(string colonna)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        // 'colonna' e' una costante del test, non input esterno.
        using var cmd = new SqlCommand(
            $@"SELECT TOP 1 {colonna} FROM cfg.GestioneFatture
               WHERE FkIdEnte=@e AND FkTipologiaFattura=@t AND Anno=@a AND Mese=@m", conn);
        cmd.Parameters.AddWithValue("@e", IdEnte);
        cmd.Parameters.AddWithValue("@t", Tipologia);
        cmd.Parameters.AddWithValue("@a", Anno);
        cmd.Parameters.AddWithValue("@m", Mese);
        var v = cmd.ExecuteScalar();
        return v is DBNull ? null : v;
    }

    private static int? StatoRegistrato() => ScalarDelPeriodo("Stato") is { } v ? Convert.ToInt32(v) : null;
    private static string? AzioneRegistrata() => ScalarDelPeriodo("Azione") as string;
    private static long? FkIdFatturaRegistrata() => ScalarDelPeriodo("FkIdFattura") is { } v ? Convert.ToInt64(v) : null;
    private static string NoteRegistrate() => ScalarDelPeriodo("CAST(Note AS nvarchar(max))") as string ?? string.Empty;

    private static int Count(string tabella, string colonna, long valore)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand($"SELECT COUNT(*) FROM {tabella} WHERE {colonna}=@v", conn);
        cmd.Parameters.AddWithValue("@v", valore);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void Pulisci()
    {
        Exec(@"DELETE FROM cfg.GestioneFatture
               WHERE FkIdEnte=@e AND FkTipologiaFattura=@t AND Anno=@a AND Mese=@m",
            ("@e", IdEnte), ("@t", Tipologia), ("@a", Anno), ("@m", Mese));
        Exec("DELETE FROM pfd.FattureRighe_Eliminate WHERE FkIdFattura=@id", ("@id", IdFattura));
        Exec("DELETE FROM pfd.FattureTestata_Eliminate WHERE IdFattura=@id", ("@id", IdFattura));
        Exec("DELETE FROM pfd.FattureRighe WHERE FkIdFattura=@id", ("@id", IdFattura));
        Exec("DELETE FROM pfd.FattureTestata WHERE IdFattura=@id", ("@id", IdFattura));
        Exec("DELETE FROM pfd.MesiFatture WHERE FkIdFattura=@id", ("@id", IdFattura));
        Exec("DELETE FROM pfd.CreditoSospesoStorico WHERE FkIdFattura=@id", ("@id", IdFattura));
    }

    private static void Exec(string sql, params (string Nome, object Valore)[] parametri)
    {
        try
        {
            using var conn = new SqlConnection(LocalTestDb.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            foreach (var p in parametri) cmd.Parameters.AddWithValue(p.Nome, p.Valore);
            cmd.ExecuteNonQuery();
        }
        catch (SqlException) { /* best-effort: seed/cleanup non devono mascherare l'esito del test */ }
    }
}
