using System.Net;
using System.Text;
using System.Text.Json;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.IntegrationTest.InvioSap;
using static PortaleFatture.BE.IntegrationTest.Http.InvioSapHttp;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Input ostili e casi limite sui casi del testbook "Invio fatture a SAP" (PF-735/PF-965): cosa
/// succede quando un filtro o una conferma ricevono valori che la pagina normalmente non manda. Ogni
/// test porta nel nome il caso del testbook a cui si riferisce. I test con suffisso
/// <c>_Caratterizzazione</c> fissano il comportamento attuale dove è discutibile.
///
/// Regola verificata ovunque: una richiesta rifiutata non scrive nulla (nessun batch, nessun lock).
/// Stessi dati di <see cref="InvioSapTestbookHttpTests"/> (seed 2003).
/// </summary>
[Category("InvioSap")]
[Category("TestbookInvioSap")]
[NonParallelizable]
public class InvioSapTestbookAdversarialHttpTests
{
    private const string Rigo = """ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO" """;

    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        InvioSapTestData.SkipSeOggettiAssenti();
        _factory = new ApiTestFactory();
        InvioSapTestData.Pulisci();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        InvioSapTestData.Pulisci();
        FakeSynapseService.Instance.Reset();
        _factory?.Dispose();
    }

    [SetUp]
    public void Reset()
    {
        // Lo stato del seed lo ripristina il TearDown di ogni test (e il OneTimeSetUp per il primo)
        FakeSynapseService.Instance.Reset();
        InvioSapTestData.RilasciaLockComeData();
    }

    [TearDown]
    public void Cleanup() => InvioSapTestData.Pulisci();

    private Task<HttpResponseMessage> Griglia(string body) => Post(_factory, InvioSapTestData.RottaGriglia, body);
    private Task<HttpResponseMessage> Dettaglio(string campi) => Post(_factory, InvioSapTestData.RottaDettaglio, "{" + campi + "}");
    private Task<HttpResponseMessage> Genera(object body) => Put(_factory, InvioSapTestData.RottaGenera, JsonSerializer.Serialize(body, Web));
    private Task<HttpResponseMessage> GeneraGrezzo(string body) => Put(_factory, InvioSapTestData.RottaGenera, body);

    private static void NessunaScrittura()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty, "nessun batch");
            Assert.That(InvioSapTestData.BatchDelLock(), Is.Null, "nessun lock");
            Assert.That(FakeSynapseService.Instance.Chiamate, Is.Empty, "nessuna chiamata a Synapse");
        });
    }

    // ================================================================================ T02: accesso

    private static readonly (HttpMethod Metodo, string Rotta, string? Body)[] RotteDellaPagina =
    [
        (HttpMethod.Get, InvioSapTestData.RottaAnni, null),
        (HttpMethod.Post, InvioSapTestData.RottaMesi, """{ "anno": 2003 }"""),
        (HttpMethod.Post, InvioSapTestData.RottaTipologie, """{ "anno": 2003 }"""),
        (HttpMethod.Get, InvioSapTestData.RottaStati, null),
        (HttpMethod.Post, InvioSapTestData.RottaGriglia, """{ "anno": 2003 }"""),
        (HttpMethod.Post, InvioSapTestData.RottaDettaglio, "{" + Rigo + """, "statoInvio": "INVIATE" }"""),
        (HttpMethod.Post, InvioSapTestData.RottaDownload, """{ "anno": 2003 }"""),
        (HttpMethod.Post, InvioSapTestData.RottaDownloadReinviate, """{ "anno": 2003 }"""),
        (HttpMethod.Get, InvioSapTestData.RottaLock, null),
        (HttpMethod.Put, InvioSapTestData.RottaGenera, """{ "anno": 2003, "mese": 2, "tipologiaFattura": "PRIMO SALDO", "nota": "x" }""")
    ];

    private async Task<List<(string Rotta, int Esito)>> ChiamaTutte(HttpClient client, string prodottoNonce = "prod-pn")
    {
        var esiti = new List<(string, int)>();
        foreach (var (metodo, rotta, body) in RotteDellaPagina)
        {
            using var req = new HttpRequestMessage(metodo, _factory.WithNonce(rotta, prodotto: prodottoNonce));
            if (body is not null) req.Content = new StringContent(body, Encoding.UTF8, "application/json");
            esiti.Add((rotta, (int)(await client.SendAsync(req)).StatusCode));
        }
        return esiti;
    }

    /// <summary>
    /// T02 — Accesso diretto senza essere ADMIN del pannello: anonimo 401, admin di un ente SelfCare
    /// 403, nonce di un altro prodotto 419. In nessun caso parte una generazione.
    /// </summary>
    [Test]
    public async Task T02_AccessoNonAutorizzato_ShouldEssereNegatoSuOgniRotta()
    {
        var anonimo = await ChiamaTutte(_factory.CreateClientAs(null));
        var selfCare = await ChiamaTutte(_factory.CreateClientAs(Ruolo.ADMIN, auth: AuthType.SELFCARE, profilo: Profilo.PubblicaAmministrazione));
        var altroProdotto = await ChiamaTutte(_factory.CreateClientAs(Ruolo.ADMIN), prodottoNonce: "prod-appio");

        Assert.Multiple(() =>
        {
            Assert.That(anonimo.Select(x => x.Esito), Is.All.EqualTo(401), string.Join(", ", anonimo));
            Assert.That(selfCare.Select(x => x.Esito), Is.All.EqualTo(403), string.Join(", ", selfCare));
            Assert.That(altroProdotto.Select(x => x.Esito), Is.All.EqualTo(419), string.Join(", ", altroProdotto));
        });
        NessunaScrittura();
    }

    // ============================================================================ T04–T08: filtri

    /// <summary>
    /// T04 — Anno fuori intervallo: sono ammessi gli anni dal 2000 a quello successivo al corrente. Un
    /// anno ammesso ma senza fatture dà 404, uno fuori intervallo 400.
    /// </summary>
    [Test]
    public async Task T04_AnnoFuoriIntervallo_ShouldEssere400()
    {
        var annoCorrente = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Rome").Year;

        var prossimo = await Griglia($$"""{ "anno": {{annoCorrente + 1}} }""");
        var troppoAvanti = await Griglia($$"""{ "anno": {{annoCorrente + 2}} }""");
        var troppoIndietro = await Griglia("""{ "anno": 1999 }""");
        var nullo = await Griglia("""{ "anno": null }""");

        Assert.Multiple(() =>
        {
            Assert.That(prossimo.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "anno ammesso, senza fatture");
            Assert.That(troppoAvanti.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(troppoIndietro.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(nullo.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        });
    }

    /// <summary>
    /// T04 — L'anno è un numero: come testo (anche numerico fra virgolette no: v. sotto) o con
    /// un'injection non supera il binding e la risposta è 500, come per gli altri body malformati.
    /// </summary>
    [TestCase("""{ "anno": "duemilatre" }""")]
    [TestCase("""{ "anno": "2003 OR 1=1" }""")]
    [TestCase("""{ "anno": 2003.5 }""")]
    public async Task T04_AnnoNonNumerico_ShouldEssere500_Caratterizzazione(string body)
    {
        var resp = await Griglia(body);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    /// <summary>
    /// T04 — L'anno come stringa numerica ("2003") viene accettato dal binding: la pagina può mandarlo
    /// anche così.
    /// </summary>
    [Test]
    public async Task T04_AnnoComeStringaNumerica_ShouldEssereAccettato_Caratterizzazione()
    {
        var resp = await Griglia("""{ "anno": "2003" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// T05, T06 — Mesi non validi: fuori da 1-12 o ripetuti danno 400; un mese valido senza fatture
    /// dà 404.
    /// </summary>
    [TestCase("[0]", HttpStatusCode.BadRequest)]
    [TestCase("[13]", HttpStatusCode.BadRequest)]
    [TestCase("[-1]", HttpStatusCode.BadRequest)]
    [TestCase("[1, 1]", HttpStatusCode.BadRequest)]
    [TestCase("[12]", HttpStatusCode.NotFound)]
    [TestCase("[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]", HttpStatusCode.OK)]
    public async Task T05_T06_MesiNonValidi_ShouldEssere400(string mesi, HttpStatusCode atteso)
    {
        var resp = await Griglia($$"""{ "anno": 2003, "mesi": {{mesi}} }""");

        Assert.That(resp.StatusCode, Is.EqualTo(atteso));
    }

    /// <summary>
    /// T07 — Una tipologia che non esiste, o con un tentativo di injection, non trova nulla: resta un
    /// valore. Una tipologia vuota nella multiselezione è un errore.
    /// </summary>
    [TestCase("TIPOLOGIA INESISTENTE", HttpStatusCode.NotFound)]
    [TestCase("PRIMO SALDO' OR '1'='1", HttpStatusCode.NotFound)]
    [TestCase("'; DROP TABLE pfw.RegistroInvioSAP; --", HttpStatusCode.NotFound)]
    [TestCase("%", HttpStatusCode.NotFound)]
    [TestCase("", HttpStatusCode.BadRequest)]
    [TestCase("   ", HttpStatusCode.BadRequest)]
    public async Task T07_TipologiaNonValida_ShouldNonAllargareIlFiltro(string tipologia, HttpStatusCode atteso)
    {
        var resp = await Griglia(JsonSerializer.Serialize(new { anno = 2003, tipologieFattura = new[] { tipologia } }));

        Assert.That(resp.StatusCode, Is.EqualTo(atteso));
        Assert.That(InvioSapTestData.ContaRighe(InvioSapTestData.Registro), Is.GreaterThan(0), "tabella intatta");
    }

    /// <summary>
    /// T07 — Il confronto sulla tipologia non distingue maiuscole e minuscole (collation del database).
    /// </summary>
    [Test]
    public async Task T07_TipologiaInMinuscolo_ShouldTrovareGliStessiRighi_Caratterizzazione()
    {
        var righe = await Json(await Griglia("""{ "anno": 2003, "tipologieFattura": ["anticipo"] }"""));

        Assert.That(righe.EnumerateArray().Select(r => r.GetProperty("id").GetString()), Is.EqualTo(new[] { "ANTICIPO|2003|1|INVIATE" }));
    }

    /// <summary>
    /// T08, T16 — "Re-inviate" non è un valore del filtro di stato: mandato come stato dà 400.
    /// </summary>
    [TestCase("REINVIATE")]
    [TestCase("RE-INVIATE")]
    [TestCase("INVIATA")]
    [TestCase("TUTTI")]
    [TestCase("<script>alert(1)</script>")]
    public async Task T08_T16_StatoNonAmmesso_ShouldEssere400(string stato)
    {
        var resp = await Griglia(JsonSerializer.Serialize(new { anno = 2003, statoInvio = stato }));

        Assert.Multiple(async () =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await resp.Content.ReadAsStringAsync(), Does.Contain("Stato invio non valido").And.Not.Contain("<script>"));
        });
    }

    /// <summary>
    /// T13 — Download delle re-inviate: l'anno è obbligatorio, e un filtro ostile non produce un file.
    /// </summary>
    [TestCase("{}", HttpStatusCode.BadRequest)]
    [TestCase("""{ "mesi": [1] }""", HttpStatusCode.BadRequest)]
    [TestCase("""{ "anno": 2003, "mesi": [13] }""", HttpStatusCode.BadRequest)]
    [TestCase("""{ "anno": 2003, "tipologieFattura": ["' OR 1=1 --"] }""", HttpStatusCode.NotFound)]
    [TestCase("""{ "anno": 2001 }""", HttpStatusCode.NotFound)]
    public async Task T13_DownloadReInviateConFiltroNonValido_ShouldNonProdurreUnFile(string body, HttpStatusCode atteso)
    {
        var resp = await Post(_factory, InvioSapTestData.RottaDownloadReinviate, body);

        Assert.That(resp.StatusCode, Is.EqualTo(atteso));
        Assert.That(resp.Content.Headers.ContentDisposition, Is.Null, "nessun allegato");
    }

    // ================================================================== T15, T22–T25: dettaglio

    /// <summary>
    /// T22 — Il dettaglio è di un rigo preciso: senza stato, con "Tutte" o con un periodo non valido la
    /// richiesta è rifiutata con 400.
    /// </summary>
    [TestCase(Rigo)]
    [TestCase(Rigo + """, "statoInvio": "TUTTE" """)]
    [TestCase(Rigo + """, "statoInvio": "" """)]
    [TestCase(""" "annoRiferimento": 2003, "meseRiferimento": 13, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE" """)]
    [TestCase(""" "annoRiferimento": 2003, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE" """)]
    [TestCase(""" "annoRiferimento": 2003, "meseRiferimento": 1, "statoInvio": "INVIATE" """)]
    [TestCase(""" "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE" """)]
    public async Task T22_DettaglioSenzaUnRigoPreciso_ShouldEssere400(string campi)
    {
        var resp = await Dettaglio(campi);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// T15, T23 — Nei filtri del dettaglio i caratteri speciali sono letterali: jolly e injection non
    /// allargano la ricerca alle altre fatture del rigo.
    /// </summary>
    [TestCase("ragioneSociale", "%")]
    [TestCase("ragioneSociale", "_")]
    [TestCase("ragioneSociale", "' OR '1'='1")]
    [TestCase("ragioneSociale", "<script>alert(1)</script>")]
    [TestCase("numeroFattura", "%")]
    [TestCase("numeroFattura", "9502 OR 1=1")]
    public async Task T15_T23_FiltriDelDettaglioOstili_ShouldNonTrovareNulla(string campo, string valore)
    {
        var resp = await Dettaglio(Rigo + $", \"statoInvio\": \"INVIATE\", \"{campo}\": {JsonSerializer.Serialize(valore)}");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// T23 — Filtri del dettaglio troppo lunghi: 400 con un messaggio, non un errore del database.
    /// </summary>
    [TestCase("ragioneSociale", 256, "Ragione sociale troppo lunga")]
    [TestCase("numeroFattura", 51, "Numero fattura troppo lungo")]
    public async Task T23_FiltriDelDettaglioTroppoLunghi_ShouldEssere400(string campo, int lunghezza, string messaggio)
    {
        var resp = await Dettaglio(Rigo + $", \"statoInvio\": \"INVIATE\", \"{campo}\": \"{new string('a', lunghezza)}\"");

        Assert.Multiple(async () =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await resp.Content.ReadAsStringAsync(), Does.Contain(messaggio));
        });
    }

    /// <summary>
    /// T23 — La data del filtro va in formato ISO: una data all'italiana non supera il binding (500).
    /// </summary>
    [TestCase("10/02/2003")]
    [TestCase("ieri")]
    public async Task T23_DataFatturaInFormatoNonIso_ShouldEssere500_Caratterizzazione(string data)
    {
        var resp = await Dettaglio(Rigo + $", \"statoInvio\": \"INVIATE\", \"dataFattura\": \"{data}\"");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    /// <summary>
    /// T25 — Paginazione del dettaglio: la prima pagina è la 1. Pagina 0 (l'indice di una tabella che
    /// conta da zero), valori negativi, pagine troppo grandi o un solo parametro su due danno 400
    /// "Paginazione non valida".
    /// </summary>
    [TestCase(""" "page": 0, "pageSize": 10 """)]
    [TestCase(""" "page": -1, "pageSize": 10 """)]
    [TestCase(""" "page": 1, "pageSize": 0 """)]
    [TestCase(""" "page": 1, "pageSize": 501 """)]
    [TestCase(""" "page": 1 """)]
    [TestCase(""" "pageSize": 10 """)]
    public async Task T25_PaginazioneNonValida_ShouldEssere400(string paginazione)
    {
        var resp = await Dettaglio(Rigo + ", \"statoInvio\": \"INVIATE\", " + paginazione);

        Assert.Multiple(async () =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await resp.Content.ReadAsStringAsync(), Does.Contain("Paginazione non valida"));
        });
    }

    /// <summary>
    /// T25 — Una pagina oltre l'ultima risponde 200 con il totale giusto e nessuna fattura: il 404 è
    /// solo per il rigo o il filtro che non trovano nulla.
    /// </summary>
    [Test]
    public async Task T25_PaginaOltreLUltima_ShouldRestituireTotaleSenzaFatture_Caratterizzazione()
    {
        var resp = await Dettaglio(Rigo + """, "statoInvio": "INVIATE", "page": 99, "pageSize": 10 """);
        var corpo = await Json(resp);

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(corpo.GetProperty("count").GetInt32(), Is.EqualTo(2));
            Assert.That(corpo.GetProperty("fatture").GetArrayLength(), Is.EqualTo(0));
        });
    }

    // ============================================================ T26–T32: selezione delle fatture

    /// <summary>
    /// T28 — Selezione non valida: elenco vuoto, id non positivi o ripetuti danno 400; una fattura che
    /// non esiste dà 404. Nulla viene scritto.
    /// </summary>
    [TestCase("[]", HttpStatusCode.BadRequest, "Selezionare almeno una fattura")]
    [TestCase("[0]", HttpStatusCode.BadRequest, "Identificativo fattura non valido")]
    [TestCase("[-9505]", HttpStatusCode.BadRequest, "Identificativo fattura non valido")]
    [TestCase("[9505, 9505]", HttpStatusCode.BadRequest, "fatture ripetute")]
    [TestCase("[99999999]", HttpStatusCode.NotFound, "")]
    public async Task T28_SelezioneNonValida_ShouldEssereRifiutataSenzaScritture(string ids, HttpStatusCode atteso, string messaggio)
    {
        var resp = await GeneraGrezzo($$"""{ "anno": 2003, "mese": 2, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "DA_INVIARE", "idFatture": {{ids}}, "nota": "x" }""");

        Assert.Multiple(async () =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(atteso));
            Assert.That(await resp.Content.ReadAsStringAsync(), Does.Contain(messaggio));
        });
        NessunaScrittura();
    }

    /// <summary>
    /// T28 — Oltre mille fatture in una selezione: 400, senza arrivare al database.
    /// </summary>
    [Test]
    public async Task T28_SelezioneOltreIlLimite_ShouldEssere400()
    {
        var resp = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", idFatture = Enumerable.Range(1, 1001).ToArray(), nota = "x" });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        NessunaScrittura();
    }

    /// <summary>
    /// T26, T30 — Rigo selezionato per intero ma con tutte le fatture deselezionate: non resta nulla da
    /// inviare, 400. Deselezionare una fattura di un altro rigo è anch'esso un errore.
    /// </summary>
    [Test]
    public async Task T26_T30_RigoSelezionatoSenzaFattureRimaste_ShouldEssere400()
    {
        var tutteEscluse = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", allSelected = true, idFattureEscluse = new[] { 9505, 9506 }, nota = "x" });
        var esclusaDiUnAltroRigo = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", allSelected = true, idFattureEscluse = new[] { 9502 }, nota = "x" });

        Assert.Multiple(() =>
        {
            Assert.That(tutteEscluse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(esclusaDiUnAltroRigo.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        });
        NessunaScrittura();
    }

    /// <summary>
    /// T31 — Il gruppo dichiarato e le fatture scelte devono coincidere: fatture di un'altra tipologia,
    /// di un altro mese o di un altro anno vengono rifiutate.
    /// </summary>
    [TestCase(9509L, TestName = "T31_FatturaDiUnAltraTipologia_ShouldEssere400")]
    [TestCase(9502L, TestName = "T31_FatturaDiUnAltroMese_ShouldEssere400")]
    public async Task T31_FatturaFuoriDalGruppo_ShouldEssere400(long idFattura)
    {
        var resp = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", idFatture = new[] { 9505, idFattura }, nota = "x" });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        NessunaScrittura();
    }

    /// <summary>
    /// T32 — Una fattura in elaborazione non parte nemmeno dichiarandola "inviata" e aggiungendo lo
    /// storno: il backend guarda lo stato vero della fattura.
    /// </summary>
    [Test]
    public async Task T32_FatturaInElaborazioneDichiarataInviata_ShouldEssere400()
    {
        var resp = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "INVIATE", idFatture = new[] { InvioSapTestData.FatturaStessoMeseInElaborazione }, nota = "x", numeroDocumentoStorno = "NC-1" });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        NessunaScrittura();
    }

    // ====================================================== T37, T44, T47, T49: nota e storno

    /// <summary>
    /// T37, T47 — Storno e nota fatti di soli spazi valgono come assenti; oltre la lunghezza massima
    /// sono rifiutati con un messaggio.
    /// </summary>
    [TestCase("   ", "Motivo", "storno obbligatorio")]
    [TestCase("NC-1", "   ", "nota e' obbligatoria")]
    [TestCase("NC-1", "\t\n", "nota e' obbligatoria")]
    public async Task T37_T47_StornoONotaDiSoliSpazi_ShouldValereComeAssenti(string storno, string nota, string messaggio)
    {
        var resp = await Genera(new { anno = 2003, mese = 1, tipologiaFattura = "PRIMO SALDO", statoInvio = "INVIATE", idFatture = new[] { 9502 }, nota, numeroDocumentoStorno = storno });

        Assert.Multiple(async () =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await resp.Content.ReadAsStringAsync(), Does.Contain(messaggio));
        });
        NessunaScrittura();
    }

    /// <summary>
    /// T44 — Lunghezza della nota: 2000 caratteri sono accettati, 2001 no. Lo storno si ferma a 255.
    /// </summary>
    [TestCase(2000, 255, HttpStatusCode.Accepted)]
    [TestCase(2001, 255, HttpStatusCode.BadRequest)]
    [TestCase(2000, 256, HttpStatusCode.BadRequest)]
    public async Task T44_LunghezzaDiNotaEStorno_ShouldRispettareILimiti(int nota, int storno, HttpStatusCode atteso)
    {
        var resp = await Genera(new { anno = 2003, mese = 1, tipologiaFattura = "PRIMO SALDO", statoInvio = "INVIATE", idFatture = new[] { 9502 }, nota = new string('à', nota), numeroDocumentoStorno = new string('9', storno) });

        Assert.That(resp.StatusCode, Is.EqualTo(atteso));
        if (atteso != HttpStatusCode.Accepted) NessunaScrittura();
    }

    /// <summary>
    /// T47 — Lo storno distingue primo invio e re-invio: su una fattura mai inviata non è ammesso, e
    /// una fattura mai inviata non si può "re-inviare" aggiungendolo.
    /// </summary>
    [Test]
    public async Task T47_StornoSuFatturaMaiInviata_ShouldEssere400()
    {
        var primoInvioConStorno = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", idFatture = new[] { 9505 }, nota = "x", numeroDocumentoStorno = "NC-1" });
        var reInvioDiMaiInviata = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "INVIATE", idFatture = new[] { 9505 }, nota = "x", numeroDocumentoStorno = "NC-1" });

        Assert.Multiple(async () =>
        {
            Assert.That(primoInvioConStorno.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await primoInvioConStorno.Content.ReadAsStringAsync(), Does.Contain("non e' ammesso nel primo invio"));
            Assert.That(reInvioDiMaiInviata.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await reInvioDiMaiInviata.Content.ReadAsStringAsync(), Does.Contain("solo fatture gia' inviate"));
        });
        NessunaScrittura();
    }

    /// <summary>
    /// T49 — Injection SQL in nota e storno: sono dati. Il batch nasce, le tabelle restano al loro
    /// posto e i valori sono salvati così come sono stati scritti.
    /// </summary>
    [Test]
    public async Task T49_InjectionSqlInNotaEStorno_ShouldEssereSalvataComeTesto()
    {
        const string nota = "'; DROP TABLE pfw.DettaglioInvioSAP; --";
        const string storno = "NC'); DELETE FROM pfw.RegistroInvioSAP; --";
        var batchPrima = InvioSapTestData.ContaRighe(InvioSapTestData.Registro);

        var resp = await Genera(new { anno = 2003, mese = 1, tipologiaFattura = "PRIMO SALDO", statoInvio = "INVIATE", idFatture = new[] { 9503 }, nota, numeroDocumentoStorno = storno });
        var batch = (await Json(resp)).GetProperty("batchId").GetInt32();

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(InvioSapTestData.ContaRighe(InvioSapTestData.Registro), Is.EqualTo(batchPrima + 1), "nessun batch cancellato");
            Assert.That(InvioSapTestData.Scalar($"SELECT JSON_VALUE(CAST(Nota AS nvarchar(max)), '$.Testo') FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b", ("@b", batch)), Is.EqualTo(nota));
            Assert.That(InvioSapTestData.Scalar($"SELECT NumeroDocStorno FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b", ("@b", batch)), Is.EqualTo(storno));
        });
    }

    // ============================================================= T40, T46, T50: generazione

    /// <summary>
    /// T40 — Conferma con una generazione già in corso (di un'altra sessione): 409 con il batch che
    /// tiene il lock, senza scrivere nulla, qualunque sia il contenuto della richiesta.
    /// </summary>
    [Test]
    public async Task T40_ConfermaConGenerazioneInCorso_ShouldEssere409SenzaScritture()
    {
        InvioSapTestData.AttivaLock(InvioSapTestData.BatchErrore);

        var resp = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", allSelected = true, nota = "x" });
        var corpo = await Json(resp);

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(corpo.GetProperty("motivo").GetString(), Is.EqualTo("LOCK_ATTIVO"));
            Assert.That(corpo.GetProperty("batchId").GetInt32(), Is.EqualTo(InvioSapTestData.BatchErrore));
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty);
            Assert.That(InvioSapTestData.FatturaInviata(9505), Is.False, "fattura non toccata");
        });
    }

    /// <summary>
    /// T46 — La pipeline non parte (Synapse risponde KO): la pagina riceve un errore senza dettagli
    /// tecnici.
    /// </summary>
    [Test]
    public async Task T46_AvvioDellaPipelineFallito_ShouldRispondere500SenzaDettagliTecnici()
    {
        FakeSynapseService.Instance.Esito = false;

        var resp = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", allSelected = true, nota = "x" });
        var testo = await resp.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(testo, Does.Not.Contain("Exception").And.Not.Contain("   at "));
        });
    }

    /// <summary>
    /// T50 — Cinque sessioni confermano insieme lo stesso invio: ne passa una sola, le altre ricevono
    /// il conflitto, e la fattura finisce in un solo batch.
    /// </summary>
    [Test]
    public async Task T50_CinqueSessioniInsieme_ShouldAccettarneUnaSola()
    {
        var body = JsonSerializer.Serialize(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", idFatture = new[] { 9505 }, nota = "Cinque sessioni" }, Web);

        var esiti = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Put(_factory, InvioSapTestData.RottaGenera, body)));

        Assert.Multiple(() =>
        {
            Assert.That(esiti.Count(r => r.StatusCode == HttpStatusCode.Accepted), Is.EqualTo(1));
            Assert.That(esiti.Count(r => r.StatusCode == HttpStatusCode.Conflict), Is.EqualTo(4));
            Assert.That(InvioSapTestData.BatchDeiTest(), Has.Count.EqualTo(1));
            Assert.That(FakeSynapseService.Instance.RichiesteGenerazione, Has.Count.EqualTo(1));
        });
    }

    /// <summary>
    /// T39, T40 — Una conferma con il corpo vuoto o malformato non avvia nulla.
    /// </summary>
    [TestCase("{}", HttpStatusCode.BadRequest)]
    [TestCase("null", HttpStatusCode.BadRequest)]
    [TestCase("""{ "anno": 2003, "mese": 2, "tipologiaFattura": "PRIMO SALDO" }""", HttpStatusCode.BadRequest)]
    [TestCase("non-json", HttpStatusCode.InternalServerError)]
    [TestCase("""{ "anno": 2003, "mese": 2, "tipologiaFattura": "PRIMO SALDO", "nota": "x", "idFatture": "9505" }""", HttpStatusCode.InternalServerError)]
    public async Task T39_T40_ConfermaSenzaDatiValidi_ShouldNonAvviareNulla(string body, HttpStatusCode atteso)
    {
        var resp = await GeneraGrezzo(body);

        Assert.That(resp.StatusCode, Is.EqualTo(atteso));
        NessunaScrittura();
    }
}
