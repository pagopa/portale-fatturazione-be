using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// L'area Orchestratore: il monitoraggio interno di dove sia arrivata l'elaborazione (REL, fine
/// contestazioni, import notifiche, emissione fatture) per ogni periodo.
///
/// Fino al 31/08/2026 non aveva copertura di query, per un motivo strutturale: legge
/// `pfd.vOrchestratore`, che non era nel DB seedato. Ora c'e' (`tests/Data/views/94_vOrchestratore.sql`,
/// DDL reale estratta da PRODUZIONE), insieme alle due tabelle di calendario da cui dipende
/// (`tests/Data/orchestratore.sql`).
///
/// ⚠️ Episodio del 31/08/2026, utile come precedente: la vista c'era in produzione ma **mancava in
///    UAT**, dove le quattro rotte dell'area rispondevano 500 — un disallineamento di ambiente, non un
///    difetto del backend (l'allineamento DB fra ambienti e' manuale, v. `docs/cicd-release.md`).
///    Segnalato al team Data e risolto in giornata: oggi la vista c'e' in DEV, UAT e PROD. Davanti a
///    un 500 su un'area che in locale funziona, la prima domanda e' "quell'oggetto esiste su
///    quell'ambiente?".
///
/// Due cose che questi test hanno stabilito, e che il testbook diceva al contrario:
///
///   - il ramo "lista senza date" **non e' un 500**. La persistence filtra con
///     `ISNULL(DataEsecuzione, DataFineContestazioni) >= '0001-01-01'`, e sembrava fuori dal dominio
///     di `datetime` (che parte dal 1753). Ma nella vista `DataEsecuzione` e' `CAST(... AS DATE)`, e
///     `ISNULL` restituisce il tipo del **primo** argomento: il confronto avviene in `date`, dominio
///     che parte dall'anno 1. E' il `CAST` a tenere in piedi quel ramo (v. il test omonimo);
///   - `Esecuzione` non puo' essere NULL: tutti e otto i rami dell'UNION la calcolano con un `CASE`
///     che ha sempre un `ELSE` costante. `DescrizioneEsecuzione`, che fa `Esecuzione!.Value`, resta
///     quindi fragile nel C# ma da questa vista non e' raggiungibile.
///
/// **Regola per chi aggiunge test qui**: mai asserire sul numero totale di righe della vista. I due
/// rami "IMPORT DATI" non leggono righe, le generano con un CROSS JOIN dei 12 mesi, e per l'anno
/// corrente si fermano a `MONTH(GETDATE())+1` — il totale cambia da solo a ogni cambio di mese. Le
/// righe del seed si identificano per Anno/Mese/Tipologia/Fase, e le date sono estreme (2020 =
/// passato per sempre, 2099 = futuro per sempre) proprio per non dipendere dalla data di esecuzione.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class OrchestratoreQueryIntegrationTests
{
    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    // =============================================================================================
    // Il ramo senza date: quello che si temeva fosse un 500
    // =============================================================================================

    [Test]
    public async Task SenzaDate_ShouldRestituireLeRighe_NonUnErrore()
    {
        // Nessun init, nessun end: la persistence emette
        //   WHERE ISNULL(DataEsecuzione, DataFineContestazioni) >= '0001-01-01'
        // Se qualcuno togliesse il CAST(... AS DATE) dalla vista, il confronto tornerebbe datetime
        // e questo test diventerebbe un errore SQL "conversione fuori intervallo".
        var dto = await Lista();

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.Count, Is.GreaterThan(0),
            "Il letterale '0001-01-01' e' valido perche' il confronto avviene in `date`: e' il "
            + "CAST(DataEsecuzione AS DATE) della vista a renderlo tale.");
        Assert.That(dto.Items!.Count(), Is.EqualTo(dto.Count),
            "Senza paginazione, righe e Count devono coincidere: sono due query con lo stesso WHERE.");
    }

    [Test]
    public async Task OgniRiga_ShouldAvereUnaDescrizioneEsecuzione()
    {
        // `DescrizioneEsecuzione` e' una proprieta' calcolata SENZA setter: non viene mai assegnata da
        // Dapper, viene valutata alla serializzazione della risposta e durante l'export Excel. Fa
        // `Esecuzione!.Value`, quindi una riga con Esecuzione NULL sarebbe un 500 sull'INTERA risposta.
        // Qui si verifica sul dato reale cio' che i metadati della vista dichiarano (Esecuzione NOT
        // NULL): nessuna riga fa esplodere il getter.
        var dto = await Lista();

        Assert.That(dto.Items!.Select(i => i.Esecuzione), Has.All.Not.Null);
        Assert.That(dto.Items!.Select(i => i.DescrizioneEsecuzione), Has.All.Not.Null,
            "Uno stato fuori dai quattro noti (0-3) darebbe una descrizione null: gli stati sono "
            + "hardcoded in StatiQuery, la colonna la popolano le pipeline del team Data.");
    }

    // =============================================================================================
    // Filtro per data — lavora su ISNULL(DataEsecuzione, DataFineContestazioni)
    // =============================================================================================

    [Test]
    public async Task IntervalloDiDate_ShouldSelezionareSoloLeRigheNelPeriodo()
    {
        // Tutto il 2020: sono le righe che il seed ha messo "nel passato per sempre".
        var dto = await Lista(q =>
        {
            q.Init = new DateTime(2020, 1, 1);
            q.End = new DateTime(2020, 12, 31);
        });

        Assert.That(Chiavi(dto), Is.EquivalentTo(new[]
        {
            "PRIMO SALDO|FATT.|2026|3",
            "PRIMO SALDO|FATT.|2026|6",
            "PRIMO SALDO|FATT. REL FIRM.|2026|6",
            "SECONDO SALDO|FATT. REL FIRM.|2026|2",
            "VAR. SEMESTRALE|REL|2026|5",
            "VAR. SEMESTRALE|REL|2026|11",
        }));
    }

    [Test]
    public async Task SoloInit_ShouldPrendereTuttoCioCheViene_Dopo()
    {
        var dto = await Lista(q => q.Init = new DateTime(2099, 1, 1));

        Assert.That(dto.Count, Is.EqualTo(6));
        Assert.That(dto.Items!.Select(i => i.Anno), Has.All.EqualTo(2026),
            "Le righe 'future per sempre' del seed restano righe di periodo 2026: la data di "
            + "esecuzione e' cosa diversa dal periodo di riferimento.");
    }

    [Test]
    public async Task SoloEnd_ShouldPrendereTuttoCioCheViene_Prima()
    {
        var dto = await Lista(q => q.End = new DateTime(2020, 12, 31));

        Assert.That(dto.Count, Is.EqualTo(6), "Nel seed non c'e' nulla prima del 2020.");
    }

    [Test]
    public async Task PeriodoSenzaRighe_ShouldRestituireListaVuota_NonUnErrore()
    {
        // E' il caso che l'endpoint traduce in 404 (v. Http/OrchestratoreHttpTests): qui si verifica
        // che a livello di query sia una lista vuota, non un'eccezione.
        var dto = await Lista(q =>
        {
            q.Init = new DateTime(1990, 1, 1);
            q.End = new DateTime(1990, 12, 31);
        });

        Assert.That(dto.Items, Is.Empty);
        Assert.That(dto.Count, Is.Zero);
    }

    // =============================================================================================
    // Filtri a lista: stati, tipologie, fasi
    // =============================================================================================

    [Test]
    public async Task FiltroStati_ShouldSelezionareSoloQuelliChiesti()
    {
        // Stato 3 = "Errore". Il filtro e' combinato con l'intervallo 2020 perche' altrimenti
        // entrerebbero anche le righe generate del ramo IMPORT DATI, il cui stato dipende da GETDATE().
        var dto = await Lista(q =>
        {
            q.Init = new DateTime(2020, 1, 1);
            q.End = new DateTime(2020, 12, 31);
            q.Stati = [3];
        });

        Assert.That(Chiavi(dto), Is.EquivalentTo(new[] { "PRIMO SALDO|FATT.|2026|3" }));
    }

    [Test]
    public async Task FiltroTipologie_ShouldSelezionareSoloQuelleChieste()
    {
        var dto = await Lista(q => q.Tipologie = ["VAR. SEMESTRALE"]);

        Assert.That(dto.Count, Is.EqualTo(3));
        Assert.That(dto.Items!.Select(i => i.Tipologia), Has.All.EqualTo("VAR. SEMESTRALE"));
        Assert.That(dto.Items!.Select(i => i.Esecuzione), Is.EquivalentTo(new int?[] { 1, 2, 0 }),
            "Le tre righe coprono i tre esiti del ramo semestrale: REL presente (1), data passata "
            + "senza REL (2), data futura (0).");
    }

    [Test]
    public async Task FiltroFasi_ShouldSelezionareSoloQuelleChieste()
    {
        var dto = await Lista(q => q.Fasi = ["FINE CONT."]);

        Assert.That(Chiavi(dto), Is.EquivalentTo(new[]
        {
            "PRIMO SALDO|FINE CONT.|2026|3",
            "PRIMO SALDO|FINE CONT.|2026|4",
            "SECONDO SALDO|FINE CONT.|2026|3",
            "SECONDO SALDO|FINE CONT.|2026|4",
        }));
    }

    [Test]
    public async Task FiltriCombinati_ShouldIntersecare()
    {
        var dto = await Lista(q =>
        {
            q.Tipologie = ["PRIMO SALDO"];
            q.Fasi = ["REL"];
        });

        Assert.That(Chiavi(dto), Is.EquivalentTo(new[]
        {
            "PRIMO SALDO|REL|2026|3",
            "PRIMO SALDO|REL|2026|4",
        }));
    }

    [Test]
    public async Task ListaVuota_ShouldSignificareNonFiltrare_NonNessunRisultato()
    {
        // `IsNullNotAny()` e' vera sia per null sia per l'array vuoto, quindi un array vuoto NON
        // aggiunge la condizione: il risultato e' "tutte le righe", non "nessuna". E' l'opposto di
        // quanto suggerisce un IN vuoto, ed e' coerente con gli altri filtri a lista del progetto
        // (v. la stessa forma nella ricerca notifiche).
        var senzaFiltri = await Lista();
        var conListeVuote = await Lista(q =>
        {
            q.Tipologie = [];
            q.Fasi = [];
            q.Stati = [];
        });

        Assert.That(conListeVuote.Count, Is.EqualTo(senzaFiltri.Count));
    }

    // =============================================================================================
    // Ordinamento
    // =============================================================================================

    [Test]
    public async Task Ordinamento_ShouldInvertirsiFraAscEDesc()
    {
        // Le tre righe VAR. SEMESTRALE hanno date distinte (2020-05-20, 2020-11-20, 2099-12-31),
        // quindi l'ordine e' totale e il confronto non dipende da come SQL Server rompe i pari.
        var asc = await Lista(q => { q.Tipologie = ["VAR. SEMESTRALE"]; q.Ordinamento = 0; });
        var desc = await Lista(q => { q.Tipologie = ["VAR. SEMESTRALE"]; q.Ordinamento = 1; });

        Assert.That(Chiavi(asc), Is.EqualTo(Chiavi(desc).Reverse()).AsCollection);
        Assert.That(asc.Items!.First().Mese, Is.EqualTo(5), "ASC parte dalla data piu' vecchia.");
    }

    /// <summary>
    /// CARATTERIZZAZIONE. `Ordinamento` non e' un booleano ne' un enum: la persistence fa
    /// `Ordinamento == 0 ? "ASC" : "DESC"`, quindi **solo lo zero e' ascendente** e qualunque altro
    /// valore — 2, 99, un negativo — e' silenziosamente DESC. Nessun errore, nessuna validazione.
    ///
    /// Non e' un difetto da correggere di slancio (cambiarlo cambierebbe il contratto verso il
    /// frontend), ma va saputo: se un domani qualcuno introducesse un terzo criterio di ordinamento
    /// passando `Ordinamento = 2`, otterrebbe DESC senza accorgersene.
    /// </summary>
    [Test]
    public async Task OrdinamentoDiversoDaZero_ShouldEssereTrattatoComeDesc()
    {
        var desc = await Lista(q => { q.Tipologie = ["VAR. SEMESTRALE"]; q.Ordinamento = 1; });
        var strambo = await Lista(q => { q.Tipologie = ["VAR. SEMESTRALE"]; q.Ordinamento = 99; });

        Assert.That(Chiavi(strambo), Is.EqualTo(Chiavi(desc)).AsCollection);
    }

    // =============================================================================================
    // Paginazione
    // =============================================================================================

    [Test]
    public async Task Paginazione_ShouldRestituirePagineDiverse_ConCountInvariato()
    {
        var pagina1 = await Lista(q => { q.Tipologie = ["VAR. SEMESTRALE"]; q.Page = 1; q.Size = 2; });
        var pagina2 = await Lista(q => { q.Tipologie = ["VAR. SEMESTRALE"]; q.Page = 2; q.Size = 2; });

        Assert.Multiple(() =>
        {
            Assert.That(pagina1.Items!.Count(), Is.EqualTo(2));
            Assert.That(pagina2.Items!.Count(), Is.EqualTo(1));
            Assert.That(Chiavi(pagina1).Intersect(Chiavi(pagina2)), Is.Empty);
            Assert.That(pagina1.Count, Is.EqualTo(3), "Count e' il TOTALE, non la dimensione della pagina.");
            Assert.That(pagina2.Count, Is.EqualTo(3));
        });
    }

    /// <summary>
    /// DIFETTO APERTO — i due parametri di paginazione vanno passati insieme o per nessuno dei due,
    /// ma niente lo impone e niente lo documenta.
    ///
    /// La persistence aggiunge l'OFFSET se **non** sono entrambi null, ma poi passa `@Page` e `@Size`
    /// al comando **solo se valorizzati singolarmente**: con la sola `Page` la query referenzia un
    /// parametro che non esiste. E' lo stesso difetto gia' registrato sulla ricerca notifiche.
    ///
    /// L'aspettativa qui sotto e' quella CORRETTA: chi ripara la persistence toglie l'[Ignore] e
    /// trova il test verde.
    /// </summary>
    [Test]
    [Ignore("DIFETTO APERTO — Page senza Size (o viceversa) referenzia un parametro non dichiarato: "
        + "SqlException 'Must declare the scalar variable \"@size\"'. Rimedio: completare il parametro "
        + "mancante con un default, oppure rifiutare a monte la combinazione parziale. "
        + "V. coverage/test-backlog.md.")]
    public async Task PaginazioneParziale_ShouldRispondereSenzaErrori()
    {
        var dto = await Lista(q => { q.Tipologie = ["VAR. SEMESTRALE"]; q.Page = 1; });

        Assert.That(dto.Items, Is.Not.Null);
    }

    /// <summary>
    /// DIFETTO APERTO — `Size = 0` arriva fino al DB e diventa `FETCH NEXT 0 ROWS`, che SQL Server
    /// rifiuta: *"The number of rows provided for a FETCH clause must be greater then zero"* (il
    /// refuso e' suo).
    ///
    /// Sulla rotta HTTP ci si arriva passando `pageSize=0` **esplicitamente**: omettere del tutto i
    /// parametri e' un difetto diverso, che si ferma prima nel binding (v. Http/OrchestratoreHttpTests).
    /// </summary>
    [Test]
    [Ignore("DIFETTO APERTO — Size = 0 produce FETCH NEXT 0 ROWS, rifiutato da SQL Server. "
        + "Rimedio: validare la size sull'endpoint (o trattare 0 come 'nessuna paginazione'). "
        + "V. coverage/test-backlog.md.")]
    public async Task SizeZero_ShouldRestituirePaginaVuota_NonUnErrore()
    {
        var dto = await Lista(q => { q.Tipologie = ["VAR. SEMESTRALE"]; q.Page = 1; q.Size = 0; });

        Assert.That(dto.Items, Is.Empty);
        Assert.That(dto.Count, Is.EqualTo(3), "La size non deve influenzare il totale.");
    }

    // =============================================================================================
    // I due dropdown
    // =============================================================================================

    [Test]
    public async Task Tipologie_ShouldEssereQuelleDistinteDellaVista()
    {
        var tipologie = await _handler.Send(new OrchestratoreByTipologiaQuery(Auth()));

        Assert.That(tipologie, Is.EquivalentTo(new[]
        {
            "ANTICIPO", "IMPORT DATI", "PRIMO SALDO", "SECONDO SALDO", "VAR. SEMESTRALE",
        }));
    }

    [Test]
    public async Task Fasi_ShouldEssereQuelleDistinteDellaVista()
    {
        var fasi = await _handler.Send(new OrchestratoreByFaseQuery(Auth()));

        Assert.That(fasi, Is.EquivalentTo(new[]
        {
            "FATT.", "FATT. REL FIRM.", "FINE CONT.", "NOTIFICHE", "REL",
        }));
    }

    // =============================================================================================
    // Helper
    //
    // Nota di perimetro: nessuna riga del seed ha DataEsecuzione NULL, quindi il **fallback** di
    // ISNULL su DataFineContestazioni non e' esercitato — servirebbe una riga di
    // pfw.ContestazioniCalendario con DataCalcoloPrimoSecondo NULL, che pero' e' una tabella con
    // vincoli di mapping propri (v. tests/Data/notifiche.sql) e va toccata con cautela.
    // =============================================================================================

    private async Task<OrchestratoreDto> Lista(Action<OrchestratoreByDateQuery>? configura = null)
    {
        var query = new OrchestratoreByDateQuery(Auth());
        configura?.Invoke(query);
        return (await _handler.Send(query))!;
    }

    /// <summary>Identifica una riga per cio' che la rende unica nel seed, non per posizione.</summary>
    private static IEnumerable<string> Chiavi(OrchestratoreDto dto)
        => dto.Items!.Select(i => $"{i.Tipologia}|{i.Fase}|{i.Anno}|{i.Mese}");

    private static AuthenticationInfo Auth() => new()
    {
        Id = "integration-test-orchestratore",
        IdEnte = "11111111-1111-1111-1111-111111111111",
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };
}
