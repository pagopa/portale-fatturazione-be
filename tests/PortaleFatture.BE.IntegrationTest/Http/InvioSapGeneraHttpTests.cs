using System.Net;
using System.Text;
using System.Text.Json;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.IntegrationTest.InvioSap;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// End-to-end di <c>PUT api/fatture/invio-sap/genera</c> attraverso la pipeline reale (routing,
/// autorizzazione, binding, regole, SP, Storico, Messaggi, Synapse finto) sul DB seedato. Copre le storie
/// US2 (generazione) e US3 (lock, completamento) del piano: T021, T028, T030, T033.
///
/// Due gruppi di casi:
///  - **400 formali**: non toccano il DB (la regola rifiuta prima), quindi girano anche senza la DDL di DATA;
///  - **202/404/409/500**: hanno bisogno delle tabelle e della SP, e restano gialli finche' mancano.
///
/// Il completamento lo simula il test scrivendo le tabelle come fa DATA (decisione 27/10: nessuna callback).
/// La pipeline Synapse e' <see cref="FakeSynapseService"/>: l'esito e' impostabile, per provare il ramo
/// "pipeline non avviata" senza raggiungere Azure.
/// </summary>
[Category("InvioSap")]
[Explicit("PF-965: rotte non ancora mappate (fase 3-4). Eseguire con --filter Category=InvioSap.")]
public class InvioSapGeneraHttpTests
{
    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown()
    {
        InvioSapTestData.Pulisci();
        _factory?.Dispose();
    }

