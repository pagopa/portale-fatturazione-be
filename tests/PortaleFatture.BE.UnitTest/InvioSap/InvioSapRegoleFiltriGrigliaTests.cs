using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>
/// Regole dei filtri della griglia Invio a SAP (RF-13, RF-15) e del vocabolario degli stati. Puri.
/// L'anno corrente e' un parametro: la regola "non oltre l'anno prossimo" si prova senza orologio.
/// </summary>
[Category("InvioSap")]
public class InvioSapRegoleFiltriGrigliaTests
{
    private const int AnnoCorrente = 2026;

    private static IReadOnlyList<string> Valida(int? anno = AnnoCorrente, int[]? mesi = null,
        string?[]? tipologie = null, string? stato = null) =>
        InvioSapRegole.ValidaFiltriGriglia(anno, mesi, tipologie, stato, AnnoCorrente);

    // =============================================================================================
    // Anno (RF-15: obbligatorio, uno solo)
    // =============================================================================================

    [Test]
    public void SoloAnno_ShouldEssereValido()
    {
        Assert.That(Valida(), Is.Empty);
    }

    [Test]
    public void AnnoAssente_ShouldEssereRifiutato()
    {
        Assert.That(Valida(anno: null), Does.Contain(InvioSapRegole.ErroreAnno));
    }

    [TestCase(1999, TestName = "Anno(sotto il minimo)")]
    [TestCase(0, TestName = "Anno(zero)")]
    [TestCase(-1, TestName = "Anno(negativo)")]
    [TestCase(AnnoCorrente + 2, TestName = "Anno(oltre il prossimo)")]
    [TestCase(int.MaxValue, TestName = "Anno(int.MaxValue)")]
    public void AnnoFuoriIntervallo_ShouldEssereRifiutato(int anno)
    {
        Assert.That(Valida(anno: anno), Has.Count.EqualTo(1).And.Some.Contains("Anno fuori intervallo"));
    }

    [TestCase(InvioSapRegole.AnnoMinimo, TestName = "Anno(minimo)")]
    [TestCase(AnnoCorrente, TestName = "Anno(corrente)")]
    [TestCase(AnnoCorrente + 1, TestName = "Anno(prossimo: fatture di dicembre+4)")]
    public void AnnoAiBordi_ShouldEssereAccettato(int anno)
    {
        Assert.That(Valida(anno: anno), Is.Empty);
    }

    // =============================================================================================
    // Mesi
    // =============================================================================================

