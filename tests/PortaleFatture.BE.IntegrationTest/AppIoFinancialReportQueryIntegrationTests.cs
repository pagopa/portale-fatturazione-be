using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Documenti contabili APP IO (PF-908): query MediatR -> handler -> persistence -> viste
/// [be].[vwAppioFinancialReportPositions] / [be].[vwAppioFinancialReport] / [be].[vwAppioContracts]
/// sul DB seedato (tests/Data/appio.sql). Verifica a livello di handler ciò che la rotta HTTP
/// nasconde: tipi del DTO letti da Dapper, colonne in più del foglio "positions", le due query del
/// Financial Report nella stessa unit of work.
///
/// Seed: posizioni APPIO-FR-001 (C1, due righe) e APPIO-FR-002 (C2) in 2026_2, APPIO-FR-003 (C1) in
/// 2026_1, APPIO-FR-004 (C5) in 2025_4; financial report per C1 (2026_2 e 2026_1) e C3 (2026_2).
/// </summary>
public class AppIoFinancialReportQueryIntegrationTests
{
    private IMediator _handler = null!;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinancialReportPositions");
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinancialReport");
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    private static AuthenticationInfo Auth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-appio",
        Ruolo = Ruolo.ADMIN
    };

    /// <summary>
    /// I trimestri vengono dalle posizioni, dal più recente; con l'anno solo quelli dell'anno.
    /// </summary>
    [Test]
    public async Task Quarters_ShouldVenireDallePosizioni()
    {
        var tutti = await _handler.Send(new AppIoFinancialReportQuartersQuery(Auth()));
        var anno = await _handler.Send(new AppIoFinancialReportQuartersQuery(Auth()) { Year = "2026" });

        Assert.Multiple(() =>
        {
            Assert.That(tutti, Is.EqualTo(new[] { "2026_2", "2026_1", "2025_4" }));
            Assert.That(anno, Is.EqualTo(new[] { "2026_2", "2026_1" }));
        });
    }

    /// <summary>
    /// La griglia legge correttamente tutti i tipi (int, datetime, decimal(38,14)) e la VatCode
    /// ricade sul codice fiscale quando manca la partita IVA.
    /// </summary>
    [Test]
    public async Task Griglia_ShouldMappareTuttiITipiDelDocumento()
    {
        var lista = await _handler.Send(new AppIoFinancialReportQueryGetByRicerca(Auth()) { YearQuarter = ["2026_2"], ContractIds = ["APPIO-C1"] });

        var doc = lista.FinancialReports!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(lista.Count, Is.EqualTo(1));
            Assert.That(doc.Id, Is.EqualTo(1));
            Assert.That(doc.Data, Is.EqualTo(new DateTime(2026, 7, 15)));
            Assert.That(doc.TipoDoc, Is.EqualTo("TD01"));
            Assert.That(doc.Valuta, Is.EqualTo("EUR"));
            Assert.That(doc.Posizioni.Sum(p => p.Importo), Is.EqualTo(20m));
            Assert.That(doc.Posizioni.Select(p => p.Quantita), Is.EqualTo(new int?[] { 1000, 500 }));
        });
    }

    /// <summary>Senza partita IVA la VatCode è il codice fiscale (ISNULL nel builder).</summary>
    [Test]
    public async Task Griglia_SenzaPartitaIva_ShouldUsareIlCodiceFiscale()
    {
        var lista = await _handler.Send(new AppIoFinancialReportQueryGetByRicerca(Auth()) { YearQuarter = ["2025_4"] });

        Assert.That(lista.FinancialReports!.Single().VatCode, Is.EqualTo("00000000005"));
    }

    /// <summary>
    /// Il download "Documenti Contabili" è piatto: una riga per posizione, nello stesso ordine della griglia.
    /// </summary>
    [Test]
    public async Task Excel_ShouldRestituireUnaRigaPerPosizioneInOrdine()
    {
        var righe = (await _handler.Send(new AppIoFinancialReportQueryGetByRicercaExcel(Auth()) { YearQuarter = ["2026_2"] })).ToList();

        Assert.That(righe.Select(r => $"{r.Numero}/{r.ProgressivoRiga}"),
            Is.EqualTo(new[] { "APPIO-FR-001/1", "APPIO-FR-001/2", "APPIO-FR-002/1" }));
    }

    /// <summary>
    /// Il Financial Report legge report e posizioni nella stessa unit of work (era il punto del 500:
    /// senza transazione la seconda query trovava la connessione chiusa), con le colonne in più del
    /// foglio positions e il report anche dove non ci sono posizioni (C3).
    /// </summary>
    [Test]
    public async Task Dettaglio_ShouldLeggereReportEPosizioniConLoStessoFiltro()
    {
        var dettaglio = await _handler.Send(new AppIoFinancialReportQueryGetDettaglioExcel(Auth()) { YearQuarter = ["2026_2"] });

        var c5 = dettaglio.Posizioni!.First(p => p.ContractId == "APPIO-C1" && p.ProgressivoRiga == 1);
        Assert.Multiple(() =>
        {
            Assert.That(dettaglio.FinancialReports!.Select(r => r.RecipientId).Distinct(), Is.EquivalentTo(new[] { "APPIO-C1", "APPIO-C3" }));
            Assert.That(dettaglio.FinancialReports!.Single(r => r.RecipientId == "APPIO-C3").CurrentTrx, Is.EqualTo(50));
            Assert.That(dettaglio.Posizioni!.Count(), Is.EqualTo(3));
            Assert.That(c5.PrezzoUnit, Is.EqualTo("0,01"), "prezzo_unit è testo a DB");
            Assert.That(c5.PercentIva, Is.EqualTo("22"));
            Assert.That(c5.CodiceSdi, Is.EqualTo("AAAAAA1"));
            Assert.That(c5.Name, Is.EqualTo("Comune Alfa AppIO FR"));
        });
    }

    /// <summary>
    /// Il filtro per contratto sul report usa recipient_id: chiedendo C2 (senza report) il report è vuoto
    /// ma le posizioni ci sono.
    /// </summary>
    [Test]
    public async Task Dettaglio_FiltroContratto_ShouldUsareRecipientIdSulReport()
    {
        var dettaglio = await _handler.Send(new AppIoFinancialReportQueryGetDettaglioExcel(Auth()) { YearQuarter = ["2026_2"], ContractIds = ["APPIO-C2"] });

        Assert.Multiple(() =>
        {
            Assert.That(dettaglio.FinancialReports, Is.Empty);
            Assert.That(dettaglio.Posizioni!.Select(p => p.Numero), Is.EqualTo(new[] { "APPIO-FR-002" }));
        });
    }
}
