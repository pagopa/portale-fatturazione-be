using System.Text.Json;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>
/// Regole pure del re-invio (<c>PUT api/fatture/invio-sap/reinvia</c>): un solo rigo per chiamata, e su ogni fattura
/// <c>idFattura</c>, <c>nota</c> e <c>numeroDocumentoStorno</c> obbligatori. Piu' il contratto in ingresso (binding,
/// mapping) e il JSON che la scrittura passa a <c>OPENJSON</c>.
/// </summary>
[Category("InvioSap")]
public class InvioSapRegoleReinvioTests
{
    private const string Tipologia = "PRIMO SALDO";
    private const int Anno = 2093;
    private const int Mese = 1;
    private const string Utente = "admin@test";
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static InvioSapFatturaDaReinviare Fattura(long? id, string? nota = "Re-invio", string? storno = "NC-1") =>
        new() { IdFattura = id, Nota = nota, NumeroDocumentoStorno = storno };

    private static InvioSapRichiestaReinvio Richiesta(params InvioSapFatturaDaReinviare?[] fatture) => new()
    {
        Anno = Anno, Mese = Mese, TipologiaFattura = Tipologia, Fatture = fatture
    };

    private static IReadOnlyList<string> Valida(InvioSapRichiestaReinvio r, string? utente = Utente) =>
        InvioSapRegoleReinvio.ValidaFormale(r, utente);

    // =============================================================================================
    // Validazione formale
    // =============================================================================================

    [Test]
    public void RichiestaCompleta_ShouldNonAvereErrori()
    {
        var errori = Valida(Richiesta(Fattura(9502, "Nota A", "NC-A"), Fattura(9503, "Nota B", "NC-B")));
        Assert.That(errori, Is.Empty);
    }

    [Test]
    public void RigoMancante_ShouldDareUnErrorePerCampo()
    {
        var errori = Valida(new InvioSapRichiestaReinvio { Fatture = [Fattura(1)] });
        Assert.That(errori, Is.EquivalentTo(new[] { InvioSapRegole.ErroreAnno, InvioSapRegole.ErroreMese, InvioSapRegole.ErroreTipologia }));
    }

    [TestCase(1999)]
    [TestCase(10000)]
    public void AnnoFuoriIntervallo_ShouldEssereErrore(int anno)
    {
        var r = new InvioSapRichiestaReinvio { Anno = anno, Mese = Mese, TipologiaFattura = Tipologia, Fatture = [Fattura(1)] };
        Assert.That(Valida(r), Does.Contain(InvioSapRegole.ErroreAnno));
    }

    [TestCase(0)]
    [TestCase(13)]
    public void MeseFuoriIntervallo_ShouldEssereErrore(int mese)
    {
        var r = new InvioSapRichiestaReinvio { Anno = Anno, Mese = mese, TipologiaFattura = Tipologia, Fatture = [Fattura(1)] };
        Assert.That(Valida(r), Does.Contain(InvioSapRegole.ErroreMese));
    }

    [TestCase(null)]
    [TestCase("   ")]
    public void TipologiaVuota_ShouldEssereErrore(string? tipologia)
    {
        var r = new InvioSapRichiestaReinvio { Anno = Anno, Mese = Mese, TipologiaFattura = tipologia, Fatture = [Fattura(1)] };
        Assert.That(Valida(r), Does.Contain(InvioSapRegole.ErroreTipologia));
    }

    [TestCase(null)]
    [TestCase("")]
    public void UtenteAssente_ShouldEssereErrore(string? utente)
    {
        Assert.That(Valida(Richiesta(Fattura(1)), utente), Does.Contain(InvioSapRegole.ErroreUtente));
    }

    [Test]
    public void UtenteTroppoLungo_ShouldEssereErrore()
    {
        Assert.That(Valida(Richiesta(Fattura(1)), new string('u', InvioSapRegole.MaxIdUtente + 1)), Does.Contain(InvioSapRegole.ErroreUtente));
    }

    [Test]
    public void FattureAssentiOVuote_ShouldEssereSelezioneVuota()
    {
        var assenti = new InvioSapRichiestaReinvio { Anno = Anno, Mese = Mese, TipologiaFattura = Tipologia };
        Assert.Multiple(() =>
        {
            Assert.That(Valida(assenti), Is.EqualTo(new[] { InvioSapRegole.ErroreSelezioneVuota }));
            Assert.That(Valida(Richiesta()), Is.EqualTo(new[] { InvioSapRegole.ErroreSelezioneVuota }));
        });
    }

