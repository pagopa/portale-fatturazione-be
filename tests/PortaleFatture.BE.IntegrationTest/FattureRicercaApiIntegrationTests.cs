using Dapper;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test (DB seedato) COMPLESSIVI su api/fatture (PostFattureByRicercaAsync) — funzionalità
/// core consumata dal FE (pagina Documenti Emessi). Esercita la stessa catena reale dell'endpoint:
/// FattureQueryRicerca -> FattureQueryRicercaHandler -> FattureQueryRicercaPersistence, che sceglie
///   - Cancellata=false -> FattureQueryRicercaBuilder.SelectView()          (fatture EMESSE, esclude posticipate)
///   - Cancellata=true  -> FattureQueryRicercaBuilder.SelectViewCancellate() (NON FATTURATE = Eliminate + Posticipate)
/// entrambe FOR JSON -> listaFatture -> FattureListaDto (JsonTypeHandler) + arricchimento ente in C#.
///
/// Filtri coperti su entrambi i rami: Anno/Mese (obbligatori), TipologiaFattura[], FkIdTipoContratto,
/// FatturaInviata (solo ramo EMESSE), periodo vuoto, e la disgiunzione emesse/non-fatturate.
///
/// Seed DEDICATO (Anno 2024) per non interferire con gli altri test:
///   EMESSE:        6001 ente1/SECONDO SALDO/mese 3, FatturaInviata=1, tipocontratto=2.
///   NON FATTURATE: 5001/5002 Eliminate ente3/mese 2 (5002 senza righe -> posizioni null, tipocontratto=1);
///                  4001 Posticipata ente1/mese 1 (tipocontratto=2).
/// </summary>
public class FattureRicercaApiIntegrationTests
{
    private IMediator _handler = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
        // Il JSON listaFatture -> FattureListaDto e' deserializzato via JsonTypeHandler, registrato in
        // produzione dalla config API. Nei test va registrato esplicitamente (Dapper globale).
        => SqlMapper.AddTypeHandler(typeof(FattureListaDto), new JsonTypeHandler());

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    private static AuthenticationInfo AdminAuth() => new()
    { IdEnte = Guid.NewGuid().ToString(), Prodotto = "prod-pn", Ruolo = Ruolo.ADMIN, IdTipoContratto = 1 };

    private async Task<List<TitoloFatturaDto>> Query(bool cancellata, int anno, int mese,
        string[]? tipologia = null, int? fkIdTipoContratto = null, int? fatturaInviata = null,
        string[]? idEnti = null)
    {
        var res = await _handler.Send(new FattureQueryRicerca(AdminAuth())
        {
            Cancellata = cancellata,
            Anno = anno,
            Mese = mese,
            TipologiaFattura = tipologia,
            FkIdTipoContratto = fkIdTipoContratto,
            FatturaInviata = fatturaInviata,
            IdEnti = idEnti
        });
        return (res ?? new FattureListaDto()).Select(x => x.fattura!).ToList();
    }

    // =================== Ramo EMESSE (Cancellata=false, SelectView) ===================

