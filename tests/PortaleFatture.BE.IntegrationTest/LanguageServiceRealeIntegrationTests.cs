using PortaleFatture.BE.Infrastructure.Common.Language.Service;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Test FUNZIONALI (non adversarial) contro **Azure AI Language reale**: verificano che le tre
/// operazioni facciano il loro mestiere sul percorso felice, cosa che i 16 test HTTP dell'area non
/// possono dire perche' girano su un servizio finto — provano la nostra mappatura, non il servizio.
///
/// Divisione dei ruoli con l'altra fixture reale:
/// - **qui**: input tipici e ben formati, si verifica che il risultato sia utile;
/// - **`LanguageServiceAdversarialIntegrationTests`**: input ostili, valori limite, credenziali
///   sbagliate — cioe' cio' che il sistema non ha previsto.
///
/// Protezioni e costi: identici all'altra fixture, e centralizzati in <see cref="AzureLanguageTestbed"/>
/// — `[Explicit]` **piu'** l'opt-in `PF_RUN_AZURE_LANGUAGE_TESTS=1`, perche' `[Explicit]` da solo non
/// impedisce l'esecuzione quando un filtro seleziona la classe (verificato il 03/09/2026).
///
/// I dati sono **sintetici** e devono restare tali: sono spediti a un servizio esterno e committati in
/// un repository pubblico (requisito PNRR). CF e IBAN sono gli esempi canonici della documentazione
/// italiana, il dominio e' `.invalid` (RFC 2606).
///
/// ASSERZIONI SU INVARIANTI, non su valori: le versioni del modello Azure cambiano e i punteggi di
/// confidence derivano. Un `confidence == 0.97` diventerebbe rosso da solo fra qualche mese senza che
/// nulla si sia rotto — v. la stessa regola applicata ai conteggi di `vOrchestratore`.
/// </summary>
[TestFixture]
[Explicit("Colpisce Azure AI Language reale: consuma quota a pagamento. Richiede PF_RUN_AZURE_LANGUAGE_TESTS=1.")]
public class LanguageServiceRealeIntegrationTests
{
    private const string CodiceFiscaleFinto = "RSSMRA85M01H501Z";
    private const string EmailFinta = "mario.rossi@example.invalid";

    private LanguageService _servizio = null!;

    [OneTimeSetUp]
    public void CostruisciServizioReale() => _servizio = AzureLanguageTestbed.CostruisciOSalta().Servizio;

    // ---------------------------------------------------------------------------------------------
    // api/piid — rilevazione e redazione PII
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task Pii_TestoConDatiPersonali_LiRilevaERestituisceIlTestoRedatto()
    {
        var testo = $"Il signor Mario Rossi (CF {CodiceFiscaleFinto}) e' raggiungibile a {EmailFinta}.";

        var risultato = await _servizio.DetectPersonalIdentifiableInformationAsync(testo);

        Assert.That(risultato, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(risultato!.Count, Is.GreaterThan(0), "Nessuna entita' rilevata su un testo che ne contiene.");
            Assert.That(risultato.RedactedText, Is.Not.Null.And.Not.Empty);
            // Gli invarianti che oggi reggono davvero: nome ed email spariscono.
            Assert.That(risultato.RedactedText, Does.Not.Contain("Mario Rossi"));
            Assert.That(risultato.RedactedText, Does.Not.Contain(EmailFinta));
        });

