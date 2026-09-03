using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Common.Language.Service;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Test ADVERSARIAL su Azure AI Language che colpiscono il **servizio vero**, con endpoint e chiave
/// reali presi dagli user secrets del progetto (`PortaleFattureOptions:Language:Endpoint` e `:Key`).
///
/// Perché esistono, dato che l'area ha già 16 test HTTP: quelli usano un servizio **finto**, quindi
/// provano la nostra mappatura degli errori — non ciò che Azure fa davvero. Qui stanno solo i casi che
/// rispondono a una domanda che il fake non puo' rispondere: se i nostri limiti di lunghezza sono i
/// numeri giusti, se la redazione PII redige davvero, quanto impiega una sintesi grande rispetto al
/// timeout che le abbiamo dato.
///
/// ATTENZIONE — TRE PROTEZIONI, e la prima da sola NON basta (verificato, v. sotto):
/// 1. la fixture e' **[Explicit]**: la esclude dai run *senza filtro*, cioe' dalla suite completa.
/// 2. serve l'**opt-in esplicito** `PF_RUN_AZURE_LANGUAGE_TESTS=1` (variabile d'ambiente o chiave di
///    configurazione), **oltre** ai secrets — v. <see cref="AzureLanguageTestbed"/>. E' la protezione
///    che conta davvero.
/// 3. ogni chiamata **consuma quota a pagamento**: il caso sulla sintesi grande e' in
///    `[Category("Costoso")]` per poterlo escludere.
///
/// ⚠️ **Perche' [Explicit] non basta** (misurato il 03/09/2026 leggendo il `.trx`): con un filtro che
/// li seleziona — anche indirettamente — l'adapter NUnit li considera "scelti esplicitamente" e li
/// **esegue**, `[Explicit]` compreso. Il `OneTimeSetUp` girava davvero: a fermarli era solo l'assenza
/// dei secrets. E qui si somma la trappola gia' nota dei filtri (v.
/// `docs/test-integrazione-db-seedato.md`): il namespace e' `PortaleFatture.BE.IntegrationTest`, quindi
/// un innocuo `--filter "FullyQualifiedName~Fatture"` seleziona **l'intera suite**, questi test
/// inclusi. Senza l'opt-in del punto 2, il primo collega che filtra cosi' dopo aver configurato i
/// secrets paga chiamate ad Azure senza sapere di averle chieste.
///
/// ATTENZIONE — I DATI DELLE FIXTURE SONO SINTETICI, e devono restare tali. Sono testi spediti a un
/// servizio esterno **e** committati in un repository pubblico (requisito PNRR): mettere PII reale qui
/// creerebbe esattamente il problema che il punto privacy di questa feature deve governare. Il codice
/// fiscale e l'IBAN sotto sono gli esempi canonici della documentazione italiana, il dominio e'
/// `.invalid` (RFC 2606, riservato proprio a questo scopo).
///
/// REGOLA DI ASSERZIONE: le versioni del modello Azure cambiano e i punteggi di confidence derivano.
/// Si asserisce sugli **invarianti** (il dato non compare piu' nel testo redatto, la lingua e' `it`,
/// la durata sta sotto la soglia), mai su valori esatti — un `confidence == 0.97` diventerebbe rosso
/// da solo fra qualche mese senza che nulla si sia rotto.
/// </summary>
[TestFixture]
[Explicit("Colpisce Azure AI Language reale: consuma quota a pagamento. Richiede PF_RUN_AZURE_LANGUAGE_TESTS=1.")]
public class LanguageServiceAdversarialIntegrationTests
{
    // Esempi canonici, persone inesistenti. Non sostituire con dati veri: v. l'avvertenza sopra.
    private const string CodiceFiscaleFinto = "RSSMRA85M01H501Z";
    private const string EmailFinta = "mario.rossi@example.invalid";
    private const string IbanFinto = "IT60X0542811101000000123456";
    private const string TelefonoFinto = "+39 06 12345678";

    private LanguageService _servizio = null!;
    private int _maxChars;
    private int _maxCharsSummarize;
    private int _timeoutSeconds;

    private string _endpoint = null!;
    private string _key = null!;

