using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>
/// Unit test PURI (nessun DB) sulle regole di <c>PUT api/fatture/invio-sap/genera</c> (PF-735 RF-6, RF-8,
/// RF-10, RF-16). La matrice completa, con la corrispondenza fra caso e livello di automazione, e' in
/// <c>coverage/invio-sap-matrice-test.md</c> del repo di contesto (sezione "Genera").
///
/// Ogni caso costruisce una richiesta e lo stato corrente delle fatture (quello che a runtime arriva
/// dalla vista) e asserisce esito, messaggio e fatture coinvolte. La precedenza degli esiti e' parte
/// del contratto: formale (400) → non trovate (404) → fuori gruppo (400) → in elaborazione (409) →
/// storno mancante (400).
/// </summary>
[Category("InvioSap")]
public class InvioSapRegoleGenerazioneTests
{
    private const string Tipologia = "PRIMO SALDO";
    private const int Anno = 2026;
    private const int Mese = 5;

    private static InvioSapFatturaCorrente DaInviare(long id) => new(id, Anno, Mese, Tipologia, StatoInvioSap.DA_INVIARE);
    private static InvioSapFatturaCorrente Inviata(long id) => new(id, Anno, Mese, Tipologia, StatoInvioSap.INVIATA);
    private static InvioSapFatturaCorrente InElaborazione(long id) => new(id, Anno, Mese, Tipologia, StatoInvioSap.IN_ELABORAZIONE);

    private static InvioSapRichiestaGenerazione Richiesta(
        long[]? idFatture = null, string? nota = "Invio di maggio", string? storno = null,
        int? anno = Anno, int? mese = Mese, string? tipologia = Tipologia) => new()
        {
            Anno = anno, Mese = mese, TipologiaFattura = tipologia,
            IdFatture = idFatture ?? [2001, 2002], Nota = nota, NumeroDocumentoStorno = storno
        };

    // =============================================================================================
    // Percorso felice
    // =============================================================================================

    [Test]
    public void PrimoInvio_TutteDaInviare_SenzaStorno_ShouldEssereOk()
    {
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(), [DaInviare(2001), DaInviare(2002)]);

