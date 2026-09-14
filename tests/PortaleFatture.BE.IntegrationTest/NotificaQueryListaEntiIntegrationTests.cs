
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// `NotificaQueryGetByListEntiPersistence` — la ricerca notifiche lato admin, cioe' la query piu'
/// filtrata del progetto: anno, mese, enti, recapitisti, consolidatori, prodotto, CAP, profilo, IUN,
/// destinatario, tipo notifica, stato contestazione, piu' ordinamento e paginazione.
///
/// Perche' servono test su DB e non unit test: il WHERE viene composto **a stringhe dentro Execute**,
/// quindi non e' isolabile. E il rischio non e' un errore di sintassi — quello si vede subito — ma un
/// filtro che seleziona **piu' righe del dovuto**: su una ricerca notifiche significa mostrare
/// all'operatore dati di un periodo o di un ente che non ha chiesto.
///
/// Seed: ente1 / TOKEN-E1, periodo 2026/3, tre notifiche che coprono i tre casi che i filtri devono
/// saper distinguere (v. tests/Data/notifiche.sql):
///   EVT-3001  AR   (analogica)   CAP 00100  REC-3001  non contestata
///   EVT-3002  890  (analogica)   CAP 20100  REC-3002  CONTESTATA, flag 3
///   EVT-3003  digitale (paper_product_type NULL)      non contestata
/// </summary>
public class NotificaQueryListaEntiIntegrationTests
{
    private const string Ente = "11111111-1111-1111-1111-111111111111";
    private const int Anno = 2026;
    private const int Mese = 3;

    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    // ---------------------------------------------------------------------------------------------
    // Periodo, che e' l'unico filtro sempre presente
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task Periodo_ShouldRestituireLeTreNotificheDelMese_EIlCountCoerente()
    {
        var r = await Cerca(q => { });

        Assert.Multiple(() =>
        {
            Assert.That(Ids(r), Is.EquivalentTo(new[] { "EVT-3001", "EVT-3002", "EVT-3003" }));
            Assert.That(r!.Count, Is.EqualTo(3),
                "Il Count arriva da una seconda query con lo STESSO where: se diverge dalle righe, "
                + "le due SELECT non stanno piu' filtrando allo stesso modo.");
        });
    }

