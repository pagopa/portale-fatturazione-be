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
/// Test ADVERSARIAL su `AzioneContestazioneQueryGetByIdNotificaHandler`: valori che l'handler puo'
/// legittimamente ricevere e che non prevede.
///
/// La matrice "buona" sta in <see cref="AzioneContestazioneMatriceUnitTests"/>. Qui si attaccano i
/// tre punti in cui l'handler **si fida**: il profilo che arriva dal claim, lo stato che arriva dal
/// database, e la conversione di anno/mese da stringa a `Int16`.
///
/// Convenzione delle asserzioni: dove il comportamento e' difettoso il test asserisce cio' che
/// dovrebbe succedere ed e' `[Ignore]` col motivo nell'attributo — suite verde, debito visibile.
/// </summary>
public class AzioneContestazioneAdversarialUnitTests
{
    // ---------------------------------------------------------------------------------------------
    // Il profilo, che arriva dal claim del token
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// L'if-chain sui profili termina con un `else` che solleva `DomainException`. Un profilo nuovo
    /// lato SelfCare — o un claim assente — non degrada a "nessuna azione permessa": fa fallire la
    /// pagina di dettaglio della notifica.
    /// </summary>
    [TestCase("XXX", TestName = "profilo · sconosciuto")]
    [TestCase("", TestName = "profilo · stringa vuota")]
    [TestCase(null, TestName = "profilo · assente")]
    public void ProfiloNonRiconosciuto_ShouldSollevareDomainException(string? profilo)
    {
        var ex = Assert.ThrowsAsync<DomainException>(async () => await Esegui(profilo, StatoContestazione.NonContestata));

        Assert.That(ex!.Message, Does.Contain("Non esiste il profilo"),
            "Un profilo nuovo introdotto lato SelfCare romperebbe la pagina invece di negare le azioni.");
    }

    /// <summary>
    /// ATTENZIONE Il confronto e' `==` fra stringhe, quindi **case-sensitive**: un `pa` minuscolo non e' un
    /// `PA`. La colonna `pfd.Enti.institutionType` e' popolata dall'onboarding SelfCare e il valore
    /// viaggia fino al claim senza normalizzazione — se un giorno arrivasse con un casing diverso,
    /// l'utente non vedrebbe "azioni non permesse" ma un errore.
    ///
    /// Vale la pena ricordare che e' la **stessa famiglia** del 404 fantasma su `api/fatture`, dove il
    /// casing di un GUID confrontato in C# scartava righe in silenzio (v. `docs/viste-endpoint.md`).
    /// </summary>
    [TestCase("pa")]
    [TestCase("Pa")]
    [TestCase("rec")]
    public void ProfiloConCasingDiverso_ShouldEssereRifiutato_Caratterizzazione(string profilo)
    {
        Assert.ThrowsAsync<DomainException>(async () => await Esegui(profilo, StatoContestazione.NonContestata),
            "Comportamento attuale: il confronto e' case-sensitive e il profilo non viene riconosciuto.");
    }

    [Test]
    public void ProfiloConSpaziIntorno_ShouldEssereRifiutato_Caratterizzazione()
    {
        // Nessun Trim() da nessuna parte della catena.
        Assert.ThrowsAsync<DomainException>(async () => await Esegui(" PA ", StatoContestazione.NonContestata));
    }

    // ---------------------------------------------------------------------------------------------
    // Lo stato, che arriva dal database
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Anche l'if-chain sugli stati termina con un `else` che solleva. Lo stato arriva da
    /// `pfw.FlagContestazione` via `INNER JOIN … ISNULL(t.FkIdFlagContestazione, 1)`: **basta
    /// aggiungere una riga a quella tabella di lookup** — cosa che non richiede alcuna modifica al
    /// backend — perche' le notifiche in quello stato facciano fallire la pagina di dettaglio.
    ///
    /// I tre rami di profilo hanno tre `else` distinti: il test li attraversa tutti, perche' sono
    /// copie separate e una potrebbe essere corretta senza le altre.
    /// </summary>
    [TestCase("PA", (short)0)]
    [TestCase("PA", (short)10)]
    [TestCase("PA", (short)-1)]
    [TestCase("REC", (short)10)]
    [TestCase("CON", (short)0)]
    [TestCase("SUP", (short)10)]
    [TestCase("FIN", (short)99)]
    public void StatoContestazioneFuoriDalDominio_ShouldSollevareDomainException(string profilo, short stato)
    {
        var ex = Assert.ThrowsAsync<DomainException>(async () =>
            await Esegui(profilo, StatoContestazione.NonContestata, personalizzaNotifica: n => n.StatoContestazione = stato));

        Assert.That(ex!.Message, Does.Contain("Non esiste stato valido"));
    }

