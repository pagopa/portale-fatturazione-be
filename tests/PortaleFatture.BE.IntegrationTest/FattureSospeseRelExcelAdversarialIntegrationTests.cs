using System.Data;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.IntegrationTest;

[NonParallelizable]
public class FattureSospeseRelExcelAdversarialIntegrationTests
{
    private const int Year = 2091;
    private const string Tipologia = "SECONDO SALDO";
    private const string EntePac = "11111111-1111-1111-1111-111111111111";
    private const string EntePal = "33333333-3333-3333-3333-333333333333";
    private SqlConnection _connection = null!;
    private SqlTransaction _transaction = null!;

    [SetUp]
    public async Task Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _connection = new SqlConnection(LocalTestDb.ConnectionString);
        await _connection.OpenAsync();
        _transaction = _connection.BeginTransaction();
    }

    [TearDown]
    public void Cleanup()
    {
        try
        {
            _transaction?.Rollback();
        }
        finally
        {
            _transaction?.Dispose();
            _connection?.Dispose();
        }
    }

    [Test]
    public async Task DuplicateProjectedRows_AreCountedOnce_ButDifferentAmountsAndMaterialsRemain()
    {
        var id = await Invoice(7002, 250m);
        await Line(id, "STORNO ANT. NA", 10m);
        await Line(id, "STORNO ANT. NA", 10m);
        await Line(id, "STORNO ANT. NA", 15m);
        await Line(id, "STORNO ANTICIPO ND", 20m);
        await Line(id, "STORNO ACCONTO NA", 30m);
        await Line(id, "STORNO ACCONTO ND", 40m);
        await Line(id, "MATERIALE", 100m);

        var rows = await Read(Query());

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].TotaleFatturaImponibile, Is.EqualTo(250m));
            Assert.That(rows[0].StornoAnticipoAnalogico, Is.EqualTo(-25m));
            Assert.That(rows[0].StornoAnticipoDigitale, Is.EqualTo(-20m));
            Assert.That(rows[0].StornoAccontoAnalogico, Is.EqualTo(-30m));
            Assert.That(rows[0].StornoAccontoDigitale, Is.EqualTo(-40m));
        });
    }

    [Test]
    public async Task NotesOnlyInvoice_IsExcluded_WithoutRemovingOverlappingRelStorno()
    {
        var noteId = await Invoice(7001, -99999m);
        await Line(noteId, "STORNO ANT. NA", 99999m);
        var relId = await Invoice(7002, 250m);
        await Line(relId, "STORNO ANT. NA", 25m);

        var noteCount = await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM be.vwFattureSospeseNoteReport WHERE IdFattura = @noteId",
            new { noteId }, _transaction);
        Assert.That(noteCount, Is.EqualTo(1), "La precondizione deve esercitare una riga esclusiva delle note.");

        var rows = await Read(Query());
        Assert.That(rows.Select(x => x.IdFattura), Is.EqualTo(new[] { relId.ToString() }));
        Assert.That(rows[0].StornoAnticipoAnalogico, Is.EqualTo(-25m));

        var data = new List<IEnumerable<FattureRelSospeseExcelDto>>
        {
            rows, rows, Array.Empty<FattureRelSospeseExcelDto>()
        };
        using var workbook = new XLWorkbook(new MemoryStream(data.ReportFattureSospeseRel("Febbraio", Tipologia)));
        var sheet = workbook.Worksheet("Enti Fatt. Sospese Febbraio");
        var amount = Column(sheet, "Totale Fattura Imponibile \u20ac");
        var storno = Column(sheet, "Storno Anticipo Analogico \u20ac");
        var totalsRow = sheet.RowsUsed().Single(r => r.Cell(1).GetString() == "Totali:").RowNumber();
        Assert.Multiple(() =>
        {
            Assert.That(sheet.LastRowUsed()!.RowNumber(), Is.EqualTo(4));
            Assert.That(sheet.Cell(2, amount).GetValue<decimal>(), Is.EqualTo(250m));
            Assert.That(sheet.Cell(totalsRow, amount).GetValue<decimal>(), Is.EqualTo(250m));
            Assert.That(sheet.Cell(totalsRow, storno).GetValue<decimal>(), Is.EqualTo(-25m));
        });
    }

    [TestCase(null, 2)]
    [TestCase(0, 1)]
    [TestCase(1, 0)]
    [TestCase(2, 1)]
    public async Task InvoiceSentFilter_PreservesNullAndExplicitStatuses(int? sent, int count)
    {
        var nullId = await Invoice(7001, 100m, sent: null);
        var notSentId = await Invoice(7002, 200m, sent: false);
        await Line(nullId, "MATERIALE", 100m);
        await Line(notSentId, "MATERIALE", 200m);
        var query = Query();
        query.FatturaInviata = sent;

        var rows = await Read(query);

        Assert.That(rows, Has.Count.EqualTo(count));
        var expected = sent == 0 ? new[] { notSentId.ToString() }
            : sent == 2 ? new[] { nullId.ToString() }
            : sent == 1 ? Array.Empty<string>() : new[] { notSentId.ToString(), nullId.ToString() };
        Assert.That(rows.Select(x => x.IdFattura), Is.EqualTo(expected));
    }

    [Test]
    public async Task InvoiceSentFilter_One_SelectsOnlySentInvoices()
    {
        var id = await Invoice(7001, 100m, sent: true);
        await Line(id, "MATERIALE", 100m);
        var query = Query();
        query.FatturaInviata = 1;
        Assert.That((await Read(query)).Select(x => x.IdFattura), Is.EqualTo(new[] { id.ToString() }));
        query.FatturaInviata = 2;
        Assert.That(await Read(query), Is.Empty);
    }

    [TestCase("empty", 2)]
    [TestCase("duplicate", 1)]
    [TestCase("missing", 0)]
    [TestCase("hostile", 0)]
    public async Task EntityFilter_DoesNotDuplicateRowsOrInterpretValuesAsSql(string mode, int count)
    {
        var pacId = await Invoice(7001, 100m);
        var palId = await Invoice(7002, 200m);
        await Line(pacId, "MATERIALE", 100m);
        await Line(palId, "MATERIALE", 200m);
        var query = Query();
        query.IdEnti = mode switch
        {
            "empty" => Array.Empty<string>(),
            "duplicate" => new[] { EntePac, EntePac },
            "missing" => new[] { "missing-ente" },
            _ => new[] { "' OR 1=1 --" }
        };

        var rows = await Read(query);

        Assert.That(rows, Has.Count.EqualTo(count));
        if (count == 1)
            Assert.That(rows[0].IdEnte, Is.EqualTo(EntePac));
        if (count == 2)
            Assert.That(rows.Select(x => x.IdEnte), Is.EquivalentTo(new[] { EntePac, EntePal }));
    }

    [TestCase(2090, 2, Tipologia, null, 0)]
    [TestCase(Year, 3, Tipologia, null, 0)]
    [TestCase(Year, 2, "PRIMO SALDO", null, 0)]
    [TestCase(Year, 2, Tipologia, 1, 1)]
    [TestCase(Year, 2, Tipologia, 2, 1)]
    [TestCase(Year, 2, Tipologia, 99, 0)]
    public async Task PeriodAndContractFilters_SelectOnlyMatchingRows(int year, int month, string tipologia, int? contract, int count)
    {
        var pacId = await Invoice(7001, 100m);
        var palId = await Invoice(7002, 200m);
        await Line(pacId, "MATERIALE", 100m);
        await Line(palId, "MATERIALE", 200m);
        var query = Query();
        query.Anno = year;
        query.Mese = month;
        query.TipologiaFattura = tipologia;
        query.FkIdTipoContratto = contract;

        var rows = await Read(query);

        Assert.That(rows, Has.Count.EqualTo(count));
        if (count == 1)
            Assert.That(rows[0].IdEnte, Is.EqualTo(contract == 1 ? EntePal : EntePac));
    }

    [TestCase("TD01", 300)]
    [TestCase("TD04", -300)]
    public async Task DocumentSign_AndFinalDescendingOrder_ArePreserved(string document, int expected)
    {
        var pacId = await Invoice(7001, 300m, document: document);
        var palId = await Invoice(7002, 200m);
        await Line(pacId, "MATERIALE", 300m);
        await Line(palId, "MATERIALE", 200m);

        var rows = await Read(Query());

        Assert.That(rows.Single(x => x.IdEnte == EntePac).TotaleFatturaImponibile, Is.EqualTo(expected));
        Assert.That(rows.Select(x => x.TotaleFatturaImponibile),
            Is.EqualTo(document == "TD01" ? new[] { 300m, 200m } : new[] { 200m, -300m }));
    }

    [Test]
    public async Task InvoiceWithoutLines_ThrowsDiagnosticDataException()
    {
        var id = await Invoice(7001, 100m);
        var error = Assert.ThrowsAsync<DataException>(() => Read(Query()));
        Assert.That(error!.Message, Does.Contain("CodiceMateriale NULL").And.Contain(EntePac).And.Contain(id.ToString()));
    }

    private async Task<long> Invoice(long template, decimal total, bool? sent = false, string document = "TD01") =>
        await _connection.QuerySingleAsync<long>("""
            INSERT INTO pfd.tmpFattureTestata
                (FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, DataFattura,
                 IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento, AnnoRiferimento,
                 MeseRiferimento, CodiceContratto, Progressivo, FatturaInviata, FlagFatturata)
            OUTPUT INSERTED.IdFattura
            SELECT FkProdotto, @document, FkTipologiaFattura, FkIdEnte, DataFattura,
                   'HOTFIX-SOSPESE', @total, Divisa, MetodoPagamento, @year,
                   2, CodiceContratto, Progressivo, @sent, 0
            FROM pfd.tmpFattureTestata WHERE IdFattura = @template
            """, new { template, total, document, sent, year = Year }, _transaction);

    private async Task Line(long id, string material, decimal amount) =>
        await _connection.ExecuteAsync("""
            INSERT INTO pfd.tmpFattureRighe
                (FkIdFattura, NumeroLinea, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo)
            VALUES (@id, 1, @material, 1, @amount, @amount, 0)
            """, new { id, material, amount }, _transaction);

    private async Task<List<FattureRelSospeseExcelDto>> Read(FattureSospeseRelExcelQuery query) =>
        (await new FattureSospeseUnionRelExcelPersistence(query).Execute(_connection, "pfd", _transaction))!.ToList();

    private static FattureSospeseRelExcelQuery Query() => new(new AuthenticationInfo())
    {
        Anno = Year, Mese = 2, TipologiaFattura = Tipologia
    };

    private static int Column(IXLWorksheet sheet, string caption) =>
        sheet.Row(1).CellsUsed().Single(x => x.GetString() == caption).Address.ColumnNumber;
}
