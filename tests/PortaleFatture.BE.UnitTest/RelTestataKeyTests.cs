using PortaleFatture.BE.Core.Entities.SEND.DatiRel;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Unit test PURI (nessun DB) su RelTestataKey: serializzazione/deserializzazione dell'id composito
/// {IdEnte}_{IdContratto}_{TipologiaFattura}_{Anno}_{Mese} usato dalle rotte api/rel/pagopa/{id}.
/// E' il punto dove le tipologie con spazio ("SECONDO SALDO", "SEM. SOSPESI", "VAR. SEMESTRALE")
/// vengono mappate su trattino e viceversa; un errore qui manda la query del dettaglio su una chiave
/// sbagliata.
/// </summary>
public class RelTestataKeyTests
{
    [Test]
    /// <summary>
    /// Verifica che la deserializzazione di un id valido estrae correttamente i campi. 
    /// </summary>
    public void Deserialize_IdValido_EstraeICampi()
    {
        var id = "76fd95f3-c1a1-410f-95c8-a6ac00989aae_97d4d355-ab9f-4a09-8fe1-d9901f181a77_SECONDO-SALDO_2026_4";
        var k = RelTestataKey.Deserialize(id);

        Assert.Multiple(() =>
        {
            Assert.That(k.IdEnte, Is.EqualTo("76fd95f3-c1a1-410f-95c8-a6ac00989aae"));
            Assert.That(k.IdContratto, Is.EqualTo("97d4d355-ab9f-4a09-8fe1-d9901f181a77"));
            Assert.That(k.TipologiaFattura, Is.EqualTo("SECONDO SALDO"), "trattino -> spazio");
            Assert.That(k.Anno, Is.EqualTo(2026));
            Assert.That(k.Mese, Is.EqualTo(4));
        });
    }

    // Round-trip per le tipologie reali con spazio/punto: ToString -> Deserialize deve tornare l'originale.
    [TestCase("PRIMO SALDO")]
    [TestCase("SECONDO SALDO")]
    [TestCase("VAR. SEMESTRALE")]
    [TestCase("SEM. SOSPESI")]
    [TestCase("ANTICIPO")]
    public void RoundTrip_Tipologia_ConservaLoSpazio(string tipologia)
    {
        var originale = new RelTestataKey("ente-1", "contratto-1", tipologia, 2026, 7);
        var ricostruita = RelTestataKey.Deserialize(originale.ToString());

        Assert.That(ricostruita.TipologiaFattura, Is.EqualTo(tipologia),
            $"Round-trip di '{tipologia}' deve conservare gli spazi (via trattino nell'id).");
    }

    [Test]
    /// <summary>
    /// Verifica che la deserializzazione di un id malformato con meno di cinque parti lancia un'eccezione.
    /// </summary>
    public void Deserialize_IdMalformato_MenoDiCinqueParti_Lancia()
    {
        // Un id senza tutte le parti (es. manca mese) non e' deserializzabile: la SP/query non deve
        // ricevere valori indefiniti. Documenta il fallimento attuale (accesso a indice mancante).
        Assert.That(() => RelTestataKey.Deserialize("ente_contratto_SECONDO-SALDO_2026"),
            Throws.InstanceOf<IndexOutOfRangeException>());
    }

    [Test]
    /// <summary>
    /// Verifica che la deserializzazione di un id con anno non numerico lancia un'eccezione.
    /// </summary>
    public void Deserialize_AnnoNonNumerico_Lancia()
    {
        Assert.That(() => RelTestataKey.Deserialize("ente_contratto_SECONDO-SALDO_XXXX_4"),
            Throws.InstanceOf<FormatException>());
    }
}