    [Test]
    public async Task PeriodoSenzaDati_ShouldRestituireListaVuota_ECountZero()
    {
        var r = await Cerca(q => { q.AnnoValidita = 1999; q.MeseValidita = 1; });

        Assert.Multiple(() =>
        {
            Assert.That(r!.Notifiche, Is.Empty);
            Assert.That(r.Count, Is.Zero);
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Filtri di identita'
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task FiltroEnte_ShouldRestituireSoloLeNotificheDiQuellEnte()
    {
        var mie = await Cerca(q => q.EntiIds = [Ente]);
        var altrui = await Cerca(q => q.EntiIds = ["99999999-0000-0000-0000-000000000000"]);

        Assert.Multiple(() =>
        {
            Assert.That(Ids(mie), Has.Count.EqualTo(3));
            Assert.That(altrui!.Notifiche, Is.Empty, "Un ente senza notifiche non deve vedere quelle altrui.");
        });
    }

    [Test]
    public async Task FiltroIun_ShouldSelezionareLaSingolaNotifica()
    {
        var r = await Cerca(q => q.Iun = "IUN-3002");

        Assert.That(Ids(r), Is.EquivalentTo(new[] { "EVT-3002" }));
    }

    [Test]
    public async Task FiltroRecipientId_ShouldSelezionareLaSingolaNotifica()
    {
        var r = await Cerca(q => q.RecipientId = "REC-3001");

        Assert.That(Ids(r), Is.EquivalentTo(new[] { "EVT-3001" }));
    }

    [Test]
    public async Task FiltroCap_ShouldSelezionareSoloQuelCap()
    {
        var r = await Cerca(q => q.Cap = "00100");

        Assert.That(Ids(r), Is.EquivalentTo(new[] { "EVT-3001" }),
            "La digitale non ha CAP (NULL) e non deve comparire.");
    }

    // ---------------------------------------------------------------------------------------------
    // Tipo notifica: il ramo con la logica meno ovvia
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task FiltroDigitali_ShouldSelezionareLeNotificheSenzaPaperProductType()
    {
        // Digitali non ha un codice: la persistence lo traduce in "paper_product_type IS NULL"
        // (v. NotificaFiltriUnitTests). Chiedendo solo Digitali la lista di codici resta vuota.
        var r = await Cerca(q => q.TipoNotifica = [TipoNotifica.Digitali]);

        Assert.That(Ids(r), Is.EquivalentTo(new[] { "EVT-3003" }));
    }

    [Test]
    public async Task FiltroAnalogico_ShouldSelezionareSoloQuelCodice()
    {
        var r = await Cerca(q => q.TipoNotifica = [TipoNotifica.Analogico890]);

        Assert.That(Ids(r), Is.EquivalentTo(new[] { "EVT-3002" }),
            "Senza Digitali fra i richiesti non deve scattare il ramo IS NULL: la digitale resta fuori.");
    }

    [Test]
    public async Task FiltroMistoDigitaleEAnalogico_ShouldUnireIDueInsiemi()
    {
        var r = await Cerca(q => q.TipoNotifica = [TipoNotifica.Digitali, TipoNotifica.AnalogicoARNazionali]);

        Assert.That(Ids(r), Is.EquivalentTo(new[] { "EVT-3001", "EVT-3003" }),
            "AR dall'IN, la digitale dal ramo IS NULL. L'890 deve restare escluso.");
    }

    // ---------------------------------------------------------------------------------------------
    // Stato contestazione: "non contestata" non e' un valore, e' l'assenza di riga
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task StatoNonContestata_ShouldSelezionareLeNotificheSenzaRigaDiContestazione()
    {
        // Lo stato 1 non esiste in pfw.Contestazioni: e' il default di chi non ha una riga. La
        // persistence lo traduce infatti in "t.FKIdFlagContestazione is NULL", non in un IN.
        var r = await Cerca(q => q.StatoContestazione = [1]);

        Assert.That(Ids(r), Is.EquivalentTo(new[] { "EVT-3001", "EVT-3003" }));
    }

    [Test]
    public async Task StatoContestata_ShouldSelezionareSoloQuellaConLaRiga()
    {
        var r = await Cerca(q => q.StatoContestazione = [3]);

        Assert.That(Ids(r), Is.EquivalentTo(new[] { "EVT-3002" }));
    }

    [Test]
    public async Task StatoMistoNonContestataEContestata_ShouldUnireIDueInsiemi()
    {
        // Ramo con l'OR: "IS NULL OR IN (...)". E' il caso che distingue questa query da un IN secco.
        var r = await Cerca(q => q.StatoContestazione = [1, 3]);

        Assert.That(Ids(r), Has.Count.EqualTo(3));
    }

    // ---------------------------------------------------------------------------------------------
    // Paginazione e ordinamento
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task Paginazione_ShouldLimitareLeRigheMaNonIlCount()
    {
        var pagina1 = await Cerca(q => { q.Page = 1; q.Size = 2; });
        var pagina2 = await Cerca(q => { q.Page = 2; q.Size = 2; });

        Assert.Multiple(() =>
        {
            Assert.That(Ids(pagina1), Has.Count.EqualTo(2));
            Assert.That(Ids(pagina2), Has.Count.EqualTo(1));
            Assert.That(pagina1!.Count, Is.EqualTo(3),
                "Il Count e' il totale del filtro, non della pagina: e' cio' che permette al client di "
                + "sapere quante pagine ci sono.");
            Assert.That(Ids(pagina1).Intersect(Ids(pagina2)), Is.Empty, "Le pagine non devono sovrapporsi.");
        });
    }

    [Test]
    public async Task Ordinamento_PerDataAscendenteEDiscendente_ShouldEssereInverso()
    {
        var asc = await Cerca(q => { q.ColumName = "data"; q.OrderDir = "1"; });
        var desc = await Cerca(q => { q.ColumName = "data"; q.OrderDir = "2"; });

        Assert.That(Ids(asc), Is.EqualTo(Ids(desc).AsEnumerable().Reverse().ToList()),
            "ASC e DESC sullo stesso insieme devono dare l'ordine opposto.");
    }

    [Test]
    public async Task Ordinamento_ColonnaSconosciuta_ShouldRicadereSullOrdineDiDefault()
    {
        // Qualunque colonna diversa da "data" finisce nel ramo else: nessun errore, ordine per anno/mese.
        var r = await Cerca(q => { q.ColumName = "colonna-che-non-esiste"; q.OrderDir = "1"; });

        Assert.That(Ids(r), Has.Count.EqualTo(3), "Deve rispondere comunque, senza errori.");
    }

    // ---------------------------------------------------------------------------------------------
    // Filtri che nel seed non hanno corrispondenza: verificano che ESCLUDANO
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task FiltriSuColonneNonPopolateNelSeed_ShouldEscludereTutto()
    {
        // Nel seed institutionType e product sono NULL, quindi qualunque valore esplicito esclude
        // tutto. Non e' un difetto: e' la prova che il filtro viene davvero applicato.
        var perProfilo = await Cerca(q => q.Profilo = "PA");
        var perProdotto = await Cerca(q => q.Prodotto = "prod-pn");
        var perRecapitista = await Cerca(q => q.Recapitisti = ["Recapitista Inesistente"]);
        var perConsolidatore = await Cerca(q => q.Consolidatori = ["Consolidatore Inesistente"]);

        Assert.Multiple(() =>
        {
            Assert.That(perProfilo!.Notifiche, Is.Empty);
            Assert.That(perProdotto!.Notifiche, Is.Empty);
            Assert.That(perRecapitista!.Notifiche, Is.Empty);
            Assert.That(perConsolidatore!.Notifiche, Is.Empty);
        });
    }

    [Test]
    public async Task FiltroRecapitistaEsistente_ShouldSelezionareLeAnalogiche()
    {
        // Le due analogiche hanno Recapitista valorizzato, la digitale no.
        var r = await Cerca(q => q.Recapitisti = ["Recapitista Uno"]);

        Assert.That(Ids(r), Is.EquivalentTo(new[] { "EVT-3001", "EVT-3002" }));
    }

    // ---------------------------------------------------------------------------------------------

    /// <summary>Ricerca sul periodo del seed, con le personalizzazioni del caso.</summary>
    private async Task<Infrastructure.Common.SEND.Notifiche.Dto.NotificaDto?> Cerca(
        Action<NotificaQueryGetByListaEnti> personalizza)
    {
        var query = new NotificaQueryGetByListaEnti(Auth())
        {
            AnnoValidita = Anno,
            MeseValidita = Mese
        };
        personalizza(query);
        return await _handler.Send(query);
    }

    private static List<string> Ids(Infrastructure.Common.SEND.Notifiche.Dto.NotificaDto? r) =>
        r?.Notifiche?.Select(x => x.IdNotifica ?? string.Empty).ToList() ?? [];

    private static AuthenticationInfo Auth() => new()
    {
        Id = "integration-test-notifiche",
        IdEnte = Ente,
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };
}
