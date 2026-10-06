using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Extensions;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Extensions;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence.Builder;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Documenti contabili APP IO (PF-908): la logica che non dipende dal DB — composizione del filtro,
/// raggruppamento delle righe in documenti e fogli del Financial Report. L'esecuzione reale è
/// coperta su DB seedato da Http/AppIoFinancialReportsHttpTests.
/// </summary>
public class AppIoFinancialReportTests
{
    private sealed class Filtro : IAppIoFinancialReportFiltro
    {
        public string[]? ContractIds { get; init; }
        public string[]? YearQuarter { get; init; }
        public string? Year { get; init; }
    }

    private static IDictionary<string, object?> Parametri(System.Dynamic.ExpandoObject p) => p;

    // --- Where ---

    /// <summary>
    /// Senza trimestri né anno si cerca il trimestre più recente fra le posizioni.
    /// </summary>
    [Test]
    public void Where_SenzaFiltri_ShouldCercareIlTrimestrePiuRecente()
    {
        var (where, p) = AppIoFinancialReportSQLBuilder.Where(new Filtro(), "p", "contract_id");

        Assert.Multiple(() =>
        {
            Assert.That(where, Is.EqualTo(" WHERE p.year_quarter = (SELECT MAX(year_quarter) FROM [be].[vwAppioFinancialReportPositions])"));
            Assert.That(Parametri(p), Is.Empty);
        });
    }

    /// <summary>
    /// Con il solo anno si cercano i trimestri che iniziano con 'AAAA_' (il '_' è protetto, in LIKE è un jolly).
    /// </summary>
    [Test]
    public void Where_SoloAnno_ShouldFiltrarePerPrefissoDellAnno()
    {
        var (where, p) = AppIoFinancialReportSQLBuilder.Where(new Filtro { Year = "2026" }, "p", "contract_id");

        Assert.Multiple(() =>
        {
            Assert.That(where, Is.EqualTo(" WHERE p.year_quarter LIKE @Year + '[_]%'"));
            Assert.That(Parametri(p)["Year"], Is.EqualTo("2026"));
        });
    }

    /// <summary>
    /// I trimestri prevalgono sull'anno, e il contratto si filtra sulla colonna della vista indicata.
    /// </summary>
    [Test]
    public void Where_TrimestriEContratti_ShouldPrevalereSullAnnoEUsareLaColonnaIndicata()
    {
        var filtro = new Filtro { Year = "2025", YearQuarter = ["2026_1"], ContractIds = ["C1"] };

        var (where, p) = AppIoFinancialReportSQLBuilder.Where(filtro, "r", "recipient_id");

        Assert.Multiple(() =>
        {
            Assert.That(where, Is.EqualTo(" WHERE r.year_quarter IN @YearQuarter AND r.recipient_id IN @ContractIds"));
            Assert.That(Parametri(p).ContainsKey("Year"), Is.False, "l'anno non deve finire nei parametri se ci sono i trimestri");
            Assert.That(Parametri(p)["ContractIds"], Is.EqualTo(new[] { "C1" }));
        });
    }

    /// <summary>
    /// Le query di righe e posizioni partono dalle posizioni e arricchiscono in LEFT JOIN: un
    /// documento senza report o senza contratto deve comparire lo stesso.
    /// </summary>
    [TestCase("SelectRighe")]
    [TestCase("SelectPosizioni")]
    public void Select_ShouldPartireDallePosizioniInLeftJoin(string metodo)
    {
        var sql = (string)typeof(AppIoFinancialReportSQLBuilder).GetMethod(metodo)!.Invoke(null, null)!;

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("FROM [be].[vwAppioFinancialReportPositions] p"));
            Assert.That(sql, Does.Contain("LEFT OUTER JOIN [be].[vwAppioFinancialReport] r"));
            Assert.That(sql, Does.Contain("LEFT OUTER JOIN [be].[vwAppioContracts] c"));
        });
    }

    // --- Raggruppa ---

    private static AppIoFinancialReportRigaDto Riga(string contratto, string trimestre, string numero, int progressivo)
        => new() { ContractId = contratto, YearQuarter = trimestre, Numero = numero, ProgressivoRiga = progressivo, Name = contratto };

    /// <summary>
    /// Le righe dello stesso documento diventano posizioni, nell'ordine di arrivo; il conteggio è dei documenti.
    /// </summary>
    [Test]
    public void Raggruppa_ShouldUnireLeRigheDelloStessoDocumento()
    {
        var righe = new[]
        {
            Riga("C1", "2026_2", "001", 1),
            Riga("C1", "2026_2", "001", 2),
            Riga("C2", "2026_2", "002", 1)
        };

        var lista = righe.Raggruppa();

        Assert.Multiple(() =>
        {
            Assert.That(lista.Count, Is.EqualTo(2));
            Assert.That(lista.FinancialReports!.Select(x => x.Key), Is.EqualTo(new[] { "C1|2026_2|001", "C2|2026_2|002" }));
            Assert.That(lista.FinancialReports!.First().Posizioni.Select(x => x.ProgressivoRiga), Is.EqualTo(new[] { 1, 2 }));
        });
    }

    /// <summary>
    /// Stesso numero ma trimestre o contratto diverso sono documenti distinti.
    /// </summary>
    [Test]
    public void Raggruppa_StessoNumeroSuTrimestriOContrattiDiversi_ShouldEssereDocumentiDistinti()
    {
        var righe = new[]
        {
            Riga("C1", "2026_1", "001", 1),
            Riga("C1", "2026_2", "001", 1),
            Riga("C2", "2026_2", "001", 1)
        };

        Assert.That(righe.Raggruppa().Count, Is.EqualTo(3));
    }

    /// <summary>
    /// Nessuna riga (o null) dà una lista vuota, non un errore: l'endpoint la traduce in 404.
    /// </summary>
    [Test]
    public void Raggruppa_NessunaRiga_ShouldDareListaVuota()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Array.Empty<AppIoFinancialReportRigaDto>().Raggruppa().Count, Is.Zero);
            Assert.That(((IEnumerable<AppIoFinancialReportRigaDto>?)null).Raggruppa().Count, Is.Zero);
        });
    }

    // --- MapExcel (Financial Report) ---

    /// <summary>
    /// Un foglio per trimestre e per vista, con l'anno nel nome: due Q1 di anni diversi non collidono.
    /// </summary>
    [Test]
    public void MapExcel_ShouldCreareUnFoglioPerTrimestreEVista()
    {
        var dto = new AppIoFinancialReportDettaglioDto
        {
            FinancialReports = [new() { YearQuarter = "2026_1" }, new() { YearQuarter = "2025_1" }],
            Posizioni = [new() { YearQuarter = "2026_1" }]
        };

        var nomi = dto.MapExcel()!.Tables.Cast<System.Data.DataTable>().Select(t => t.TableName);

        Assert.That(nomi, Is.EqualTo(new[] { "2025-q1-financial-report", "2026-q1-financial-report", "2026-q1-positions" }));
    }

    /// <summary>
    /// Nessuna riga in nessuna delle due viste: null, che l'endpoint traduce in 404.
    /// </summary>
    [Test]
    public void MapExcel_NessunaRiga_ShouldDareNull()
    {
        Assert.That(new AppIoFinancialReportDettaglioDto().MapExcel(), Is.Null);
    }
}
