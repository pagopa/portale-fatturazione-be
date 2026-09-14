using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Il caso di testbook `PF-672 TD-17`: *"l'admin, **per il solo aderente INPS**, deve poter eliminare
/// la fattura di tipologia PRIMO SALDO; questa è l'unica eccezione del processo di eliminazione"*.
///
/// La regola generale è che si eliminano solo `ANTICIPO` e `ACCONTO`. L'eccezione è **hardcoded in due
/// punti** della stored procedure — il filtro sulle fatture e la whitelist del ramo di
/// pre-eliminazione — entrambi con un commento `TODO: rimuovere eccezione per fattura INPS - PRIMO
/// SALDO`. È quindi dichiaratamente **temporanea**.
///
/// ATTENZIONE **Quando l'eccezione verrà rimossa, il primo test di questa classe diventerà rosso.** Non è un
/// difetto: è il segnale che la regola è cambiata, e va aggiornato invece che "riparato" — l'eccezione
/// sparisce e il caso di testbook con lei. È il motivo per cui la classe la isola invece di
/// spargerla fra i test dell'ELIMINA.
///
/// Un'eccezione si verifica solo insieme al suo **contrasto**: che la stessa azione sia rifiutata a un
/// altro ente, e che per INPS non si estenda alle altre tipologie di saldo. Senza quei due, il primo
/// test direbbe soltanto "l'elimina funziona".
///
/// I test usano un periodo **senza fattura** (ramo di pre-eliminazione): la whitelist si applica lo
/// stesso, e non si distrugge la fattura 3001 del seed, che serve ad altri.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class GestioneFattureEliminaEccezioneInpsHttpTests
{
    private const string Rotta = "/api/fatture/pagopa/gestione-fatture/azione";

    private const string Inps = "53b40136-65f2-424b-acfb-7fae17e35c60";
    private const string AltroEnte = "11111111-1111-1111-1111-111111111111";
    private const int Anno = 2027;
    private const int Mese = 9;

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
    public async Task Elimina_PrimoSaldo_PerINPS_ShouldEssereAmmessa()
    {
        var (stato, corpo) = await Elimina(Inps, "PRIMO SALDO");

        Assert.Multiple(() =>
        {
            Assert.That(stato, Is.EqualTo(HttpStatusCode.OK),
                "E' l'eccezione INPS. Se questo test diventa rosso, l'eccezione e' stata rimossa "
                + "(nella SP c'e' un TODO che lo prevede): aggiornare il test, non 'ripararlo'.");
            Assert.That(corpo, Is.EqualTo("1"));
            Assert.That(StatoRegistrato(Inps, "PRIMO SALDO"), Is.EqualTo(3), "3 = ELIMINATA.");
        });
    }

    [Test]
    public async Task Elimina_PrimoSaldo_PerUnAltroEnte_ShouldEssereRifiutata()
    {
        // Il contrasto che rende l'eccezione un'eccezione: stessa azione, stessa tipologia, ente diverso.
        var (stato, _) = await Elimina(AltroEnte, "PRIMO SALDO");

        Assert.Multiple(() =>
        {
            Assert.That(stato, Is.EqualTo(HttpStatusCode.NotFound),
                "Per chiunque altro il PRIMO SALDO non e' eliminabile (404 muto, difetto noto "
                + "sull'esposizione del rifiuto).");
            Assert.That(RigheDelPeriodo(AltroEnte), Is.Zero, "E un rifiuto non deve lasciare righe.");
        });
    }

    [Test]
    public async Task Elimina_SecondoSaldo_PerINPS_ShouldEssereRifiutata()
    {
        // L'eccezione riguarda il PRIMO SALDO, non "i saldi di INPS": e' scritta cosi' nella SP, e
        // vale la pena fissarlo perche' e' il tipo di dettaglio che si allarga per errore.
        var (stato, _) = await Elimina(Inps, "SECONDO SALDO");

        Assert.That(stato, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(RigheDelPeriodo(Inps), Is.Zero);
    }

    [Test]
    public async Task Elimina_Anticipo_PerINPS_ShouldEssereAmmessa()
    {
        // Contro-prova sull'altro lato: per INPS vale anche la regola generale, l'eccezione si aggiunge
        // e non sostituisce.
        var (stato, _) = await Elimina(Inps, "ANTICIPO");

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(StatoRegistrato(Inps, "ANTICIPO"), Is.EqualTo(3));
    }

    [Test]
    public async Task Elimina_PrimoSaldo_PerINPS_ShouldComparireInGriglia()
    {
        await Elimina(Inps, "PRIMO SALDO");

        var riga = await RigaInGriglia(Inps, "PRIMO SALDO");

        Assert.That(riga, Is.Not.Null);
        Assert.That(riga!.Value.GetProperty("azione").GetString(), Is.EqualTo("ELIMINATA"));
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    private async Task<(HttpStatusCode stato, string corpo)> Elimina(string idEnte, string tipologia)
    {
        var body = $$"""
        {
            "mese": "{{Mese}}",
            "anno": "{{Anno}}",
            "tipologiaFattura": "{{tipologia}}",
            "idEnte": "{{idEnte}}",
            "azione": "Elimina",
            "nota": { "data": "2026-08-31T21:00:00", "testo": "verifica eccezione di eliminazione" },
            "idFattura": null
        }
        """;
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.PostAsync(_factory.WithNonce(Rotta),
            new StringContent(body, Encoding.UTF8, "application/json"));
        var corpo = await resp.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine($"ELIMINA {tipologia} ente {idEnte[..8]} -> {(int)resp.StatusCode} | {corpo}");
        return (resp.StatusCode, corpo);
    }

    private async Task<JsonElement?> RigaInGriglia(string idEnte, string tipologia)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var body = $$"""
        { "idEnti": ["{{idEnte}}"], "anno": {{Anno}}, "mesi": [{{Mese}}], "tipologiaFattura": "{{tipologia}}" }
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

    private static int? StatoRegistrato(string idEnte, string tipologia)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT TOP(1) Stato FROM cfg.GestioneFatture
            WHERE FkIdEnte='{idEnte}' AND FkTipologiaFattura='{tipologia}' AND Anno={Anno} AND Mese={Mese}";
        var v = cmd.ExecuteScalar();
        return v == null || v == DBNull.Value ? null : Convert.ToInt32(v);
    }

    private static int RigheDelPeriodo(string idEnte)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM cfg.GestioneFatture WHERE FkIdEnte='{idEnte}' AND Anno={Anno} AND Mese={Mese}";
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
                WHERE FkIdEnte IN ('{Inps}', '{AltroEnte}') AND Anno={Anno} AND Mese={Mese};";
            cmd.ExecuteNonQuery();
        }
        catch (SqlException) { /* best-effort */ }
    }
}
