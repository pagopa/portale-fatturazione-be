using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche.Dto;
using PortaleFatture.BE.Core.Entities.SEND.Scadenziari;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.QueryHandlers;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// `AzioneContestazioneQueryGetByIdNotificaHandler` — l'handler che decide **quali azioni sono
/// permesse** su una notifica: `ChiusuraPermessa`, `CreazionePermessa`, `RispostaPermessa`. E' il
/// punto in cui il ciclo di vita della Contestazione descritto in `docs/business-contestazioni.md`
/// diventa codice: chi puo' contestare, chi puo' rispondere, chi puo' chiudere e in quale finestra.
///
/// Perche' UNIT e non integration: la decisione e' una funzione pura di cinque assi —
/// **profilo × stato contestazione × fatturata × calendario × ruolo** — e i tre collaboratori
/// (le due factory, il mediator per il calendario) sono interfacce. Coprire la matrice su DB
/// significherebbe seedare decine di combinazioni di stato; qui si esprimono direttamente.
///
/// I tre rami di profilo sono **copie quasi identiche con differenze piccole e sostanziali**: e'
/// esattamente la forma di codice in cui una modifica applicata a un ramo solo passa inosservata.
/// Questa matrice serve a impedirlo.
/// </summary>
public class AzioneContestazioneMatriceUnitTests
{
    // I valori di Profilo sono campi `static`, non `const`: non si possono usare in un [TestCase].
    // Si usano quindi i letterali, che sono anche i valori reali del claim e della colonna
    // `pfd.Enti.institutionType`.
    private const string Recapitista = "REC";
    private const string Consolidatore = "CON";
    private const string EntePA = "PA";
    private const string InternoAssistenza = "SUP";

    // ---------------------------------------------------------------------------------------------
    // Ramo 1 — Recapitista e Consolidatore: possono SOLO rispondere
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Recapitista e Consolidatore non aprono e non chiudono contestazioni: intervengono solo nel
    /// dialogo. Coerente con `business-contestazioni.md`, che li descrive come soggetti che
    /// *rispondono* — e che nella pratica non usano nemmeno questa possibilita', perche' media
    /// sempre il supporto SEND.
    /// </summary>
    [TestCase(Recapitista, StatoContestazione.NonContestata, false)]
    [TestCase(Recapitista, StatoContestazione.Annullata, false)]
    [TestCase(Recapitista, StatoContestazione.ContestataEnte, true)]
    [TestCase(Recapitista, StatoContestazione.RispostaSend, true)]
    [TestCase(Recapitista, StatoContestazione.RispostaRecapitista, true)]
    [TestCase(Recapitista, StatoContestazione.RispostaConsolidatore, true)]
    [TestCase(Recapitista, StatoContestazione.RispostaEnte, true)]
    [TestCase(Recapitista, StatoContestazione.Accettata, false)]
    [TestCase(Recapitista, StatoContestazione.Chiusa, false)]
    [TestCase(Consolidatore, StatoContestazione.ContestataEnte, true)]
    [TestCase(Consolidatore, StatoContestazione.NonContestata, false)]
    [TestCase(Consolidatore, StatoContestazione.Chiusa, false)]
    public async Task RecapitistaEConsolidatore_ShouldPotereSoloRispondere(
        string profilo, StatoContestazione stato, bool rispostaAttesa)
    {
        var esito = await Esegui(profilo, stato);

        Assert.Multiple(() =>
        {
            Assert.That(esito!.RispostaPermessa, Is.EqualTo(rispostaAttesa), "RispostaPermessa");
            Assert.That(esito.ChiusuraPermessa, Is.False, "Non chiudono mai una contestazione.");
            Assert.That(esito.CreazionePermessa, Is.False, "Non aprono mai una contestazione.");
        });
    }

