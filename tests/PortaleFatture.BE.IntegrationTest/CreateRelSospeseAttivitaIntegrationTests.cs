using System.Text.Json;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture_BE_SendEmailFunction;
using PortaleFatture_BE_SendEmailFunction.Models;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Gemella di `CreateRelRigheAttivitaIntegrationTests` per l'activity **`CreateRelSospese`**, che il
/// 22/09/2026 ha ricevuto la stessa correzione: il ramo "nessuna REL sospesa per il periodo" non
/// solleva piu' `DomainException` ma restituisce `Count = 0`, cosi' l'orchestrazione chiude in
/// `Completed` e la pipeline legge l'esito in `output` invece di vedere un `Failed` con stack trace.
///
/// PERIMETRO, e non e' una scelta di comodo: il percorso felice **non e' esercitabile sul DB seedato**
/// perche' `RelRigheSospeseQueryGetById` legge `pfd.tmpRelRighe`, che nel seed non esiste (v.
/// `docs/test-integrazione-db-seedato.md`). Per lo stesso motivo i periodi vanno scelti **senza righe
/// in `pfd.tmpRelTestata`**: un periodo con staging entrerebbe nel ramo "trovate" e fallirebbe su una
/// tabella assente, cioe' per un difetto del seed e non del prodotto. Il caso che conta — quello
/// corretto — e' comunque il periodo vuoto, che non tocca ne' righe ne' blob.
///
/// ⚠️ Da NON allineare alle attese del flusso ordinario: `RelRigheSospeseQueryGetByIdPersistence`
/// conserva di proposito la vecchia scelta testuale del periodo (chiarito il 22/09/2026, v.
/// `docs/pipeline-dati-send.md`). Le due query divergono, ed e' voluto.
/// </summary>
public class CreateRelSospeseAttivitaIntegrationTests
{
    // Periodo senza alcuna tmpRelTestata nel seed. Volutamente distinto sia dai periodi delle altre
    // fixture sia da quello usato dalla gemella (2031/7), cosi' un fallimento dice subito quale delle
    // due sta parlando.
    private const int AnnoSenzaRel = 2031;
    private const int MeseSenzaRel = 8;
    private const string Tipologia = "SECONDO SALDO";

    /// <summary>
    /// ⚠️ **Oggi non c'e' nel DB seedato**, e senza di lei nemmeno il ramo "periodo vuoto" e'
    /// raggiungibile: `RelTestataSospesaSQLBuilder` la mette in `INNER JOIN` con `pfd.tmpRelTestata`,
    /// quindi la query fallisce con una `SqlException` **prima** di poter restituire zero righe.
    ///
    /// I test che la attraversano restano percio' **gialli** finche' la tabella non entra nel seed con
    /// la sua DDL reale (da chiedere, non da dedurre): diventano verdi da soli. E' il motivo per cui
    /// qui si usa `SkipSeOggettoAssente` e non il solito `SkipIfUnavailable` — un rosso da lacuna del
    /// seed mostrerebbe una stack trace di codice di produzione e farebbe cercare un difetto che non
    /// c'e'.
    /// </summary>
    private const string TabellaRiepilogo = "pfd.RiepilogoFatturazione_NPF";

    private static readonly string[] Variabili =
        ["CONNECTION_STRING", "StorageRELAccountName", "StorageRELAccountKey", "StorageRELBlobContainerName"];

    private readonly Dictionary<string, string?> _precedenti = [];
    private string _cn = null!;

