using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// I casi di testbook `PF-672 TD-08` e `TD-09`: **eliminare una fattura già emessa ma NON INVIATA**
/// (`FatturaInviata = 0`), dai **due punti di ingresso** — il pulsante sulla riga in Documenti Emessi
/// (con `idFattura`, TD-08) e il form "aggiungi" (per periodo, senza id, TD-09). Le eliminate si
/// ritrovano poi in Gestione Fatture con stato `ELIMINATA`.
///
/// I due casi stanno insieme perché la risposta interessante è che **convergono**: la stored procedure
/// risolve l'id dalla tabella variabile popolata per periodo, non da quello ricevuto, quindi il ramo
/// distruttivo scatta in entrambi i casi.
///
/// Sta a TD-07 come TD-06 sta a TD-01: stessa azione, punto di ingresso diverso. Ma qui la differenza
/// **non è solo il body**: con una fattura esistente e non inviata la stored procedure entra nel ramo
/// distruttivo e chiama `pfd.EliminaFattura`, che **sposta fisicamente** la riga in
/// `pfd.FattureTestata_Eliminate` e la toglie da `pfd.FattureTestata`. Dal form su un periodo senza
/// fattura (TD-07) non c'è nulla da spostare.
///
/// **Perché un file nuovo invece di estendere `GestioneFattureEliminaBodyRealeHttpTests`**, che quel
/// ramo già lo copre: quel file usa l'ente `00514501-…` del caso segnalato dal frontend, che **non
/// esiste in `pfd.Enti`**. Va benissimo per verificare la stored procedure, ma le sue righe non
/// possono comparire nella griglia — la vista ci arriva con tre INNER JOIN (Enti → Contratti →
/// TipoContratto). Asserire lì la comparsa in griglia darebbe un rosso per il motivo sbagliato.
/// Qui si usa un ente seedato, così la seconda metà del caso di testbook è verificabile davvero.
///
/// La fattura è seminata **dal test**, non dal file di seed: l'azione la distrugge, quindi ogni test
/// se la ricrea e la ripulisce (comprese le tabelle `*_Eliminate`).
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class GestioneFattureEliminaDallaRigaHttpTests
{
    private const string Rotta = "/api/fatture/pagopa/gestione-fatture/azione";

    // Ente3 (PAL) e' seedato con contratto, quindi visibile in griglia. Periodo 2026/1 libero.
    private const string IdEnte = "33333333-3333-3333-3333-333333333333";
    private const string Tipologia = "ANTICIPO";
    private const int Anno = 2026;
    private const int Mese = 1;
    private const long IdFattura = 7601;

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
        Pulisci();
        SeminaFatturaNonInviata();
    }

    [TearDown]
    public void TearDown() => Pulisci();

    // =============================================================================================

    [Test]
    public async Task Elimina_DallaRigaEmessa_ShouldReturn200_ERegistrareEliminata()
    {
        var (stato, corpo) = await Elimina();

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(corpo, Is.EqualTo("1"));

        var riga = LeggiRigaDelPeriodo();
        Assert.That(riga, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(riga!.Value.stato, Is.EqualTo(3), "3 = ELIMINATA.");
            Assert.That(riga.Value.azione, Is.EqualTo("ELIMINATA"));
            Assert.That(riga.Value.idFattura, Is.Null,
                "Anche col ramo distruttivo la colonna resta NULL: e' il contratto dell'ELIMINA, la "
                + "chiave logica e' il periodo. Da non confondere con la POSTICIPA, dove invece la SP "
                + "risolve e valorizza l'id.");
        });
    }

    [Test]
    public async Task Elimina_DallaRigaEmessa_ShouldSpostareLaFatturaInEliminate()
    {
        Assert.That(Conta("pfd.FattureTestata"), Is.EqualTo(1), "Contro-prova: la fattura c'e' prima.");

        await Elimina();

        Assert.Multiple(() =>
        {
            Assert.That(Conta("pfd.FattureTestata"), Is.Zero, "Sparisce dalla tabella di origine...");
            Assert.That(Conta("pfd.FattureTestata_Eliminate"), Is.EqualTo(1), "...e finisce in _Eliminate.");
        });
    }

    /// <summary>Il cuore di TD-08: dopo l'eliminazione la riga si legge in Gestione Fatture.</summary>
    [Test]
    public async Task Elimina_DallaRigaEmessa_ShouldComparireInGriglia_ComeEliminata()
    {
        Assert.That(await RigaInGriglia(), Is.Null, "Contro-prova: prima dell'azione il periodo non c'e'.");

        await Elimina();

        var riga = await RigaInGriglia();
        Assert.That(riga, Is.Not.Null,
            "La vista della griglia esclude solo Stato = 2, quindi l'eliminata resta visibile — e "
            + "l'ente e' seedato, quindi supera i tre INNER JOIN.");
        Assert.That(riga!.Value.GetProperty("azione").GetString(), Is.EqualTo("ELIMINATA"));
    }

    /// <summary>
    /// La conseguenza che l'operatore vede sull'**altra** pagina, e che nessun test copriva: la
    /// fattura eliminata esce da Documenti Emessi e si ritrova nella scheda "Non Fatturate", che legge
    /// `be.vwDocumentiEmessiNonFatturati` (Eliminate ∪ Posticipate).
    /// </summary>
    [Test]
    public async Task Elimina_DallaRigaEmessa_ShouldComparireFraLeNonFatturate()
    {
        Assert.That(await FraLeNonFatturate(), Is.False, "Contro-prova: prima non c'e'.");

        await Elimina();

        Assert.That(await FraLeNonFatturate(), Is.True,
            "Dopo l'eliminazione la fattura e' in pfd.FattureTestata_Eliminate, quindi la scheda "
            + "'Non Fatturate' deve mostrarla: e' li' che l'ente la ritrova.");
    }

    // =============================================================================================
    // `PF-672 TD-09`: la stessa eliminazione **dal form "aggiungi"**, quindi per periodo e senza id.
    //
    // La stored procedure risolve l'id dalla tabella variabile popolata per periodo, non da quello
    // ricevuto: il ramo distruttivo scatta lo stesso. È la stessa convergenza già vista sulla
    // posticipa (TD-01 vs TD-06) — con una differenza che vale la pena dire: lì le due vie convergono
    // sul *valore scritto*, qui sull'*effetto distruttivo*.
    // =============================================================================================

    [Test]
    public async Task Elimina_DalForm_SuFatturaEsistente_ShouldSpostareLaFatturaInEliminate()
    {
        var (stato, corpo) = await Elimina(conIdFattura: false);

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(corpo, Is.EqualTo("1"));
        Assert.Multiple(() =>
        {
            Assert.That(Conta("pfd.FattureTestata"), Is.Zero,
                "Anche senza id la fattura viene spostata: la SP la risolve dal periodo.");
            Assert.That(Conta("pfd.FattureTestata_Eliminate"), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Elimina_DalFormODallaRiga_ShouldProdurreLoStessoEsito()
    {
        await Elimina(conIdFattura: false);
        var dalForm = (staging: LeggiRigaDelPeriodo(), eliminate: Conta("pfd.FattureTestata_Eliminate"));

        // Si riparte da zero: la prima eliminazione ha distrutto la fattura.
        Pulisci();
        SeminaFatturaNonInviata();

        await Elimina(conIdFattura: true);
        var dallaRiga = (staging: LeggiRigaDelPeriodo(), eliminate: Conta("pfd.FattureTestata_Eliminate"));

        Assert.That(dallaRiga, Is.EqualTo(dalForm),
            "Le due vie devono convergere: stessa riga di staging e stesso effetto sulla fattura. "
            + "Se divergessero, la pagina da cui si e' agito cambierebbe il risultato.");
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    /// <param name="conIdFattura">
    /// `true` = body del pulsante sulla riga in Documenti Emessi, che conosce la fattura;
    /// `false` = body del form "aggiungi", che agisce per periodo.
    /// </param>
    private async Task<(HttpStatusCode stato, string corpo)> Elimina(bool conIdFattura = true)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var body = $$"""
        {
            "mese": "{{Mese}}",
            "anno": "{{Anno}}",
            "tipologiaFattura": "{{Tipologia}}",
            "idEnte": "{{IdEnte}}",
            "azione": "Elimina",
            "idFattura": {{(conIdFattura ? IdFattura.ToString() : "null")}},
            "nota": { "data": "2026-08-31T19:30:00", "testo": "elimina fattura emessa non inviata" }
        }
        """;
        var resp = await client.PostAsync(_factory.WithNonce(Rotta),
            new StringContent(body, Encoding.UTF8, "application/json"));
        var corpo = await resp.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine($"ELIMINA -> {(int)resp.StatusCode} {resp.StatusCode} | {corpo}");
        return (resp.StatusCode, corpo);
    }

    private async Task<JsonElement?> RigaInGriglia()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var body = $$"""
        {
            "idEnti": ["{{IdEnte}}"],
            "anno": {{Anno}},
            "mesi": [{{Mese}}],
            "tipologiaFattura": "{{Tipologia}}"
        }
        """;
        var rotta = "/api/fatture/pagopa/gestione-fatture?page=1&pageSize=50"
                  + $"&nonce={Uri.EscapeDataString(_factory.Nonce())}";
        var resp = await client.PostAsync(rotta, new StringContent(body, Encoding.UTF8, "application/json"));

        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var item in doc.RootElement.GetProperty("gestioneFatture").EnumerateArray())
            if (item.GetProperty("anno").GetInt32() == Anno && item.GetProperty("mese").GetInt32() == Mese)
                return item.Clone();

        return null;
    }

    /// <summary>La scheda "Non Fatturate" di Documenti Emessi: `api/fatture` con `cancellata = true`.</summary>
    private async Task<bool> FraLeNonFatturate()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var body = $$"""{ "cancellata": true, "anno": {{Anno}}, "mese": {{Mese}} }""";
        var rotta = $"/api/fatture?nonce={Uri.EscapeDataString(_factory.Nonce())}";
        var resp = await client.PostAsync(rotta, new StringContent(body, Encoding.UTF8, "application/json"));
        TestContext.Out.WriteLine($"non fatturate -> {(int)resp.StatusCode} {resp.StatusCode}");

        if (resp.StatusCode == HttpStatusCode.NotFound) return false; // lista vuota => 404
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return (await resp.Content.ReadAsStringAsync()).Contains(IdFattura.ToString());
    }

    private static (int stato, string? azione, long? idFattura)? LeggiRigaDelPeriodo()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            SELECT TOP(1) Stato, Azione, FkIdFattura FROM cfg.GestioneFatture
            WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}' AND Anno={Anno} AND Mese={Mese}";
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return (reader.GetInt32(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetInt64(2));
    }

    private static int Conta(string tabella)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {tabella} WHERE IdFattura = {IdFattura}";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// ATTENZIONE `CodiceContratto` non è un dettaglio riempitivo: `be.vwDocumentiEmessiNonFatturati` arriva
    /// alle fatture con `INNER JOIN pfd.Contratti ON c.onboardingtokenid = FT.CodiceContratto`, e in
    /// più con un `INNER JOIN pfw.FatturaTestataConfig` su (tipologia, tipo contratto). Una fattura
    /// senza codice contratto — o con una combinazione tipologia/contratto non configurata — **non
    /// compare** in "Non Fatturate", e sparisce senza errore. È la stessa trappola diagnostica di
    /// `api/fatture` documentata in `docs/viste-endpoint.md`, ed è esattamente ciò che ha fatto
    /// fallire la prima versione di questo test.
    ///
    /// Qui: ente3 ha `onboardingtokenid = 'TOKEN-E3'` ed è di tipo contratto 1, e la configurazione
    /// (1, ANTICIPO) esiste — quindi la coppia regge.
    /// </summary>
    private static void SeminaFatturaNonInviata() => Esegui($@"
        SET IDENTITY_INSERT pfd.FattureTestata ON;
        IF NOT EXISTS (SELECT 1 FROM pfd.FattureTestata WHERE IdFattura = {IdFattura})
        INSERT INTO pfd.FattureTestata
            (IdFattura, FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, DataFattura,
             IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento, AnnoRiferimento,
             MeseRiferimento, FatturaInviata, Progressivo, CodiceContratto)
        VALUES ({IdFattura}, 'prod-pn', 'TD01', '{Tipologia}', '{IdEnte}', '2026-01-31',
                'IT-{IdFattura}', 305.00, 'EUR', 'MP5', {Anno}, {Mese}, 0, {IdFattura}, 'TOKEN-E3');
        SET IDENTITY_INSERT pfd.FattureTestata OFF;
        IF NOT EXISTS (SELECT 1 FROM pfd.FattureRighe WHERE FkIdFattura = {IdFattura})
        INSERT INTO pfd.FattureRighe
            (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile,
             RigaBollo, PeriodoRiferimento)
        VALUES ({IdFattura}, 1, 'riga anticipo', 'MAT-A', 1, 305.00, 305.00, 0, '01/2026');");

    private static void Pulisci() => Esegui($@"
        DELETE FROM cfg.GestioneFatture
            WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}' AND Anno={Anno} AND Mese={Mese};
        DELETE FROM pfd.FattureRighe            WHERE FkIdFattura = {IdFattura};
        DELETE FROM pfd.FattureRighe_Eliminate  WHERE FkIdFattura = {IdFattura};
        DELETE FROM pfd.FattureTestata          WHERE IdFattura   = {IdFattura};
        DELETE FROM pfd.FattureTestata_Eliminate WHERE IdFattura  = {IdFattura};");

    private static void Esegui(string sql)
    {
        try
        {
            using var conn = new SqlConnection(LocalTestDb.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
        catch (SqlException) { /* best-effort: se il container e' giu' i test si auto-ignorano */ }
    }
}