        // ⚠️ Il CODICE FISCALE **non** e' fra gli invarianti, e non e' una dimenticanza: misurato il
        // 03/09/2026, Azure non lo riconosce come dato personale e lo lascia in chiaro — in un
        // documento italiano e' il dato piu' identificante di tutti. La lacuna e' documentata e fissata
        // in LanguageServiceAdversarialIntegrationTests (caratterizzazione + aspettativa [Ignore]);
        // qui si evita solo di dare per protetto cio' che non lo e'.
        TestContext.Out.WriteLine($"Redatto: {risultato!.RedactedText}");
    }

    [Test]
    public async Task Pii_TestoSenzaDatiPersonali_NonRilevaNullaELasciaIlTestoIntatto()
    {
        const string testo = "La fattura di primo saldo relativa al periodo indicato risulta regolarmente emessa.";

        var risultato = await _servizio.DetectPersonalIdentifiableInformationAsync(testo);

        // Il caso simmetrico, che dice se il servizio e' utilizzabile in produzione: se redigesse anche
        // qui, ogni nota di lavoro tornerebbe piena di asterischi.
        Assert.That(risultato?.Count ?? 0, Is.EqualTo(0), "Rilevata PII in un testo che non ne contiene.");
        Assert.That(risultato?.RedactedText ?? testo, Is.EqualTo(testo));
    }

    // ---------------------------------------------------------------------------------------------
    // api/language-detection
    // ---------------------------------------------------------------------------------------------

    [TestCase("Il presente documento attesta la regolare esecuzione delle prestazioni contrattuali.", "it")]
    [TestCase("This document certifies the correct execution of the contractual obligations.", "en")]
    [TestCase("Le présent document atteste la bonne exécution des prestations contractuelles.", "fr")]
    public async Task Lingua_TestoMonolingua_RilevaLaLinguaAttesa(string testo, string isoAtteso)
    {
        var risultato = await _servizio.DetectLanguageAsync(testo);

        Assert.That(risultato, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(risultato!.Value.Iso6391Name, Is.EqualTo(isoAtteso));
            // Soglia larga di proposito: interessa che il servizio sia sicuro, non quanto.
            Assert.That(risultato.Value.ConfidenceScore, Is.GreaterThan(0.5));
        });
    }

    // ---------------------------------------------------------------------------------------------
    // api/summarize-text
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task Sintesi_TestoArticolato_RestituisceUnaSintesiNonVuotaEPiuCorta()
    {
        var testo = string.Join(" ", Enumerable.Repeat(
            "Il procedimento di fatturazione prevede una fase di anticipo calcolata sul modulo commessa "
            + "dichiarato dall'ente, seguita dal primo saldo che storna l'anticipo gia' versato e "
            + "fattura le notifiche effettivamente verificate nel periodo di riferimento. "
            + "Le contestazioni aperte sospendono la fatturabilita' delle relative notifiche fino alla "
            + "loro risoluzione da parte del supporto.", 6));

        var risultato = await _servizio.SummarizeTextAsync(testo);

        var frasi = risultato?
            .SelectMany(collezione => collezione)
            .SelectMany(elemento => elemento.Summaries)
            .Select(sintesi => sintesi.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList() ?? [];

        Assert.That(frasi, Is.Not.Empty, "Nessuna sintesi prodotta su un testo articolato.");
        // Non si asserisce il CONTENUTO (dipende dal modello e cambia): si asserisce che sintetizzi,
        // cioe' che il risultato sia sensibilmente piu' corto dell'originale.
        Assert.That(string.Join(" ", frasi).Length, Is.LessThan(testo.Length / 2),
            "La 'sintesi' non e' piu' corta della meta' del testo: non sta sintetizzando.");
    }

    [Test]
    public async Task Sintesi_TestoItaliano_RestituisceUnaSintesiInItaliano()
    {
        var testo = string.Join(" ", Enumerable.Repeat(
            "La regolare esecuzione va firmata dall'ente prima che la fattura di saldo possa essere "
            + "emessa e inviata al sistema contabile.", 8));

        var risultato = await _servizio.SummarizeTextAsync(testo);
        var frasi = risultato?
            .SelectMany(c => c).SelectMany(e => e.Summaries).Select(s => s.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? [];

        Assert.That(frasi, Is.Not.Empty);

        // Verifica incrociata fra due operazioni del servizio: la sintesi di un testo italiano deve
        // essere riconosciuta come italiana. Piu' robusto che cercare parole chiave nel risultato.
        var linguaDellaSintesi = await _servizio.DetectLanguageAsync(string.Join(" ", frasi));
        Assert.That(linguaDellaSintesi?.Iso6391Name, Is.EqualTo("it"),
            "La sintesi di un testo italiano non risulta in italiano.");
    }
}
