using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Test ADVERSARIAL su `NotificaQueryGetByListEntiPersistence`: input che il chiamante puo'
/// legittimamente costruire ma che la composizione del WHERE non prevede.
///
/// L'attenzione qui e' su un punto preciso: il WHERE viene costruito **concatenando stringhe**, e la
/// parola `WHERE` viene emessa **solo dal filtro sull'anno**. Tutti gli altri filtri aggiungono
/// ` AND ...`.
///
/// L'ipotesi da cui erano partiti questi test era che senza anno si ottenesse SQL invalido. **Non e'
/// cosi', ed e' peggio**: l' ` AND` si attacca alla ON dell'ultima LEFT JOIN e il filtro viene
/// silenziosamente ignorato, restituendo tutte le righe. Il dettaglio nel primo test.
///
/// Convenzione delle asserzioni, la stessa dei test sulle SP di Gestione Fatture: dove il
/// comportamento e' **difettoso** il test asserisce cio' che dovrebbe succedere ed e' marcato
/// `[Ignore]` col motivo nell'attributo — la suite resta verde ma il debito resta visibile, e chi
/// corregge il difetto lo chiude semplicemente togliendo l'attributo. Dove invece il comportamento e'
/// solo **scomodo ma voluto** (liste vuote che non filtrano, limite di parametri di SQL Server) il
/// test e' attivo e fissa cosa succede oggi.
/// </summary>
public class NotificaQueryListaEntiAdversarialTests
{
    private const string Ente = "11111111-1111-1111-1111-111111111111";

    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    // ---------------------------------------------------------------------------------------------
    // Il WHERE che non c'e'
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// DIFETTO, e non e' un errore di sintassi: e' peggio.
    ///
    /// La parola `WHERE` viene emessa **solo** da `if (anno.HasValue)`; ogni altro filtro aggiunge
    /// ` AND ...`. Senza anno, quell' ` AND` non finisce in un WHERE inesistente: si attacca all'ultima
    /// riga della SELECT, che e' `LEFT JOIN pfw.TipoContestazione a ON ...`. Il filtro diventa cosi'
    /// **parte della condizione ON di una LEFT JOIN** — dove non elimina righe, decide solo se la
    /// tabella joinata viene agganciata.
    ///
    /// Risultato: la query non fallisce e non restituisce meno righe. **Le restituisce tutte, come se
    /// il filtro non fosse stato passato.** Cercare una notifica per IUN senza indicare l'anno — l'uso
    /// piu' naturale che esista — restituisce l'intero insieme invece di quella notifica.
    ///
    /// Ogni caso usa un valore che NON corrisponde a nulla: la risposta corretta e' zero righe. Oggi
    /// tornano tutte e 3 le notifiche del seed, quindi il test e' ROSSO ed e' `[Ignore]`.
    /// </summary>
    [Ignore("DIFETTO APERTO — un filtro passato senza AnnoValidita non filtra nulla: l' AND viene "
        + "assorbito nella ON dell'ultima LEFT JOIN. Cercare per IUN senza indicare l'anno restituisce "
        + "TUTTE le notifiche invece di quella richiesta, senza errore. Il test asserisce il "
        + "comportamento CORRETTO (zero righe): togliere questo attributo quando il WHERE verra' "
        + "composto con un accumulatore di condizioni invece di dipendere dal filtro sull'anno.")]
    [TestCase("iun", TestName = "senza anno · IUN inesistente")]
    [TestCase("mese", TestName = "senza anno · mese senza notifiche")]
    [TestCase("ente", TestName = "senza anno · ente senza notifiche")]
    [TestCase("cap", TestName = "senza anno · CAP inesistente")]
    [TestCase("recipient", TestName = "senza anno · destinatario inesistente")]
    public async Task FiltroSenzaAnno_ShouldEssereApplicato(string filtro)
    {
        var query = new NotificaQueryGetByListaEnti(Auth());
        switch (filtro)
        {
            case "iun": query.Iun = "IUN-CHE-NON-ESISTE"; break;
            case "mese": query.MeseValidita = 7; break;
            case "ente": query.EntiIds = ["00000000-0000-0000-0000-000000000000"]; break;
            case "cap": query.Cap = "99999"; break;
            case "recipient": query.RecipientId = "REC-CHE-NON-ESISTE"; break;
        }

        var r = await _handler.Send(query);

        Assert.That(r!.Notifiche, Is.Empty,
            "Nessuna notifica corrisponde al valore cercato, quindi la risposta deve essere vuota. "
            + "Oggi tornano tutte e 3: il filtro non e' stato applicato affatto.");
    }

