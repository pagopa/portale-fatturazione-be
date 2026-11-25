using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using static PortaleFatture.BE.IntegrationTest.FattureAzioneTestData;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// PF-1003: test HTTP end-to-end su <c>POST /api/fatture</c> per il campo <c>azione</c>, cioè il contratto
/// che il frontend legge per decidere quale pulsante mostrare su ogni fattura. Gli integration test
/// verificano la query; qui si verifica che il valore arrivi davvero nel JSON, con quel nome e quel
/// formato, attraverso routing, autorizzazione, binding e serializzazione.
///
/// Dati: <see cref="FattureAzioneTestData"/> (date relative al mese corrente del DB). La gemella con i
/// feature flag spenti è in <c>FlagSpentiHttpTests.cs</c>.
/// </summary>
[Category("Fatture")]
[NonParallelizable]
public class FattureRicercaAzioneHttpTests
{
    private const string Rotta = "/api/fatture";
    private ApiTestFactory _factory = null!;
    private DateTime _oggi;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _oggi = OggiDb(LocalTestDb.ConnectionString);
        Inserisci(LocalTestDb.ConnectionString, _oggi);
        _factory = new ApiTestFactory();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory?.Dispose();
        try { Pulisci(LocalTestDb.ConnectionString); } catch (SqlException) { /* container spento */ }
    }

    private async Task<(HttpStatusCode Status, string Corpo)> Post(object body, string ruolo = Ruolo.ADMIN)
    {
        var client = _factory.CreateClientAs(ruolo);
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(_factory.WithNonce(Rotta), content);
        return (resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    private Task<(HttpStatusCode Status, string Corpo)> PostPeriodo(int offset, string ruolo = Ruolo.ADMIN, object? extra = null)
    {
        var d = new DateTime(_oggi.Year, _oggi.Month, 1).AddMonths(-offset);
        return extra is null
            ? Post(new { cancellata = false, anno = d.Year, mese = d.Month }, ruolo)
            : Post(extra, ruolo);
    }

    /// <summary>
    /// Per ogni oggetto del JSON che ha un <c>idFattura</c>: se ha la proprietà <c>azione</c> e il suo valore
    /// (null compreso). Distingue "azione assente" da "azione null", che per il frontend non sono la stessa cosa.
    /// </summary>
    private static Dictionary<long, (bool Presente, string? Valore)> Azioni(string corpo)
    {
        var mappa = new Dictionary<long, (bool, string?)>();
        using var doc = JsonDocument.Parse(corpo);
        Visita(doc.RootElement);
        return mappa;

        void Visita(JsonElement e)
        {
            if (e.ValueKind == JsonValueKind.Object)
            {
                long? id = null;
                (bool, string?) azione = (false, null);
                foreach (var p in e.EnumerateObject())
                {
                    if (p.Name.Equals("idFattura", StringComparison.OrdinalIgnoreCase) && p.Value.TryGetInt64(out var v))
                        id = v;
                    if (p.Name == "azione")
                        azione = (true, p.Value.ValueKind == JsonValueKind.Null ? null : p.Value.GetString());
                    Visita(p.Value);
                }
                if (id is long trovato)
                    mappa[trovato] = azione;
            }
            else if (e.ValueKind == JsonValueKind.Array)
                foreach (var x in e.EnumerateArray()) Visita(x);
        }
    }

    [Test]
    public async Task MeseCorrente_ShouldEsporreAzionePerTipologia()
    {
        var (status, corpo) = await PostPeriodo(0);
        Assert.That(status, Is.EqualTo(HttpStatusCode.OK), corpo);
        var azioni = Azioni(corpo);

        Assert.Multiple(() =>
        {
            Assert.That(azioni[7801], Is.EqualTo((true, Elimina)), "ANTICIPO -> ELIMINA");
            Assert.That(azioni[7802], Is.EqualTo((true, Elimina)), "ACCONTO -> ELIMINA");
            Assert.That(azioni[7803], Is.EqualTo((true, Posticipa)), "PRIMO SALDO -> POSTICIPA");
            Assert.That(azioni[7804], Is.EqualTo((true, Posticipa)), "SECONDO SALDO -> POSTICIPA");
            Assert.That(azioni[7805], Is.EqualTo((true, Posticipa)), "VAR. SEMESTRALE -> POSTICIPA");
            Assert.That(azioni[7806], Is.EqualTo((true, Posticipa)), "SEM. SOSPESI -> POSTICIPA");
        });
    }

    [Test]
    public async Task SenzaAzione_ShouldArrivareComeNullEsplicito_NonComeCampoAssente()
    {
        // Mese -1: 7811 gia' inviata e 7823 eliminata (Stato 3) -> nessuna azione. 7821 ripristinata e 7822
        // cancellata conservano invece l'azione della vista.
        // Il campo deve esserci comunque (INCLUDE_NULL_VALUES + serializzazione senza WhenWritingNull):
        // il frontend legge "azione": null come "nessun pulsante".
        var (status, corpo) = await PostPeriodo(1);
        Assert.That(status, Is.EqualTo(HttpStatusCode.OK), corpo);
        var azioni = Azioni(corpo);

        Assert.Multiple(() =>
        {
            foreach (var id in new long[] { 7811, 7823 })
                Assert.That(azioni[id], Is.EqualTo((true, (string?)null)), $"{id}: atteso \"azione\": null.");
            Assert.That(azioni[7821], Is.EqualTo((true, Elimina)), "7821 ripristinata: si puo' agire di nuovo.");
            Assert.That(azioni[7822], Is.EqualTo((true, Elimina)), "7822 cancellata: si puo' agire di nuovo.");
            Assert.That(azioni[7812], Is.EqualTo((true, Posticipa)), "7812 in elaborazione: azione disponibile.");
            Assert.That(azioni.ContainsKey(7824), Is.False, "7824 posticipata: fuori dalla griglia.");
        });
    }

    [Test]
    public async Task FuoriFinestra_ShouldArrivareNull()
    {
        var (_, corpo) = await PostPeriodo(4);
        Assert.That(Azioni(corpo)[7835], Is.EqualTo((true, (string?)null)), "SECONDO SALDO a -4: fuori finestra.");
    }

    [Test]
    public async Task Operatore_ShouldRicevereLaStessaAzioneDellAdmin()
    {
        // OPERATOR e' in sola lettura: la rotta e' aperta a entrambi i ruoli e il campo non cambia per ruolo.
        // Se il prodotto decidesse di nascondere le azioni all'OPERATOR, questo test va aggiornato.
        var admin = Azioni((await PostPeriodo(0, Ruolo.ADMIN)).Corpo);
        var operatore = Azioni((await PostPeriodo(0, Ruolo.OPERATOR)).Corpo);

        Assert.That(operatore.Where(k => k.Key is >= 7801 and <= 7899),
            Is.EquivalentTo(admin.Where(k => k.Key is >= 7801 and <= 7899)));
    }

    [Test]
    public async Task NonFatturate_ShouldNonEsporreAzioni()
    {
        var d = new DateTime(_oggi.Year, _oggi.Month, 1).AddMonths(-1);
        var (status, corpo) = await Post(new { cancellata = true, anno = d.Year, mese = d.Month });
        Assert.That(status, Is.EqualTo(HttpStatusCode.OK), corpo);

        var azioni = Azioni(corpo);
        Assert.That(azioni.ContainsKey(7824), Is.True, "La posticipata 7824 compare tra le Non Fatturate.");
        Assert.That(azioni.Values.Select(v => v.Valore), Is.All.Null);
    }

    [Test]
    public async Task Download_StessaQuery_ShouldRestituireUnExcelValido()
    {
        // POST api/fatture/download esegue la stessa _sqlView: la join con la vista non deve romperlo.
        // L'Excel non esporta l'azione (TitoloFatturaDto -> Map() non la mappa): qui si verifica solo che il
        // file arrivi e sia un xlsx (zip, primi byte "PK").
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var body = JsonSerializer.Serialize(new { cancellata = false, anno = _oggi.Year, mese = _oggi.Month });
        var resp = await client.PostAsync(_factory.WithNonce("/api/fatture/download"),
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Assert.That(bytes.Length, Is.GreaterThan(4));
        Assert.That(Encoding.ASCII.GetString(bytes, 0, 2), Is.EqualTo("PK"), "Un .xlsx e' uno zip.");
    }

    [Test]
    public async Task MeseComeStringa_ShouldDareLaStessaAzione()
    {
        // Il frontend manda il mese come stringa: il binding non deve cambiare il risultato.
        var d = new DateTime(_oggi.Year, _oggi.Month, 1);
        var numero = Azioni((await Post(new { cancellata = false, anno = d.Year, mese = d.Month })).Corpo);
        var stringa = Azioni((await Post(new { cancellata = false, anno = d.Year, mese = d.Month.ToString() })).Corpo);

        Assert.That(stringa[7801], Is.EqualTo(numero[7801]));
    }
}