    /// <summary>
    /// Contro-prova utile a delimitare il difetto precedente: con il lock di fatturazione attivo il
    /// controllo sullo stato **non viene nemmeno raggiunto**, perche' e' il primo ramo dell'if-chain.
    /// Una notifica gia' fatturata con uno stato anomalo non esplode.
    /// </summary>
    [Test]
    public async Task StatoFuoriDominio_MaNotificaFatturata_ShouldRispondereSenzaErrori()
    {
        var esito = await Esegui("PA", StatoContestazione.NonContestata,
            personalizzaNotifica: n => { n.StatoContestazione = 42; n.Fatturata = true; });

        Assert.That(esito!.CreazionePermessa, Is.False);
    }

    // ---------------------------------------------------------------------------------------------
    // La conversione di anno e mese: `Convert.ToInt16` su una stringa che arriva dal DB
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// ATTENZIONE DIFETTO. `Notifica.Anno` e `Notifica.Mese` sono **stringhe** (le colonne `year`/`month` sono
    /// `int` a DB e vengono convertite da Dapper), e l'handler fa `Convert.ToInt16(notifica.Anno)`
    /// senza alcuna protezione. `Int16` arriva a **32767**: un anno oltre quel valore — che la colonna
    /// `int` accetta senza problemi — produce una `OverflowException` non gestita, cioe' un 500.
    ///
    /// Non e' teorico quanto sembra: e' un dato che arriva dalla pipeline del team Data, e il backend
    /// lo tratta come sicuramente convertibile. Lo stesso vale per un valore non numerico
    /// (`FormatException`). Il tipo giusto qui e' `int`, che coprirebbe l'intero dominio della colonna.
    /// </summary>
    [Ignore("DIFETTO APERTO — Convert.ToInt16 su anno/mese non e' protetto: un valore oltre 32767 o "
        + "non numerico, che la colonna int di pfd.Notifiche accetta, diventa un 500 invece di una "
        + "risposta. Il test asserisce il comportamento CORRETTO (rispondere): togliere questo "
        + "attributo quando la conversione usera' int con un TryParse, o quando anno/mese saranno "
        + "tipizzati correttamente sull'entita'.")]
    [TestCase("40000", "3", TestName = "conversione · anno oltre Int16")]
    [TestCase("2026", "40000", TestName = "conversione · mese oltre Int16")]
    [TestCase("duemilaventisei", "3", TestName = "conversione · anno non numerico")]
    [TestCase("2026", "marzo", TestName = "conversione · mese non numerico")]
    public async Task AnnoOMeseNonConvertibili_ShouldRispondereSenzaErrori(string anno, string mese)
    {
        var esito = await Esegui("PA", StatoContestazione.NonContestata,
            personalizzaNotifica: n => { n.Anno = anno; n.Mese = mese; });

        Assert.That(esito, Is.Not.Null,
            "Un anno o un mese anomali provengono dai dati, non dall'utente: vanno gestiti, non fatti esplodere.");
    }

    /// <summary>
    /// Caso vicino ma con esito opposto, e vale la pena saperlo: anno/mese **null** non sollevano —
    /// `Convert.ToInt16((string)null)` restituisce `0`. Si finisce a cercare il calendario dell'anno 0,
    /// che non esiste, quindi nessuna azione permessa. Degradazione prudente, per caso.
    /// </summary>
    [Test]
    public async Task AnnoEMeseNulli_ShouldDegradareANessunPermesso()
    {
        var esito = await Esegui("PA", StatoContestazione.NonContestata,
            calendario: new CalendarioContestazione { Valid = false, ValidVerifica = false },
            personalizzaNotifica: n => { n.Anno = null; n.Mese = null; });

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.Not.Null, "Non solleva: Convert.ToInt16(null) vale 0.");
            Assert.That(esito!.CreazionePermessa, Is.False);
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Il calendario, di cui l'handler si fida
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// L'handler dereferenzia `calendario.ValidVerifica` **senza controllo di null**. Oggi non e'
    /// raggiungibile — `CalendarioContestazioneQueryHandler` restituisce sempre un oggetto, anche
    /// quando la riga non c'e' — ma i tipi non lo garantiscono: la query e' dichiarata
    /// `IRequest&lt;CalendarioContestazione&gt;` mentre l'handler la implementa restituendo
    /// `CalendarioContestazione?`, quindi un null passerebbe il compilatore.
    ///
    /// Test di confine: fissa che la protezione oggi e' **nel collaboratore, non qui**. Se un domani
    /// quel fallback venisse rimosso, il sintomo sarebbe una NullReferenceException in questo handler.
    /// </summary>
    [Test]
    public void CalendarioNull_ShouldSollevareNullReference_Caratterizzazione()
    {
        Assert.ThrowsAsync<NullReferenceException>(async () =>
            await Esegui("PA", StatoContestazione.NonContestata, calendario: null, calendarioEsplicitamenteNullo: true),
            "Nessuna guardia sul calendario: la prudenza sta tutta nel fallback del suo handler.");
    }