    [Test]
    public async Task NessunFiltroAffatto_ShouldFunzionare()
    {
        // Contro-prova che isola la causa: senza NESSUN filtro il WHERE non serve e la query e'
        // valida. E' proprio la combinazione "nessun anno + almeno un altro filtro" a romperla.
        var r = await _handler.Send(new NotificaQueryGetByListaEnti(Auth()) { Page = 1, Size = 5 });

        Assert.That(r, Is.Not.Null);
    }

    // ---------------------------------------------------------------------------------------------
    // Paginazione parziale
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// DIFETTO. L'OFFSET viene aggiunto quando **non** sono entrambi null (`page == null &amp;&amp; size == null`),
    /// ma i parametri `@page`/`@size` sono aggiunti singolarmente e solo se valorizzati. Passandone uno
    /// solo — tipico di un client che manda `page` e lascia la size al default — la query referenzia un
    /// parametro che non esiste e va in eccezione.
    /// </summary>
    [Ignore("DIFETTO APERTO — page e size vanno passati insieme o per nessuno dei due, ma niente lo "
        + "impone: e' un contratto implicito verso il frontend. Passandone uno solo si ottiene una "
        + "SqlException su un parametro mancante, cioe' un 500. Il test asserisce il comportamento "
        + "CORRETTO (rispondere, completando il parametro mancante con un default): togliere questo "
        + "attributo quando la paginazione parziale verra' gestita.")]
    [TestCase(1, null, TestName = "paginazione · page senza size")]
    [TestCase(null, 10, TestName = "paginazione · size senza page")]
    public async Task PaginazioneParziale_ShouldRispondereSenzaErrori(int? page, int? size)
    {
        var query = Base();
        query.Page = page;
        query.Size = size;

        var r = await _handler.Send(query);

        Assert.That(r, Is.Not.Null, "Una paginazione incompleta e' un input plausibile, non un errore server.");
    }

    /// <summary>
    /// DIFETTO. `FETCH NEXT 0 ROWS` non e' valido in T-SQL, e una size a zero — input plausibile da un
    /// client che azzera un contatore — arriva fino al database senza incontrare alcuna validazione.
    /// </summary>
    [Ignore("DIFETTO APERTO — Size = 0 produce SQL invalido e quindi un 500. Il test asserisce il "
        + "comportamento CORRETTO (pagina vuota, o rifiuto esplicito lato endpoint): togliere questo "
        + "attributo quando la size verra' validata.")]
    [Test]
    public async Task SizeZero_ShouldRestituirePaginaVuota_NonUnErrore()
    {
        var query = Base();
        query.Page = 1;
        query.Size = 0;

        var r = await _handler.Send(query);

        Assert.That(r!.Notifiche, Is.Empty);
    }

    [Test]
    public async Task SizeMoltoGrande_ShouldRestituireTuttoSenzaErrori()
    {
        var query = Base();
        query.Page = 1;
        query.Size = 1_000_000;

        var r = await _handler.Send(query);

        Assert.That(r!.Notifiche!.Count(), Is.EqualTo(3), "Una size sovradimensionata non deve rompere nulla.");
    }

    // ---------------------------------------------------------------------------------------------
    // Injection e valori ostili
    // ---------------------------------------------------------------------------------------------