    [OneTimeSetUp]
    public void CostruisciServizioReale()
    {
        var configurato = AzureLanguageTestbed.CostruisciOSalta();

        _servizio = configurato.Servizio;
        _endpoint = configurato.Endpoint;
        _key = configurato.Key;
        _maxChars = configurato.MaxChars;
        _maxCharsSummarize = configurato.MaxCharsSummarize;
        _timeoutSeconds = configurato.TimeoutSeconds;
    }

    private static string Riempi(int caratteri, string frammento = "Testo di prova per il servizio. ")
    {
        var sb = new System.Text.StringBuilder(caratteri + frammento.Length);
        while (sb.Length < caratteri) sb.Append(frammento);
        return sb.ToString(0, caratteri);
    }

    // ---------------------------------------------------------------------------------------------
    // GRUPPO 1 — I nostri limiti di lunghezza sono ASSUNZIONI, mai verificate contro il servizio vero.
    // Se il limite reale fosse piu' basso del nostro, il 400 non scatterebbe e l'utente prenderebbe un
    // 502 DOPO aver pagato la chiamata: esattamente il difetto che il limite doveva chiudere.
    // ---------------------------------------------------------------------------------------------

    [Test]
    public void Pii_TestoAppenaSottoIlNostroLimite_AzureLoAccetta()
    {
        var testo = Riempi(_maxChars - 1);

        // Non interessa COSA trova (probabilmente nulla): interessa che Azure non rifiuti la
        // dimensione. Un UpstreamServiceException qui significherebbe che il nostro limite e' troppo
        // permissivo e va abbassato.
        Assert.DoesNotThrowAsync(async () => await _servizio.DetectPersonalIdentifiableInformationAsync(testo));
    }