    // ---------------------------------------------------------------------------------------------
    // Il ruolo
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Il confronto e' `Ruolo == Ruolo.ADMIN`, cioe' `== "R/W"`. Qualunque altra cosa — un ruolo
    /// sconosciuto, vuoto o assente — azzera i permessi. E' il verso giusto: si nega per difetto.
    /// </summary>
    [TestCase("R", TestName = "ruolo · OPERATOR")]
    [TestCase("ADMIN", TestName = "ruolo · il NOME della costante, non il suo valore")]
    [TestCase("r/w", TestName = "ruolo · casing diverso")]
    [TestCase("", TestName = "ruolo · vuoto")]
    [TestCase(null, TestName = "ruolo · assente")]
    public async Task RuoloNonEsattamenteAdmin_ShouldNegareOgniAzione(string? ruolo)
    {
        var esito = await Esegui("PA", StatoContestazione.ContestataEnte, ruolo: ruolo);

        Assert.Multiple(() =>
        {
            Assert.That(esito!.ChiusuraPermessa, Is.False);
            Assert.That(esito.CreazionePermessa, Is.False);
            Assert.That(esito.RispostaPermessa, Is.False);
        });
    }

    // ---------------------------------------------------------------------------------------------

    private static async Task<AzioneNotificaDto?> Esegui(
        string? profilo,
        StatoContestazione stato,
        string? ruolo = Ruolo.ADMIN,
        CalendarioContestazione? calendario = null,
        Action<Notifica>? personalizzaNotifica = null,
        bool calendarioEsplicitamenteNullo = false)
    {
        var notifica = new Notifica
        {
            IdNotifica = "EVT-ADV",
            Anno = "2026",
            Mese = "3",
            StatoContestazione = (short)stato,
            Fatturata = false
        };
        personalizzaNotifica?.Invoke(notifica);

        var ctxNotifica = new Mock<IDbContext>();
        ctxNotifica.Setup(c => c.Query(It.IsAny<IQuery<Notifica?>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(notifica);
        var selfCare = new Mock<ISelfCareDbContextFactory>();
        selfCare.Setup(f => f.Create(It.IsAny<bool>(), It.IsAny<System.Data.IsolationLevel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ctxNotifica.Object);

        var ctxContestazione = new Mock<IDbContext>();
        ctxContestazione.Setup(c => c.Query(It.IsAny<IQuery<Contestazione?>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Contestazione?)null);
        var fatture = new Mock<IFattureDbContextFactory>();
        fatture.Setup(f => f.Create(It.IsAny<bool>(), It.IsAny<System.Data.IsolationLevel>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(ctxContestazione.Object);

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CalendarioContestazioneQueryGet>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(calendarioEsplicitamenteNullo
                    ? null!
                    : calendario ?? new CalendarioContestazione { Valid = true, ValidVerifica = true });

        var handler = new AzioneContestazioneQueryGetByIdNotificaHandler(
            selfCare.Object,
            fatture.Object,
            mediator.Object,
            new Mock<IStringLocalizer<Localization>>().Object,
            new Mock<ILogger<AzioneContestazioneQueryGetByIdNotificaHandler>>().Object);

        var auth = new AuthenticationInfo { Profilo = profilo, Ruolo = ruolo, IdEnte = "ente-test" };
        return await handler.Handle(new AzioneContestazioneQueryGetByIdNotifica(auth, "EVT-ADV"), default);
    }
}
