using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Documenti emessi APP IO (modello dati dell'11/11/2026, PF-1016): query MediatR -> handler ->
/// persistence -> viste [be].[vwAppioFinalReport*] sul DB seedato (tests/Data/appio.sql). Verifica
/// a livello di handler ciò che la rotta HTTP nasconde: i tipi letti da Dapper (date, bigint,
/// decimal) e le conversioni TRY_CONVERT delle viste.
///
/// Seed: 2026_2 → OB1 (APPIO-FR-001 con due righe da 10, APPIO-FR-002 da 5) e OB2 (APPIO-FR-003 da 2);
/// 2026_1 → OB1 (APPIO-FR-004 da 3); 2025_4 → OB4 (APPIO-FR-005 da 1, Data con spazi e
/// Riferimento_data '31/12/2025'); OB9 orfano, escluso dalla INNER JOIN.
/// </summary>
[Category("AppIO")]
public class AppIoFinancialReportQueryIntegrationTests
{
    private IMediator _handler = null!;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinalReport");
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinalReportDetails");
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    private static AuthenticationInfo Auth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-appio",
        Ruolo = Ruolo.ADMIN
    };

    /// <summary>
    /// I trimestri sono quelli con documenti, dal più recente; con l'anno solo quelli dell'anno.
    /// </summary>
    [Test]
    public async Task Quarters_ShouldEssereQuelliConDocumenti()
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
    /// Anni e codici fiscali vengono dalle viste filtro. Dal 12/11/2026 la vista dei codici fiscali
    /// elenca i soli enti con documenti: 00000000003 (OB3) è in anagrafica ma non ha documenti,
    /// quindi il filtro non lo propone.
    /// </summary>
    [Test]
    public async Task AnniECodiciFiscali_ShouldEssereQuelliDeiDocumenti()
    {
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinalReportFiltroAnno");
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinalReportFiltroCodFiscaleEnte");

        var anni = await _handler.Send(new AppIoFinancialReportYearsQuery(Auth()));
        var codici = await _handler.Send(new AppIoFinancialReportCodiciFiscaliQuery(Auth()));

        Assert.Multiple(() =>
        {
            Assert.That(anni, Is.EqualTo(new[] { 2026, 2025 }));
            Assert.That(codici, Is.EqualTo(new[] { "00000000001", "00000000002", "00000000004" }));
        });
    }

    /// <summary>
    /// Senza filtri la griglia mostra il trimestre più recente, una riga padre per contratto, con
    /// ImportoTotale della vista (totale del trimestre: 25 per OB1, che ha due documenti).
    /// </summary>
    [Test]
    public async Task Griglia_SenzaFiltri_ShouldRaggruppareIlTrimestrePiuRecentePerContratto()
    {
        var lista = await _handler.Send(new AppIoFinancialReportQueryGetByRicerca(Auth()));
        var padri = lista.FinancialReports!.ToList();

        Assert.Multiple(() =>
        {
            Assert.That(lista.Count, Is.EqualTo(2));
            Assert.That(padri.Select(x => x.Key), Is.EqualTo(new[] { "APPIO-OB1|2026_2", "APPIO-OB2|2026_2" }));
            Assert.That(padri[0].ImportoTotale, Is.EqualTo(25m));
            Assert.That(padri[0].Posizioni.Select(x => x.Numero), Is.EqualTo(new[] { "APPIO-FR-001", "APPIO-FR-001", "APPIO-FR-002" }));
            Assert.That(padri[1].ImportoTotale, Is.EqualTo(2m));
        });
    }

    /// <summary>
    /// La griglia legge correttamente tutti i tipi: int per anno e trimestre, date per Data
    /// (convertita dalla vista), bigint per la quantità, decimal per gli importi.
    /// </summary>
    [Test]
    public async Task Griglia_ShouldMappareTuttiITipi()
    {
        var padre = (await _handler.Send(new AppIoFinancialReportQueryGetByRicerca(Auth()) { YearQuarter = ["2026_2"], InstitutionIds = ["APPIO-ENTE-1"] }))
            .FinancialReports!.Single();
        var prima = padre.Posizioni[0];

        Assert.Multiple(() =>
        {
            Assert.That(padre.ContractId, Is.EqualTo("APPIO-OB1"));
            Assert.That(padre.InstitutionId, Is.EqualTo("APPIO-ENTE-1"));
            Assert.That(padre.Name, Is.EqualTo("Comune Alfa AppIO Test"));
            Assert.That(padre.Prodotto, Is.EqualTo("prod-io"));
            Assert.That(padre.CodiceFiscale, Is.EqualTo("00000000001"));
            Assert.That(padre.Year, Is.EqualTo(2026));
            Assert.That(padre.Quarter, Is.EqualTo(2));
            Assert.That(prima.Data, Is.EqualTo(new DateTime(2026, 7, 15)));
            Assert.That(prima.CodiceArticolo, Is.EqualTo("ART-A"));
            Assert.That(prima.Quantita, Is.EqualTo(1000L));
            Assert.That(prima.Importo, Is.EqualTo(10m));
            Assert.That(prima.CodIva, Is.EqualTo("IVA22"));
            Assert.That(prima.Condizioni, Is.EqualTo("TP02"));
            Assert.That(prima.Causale, Is.EqualTo("Servizio APP IO Q2"));
        });
    }

    /// <summary>
    /// Il codice fiscale filtra la griglia; il contratto orfano OB9 non compare mai.
    /// </summary>
    [Test]
    public async Task Griglia_CodiceFiscale_ShouldFiltrare_SenzaOrfani()
    {
        var lista = await _handler.Send(new AppIoFinancialReportQueryGetByRicerca(Auth()) { Year = "2026", CodiciFiscali = ["00000000002"] });

        Assert.That(lista.FinancialReports!.Select(x => x.Key), Is.EqualTo(new[] { "APPIO-OB2|2026_2" }));
    }

    /// <summary>
    /// Il percorso del file del financial report è uno per trimestre (ripetuto su tutte le righe):
    /// la query restituisce quello del trimestre chiesto, null dove non ci sono documenti.
    /// </summary>
    [TestCase(2026, 2, "abfss://appioexport@stappioseed.dfs.core.windows.net/export/sap/2026/2/appio-final_report-2026-q2.csv")]
    [TestCase(2026, 1, "abfss://appioexport@stappioseed.dfs.core.windows.net/export/sap/2026/1/appio-final_report-2026-q1.csv")]
    [TestCase(2025, 4, "abfss://appioexport@stappioseed.dfs.core.windows.net/export/sap/2025/4/appio-final_report-2025-q4.csv")]
    [TestCase(2026, 3, null)]
    [TestCase(2025, 2, null)]
    public async Task FilePath_ShouldEssereQuelloDelTrimestre(int year, int quarter, string? atteso)
    {
        var path = await _handler.Send(new AppIoFinancialReportFilePathQuery(Auth()) { Year = year, Quarter = quarter });

        Assert.That(path, Is.EqualTo(atteso));
    }

    /// <summary>
    /// La garanzia che il filtro "Codice Fiscale" deve dare: ogni codice proposto trova almeno una
    /// riga di documento in uno degli anni proposti. Con i codici presi dall'anagrafica non era vero
    /// (00000000003 non trovava nulla).
    /// </summary>
    [Test]
    public async Task CodiciFiscali_OgniCodiceProposto_ShouldTrovareAlmenoUnDocumento()
    {
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinalReportFiltroAnno");
        var anni = await _handler.Send(new AppIoFinancialReportYearsQuery(Auth()));
        var codici = (await _handler.Send(new AppIoFinancialReportCodiciFiscaliQuery(Auth()))).ToList();

        Assert.That(codici, Is.Not.Empty);
        foreach (var codice in codici)
        {
            var righe = 0;
            foreach (var anno in anni)
                righe += (await _handler.Send(new AppIoFinancialReportQueryGetByRicercaExcel(Auth()) { Year = anno.ToString(), CodiciFiscali = [codice] }))?.Count() ?? 0;
            Assert.That(righe, Is.GreaterThan(0), $"il codice {codice} è proposto dal filtro ma non ha documenti");
        }
    }

    /// <summary>
    /// Il filtro segue i documenti, non l'anagrafica: appena OB3 (00000000003) riceve un documento il
    /// suo codice compare, mentre un documento di OB5, che non ha codice fiscale, non aggiunge un
    /// valore nullo. Le righe inserite vengono rimosse a fine test.
    /// </summary>
    [Test]
    public async Task CodiciFiscali_NuoviDocumenti_ShouldSeguireIDocumentiESaltareINull()
    {
        const string numero = "APPIO-FR-TMP-CF";
        await using var conn = new Microsoft.Data.SqlClient.SqlConnection(LocalTestDb.ConnectionString);
        await conn.OpenAsync();

        async Task Esegui(string sql)
        {
            await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        try
        {
            var prima = await _handler.Send(new AppIoFinancialReportCodiciFiscaliQuery(Auth()));
            Assert.That(prima, Does.Not.Contain("00000000003"), "contro-prova: prima dell'inserimento OB3 non ha documenti");

            await Esegui($"""
                INSERT INTO appio.messages_final_report ([year], [quarter], contract_id, Numero, Importo, file_path) VALUES
                 (2026, 2, 'APPIO-OB3', '{numero}', 1.00, 'appio/tmp.csv'),
                 (2026, 2, 'APPIO-OB3', '{numero}', 1.00, 'appio/tmp.csv'),
                 (2026, 2, 'APPIO-OB5', '{numero}', 1.00, 'appio/tmp.csv')
                """);

            var dopo = await _handler.Send(new AppIoFinancialReportCodiciFiscaliQuery(Auth()));

            Assert.That(dopo, Is.EqualTo(new[] { "00000000001", "00000000002", "00000000003", "00000000004" }),
                "OB3 compare una volta sola anche con due righe; OB5 (codice fiscale NULL) non aggiunge nulla");
        }
        finally
        {
            await Esegui($"DELETE FROM appio.messages_final_report WHERE Numero = '{numero}'");
        }
    }

    /// <summary>
    /// Il dettaglio legge anagrafica e date convertite dalla vista: Data con spazi viene letta
    /// (TRIM), Riferimento_data in formato errato diventa null senza far fallire la query.
    /// </summary>
    [Test]
    public async Task Dettaglio_ShouldLeggereAnagraficaEDateConvertite()
    {
        var righe = (await _handler.Send(new AppIoFinancialReportQueryGetDettaglio(Auth()) { ContractId = "APPIO-OB4", YearQuarter = "2025_4" })).ToList();
        var riga = righe.Single();

        Assert.Multiple(() =>
        {
            Assert.That(riga.Name, Is.EqualTo("Comune Alfa AppIO Test"));
            Assert.That(riga.PartitaIva, Is.EqualTo("00000000004"));
            Assert.That(riga.CodiceFiscale, Is.EqualTo("00000000004"));
            Assert.That(riga.DataCreazione, Is.EqualTo(new DateTime(2025, 4, 10)));
            Assert.That(riga.UltimoAggiornamento, Is.EqualTo(new DateTime(2026, 3, 4)));
            Assert.That(riga.Year, Is.EqualTo(2025));
            Assert.That(riga.Quarter, Is.EqualTo(4));
            Assert.That(riga.TipoDoc, Is.EqualTo("TD01"));
            Assert.That(riga.Valuta, Is.EqualTo("EUR"));
            Assert.That(riga.CodiceAggiuntivo, Is.EqualTo("AGG4"));
            Assert.That(riga.DataReport, Is.EqualTo(new DateTime(2026, 1, 15)));
            Assert.That(riga.DataRiferimento, Is.Null);
            Assert.That(riga.Quantita, Is.EqualTo(100L));
        });
    }

    /// <summary>
    /// Il dettaglio di OB1 nel 2026_2 contiene le righe dei due documenti del trimestre, ordinate.
    /// </summary>
    [Test]
    public async Task Dettaglio_ShouldContenereTutteLeRigheDelTrimestre()
    {
        var righe = await _handler.Send(new AppIoFinancialReportQueryGetDettaglio(Auth()) { ContractId = "APPIO-OB1", YearQuarter = "2026_2" });

        Assert.That(righe.Select(x => $"{x.Numero}/{x.CodiceArticolo}"),
            Is.EqualTo(new[] { "APPIO-FR-001/ART-A", "APPIO-FR-001/ART-B", "APPIO-FR-002/ART-A" }));
    }
}
