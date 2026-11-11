using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>
/// Regole pure del Genera (be-invio-sap.md §3.3, §8.5). Due modalita' (D7): primo invio di un rigo
/// <c>DA_INVIARE</c> intero (senza <c>idFatture</c>, senza storno) e re-invio di fatture scelte nel dettaglio di
/// un rigo <c>INVIATE</c> (storno obbligatorio). Precedenza: lock 409 (handler) → formale 400 → non trovate 404 →
/// fuori gruppo o non inviate 400.
/// </summary>
[Category("InvioSap")]
public class InvioSapRegoleGenerazioneTests
{
    private const string Tipologia = "PRIMO SALDO";
    private const int Anno = 2093;
    private const int Mese = 1;
    private const string Utente = "admin@test";

    private static InvioSapFatturaCorrente Fattura(long id, bool? inviata = true, int anno = Anno, int mese = Mese, string tipologia = Tipologia)
        => new(id, anno, mese, tipologia, inviata);

    private static InvioSapRichiestaGenerazione PrimoInvio(string? nota = "Primo invio", string? storno = null,
        int? anno = Anno, int? mese = Mese, string? tipologia = Tipologia) => new()
        {
            Anno = anno, Mese = mese, TipologiaFattura = tipologia, Nota = nota, NumeroDocumentoStorno = storno
        };

    private static InvioSapRichiestaGenerazione ReInvio(long[]? ids = null, string? storno = "NC-2093-0042",
        string? nota = "Re-invio", int? anno = Anno, int? mese = Mese, string? tipologia = Tipologia) => new()
        {
            Anno = anno, Mese = mese, TipologiaFattura = tipologia, IdFatture = ids ?? [9502, 9503],
            Nota = nota, NumeroDocumentoStorno = storno
        };

    // =============================================================================================
    // Modalita'
    // =============================================================================================

    [Test]
    public void SenzaIdFatture_ShouldEssere_PrimoInvio()
    {
        Assert.That(InvioSapRegole.Modalita(PrimoInvio()), Is.EqualTo(InvioSapModalita.PrimoInvio));
    }

