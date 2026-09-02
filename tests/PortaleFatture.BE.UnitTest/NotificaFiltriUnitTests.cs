using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Logica pura su cui poggia `NotificaQueryGetByListEntiPersistence`, la query di ricerca notifiche.
///
/// Quella persistence compone il WHERE a stringhe dentro `Execute`, quindi non e' isolabile senza un
/// DB: la sua copertura vive in NotificaQueryListaEntiIntegrationTests. Qui stanno i due pezzi
/// **pubblici e puri** da cui dipendono le sue decisioni — e sono i due che, se cambiassero, ne
/// cambierebbero il comportamento senza che nulla fallisca a compile-time.
///
/// Nota: `NotificaSQLBuilder` (con `OrderBy`) e' `internal` e Infrastructure non espone i propri
/// internal ai progetti di test — solo SendEmailFunction lo fa. L'ordinamento e' quindi verificato
/// per i suoi EFFETTI negli integration test. Aprire gli internal sarebbe possibile, ma e' una
/// modifica a un assembly di produzione: da decidere, non da fare di slancio.
/// </summary>
public class NotificaFiltriUnitTests
{
    // ---------------------------------------------------------------------------------------------
    // TipoNotifica.Map(): traduce l'enum nel valore di paper_product_type salvato a DB
    // ---------------------------------------------------------------------------------------------

    [TestCase(TipoNotifica.AnalogicoARNazionali, "AR")]
    [TestCase(TipoNotifica.AnalogicoARInternazionali, "RIR")]
    [TestCase(TipoNotifica.AnalogicoRSNazionali, "RS")]
    [TestCase(TipoNotifica.AnalogicoRSInternazionali, "RIS")]
    [TestCase(TipoNotifica.Analogico890, "890")]
    public void Map_TipologieAnalogiche_ShouldTradurreNelCodiceDiDatabase(TipoNotifica tipo, string atteso)
        => Assert.That(tipo.Map(), Is.EqualTo(atteso));

    /// <summary>
    /// ATTENZIONE Il caso che spiega tutto il ramo digitale della query. `Digitali` **non** mappa su un codice:
    /// mappa sulla **stringa vuota**, perche' a DB una notifica digitale ha `paper_product_type` NULL.
    ///
    /// Conseguenza nella persistence: i valori mappati vengono filtrati con
    /// `Where(x =&gt; !string.IsNullOrEmpty(x))`, quindi Digitali **sparisce dalla lista** dei
    /// `paper_product_type` da cercare, e viene invece rappresentato aggiungendo al WHERE un
    /// `OR paper_product_type IS NULL`. Chiedendo SOLO Digitali la lista resta vuota e la condizione
    /// si riduce di fatto al solo IS NULL — funziona, ma per costruzione, non per progetto.
    ///
    /// Se qualcuno "sistemasse" questo mapping restituendo un codice vero (o null), la ricerca delle
    /// notifiche digitali cambierebbe risultato in silenzio.
    /// </summary>
    [Test]
    public void Map_Digitali_ShouldRestituireStringaVuota_NonUnCodice()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TipoNotifica.Digitali.Map(), Is.Empty,
                "Digitali non ha un paper_product_type: a DB e' NULL.");
            Assert.That(TipoNotifica.Digitali.Map(), Is.Not.Null,
                "Deve essere stringa vuota, non null: e' null a indicare un valore NON riconosciuto.");
        });
    }

    [Test]
    public void Map_ValoreFuoriDallEnum_ShouldRestituireNull()
        => Assert.That(((TipoNotifica)999).Map(), Is.Null,
            "Un valore non riconosciuto deve dare null, distinguibile dalla stringa vuota di Digitali.");

    [Test]
    public void Map_SoloDigitali_ShouldProdurreUnaListaVuotaDiCodici()
    {
        // Riproduce esattamente la riga della persistence:
        //   tnot = tipoNotifica.Select(x => x.Map()).Where(x => !string.IsNullOrEmpty(x))
        TipoNotifica[] richiesti = [TipoNotifica.Digitali];

        var codici = richiesti.Select(x => x.Map()).Where(x => !string.IsNullOrEmpty(x)).ToList();

        Assert.That(codici, Is.Empty,
            "Con il solo Digitali la lista dei paper_product_type e' vuota: a selezionare le notifiche "
            + "e' il ramo 'OR paper_product_type IS NULL', non un IN.");
    }

    [Test]
    public void Map_MistoDigitaleEAnalogico_ShouldTenereSoloICodiciAnalogici()
    {
        TipoNotifica[] richiesti = [TipoNotifica.Digitali, TipoNotifica.Analogico890, TipoNotifica.AnalogicoARNazionali];

        var codici = richiesti.Select(x => x.Map()).Where(x => !string.IsNullOrEmpty(x)).ToList();

        Assert.That(codici, Is.EquivalentTo(new[] { "890", "AR" }),
            "Il digitale non entra nell'IN: viene aggiunto come IS NULL accanto ad esso.");
    }

    // ---------------------------------------------------------------------------------------------
    // IsNullNotAny(): decide se un filtro viene applicato o ignorato
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Ogni filtro a lista della persistence e' governato da `!IsNullNotAny()`. Il nome e' fuorviante
    /// — si legge come "is null, not any" ma significa **"e' null OPPURE e' vuoto"** — ed e' la
    /// ragione per cui un array vuoto si comporta come un filtro assente invece che come "nessun
    /// risultato". Vale per EntiIds, Recapitisti, Consolidatori, TipoNotifica e StatoContestazione.
    /// </summary>
    [Test]
    public void IsNullNotAny_NullEVuoto_ShouldEssereEquivalenti()
    {
        string[]? nullo = null;

        Assert.Multiple(() =>
        {
            Assert.That(nullo.IsNullNotAny(), Is.True, "null");
            Assert.That(Array.Empty<string>().IsNullNotAny(), Is.True, "array vuoto");
            Assert.That(new[] { "x" }.IsNullNotAny(), Is.False, "array con elementi");
        });
    }
}