    /// <summary>
    /// Dentro il ramo Recapitista/Consolidatore `creazionePermessa` viene calcolato — e per lo stato
    /// `NonContestata` vale addirittura `true` — ma il DTO restituito **hardcoda `false`** per
    /// chiusura e creazione, ignorando le variabili appena riempite (il commento nel sorgente dice
    /// "sempre false, gia' calcolata").
    ///
    /// Non e' un difetto di comportamento, il risultato e' quello voluto: e' **codice morto che
    /// sembra vivo**. Chi legge quel ramo per capire cosa fa un Recapitista trova un `= true` che non
    /// ha alcun effetto. Il test fissa il comportamento reale, cosi' se un domani qualcuno collegasse
    /// quelle variabili al DTO "per coerenza", il cambiamento non passerebbe inosservato.
    /// </summary>
    [Test]
    public async Task RecapitistaSuNonContestata_LaCreazioneCalcolataATrue_ShouldRestareFalseNelRisultato()
    {
        var esito = await Esegui(Recapitista, StatoContestazione.NonContestata);

        Assert.That(esito!.CreazionePermessa, Is.False,
            "Il ramo calcola creazionePermessa = true e poi lo scarta: il DTO hardcoda false.");
    }

    // ---------------------------------------------------------------------------------------------
    // Ramo 2 — profili Ente (PA, GSP, SCP, PSP, AS, SA, PT): aprono, modificano e chiudono
    // ---------------------------------------------------------------------------------------------

    [TestCase(StatoContestazione.NonContestata, false, true, false)]
    [TestCase(StatoContestazione.Annullata, false, false, false)]
    [TestCase(StatoContestazione.ContestataEnte, true, true, false)]
    [TestCase(StatoContestazione.RispostaSend, true, false, true)]
    [TestCase(StatoContestazione.RispostaRecapitista, true, false, true)]
    [TestCase(StatoContestazione.RispostaConsolidatore, true, false, true)]
    [TestCase(StatoContestazione.RispostaEnte, true, false, true)]
    [TestCase(StatoContestazione.Accettata, false, false, false)]
    [TestCase(StatoContestazione.Chiusa, false, false, false)]
    public async Task ProfiliEnte_ShouldSeguireIlCicloDiVitaDellaContestazione(
        StatoContestazione stato, bool chiusura, bool creazione, bool risposta)
    {
        var esito = await Esegui(EntePA, stato);

        Assert.Multiple(() =>
        {
            Assert.That(esito!.ChiusuraPermessa, Is.EqualTo(chiusura), "ChiusuraPermessa");
            Assert.That(esito.CreazionePermessa, Is.EqualTo(creazione), "CreazionePermessa");
            Assert.That(esito.RispostaPermessa, Is.EqualTo(risposta), "RispostaPermessa");
        });
    }

    /// <summary>
    /// Tutti e sette i profili SelfCare condividono lo stesso ramo: il test lo fissa, perche' e' una
    /// lista scritta a mano nell'`if` e un profilo dimenticato finirebbe nell'`else` finale, cioe'
    /// in una `DomainException`.
    /// </summary>
    [TestCase("PA")]
    [TestCase("GSP")]
    [TestCase("SCP")]
    [TestCase("PSP")]
    [TestCase("AS")]
    [TestCase("SA")]
    [TestCase("PT")]
    public async Task TuttiIProfiliSelfCare_ShouldCondividereLoStessoRamo(string profilo)
    {
        var esito = await Esegui(profilo, StatoContestazione.NonContestata);

        Assert.That(esito!.CreazionePermessa, Is.True, $"Il profilo {profilo} deve poter contestare.");
    }

    // ---------------------------------------------------------------------------------------------
    // Ramo 3 — profili interni pagoPA (PRO, FIN, SUP): rispondono e chiudono, non aprono
    // ---------------------------------------------------------------------------------------------

    [TestCase(StatoContestazione.NonContestata, false, false)]
    [TestCase(StatoContestazione.Annullata, false, false)]
    [TestCase(StatoContestazione.Accettata, false, false)]
    [TestCase(StatoContestazione.Chiusa, false, false)]
    [TestCase(StatoContestazione.ContestataEnte, true, true)]
    [TestCase(StatoContestazione.RispostaSend, true, true)]
    [TestCase(StatoContestazione.RispostaRecapitista, true, true)]
    [TestCase(StatoContestazione.RispostaConsolidatore, true, true)]
    [TestCase(StatoContestazione.RispostaEnte, true, true)]
    public async Task ProfiliInterniPagoPA_ShouldPotereChiudereERispondere_MaiCreare(
        StatoContestazione stato, bool chiusura, bool risposta)
    {
        var esito = await Esegui(InternoAssistenza, stato);

        Assert.Multiple(() =>
        {
            Assert.That(esito!.ChiusuraPermessa, Is.EqualTo(chiusura), "ChiusuraPermessa");
            Assert.That(esito.RispostaPermessa, Is.EqualTo(risposta), "RispostaPermessa");
            Assert.That(esito.CreazionePermessa, Is.False,
                "Il supporto interno non apre contestazioni: quelle le apre l'Ente.");
        });
    }

