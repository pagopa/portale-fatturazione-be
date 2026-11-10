using System.Text.Json;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>
/// "Seleziona tutto" del dettaglio paginato (be-invio-sap.md §3.3): con <c>allSelected = true</c> il Genera re-invia
/// tutte le fatture inviate del rigo tranne <c>idFattureEscluse</c>, e ignora <c>idFatture</c>; con
/// <c>allSelected</c> false o assente vale <c>idFatture</c> e le escluse vengono ignorate. Qui le regole pure e il
/// body; il filtro sul DB e' nei test HTTP (<c>InvioSapSelezionaTuttoHttpTests</c>).
/// </summary>
[Category("InvioSap")]
public class InvioSapSelezionaTuttoTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private const string Tipologia = "PRIMO SALDO";
    private const int Anno = 2093;
    private const int Mese = 1;
    private const string Utente = "admin@test";

    private static InvioSapFatturaCorrente Fattura(long id, bool? inviata = true, int anno = Anno, int mese = Mese, string tipologia = Tipologia)
        => new(id, anno, mese, tipologia, inviata);

    private static InvioSapRichiestaGenerazione Tutto(long[]? escluse = null, long[]? ids = null, string? storno = "NC-2093-0042",
        bool? allSelected = true) => new()
        {
            Anno = Anno, Mese = Mese, TipologiaFattura = Tipologia, AllSelected = allSelected,
            IdFatture = ids, IdFattureEscluse = escluse, Nota = "Re-invio di tutto il rigo", NumeroDocumentoStorno = storno
        };

    // =============================================================================================
    // Modalita'
    // =============================================================================================

    [Test]
    public void AllSelectedTrue_ShouldEssereSempre_ReInvio_AncheSenzaIdFatture()
    {
        // Non deve mai diventare un primo invio: senza storno la richiesta va respinta, non reinterpretata.
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.Modalita(Tutto()), Is.EqualTo(InvioSapModalita.ReInvio));
            Assert.That(InvioSapRegole.Modalita(Tutto(ids: [])), Is.EqualTo(InvioSapModalita.ReInvio));
            Assert.That(InvioSapRegole.SelezioneTotale(Tutto()), Is.True);
        });
    }

    [TestCase(false, TestName = "AllSelectedFalseOAssente_ShouldLasciareLaModalitaAIdFatture(allSelected: false)")]
    [TestCase(null, TestName = "AllSelectedFalseOAssente_ShouldLasciareLaModalitaAIdFatture(allSelected: assente)")]
    public void AllSelectedFalseOAssente_ShouldLasciareLaModalitaAIdFatture(bool? allSelected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.SelezioneTotale(Tutto(allSelected: allSelected)), Is.False);
            Assert.That(InvioSapRegole.Modalita(Tutto(allSelected: allSelected)), Is.EqualTo(InvioSapModalita.PrimoInvio),
                "Senza idFatture resta il primo invio del rigo, come prima.");
            Assert.That(InvioSapRegole.Modalita(Tutto(allSelected: allSelected, ids: [1])), Is.EqualTo(InvioSapModalita.ReInvio));
            Assert.That(InvioSapRegole.Modalita(Tutto(allSelected: allSelected, escluse: [1])), Is.EqualTo(InvioSapModalita.PrimoInvio),
                "Le escluse da sole non cambiano la modalita': senza seleziona tutto non vengono lette.");
        });
    }

    // =============================================================================================
    // Validazione formale
    // =============================================================================================

    [Test]
    public void Tutto_SenzaEscluse_ShouldEssereValido()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.ValidaFormale(Tutto(), Utente), Is.Empty);
            Assert.That(InvioSapRegole.ValidaFormale(Tutto(escluse: []), Utente), Is.Empty);
            Assert.That(InvioSapRegole.ValidaFormale(Tutto(escluse: [9502]), Utente), Is.Empty);
        });
    }

    [TestCase(null, TestName = "Tutto_SenzaStorno_ShouldEssereRifiutato(storno: null)")]
    [TestCase(" ", TestName = "Tutto_SenzaStorno_ShouldEssereRifiutato(storno: spazi)")]
    public void Tutto_SenzaStorno_ShouldEssereRifiutato(string? storno)
    {
        Assert.That(InvioSapRegole.ValidaFormale(Tutto(storno: storno), Utente), Is.EqualTo(new[] { InvioSapRegole.ErroreStornoMancante }));
    }

    [Test]
    public void Tutto_ShouldIgnorareIdFatture_AncheSeNonValide()
    {
        // Vuota, ripetuta, con id non validi o oltre il tetto: con allSelected = true non viene letta.
        var oltreIlTetto = Enumerable.Range(1, InvioSapRegole.MaxFatture + 1).Select(i => (long)i).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.ValidaFormale(Tutto(ids: []), Utente), Is.Empty);
            Assert.That(InvioSapRegole.ValidaFormale(Tutto(ids: [0, 0, -1]), Utente), Is.Empty);
            Assert.That(InvioSapRegole.ValidaFormale(Tutto(ids: oltreIlTetto), Utente), Is.Empty);
        });
    }

    [Test]
    public void SenzaSelezionaTutto_ShouldIgnorareLeEscluse_AncheSeNonValide()
    {
        var oltreIlTetto = Enumerable.Range(1, InvioSapRegole.MaxFatture + 1).Select(i => (long)i).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.ValidaFormale(Tutto(allSelected: false, ids: [9502], escluse: [0, 0, -1]), Utente), Is.Empty);
            Assert.That(InvioSapRegole.ValidaFormale(Tutto(allSelected: null, ids: [9502], escluse: oltreIlTetto), Utente), Is.Empty);
            Assert.That(InvioSapRegole.ValidaFormale(Tutto(allSelected: false, ids: [], escluse: [9502]), Utente),
                Is.EqualTo(new[] { InvioSapRegole.ErroreSelezioneVuota }), "Conta idFatture: vuota resta un errore.");
        });
    }

    [Test]
    public void Tutto_EscluseAlLimite_ShouldEssereValide_OltreRifiutate()
    {
        var mille = Enumerable.Range(1, InvioSapRegole.MaxFatture).Select(i => (long)i).ToArray();
        var milleUno = Enumerable.Range(1, InvioSapRegole.MaxFatture + 1).Select(i => (long)i).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.ValidaFormale(Tutto(escluse: mille), Utente), Is.Empty);
            Assert.That(InvioSapRegole.ValidaFormale(Tutto(escluse: milleUno), Utente), Is.EqualTo(new[] { InvioSapRegole.ErroreEsclusioniTroppe }));
        });
    }

    [TestCase(new long[] { 0 }, TestName = "Tutto_EscluseNonValide_ShouldEssereRifiutate(esclusa zero)")]
    [TestCase(new long[] { -5 }, TestName = "Tutto_EscluseNonValide_ShouldEssereRifiutate(esclusa negativa)")]
    [TestCase(new long[] { long.MinValue }, TestName = "Tutto_EscluseNonValide_ShouldEssereRifiutate(long.MinValue)")]
    public void Tutto_EscluseNonValide_ShouldEssereRifiutate(long[] escluse)
    {
        Assert.That(InvioSapRegole.ValidaFormale(Tutto(escluse: escluse), Utente), Is.EqualTo(new[] { InvioSapRegole.ErroreIdNonValido }));
    }

    [Test]
    public void Tutto_EscluseRipetute_ShouldEssereRifiutate()
    {
        Assert.That(InvioSapRegole.ValidaFormale(Tutto(escluse: [9502, 9502]), Utente), Is.EqualTo(new[] { InvioSapRegole.ErroreEsclusioniDuplicate }));
    }

    [Test]
    public void Tutto_EsclusaEnorme_ShouldEssereFormalmenteValida()
    {
        // long.MaxValue non e' un errore di forma: e' una fattura che non esiste, quindi 404 piu' avanti.
        Assert.That(InvioSapRegole.ValidaFormale(Tutto(escluse: [long.MaxValue]), Utente), Is.Empty);
    }

    // =============================================================================================
    // Escluse lette dalla vista dettaglio
    // =============================================================================================

    [Test]
    public void Escluse_NelGruppo_ShouldPassare_QualunqueSiaLoStato()
    {
        // Lo stato dell'esclusa non conta: una fattura non inviata non entrerebbe comunque nel re-invio.
        var esito = InvioSapRegole.ValidaEsclusioni(Tutto(escluse: [1, 2, 3]),
            [Fattura(1, inviata: true), Fattura(2, inviata: false), Fattura(3, inviata: null)]);
        Assert.That(esito, Is.Null);
    }

    [Test]
    public void Escluse_Nessuna_ShouldPassare()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.ValidaEsclusioni(Tutto(), []), Is.Null);
            Assert.That(InvioSapRegole.ValidaEsclusioni(Tutto(escluse: []), []), Is.Null);
        });
    }

    [Test]
    public void Esclusa_AssenteDallaVista_ShouldEssere404_ConLeSoleMancanti()
    {
        var esito = InvioSapRegole.ValidaEsclusioni(Tutto(escluse: [9502, 777]), [Fattura(9502)]);
        Assert.Multiple(() =>
        {
            Assert.That(esito!.Tipo, Is.EqualTo(InvioSapGeneraEsitoTipo.NonTrovate));
            Assert.That(esito.IdFattureCoinvolte, Is.EqualTo(new long[] { 777 }));
        });
    }

    [TestCase(2092, Mese, Tipologia, TestName = "Esclusa_DiUnAltroRigo_ShouldEssere400(anno)")]
    [TestCase(Anno, 2, Tipologia, TestName = "Esclusa_DiUnAltroRigo_ShouldEssere400(mese)")]
    [TestCase(Anno, Mese, "SECONDO SALDO", TestName = "Esclusa_DiUnAltroRigo_ShouldEssere400(tipologia)")]
    public void Esclusa_DiUnAltroRigo_ShouldEssere400(int anno, int mese, string tipologia)
    {
        // Un'esclusa che non e' del rigo e' un errore del chiamante: ignorarla vorrebbe dire inviare piu' del voluto.
        var esito = InvioSapRegole.ValidaEsclusioni(Tutto(escluse: [9502, 9503]),
            [Fattura(9502), Fattura(9503, anno: anno, mese: mese, tipologia: tipologia)]);
        Assert.Multiple(() =>
        {
            Assert.That(esito!.Tipo, Is.EqualTo(InvioSapGeneraEsitoTipo.NonValida));
            Assert.That(esito.Errori, Is.EqualTo(new[] { InvioSapRegole.ErroreFuoriGruppo }));
            Assert.That(esito.IdFattureCoinvolte, Is.EqualTo(new long[] { 9503 }));
        });
    }

    [Test]
    public void Escluse_Precedenza_NonTrovate_PrimaDi_FuoriGruppo()
    {
        var esito = InvioSapRegole.ValidaEsclusioni(Tutto(escluse: [1, 2]), [Fattura(1, anno: 2092)]);
        Assert.That(esito!.Tipo, Is.EqualTo(InvioSapGeneraEsitoTipo.NonTrovate));
    }

    // =============================================================================================
    // Body
    // =============================================================================================

    [Test]
    public void Body_ConSelezionaTutto_ShouldDeserializzare()
    {
        const string json = """
            { "anno": 2026, "mese": 5, "tipologiaFattura": "PRIMO SALDO", "allSelected": true,
              "idFattureEscluse": [2001, 2002], "nota": "n", "numeroDocumentoStorno": "NC-1" }
            """;
        var req = JsonSerializer.Deserialize<InvioSapGeneraRequest>(json, Web)!;
        Assert.Multiple(() =>
        {
            Assert.That(req.AllSelected, Is.True);
            Assert.That(req.IdFattureEscluse, Is.EqualTo(new long[] { 2001, 2002 }));
            Assert.That(req.IdFatture, Is.Null);
        });
    }

    [Test]
    public void Body_NomiDeiCampi_ShouldNonDipendereDalleMaiuscole()
    {
        // Il FE ha proposto "AllSelected": le opzioni dell'API accettano entrambe le forme.
        var req = JsonSerializer.Deserialize<InvioSapGeneraRequest>("""{ "AllSelected": true, "IdFattureEscluse": [7] }""", Web)!;
        Assert.Multiple(() =>
        {
            Assert.That(req.AllSelected, Is.True);
            Assert.That(req.IdFattureEscluse, Is.EqualTo(new long[] { 7 }));
        });
    }

    [Test]
    public void Body_SenzaINuoviCampi_ShouldRestareComePrima()
    {
        var req = JsonSerializer.Deserialize<InvioSapGeneraRequest>("""{ "idFatture": [1] }""", Web)!;
        Assert.Multiple(() =>
        {
            Assert.That(req.AllSelected, Is.Null);
            Assert.That(req.IdFattureEscluse, Is.Null);
            Assert.That(req.IdFatture, Is.EqualTo(new long[] { 1 }));
        });
    }

    [Test]
    public void Body_AllSelectedNullEsplicito_ShouldValereComeAssente()
    {
        var req = JsonSerializer.Deserialize<InvioSapGeneraRequest>("""{ "allSelected": null, "idFattureEscluse": null }""", Web)!;
        Assert.Multiple(() =>
        {
            Assert.That(req.AllSelected, Is.Null);
            Assert.That(req.IdFattureEscluse, Is.Null);
        });
    }

    [Test]
    public void Body_EsclusaOltreInt32_ShouldArrivareIntegra()
    {
        const long grande = 3_000_000_000;
        var req = JsonSerializer.Deserialize<InvioSapGeneraRequest>($$"""{ "idFattureEscluse": [{{grande}}] }""", Web)!;
        Assert.That(req.IdFattureEscluse![0], Is.EqualTo(grande));
    }

    [TestCase("""{ "allSelected": "true" }""", TestName = "Body_TipiSbagliati_ShouldLanciareJsonException(allSelected come stringa)")]
    [TestCase("""{ "allSelected": 1 }""", TestName = "Body_TipiSbagliati_ShouldLanciareJsonException(allSelected come numero)")]
    [TestCase("""{ "allSelected": [true] }""", TestName = "Body_TipiSbagliati_ShouldLanciareJsonException(allSelected come lista)")]
    [TestCase("""{ "idFattureEscluse": 2001 }""", TestName = "Body_TipiSbagliati_ShouldLanciareJsonException(escluse scalare)")]
    [TestCase("""{ "idFattureEscluse": ["abc"] }""", TestName = "Body_TipiSbagliati_ShouldLanciareJsonException(esclusa non numerica)")]
    [TestCase("""{ "idFattureEscluse": [1.5] }""", TestName = "Body_TipiSbagliati_ShouldLanciareJsonException(esclusa decimale)")]
    public void Body_TipiSbagliati_ShouldLanciareJsonException(string json)
    {
        // Nell'API diventa il 500 di binding comune a tutte le rotte (caratterizzato nei test HTTP).
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<InvioSapGeneraRequest>(json, Web));
    }
}