    [Test]
    public void MilleFatture_ShouldPassare_MilleUno_No()
    {
        var mille = Enumerable.Range(1, InvioSapRegole.MaxFatture).Select(i => Fattura(i)).ToArray();
        var milleUno = Enumerable.Range(1, InvioSapRegole.MaxFatture + 1).Select(i => Fattura(i)).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(Valida(Richiesta(mille)), Is.Empty);
            Assert.That(Valida(Richiesta(milleUno)), Does.Contain(InvioSapRegole.ErroreSelezioneTroppoGrande));
        });
    }

    [Test]
    public void ElementoNull_ShouldEssereFatturaNonValida()
    {
        Assert.That(Valida(Richiesta(Fattura(1), null)), Does.Contain(InvioSapRegoleReinvio.ErroreFatturaNonValida));
    }

    [TestCase(null)]
    [TestCase(0L)]
    [TestCase(-5L)]
    public void IdNonValido_ShouldEssereErrore(long? id)
    {
        Assert.That(Valida(Richiesta(Fattura(id))), Does.Contain(InvioSapRegole.ErroreIdNonValido));
    }

    [Test]
    public void IdRipetuto_ShouldEssereErrore()
    {
        Assert.That(Valida(Richiesta(Fattura(9502, "a", "NC-1"), Fattura(9502, "b", "NC-2"))), Does.Contain(InvioSapRegole.ErroreSelezioneDuplicata));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  \t ")]
    public void NotaMancante_ShouldIndicareLeFattureCoinvolte(string? nota)
    {
        var errori = Valida(Richiesta(Fattura(9502), Fattura(9503, nota), Fattura(9504, nota)));
        Assert.That(errori, Is.EqualTo(new[] { $"{InvioSapRegoleReinvio.ErroreNotaMancante} (fatture: 9503, 9504)" }));
    }

    [Test]
    public void NotaDi2000Caratteri_ShouldPassare_2001_No()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Valida(Richiesta(Fattura(1, new string('n', InvioSapRegole.MaxNota)))), Is.Empty);
            Assert.That(Valida(Richiesta(Fattura(1, new string('n', InvioSapRegole.MaxNota + 1)))),
                Is.EqualTo(new[] { $"{InvioSapRegoleReinvio.ErroreNotaLunga} (fatture: 1)" }));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void StornoMancante_ShouldIndicareLeFattureCoinvolte(string? storno)
    {
        var errori = Valida(Richiesta(Fattura(9502, storno: storno), Fattura(9503)));
        Assert.That(errori, Is.EqualTo(new[] { $"{InvioSapRegoleReinvio.ErroreStornoMancante} (fatture: 9502)" }));
    }

    [Test]
    public void StornoDi255Caratteri_ShouldPassare_256_No()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Valida(Richiesta(Fattura(1, storno: new string('s', InvioSapRegole.MaxStorno)))), Is.Empty);
            Assert.That(Valida(Richiesta(Fattura(1, storno: new string('s', InvioSapRegole.MaxStorno + 1)))),
                Is.EqualTo(new[] { $"{InvioSapRegoleReinvio.ErroreStornoLungo} (fatture: 1)" }));
        });
    }

    [Test]
    public void StessoStornoSuPiuFatture_ShouldEssereAmmesso()
    {
        // Nessuna regola sull'unicita' del documento di storno: decisione aperta, oggi ammesso.
        Assert.That(Valida(Richiesta(Fattura(9502, storno: "NC-1"), Fattura(9503, storno: "NC-1"))), Is.Empty);
    }

    [Test]
    public void NotaEStornoMancantiSenzaId_ShouldDareIlMessaggioSenzaElenco()
    {
        var errori = Valida(Richiesta(Fattura(null, null, null)));
        Assert.That(errori, Is.EquivalentTo(new[]
        {
            InvioSapRegole.ErroreIdNonValido, InvioSapRegoleReinvio.ErroreNotaMancante, InvioSapRegoleReinvio.ErroreStornoMancante
        }));
    }

    [Test]
    public void PiuErrori_ShouldEssereRiportatiTuttiInsieme()
    {
        var r = new InvioSapRichiestaReinvio { Anno = Anno, Mese = 0, TipologiaFattura = Tipologia, Fatture = [Fattura(7, null, null)] };
        Assert.That(Valida(r), Has.Count.EqualTo(3), "mese, nota e storno in un solo 400");
    }

    // =============================================================================================
    // Riuso delle regole del Genera sulla selezione
    // =============================================================================================

    [Test]
    public void ComeGenerazione_ShouldEssereUnReInvioDelleStesseFatture()
    {
        var g = Richiesta(Fattura(9502), Fattura(9503)).ComeGenerazione();
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.Modalita(g), Is.EqualTo(InvioSapModalita.ReInvio));
            Assert.That(g.IdFatture, Is.EqualTo(new long[] { 9502, 9503 }));
            Assert.That(g.AllSelected, Is.Null, "Il re-invio non ha il seleziona tutto.");
        });
    }

    [Test]
    public void ComeGenerazione_FatturaNonInviata_ShouldEssere400DelGenera()
    {
        var g = Richiesta(Fattura(9502), Fattura(9505)).ComeGenerazione();
        var correnti = new[]
        {
            new InvioSapFatturaCorrente(9502, Anno, Mese, Tipologia, true),
            new InvioSapFatturaCorrente(9505, Anno, Mese, Tipologia, false)
        };
        var esito = InvioSapRegole.ValidaSelezione(g, correnti);
        Assert.Multiple(() =>
        {
            Assert.That(esito!.Tipo, Is.EqualTo(InvioSapGeneraEsitoTipo.NonValida));
            Assert.That(esito.Errori, Is.EqualTo(new[] { InvioSapRegole.ErroreNonInviate }));
            Assert.That(esito.IdFattureCoinvolte, Is.EqualTo(new long[] { 9505 }));
        });
    }

    [Test]
    public void ComeGenerazione_FatturaInesistente_ShouldEssere404()
    {
        var esito = InvioSapRegole.ValidaSelezione(Richiesta(Fattura(1)).ComeGenerazione(), []);
        Assert.That(esito!.Tipo, Is.EqualTo(InvioSapGeneraEsitoTipo.NonTrovate));
    }

    // =============================================================================================
    // Contratto in ingresso e mapping
    // =============================================================================================

    [Test]
    public void Body_ShouldDeserializzareEMappare()
    {
        var json = """
        { "anno": 2026, "mese": 9, "tipologiaFattura": "PRIMO SALDO",
          "fatture": [ { "idFattura": 7770001234567, "nota": "nota", "numeroDocumentoStorno": "NC-2026-0012" } ] }
        """;
        var req = JsonSerializer.Deserialize<InvioSapReinviaRequest>(json, Web)!;
        var cmd = req.Map(new PortaleFatture.BE.Core.Auth.AuthenticationInfo());

        Assert.Multiple(() =>
        {
            Assert.That(cmd.Anno, Is.EqualTo(2026));
            Assert.That(cmd.Mese, Is.EqualTo(9));
            Assert.That(cmd.TipologiaFattura, Is.EqualTo("PRIMO SALDO"));
            Assert.That(cmd.Fatture![0]!.IdFattura, Is.EqualTo(7770001234567L), "BIGINT: oltre Int32 arriva integro.");
            Assert.That(cmd.Fatture[0]!.Nota, Is.EqualTo("nota"));
            Assert.That(cmd.Fatture[0]!.NumeroDocumentoStorno, Is.EqualTo("NC-2026-0012"));
        });
    }

    [Test]
    public void BodyNullOElementoNull_ShouldMappareSenzaEccezioni()
    {
        var auth = new PortaleFatture.BE.Core.Auth.AuthenticationInfo();
        var daNull = ((InvioSapReinviaRequest?)null).Map(auth);
        var conNull = JsonSerializer.Deserialize<InvioSapReinviaRequest>("""{ "fatture": [ null ] }""", Web)!.Map(auth);
        Assert.Multiple(() =>
        {
            Assert.That(daNull.Fatture, Is.Null);
            Assert.That(conNull.Fatture, Has.Count.EqualTo(1));
            Assert.That(conNull.Fatture![0], Is.Null);
            Assert.That(InvioSapRegoleReinvio.ValidaFormale(conNull.ToRichiesta(), Utente), Does.Contain(InvioSapRegoleReinvio.ErroreFatturaNonValida));
        });
    }

    [Test]
    public void CampiDelGeneraNelBody_ShouldEssereIgnorati()
    {
        // statoInvio, idFatture, allSelected non fanno parte del contratto: non devono cambiare la selezione.
        var json = """
        { "anno": 2026, "mese": 9, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "DA_INVIARE",
          "allSelected": true, "idFatture": [1, 2, 3],
          "fatture": [ { "idFattura": 9, "nota": "n", "numeroDocumentoStorno": "s" } ] }
        """;
        var cmd = JsonSerializer.Deserialize<InvioSapReinviaRequest>(json, Web)!.Map(new PortaleFatture.BE.Core.Auth.AuthenticationInfo());
        Assert.That(cmd.ToRichiesta().ComeGenerazione().IdFatture, Is.EqualTo(new long[] { 9 }));
    }

    // =============================================================================================
    // JSON passato a OPENJSON
    // =============================================================================================

    [Test]
    public void Serializza_ShouldProdurreUnArrayConINomiLettiDaOpenJson()
    {
        var nota = new InvioSapNota(new DateTime(2026, 11, 17, 10, 0, 0, DateTimeKind.Utc), "Testo \"con apici\"\na capo 🚀", "GeneraJsonFatture").ToJson();
        var json = InvioSapDettaglioReinvioCommandPersistence.Serializza([new InvioSapRigaReinvio(9502, "NC-'1'-ü", nota)]);

        using var doc = JsonDocument.Parse(json);
        var riga = doc.RootElement[0];
        Assert.Multiple(() =>
        {
            Assert.That(doc.RootElement.GetArrayLength(), Is.EqualTo(1));
            Assert.That(riga.GetProperty("idFattura").GetInt64(), Is.EqualTo(9502));
            Assert.That(riga.GetProperty("numeroDocumentoStorno").GetString(), Is.EqualTo("NC-'1'-ü"));
            Assert.That(riga.GetProperty("nota").GetString(), Is.EqualTo(nota), "La nota viaggia come stringa: la colonna riceve il JSON di DATA intatto.");
            Assert.That(InvioSapNota.ParseNoteInvio($"[{{\"Nota\":{nota}}}]").Single().Testo, Is.EqualTo("Testo \"con apici\"\na capo 🚀"));
        });
    }
}
