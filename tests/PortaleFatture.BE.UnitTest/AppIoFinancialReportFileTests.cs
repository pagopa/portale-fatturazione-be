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

    // --- Endpoint ---

    /// <summary>
    /// Senza Endpoint vale l'endpoint blob pubblico dell'account; con Endpoint vale quello, con o senza
    /// schema e senza la barra finale.
    /// </summary>
    [TestCase(null, "https://stappio.blob.core.windows.net/")]
    [TestCase("  ", "https://stappio.blob.core.windows.net/")]
    [TestCase("https://stappio.privatelink.blob.core.windows.net", "https://stappio.privatelink.blob.core.windows.net/")]
    [TestCase("https://storage.uat.example.it/", "https://storage.uat.example.it/")]
    [TestCase("storage.uat.example.it", "https://storage.uat.example.it/")]
    public void BlobServiceUri_ShouldUsareEndpointSeConfigurato(string? endpoint, string atteso)
        => Assert.That(AppIoFinancialReportStorageService.BlobServiceUri(new StorageAppIO { AccountName = "stappio", Endpoint = endpoint })!.ToString(),
            Is.EqualTo(atteso));

    /// <summary>
    /// Un Endpoint non https o non valido non viene usato: il file risulta non leggibile (503), non si
    /// tenta una connessione in chiaro né si va in eccezione.
    /// </summary>
    [TestCase("http://storage.uat.example.it")]
    [TestCase("https://")]
    [TestCase("ftp://storage.uat.example.it")]
    public void CanRead_EndpointNonValido_ShouldEssereFalse(string endpoint)
    {
        var servizio = new AppIoFinancialReportStorageService(new PortaleFattureOptions
        {
            StorageAppIO = new StorageAppIO { AccountName = "stappio", AccountKey = "Y2hpYXZl", Endpoint = endpoint }
        });
        Assert.That(servizio.CanRead(Abfss), Is.False);
    }

    /// <summary>
    /// Con un Endpoint valido resta il controllo sull'account di file_path: la chiave vale per un solo account.
    /// </summary>
    [TestCase("stappio", true)]
    [TestCase("altroaccount", false)]
    public void CanRead_EndpointValido_ShouldMantenereIlControlloSullAccount(string account, bool atteso)
    {
        var servizio = new AppIoFinancialReportStorageService(new PortaleFattureOptions
        {
            StorageAppIO = new StorageAppIO { AccountName = account, AccountKey = "Y2hpYXZl", Endpoint = "https://storage.uat.example.it" }
        });
        Assert.That(servizio.CanRead(Abfss), Is.EqualTo(atteso));
    }


    // --- richiesta ---

    private static AppIoFinancialReportsFileRequest Richiesta(int? year, params string?[]? quarters) => new() { Year = year, Quarters = quarters! };

    private static readonly object[] RichiesteNonValide =
    [
        new object?[] { null, new string?[] { "2026_1" } },
        new object?[] { 0, new string?[] { "2026_1" } },
        new object?[] { -2026, Array.Empty<string?>() },
        new object?[] { 26, Array.Empty<string?>() },
        new object?[] { 20260, Array.Empty<string?>() },
        new object?[] { 2026, new string?[] { "2026" } },
        new object?[] { 2026, new string?[] { "2026_0" } },
        new object?[] { 2026, new string?[] { "2026_5" } },
        new object?[] { 2026, new string?[] { "2026-1" } },
        new object?[] { 2026, new string?[] { "2026_1", "" } },
        new object?[] { 2026, new string?[] { "2026_1", null } },
        new object?[] { 2026, new string?[] { "2026_1' OR 1=1 --" } },
        new object?[] { 2026, new string?[] { "2025_4" } },
        new object?[] { 2026, new string?[] { "2026_1", "2025_4" } },
    ];

    /// <summary>
    /// Trimestri dell'anno nel formato 'AAAA_T' con T da 1 a 4: spazi tolti, ripetizioni eliminate,
    /// ordine crescente.
    /// </summary>
    [TestCase(new[] { "2026_1" }, new[] { "2026_1" })]
    [TestCase(new[] { "2026_2", "2026_1" }, new[] { "2026_1", "2026_2" })]
    [TestCase(new[] { " 2026_1 ", "2026_1", "2026_4" }, new[] { "2026_1", "2026_4" })]
    public void Trimestri_Validi_ShouldEssereNormalizzati(string[] quarters, string[] attesi)
        => Assert.That(Richiesta(2026, quarters).Trimestri(), Is.EqualTo(attesi));

    /// <summary>
    /// Senza trimestri (lista assente o vuota) si chiedono tutti i trimestri dell'anno: la rotta poi
    /// scarica quelli che hanno un file.
    /// </summary>
    [Test]
    public void Trimestri_Assenti_ShouldEssereTuttiQuelliDellAnno()
    {
        var attesi = new[] { "2026_1", "2026_2", "2026_3", "2026_4" };

        Assert.Multiple(() =>
        {
            Assert.That(Richiesta(2026, null).Trimestri(), Is.EqualTo(attesi));
            Assert.That(Richiesta(2026).Trimestri(), Is.EqualTo(attesi));
        });
    }

    /// <summary>
    /// Anno assente o non di quattro cifre, trimestre non nel formato 'AAAA_T', fuori da 1-4 o di un
    /// altro anno: richiesta non valida.
    /// </summary>
    [TestCaseSource(nameof(RichiesteNonValide))]
    public void Trimestri_NonValidi_ShouldEssereNull(int? year, string?[] quarters)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Richiesta(year, quarters).Trimestri(), Is.Null);
            Assert.That(Richiesta(year, quarters).IsValida(), Is.False);
        });
    }

    /// <summary>
    /// Richiesta assente o senza anno: non valida.
    /// </summary>
    [Test]
    public void IsValida_RichiestaAssenteOSenzaAnno_ShouldEssereFalse()
    {
        Assert.Multiple(() =>
        {
            Assert.That(((AppIoFinancialReportsFileRequest?)null).IsValida(), Is.False);
            Assert.That(new AppIoFinancialReportsFileRequest().IsValida(), Is.False);
        });
    }

    /// <summary>
    /// La richiesta diventa la query con i trimestri normalizzati.
    /// </summary>
    [Test]
    public void Map_ShouldPortareITrimestri()
    {
        var query = Richiesta(2026, "2026_2", " 2026_1").Map(new AuthenticationInfo { Id = "utente-test", Prodotto = "prod-appio", Ruolo = Ruolo.ADMIN });

        Assert.That(query.YearQuarters, Is.EqualTo(new[] { "2026_1", "2026_2" }));
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
        => Assert.That(AppIoFinancialReportsExtensions.FileName("2026_1", filePath), Is.EqualTo(atteso));

    /// <summary>
    /// Lo ZIP prende il nome dal primo e dall'ultimo trimestre chiesto.
    /// </summary>
    [Test]
    public void ZipName_ShouldIndicareIlPrimoEUltimoTrimestre()
        => Assert.That(AppIoFinancialReportsExtensions.ZipName(["2025_4", "2026_1", "2026_2"]), Is.EqualTo("financial-report-appio-2025_4-2026_2.zip"));

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
    /// I percorsi si leggono dalla vista, uno per trimestre (il dato è ripetuto uguale su tutte le righe
    /// del trimestre), filtrando i trimestri con un parametro.
    /// </summary>
    [Test]
    public void SelectFilePaths_ShouldLeggereUnaRigaPerTrimestreDallaVista()
    {
        var sql = AppIoFinancialReportSQLBuilder.SelectFilePaths();

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("FROM [be].[vwAppioFinalReport] f"));
            Assert.That(sql, Does.Contain("f.[trimestre] IN @YearQuarters"));
            Assert.That(sql, Does.Contain("PARTITION BY f.[trimestre]"));
            Assert.That(sql, Does.Contain("p.Riga = 1"));
            Assert.That(sql, Does.Not.Contain("appio."));
        });
    }
}
