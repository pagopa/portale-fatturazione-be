using System.Text.Json;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>
/// Contratto in ingresso dei tre body di Invio a SAP, con le opzioni di ASP.NET Core
/// (<c>JsonSerializerDefaults.Web</c>). Sono i casi "adversarial di binding": cio' che il client puo'
/// mandare e che deve arrivare integro all'endpoint o degradare a un 400, mai a un 500.
/// </summary>
[Category("InvioSap")]
public class InvioSapRequestDeserializationTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    // =============================================================================================
    // Genera
    // =============================================================================================

    [Test]
    public void Genera_BodyCompleto_ShouldDeserializzare()
    {
        var json = """
        { "anno": 2026, "mese": 5, "tipologiaFattura": "PRIMO SALDO",
          "idFatture": [2001, 2002], "nota": "re-invio", "numeroDocumentoStorno": "NC-1" }
        """;

        var req = JsonSerializer.Deserialize<InvioSapGeneraRequest>(json, Web)!;

        Assert.Multiple(() =>
        {
            Assert.That(req.Anno, Is.EqualTo(2026));
            Assert.That(req.Mese, Is.EqualTo(5));
            Assert.That(req.TipologiaFattura, Is.EqualTo("PRIMO SALDO"));
            Assert.That(req.IdFatture, Is.EqualTo(new long[] { 2001, 2002 }));
            Assert.That(req.Nota, Is.EqualTo("re-invio"));
            Assert.That(req.NumeroDocumentoStorno, Is.EqualTo("NC-1"));
        });
    }

    [Test]
    public void Genera_IdFatturaOltreInt32_ShouldArrivareIntegro()
    {
        const long grande = 7770001234567L;
        var req = JsonSerializer.Deserialize<InvioSapGeneraRequest>($$"""{ "idFatture": [{{grande}}] }""", Web)!;

        Assert.That(req.IdFatture![0], Is.EqualTo(grande), "IdFattura e' BIGINT: con int qui ci sarebbe overflow.");
    }

    [Test]
    public void Genera_IdFattureComeStringhe_ShouldEssereAccettate()
    {
        // ASP.NET usa NumberHandling.AllowReadingFromString: alcuni client mandano i numeri come stringa.
        var req = JsonSerializer.Deserialize<InvioSapGeneraRequest>("""{ "idFatture": ["2001", "2002"] }""", Web)!;

        Assert.That(req.IdFatture, Is.EqualTo(new long[] { 2001, 2002 }));
    }

    [Test]
    public void Genera_MeseComeStringa_ShouldEssereAccettato()
    {
        var req = JsonSerializer.Deserialize<InvioSapGeneraRequest>("""{ "mese": "7", "anno": "2026" }""", Web)!;
        Assert.That(req.Mese, Is.EqualTo(7));
        Assert.That(req.Anno, Is.EqualTo(2026));
    }

    [Test]
    public void Genera_CampiAssenti_ShouldRestareNull_NonLanciare()
    {
        // La validazione e' in InvioSapRegole: il binding non deve trasformare un campo mancante in 500.
        var req = JsonSerializer.Deserialize<InvioSapGeneraRequest>("{}", Web)!;

        Assert.Multiple(() =>
        {
            Assert.That(req.Anno, Is.Null);
            Assert.That(req.Mese, Is.Null);
            Assert.That(req.IdFatture, Is.Null);
            Assert.That(req.Nota, Is.Null);
            Assert.That(req.NumeroDocumentoStorno, Is.Null);
        });
    }

    [Test]
    public void Genera_StornoNullEsplicito_ShouldRestareNull()
    {
        var req = JsonSerializer.Deserialize<InvioSapGeneraRequest>("""{ "numeroDocumentoStorno": null }""", Web)!;
        Assert.That(req.NumeroDocumentoStorno, Is.Null);
    }

    [Test]
    public void Genera_CampiSconosciuti_ShouldEssereIgnorati()
    {
        // Il vecchio contratto aveva "annoRiferimento"/"meseRiferimento": un client non aggiornato non rompe
        // il binding, semplicemente anno/mese restano null e la regola risponde 400.
        var req = JsonSerializer.Deserialize<InvioSapGeneraRequest>(
            """{ "annoRiferimento": 2026, "meseRiferimento": 5, "idFatture": [1] }""", Web)!;

        Assert.That(req.Anno, Is.Null);
        Assert.That(req.IdFatture, Is.EqualTo(new long[] { 1 }));
    }

    [TestCase("""{ "idFatture": [1.5] }""", TestName = "Genera_IdDecimale")]
    [TestCase("""{ "idFatture": ["abc"] }""", TestName = "Genera_IdNonNumerico")]
    [TestCase("""{ "idFatture": 2001 }""", TestName = "Genera_IdScalareInveceDiLista")]
    [TestCase("""{ "mese": 5.5 }""", TestName = "Genera_MeseDecimale")]
    [TestCase("""{ "idFatture": [99999999999999999999] }""", TestName = "Genera_IdOltreInt64")]
    public void Genera_TipiSbagliati_ShouldLanciareJsonException(string json)
    {
        // A runtime ASP.NET traduce JsonException in 400 (body non valido): e' il comportamento atteso,
        // non un 500. Qui si fissa che il tipo rifiuti il valore invece di troncarlo in silenzio.
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<InvioSapGeneraRequest>(json, Web));
    }

    [Test]
    public void Genera_NotaMoltoLunga_ShouldDeserializzare()
    {
        // La lunghezza massima la decide la DDL (nota su pfw.DettaglioInvioSAP), non il binding.
        var nota = new string('x', 100_000);
        var req = JsonSerializer.Deserialize<InvioSapGeneraRequest>($$"""{ "nota": "{{nota}}" }""", Web)!;
        Assert.That(req.Nota, Has.Length.EqualTo(100_000));
    }

    // =============================================================================================
    // Griglia
    // =============================================================================================

    [Test]
    public void Griglia_BodyCompleto_ShouldDeserializzare()
    {
        var json = """
        { "anno": 2026, "mesi": [5, 6], "tipologieFattura": ["PRIMO SALDO"], "statoInvio": "INVIATE", "soloReinviate": true }
        """;

        var req = JsonSerializer.Deserialize<InvioSapGrigliaRequest>(json, Web)!;

        Assert.Multiple(() =>
        {
            Assert.That(req.Anno, Is.EqualTo(2026));
            Assert.That(req.Mesi, Is.EqualTo(new[] { 5, 6 }));
            Assert.That(req.TipologieFattura, Is.EqualTo(new[] { "PRIMO SALDO" }));
            Assert.That(req.StatoInvio, Is.EqualTo("INVIATE"));
            Assert.That(req.SoloReinviate, Is.True);
        });
    }

    [Test]
    public void Griglia_AnnoNull_ShouldRestareNull()
    {
        // RF-15 lo rende obbligatorio, ma e' la regola a dirlo (400 con messaggio), non il binding.
        var req = JsonSerializer.Deserialize<InvioSapGrigliaRequest>("""{ "anno": null }""", Web)!;
        Assert.That(req.Anno, Is.Null);
    }

    [Test]
    public void Griglia_MesiComeStringhe_ShouldEssereAccettati()
    {
        var req = JsonSerializer.Deserialize<InvioSapGrigliaRequest>("""{ "anno": 2026, "mesi": ["5"] }""", Web)!;
        Assert.That(req.Mesi, Is.EqualTo(new[] { 5 }));
    }

    [Test]
    public void Griglia_SoloReinviateComeStringa_ShouldLanciare()
    {
        // I booleani NON hanno la lettura da stringa: "true" tra virgolette e' un body non valido (400).
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<InvioSapGrigliaRequest>("""{ "anno": 2026, "soloReinviate": "true" }""", Web));
    }

    [Test]
    public void Griglia_BodyVuoto_ShouldDeserializzareConTuttiNull()
    {
        var req = JsonSerializer.Deserialize<InvioSapGrigliaRequest>("{}", Web)!;
        Assert.That(req.Anno, Is.Null);
        Assert.That(req.Mesi, Is.Null);
        Assert.That(req.SoloReinviate, Is.Null);
    }

    // =============================================================================================
    // Dettaglio
    // =============================================================================================

    [Test]
    public void Dettaglio_BodyCompleto_ShouldDeserializzare()
    {
        var json = """
        { "annoRiferimento": 2026, "meseRiferimento": 5, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATA",
          "ragioneSociale": "Regione", "numeroFattura": "1111", "dataFattura": "2025-02-10", "page": 2, "pageSize": 25 }
        """;

        var req = JsonSerializer.Deserialize<InvioSapDettaglioRequest>(json, Web)!;

        Assert.Multiple(() =>
        {
            Assert.That(req.AnnoRiferimento, Is.EqualTo(2026));
            Assert.That(req.MeseRiferimento, Is.EqualTo(5));
            Assert.That(req.StatoInvio, Is.EqualTo("INVIATA"));
            Assert.That(req.RagioneSociale, Is.EqualTo("Regione"));
            Assert.That(req.NumeroFattura, Is.EqualTo("1111"));
            Assert.That(req.DataFattura, Is.EqualTo(new DateTime(2025, 2, 10)));
            Assert.That(req.Page, Is.EqualTo(2));
            Assert.That(req.PageSize, Is.EqualTo(25));
        });
    }

    [Test]
    public void Dettaglio_NumeroFatturaNumerico_ShouldLanciare()
    {
        // numeroFattura e' una stringa (e' il Progressivo, ma il FE lo tratta come testo): un numero nudo
        // non e' accettato da System.Text.Json per le stringhe. E' il motivo per cui il contratto lo
        // dichiara stringa: va detto al FE.
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<InvioSapDettaglioRequest>("""{ "numeroFattura": 1111 }""", Web));
    }

    [TestCase("""{ "dataFattura": "10/02/2025" }""", TestName = "Dettaglio_DataFormatoItaliano")]
    [TestCase("""{ "dataFattura": "non-una-data" }""", TestName = "Dettaglio_DataTesto")]
    public void Dettaglio_DataNonIso_ShouldLanciare(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<InvioSapDettaglioRequest>(json, Web));
    }
}
