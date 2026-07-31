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
        string[]? tipologia = null, int? fkIdTipoContratto = null, int? fatturaInviata = null)
    {
        var res = await _handler.Send(new FattureQueryRicerca(AdminAuth())
        {
            Cancellata = cancellata,
            Anno = anno,
            Mese = mese,
            TipologiaFattura = tipologia,
            FkIdTipoContratto = fkIdTipoContratto,
            FatturaInviata = fatturaInviata
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
}
