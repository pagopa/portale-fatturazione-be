using ClosedXML.Excel;
using MediatR;
using Microsoft.Extensions.Configuration;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test end-to-end (handler MediatR -> DB seedato) per il report Fatture Sospese, che legge
/// [be].[vwFattureSospeseReport] ed espone la colonna RelNonFirmata.
/// L'handler mantiene 3 bucket: { view, REL deduplicate, note vuote }.
///
/// Gira sul container seedato (LocalTestDb): le tabelle tmpFattureTestata/tmpFattureRighe/tmpRelTestata/
/// MesiFatture e le due viste sono in tests/Data/. Seed DEDICATO su Anno 2026 / Mese 2 / 'SECONDO SALDO'
/// (coincide coi default Conf*):
///   - 7001 ente1 (PAC/tipo2) + MesiFatture -> ramo VIEW, RelNonFirmata='Rel in attesa di firma';
///   - 7002 ente3 (PAL/tipo1) riga STORNO, senza MesiFatture, presente anche nel ramo REL.
///
/// I test "strutturali" (esecuzione, periodo vuoto, per-tipologia) validano schema/colonne e passano
/// anche senza dati; quelli "data-dependent" ora asseriscono sul seed (niente piu' Inconclusive).
/// </summary>
public class FattureSospeseRelExcelQueryTests
{
    private IMediator _handler;
    private IConfiguration _conf;