    [TestCase("PRO")]
    [TestCase("FIN")]
    [TestCase("SUP")]
    public async Task TuttiIProfiliInterni_ShouldCondividereLoStessoRamo(string profilo)
    {
        var esito = await Esegui(profilo, StatoContestazione.ContestataEnte);

        Assert.That(esito!.ChiusuraPermessa, Is.True);
    }

    /// <summary>
    /// Il supporto SEND puo' chiudere una contestazione **anche fuori dalla finestra di apertura**:
    /// il suo unico vincolo e' `ValidVerifica`. E' la traduzione in codice della nota di
    /// `business-contestazioni.md` — "le finestre vincolano solo l'aderente" — e spiega perche' nei
    /// dati storici si trovano chiusure molto oltre la finestra nominale.
    /// </summary>
    [Test]
    public async Task SupportoInterno_ShouldPotereChiudereAncheFuoriDallaFinestraDiApertura()
    {
        var esito = await Esegui(InternoAssistenza, StatoContestazione.ContestataEnte,
            calendario: Calendario(valid: false, validVerifica: true));

        Assert.That(esito!.ChiusuraPermessa, Is.True,
            "Conta solo ValidVerifica: la finestra di apertura (Valid) non lo riguarda.");
    }

    // ---------------------------------------------------------------------------------------------
    // Il lock della fatturazione: una notifica fatturata non si tocca piu'
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// `TipologiaFattura` funziona da lock (`business-contestazioni.md`): inclusa in un ciclo di
    /// fatturazione, la notifica e' archiviata e non e' piu' contestabile. Il controllo e' il **primo**
    /// dell'if-chain in tutti e tre i rami, quindi vince su qualunque stato — anche su uno in cui
    /// l'azione sarebbe altrimenti permessa.
    /// </summary>
    [TestCase(EntePA)]
    [TestCase(Recapitista)]
    [TestCase(InternoAssistenza)]
    public async Task NotificaFatturata_ShouldBloccareOgniAzione_QualunqueSiaLoStato(string profilo)
    {
        // ContestataEnte: senza il lock, l'Ente potrebbe chiudere e il supporto rispondere.
        var esito = await Esegui(profilo, StatoContestazione.ContestataEnte,
            personalizzaNotifica: n => n.Fatturata = true);

        Assert.Multiple(() =>
        {
            Assert.That(esito!.ChiusuraPermessa, Is.False);
            Assert.That(esito.CreazionePermessa, Is.False);
            Assert.That(esito.RispostaPermessa, Is.False);
        });
    }

    /// <summary>
    /// Le due condizioni del lock sono in **OR**: basta la `TipologiaFattura` valorizzata, anche con
    /// `Fatturata` a false o null. E' il caso reale della notifica gia' assegnata a un ciclo ma non
    /// ancora fatturata.
    /// </summary>
    [TestCase(true, null, TestName = "lock · Fatturata = true")]
    [TestCase(false, "PRIMO SALDO", TestName = "lock · TipologiaFattura valorizzata, Fatturata = false")]
    [TestCase(null, "PRIMO SALDO", TestName = "lock · TipologiaFattura valorizzata, Fatturata = null")]
    public async Task IlLockScatta_SeFatturataOppureSeCiSonoTipologiaFattura(bool? fatturata, string? tipologia)
    {
        var esito = await Esegui(EntePA, StatoContestazione.NonContestata,
            personalizzaNotifica: n => { n.Fatturata = fatturata; n.TipologiaFattura = tipologia; });

        Assert.That(esito!.CreazionePermessa, Is.False);
    }

    [Test]
    public async Task SenzaLock_LaNotificaNonContestata_ShouldEssereContestabile()
    {
        // Contro-prova del test precedente: senza lock la stessa notifica e' contestabile.
        var esito = await Esegui(EntePA, StatoContestazione.NonContestata,
            personalizzaNotifica: n => { n.Fatturata = false; n.TipologiaFattura = null; });

        Assert.That(esito!.CreazionePermessa, Is.True);
    }

