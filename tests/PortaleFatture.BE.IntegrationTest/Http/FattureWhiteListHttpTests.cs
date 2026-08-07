using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Rotte `api/fatture/pagopa/whitelist/*` attraverso la pipeline reale. Complementano
/// FattureWhiteListCommandIntegrationTests, che invocano gli handler via MediatR e quindi saltano
/// routing, [Authorize] e model binding.
///
/// Qui si vede come i due contratti di ritorno dei command arrivano al CLIENT, ed è dove i loro
/// difetti diventano osservabili dal frontend:
///   inserisci · true -> 200, qualunque altro esito -> 409 Conflict
///   elimina   · 0 -> 200, negativo -> 409 Conflict, positivo -> 400 (**ramo irraggiungibile**: il
///               command restituisce `righe aggiornate - id richiesti`, che non può essere > 0)
///
/// Sandbox: Anno 2099, come i test sui command. Cleanup per anno.
/// </summary>
public class FattureWhiteListHttpTests
{
    private const int AnnoSandbox = 2099;
    private const string Ente = "11111111-1111-1111-1111-111111111111";
    private const string Tipologia = "PRIMO SALDO";
    private const string RottaInserisci = "/api/fatture/pagopa/whitelist/inserisci";
    private const string RottaWhiteList = "/api/fatture/pagopa/whitelist";

    private ApiTestFactory _factory;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    [SetUp]
    public void Reset()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        Pulisci();
    }

    [TearDown]
    public void Cleanup() => Pulisci();

    [Test]
    public async Task Inserisci_MesiValidi_ShouldReturn200()
    {
        var resp = await Admin().PostAsJsonAsync(_factory.WithNonce(RottaInserisci),
            new { tipologiaFattura = Tipologia, idEnte = Ente, anno = AnnoSandbox, mesi = new[] { 1, 2 } });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(RigheSandbox(), Is.EqualTo(2));
    }

    [Test]
    public async Task Inserisci_SenzaMesi_ShouldReturn200_SenzaInserireNulla_Caratterizzazione()
    {
        // Qui il difetto del command diventa visibile al client: una richiesta che non fa nulla
        // riceve un 200 indistinguibile da un inserimento riuscito. Il portale mostrerà "salvato".
        var resp = await Admin().PostAsJsonAsync(_factory.WithNonce(RottaInserisci),
            new { tipologiaFattura = Tipologia, idEnte = Ente, anno = AnnoSandbox, mesi = Array.Empty<int>() });

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(RigheSandbox(), Is.Zero, "Nessuna riga inserita, eppure 200.");
        });
    }

    [Test]
    public async Task Inserisci_ConDatiCheFannoFallireLaScrittura_ShouldReturn409()
    {
        var resp = await Admin().PostAsJsonAsync(_factory.WithNonce(RottaInserisci),
            new { tipologiaFattura = new string('X', 200), idEnte = Ente, anno = AnnoSandbox, mesi = new[] { 3 } });

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict),
                "Il false del command diventa 409, non un 500: l'errore SQL è gestito.");
            Assert.That(RigheSandbox(), Is.Zero, "Rollback: nessun residuo.");
        });
    }

    [Test]
    public async Task Elimina_IdEsistenti_ShouldReturn200()
    {
        await Admin().PostAsJsonAsync(_factory.WithNonce(RottaInserisci),
            new { tipologiaFattura = Tipologia, idEnte = Ente, anno = AnnoSandbox, mesi = new[] { 4, 5 } });
        var ids = IdSandbox();

        var resp = await Delete(new { ids });

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(RigheAttiveSandbox(), Is.Zero, "Soft-delete: le righe restano ma con DataFine valorizzata.");
            Assert.That(RigheSandbox(), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Elimina_IdInesistente_ShouldReturn409()
    {
        var resp = await Delete(new { ids = new[] { int.MaxValue } });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict),
            "Il negativo del command diventa 409. Nota: 409 non distingue 'id inesistente' da "
            + "'già cancellata', e nemmeno da 'alcuni id sì e altri no'.");
    }

    [Test]
    public async Task Elimina_SuGiaCancellata_ShouldReturn409()
    {
        await Admin().PostAsJsonAsync(_factory.WithNonce(RottaInserisci),
            new { tipologiaFattura = Tipologia, idEnte = Ente, anno = AnnoSandbox, mesi = new[] { 6 } });
        var ids = IdSandbox();
        await Delete(new { ids });

        var resp = await Delete(new { ids });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task WhiteList_SenzaAutenticazione_ShouldReturn401()
    {
        var resp = await _factory.CreateClientAs(null).PostAsJsonAsync(
            _factory.WithNonce(RottaInserisci),
            new { tipologiaFattura = Tipologia, idEnte = Ente, anno = AnnoSandbox, mesi = new[] { 1 } });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task WhiteList_ConTokenAderente_ShouldReturn403()
    {
        // La whitelist è uno strumento amministrativo: un aderente non deve poter escludere
        // sé stesso (o altri) dalla fatturazione.
        var client = _factory.CreateClientAs(Ruolo.ADMIN, Ente, AuthType.SELFCARE, Profilo.PubblicaAmministrazione);

        var resp = await client.PostAsJsonAsync(_factory.WithNonce(RottaInserisci),
            new { tipologiaFattura = Tipologia, idEnte = Ente, anno = AnnoSandbox, mesi = new[] { 1 } });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    // ---------------------------------------------------------------------------------------------

    private HttpClient Admin() => _factory.CreateClientAs(Ruolo.ADMIN);

    private async Task<HttpResponseMessage> Delete(object body)
    {
        var richiesta = new HttpRequestMessage(HttpMethod.Delete, _factory.WithNonce(RottaWhiteList))
        {
            Content = JsonContent.Create(body)
        };
        return await Admin().SendAsync(richiesta);
    }

    private static int RigheSandbox() => Conta("SELECT COUNT(*) FROM pfd.FattureWhiteList WHERE Anno = @anno");

    private static int RigheAttiveSandbox() =>
        Conta("SELECT COUNT(*) FROM pfd.FattureWhiteList WHERE Anno = @anno AND DataFine IS NULL");

    private static int[] IdSandbox()
    {
        var ids = new List<int>();
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT IdLista FROM pfd.FattureWhiteList WHERE Anno = @anno AND DataFine IS NULL";
        cmd.Parameters.AddWithValue("@anno", AnnoSandbox);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            ids.Add(reader.GetInt32(0));
        return [.. ids];
    }

    private static int Conta(string sql)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@anno", AnnoSandbox);
        return (int)cmd.ExecuteScalar()!;
    }

    private static void Pulisci()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM pfd.FattureWhiteList WHERE Anno = @anno";
        cmd.Parameters.AddWithValue("@anno", AnnoSandbox);
        cmd.ExecuteNonQuery();
    }
}