    // Il nome legacy UnionBranch identifica ora il report REL deduplicato.
    private const int ViewBranch = 0;
    private const int UnionBranch = 1;
    private const int NoteBranch = 2;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
        _conf = ServiceProvider.GetRequiredService<IConfiguration>();
    }

    private static AuthenticationInfo AdminAuth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    private int ConfAnno => int.TryParse(_conf["IntegrationTest:Anno"], out var a) ? a : 2026;
    private int ConfMese => int.TryParse(_conf["IntegrationTest:Mese"], out var m) ? m : 2;
    private string ConfTipologia => _conf["IntegrationTest:TipologiaFattura"] ?? "SECONDO SALDO";

    private FattureSospeseRelExcelQuery BuildSospeseQuery(
        int? anno = null, int? mese = null, string? tipologia = null,
        int? fkIdTipoContratto = null, int? fatturaInviata = null) => new(AdminAuth())
        {
            Anno = anno ?? ConfAnno,
            Mese = mese ?? ConfMese,
            TipologiaFattura = tipologia ?? ConfTipologia,
            FkIdTipoContratto = fkIdTipoContratto,
            FatturaInviata = fatturaInviata
        };

    // -------- Strutturali (no data) --------

    /// <summary>
    /// L'esecuzione senza eccezioni prova che lo schema/colonna sono corretti:
    /// [be].[vwFattureSospeseReport] espone RelNonFirmata e il terzo slot rimane vuoto.
    /// Una colonna/vista errata farebbe fallire la query con "Invalid column name".
    /// </summary>
    [Test]
    public async Task Query_ShouldExecute_AndReturnThreeBuckets()
    {
        var result = await _handler.Send(BuildSospeseQuery());

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(3));
        Assert.That(result[ViewBranch], Is.Not.Null);
        Assert.That(result[UnionBranch], Is.Not.Null);
        Assert.That(result[NoteBranch], Is.Not.Null.And.Empty);
    }

    /// <summary>
    /// Periodo sicuramente privo di dati: valida l'intera pipeline SQL/mapping senza dipendere
    /// dai dati. Deve ritornare 3 bucket vuoti senza eccezioni.
    /// </summary>
    [Test]
    public async Task Query_EmptyPeriod_ShouldReturnThreeEmptyBuckets()
    {
        var result = await _handler.Send(BuildSospeseQuery(anno: 1900, mese: 1));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(3));
        Assert.That(result.TrueForAll(b => b != null && !b.Any()), Is.True,
            "Con un periodo senza dati tutti e tre i bucket devono essere vuoti.");
    }

    /// <summary>
    /// I filtri opzionali devono selezionare esattamente le fatture previste dal seed.
    /// </summary>
    [TestCase(null, null, 2)]
    [TestCase(1, null, 1)]
    [TestCase(2, null, 1)]
    [TestCase(null, 0, 2)]
    [TestCase(null, 1, 0)]
    [TestCase(null, 2, 0)]
    public async Task Query_WithOptionalFilters_ReturnsExpectedRows(int? fkIdTipoContratto, int? fatturaInviata, int count)
    {
        var result = await _handler.Send(
            BuildSospeseQuery(fkIdTipoContratto: fkIdTipoContratto, fatturaInviata: fatturaInviata));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(3));
        var expected = fkIdTipoContratto == 1 ? new[] { "7002" }
            : fkIdTipoContratto == 2 ? new[] { "7001" } : new[] { "7001", "7002" };
        foreach (var branch in result.Take(2))
            Assert.That(branch.Select(x => x.IdFattura),
                Is.EquivalentTo(count == 0 ? Array.Empty<string>() : expected));
        Assert.That(result[NoteBranch], Is.Empty);
    }

    /// <summary>
    /// Schema/colonna validi per tutte le tipologie REL del report sospesi.
    /// </summary>
    [TestCase("PRIMO SALDO")]
    [TestCase("SECONDO SALDO")]
    [TestCase("VAR. SEMESTRALE")]
    [TestCase("SEM. SOSPESI")]
    public async Task Query_PerTipologia_ShouldExecute(string tipologia)
    {
        var result = await _handler.Send(BuildSospeseQuery(tipologia: tipologia));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(3));
    }

    // -------- Seed locale --------

    /// <summary>
    /// Nel ramo vista, RelNonFirmata deve essere costante per IdFattura (relazione 1:1):
    /// garantisce che la colonna sia preservata nel report.
    /// </summary>
    [Test]
    public async Task Query_ViewBranch_RelNonFirmata_IsConsistentPerIdFattura()
    {
        var result = await _handler.Send(BuildSospeseQuery());
        var viewRows = result![ViewBranch]?.ToList() ?? new List<FattureRelSospeseExcelDto>();

        Assert.That(viewRows.Count, Is.GreaterThan(0),
            "Il seed sospesi (7001/7002 su 2026/2/SECONDO SALDO) deve popolare il ramo view.");

        var fanOut = viewRows
            .GroupBy(x => x.IdFattura)
            .Where(g => g.Select(x => x.RelNonFirmata).Distinct().Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.That(fanOut, Is.Empty,
            "RelNonFirmata deve essere costante per IdFattura.");
    }

    /// <summary>
    /// Le note separate non sono piu' interrogate; lo slot resta presente per compatibilita'.
    /// </summary>
    [Test]
    public async Task Query_NoteBranch_IsEmpty()
    {
        var result = await _handler.Send(BuildSospeseQuery());
        var noteRows = result![NoteBranch]?.ToList() ?? new List<FattureRelSospeseExcelDto>();

        Assert.That(noteRows, Is.Empty);
    }

    /// <summary>
    /// Lo storno 7002 rimane nel ramo REL, ma non viene sommato di nuovo dal vecchio ramo note.
    /// </summary>
    [Test]
    public async Task Query_ReportBranch_PreservesRelAmounts_WithoutAddingNotesAgain()
    {
        var result = await _handler.Send(BuildSospeseQuery());
        var rows = result![UnionBranch].ToList();
        Assert.That(rows.Select(x => x.IdFattura), Is.EqualTo(new[] { "7001", "7002" }));
        Assert.Multiple(() =>
        {
            Assert.That(rows.Select(x => x.TotaleFatturaImponibile), Is.EqualTo(new[] { 1000m, 250m }));
            Assert.That(rows[0].RelNonFirmata, Is.EqualTo("Rel in attesa di firma"));
            Assert.That(rows[1].RelNonFirmata, Is.Empty);
            Assert.That(rows[1].StornoAnticipoAnalogico, Is.EqualTo(-250m));
            Assert.That(rows[1].StornoAnticipoDigitale, Is.Zero);
            Assert.That(result[ViewBranch].Single(x => x.IdFattura == "7002").StornoAnticipoAnalogico, Is.EqualTo(-250m));
        });
    }

    [Test]
    public async Task Reports_FromRealMediator_PreserveAmountsAndTotals()
    {
        var result = await _handler.Send(BuildSospeseQuery());
        using var sospese = new XLWorkbook(new MemoryStream(result!.ReportFattureSospeseRel("Febbraio", ConfTipologia)));
        AssertSheetAmounts(sospese.Worksheet("Enti Fatt. Sospese Febbraio"));

        var reports = await new FatturaRicercaRequest
        {
            Anno = ConfAnno, Mese = ConfMese, TipologiaFattura = new[] { ConfTipologia }
        }.ReportFatture(_handler, AdminAuth());
        Assert.That(reports, Has.Count.EqualTo(1));
        using var combined = new XLWorkbook(new MemoryStream(reports.Single().Value));
        var sheet = combined.Worksheets.Single(x => x.Name.StartsWith("Enti Fatt.") && x.Name.EndsWith("Sospesi"));
        AssertSheetAmounts(sheet);
    }

    private static void AssertSheetAmounts(IXLWorksheet sheet)
    {
        var amountColumn = sheet.Row(1).CellsUsed().Single(x => x.GetString() == "Totale Fattura Imponibile \u20ac").Address.ColumnNumber;
        var stornoColumn = sheet.Row(1).CellsUsed().Single(x => x.GetString() == "Storno Anticipo Analogico \u20ac").Address.ColumnNumber;
        var totalsRow = sheet.RowsUsed().Single(r => r.Cell(1).GetString() == "Totali:").RowNumber();
        Assert.Multiple(() =>
        {
            Assert.That(sheet.Cell(2, amountColumn).GetValue<decimal>(), Is.EqualTo(1000m));
            Assert.That(sheet.Cell(3, amountColumn).GetValue<decimal>(), Is.EqualTo(250m));
            Assert.That(sheet.Cell(totalsRow, amountColumn).GetValue<decimal>(), Is.EqualTo(1250m));
            Assert.That(sheet.Cell(3, stornoColumn).GetValue<decimal>(), Is.EqualTo(-250m));
            Assert.That(sheet.Cell(totalsRow, stornoColumn).GetValue<decimal>(), Is.EqualTo(-250m));
        });
    }

    // -------- Regressione report non-sospesi (DTO condiviso) --------

    /// <summary>
    /// Il report Fatture Emesse (non sospese) usa FattureRelExcelDto che, dopo la separazione dei DTO,
    /// NON espone piu' RelNonFirmata: l'assenza della colonna e' garantita a livello di tipo (compile-time)
    /// ed e' verificata dai unit test FattureRelDtoMetadataTests. Qui si verificano anche
    /// gli importi e i totali REL della fattura regolare 8001.
    /// </summary>
    [Test]
    public async Task RelExcelQuery_NonSospese_ShouldExecute()
    {
        var query = new FattureRelExcelQuery(AdminAuth())
        {
            Anno = ConfAnno,
            Mese = ConfMese,
            TipologiaFattura = ConfTipologia
        };

        var result = await _handler.Send(query);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(3));
        foreach (var branch in result.Take(2))
        {
            var row = branch.Single(x => x.IdFattura == "8001");
            Assert.Multiple(() =>
            {
                Assert.That(row.TotaleFatturaImponibile, Is.EqualTo(1500m));
                Assert.That(row.RelTotale, Is.EqualTo(300m));
                Assert.That(row.RelTotaleIvato, Is.EqualTo(366m));
                Assert.That(row.StornoAnticipoAnalogico, Is.Zero);
            });
        }
    }
}
