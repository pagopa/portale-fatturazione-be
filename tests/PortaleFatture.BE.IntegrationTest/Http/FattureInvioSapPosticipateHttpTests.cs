using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Il caso di testbook `PF-672 TD-19`: una fattura **posticipata** non dev'essere *"né conteggiata né
/// visualizzata"* nella pagina **Invio Fatture**, nella sua tipologia.
///
/// È il senso stesso della posticipa — escludere una fattura dal giro verso SAP — e si verifica su
/// **due rotte diverse**, che è il punto interessante:
///
/// | Cosa mostra la pagina | Rotta | Come esclude |
/// |---|---|---|
/// | il **conteggio** per tipologia | `GET api/fatture/invio/sap/multiplo` | `(gf.Stato &lt;&gt; 0 OR gf.Stato IS NULL)` — solo le posticipate |
/// | il **dettaglio** del periodo | `POST api/fatture/invio/sap/multiplo/periodo` | `gf.FkIdEnte IS NULL` — qualunque riga di staging |
///
/// I due predicati sono diversi, ed è il **finding §11** già tracciato: dopo un RIPRISTINO la fattura
/// rientra nel conteggio ma non nel dettaglio, e i due numeri si disallineano
/// (`DiscrepanzaDaInviare_RipristinataInEmesseMaNonInDaInviare`). Per una **posticipata**, invece, le
/// due esclusioni coincidono — ed è esattamente ciò che TD-19 richiede: qui si verifica che coincidano
/// davvero, su entrambe le rotte, invece di dedurlo dai due predicati.
///
/// La posticipa non tocca la fattura, quindi il test è non distruttivo: si posticipa la fattura 1001
/// del seed (`SECONDO SALDO` 2026/7, non inviata) e si ripulisce la sola riga di staging.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class FattureInvioSapPosticipateHttpTests
{
    private const string RottaAzione = "/api/fatture/pagopa/gestione-fatture/azione";
    private const string RottaConteggio = "/api/fatture/invio/sap/multiplo";
    private const string RottaDettaglio = "/api/fatture/invio/sap/multiplo/periodo";

    // Fattura 1001 del seed: ente1, SECONDO SALDO, 2026/7, FatturaInviata = 0 => e' "da inviare".
    private const string IdEnte = "11111111-1111-1111-1111-111111111111";
    private const string Tipologia = "SECONDO SALDO";
    private const int Anno = 2026;
    private const int Mese = 7;

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
        SaltaSeLaFatturaNonEDaInviare();
    }

    [TearDown]
    public void TearDown() => Pulisci();

    // =============================================================================================

    [Test]
    public async Task Posticipata_ShouldNonEssereConteggiata()
    {
        var prima = await ConteggioDellaTipologia();
        Assert.That(prima, Is.Not.Null, "Contro-prova: prima della posticipa la tipologia e' conteggiata.");

        await Posticipa();

        var dopo = await ConteggioDellaTipologia();
        Assert.That(dopo, Is.Null.Or.LessThan(prima),
            "Dopo la posticipa la fattura non deve piu' pesare sul conteggio: o la tipologia sparisce "
            + "dall'elenco, o il numero di fatture cala.");
    }

    [Test]
    public async Task Posticipata_ShouldNonEssereVisualizzataNelDettaglio()
    {
        Assert.That(await NelDettaglio(), Is.True, "Contro-prova: prima della posticipa la fattura c'e'.");

        await Posticipa();

        Assert.That(await NelDettaglio(), Is.False,
            "La pagina Invio Fatture non deve mostrarla: e' esclusa dalla vista che alimenta il "
            + "dettaglio del periodo.");
    }

    [Test]
    public async Task Posticipata_ConteggioEDettaglio_ShouldEssereCoerentiFraLoro()
    {
        // Le due rotte usano predicati diversi (v. finding §11): per la posticipata devono comunque
        // dare la stessa risposta. Se un domani divergessero anche qui, la pagina mostrerebbe un
        // contatore che non corrisponde all'elenco sottostante.
        await Posticipa();

        // I due valori si raccolgono PRIMA di asserire: `Assert.Multiple` con una lambda async e' una
        // trappola nota di NUnit — il delegate e' `void`, quindi le asserzioni interne possono girare
        // fuori dal blocco e il test passerebbe senza averle valutate.
        var nelDettaglio = await NelDettaglio();
        var conteggio = await ConteggioDellaTipologia();

        Assert.Multiple(() =>
        {
            Assert.That(nelDettaglio, Is.False);
            Assert.That(conteggio, Is.Null.Or.Zero,
                "Conteggio e dettaglio devono raccontare la stessa cosa sulla fattura posticipata.");
        });
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    private async Task Posticipa()
    {
        var body = $$"""
        {
            "mese": "{{Mese}}",
            "anno": "{{Anno}}",
            "tipologiaFattura": "{{Tipologia}}",
            "idEnte": "{{IdEnte}}",
            "azione": "Posticipa",
            "nota": { "data": "2026-09-01T09:00:00", "testo": "esclusione dall'invio a SAP" },
            "idFattura": null
        }
        """;
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.PostAsync(_factory.WithNonce(RottaAzione),
            new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK), "La posticipa deve riuscire.");
    }

    /// <summary>Numero di fatture che il conteggio attribuisce alla tipologia/periodo, o null se assente.</summary>
    private async Task<long?> ConteggioDellaTipologia()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.GetAsync(_factory.WithNonce(RottaConteggio));
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var voce in doc.RootElement.EnumerateArray())
        {
            if (voce.GetProperty("tipologiaFattura").GetString() != Tipologia) continue;
            if (voce.GetProperty("annoRiferimento").GetInt32() != Anno) continue;
            if (voce.GetProperty("meseRiferimento").GetInt32() != Mese) continue;
            return voce.GetProperty("numeroFatture").GetInt64();
        }
        return null;
    }

    /// <summary>La fattura compare nel dettaglio "da inviare" del periodo?</summary>
    private async Task<bool> NelDettaglio()
    {
        var body = $$"""
        { "annoRiferimento": {{Anno}}, "meseRiferimento": {{Mese}}, "tipologiaFattura": "{{Tipologia}}" }
        """;
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.PostAsync(_factory.WithNonce(RottaDettaglio),
            new StringContent(body, Encoding.UTF8, "application/json"));
        TestContext.Out.WriteLine($"dettaglio -> {(int)resp.StatusCode}");

        if (resp.StatusCode == HttpStatusCode.NotFound) return false;
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        return (await resp.Content.ReadAsStringAsync()).Contains(IdEnte, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Guardia: se la fattura del seed non è più "da inviare" (`FatturaInviata` diverso da 0) le
    /// contro-prove non direbbero nulla, perché sarebbe già assente da entrambe le rotte.
    /// </summary>
    private static void SaltaSeLaFatturaNonEDaInviare()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT COUNT(*) FROM pfd.FattureTestata
            WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}'
              AND AnnoRiferimento={Anno} AND MeseRiferimento={Mese} AND ISNULL(FatturaInviata,0)=0";
        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
            Assert.Ignore($"Nessuna fattura da inviare per {Tipologia} {Anno}/{Mese}: senza, le "
                + "contro-prove passerebbero senza provare nulla.");
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