    [SetUp]
    public void Reset()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        FakeSynapseService.Instance.Reset();
        InvioSapTestData.Pulisci();
    }

    [TearDown]
    public void Cleanup() => InvioSapTestData.Pulisci();

    // =============================================================================================
    // 400 formali (senza DB)
    // =============================================================================================

    [TestCase("""{ "anno": 2026, "mese": 7, "tipologiaFattura": "SECONDO SALDO", "idFatture": [1001] }""", "nota", TestName = "400(nota assente)")]
    [TestCase("""{ "anno": 2026, "mese": 7, "tipologiaFattura": "SECONDO SALDO", "idFatture": [1001], "nota": "   " }""", "nota", TestName = "400(nota di spazi)")]
    [TestCase("""{ "anno": 2026, "mese": 7, "tipologiaFattura": "SECONDO SALDO", "idFatture": [], "nota": "x" }""", "almeno una", TestName = "400(selezione vuota)")]
    [TestCase("""{ "anno": 2026, "mese": 7, "tipologiaFattura": "SECONDO SALDO", "nota": "x" }""", "almeno una", TestName = "400(idFatture assente)")]
    [TestCase("""{ "anno": 2026, "mese": 7, "tipologiaFattura": "SECONDO SALDO", "idFatture": [1001, 1001], "nota": "x" }""", "ripetute", TestName = "400(duplicati)")]
    [TestCase("""{ "anno": 2026, "mese": 7, "tipologiaFattura": "SECONDO SALDO", "idFatture": [0], "nota": "x" }""", "non valido", TestName = "400(id zero)")]
    [TestCase("""{ "mese": 7, "tipologiaFattura": "SECONDO SALDO", "idFatture": [1001], "nota": "x" }""", "Anno", TestName = "400(anno assente)")]
    [TestCase("""{ "anno": 2026, "mese": 13, "tipologiaFattura": "SECONDO SALDO", "idFatture": [1001], "nota": "x" }""", "Mese", TestName = "400(mese 13)")]
    [TestCase("""{ "anno": 2026, "mese": 7, "idFatture": [1001], "nota": "x" }""", "Tipologia", TestName = "400(tipologia assente)")]
    [TestCase("{}", "", TestName = "400(body vuoto)")]
    public async Task BodyNonValido_ShouldRispondere400_ConIlMotivo(string body, string frammentoAtteso)
    {
        var resp = await Genera(body);
        var testo = await resp.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain(frammentoAtteso).IgnoreCase, "Il 400 deve dire all'admin cosa manca.");
            Assert.That(FakeSynapseService.Instance.Chiamate, Is.Empty, "Nessuna pipeline avviata su body non valido.");
        });
    }

    [TestCase("""{ "anno": 2026, "mese": 7, "tipologiaFattura": "SECONDO SALDO", "idFatture": ["abc"], "nota": "x" }""", TestName = "400(id non numerico)")]
    [TestCase("""{ "anno": 2026, "mese": 7, "tipologiaFattura": "SECONDO SALDO", "idFatture": 1001, "nota": "x" }""", TestName = "400(idFatture scalare)")]
    [TestCase("""non-json""", TestName = "400(non JSON)")]
    [TestCase("", TestName = "400(body assente)")]
    public async Task BodyMalformato_ShouldRispondere400_Non500(string body)
    {
        var resp = await Genera(body);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // =============================================================================================
    // 404 / 409 / 202 / 500 (con DB e DDL di DATA)
    // =============================================================================================

    [Test]
    public async Task FatturaInesistente_ShouldRispondere404()
    {
        InvioSapTestData.SkipSeOggettiAssenti();

        var resp = await Genera(Body([999_999_999], nota: "inesistente"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(FakeSynapseService.Instance.Chiamate, Is.Empty);
    }

    [Test]
    public async Task FatturaDiEntePagoPA_ShouldRispondere404_FuoriPerimetro()
    {
        // L'ente PagoPA e' escluso dalla vista: per il client la fattura "non esiste" (stesso 404 delle inesistenti).
        InvioSapTestData.SkipSeOggettiAssenti();
        var id = InvioSapTestData.Scalar(
            "SELECT TOP 1 IdFattura FROM pfd.FattureTestata WHERE FkIdEnte = @e", ("@e", InvioSapTestData.EntePagoPA)) as long?;
        if (id is null) Assert.Ignore("Il seed non ha fatture dell'ente PagoPA.");

        var resp = await Genera(Body([id!.Value], nota: "pagopa"));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task PrimoInvio_ShouldRispondere202_EScrivereBatchDettaglioStoricoMessaggio()
    {
        InvioSapTestData.SkipSeOggettiAssenti();
        var ids = IdsDaInviare(2);

        var resp = await Genera(Body(ids, nota: "primo invio"));
        var json = await resp.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine(json);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), json);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var batchId = root.GetProperty("batchId").GetGuid();
        var nomeFile = root.GetProperty("nomeFileJson").GetString();

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("stato").GetString(), Is.EqualTo("IN_ELABORAZIONE"));
            Assert.That(nomeFile, Does.Match(@"^sap-\d{8}-\d{6}-[0-9a-f-]{36}\.json$"));
            Assert.That(nomeFile, Does.Contain(batchId.ToString("D")), "Il nome file porta il batchId: e' il contratto con DATA.");
            Assert.That(root.GetProperty("idFatture").EnumerateArray().Select(e => e.GetInt64()), Is.EquivalentTo(ids));

            // Registro + Dettaglio
            Assert.That(InvioSapTestData.StatoBatch(batchId), Is.EqualTo("IN_ELABORAZIONE"));
            Assert.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b", ("@b", batchId)),
                Is.EqualTo(ids.Count), "Una riga di dettaglio per fattura selezionata.");
            Assert.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b AND statoPrecedente = 'DA_INVIARE'", ("@b", batchId)),
                Is.EqualTo(ids.Count), "statoPrecedente = DA_INVIARE: e' cio' che si ripristina in caso di errore.");

            // Stato derivato e lock
            foreach (var id in ids) Assert.That(InvioSapTestData.StatoFattura(id), Is.EqualTo("IN_ELABORAZIONE"), $"fattura {id}");
            Assert.That(InvioSapTestData.BatchCheDetieneIlLock(), Is.EqualTo(batchId));

            // Storico ISAP e Messaggio (Hash = batchId, nessun link, stato elaborazione)
            Assert.That(InvioSapTestData.Scalar("SELECT COUNT(*) FROM pfw.Log WHERE DescrizioneEvento = 'ISAP' AND JsonTransazione LIKE @b", ("@b", $"%{batchId}%")),
                Is.GreaterThanOrEqualTo(1));
            Assert.That(InvioSapTestData.Scalar("SELECT COUNT(*) FROM pfd.Messaggi WHERE Hash = @h AND LinkDocumento IS NULL AND Stato = 1", ("@h", batchId.ToString())),
                Is.EqualTo(1), "Un messaggio 'generazione in corso', senza link (RF-14).");

            // Synapse chiamata una volta
            Assert.That(FakeSynapseService.Instance.Chiamate, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Reinvio_SenzaStorno_ShouldRispondere400_SenzaToccareNulla()
    {
        InvioSapTestData.SkipSeOggettiAssenti();
        var inviata = InvioSapTestData.Scalar(
            $"SELECT TOP 1 IdFattura FROM {InvioSapTestData.VistaDettaglio} WHERE StatoInvio = 'INVIATA' AND FkTipologiaFattura = @t AND AnnoRiferimento = @a AND MeseRiferimento = @m",
            ("@t", InvioSapTestData.Tipologia), ("@a", InvioSapTestData.Anno), ("@m", InvioSapTestData.Mese)) as long?;
        if (inviata is null) Assert.Ignore("Il seed non ha una fattura INVIATA nel gruppo di test.");

        var batchPrima = InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.Registro}");

        var resp = await Genera(Body([inviata!.Value], nota: "re-invio senza storno"));

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(InvioSapTestData.StatoFattura(inviata.Value), Is.EqualTo("INVIATA"), "Nessuna transizione.");
            Assert.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.Registro}"), Is.EqualTo(batchPrima), "Nessun batch creato.");
            Assert.That(FakeSynapseService.Instance.Chiamate, Is.Empty);
        });
    }

    [Test]
    public async Task SecondaRichiesta_ConBatchAttivo_ShouldRispondere409_LockAttivo()
    {
        InvioSapTestData.SkipSeOggettiAssenti();
        var ids = IdsDaInviare(3);

        var prima = await Genera(Body(ids.Take(1).ToList(), nota: "primo batch"));
        Assert.That(prima.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

        // Nuovo sottoinsieme, stesso admin: il lock e' globale (RF-10), non per fattura.
        var seconda = await Genera(Body(ids.Skip(1).ToList(), nota: "secondo batch"));
        var testo = await seconda.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(seconda.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(testo, Does.Contain("LOCK_ATTIVO"));
            Assert.That(FakeSynapseService.Instance.Chiamate, Has.Count.EqualTo(1), "La seconda non arriva a Synapse.");
        });
    }

    [Test]
    public async Task RichiesteConcorrenti_ShouldAccettarneUnaSola()
    {
        // Il cuore di US3: N admin premono insieme, una sola 202, le altre 409, un solo batch, un solo lock.
        InvioSapTestData.SkipSeOggettiAssenti();
        var ids = IdsDaInviare(1);
        FakeSynapseService.Instance.Ritardo = TimeSpan.FromMilliseconds(300);

        var risposte = await Task.WhenAll(Enumerable.Range(0, 6)
            .Select(i => Genera(Body(ids, nota: $"concorrente {i}"))));

        var codici = risposte.Select(r => (int)r.StatusCode).ToList();
        TestContext.Out.WriteLine(string.Join(",", codici));

        Assert.Multiple(() =>
        {
            Assert.That(codici.Count(c => c == 202), Is.EqualTo(1), "Esattamente una richiesta vince il lock.");
            Assert.That(codici.Count(c => c == 409), Is.EqualTo(5));
            Assert.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.Registro} WHERE idUtente = @u AND stato = 'IN_ELABORAZIONE'", ("@u", InvioSapTestData.UtenteTest)),
                Is.EqualTo(1));
            Assert.That(FakeSynapseService.Instance.Chiamate, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task CompletamentoDiData_ShouldPortareAInviata_ELiberareIlLock()
    {
        InvioSapTestData.SkipSeOggettiAssenti();
        var ids = IdsDaInviare(1);
        var batchId = await AvviaBatch(ids, "da completare");

        InvioSapTestData.ChiudiBatchComeData(batchId, "COMPLETATO");

        // La griglia riconcilia in lettura (nessuna callback): dopo averla letta il messaggio e' chiuso.
        var griglia = await Griglia();
        var stato = await StatoBatchViaApi(batchId);

        Assert.Multiple(() =>
        {
            Assert.That(griglia.StatusCode, Is.EqualTo(HttpStatusCode.OK).Or.EqualTo(HttpStatusCode.NotFound));
            Assert.That(InvioSapTestData.StatoFattura(ids[0]), Is.EqualTo("INVIATA"));
            Assert.That(InvioSapTestData.BatchCheDetieneIlLock(), Is.Null);
            Assert.That(stato.GetProperty("stato").GetString(), Is.EqualTo("COMPLETATO"));
            Assert.That(InvioSapTestData.Scalar("SELECT Stato FROM pfd.Messaggi WHERE Hash = @h", ("@h", batchId.ToString())),
                Is.EqualTo((short)2), "Messaggio chiuso (Completato) dalla riconciliazione.");
        });

        // E ora una nuova generazione e' di nuovo ammessa.
        var nuova = await Genera(Body(IdsDaInviare(1), nota: "dopo il completamento", storno: null));
        Assert.That((int)nuova.StatusCode, Is.EqualTo(202).Or.EqualTo(400),
            "Il lock e' libero: la risposta dipende dai dati (400 se la fattura ora e' INVIATA e manca lo storno), non dal lock.");
    }

    [Test]
    public async Task FallimentoDiData_ShouldRipristinareLoStatoPrecedente()
    {
        InvioSapTestData.SkipSeOggettiAssenti();
        var ids = IdsDaInviare(1);
        var batchId = await AvviaBatch(ids, "fallira'");

        InvioSapTestData.ChiudiBatchComeData(batchId, "FALLITO", errore: "ERRORE_DATA");
        await Griglia();

        Assert.Multiple(() =>
        {
            Assert.That(InvioSapTestData.StatoFattura(ids[0]), Is.EqualTo("DA_INVIARE"), "statoPrecedente ripristinato.");
            Assert.That(InvioSapTestData.BatchCheDetieneIlLock(), Is.Null);
        });
    }

    [Test]
    public async Task LockScaduto_ShouldEssereRiconciliato_ABatchFallitoPerTimeout()
    {
        InvioSapTestData.SkipSeOggettiAssenti();
        var ids = IdsDaInviare(1);
        var batchId = await AvviaBatch(ids, "si bloccherà");

        InvioSapTestData.ScadiLock(batchId);
        var stato = await StatoBatchViaApi(batchId);

        Assert.Multiple(() =>
        {
            Assert.That(stato.GetProperty("stato").GetString(), Is.EqualTo("FALLITO"));
            Assert.That(stato.GetProperty("errore").GetString(), Is.EqualTo("TIMEOUT"));
            Assert.That(InvioSapTestData.StatoFattura(ids[0]), Is.EqualTo("DA_INVIARE"));
            Assert.That(InvioSapTestData.BatchCheDetieneIlLock(), Is.Null, "Il lock scaduto e' stato liberato.");
        });
    }

    [Test]
    public async Task PipelineNonAvviata_ShouldRispondere500_EMarcareFallito_ELiberareIlLock()
    {
        InvioSapTestData.SkipSeOggettiAssenti();
        var ids = IdsDaInviare(1);
        FakeSynapseService.Instance.Esito = false;

        var resp = await Genera(Body(ids, nota: "synapse giu'"));

        var batch = InvioSapTestData.Scalar(
            $"SELECT TOP 1 batchId FROM {InvioSapTestData.Registro} WHERE idUtente = @u ORDER BY dataAvvio DESC", ("@u", InvioSapTestData.UtenteTest)) as Guid?;

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(batch, Is.Not.Null, "Il batch resta come traccia dell'errore.");
            Assert.That(InvioSapTestData.StatoBatch(batch!.Value), Is.EqualTo("FALLITO"));
            Assert.That(InvioSapTestData.StatoFattura(ids[0]), Is.EqualTo("DA_INVIARE"));
            Assert.That(InvioSapTestData.BatchCheDetieneIlLock(), Is.Null, "Altrimenti nessuno potrebbe riprovare.");
        });
    }

    [Test]
    public async Task BatchSconosciuto_ShouldRispondere404()
    {
        var resp = await _factory.CreateClientAs(Ruolo.ADMIN)
            .GetAsync(_factory.WithNonce($"{InvioSapTestData.RottaGenerazioni}/{Guid.NewGuid()}"));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    private static string Body(IReadOnlyList<long> ids, string nota, string? storno = null) => $$"""
        { "anno": {{InvioSapTestData.Anno}}, "mese": {{InvioSapTestData.Mese}}, "tipologiaFattura": "{{InvioSapTestData.Tipologia}}",
          "idFatture": [{{string.Join(",", ids)}}], "nota": "{{InvioSapTestData.MarcatoreNota}} {{nota}}",
          "numeroDocumentoStorno": {{(storno is null ? "null" : $"\"{storno}\"")}} }
        """;

    private static List<long> IdsDaInviare(int quante)
    {
        var ids = InvioSapTestData.IdFattureDaInviare(max: quante);
        if (ids.Count < quante)
            Assert.Ignore($"Servono {quante} fatture DA_INVIARE in {InvioSapTestData.Tipologia} {InvioSapTestData.Anno}/{InvioSapTestData.Mese}: il seed ne ha {ids.Count}.");
        return ids;
    }

    private async Task<HttpResponseMessage> Genera(string body)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.PutAsync(_factory.WithNonce(InvioSapTestData.RottaGenera),
            new StringContent(body, Encoding.UTF8, "application/json"));
        TestContext.Out.WriteLine($"genera -> {(int)resp.StatusCode}");
        return resp;
    }

    private async Task<Guid> AvviaBatch(IReadOnlyList<long> ids, string nota)
    {
        var resp = await Genera(Body(ids, nota));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), await resp.Content.ReadAsStringAsync());
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("batchId").GetGuid();
    }

    private Task<HttpResponseMessage> Griglia() =>
        _factory.CreateClientAs(Ruolo.ADMIN).PostAsync(_factory.WithNonce(InvioSapTestData.RottaGriglia),
            new StringContent($$"""{ "anno": {{InvioSapTestData.Anno}} }""", Encoding.UTF8, "application/json"));

    private async Task<JsonElement> StatoBatchViaApi(Guid batchId)
    {
        var resp = await _factory.CreateClientAs(Ruolo.ADMIN)
            .GetAsync(_factory.WithNonce($"{InvioSapTestData.RottaGenerazioni}/{batchId}"));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