    // ---------------------------------------------------------------------------------------------
    // I due cancelli finali: calendario e ruolo
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Ogni permesso e' moltiplicato per `Ruolo == ADMIN`: un `OPERATOR` e' in sola lettura e non puo'
    /// fare **nulla**, per quanto lo stato lo consentirebbe.
    /// </summary>
    [TestCase(EntePA, StatoContestazione.NonContestata)]
    [TestCase(EntePA, StatoContestazione.RispostaSend)]
    [TestCase(Recapitista, StatoContestazione.ContestataEnte)]
    [TestCase(InternoAssistenza, StatoContestazione.ContestataEnte)]
    public async Task RuoloOperator_ShouldAzzerareOgniPermesso(string profilo, StatoContestazione stato)
    {
        var esito = await Esegui(profilo, stato, ruolo: Ruolo.OPERATOR);

        Assert.Multiple(() =>
        {
            Assert.That(esito!.ChiusuraPermessa, Is.False);
            Assert.That(esito.CreazionePermessa, Is.False);
            Assert.That(esito.RispostaPermessa, Is.False);
        });
    }

    /// <summary>
    /// ⚠️ Il test che distingue i due cancelli temporali, ed e' l'unico posto in cui la differenza si
    /// vede: per un profilo Ente **`CreazionePermessa` e' governata da `Valid`** (la finestra di
    /// apertura, `DataInizio`–`DataFine`) mentre chiusura e risposta sono governate da `ValidVerifica`
    /// (`DataInizio`–`DataVerifica`). Sono finestre diverse e non vanno confuse.
    /// </summary>
    [Test]
    public async Task ProfiloEnte_LaCreazione_ShouldSeguireValid_NonValidVerifica()
    {
        var soloApertura = await Esegui(EntePA, StatoContestazione.NonContestata,
            calendario: Calendario(valid: true, validVerifica: false));
        var soloVerifica = await Esegui(EntePA, StatoContestazione.NonContestata,
            calendario: Calendario(valid: false, validVerifica: true));

        Assert.Multiple(() =>
        {
            Assert.That(soloApertura!.CreazionePermessa, Is.True,
                "Finestra di apertura aperta: si puo' contestare.");
            Assert.That(soloVerifica!.CreazionePermessa, Is.False,
                "Finestra di apertura chiusa: non si contesta piu', anche se la verifica e' aperta.");
        });
    }

    [Test]
    public async Task ProfiloEnte_ChiusuraERisposta_ShouldSeguireValidVerifica()
    {
        var esito = await Esegui(EntePA, StatoContestazione.RispostaSend,
            calendario: Calendario(valid: false, validVerifica: true));

        Assert.Multiple(() =>
        {
            Assert.That(esito!.ChiusuraPermessa, Is.True);
            Assert.That(esito.RispostaPermessa, Is.True);
        });
    }

