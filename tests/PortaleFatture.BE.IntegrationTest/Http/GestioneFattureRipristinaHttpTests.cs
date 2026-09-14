using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Il caso di testbook `PF-672 TD-05`: **ripristinare una posticipa** col pulsante "Ripristina" sulla
/// riga; *"le fatture ripristinate rimangono visibili nella griglia risultati e lo stato visualizzato
/// è RIPRISTINATA"*.
///
/// È lo **speculare di TD-04**, e i due vanno letti insieme perché la differenza sta tutta in un
/// predicato della vista della griglia, `WHERE gf.Stato &lt;&gt; 2`:
///
/// | Azione | Stato | In griglia |
/// |---|---|---|
/// | Annulla (`CANCELLA`) | 2 CANCELLATA | **sparisce** |
/// | Ripristina (`RIPRISTINA`) | 1 RIPRISTINATA | **resta**, con lo stato aggiornato |
///
/// In entrambi i casi la riga **non viene rimossa** da `cfg.GestioneFatture`: cambia solo lo stato. La
/// visibilità è una decisione della vista, non una cancellazione.
///
/// Anche qui il pulsante e l'azione hanno lo stesso nome (a differenza di "Annulla" → `CANCELLA`), ma
/// resta il disallineamento imperativo/passato: si manda `RIPRISTINA`, in griglia si legge
/// `RIPRISTINATA`.
///
/// **Due comportamenti noti che questo file NON ripete**, per non duplicare copertura già esistente:
/// ripristinare una **già ripristinata** è correttamente rifiutato dalla SP ma arriva al client come
/// 404 muto (`Ripristina_SuGiaRipristinata_ShouldReturn400`, `[Ignore]` in `GestioneFattureHttpTests`);
/// e dopo un ripristino la fattura **rientra nel counter "Invia Fatture" ma non nella lista di
/// dettaglio per periodo**, perché le due rotte usano esclusioni diverse
/// (`DiscrepanzaDaInviare_RipristinataInEmesseMaNonInDaInviare`, in `FattureRicercaApiIntegrationTests`).
/// Chi collauda questa pagina dovrebbe conoscere entrambi.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class GestioneFattureRipristinaHttpTests
{
    private const string Rotta = "/api/fatture/pagopa/gestione-fatture/azione";

    // Periodo riservato a questa classe (2026/10): TD-03 usa 2026/12 e TD-04 2026/11. La PK di
    // cfg.GestioneFatture e' per periodo, quindi due classi sullo stesso periodo si ostacolerebbero
    // al primo run interrotto.
    private const string IdEnte = "11111111-1111-1111-1111-111111111111";
    private const string Tipologia = "PRIMO SALDO";
    private const int Anno = 2026;
    private const int Mese = 10;

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
    }

    [TearDown]
    public void TearDown() => Pulisci();

    // =============================================================================================

    [Test]
    public async Task Ripristina_SuUnaPosticipata_ShouldReturn200()
    {
        await Azione("Posticipa");

        var (stato, corpo) = await Azione("Ripristina");

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(corpo, Is.EqualTo("1"));
    }

    [Test]
    public async Task Ripristina_ShouldPortareLoStatoARipristinata_TracciandoUtenteEData()
    {
        await Azione("Posticipa");
        await Azione("Ripristina");

        var riga = LeggiRigaDelPeriodo();

        Assert.That(riga, Is.Not.Null, "La riga non viene rimossa: cambia stato.");
        Assert.Multiple(() =>
        {
            Assert.That(riga!.Value.stato, Is.EqualTo(1), "1 = RIPRISTINATA.");
            Assert.That(riga.Value.azione, Is.EqualTo("RIPRISTINATA"));
            Assert.That(riga.Value.utenteRipristino, Is.EqualTo("integration-test-user"),
                "Il ripristino e' una TRANSIZIONE: si traccia in IdUtenteRipristino/DataRipristino, "
                + "colonne diverse da quelle dell'inserimento.");
            Assert.That(riga.Value.dataRipristino, Is.Not.Null);
            Assert.That(riga.Value.utenteInserimento, Is.EqualTo("integration-test-user"),
                "Chi aveva posticipato resta registrato.");
        });
    }

    /// <summary>Il cuore di TD-05: a differenza dell'annulla, la riga resta in griglia.</summary>
    [Test]
    public async Task Ripristina_ShouldRestareVisibileInGriglia_ConStatoRipristinata()
    {
        await Azione("Posticipa");
        var primaDelRipristino = await RigaInGriglia();
        Assert.That(primaDelRipristino, Is.Not.Null);
        Assert.That(primaDelRipristino!.Value.GetProperty("azione").GetString(), Is.EqualTo("POSTICIPATA"));

        await Azione("Ripristina");

        var dopo = await RigaInGriglia();
        Assert.That(dopo, Is.Not.Null,
            "La vista esclude solo Stato = 2: una RIPRISTINATA deve restare visibile. Se sparisse, "
            + "l'operatore non avrebbe modo di sapere che il ripristino e' andato a buon fine.");
        Assert.That(dopo!.Value.GetProperty("azione").GetString(), Is.EqualTo("RIPRISTINATA"),
            "Ed e' lo stato aggiornato a doversi leggere, non piu' POSTICIPATA.");
    }

    [Test]
    public async Task Ripristina_ShouldLasciareUnaSolaRigaPerPeriodo()
    {
        // Il ripristino aggiorna il record esistente. Se ne comparisse una seconda, la PK per periodo
        // sarebbe occupata due volte e l'azione successiva fallirebbe in modo poco spiegabile.
        await Azione("Posticipa");
        await Azione("Ripristina");

        Assert.That(RigheDelPeriodo(), Is.EqualTo(1));
    }

    /// <summary>
    /// CARATTERIZZAZIONE dello stesso difetto già visto sull'annulla: ripristinare qualcosa che non è
    /// stato posticipato è correttamente rifiutato dalla stored procedure (`Result 0`), ma l'endpoint
    /// traduce quello zero in `NotFound()` — 404 senza corpo, che non dice il motivo.
    /// </summary>
    [Test]
    public async Task Ripristina_SenzaPosticipaPrecedente_ShouldReturn404Muto_Caratterizzazione()
    {
        var (stato, corpo) = await Azione("Ripristina");

        Assert.Multiple(() =>
        {
            Assert.That(stato, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(corpo, Is.Empty);
            Assert.That(RigheDelPeriodo(), Is.Zero, "Un rifiuto non deve lasciare righe.");
        });
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    private async Task<(HttpStatusCode stato, string corpo)> Azione(string azione)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var body = $$"""
        {
            "mese": "{{Mese}}",
            "anno": "{{Anno}}",
            "tipologiaFattura": "{{Tipologia}}",
            "idEnte": "{{IdEnte}}",
            "azione": "{{azione}}",
            "nota": { "data": "2026-08-31T18:30:00", "testo": "azione {{azione}} da test" },
            "idFattura": null
        }
        """;
        var resp = await client.PostAsync(_factory.WithNonce(Rotta),
            new StringContent(body, Encoding.UTF8, "application/json"));
        var corpo = await resp.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine($"{azione,-11} -> {(int)resp.StatusCode} {resp.StatusCode} | {corpo}");
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

    private static (int stato, string? azione, string? utenteInserimento,
                    string? utenteRipristino, DateTime? dataRipristino)? LeggiRigaDelPeriodo()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            SELECT TOP(1) Stato, Azione, IdUtenteInserimento, IdUtenteRipristino, DataRipristino
            FROM cfg.GestioneFatture
            WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}' AND Anno={Anno} AND Mese={Mese}";
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return (reader.GetInt32(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetDateTime(4));
    }

    private static int RigheDelPeriodo()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT COUNT(*) FROM cfg.GestioneFatture
            WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}' AND Anno={Anno} AND Mese={Mese}";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void Pulisci()
    {
        try
        {
            using var conn = new SqlConnection(LocalTestDb.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"DELETE FROM cfg.GestioneFatture
                WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}' AND Anno={Anno} AND Mese={Mese};";
            cmd.ExecuteNonQuery();
        }
        catch (SqlException) { /* best-effort */ }
    }
}
