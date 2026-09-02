using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Test ADVERSARIAL su `NotificaQueryGetByListEntiPersistencev2`, la ricerca notifiche admin dietro
/// **`POST api/v2/notifiche/pagopa`**. Il comportamento "buono" sta in
/// <see cref="NotificaQueryListaEntiV2IntegrationTests"/>.
///
/// Qui non servono input ostili: bastano quelli **normali**. La v2 e' una riscrittura della v1 in cui
/// l'esecuzione Dapper (`QueryMultipleAsync(sql, parameters)`) e' stata sostituita da un `SqlCommand`
/// costruito a mano — e nella sostituzione i parametri si sono persi per strada:
///
/// <code>
/// var sqlParameters = new List&lt;SqlParameter&gt;();     // ← ne vengono aggiunti SOLO 4:
/// if (page.HasValue)  sqlParameters.Add(new SqlParameter("@Page", page));
/// if (size.HasValue)  sqlParameters.Add(new SqlParameter("@Size", size));
/// if (anno.HasValue)  sqlParameters.Add(new SqlParameter("@Anno", anno.Value));
/// if (mese.HasValue)  sqlParameters.Add(new SqlParameter("@Mese", mese.Value));
///
/// dynamic parameters = new ExpandoObject();          // ← e questo, che li avrebbe tutti,
/// if (!string.IsNullOrEmpty(iun)) parameters.Iun = iun;   //   e' rimasto ma NON viene piu' letto
/// </code>
///
/// L'`ExpandoObject` con gli altri dieci filtri e' ancora li', riga per riga, ma nessuno lo passa piu'
/// al comando: e' **codice morto**, e a colpo d'occhio la persistence sembra completa. Per questo il
/// difetto e' invisibile in review e va fissato da test.
///
/// Due modi diversi di fallire, entrambi verificati sul DB seedato:
///   • filtri scalari (`@iun`, `@cap`, `@profilo`, `@prodotto`, `@recipientId`)
///     → «Must declare the scalar variable»
///   • filtri di lista (`IN @entiIds`, `@Recapitisti`, `@Consolidatori`, `@tipoNotifica`,
///     `@contestazione`) → «Incorrect syntax near» — perche' `IN @lista` e' una comodita' di **Dapper**,
///     non T-SQL valido: senza Dapper a espanderla, la sintassi non sta in piedi
///
/// In pratica **`api/v2/notifiche/pagopa` risponde solo se si filtra per periodo**; qualunque altro
/// filtro della maschera di ricerca produce un 500. Convenzione delle asserzioni (la stessa della v1 e
/// delle SP di Gestione Fatture): il test che descrive il comportamento CORRETTO e' `[Ignore]` col
/// motivo nell'attributo, affiancato da una **caratterizzazione attiva** che fissa cosa succede oggi —
/// cosi' la suite resta verde, il debito resta visibile, e il giorno in cui qualcuno ripara la
/// persistence la caratterizzazione diventa rossa e obbliga a chiudere anche l'`[Ignore]`.
/// </summary>
public class NotificaQueryListaEntiV2AdversarialTests
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
    // I dieci filtri senza parametro
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Il raggio d'azione del difetto, filtro per filtro: e' l'elenco di cosa **non** si puo' cercare
    /// oggi dalla maschera admin della v2. Ogni caso corrisponde a un campo della UI.
    /// </summary>
    [TestCase("iun", "Must declare the scalar variable", TestName = "scalare · IUN")]
    [TestCase("cap", "Must declare the scalar variable", TestName = "scalare · CAP")]
    [TestCase("profilo", "Must declare the scalar variable", TestName = "scalare · profilo")]
    [TestCase("prodotto", "Must declare the scalar variable", TestName = "scalare · prodotto")]
    [TestCase("recipient", "Must declare the scalar variable", TestName = "scalare · destinatario")]
    [TestCase("enti", "Incorrect syntax near", TestName = "lista · enti")]
    [TestCase("recapitisti", "Incorrect syntax near", TestName = "lista · recapitisti")]
    [TestCase("consolidatori", "Incorrect syntax near", TestName = "lista · consolidatori")]
    [TestCase("tipoNotifica", "Incorrect syntax near", TestName = "lista · tipo notifica")]
    [TestCase("statoMulti", "Incorrect syntax near", TestName = "lista · stato contestazione (piu' di uno)")]
    public void OgniFiltroOltreIlPeriodo_OggiFallisce_Caratterizzazione(string filtro, string messaggio)
    {
        var ex = Assert.ThrowsAsync<SqlException>(async () => await Cerca(q => Applica(q, filtro)));

        Assert.That(ex!.Message, Does.Contain(messaggio),
            "Se questo test diventa rosso la persistence e' stata riparata: togliere gli [Ignore] "
            + "dei test gemelli qui sotto e cancellare questa caratterizzazione.");
    }

    /// <summary>
    /// Il comportamento atteso, sugli stessi filtri: sono gli identici casi gia' verdi sulla v1
    /// (v. <see cref="NotificaQueryListaEntiIntegrationTests"/>), quindi non c'e' dubbio su quale sia
    /// la risposta giusta — e' quella che la rotta di produzione dava prima della riscrittura.
    /// </summary>
    [Ignore("DIFETTO APERTO — la v2 aggiunge al SqlCommand solo i parametri Page/Size/Anno/Mese: ogni "
        + "altro filtro finisce nel testo SQL senza SqlParameter (o con la sintassi 'IN @lista' che e' "
        + "di Dapper, non T-SQL) e produce un 500. L'ExpandoObject che li conteneva tutti e' rimasto "
        + "nel codice ma non e' piu' letto da nessuno. Togliere questo attributo quando i parametri "
        + "saranno passati al comando e le liste espanse.")]
    [TestCase("iun", new[] { "EVT-3002" }, TestName = "atteso · IUN seleziona la singola notifica")]
    [TestCase("enti", new[] { "EVT-3001", "EVT-3002", "EVT-3003" }, TestName = "atteso · ente seleziona le sue")]
    [TestCase("tipoNotifica", new[] { "EVT-3002" }, TestName = "atteso · tipo notifica seleziona le 890")]
    public async Task OgniFiltroOltreIlPeriodo_ShouldFiltrareComeNellaV1(string filtro, string[] attesi)
    {
        var r = await Cerca(q => Applica(q, filtro));

        Assert.That(Ids(r), Is.EquivalentTo(attesi));
    }

    // ---------------------------------------------------------------------------------------------
    // L'ordinamento, che e' il motivo per cui la v2 esiste
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// La rotta v2 accetta `columnName` e `order` in query string, la query li porta fino alla
    /// persistence… che li butta via. La differenza con la v1 sta in una riga:
    ///
    /// <code>
    /// // v1: l'ordinamento e' calcolato per richiesta, dai parametri del comando
    /// var _orderBy = NotificaSQLBuilder.OrderBy(new SortParamSQLBuilder(_command.ColumName, _command.OrderDir));
    ///
    /// // v2: e' un campo static readonly, calcolato UNA VOLTA per processo, senza parametri
    /// private static readonly string _orderBy = NotificaSQLBuilder.OrderBy();
    /// </code>
    ///
    /// Essendo `static readonly`, non e' un caso da correggere spostando l'assegnazione: per richiesta
    /// non potrebbe esserlo comunque. L'overload `OrderBy(SortParamSQLBuilder)` resta invocato solo
    /// dalla v1.
    /// </summary>
    [Test]
    public async Task Ordinamento_OggiIgnorato_Caratterizzazione()
    {
        var asc = await Cerca(q => { q.ColumName = "data"; q.OrderDir = "1"; });
        var desc = await Cerca(q => { q.ColumName = "data"; q.OrderDir = "2"; });

        Assert.That(Ids(asc), Is.EqualTo(Ids(desc)),
            "Oggi ASC e DESC danno lo stesso ordine: l'ordinamento richiesto non arriva alla query.");
    }

    [Ignore("DIFETTO APERTO — _orderBy e' un campo static readonly inizializzato con OrderBy() senza "
        + "parametri, quindi ColumName/OrderDir della richiesta non hanno alcun effetto (nella v1 "
        + "invece l'ordinamento e' calcolato per richiesta). Togliere questo attributo quando la v2 "
        + "costruira' l'ORDER BY dal comando, come fa la v1.")]
    [Test]
    public async Task Ordinamento_ShouldEssereApplicatoComeNellaV1()
    {
        var asc = await Cerca(q => { q.ColumName = "data"; q.OrderDir = "1"; });
        var desc = await Cerca(q => { q.ColumName = "data"; q.OrderDir = "2"; });

        Assert.That(Ids(asc), Is.EqualTo(Ids(desc).AsEnumerable().Reverse().ToList()),
            "ASC e DESC sullo stesso insieme devono dare l'ordine opposto.");
    }

    // ---------------------------------------------------------------------------------------------
    // Il WHERE composto a stringhe: difetto ereditato dalla v1, qui piu' facile da vedere
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// ⚠️ Identico al difetto n.1 gia' documentato sulla v1: la parola `WHERE` la emette **solo**
    /// `if (anno.HasValue)`, tutti gli altri filtri aggiungono `" AND …"`. Senza anno, quell'`AND` non
    /// finisce in un WHERE inesistente — si attacca all'ultima riga della SELECT, che e'
    /// `LEFT JOIN pfw.TipoContestazione a ON …`, e diventa parte della **ON di una LEFT JOIN**, dove
    /// non elimina righe.
    ///
    /// Nella v2 l'effetto e' piu' evidente che nella v1 perche' il seed ha ora anche il periodo
    /// 2026/4: chiedendo il **mese 3 senza anno** tornano 4 notifiche, cioe' anche quella di aprile.
    /// Non un errore, non un filtro: semplicemente l'intero insieme.
    /// </summary>
    [Ignore("DIFETTO APERTO — un filtro passato senza AnnoValidita non filtra nulla: l' AND finisce "
        + "nella ON dell'ultima LEFT JOIN invece che in un WHERE. E' lo stesso difetto della v1 "
        + "(FiltroSenzaAnno_ShouldEssereApplicato): si chiude in un colpo solo componendo il WHERE "
        + "con un accumulatore di condizioni in entrambe le persistence.")]
    [Test]
    public async Task MeseSenzaAnno_ShouldEssereApplicato()
    {
        var r = await Cerca(q => { q.AnnoValidita = null; q.MeseValidita = 3; });

        Assert.That(Ids(r), Does.Not.Contain("EVT-3004"),
            "EVT-3004 e' di aprile: chiedendo marzo non deve comparire.");
    }

    // ---------------------------------------------------------------------------------------------
    // Paginazione: sulla v2 il caso limite e' piu' raggiungibile che sulla v1
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// ⚠️ Sulla v2 questo non e' un caso di laboratorio. L'endpoint dichiara
    /// `[FromQuery] int page, [FromQuery] int pageSize` **non nullable**: una chiamata a
    /// `api/v2/notifiche/pagopa` che ometta i due parametri non li lascia assenti, li lega a **0** — e
    /// `size = 0` arriva fino a `FETCH NEXT 0 ROWS ONLY`, che non e' T-SQL valido. Un client che
    /// dimentica la query string prende un 500 invece di una prima pagina.
    ///
    /// Sulla v1, dove page/size sono nullable, lo stesso valore poteva arrivare solo da chi lo
    /// scriveva esplicitamente.
    /// </summary>
    [Ignore("DIFETTO APERTO — Size = 0 arriva al DB e produce FETCH NEXT 0 ROWS, che SQL Server "
        + "rifiuta. Sulla v2 e' raggiungibile semplicemente omettendo page/pageSize dalla query "
        + "string, che il binding lega a 0. Togliere questo attributo quando la size sara' validata "
        + "sull'endpoint (o normalizzata a un default).")]
    [TestCase(1, 0, TestName = "paginazione · size esplicita a zero")]
    [TestCase(0, 0, TestName = "paginazione · page e size assenti dalla query string")]
    public async Task SizeZero_ShouldRestituirePaginaVuota_NonUnErrore(int page, int size)
    {
        var r = await Cerca(q => { q.Page = page; q.Size = size; });

        Assert.That(r, Is.Not.Null, "Una pagina vuota e' un risultato, non un errore.");
    }

    [Test]
    public void SizeZero_OggiSollevaSqlException_Caratterizzazione()
    {
        var ex = Assert.ThrowsAsync<SqlException>(async () => await Cerca(q => { q.Page = 1; q.Size = 0; }));

        Assert.That(ex!.Message, Does.Contain("FETCH"));
    }

    /// <summary>
    /// Il contro-caso che dimostra che non e' la paginazione in se' a essere fragile: una size enorme
    /// (oltre il numero di righe esistenti) e' gestita senza errori.
    /// </summary>
    [Test]
    public async Task SizeMoltoGrande_ShouldRestituireTuttoSenzaErrori()
    {
        var r = await Cerca(q => { q.Page = 1; q.Size = 100000; });

        Assert.That(Ids(r), Has.Count.EqualTo(3));
    }

    // ---------------------------------------------------------------------------------------------
    // Liste vuote: coerenti con la v1, e senza il difetto dei parametri
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// `IsNullNotAny()` e' vera sia per `null` sia per l'array vuoto, quindi una lista vuota significa
    /// "non filtrare" e il ramo che aggiunge l'`IN` non viene mai preso. Effetto collaterale utile:
    /// una lista vuota **non** innesca il difetto dei parametri, perche' la clausola non viene proprio
    /// scritta. E' l'unico caso in cui passare un filtro di lista alla v2 non produce un 500.
    /// </summary>
    [Test]
    public async Task ListeVuote_ShouldComportarsiComeFiltroAssente()
    {
        var r = await Cerca(q =>
        {
            q.EntiIds = [];
            q.Recapitisti = [];
            q.Consolidatori = [];
            q.TipoNotifica = [];
            q.StatoContestazione = [];
        });

        Assert.That(Ids(r), Has.Count.EqualTo(3),
            "Cinque liste vuote insieme: nessuna clausola aggiunta, nessun parametro mancante.");
    }

    // ---------------------------------------------------------------------------------------------

    private static void Applica(NotificaQueryGetByListaEntiv2 q, string filtro)
    {
        switch (filtro)
        {
            case "iun": q.Iun = "IUN-3002"; break;
            case "cap": q.Cap = "00100"; break;
            case "profilo": q.Profilo = "PA"; break;
            case "prodotto": q.Prodotto = "prod-pn"; break;
            case "recipient": q.RecipientId = "REC-3001"; break;
            case "enti": q.EntiIds = [Ente]; break;
            case "recapitisti": q.Recapitisti = ["Recapitista Uno"]; break;
            case "consolidatori": q.Consolidatori = ["Consolidatore Uno"]; break;
            case "tipoNotifica": q.TipoNotifica = [TipoNotifica.Analogico890]; break;
            case "statoMulti": q.StatoContestazione = [1, 3]; break;
            default: throw new ArgumentOutOfRangeException(nameof(filtro), filtro, "Filtro non previsto.");
        }
    }

    private async Task<Infrastructure.Common.SEND.Notifiche.Dto.NotificaDto?> Cerca(
        Action<NotificaQueryGetByListaEntiv2> personalizza)
    {
        var query = new NotificaQueryGetByListaEntiv2(Auth())
        {
            AnnoValidita = 2026,
            MeseValidita = 3
        };
        personalizza(query);
        return await _handler.Send(query);
    }

    private static List<string> Ids(Infrastructure.Common.SEND.Notifiche.Dto.NotificaDto? r) =>
        r?.Notifiche?.Select(x => x.IdNotifica ?? string.Empty).ToList() ?? [];

    private static AuthenticationInfo Auth() => new()
    {
        Id = "integration-test-notifiche-v2-adv",
        IdEnte = Ente,
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };
}