    [Test]
    public void ConIdFatture_AncheVuota_ShouldEssere_ReInvio()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.Modalita(ReInvio()), Is.EqualTo(InvioSapModalita.ReInvio));
            Assert.That(InvioSapRegole.Modalita(ReInvio(ids: [])), Is.EqualTo(InvioSapModalita.ReInvio),
                "idFatture: [] non equivale ad assente: e' un re-invio senza selezione, quindi 400.");
        });
    }

    // =============================================================================================
    // Validazione formale
    // =============================================================================================

    [Test]
    public void PrimoInvio_Completo_ShouldEssereValido()
    {
        Assert.That(InvioSapRegole.ValidaFormale(PrimoInvio(), Utente), Is.Empty);
    }

    [Test]
    public void ReInvio_Completo_ShouldEssereValido()
    {
        Assert.That(InvioSapRegole.ValidaFormale(ReInvio(), Utente), Is.Empty);
    }

    [TestCase("NC-1", TestName = "PrimoInvio_ConStorno_ShouldEssereRifiutato(PrimoInvio: storno presente)")]
    [TestCase(" ", TestName = "PrimoInvio_ConStorno_ShouldEssereRifiutato(PrimoInvio: storno di soli spazi)")]
    public void PrimoInvio_ConStorno_ShouldEssereRifiutato(string storno)
    {
        Assert.That(InvioSapRegole.ValidaFormale(PrimoInvio(storno: storno), Utente),
            Is.EqualTo(new[] { InvioSapRegole.ErroreStornoNonAmmesso }));
    }

    [TestCase(null, TestName = "ReInvio_SenzaStorno_ShouldEssereRifiutato(ReInvio: storno null)")]
    [TestCase("", TestName = "ReInvio_SenzaStorno_ShouldEssereRifiutato(ReInvio: storno vuoto)")]
    [TestCase("   ", TestName = "ReInvio_SenzaStorno_ShouldEssereRifiutato(ReInvio: storno di spazi)")]
    public void ReInvio_SenzaStorno_ShouldEssereRifiutato(string? storno)
    {
        Assert.That(InvioSapRegole.ValidaFormale(ReInvio(storno: storno), Utente),
            Is.EqualTo(new[] { InvioSapRegole.ErroreStornoMancante }));
    }

    [Test]
    public void ReInvio_StornoAlLimite_ShouldEssereValido_OltreRifiutato()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.ValidaFormale(ReInvio(storno: new string('S', InvioSapRegole.MaxStorno)), Utente), Is.Empty);
            Assert.That(InvioSapRegole.ValidaFormale(ReInvio(storno: new string('S', InvioSapRegole.MaxStorno + 1)), Utente),
                Is.EqualTo(new[] { InvioSapRegole.ErroreStornoLungo }));
        });
    }

    [TestCase(null, TestName = "NotaMancante_ShouldEssereRifiutata(Nota: null)")]
    [TestCase("", TestName = "NotaMancante_ShouldEssereRifiutata(Nota: vuota)")]
    [TestCase(" \t\n ", TestName = "NotaMancante_ShouldEssereRifiutata(Nota: solo spazi)")]
    public void NotaMancante_ShouldEssereRifiutata(string? nota)
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.ValidaFormale(PrimoInvio(nota: nota), Utente), Does.Contain(InvioSapRegole.ErroreNota));
            Assert.That(InvioSapRegole.ValidaFormale(ReInvio(nota: nota), Utente), Does.Contain(InvioSapRegole.ErroreNota));
        });
    }

    [Test]
    public void Nota_AlLimite_ShouldEssereValida_OltreRifiutata()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.ValidaFormale(PrimoInvio(nota: new string('n', InvioSapRegole.MaxNota)), Utente), Is.Empty);
            Assert.That(InvioSapRegole.ValidaFormale(PrimoInvio(nota: new string('n', InvioSapRegole.MaxNota + 1)), Utente),
                Is.EqualTo(new[] { InvioSapRegole.ErroreNotaLunga }));
            Assert.That(InvioSapRegole.ValidaFormale(PrimoInvio(nota: new string('n', 100_000)), Utente),
                Is.EqualTo(new[] { InvioSapRegole.ErroreNotaLunga }), "Nota da 100 KB: 400, non 500.");
        });
    }

    [Test]
    public void Nota_ConCaratteriSpeciali_ShouldEssereValida()
    {
        Assert.That(InvioSapRegole.ValidaFormale(PrimoInvio(nota: "Nota con \"apici\", \\ barre, a capo\n ed emoji 🚀"), Utente), Is.Empty);
    }

    [TestCase(null, TestName = "AnnoNonValido_ShouldEssereRifiutato(Anno: null)")]
    [TestCase(1999, TestName = "AnnoNonValido_ShouldEssereRifiutato(Anno: sotto il minimo)")]
    [TestCase(0, TestName = "AnnoNonValido_ShouldEssereRifiutato(Anno: zero)")]
    [TestCase(-2093, TestName = "AnnoNonValido_ShouldEssereRifiutato(Anno: negativo)")]
    [TestCase(10000, TestName = "AnnoNonValido_ShouldEssereRifiutato(Anno: cinque cifre)")]
    public void AnnoNonValido_ShouldEssereRifiutato(int? anno)
    {
        Assert.That(InvioSapRegole.ValidaFormale(PrimoInvio(anno: anno), Utente), Is.EqualTo(new[] { InvioSapRegole.ErroreAnno }));
    }

    [TestCase(null, TestName = "MeseNonValido_ShouldEssereRifiutato(Mese: null)")]
    [TestCase(0, TestName = "MeseNonValido_ShouldEssereRifiutato(Mese: zero)")]
    [TestCase(13, TestName = "MeseNonValido_ShouldEssereRifiutato(Mese: 13)")]
    [TestCase(-1, TestName = "MeseNonValido_ShouldEssereRifiutato(Mese: negativo)")]
    public void MeseNonValido_ShouldEssereRifiutato(int? mese)
    {
        Assert.That(InvioSapRegole.ValidaFormale(PrimoInvio(mese: mese), Utente), Is.EqualTo(new[] { InvioSapRegole.ErroreMese }));
    }

    [TestCase(null, TestName = "TipologiaMancante_ShouldEssereRifiutata(Tipologia: null)")]
    [TestCase("", TestName = "TipologiaMancante_ShouldEssereRifiutata(Tipologia: vuota)")]
    [TestCase("  ", TestName = "TipologiaMancante_ShouldEssereRifiutata(Tipologia: spazi)")]
    public void TipologiaMancante_ShouldEssereRifiutata(string? tipologia)
    {
        Assert.That(InvioSapRegole.ValidaFormale(PrimoInvio(tipologia: tipologia), Utente), Is.EqualTo(new[] { InvioSapRegole.ErroreTipologia }));
    }

    [Test]
    public void ReInvio_SelezioneVuota_ShouldEssereRifiutata()
    {
        Assert.That(InvioSapRegole.ValidaFormale(ReInvio(ids: []), Utente), Is.EqualTo(new[] { InvioSapRegole.ErroreSelezioneVuota }));
    }

    [TestCase(new long[] { 0 }, TestName = "ReInvio_IdNonValido_ShouldEssereRifiutato(Id: zero)")]
    [TestCase(new long[] { -1 }, TestName = "ReInvio_IdNonValido_ShouldEssereRifiutato(Id: negativo)")]
    [TestCase(new long[] { 9502, long.MinValue }, TestName = "ReInvio_IdNonValido_ShouldEssereRifiutato(Id: long.MinValue)")]
    public void ReInvio_IdNonValido_ShouldEssereRifiutato(long[] ids)
    {
        Assert.That(InvioSapRegole.ValidaFormale(ReInvio(ids: ids), Utente), Does.Contain(InvioSapRegole.ErroreIdNonValido));
    }

    [Test]
    public void ReInvio_IdDuplicati_ShouldEssereRifiutati()
    {
        Assert.That(InvioSapRegole.ValidaFormale(ReInvio(ids: [9502, 9502]), Utente), Does.Contain(InvioSapRegole.ErroreSelezioneDuplicata));
    }

    [Test]
    public void ReInvio_MilleFatture_ShouldEssereValido_MilleEUnaRifiutate()
    {
        var mille = Enumerable.Range(1, InvioSapRegole.MaxFatture).Select(i => (long)i).ToArray();
        var milleUno = Enumerable.Range(1, InvioSapRegole.MaxFatture + 1).Select(i => (long)i).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.ValidaFormale(ReInvio(ids: mille), Utente), Is.Empty);
            Assert.That(InvioSapRegole.ValidaFormale(ReInvio(ids: milleUno), Utente), Is.EqualTo(new[] { InvioSapRegole.ErroreSelezioneTroppoGrande }));
        });
    }

    [Test]
    public void ReInvio_IdEnorme_ShouldEssereFormalmenteValido()
    {
        // long.MaxValue non e' un errore di forma: e' una fattura che non esiste, quindi 404 piu' avanti.
        Assert.That(InvioSapRegole.ValidaFormale(ReInvio(ids: [long.MaxValue]), Utente), Is.Empty);
    }

    [TestCase(null, TestName = "UtenteMancante_ShouldEssereRifiutato(Utente: null)")]
    [TestCase("", TestName = "UtenteMancante_ShouldEssereRifiutato(Utente: vuoto)")]
    public void UtenteMancante_ShouldEssereRifiutato(string? utente)
    {
        Assert.That(InvioSapRegole.ValidaFormale(PrimoInvio(), utente), Is.EqualTo(new[] { InvioSapRegole.ErroreUtente }));
    }

    [Test]
    public void Utente_AlLimite_ShouldEssereValido_OltreRifiutato_SenzaTroncare()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.ValidaFormale(PrimoInvio(), new string('u', InvioSapRegole.MaxIdUtente)), Is.Empty);
            Assert.That(InvioSapRegole.ValidaFormale(PrimoInvio(), new string('u', InvioSapRegole.MaxIdUtente + 1)),
                Is.EqualTo(new[] { InvioSapRegole.ErroreUtente }), "IdUtente e' NVARCHAR(50): non si tronca in silenzio.");
        });
    }

    [Test]
    public void PiuErrori_ShouldEssereTuttiRiportati()
    {
        var errori = InvioSapRegole.ValidaFormale(ReInvio(ids: [0, 0], storno: null, nota: " ", anno: null, mese: 0, tipologia: ""), Utente);
        Assert.That(errori, Is.EquivalentTo(new[]
        {
            InvioSapRegole.ErroreAnno, InvioSapRegole.ErroreMese, InvioSapRegole.ErroreTipologia, InvioSapRegole.ErroreNota,
            InvioSapRegole.ErroreIdNonValido, InvioSapRegole.ErroreSelezioneDuplicata, InvioSapRegole.ErroreStornoMancante
        }));
    }

    // =============================================================================================
    // Re-invio: controlli sulle fatture lette dalla vista dettaglio
    // =============================================================================================

    [Test]
    public void ReInvio_TutteInviateNelGruppo_ShouldPassare()
    {
        Assert.That(InvioSapRegole.ValidaSelezione(ReInvio(), [Fattura(9502), Fattura(9503)]), Is.Null);
    }

    [Test]
    public void ReInvio_FatturaAssenteDallaVista_ShouldEssere404_ConLeSoleMancanti()
    {
        // 9505 non e' mai stata inviata: non ha dettaglio (C1), quindi per il re-invio non esiste.
        var esito = InvioSapRegole.ValidaSelezione(ReInvio(ids: [9502, 9505]), [Fattura(9502)]);
        Assert.Multiple(() =>
        {
            Assert.That(esito!.Tipo, Is.EqualTo(InvioSapGeneraEsitoTipo.NonTrovate));
            Assert.That(esito.IdFattureCoinvolte, Is.EqualTo(new long[] { 9505 }));
        });
    }

    [TestCase(2092, Mese, Tipologia, TestName = "ReInvio_FatturaDiUnAltroRigo_ShouldEssere400(FuoriGruppo: anno)")]
    [TestCase(Anno, 2, Tipologia, TestName = "ReInvio_FatturaDiUnAltroRigo_ShouldEssere400(FuoriGruppo: mese)")]
    [TestCase(Anno, Mese, "SECONDO SALDO", TestName = "ReInvio_FatturaDiUnAltroRigo_ShouldEssere400(FuoriGruppo: tipologia)")]
    public void ReInvio_FatturaDiUnAltroRigo_ShouldEssere400(int anno, int mese, string tipologia)
    {
        var esito = InvioSapRegole.ValidaSelezione(ReInvio(), [Fattura(9502), Fattura(9503, anno: anno, mese: mese, tipologia: tipologia)]);
        Assert.Multiple(() =>
        {
            Assert.That(esito!.Tipo, Is.EqualTo(InvioSapGeneraEsitoTipo.NonValida));
            Assert.That(esito.Errori, Is.EqualTo(new[] { InvioSapRegole.ErroreFuoriGruppo }));
            Assert.That(esito.IdFattureCoinvolte, Is.EqualTo(new long[] { 9503 }));
        });
    }

    [Test]
    public void ReInvio_TipologiaConMaiuscoleESpazi_ShouldEssereNelGruppo()
    {
        Assert.That(InvioSapRegole.ValidaSelezione(ReInvio(tipologia: " primo saldo "), [Fattura(9502), Fattura(9503)]), Is.Null);
    }

    [TestCase(false, TestName = "ReInvio_FatturaNonInviata_ShouldEssere400(NonInviata: FatturaInviata = 0)")]
    [TestCase(null, TestName = "ReInvio_FatturaNonInviata_ShouldEssere400(NonInviata: FatturaInviata = NULL, in elaborazione)")]
    public void ReInvio_FatturaNonInviata_ShouldEssere400(bool? inviata)
    {
        var esito = InvioSapRegole.ValidaSelezione(ReInvio(), [Fattura(9502), Fattura(9503, inviata: inviata)]);
        Assert.Multiple(() =>
        {
            Assert.That(esito!.Tipo, Is.EqualTo(InvioSapGeneraEsitoTipo.NonValida));
            Assert.That(esito.Errori, Is.EqualTo(new[] { InvioSapRegole.ErroreNonInviate }));
            Assert.That(esito.IdFattureCoinvolte, Is.EqualTo(new long[] { 9503 }));
        });
    }

    [Test]
    public void Precedenza_NonTrovate_PrimaDi_FuoriGruppo_PrimaDi_NonInviate()
    {
        var correnti = new[] { Fattura(1, inviata: null), Fattura(2, anno: 2092) };
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.ValidaSelezione(ReInvio(ids: [1, 2, 3]), correnti)!.Tipo, Is.EqualTo(InvioSapGeneraEsitoTipo.NonTrovate));
            Assert.That(InvioSapRegole.ValidaSelezione(ReInvio(ids: [1, 2]), correnti)!.Errori, Is.EqualTo(new[] { InvioSapRegole.ErroreFuoriGruppo }));
            Assert.That(InvioSapRegole.ValidaSelezione(ReInvio(ids: [1]), correnti)!.Errori, Is.EqualTo(new[] { InvioSapRegole.ErroreNonInviate }));
        });
    }

    [Test]
    public void ReInvio_RigheDuplicateDallaVista_ShouldEssereTollerate()
    {
        // Un ente con piu' contratti potrebbe duplicare righe nella vista (caso manuale BE-SAP-M9).
        Assert.That(InvioSapRegole.ValidaSelezione(ReInvio(), [Fattura(9502), Fattura(9502), Fattura(9503)]), Is.Null);
    }

    [Test]
    public void Costanti_ShouldCoincidereConLeTabelleDiData()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.IdAzioneGeneraJson, Is.EqualTo(Guid.Parse("0B102032-99F2-4AF9-BC5E-EBE844171DCC")));
            Assert.That(InvioSapRegole.DescrizioneAzione, Is.EqualTo("GeneraJsonFatture"));
            Assert.That(StatoFatturaSap.NonInviata, Is.EqualTo(4));
            Assert.That(StatoFatturaSap.Inviata, Is.EqualTo(5));
        });
    }
}