        Assert.Multiple(() =>
        {
            Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.Ok));
            Assert.That(esito.Valida, Is.True);
            Assert.That(esito.RichiedeStorno, Is.False, "Solo DA_INVIARE: non e' un re-invio.");
            Assert.That(esito.Errori, Is.Empty);
        });
    }

    [Test]
    public void Reinvio_TutteInviate_ConStorno_ShouldEssereOk_ERichiedeStorno()
    {
        var esito = InvioSapRegole.ValidaGenerazione(
            Richiesta(storno: "NC-2026-0042"), [Inviata(2001), Inviata(2002)]);

        Assert.Multiple(() =>
        {
            Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.Ok));
            Assert.That(esito.RichiedeStorno, Is.True);
        });
    }

    [Test]
    public void Misto_DaInviareEInviata_ConStorno_ShouldEssereOk()
    {
        // RF-16: basta UNA inviata perche' lo storno serva; con lo storno il misto e' ammesso.
        var esito = InvioSapRegole.ValidaGenerazione(
            Richiesta(storno: "NC-1"), [DaInviare(2001), Inviata(2002)]);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.Ok));
        Assert.That(esito.RichiedeStorno, Is.True);
    }

    [Test]
    public void StornoFornitoMaNonNecessario_ShouldEssereOk_EIgnorato()
    {
        var esito = InvioSapRegole.ValidaGenerazione(
            Richiesta(storno: "NC-inutile"), [DaInviare(2001), DaInviare(2002)]);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.Ok));
        Assert.That(esito.RichiedeStorno, Is.False, "Lo storno non trasforma un primo invio in re-invio.");
    }

    [Test]
    public void TipologiaConSpaziEMinuscole_ShouldEssereOk()
    {
        // Il confronto col gruppo e' case-insensitive e ignora gli spazi: il FE manda il valore del rigo.
        var esito = InvioSapRegole.ValidaGenerazione(
            Richiesta(tipologia: "  primo saldo "), [DaInviare(2001), DaInviare(2002)]);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.Ok));
    }

    [Test]
    public void UnaSolaFattura_ShouldEssereOk()
    {
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(idFatture: [2001]), [DaInviare(2001)]);
        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.Ok));
    }

    // =============================================================================================
    // 400 formale: matrice sui campi del body
    // =============================================================================================

    [TestCase(null, TestName = "Nota(null)")]
    [TestCase("", TestName = "Nota(vuota)")]
    [TestCase("   ", TestName = "Nota(solo spazi)")]
    [TestCase("\t\n", TestName = "Nota(solo whitespace)")]
    public void NotaMancante_ShouldEssereNonValida(string? nota)
    {
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(nota: nota), [DaInviare(2001), DaInviare(2002)]);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonValida));
        Assert.That(esito.Errori, Does.Contain(InvioSapRegole.ErroreNota));
    }

    [Test]
    public void IdFattureNull_ShouldEssereNonValida()
    {
        var richiesta = new InvioSapRichiestaGenerazione
        { Anno = Anno, Mese = Mese, TipologiaFattura = Tipologia, IdFatture = null, Nota = "n" };

        var esito = InvioSapRegole.ValidaGenerazione(richiesta, [DaInviare(2001)]);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonValida));
        Assert.That(esito.Errori, Does.Contain(InvioSapRegole.ErroreSelezioneVuota));
    }

    [Test]
    public void IdFattureVuota_ShouldEssereNonValida()
    {
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(idFatture: []), [DaInviare(2001)]);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonValida));
        Assert.That(esito.Errori, Does.Contain(InvioSapRegole.ErroreSelezioneVuota));
    }

    [Test]
    public void IdFattureDuplicate_ShouldEssereNonValida()
    {
        // Un id ripetuto finirebbe due volte nel Dettaglio e violerebbe l'univocita' (batchId, idFattura).
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(idFatture: [2001, 2001]), [DaInviare(2001)]);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonValida));
        Assert.That(esito.Errori, Does.Contain(InvioSapRegole.ErroreSelezioneDuplicata));
    }

    [TestCase(0L, TestName = "IdFattura(zero)")]
    [TestCase(-1L, TestName = "IdFattura(negativo)")]
    [TestCase(long.MinValue, TestName = "IdFattura(long.MinValue)")]
    public void IdFatturaNonPositivo_ShouldEssereNonValida(long id)
    {
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(idFatture: [id]), [DaInviare(2001)]);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonValida));
        Assert.That(esito.Errori, Does.Contain(InvioSapRegole.ErroreIdNonValido));
    }

    [Test]
    public void IdFatturaOltreInt32_ShouldEssereAccettato()
    {
        // IdFattura e' BIGINT: un id oltre int.MaxValue e' legittimo (stessa lezione di Gestione Fatture).
        const long grande = 7770001234567L;
        var esito = InvioSapRegole.ValidaGenerazione(
            Richiesta(idFatture: [grande]), [DaInviare(grande)]);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.Ok));
    }

    [TestCase(null, TestName = "Anno(null)")]
    [TestCase(0, TestName = "Anno(zero)")]
    [TestCase(1999, TestName = "Anno(sotto il minimo)")]
    [TestCase(-2026, TestName = "Anno(negativo)")]
    public void AnnoNonValido_ShouldEssereNonValida(int? anno)
    {
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(anno: anno), [DaInviare(2001), DaInviare(2002)]);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonValida));
        Assert.That(esito.Errori, Does.Contain(InvioSapRegole.ErroreAnno));
    }

    [TestCase(null, TestName = "Mese(null)")]
    [TestCase(0, TestName = "Mese(zero)")]
    [TestCase(13, TestName = "Mese(13)")]
    [TestCase(-5, TestName = "Mese(negativo)")]
    public void MeseNonValido_ShouldEssereNonValida(int? mese)
    {
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(mese: mese), [DaInviare(2001), DaInviare(2002)]);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonValida));
        Assert.That(esito.Errori, Does.Contain(InvioSapRegole.ErroreMese));
    }

    [TestCase(null, TestName = "Tipologia(null)")]
    [TestCase("", TestName = "Tipologia(vuota)")]
    [TestCase("  ", TestName = "Tipologia(spazi)")]
    public void TipologiaMancante_ShouldEssereNonValida(string? tipologia)
    {
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(tipologia: tipologia), [DaInviare(2001), DaInviare(2002)]);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonValida));
        Assert.That(esito.Errori, Does.Contain(InvioSapRegole.ErroreTipologia));
    }

    [Test]
    public void PiuErroriFormali_ShouldEssereTuttiRiportati()
    {
        // L'admin deve vedere tutto cio' che manca in un colpo solo, non un errore per tentativo.
        var richiesta = new InvioSapRichiestaGenerazione { Anno = null, Mese = 0, TipologiaFattura = "", IdFatture = [], Nota = " " };

        var esito = InvioSapRegole.ValidaGenerazione(richiesta, []);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonValida));
        Assert.That(esito.Errori, Is.EquivalentTo(new[]
        {
            InvioSapRegole.ErroreAnno, InvioSapRegole.ErroreMese, InvioSapRegole.ErroreTipologia,
            InvioSapRegole.ErroreSelezioneVuota, InvioSapRegole.ErroreNota
        }));
    }

    [Test]
    public void ErroreFormale_ShouldPrecedere_NonTrovate()
    {
        // Nota mancante E fattura inesistente: vince il 400, il client corregge prima il body.
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(nota: null, idFatture: [9999]), [DaInviare(2001)]);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonValida));
    }

    // =============================================================================================
    // 404 non trovate
    // =============================================================================================

    [Test]
    public void FatturaInesistente_ShouldEssereNonTrovata_ConIdIndicato()
    {
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(idFatture: [2001, 9999]), [DaInviare(2001)]);

        Assert.Multiple(() =>
        {
            Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonTrovate));
            Assert.That(esito.IdFattureCoinvolte, Is.EqualTo(new long[] { 9999 }));
            Assert.That(esito.Errori, Does.Contain(InvioSapRegole.ErroreNonTrovate));
        });
    }

    [Test]
    public void StatoCorrenteNull_ShouldTrattarloComeVuoto_ENonTrovare()
    {
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(), null);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonTrovate));
        Assert.That(esito.IdFattureCoinvolte, Is.EquivalentTo(new long[] { 2001, 2002 }));
    }

    [Test]
    public void NonTrovate_ShouldPrecedere_InElaborazione()
    {
        // Una inesistente e una in elaborazione: 404, perche' la selezione contiene un id che non c'e'.
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(idFatture: [2001, 9999]), [InElaborazione(2001)]);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonTrovate));
    }

    // =============================================================================================
    // 400 fuori gruppo (RF-6)
    // =============================================================================================

    [Test]
    public void FatturaDiAltroMese_ShouldEssereNonValida_FuoriGruppo()
    {
        var altroMese = new InvioSapFatturaCorrente(2002, Anno, Mese + 1, Tipologia, StatoInvioSap.DA_INVIARE);

        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(), [DaInviare(2001), altroMese]);

        Assert.Multiple(() =>
        {
            Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonValida));
            Assert.That(esito.Errori, Does.Contain(InvioSapRegole.ErroreFuoriGruppo));
            Assert.That(esito.IdFattureCoinvolte, Is.EqualTo(new long[] { 2002 }));
        });
    }

    [Test]
    public void FatturaDiAltroAnno_ShouldEssereNonValida_FuoriGruppo()
    {
        var altroAnno = new InvioSapFatturaCorrente(2002, Anno - 1, Mese, Tipologia, StatoInvioSap.DA_INVIARE);
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(), [DaInviare(2001), altroAnno]);
        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonValida));
        Assert.That(esito.IdFattureCoinvolte, Is.EqualTo(new long[] { 2002 }));
    }

    [Test]
    public void FatturaDiAltraTipologia_ShouldEssereNonValida_FuoriGruppo()
    {
        var altraTipologia = new InvioSapFatturaCorrente(2002, Anno, Mese, "SECONDO SALDO", StatoInvioSap.DA_INVIARE);
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(), [DaInviare(2001), altraTipologia]);
        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonValida));
        Assert.That(esito.Errori, Does.Contain(InvioSapRegole.ErroreFuoriGruppo));
    }

    [Test]
    public void FuoriGruppo_ShouldPrecedere_InElaborazione()
    {
        var altroMeseInElab = new InvioSapFatturaCorrente(2002, Anno, Mese + 1, Tipologia, StatoInvioSap.IN_ELABORAZIONE);
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(), [DaInviare(2001), altroMeseInElab]);
        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonValida), "Prima si corregge la selezione, poi si guarda lo stato.");
    }

    // =============================================================================================
    // 409 stato non ammesso (RF-10)
    // =============================================================================================

    [Test]
    public void FatturaInElaborazione_ShouldEssereStatoNonAmmesso()
    {
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(), [DaInviare(2001), InElaborazione(2002)]);

        Assert.Multiple(() =>
        {
            Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.StatoNonAmmesso));
            Assert.That(esito.Errori, Does.Contain(InvioSapRegole.ErroreInElaborazione));
            Assert.That(esito.IdFattureCoinvolte, Is.EqualTo(new long[] { 2002 }));
        });
    }

    [Test]
    public void TutteInElaborazione_ShouldElencarleTutte()
    {
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(), [InElaborazione(2001), InElaborazione(2002)]);
        Assert.That(esito.IdFattureCoinvolte, Is.EquivalentTo(new long[] { 2001, 2002 }));
    }

    [TestCase("", TestName = "StatoSconosciuto(vuoto)")]
    [TestCase("inviata", TestName = "StatoSconosciuto(minuscolo)")]
    [TestCase("ELABORAZIONE", TestName = "StatoSconosciuto(abbreviato)")]
    [TestCase("2", TestName = "StatoSconosciuto(numerico legacy)")]
    public void StatoSconosciuto_ShouldEssereStatoNonAmmesso(string stato)
    {
        // Difesa in profondita': se la vista un giorno esponesse un valore nuovo, non si parte alla cieca.
        var strana = new InvioSapFatturaCorrente(2002, Anno, Mese, Tipologia, stato);
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(), [DaInviare(2001), strana]);

        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.StatoNonAmmesso));
        Assert.That(esito.Errori, Does.Contain(InvioSapRegole.ErroreStatoSconosciuto));
    }

    [Test]
    public void InElaborazione_ShouldPrecedere_StornoMancante()
    {
        // Inviata senza storno + una in elaborazione: 409, perche' il 400 dello storno sarebbe
        // inutile finche' il lock e' occupato.
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(storno: null), [Inviata(2001), InElaborazione(2002)]);
        Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.StatoNonAmmesso));
    }

    // =============================================================================================
    // 400 storno mancante (RF-16)
    // =============================================================================================

    [TestCase(null, TestName = "Storno(null)")]
    [TestCase("", TestName = "Storno(vuoto)")]
    [TestCase("   ", TestName = "Storno(solo spazi)")]
    public void Reinvio_SenzaStorno_ShouldEssereNonValida_ConRichiedeStorno(string? storno)
    {
        var esito = InvioSapRegole.ValidaGenerazione(Richiesta(storno: storno), [Inviata(2001), DaInviare(2002)]);

        Assert.Multiple(() =>
        {
            Assert.That(esito.Esito, Is.EqualTo(InvioSapEsito.NonValida));
            Assert.That(esito.Errori, Does.Contain(InvioSapRegole.ErroreStorno));
            Assert.That(esito.RichiedeStorno, Is.True, "Il FE usa questo flag per mostrare il campo.");
            Assert.That(esito.IdFattureCoinvolte, Is.EqualTo(new long[] { 2001 }), "Solo le gia' inviate.");
        });
    }

    [Test]
    public void RichiestaNull_ShouldLanciare()
    {
        Assert.Throws<ArgumentNullException>(() => InvioSapRegole.ValidaGenerazione(null!, []));
    }
}