    [Test]
    public void MesiNull_EMesiVuoti_ShouldEssereAccettati()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Valida(mesi: null), Is.Empty);
            Assert.That(Valida(mesi: []), Is.Empty);
        });
    }

    [Test]
    public void TuttiIDodiciMesi_ShouldEssereAccettati()
    {
        Assert.That(Valida(mesi: Enumerable.Range(1, 12).ToArray()), Is.Empty);
    }

    [TestCase(new[] { 0 }, TestName = "Mesi(zero)")]
    [TestCase(new[] { 13 }, TestName = "Mesi(13)")]
    [TestCase(new[] { 5, -1 }, TestName = "Mesi(uno negativo)")]
    [TestCase(new[] { int.MaxValue }, TestName = "Mesi(int.MaxValue)")]
    public void MeseFuoriIntervallo_ShouldEssereRifiutato(int[] mesi)
    {
        Assert.That(Valida(mesi: mesi), Does.Contain(InvioSapRegole.ErroreMese));
    }

    [Test]
    public void MesiRipetuti_ShouldEssereRifiutati()
    {
        Assert.That(Valida(mesi: [5, 5]), Does.Contain("Mesi ripetuti nel filtro"));
    }

    // =============================================================================================
    // Tipologie
    // =============================================================================================

    [Test]
    public void TipologieNullOVuote_ShouldEssereAccettate()
    {
        Assert.That(Valida(tipologie: null), Is.Empty);
        Assert.That(Valida(tipologie: []), Is.Empty);
    }

    public static IEnumerable<TestCaseData> TipologieConElementoVuoto()
    {
        // L'array va incapsulato: TestCaseData(params object[]) lo prenderebbe come lista di parametri.
        yield return new TestCaseData(new object?[] { new string?[] { null } }).SetName("Tipologie(elemento null)");
        yield return new TestCaseData(new object?[] { new string?[] { "" } }).SetName("Tipologie(elemento vuoto)");
        yield return new TestCaseData(new object?[] { new string?[] { "PRIMO SALDO", " " } }).SetName("Tipologie(uno di spazi)");
    }

    [TestCaseSource(nameof(TipologieConElementoVuoto))]
    public void TipologiaVuotaNellaLista_ShouldEssereRifiutata(string?[] tipologie)
    {
        Assert.That(Valida(tipologie: tipologie), Does.Contain(InvioSapRegole.ErroreTipologia));
    }

    [Test]
    public void TipologiaOstile_ShouldEssereSoloUnValore()
    {
        // Non e' la regola a difendere dall'injection (lo fa il parametro Dapper): qui si prova solo che
        // un testo qualunque non venga rifiutato per forma, altrimenti il test HTTP non avrebbe senso.
        Assert.That(Valida(tipologie: ["' OR 1=1 --"]), Is.Empty);
    }

    // =============================================================================================
    // Stato invio
    // =============================================================================================

    [TestCase(null, TestName = "Stato(null = TUTTE)")]
    [TestCase("", TestName = "Stato(vuoto = TUTTE)")]
    [TestCase("TUTTE", TestName = "Stato(TUTTE)")]
    [TestCase("DA_INVIARE", TestName = "Stato(DA_INVIARE)")]
    [TestCase("IN_ELABORAZIONE", TestName = "Stato(IN_ELABORAZIONE)")]
    [TestCase("INVIATE", TestName = "Stato(INVIATE)")]
    [TestCase(" inviate ", TestName = "Stato(minuscolo con spazi)")]
    public void StatoAmmesso_ShouldEssereAccettato(string? stato)
    {
        Assert.That(Valida(stato: stato), Is.Empty);
    }

    [TestCase("INVIATA", TestName = "Stato(INVIATA: e' lo stato fattura, non il filtro)")]
    [TestCase("REINVIATE", TestName = "Stato(REINVIATE: e' il booleano, non uno stato)")]
    [TestCase("2", TestName = "Stato(numerico legacy)")]
    [TestCase("DROP TABLE", TestName = "Stato(ostile)")]
    public void StatoNonAmmesso_ShouldEssereRifiutato(string stato)
    {
        Assert.That(Valida(stato: stato), Has.Some.Contains("Stato invio non valido"));
    }

    [Test]
    public void PiuErrori_ShouldEssereTuttiRiportati()
    {
        var errori = Valida(anno: null, mesi: [13], tipologie: [""], stato: "X");
        Assert.That(errori, Has.Count.EqualTo(4));
    }

    // =============================================================================================
    // Vocabolari
    // =============================================================================================

    [Test]
    public void FiltroStato_StatoFattura_ShouldMappare()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FiltroStatoInvioSap.StatoFattura("TUTTE"), Is.Null);
            Assert.That(FiltroStatoInvioSap.StatoFattura(null), Is.Null);
            Assert.That(FiltroStatoInvioSap.StatoFattura("DA_INVIARE"), Is.EqualTo(StatoInvioSap.DA_INVIARE));
            Assert.That(FiltroStatoInvioSap.StatoFattura("IN_ELABORAZIONE"), Is.EqualTo(StatoInvioSap.IN_ELABORAZIONE));
            Assert.That(FiltroStatoInvioSap.StatoFattura("inviate"), Is.EqualTo(StatoInvioSap.INVIATA));
        });
    }

    [Test]
    public void StatoInvioSap_IsValido_ShouldEssereCaseSensitive()
    {
        // I valori viaggiano nelle viste e nel contratto: una variante di maiuscole e' un dato sporco.
        Assert.Multiple(() =>
        {
            Assert.That(StatoInvioSap.IsValido("INVIATA"), Is.True);
            Assert.That(StatoInvioSap.IsValido("inviata"), Is.False);
            Assert.That(StatoInvioSap.IsValido(null), Is.False);
            Assert.That(StatoInvioSap.Tutti, Has.Length.EqualTo(3));
        });
    }

    [Test]
    public void StatoBatchSap_IsTerminale_ShouldEssereVeroSoloPerCompletatoEFallito()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StatoBatchSap.IsTerminale(StatoBatchSap.COMPLETATO), Is.True);
            Assert.That(StatoBatchSap.IsTerminale(StatoBatchSap.FALLITO), Is.True);
            Assert.That(StatoBatchSap.IsTerminale(StatoBatchSap.IN_ELABORAZIONE), Is.False);
            Assert.That(StatoBatchSap.IsTerminale(null), Is.False);
            Assert.That(StatoBatchSap.IsTerminale("ANNULLATO"), Is.False, "Non previsto: un batch si chiude solo COMPLETATO o FALLITO.");
        });
    }
}