    [SetUp]
    public void Setup()
    {
        _cn = LocalTestDb.ConnectionString;

        foreach (var nome in Variabili)
            _precedenti[nome] = Environment.GetEnvironmentVariable(nome, EnvironmentVariableTarget.Process);

        // Le tre variabili di storage devono solo essere non vuote: superano il controllo di
        // configurazione ma non vengono mai usate, perche' AddDocument sta nel ramo che qui non si
        // percorre. Nessuna credenziale reale, quindi, nemmeno per errore.
        Set("CONNECTION_STRING", _cn);
        Set("StorageRELAccountName", "test-non-usato");
        Set("StorageRELAccountKey", "test-non-usato");
        Set("StorageRELBlobContainerName", "test-non-usato");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var coppia in _precedenti)
            Environment.SetEnvironmentVariable(coppia.Key, coppia.Value, EnvironmentVariableTarget.Process);
    }

    // ---------------------------------------------------------------------------------------------
    // Il ramo corretto
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// La regressione della modifica: periodo senza REL sospese, nessuna eccezione, esito leggibile.
    /// Il messaggio deve dire **"rel sospese"** e non "rel": e' l'unica cosa che, leggendo un log o il
    /// campo `output`, distingue questa function dalla gemella.
    /// </summary>
    [Test]
    public async Task NessunaRelSospesa_ShouldRestituireCountZeroSenzaSollevare()
    {
        TestDb.SkipSeOggettoAssente(_cn, TabellaRiepilogo);

        var json = await Esegui(AnnoSenzaRel.ToString(), MeseSenzaRel.ToString(), Tipologia);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("Count").GetInt32(), Is.Zero);
            Assert.That(root.GetProperty("DbConnection").GetBoolean(), Is.True,
                "la query ha funzionato: non e' un problema di connessione");
            Assert.That(root.GetProperty("Error").GetString(), Does.Contain("Non ci sono rel sospese"));
            Assert.That(root.GetProperty("Anno").GetInt32(), Is.EqualTo(AnnoSenzaRel));
            Assert.That(root.GetProperty("Mese").GetInt32(), Is.EqualTo(MeseSenzaRel));
            Assert.That(root.GetProperty("TipologiaFattura").GetString(), Is.EqualTo(Tipologia));
        });
    }

    /// <summary>
    /// Tipologia inesistente: nessuna whitelist, il valore finisce nella WHERE e non seleziona nulla.
    /// Serve a dire che il ramo vuoto non e' legato al SECONDO SALDO ma a qualunque combinazione
    /// senza testate in staging.
    /// </summary>
    [Test]
    public async Task TipologiaInesistente_ShouldRestituireCountZeroSenzaSollevare()
    {
        TestDb.SkipSeOggettoAssente(_cn, TabellaRiepilogo);

        var json = await Esegui("2026", "5", "TIPOLOGIA CHE NON ESISTE");

        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("Count").GetInt32(), Is.Zero);
    }

    // ---------------------------------------------------------------------------------------------
    // Avversariali: quello che NON deve essere confuso con il periodo vuoto
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Un guasto vero deve restare tale: `Convert.ToInt32` solleva `FormatException`, il catch la
    /// riavvolge in `DomainException` e l'orchestrazione fallisce. E' la distinzione che la modifica
    /// introduce — "niente da fare" contro "rotto" — e senza questo test resterebbe affermata e non
    /// verificata.
    /// </summary>
    [Test]
    public void AnnoNonNumerico_ShouldSollevareDomainExceptionConMessaggioDiFormato()
    {
        var ex = Assert.ThrowsAsync<DomainException>(async () =>
            await Esegui("duemilaventisei", "6", Tipologia));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Does.Not.Contain("Non ci sono rel"),
                "un anno illeggibile non deve somigliare a un periodo vuoto");
            Assert.That(ex.InnerException, Is.TypeOf<FormatException>());
        });
    }

    /// <summary>
    /// CARATTERIZZAZIONE, identica alla gemella e per la stessa ragione: `Convert.ToInt32(null)` non
    /// solleva, restituisce 0. Un periodo assente diventa quindi l'anno zero e sfocia nel ramo vuoto,
    /// indistinguibile da un periodo legittimamente senza REL. Oggi non si manifesta perche'
    /// l'handler HTTP rifiuta con 400 le query string incomplete, ma la normalizzazione silenziosa
    /// vive nell'activity e sopravviverebbe a un secondo chiamante.
    /// </summary>
    [Test]
    public async Task AnnoEMeseAssenti_ShouldDiventareZeroEFinireNelRamoVuoto_Caratterizzazione()
    {
        TestDb.SkipSeOggettoAssente(_cn, TabellaRiepilogo);

        var json = await Esegui(null, null, Tipologia);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("Anno").GetInt32(), Is.Zero, "null convertito in 0, senza errore");
            Assert.That(root.GetProperty("Mese").GetInt32(), Is.Zero);
            Assert.That(root.GetProperty("Count").GetInt32(), Is.Zero);
        });
    }

    // ---------------------------------------------------------------------------------------------

    private static async Task<string> Esegui(string? anno, string? mese, string? tipologia)
    {
        var activity = new CreateRelSospese(LoggerFactory.Create(_ => { }));

        return await activity.RunAsync(new CreateRelSospeseDataRequest
        {
            Anno = anno,
            Mese = mese,
            TipologiaFattura = tipologia
        });
    }

    private static void Set(string nome, string? valore) =>
        Environment.SetEnvironmentVariable(nome, valore, EnvironmentVariableTarget.Process);
}