    [TestCase("IUN-3001'; DROP TABLE pfd.Notifiche; --", TestName = "injection · IUN")]
    [TestCase("' OR '1'='1", TestName = "injection · IUN sempre vero")]
    public async Task InjectionNelloIun_ShouldEssereValoreNonSql(string ostile)
    {
        var query = Base();
        query.Iun = ostile;

        var r = await _handler.Send(query);

        Assert.Multiple(() =>
        {
            Assert.That(r!.Notifiche, Is.Empty,
                "Trattato come valore: nessuno IUN corrisponde, quindi zero righe. Se l'OR '1'='1' "
                + "avesse effetto tornerebbero tutte.");
            Assert.That(Conta("SELECT COUNT(*) FROM sys.tables WHERE name = 'Notifiche'"), Is.EqualTo(1),
                "La tabella deve essere intatta.");
        });
    }

    [Test]
    public async Task InjectionNelCap_ShouldEssereValoreNonSql()
    {
        var query = Base();
        query.Cap = "00100' OR '1'='1";

        var r = await _handler.Send(query);

        Assert.That(r!.Notifiche, Is.Empty);
    }

    [Test]
    public async Task ValoriMoltoLunghi_ShouldNonRompereLaQuery()
    {
        // Le colonne sono nvarchar(100)/(400): un valore piu' lungo non deve produrre un errore in
        // una SELECT (a differenza di una INSERT), solo zero risultati.
        var query = Base();
        query.Iun = new string('X', 5000);

        var r = await _handler.Send(query);

        Assert.That(r!.Notifiche, Is.Empty);
    }

    // ---------------------------------------------------------------------------------------------
    // Liste: vuote, enormi, con valori impossibili
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task ListeVuote_ShouldComportarsiComeFiltroAssente()
    {
        // `IsNullNotAny()` e' vera sia per null sia per lista vuota: un array vuoto NON significa
        // "nessun risultato", significa "non filtrare". E' coerente fra tutti i filtri a lista, ma
        // e' l'opposto di quanto ci si aspetterebbe da un IN vuoto.
        var r = await _handler.Send(new NotificaQueryGetByListaEnti(Auth())
        {
            AnnoValidita = 2026,
            MeseValidita = 3,
            EntiIds = [],
            Recapitisti = [],
            Consolidatori = [],
            StatoContestazione = [],
            TipoNotifica = []
        });

        Assert.That(r!.Notifiche!.Count(), Is.EqualTo(3),
            "Tutte e tre: le liste vuote non hanno filtrato nulla.");
    }

    [Test]
    public void ListaEntiEnorme_ShouldFallire_Caratterizzazione()
    {
        // Dapper espande `IN @entiIds` in un parametro per elemento, e SQL Server ne accetta al
        // massimo 2100 per comando. Uno scenario reale: un admin che incolla l'elenco completo degli
        // aderenti. Il limite non e' gestito ne' documentato.
        var query = Base();
        query.EntiIds = Enumerable.Range(0, 2500).Select(i => $"ente-{i}").ToArray();

        var eccezione = Assert.CatchAsync(async () => await _handler.Send(query));

        Assert.That(eccezione, Is.Not.Null,
            "Oltre ~2100 valori la query supera il limite di parametri di SQL Server.");
    }

    [Test]
    public async Task StatoContestazioneInesistente_ShouldRestituireVuoto()
    {
        var query = Base();
        query.StatoContestazione = [99];

        var r = await _handler.Send(query);

        Assert.That(r!.Notifiche, Is.Empty);
    }

    [Test]
    public async Task TipoNotificaFuoriDallEnum_ShouldNonSelezionareNulla()
    {
        // Map() restituisce null per un valore non riconosciuto, e i null vengono scartati dalla
        // lista dei codici: resta un IN vuoto, senza il ramo IS NULL perche' Digitali non c'e'.
        var query = Base();
        query.TipoNotifica = [(TipoNotifica)999];

        var r = await _handler.Send(query);

        Assert.That(r!.Notifiche, Is.Empty);
    }

    // ---------------------------------------------------------------------------------------------

    private NotificaQueryGetByListaEnti Base() => new(Auth())
    {
        AnnoValidita = 2026,
        MeseValidita = 3
    };

    private static int Conta(string sql)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return (int)cmd.ExecuteScalar()!;
    }

    private static AuthenticationInfo Auth() => new()
    {
        Id = "integration-test-notifiche-adv",
        IdEnte = Ente,
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };
}
