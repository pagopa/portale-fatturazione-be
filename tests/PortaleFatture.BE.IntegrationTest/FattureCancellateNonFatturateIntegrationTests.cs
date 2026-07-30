using Dapper;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test (DB seedato) su api/fatture con Cancellata=true (pagina Documenti Emessi, filtro
/// Stato = Non Fatturate). Dopo la modifica, _sqlViewCancellate AVVOLGE la vista
/// be.vwDocumentiEmessiNonFatturati (Eliminate inviata=3 + Posticipate inviata=4) preservando il
/// contratto JSON listaFatture -> FattureListaDto (JsonTypeHandler) e l'arricchimento ente in C#.
///
/// Seed DEDICATO (Anno 2024): 5001/5002 Eliminate (5002 senza righe -> posizioni null), 4001 Posticipata.
/// </summary>
public class FattureCancellateNonFatturateIntegrationTests
{
    private IMediator _handler = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
        // Il JSON di listaFatture -> FattureListaDto e' deserializzato via JsonTypeHandler, registrato
        // in produzione dalla config dell'API. Nei test va registrato esplicitamente (Dapper globale).
        => SqlMapper.AddTypeHandler(typeof(FattureListaDto), new JsonTypeHandler());

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    private static AuthenticationInfo AdminAuth() => new()
    { IdEnte = Guid.NewGuid().ToString(), Prodotto = "prod-pn", Ruolo = Ruolo.ADMIN, IdTipoContratto = 1 };

    private Task<FattureListaDto?> Query(int anno, int mese, string[]? tipologia = null, int? fkIdTipoContratto = null) =>
        _handler.Send(new FattureQueryRicerca(AdminAuth())
        {
            Cancellata = true,
            Anno = anno,
            Mese = mese,
            TipologiaFattura = tipologia,
            FkIdTipoContratto = fkIdTipoContratto
        });

    [Test]
    public async Task Eliminate_2024_2_RestituisceEntrambe_Inviata3_ePosizioniNullSenzaRighe()
    {
        var rows = await Query(2024, 2) ?? new FattureListaDto();

        var f5001 = rows.FirstOrDefault(x => x.fattura!.IdFattura == 5001)?.fattura;
        var f5002 = rows.FirstOrDefault(x => x.fattura!.IdFattura == 5002)?.fattura;

        Assert.Multiple(() =>
        {
            Assert.That(f5001, Is.Not.Null, "5001 (Eliminate) deve comparire.");
            Assert.That(f5002, Is.Not.Null, "5002 (Eliminate senza righe) deve comparire.");
            Assert.That(f5001!.Inviata, Is.EqualTo(3), "Eliminate -> inviata=3 (label ELIMINATA).");
            Assert.That(f5001.RagioneSociale, Is.EqualTo("Ente Test 3"), "Ente arricchito in C#.");
            Assert.That(f5001.Posizioni, Is.Not.Null, "5001 ha righe.");
            Assert.That(f5001.Posizioni!, Has.Count.EqualTo(2), "5001 ha 2 posizioni.");
            Assert.That(f5002!.Inviata, Is.EqualTo(3));
            Assert.That(f5002.Posizioni, Is.Null.Or.Empty, "5002 senza righe -> posizioni null (FOR JSON su set vuoto).");
        });
    }

    [Test]
    public async Task Posticipate_2024_1_RestituiscePosticipata_Inviata4()
    {
        var f4001 = (await Query(2024, 1) ?? new FattureListaDto())
            .FirstOrDefault(x => x.fattura!.IdFattura == 4001)?.fattura;

        Assert.Multiple(() =>
        {
            Assert.That(f4001, Is.Not.Null, "4001 (Posticipata) deve comparire.");
            Assert.That(f4001!.Inviata, Is.EqualTo(4), "Posticipate -> inviata=4 (label POSTICIPATA).");
            Assert.That(f4001.TipologiaFattura, Is.EqualTo("SECONDO SALDO"));
            Assert.That(f4001.RagioneSociale, Is.EqualTo("Ente Test 1"));
            Assert.That(f4001.Posizioni, Is.Not.Null);
            Assert.That(f4001.Posizioni!, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task FiltroTipologia_ApplicatoSullaColonnaDellaVista()
    {
        var rows = await Query(2024, 2, new[] { "ANTICIPO" }) ?? new FattureListaDto();

        Assert.Multiple(() =>
        {
            Assert.That(rows.Any(x => x.fattura!.IdFattura == 5001), Is.True, "ANTICIPO 5001 presente col filtro tipologia.");
            Assert.That(rows.Any(x => x.fattura!.IdFattura == 5002), Is.False, "ACCONTO 5002 escluso dal filtro tipologia.");
        });
    }

    [Test]
    public async Task FiltroFkIdTipoContratto_ApplicatoSullaVista()
    {
        // Eliminate 5001/5002 sono ente3 -> tipocontratto=1; la posticipata 4001 (ente1) e' tipo=2.
        var tipo1 = await Query(2024, 2, fkIdTipoContratto: 1) ?? new FattureListaDto();
        var tipo2 = await Query(2024, 2, fkIdTipoContratto: 2) ?? new FattureListaDto();

        Assert.Multiple(() =>
        {
            Assert.That(tipo1.Any(x => x.fattura!.IdFattura == 5001), Is.True, "tipocontratto=1 include l'eliminate ente3.");
            Assert.That(tipo2.Any(x => x.fattura!.IdFattura == 5001), Is.False, "tipocontratto=2 esclude l'eliminate ente3 (tipo1).");
        });
    }

    [Test]
    public async Task PeriodoSenzaNonFatturate_RestituisceVuoto()
    {
        var rows = await Query(2024, 11) ?? new FattureListaDto();
        Assert.That(rows, Is.Empty);
    }
}