    [Test]
    public async Task Emesse_2024_3_RestituisceLaFatturaEmessa_ConPosizioni()
    {
        var f = (await Query(false, 2024, 3)).SingleOrDefault(x => x.IdFattura == 6001);

        Assert.That(f, Is.Not.Null, "6001 (emessa) deve comparire nel ramo Documenti Emessi.");
        Assert.Multiple(() =>
        {
            Assert.That(f!.Inviata, Is.EqualTo(1), "FatturaInviata reale (non un marker).");
            Assert.That(f.TipologiaFattura, Is.EqualTo("SECONDO SALDO"));
            Assert.That(f.RagioneSociale, Is.EqualTo("Ente Test 1"), "Ente arricchito in C#.");
            Assert.That(f.Posizioni, Is.Not.Null.And.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Emesse_NonRestituisceLePosticipate()
    {
        // 2024/1 contiene SOLO la posticipata 4001 (Stato=0): il ramo emesse la esclude -> vuoto.
        var rows = await Query(false, 2024, 1);
        Assert.That(rows.Any(x => x.IdFattura == 4001), Is.False, "Le POSTICIPATE (Stato=0) non sono 'emesse'.");
    }

    [Test]
    public async Task Emesse_FiltroTipologia()
    {
        var conTipo = await Query(false, 2024, 3, tipologia: new[] { "SECONDO SALDO" });
        var altraTipo = await Query(false, 2024, 3, tipologia: new[] { "ANTICIPO" });
        Assert.Multiple(() =>
        {
            Assert.That(conTipo.Any(x => x.IdFattura == 6001), Is.True);
            Assert.That(altraTipo.Any(x => x.IdFattura == 6001), Is.False);
        });
    }

    [Test]
    public async Task Emesse_FiltroFkIdTipoContratto()
    {
        var tipo2 = await Query(false, 2024, 3, fkIdTipoContratto: 2);
        var tipo1 = await Query(false, 2024, 3, fkIdTipoContratto: 1);
        Assert.Multiple(() =>
        {
            Assert.That(tipo2.Any(x => x.IdFattura == 6001), Is.True, "6001 e' tipocontratto=2.");
            Assert.That(tipo1.Any(x => x.IdFattura == 6001), Is.False);
        });
    }

    [Test]
    public async Task Emesse_FiltroFatturaInviata()
    {
        var inviata1 = await Query(false, 2024, 3, fatturaInviata: 1);
        var inviata0 = await Query(false, 2024, 3, fatturaInviata: 0);
        Assert.Multiple(() =>
        {
            Assert.That(inviata1.Any(x => x.IdFattura == 6001), Is.True, "6001 e' inviata=1.");
            Assert.That(inviata0.Any(x => x.IdFattura == 6001), Is.False);
        });
    }

    [Test]
    public async Task Emesse_PeriodoVuoto_RestituisceVuoto()
        => Assert.That(await Query(false, 2024, 11), Is.Empty);

    // =================== Ramo NON FATTURATE (Cancellata=true, SelectViewCancellate) ===================

    [Test]
    public async Task NonFatturate_Eliminate_2024_2_Inviata3_ePosizioniNullSenzaRighe()
    {
        var rows = await Query(true, 2024, 2);
        var f5001 = rows.SingleOrDefault(x => x.IdFattura == 5001);
        var f5002 = rows.SingleOrDefault(x => x.IdFattura == 5002);

        Assert.Multiple(() =>
        {
            Assert.That(f5001, Is.Not.Null, "5001 (Eliminate) deve comparire.");
            Assert.That(f5002, Is.Not.Null, "5002 (Eliminate senza righe) deve comparire.");
            Assert.That(f5001!.Inviata, Is.EqualTo(3), "Eliminate -> marker inviata=3 (label ELIMINATA).");
            Assert.That(f5001.RagioneSociale, Is.EqualTo("Ente Test 3"), "Ente arricchito in C#.");
            Assert.That(f5001.Posizioni, Is.Not.Null.And.Count.EqualTo(2));
            Assert.That(f5002!.Inviata, Is.EqualTo(3));
            Assert.That(f5002.Posizioni, Is.Null.Or.Empty, "5002 senza righe -> posizioni null (FOR JSON su set vuoto).");
        });
    }

    [Test]
    public async Task NonFatturate_Posticipate_2024_1_Inviata4()
    {
        var f = (await Query(true, 2024, 1)).SingleOrDefault(x => x.IdFattura == 4001);

        Assert.That(f, Is.Not.Null, "4001 (Posticipata) deve comparire tra le Non Fatturate.");
        Assert.Multiple(() =>
        {
            Assert.That(f!.Inviata, Is.EqualTo(4), "Posticipate -> marker inviata=4 (label POSTICIPATA).");
            Assert.That(f.TipologiaFattura, Is.EqualTo("SECONDO SALDO"));
            Assert.That(f.RagioneSociale, Is.EqualTo("Ente Test 1"));
            Assert.That(f.Posizioni, Is.Not.Null.And.Count.EqualTo(2));
        });
    }

    /// <summary>
    /// Runbook **T21**: *"con il filtro Stato = Non Fatturate voglio vedere tutte le fatture Posticipate
    /// E Eliminate"*. Le due famiglie arrivano da **tabelle diverse** — le eliminate da
    /// `pfd.FattureTestata_Eliminate`, le posticipate da `pfd.FattureTestata` + `cfg.GestioneFatture` —
    /// e la vista le unisce con una UNION ALL.
    ///
    /// Perche' serviva un test in piu' pur essendoci gia' quelli per famiglia: il seed le teneva in mesi
    /// **diversi** (posticipate 2024/1, eliminate 2024/2), quindi nessuna query ne restituiva due
    /// insieme e la condizione descritta dalla segnalazione non era mai esercitata. Periodo dedicato
    /// **2024/4**, aggiunto il 04/09/2026 con una fattura per famiglia su enti distinti.
    /// </summary>
    [Test]
    public async Task NonFatturate_PeriodoConEntrambeLeFamiglie_LeRestituisceInsieme()
    {
        var rows = await Query(true, 2024, 4);

        var posticipata = rows.SingleOrDefault(x => x.IdFattura == 4101);
        var eliminata = rows.SingleOrDefault(x => x.IdFattura == 5101);

        Assert.Multiple(() =>
        {
            Assert.That(posticipata, Is.Not.Null, "T21: la posticipata non compare nella griglia.");
            Assert.That(eliminata, Is.Not.Null, "T21: l'eliminata non compare nella griglia.");
            // I marker sono cio' che permette al frontend di distinguerle: non sono lo stato di invio.
            Assert.That(posticipata!.Inviata, Is.EqualTo(4), "Posticipata -> marker 4.");
            Assert.That(eliminata!.Inviata, Is.EqualTo(3), "Eliminata -> marker 3.");
            // Enti diversi: la griglia unisce famiglie di enti distinti, non solo di tabelle distinte.
            Assert.That(posticipata.RagioneSociale, Is.EqualTo("Ente Test 1"));
            Assert.That(eliminata.RagioneSociale, Is.EqualTo("Ente Test 3"));
        });
    }

    /// <summary>
    /// Contro-prova del periodo: la stessa griglia, filtrata per un'unica tipologia, ne restituisce una
    /// sola. Serve a dimostrare che il test sopra passa perche' **entrambe** ci sono davvero, e non
    /// perche' il filtro sia inerte.
    /// </summary>
    [Test]
    public async Task NonFatturate_PeriodoMisto_IlFiltroTipologiaSelezionaUnaSolaFamiglia()
    {
        var soloSecondoSaldo = await Query(true, 2024, 4, tipologia: ["SECONDO SALDO"]);

        Assert.Multiple(() =>
        {
            Assert.That(soloSecondoSaldo.Select(x => x.IdFattura), Does.Contain(4101));
            Assert.That(soloSecondoSaldo.Select(x => x.IdFattura), Does.Not.Contain(5101),
                "L'eliminata e' un ANTICIPO: il filtro tipologia deve escluderla.");
        });
    }

    [Test]
    public async Task NonFatturate_FiltroTipologia()
    {
        var soloAnticipo = await Query(true, 2024, 2, tipologia: new[] { "ANTICIPO" });
        Assert.Multiple(() =>
        {
            Assert.That(soloAnticipo.Any(x => x.IdFattura == 5001), Is.True, "ANTICIPO 5001 presente.");
            Assert.That(soloAnticipo.Any(x => x.IdFattura == 5002), Is.False, "ACCONTO 5002 escluso dal filtro.");
        });
    }

    [Test]
    public async Task NonFatturate_FiltroFkIdTipoContratto()
    {
        var tipo1 = await Query(true, 2024, 2, fkIdTipoContratto: 1);
        var tipo2 = await Query(true, 2024, 2, fkIdTipoContratto: 2);
        Assert.Multiple(() =>
        {
            Assert.That(tipo1.Any(x => x.IdFattura == 5001), Is.True, "tipocontratto=1 include l'eliminate ente3.");
            Assert.That(tipo2.Any(x => x.IdFattura == 5001), Is.False, "tipocontratto=2 esclude l'eliminate ente3 (tipo1).");
        });
    }

    [Test]
    public async Task NonFatturate_PeriodoVuoto_RestituisceVuoto()
        => Assert.That(await Query(true, 2024, 11), Is.Empty);

    // =================== Disgiunzione tra i due rami ===================

    [Test]
    public async Task Emesse_e_NonFatturate_SonoDisgiunte()
    {
        // 6001 (emessa) NON deve comparire tra le Non Fatturate; 5001 (eliminata) NON tra le emesse.
        var emesse2024_3 = await Query(false, 2024, 3);
        var nonFatt2024_3 = await Query(true, 2024, 3);
        var nonFatt2024_2 = await Query(true, 2024, 2);
        var emesse2024_2 = await Query(false, 2024, 2);

        Assert.Multiple(() =>
        {
            Assert.That(emesse2024_3.Any(x => x.IdFattura == 6001), Is.True);
            Assert.That(nonFatt2024_3.Any(x => x.IdFattura == 6001), Is.False, "L'emessa 6001 non e' 'Non Fatturata'.");
            Assert.That(nonFatt2024_2.Any(x => x.IdFattura == 5001), Is.True);
            Assert.That(emesse2024_2.Any(x => x.IdFattura == 5001), Is.False, "L'eliminata 5001 non e' tra le emesse.");
        });
    }

    // =================== Regressione CASING ente (match case-insensitive) ===================

    [Test]
    public async Task NonFatturate_CasingEnteDiverso_VieneComunqueRestituita()
    {
        // La posticipata 9101 (2026/7) ha cfg.GestioneFatture.FkIdEnte in MAIUSCOLO mentre pfd.Enti e'
        // lowercase. La vista (JOIN SQL case-insensitive) la restituisce con istitutioID MAIUSCOLO; il
        // match C# con EnteSQLBuilder (IdEnte lowercase) DEVE essere case-insensitive, altrimenti la riga
        // viene scartata -> lista vuota -> 404 (era il bug reale su api/fatture?Cancellata=true).
        var f = (await Query(true, 2026, 7)).SingleOrDefault(x => x.IdFattura == 9101);

        Assert.That(f, Is.Not.Null, "La Non Fatturata con casing ente diverso deve essere restituita (match case-insensitive).");
        Assert.Multiple(() =>
        {
            Assert.That(f!.Inviata, Is.EqualTo(4), "Posticipata -> marker inviata=4.");
            Assert.That(f.TipologiaFattura, Is.EqualTo("SECONDO SALDO"));
            Assert.That(f.RagioneSociale, Is.EqualTo("Ente Casing Test"),
                "L'enrichment ente deve popolare la RagioneSociale nonostante il casing diverso.");
        });
    }

    // =================== Casi "rompi api/fatture" (robustezza di PostFattureByRicercaAsync) ===================

    [Test]
    public async Task NonFatturate_PeriodoAssurdo_NonLancia_RestituisceVuoto()
        // Mese 13 / anno lontano: la query deve eseguire senza eccezioni e tornare vuoto (endpoint -> 404).
        => Assert.That(await Query(true, 1999, 13), Is.Empty);

    [Test]
    public async Task Emesse_PeriodoAssurdo_NonLancia_RestituisceVuoto()
        => Assert.That(await Query(false, 1999, 13), Is.Empty);

    [Test]
    public async Task NonFatturate_FiltroIdEnti_NonMatchante_RestituisceVuoto()
    {
        var rows = await Query(true, 2026, 7, idEnti: new[] { "00000000-0000-0000-0000-000000000000" });
        Assert.That(rows, Is.Empty, "Un IdEnti che non matcha nessun ente svuota il risultato (nessun 500).");
    }

    [Test]
    public async Task NonFatturate_FiltroIdEnti_Matchante_RestituisceSoloQuellEnte()
    {
        // Ente della posticipata casing (lowercase, come pfd.Enti): filtro IdEnti + enrichment case-insensitive.
        var rows = await Query(true, 2026, 7, idEnti: new[] { "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee" });
        Assert.That(rows.Select(x => x.IdFattura), Is.EqualTo(new long?[] { 9101 }));
    }

    [Test]
    public async Task NonFatturate_FatturaInviata_IgnorataSulRamoNonFatturate()
    {
        // Il ramo NON FATTURATE non applica @FatturaInviata (la vista espone marker 3/4, non lo stato reale):
        // 0 e 1 devono dare lo stesso insieme non vuoto.
        var a = (await Query(true, 2024, 2, fatturaInviata: 0)).Select(x => x.IdFattura).OrderBy(x => x).ToList();
        var b = (await Query(true, 2024, 2, fatturaInviata: 1)).Select(x => x.IdFattura).OrderBy(x => x).ToList();
        Assert.That(a, Is.EqualTo(b).And.Not.Empty);
    }

    [Test]
    public async Task Emesse_FkIdTipoContrattoInesistente_RestituisceVuoto()
        => Assert.That(await Query(false, 2024, 3, fkIdTipoContratto: 999), Is.Empty);

    [Test]
    public async Task NonFatturate_TipologiaMultiplaMista_RestituisceSoloLeMatchanti()
    {
        // Tipologia valida + inesistente nello stesso IN: torna solo la valida, nessun errore.
        var rows = await Query(true, 2024, 2, tipologia: new[] { "ANTICIPO", "__NO_MATCH__" });
        Assert.Multiple(() =>
        {
            Assert.That(rows.Any(x => x.IdFattura == 5001), Is.True, "L'ANTICIPO 5001 deve esserci.");
            Assert.That(rows.All(x => x.TipologiaFattura == "ANTICIPO"), Is.True, "Nessuna tipologia diversa.");
        });
    }

    // =================== Effetto azioni sulle liste: Ripristina/Cancella re-inclusione ===================

    [Test]
    public async Task Emesse_DopoRipristino_RicompareNellaLista()
    {
        // 6002 (ente1/SECONDO SALDO/2024/5) e' RIPRISTINATA (cfg Stato=1): SelectView filtra
        // (gf.Stato <> 0 OR IS NULL), quindi la ripristinata rientra tra le emesse (a differenza della posticipata).
        var rows = await Query(false, 2024, 5);
        Assert.That(rows.Any(x => x.IdFattura == 6002), Is.True, "Una RIPRISTINATA (Stato=1) rientra nelle emesse.");
    }

    [Test]
    public async Task Emesse_DopoCancella_RicompareNellaLista()
    {
        // 6003 (ente1/SECONDO SALDO/2024/6) e' CANCELLATA (cfg Stato=2): anche questa rientra (Stato <> 0).
        var rows = await Query(false, 2024, 6);
        Assert.That(rows.Any(x => x.IdFattura == 6003), Is.True, "Una CANCELLATA (Stato=2) rientra nelle emesse.");
    }

    [Test]
    public async Task DiscrepanzaDaInviare_RipristinataInEmesseMaNonInDaInviare()
    {
        // FINDING (non fix): due meccanismi di esclusione diversi. La ricerca EMESSE
        // (FattureQueryRicercaBuilder) esclude solo Stato=0; il vwDettaglioFattureDaInviare esclude QUALUNQUE
        // riga presente in cfg.GestioneFatture. Quindi la ripristinata 6002 compare in emesse ma resta esclusa
        // dal "da inviare" -> le due viste danno risposte incoerenti sullo stesso periodo.
        var emesse = await Query(false, 2024, 5);
        var daInviare = (await _handler.Send(new FattureInvioSapMultiploPeriodoQuery(AdminAuth())
        {
            AnnoRiferimento = 2024,
            MeseRiferimento = 5,
            TipologiaFattura = "SECONDO SALDO"
        }))?.ToList() ?? new List<FatturaInvioMultiploSapPeriodo>();

        Assert.Multiple(() =>
        {
            Assert.That(emesse.Any(x => x.IdFattura == 6002), Is.True, "In emesse la ripristinata 6002 c'e'.");
            Assert.That(daInviare.Any(x => x.IdFattura == 6002), Is.False,
                "Nel 'da inviare' 6002 e' esclusa perche' ha una riga in cfg.GestioneFatture (qualunque stato).");
        });
    }
}