    [Test]
    public void Pii_TestoOltreIlNostroLimite_RifiutatoDaNoiSenzaChiamareAzure()
    {
        var testo = Riempi(_maxChars + 1);
        var cronometro = Stopwatch.StartNew();

        var eccezione = Assert.CatchAsync(
            async () => await _servizio.DetectPersonalIdentifiableInformationAsync(testo));
        cronometro.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(eccezione, Is.TypeOf<ValidationException>(),
                "Deve essere un 400 nostro, non un 502 di ritorno da Azure.");
            // La prova che la chiamata NON e' partita: un giro di rete non sta in mezzo secondo.
            // E' il punto del controllo — evitare il costo, non solo dare l'errore giusto.
            Assert.That(cronometro.ElapsedMilliseconds, Is.LessThan(500),
                "Il rifiuto deve avvenire prima della chiamata di rete.");
        });
    }

    /// <summary>
    /// La domanda cattiva: Azure conta i caratteri come li conta C#? Un'emoji e' **due** `char` in .NET
    /// (coppia surrogata) ma un carattere per un umano. `text.Length` conta i primi, quindi il nostro
    /// controllo e' conservativo per i testi non latini — ma se Azure contasse i *punti di codice*
    /// accetterebbe testi che noi rifiutiamo, e staremmo negando richieste legittime.
    /// </summary>
    [Test]
    public void Pii_TestoDiEmoji_LUnitaDiMisuraNostraNonEQuellaUmana()
    {
        const string emoji = "😀";
        var quanteEmoji = (_maxChars / 2) + 10;           // oltre il limite in `char`...
        var testo = string.Concat(Enumerable.Repeat(emoji, quanteEmoji));

        Assert.That(testo.Length, Is.GreaterThan(_maxChars),
            "Precondizione: in char siamo oltre il limite.");

        var eccezione = Assert.CatchAsync(
            async () => await _servizio.DetectPersonalIdentifiableInformationAsync(testo));

        // Caratterizzazione: oggi rifiutiamo a meta' delle emoji dichiarate dal limite. Se un domani
        // si volesse contare per elementi di testo (StringInfo/rune), e' qui che si vede il cambio.
        Assert.That(eccezione, Is.TypeOf<ValidationException>(),
            $"Con {quanteEmoji} emoji ({testo.Length} char) il limite di {_maxChars} scatta: il conteggio "
            + "e' in char UTF-16, non in caratteri percepiti.");
    }

    // ---------------------------------------------------------------------------------------------
    // GRUPPO 2 — La redazione. E' l'unica funzione di SICUREZZA dell'area, e finora nessun test aveva
    // mai verificato che il dato personale sparisca davvero dal testo restituito.
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// ⚠️ **SCOPERTA DEL 03/09/2026 — Azure non riconosce il codice fiscale italiano come PII.**
    /// Misurato al primo giro reale: su `"Il contribuente Mario Rossi, codice fiscale RSSMRA85M01H501Z,
    /// ha presentato istanza."` la risposta e'
    /// `"Il ************ ***********, codice fiscale RSSMRA85M01H501Z, ha presentato istanza."` —
    /// nome oscurato, **CF in chiaro**. Stesso esito con gli spazi (`RSS MRA 85M01 H501Z`) e dentro un
    /// testo lungo con 40 ripetizioni.
    ///
    /// Non e' un difetto del nostro codice: e' una **proprieta' del servizio** che cambia il valore
    /// della feature. `api/piid` esiste per proteggere dati personali, e in un documento italiano il CF
    /// e' il dato piu' identificante di tutti — piu' del nome, che invece viene oscurato. Chi si
    /// affidasse alla redazione crederebbe di aver protetto il testo.
    ///
    /// Decisione **non tecnica**, da portare a chi segue privacy (v. il punto aperto in TD-5). Opzioni:
    /// passare esplicitamente le categorie PII all'SDK, oppure aggiungere una redazione nostra per il
    /// CF (formato regolare, 16 caratteri con carattere di controllo), oppure dichiarare il limite.
    ///
    /// Questo test resta come **aspettativa corretta**: va riattivato quando la lacuna e' chiusa.
    /// Il comportamento attuale e' fissato da <see cref="Pii_CodiceFiscale_OggiNonVieneRedatto_Caratterizzazione"/>.
    /// </summary>
    [Test]
    [Ignore("Azure non redige il codice fiscale italiano (misurato 03/09/2026). Aspettativa corretta, "
            + "in attesa di decisione privacy: categorie PII esplicite, redazione nostra, o limite dichiarato.")]
    public async Task Pii_CodiceFiscale_NonCompareNelTestoRedatto()
    {
        var testo = $"Il contribuente Mario Rossi, codice fiscale {CodiceFiscaleFinto}, ha presentato istanza.";

        var risultato = await _servizio.DetectPersonalIdentifiableInformationAsync(testo);

        Assert.That(risultato, Is.Not.Null, "Con una PII evidente il servizio deve restituire qualcosa.");
        Assert.That(risultato!.RedactedText, Does.Not.Contain(CodiceFiscaleFinto),
            "Il testo redatto contiene ancora il codice fiscale: la redazione non sta proteggendo nulla.");
    }

    /// <summary>
    /// Fissa il comportamento reale descritto sopra, cosi' la suite resta verde ma la lacuna resta
    /// **visibile**. Se un domani Azure iniziasse a riconoscere il CF, questo test diventerebbe rosso —
    /// ed e' il segnale che si aspetta, non un fastidio: vorrebbe dire che si puo' riattivare
    /// <see cref="Pii_CodiceFiscale_NonCompareNelTestoRedatto"/> e chiudere il punto.
    /// </summary>
    [Test]
    public async Task Pii_CodiceFiscale_OggiNonVieneRedatto_Caratterizzazione()
    {
        var testo = $"Il contribuente Mario Rossi, codice fiscale {CodiceFiscaleFinto}, ha presentato istanza.";

        var risultato = await _servizio.DetectPersonalIdentifiableInformationAsync(testo);
        var redatto = risultato?.RedactedText ?? testo;
        TestContext.Out.WriteLine($"Redatto: {redatto}");

        Assert.Multiple(() =>
        {
            Assert.That(redatto, Does.Not.Contain("Mario Rossi"),
                "Il NOME viene oscurato: e' la parte che funziona.");
            Assert.That(redatto, Does.Contain(CodiceFiscaleFinto),
                "Se il CF risulta ora oscurato, Azure e' migliorato: riattivare il test [Ignore] gemello.");
        });
    }

    /// <summary>
    /// I tre formati "ostili" sono tre `[Test]` distinti e non `[TestCase]` con `TestName`
    /// personalizzato: quella forma **rompe la mappatura in Test Explorer** — i casi vengono eseguiti
    /// (verificato da riga di comando) ma l'IDE non riaggancia il risultato al nodo e li mostra come mai
    /// eseguiti. Costato un'indagine il 03/09/2026. Tre metodi separati non hanno il problema, e in piu'
    /// ognuno puo' dire quale formato sonda.
    ///
    /// Se uno di questi fallisce NON e' un difetto del nostro codice: e' la scoperta che la redazione ha
    /// un buco su quel formato, e la decisione (normalizzare prima di inviare? avvisare l'utente?) va
    /// presa a livello di prodotto. Il test serve a saperlo invece di scoprirlo dopo.
    /// </summary>
    private async Task VerificaCheVengaRedatto(string testo, string datoDaNascondere)
    {
        var risultato = await _servizio.DetectPersonalIdentifiableInformationAsync(testo);

        Assert.That(risultato?.RedactedText ?? testo, Does.Not.Contain(datoDaNascondere),
            $"Formato non riconosciuto dalla redazione: '{datoDaNascondere}' resta in chiaro.");
    }

    /// <summary>Maiuscolo integrale: capita quando il dato arriva da un gestionale legacy.</summary>
    [Test]
    public Task Pii_EmailInMaiuscolo_VieneRedatta() =>
        VerificaCheVengaRedatto("scrivere a MARIO.ROSSI@EXAMPLE.INVALID", "MARIO.ROSSI@EXAMPLE.INVALID");

    /// <summary>PII dentro markup: il testo potrebbe arrivare da un campo note formattato.</summary>
    [Test]
    public Task Pii_DentroMarkupHtml_VieneRedatta() =>
        VerificaCheVengaRedatto($"<p>IBAN: {IbanFinto}</p>", IbanFinto);

    /// <summary>Prefisso internazionale e parentesi attorno: forma tipica di un recapito scritto a mano.</summary>
    [Test]
    public Task Pii_TelefonoConPrefissoInternazionale_VieneRedatto() =>
        VerificaCheVengaRedatto($"tel. {TelefonoFinto} (ufficio)", TelefonoFinto);

    [Test]
    public async Task Pii_RipetutaMolteVolte_RedattaInOgniOccorrenza()
    {
        // Un buco che si vedrebbe solo su testi lunghi: redigere la prima occorrenza e non le altre.
        var righe = Enumerable.Range(1, 40)
            .Select(i => $"Riga {i}: pratica di Mario Rossi, CF {CodiceFiscaleFinto}, email {EmailFinta}.");
        var testo = string.Join("\n", righe);

        var risultato = await _servizio.DetectPersonalIdentifiableInformationAsync(testo);

        // Si asserisce sull'EMAIL e non sul CF: il CF non viene redatto **in nessun caso** (v. la
        // scoperta documentata sopra), quindi includerlo qui non proverebbe nulla sulla ripetizione —
        // il test fallirebbe per l'altra causa, mascherando cio' che deve invece verificare.
        Assert.That(risultato!.RedactedText, Does.Not.Contain(EmailFinta),
            "Almeno un'occorrenza dell'email e' sopravvissuta alla redazione: la copertura si degrada "
            + "sui testi lunghi.");
    }

    /// <summary>
    /// Il CF **spaziato** — forma comune quando il dato arriva da un modulo o da un OCR. Stessa causa
    /// del gemello sopra: oggi non viene riconosciuto. Tenuto separato dai formati che funzionano
    /// (email, IBAN, telefono) per non lasciare rosso un caso che dipende da una decisione aperta.
    /// </summary>
    [Test]
    [Ignore("Stessa lacuna del CF non spaziato (misurato 03/09/2026): Azure non riconosce il codice "
            + "fiscale italiano. In attesa di decisione privacy.")]
    public async Task Pii_CodiceFiscaleConSpazi_VieneComunqueRedatto()
    {
        const string cfSpaziato = "RSS MRA 85M01 H501Z";

        var risultato = await _servizio.DetectPersonalIdentifiableInformationAsync(
            $"Il richiedente indica codice fiscale {cfSpaziato} nel modulo.");

        Assert.That(risultato?.RedactedText, Does.Not.Contain(cfSpaziato));
    }

    /// <summary>
    /// Il caso simmetrico, ed e' quello che rompe il prodotto invece di esporlo: un identificativo
    /// **di dominio** che somiglia a un dato personale. Se la redazione lo mascherasse, un operatore di
    /// supporto che incolla una nota di contestazione si vedrebbe sparire lo IUN — dato di lavoro, non
    /// personale.
    /// </summary>
    [Test]
    public async Task Pii_IdentificativiDiDominio_NonVengonoRedattiComeSeFosseroPersonali()
    {
        const string iun = "ABCD-EFGH-IJKL-202601-A-1";
        const string codiceContratto = "TOKEN-E1";
        var testo = $"Contestazione sulla notifica {iun} del contratto {codiceContratto}.";

        var risultato = await _servizio.DetectPersonalIdentifiableInformationAsync(testo);
        var redatto = risultato?.RedactedText ?? testo;

        Assert.Multiple(() =>
        {
            Assert.That(redatto, Does.Contain(iun),
                "Lo IUN e' stato redatto come dato personale: l'operatore perderebbe l'informazione utile.");
            Assert.That(redatto, Does.Contain(codiceContratto),
                "Il codice contratto e' stato redatto come dato personale.");
        });
    }

    // ---------------------------------------------------------------------------------------------
    // GRUPPO 3 — Rilevazione lingua: due comportamenti oggi NON DECISI, che il fake non costringe a
    // decidere perche' restituisce sempre cio' che gli diciamo.
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task Lingua_TestoItalianoChiaro_RilevaItalianoConFiduciaAlta()
    {
        var risultato = await _servizio.DetectLanguageAsync(
            "Il presente documento attesta la regolare esecuzione delle prestazioni contrattuali.");

        Assert.Multiple(() =>
        {
            Assert.That(risultato?.Iso6391Name, Is.EqualTo("it"));
            // Soglia larga di proposito: il valore esatto deriva fra versioni del modello.
            Assert.That(risultato!.Value.ConfidenceScore, Is.GreaterThan(0.5));
        });
    }

    [Test]
    public async Task Lingua_TestoMisto_RestituisceUnaSolaLinguaEQuestoVaSaputo()
    {
        var risultato = await _servizio.DetectLanguageAsync(
            "Questo documento contiene una parte in italiano and also a significant part written in English "
            + "which makes the detection genuinely ambiguous for any classifier.");

        // L'API restituisce UNA lingua, non una ripartizione: su testi misti l'esito e' una scelta del
        // modello. Si asserisce solo che una risposta arrivi e sia plausibile — chi legge il portale
        // deve sapere che il campo non descrive un documento multilingua.
        Assert.That(risultato?.Iso6391Name, Is.AnyOf("it", "en"));
    }

    [Test]
    public async Task Lingua_TestoSenzaContenutoLinguistico_ComportamentoDaDecidere()
    {
        var risultato = await _servizio.DetectLanguageAsync("123 456 789 -- 00,00 €");

        // CARATTERIZZAZIONE, non requisito: Azure risponde tipicamente `(Unknown)` con confidence
        // bassa, e noi la giriamo al client come una lingua qualsiasi. Se il prodotto volesse un 404
        // (o una soglia minima di confidence) e' qui che va deciso: oggi la decisione non esiste.
        TestContext.Out.WriteLine(
            $"Lingua: '{risultato?.Name}' ({risultato?.Iso6391Name}), confidence {risultato?.ConfidenceScore}");
        Assert.That(risultato, Is.Not.Null, "Nessuna eccezione attesa: e' un input legittimo, solo povero.");
    }

    // ---------------------------------------------------------------------------------------------
    // GRUPPO 4 — Sintesi. Il caso grande e' una MISURA prima che un test: serve a sapere se il timeout
    // a 45s e il limite a 125.000 caratteri sono coerenti fra loro.
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task Sintesi_TestoSenzaNullaDaSintetizzare_ComportamentoDaDecidere()
    {
        var risultato = await _servizio.SummarizeTextAsync("00,00 12,34 56,78 90,12 34,56 78,90");

        var frasi = risultato?
            .SelectMany(c => c)
            .SelectMany(r => r.Summaries)
            .Select(s => s.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList() ?? [];

        // Se esce vuoto, l'endpoint risponde 404 — cioe' "nessun risultato", che e' corretto ma va
        // saputo dal frontend. Caratterizzazione: si registra l'esito, non lo si impone.
        TestContext.Out.WriteLine($"Frasi di sintesi restituite: {frasi.Count}");
        Assert.Pass($"Esito registrato: {frasi.Count} frasi.");
    }

    [Test]
    [Category("Costoso")]
    public async Task Sintesi_TestoVicinoAlLimite_ConcludeEntroIlTimeoutConfigurato()
    {
        // IL test che tara la configurazione: se una sintesi ammessa dal nostro limite di lunghezza
        // NON sta nel nostro timeout, in produzione ogni sintesi grande diventa un 504 — cioe' i due
        // parametri si contraddicono e uno dei due va corretto.
        var testo = Riempi(_maxCharsSummarize - 100,
            "Il Consiglio ha deliberato in merito alle prestazioni contrattuali del periodo. ");
        var cronometro = Stopwatch.StartNew();

        // NON si usa Assert.CatchAsync: pretenderebbe un'eccezione, e il caso ATTESO qui e' che la
        // chiamata riesca. (Errore fatto e corretto il 03/09/2026: il test falliva con
        // "Expected: instance of System.Exception, But was: null" proprio quando andava tutto bene.)
        UpstreamTimeoutException? timeout = null;
        try
        {
            await _servizio.SummarizeTextAsync(testo);
        }
        catch (UpstreamTimeoutException ex)
        {
            timeout = ex;
        }
        cronometro.Stop();

        var margine = _timeoutSeconds - cronometro.Elapsed.TotalSeconds;
        TestContext.Out.WriteLine(
            $"Sintesi di {testo.Length:N0} caratteri conclusa in {cronometro.Elapsed.TotalSeconds:N1}s "
            + $"(timeout {_timeoutSeconds}s, margine {margine:N1}s).");

        Assert.That(timeout, Is.Null,
            $"Un testo entro il limite di {_maxCharsSummarize:N0} caratteri non sta nel timeout di "
            + $"{_timeoutSeconds}s: i due parametri sono incoerenti, va alzato il timeout o abbassato il limite.");

        // MISURA del 03/09/2026: 34s su 45 disponibili, cioe' circa 11s di margine (24%). I due
        // parametri sono coerenti, ma non con larghezza — e la risorsa e' dietro un AI gateway
        // CONDIVISO (v. AzureLanguageTestbed), quindi la latenza dipende anche dal carico altrui.
        // Se questa riga inizia a comparire nei log di un run, il margine si sta assottigliando.
        if (margine < 10)
            TestContext.Out.WriteLine(
                $"ATTENZIONE margine ridotto a {margine:N1}s: valutare se alzare Language:TimeoutSeconds "
                + "o abbassare MaxCharsSummarize.");
    }

    // ---------------------------------------------------------------------------------------------
    // GRUPPO 5 — Credenziali sbagliate. Falliscono PRIMA di consumare quota, quindi costano zero: sono
    // i piu' economici del file e confermano che il 502 esce davvero invece di un 500 generico.
    // ---------------------------------------------------------------------------------------------

    [Test]
    public void Credenziali_ChiaveErrata_DiventaUpstreamServiceExceptionENonUnErroreGenerico()
    {
        var conChiaveErrata = new LanguageService(
            _endpoint, "chiave-palesemente-non-valida", NullLogger<LanguageService>.Instance);

        var eccezione = Assert.CatchAsync(
            async () => await conChiaveErrata.DetectLanguageAsync("testo qualsiasi"));

        // Deve restare nella famiglia mappata a 502: una chiave scaduta e' un problema del servizio a
        // monte, non della richiesta del client (che sarebbe 400) ne' un bug nostro (500).
        Assert.That(eccezione, Is.InstanceOf<UpstreamServiceException>());
        Assert.That(eccezione, Is.Not.TypeOf<UpstreamTimeoutException>(),
            "Un rifiuto di autenticazione non e' un timeout.");
    }

    [Test]
    public void Credenziali_EndpointInesistente_DiventaUpstreamServiceException()
    {
        var conEndpointSbagliato = new LanguageService(
            "https://endpoint-inesistente-portale-fatture.cognitiveservices.azure.com/",
            _key, NullLogger<LanguageService>.Instance, timeoutSeconds: 10);

        var eccezione = Assert.CatchAsync(
            async () => await conEndpointSbagliato.DetectLanguageAsync("testo qualsiasi"));

        Assert.That(eccezione, Is.InstanceOf<UpstreamServiceException>(),
            "Un host che non risolve deve comunque uscire come errore upstream gestito, non come 500.");
    }
}
