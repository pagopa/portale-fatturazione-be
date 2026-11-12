using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Extensions;
using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Gateway.Storage.AppIO;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Download del file del financial report APP IO (POST api/appio/financialreports/blob/download,
/// 12/11/2026): la logica che non dipende da DB e storage — lettura di file_path, quando il file è
/// leggibile con la configurazione, validazione della richiesta, nome e tipo del file consegnato.
/// L'esecuzione reale è coperta da Http/AppIoFinancialReportsFileHttpTests.
/// </summary>
[Category("AppIO")]
public class AppIoFinancialReportFileTests
{
    private const string Abfss = "abfss://appioexport@stappio.dfs.core.windows.net/export/sap/2026/1/appio-final_report-2026-q1.csv";

    // --- AppIoFilePath.Parse ---

    /// <summary>
    /// La forma scritta da DATA: abfss://container@account.dfs.core.windows.net/percorso.
    /// </summary>
    [Test]
    public void Parse_Abfss_ShouldRicavareAccountContainerEPercorso()
    {
        var file = AppIoFilePath.Parse(Abfss);

        Assert.That(file, Is.EqualTo(new AppIoFilePath("stappio", "appioexport", "export/sap/2026/1/appio-final_report-2026-q1.csv")));
    }

    /// <summary>
    /// Le altre forme accettate: abfs senza TLS, URL https del blob, percorso relativo (con o senza
    /// barra iniziale), spazi attorno, caratteri codificati nel percorso.
    /// </summary>
    [TestCase("abfs://c@acc.dfs.core.windows.net/a/b.csv", "acc", "c", "a/b.csv")]
    [TestCase("  abfss://c@acc.dfs.core.windows.net/a/b.csv  ", "acc", "c", "a/b.csv")]
    [TestCase("abfss://c@acc.dfs.core.windows.net/a%20b/c.csv", "acc", "c", "a b/c.csv")]
    [TestCase("https://acc.blob.core.windows.net/c/a/b.csv", "acc", "c", "a/b.csv")]
    [TestCase("export/sap/2026/1/x.csv", null, null, "export/sap/2026/1/x.csv")]
    [TestCase("/export/sap/2026/1/x.csv", null, null, "export/sap/2026/1/x.csv")]
    public void Parse_AltreForme_ShouldRicavareLaPosizione(string filePath, string? account, string? container, string blob)
        => Assert.That(AppIoFilePath.Parse(filePath), Is.EqualTo(new AppIoFilePath(account, container, blob)));

    // --- CanRead ---

    private static AppIoFinancialReportStorageService Servizio(string? account = "stappio", string? chiave = "Y2hpYXZl", string? container = null)
        => new(new PortaleFattureOptions { StorageAppIO = new StorageAppIO { AccountName = account, AccountKey = chiave, BlobContainerName = container } });

    /// <summary>
    /// Senza account o senza chiave il download non è disponibile, qualunque sia il percorso.
    /// </summary>
    [TestCase(null, "Y2hpYXZl")]
    [TestCase("stappio", null)]
    [TestCase("", "Y2hpYXZl")]
    [TestCase("stappio", "  ")]
    public void CanRead_StorageNonConfigurato_ShouldEssereFalse(string? account, string? chiave)
        => Assert.That(Servizio(account, chiave).CanRead(Abfss), Is.False);

    /// <summary>
    /// Sezione di configurazione assente del tutto: false, senza eccezioni.
    /// </summary>
    [Test]
    public void CanRead_SezioneAssente_ShouldEssereFalse()
        => Assert.That(new AppIoFinancialReportStorageService(new PortaleFattureOptions()).CanRead(Abfss), Is.False);

    /// <summary>
    /// La chiave vale per un solo account: si legge solo un percorso dello stesso account (il confronto
    /// non distingue maiuscole e minuscole). Un file_path che indica un altro account non è leggibile.
    /// </summary>
    [TestCase("stappio", true)]
    [TestCase("STAPPIO", true)]
    [TestCase("altroaccount", false)]
    [TestCase("stappio2", false)]
    public void CanRead_AccountDelPercorso_ShouldCombaciareConQuelloConfigurato(string configurato, bool atteso)
        => Assert.That(Servizio(configurato).CanRead(Abfss), Is.EqualTo(atteso));

    /// <summary>
    /// Un percorso relativo non indica il container: serve quello di configurazione.
    /// </summary>
    [TestCase(null, false)]
    [TestCase("appioexport", true)]
    public void CanRead_PercorsoRelativo_ShouldRichiedereIlContainerDiConfigurazione(string? container, bool atteso)
        => Assert.That(Servizio(container: container).CanRead("export/sap/2026/1/x.csv"), Is.EqualTo(atteso));

    /// <summary>
    /// Un percorso senza nome di file non è leggibile.
    /// </summary>
    [TestCase("abfss://appioexport@stappio.dfs.core.windows.net/")]
    [TestCase("abfss://appioexport@stappio.dfs.core.windows.net")]
    public void CanRead_PercorsoSenzaFile_ShouldEssereFalse(string filePath)
        => Assert.That(Servizio().CanRead(filePath), Is.False);

    // --- richiesta ---

    private static AppIoFinancialReportsFileRequest Richiesta(int? year, int? quarter) => new() { Year = year, Quarter = quarter };

    /// <summary>
    /// Anno positivo e trimestre da 1 a 4, entrambi obbligatori.
    /// </summary>
    [TestCase(2026, 1, true)]
    [TestCase(2026, 4, true)]
    [TestCase(2026, 0, false)]
    [TestCase(2026, 5, false)]
    [TestCase(0, 1, false)]
    [TestCase(-1, 1, false)]
    [TestCase(null, 1, false)]
    [TestCase(2026, null, false)]
    public void IsValida_ShouldRichiedereAnnoPositivoETrimestreDa1A4(int? year, int? quarter, bool atteso)
        => Assert.That(Richiesta(year, quarter).IsValida(), Is.EqualTo(atteso));

    /// <summary>
    /// Una richiesta assente non è valida.
    /// </summary>
    [Test]
    public void IsValida_RichiestaAssente_ShouldEssereFalse()
        => Assert.That(((AppIoFinancialReportsFileRequest?)null).IsValida(), Is.False);

    /// <summary>
    /// La richiesta diventa la query con anno e trimestre.
    /// </summary>
    [Test]
    public void Map_ShouldPortareAnnoETrimestre()
    {
        var query = Richiesta(2026, 2).Map(new AuthenticationInfo { Id = "utente-test", Prodotto = "prod-appio", Ruolo = Ruolo.ADMIN });

        Assert.Multiple(() =>
        {
            Assert.That(query.Year, Is.EqualTo(2026));
            Assert.That(query.Quarter, Is.EqualTo(2));
        });
    }

    // --- nome e tipo del file ---

    /// <summary>
    /// Il nome consegnato al client è costruito dal trimestre: del percorso resta solo l'estensione,
    /// e solo se è breve e alfanumerica (niente pezzi di percorso, query string o caratteri strani).
    /// </summary>
    [TestCase(Abfss, "financial-report-appio-2026_1.csv")]
    [TestCase("export/x.XLSX", "financial-report-appio-2026_1.xlsx")]
    [TestCase("export/senza-estensione", "financial-report-appio-2026_1")]
    [TestCase("export/x.csv?sig=segreto", "financial-report-appio-2026_1.csv")]
    [TestCase("export/x.estensionelunghissima", "financial-report-appio-2026_1")]
    [TestCase("export/x.c$v", "financial-report-appio-2026_1")]
    [TestCase("export/x.csv/../../etc/passwd", "financial-report-appio-2026_1")]
    public void FileName_ShouldUsareIlTrimestreESoloLEstensioneDelPercorso(string filePath, string atteso)
        => Assert.That(Richiesta(2026, 1).FileName(filePath), Is.EqualTo(atteso));

    /// <summary>
    /// Il tipo di contenuto viene dall'estensione; se non è riconosciuta, è un file binario generico.
    /// </summary>
    [TestCase(Abfss, "text/csv")]
    [TestCase("x.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [TestCase("x.xls", "application/vnd.ms-excel")]
    [TestCase("x.parquet", "application/octet-stream")]
    [TestCase("x", "application/octet-stream")]
    public void MimeType_ShouldDipendereDallEstensione(string filePath, string atteso)
        => Assert.That(filePath.MimeType(), Is.EqualTo(atteso));

    // --- query ---

    /// <summary>
    /// Il percorso si legge dalla vista, una sola riga (il dato è ripetuto uguale su tutte le righe
    /// del trimestre), filtrando per anno e trimestre con parametri.
    /// </summary>
    [Test]
    public void SelectFilePath_ShouldLeggereUnaRigaDallaVistaPerAnnoETrimestre()
    {
        var sql = AppIoFinancialReportSQLBuilder.SelectFilePath();

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("SELECT TOP 1 f.[file_path]"));
            Assert.That(sql, Does.Contain("FROM [be].[vwAppioFinalReport] f"));
            Assert.That(sql, Does.Contain("f.[year] = @Year"));
            Assert.That(sql, Does.Contain("f.[quarter] = @Quarter"));
            Assert.That(sql, Does.Not.Contain("appio."));
        });
    }
}