    /// <summary>
    /// Calendario assente per il periodo — caso reale quando il feed mensile del team Data non ha
    /// ancora scritto la riga: `CalendarioContestazioneQueryHandler` restituisce un calendario con
    /// entrambi i flag a `false` invece di `null`. Il risultato e' che **nessuna azione e' permessa**,
    /// che e' il fallback prudente giusto.
    /// </summary>
    [Test]
    public async Task CalendarioAssente_ShouldNegareOgniAzione()
    {
        var esito = await Esegui(EntePA, StatoContestazione.ContestataEnte,
            calendario: Calendario(valid: false, validVerifica: false));

        Assert.Multiple(() =>
        {
            Assert.That(esito!.ChiusuraPermessa, Is.False);
            Assert.That(esito.CreazionePermessa, Is.False);
            Assert.That(esito.RispostaPermessa, Is.False);
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Il payload di contorno
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task IlRisultato_ShouldPortareNotificaContestazioneECalendario()
    {
        // Il DTO non serve solo a dire cosa e' permesso: la UI ci legge anche le note della
        // contestazione e le date del calendario. Se uno dei tre passasse null, la pagina di
        // dettaglio si svuoterebbe senza errori.
        var contestazione = new Contestazione { IdNotifica = "EVT-1", NoteEnte = "nota dell'ente" };

        var esito = await Esegui(EntePA, StatoContestazione.ContestataEnte, contestazione: contestazione);

        Assert.Multiple(() =>
        {
            Assert.That(esito!.Notifica, Is.Not.Null);
            Assert.That(esito.Contestazione!.NoteEnte, Is.EqualTo("nota dell'ente"));
            Assert.That(esito.Calendario, Is.Not.Null);
        });
    }

    [Test]
    public async Task ContestazioneAssente_ShouldEssereAmmessa()
    {
        // Su una notifica NON contestata non esiste riga in pfw.Contestazioni: la persistence torna
        // null ed e' corretto, non un errore.
        var esito = await Esegui(EntePA, StatoContestazione.NonContestata, contestazione: null);

        Assert.Multiple(() =>
        {
            Assert.That(esito!.Contestazione, Is.Null);
            Assert.That(esito.CreazionePermessa, Is.True);
        });
    }

    [Test]
    public void NotificaInesistente_ShouldSollevareDomainException()
    {
        var ex = Assert.ThrowsAsync<DomainException>(async () =>
            await Esegui(EntePA, StatoContestazione.NonContestata, notificaEsplicitamenteNulla: true));

        Assert.That(ex!.Message, Does.Contain("EVT-TEST"),
            "Il messaggio deve citare l'id cercato: e' quello che finisce nei log.");
    }

    // ---------------------------------------------------------------------------------------------
    // Costruzione dello scenario
    // ---------------------------------------------------------------------------------------------

    private const string IdNotifica = "EVT-TEST";

    private static CalendarioContestazione Calendario(bool valid = true, bool validVerifica = true) => new()
    {
        Valid = valid,
        ValidVerifica = validVerifica,
        AnnoContestazione = 2026,
        MeseContestazione = 3,
        Adesso = new DateTime(2026, 3, 15)
    };

    private static Notifica NotificaBase(StatoContestazione stato) => new()
    {
        IdNotifica = IdNotifica,
        Anno = "2026",
        Mese = "3",
        StatoContestazione = (short)stato,
        Fatturata = false,
        TipologiaFattura = null
    };

    /// <summary>
    /// Costruisce l'handler con i tre collaboratori simulati ed esegue la query. Tutto cio' che il
    /// test non specifica ha un default "permissivo" (calendario aperto, ruolo ADMIN, notifica non
    /// fatturata), cosi' ogni caso dichiara solo l'asse che sta esercitando.
    /// </summary>
    private static async Task<AzioneNotificaDto?> Esegui(
        string profilo,
        StatoContestazione stato,
        string ruolo = Ruolo.ADMIN,
        CalendarioContestazione? calendario = null,
        Contestazione? contestazione = null,
        Notifica? notifica = null,
        Action<Notifica>? personalizzaNotifica = null,
        bool notificaEsplicitamenteNulla = false)
    {
        notifica ??= notificaEsplicitamenteNulla ? null : NotificaBase(stato);
        personalizzaNotifica?.Invoke(notifica!);

        var handler = new AzioneContestazioneQueryGetByIdNotificaHandler(
            FactorySelfCare(notifica),
            FactoryFatture(contestazione),
            Mediator(calendario ?? Calendario()),
            new Mock<IStringLocalizer<Localization>>().Object,
            new Mock<ILogger<AzioneContestazioneQueryGetByIdNotificaHandler>>().Object);

        var auth = new AuthenticationInfo { Profilo = profilo, Ruolo = ruolo, IdEnte = "ente-test" };
        return await handler.Handle(new AzioneContestazioneQueryGetByIdNotifica(auth, IdNotifica), default);
    }

    private static ISelfCareDbContextFactory FactorySelfCare(Notifica? notifica)
    {
        var ctx = new Mock<IDbContext>();
        ctx.Setup(c => c.Query(It.IsAny<IQuery<Notifica?>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(notifica);
        var factory = new Mock<ISelfCareDbContextFactory>();
        factory.Setup(f => f.Create(It.IsAny<bool>(), It.IsAny<System.Data.IsolationLevel>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(ctx.Object);
        return factory.Object;
    }

    private static IFattureDbContextFactory FactoryFatture(Contestazione? contestazione)
    {
        var ctx = new Mock<IDbContext>();
        ctx.Setup(c => c.Query(It.IsAny<IQuery<Contestazione?>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(contestazione);
        var factory = new Mock<IFattureDbContextFactory>();
        factory.Setup(f => f.Create(It.IsAny<bool>(), It.IsAny<System.Data.IsolationLevel>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(ctx.Object);
        return factory.Object;
    }

    private static IMediator Mediator(CalendarioContestazione calendario)
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CalendarioContestazioneQueryGet>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(calendario);
        return mediator.Object;
    }
}
