using ClosedXML.Excel;
using MediatR;
using Moq;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Unit test (IMediator mockato con Moq, NESSUN DB) della logica condivisa da piu' endpoint in
/// FattureExtensions: ReportFatture (/api/fatture/report) e ReportFattureSospese (/api/fatture/sospese/report).
/// Coprono: auto-popolamento tipologia + guard NRE (TipologiaFattura null/vuota), rami dello switch,
/// guardie IsNotEmpty/dettaglio, tipologia sconosciuta, e l'invariante colonna "Rel Non Firmata"
/// (solo fogli sospesi) sui byte[] Excel generati.
/// </summary>
public class FattureReportExtensionsTests
{
    private const string Caption = "Rel Non Firmata";

    private static AuthenticationInfo AdminAuth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    private static FattureRelExcelDto NonSospesaRow() => new()
    {
        IdEnte = "ENTE-1", RagioneSociale = "Ente", IdContratto = "C1", TipologiaFattura = "X",
        IdFattura = "1", Progressivo = "1", TipoDocumento = "TD01", Anno = 2026, Mese = 2,
        TotaleFatturaImponibile = 100m, TipologiaContratto = "AC"
    };

    private static FattureRelSospeseExcelDto SospesaRow(string rel = "SI") => new()
    {
        IdEnte = "ENTE-1", RagioneSociale = "Ente", IdContratto = "C1", TipologiaFattura = "X",
        IdFattura = "1", Progressivo = "1", TipoDocumento = "TD01", Anno = 2026, Mese = 2,
        TotaleFatturaImponibile = 100m, TipologiaContratto = "AC", RelNonFirmata = rel
    };

    private static List<IEnumerable<FattureRelExcelDto>> NonSospeseData() =>
        new() { new List<FattureRelExcelDto> { NonSospesaRow() }, new List<FattureRelExcelDto> { NonSospesaRow() } };

    private static List<IEnumerable<FattureRelSospeseExcelDto>> SospeseData(string rel = "SI") =>
        new() { new List<FattureRelSospeseExcelDto> { SospesaRow(rel) }, new List<FattureRelSospeseExcelDto> { SospesaRow(rel) } };

    private static List<(string Sheet, bool HasColumn)> InspectSheets(Dictionary<string, byte[]> reports)
    {
        var result = new List<(string, bool)>();
        foreach (var bytes in reports.Values)
        {
            using var wb = new XLWorkbook(new MemoryStream(bytes));
            foreach (var ws in wb.Worksheets)
            {
                var has = ws.Row(1).CellsUsed()
                    .Any(c => string.Equals(c.GetString(), Caption, StringComparison.OrdinalIgnoreCase));
                result.Add((ws.Name, has));
            }
        }
        return result;
    }

    // =================== ReportFattureSospese ===================

    /// <summary>NRE guard: TipologiaFattura null + auto-popolamento vuoto -> dizionario vuoto, nessuna eccezione.</summary>
    [Test]
    public async Task ReportFattureSospese_NullTipologia_EmptyAutopopulate_ReturnsEmpty()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureSospeseTipologiaAnniMeseQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string>?)Array.Empty<string>());

        var request = new FatturaSospeseRicercaRequest { Anno = 2026, Mese = 2 }; // TipologiaFattura null

        var reports = await request.ReportFattureSospese(mediator.Object, AdminAuth());

        Assert.That(reports, Is.Not.Null);
        Assert.That(reports, Is.Empty);
    }

    /// <summary>Auto-popolamento con risultati -> il report viene prodotto (ramo SECONDO SALDO).</summary>
    [Test]
    public async Task ReportFattureSospese_NullTipologia_Autopopulate_ProducesReport_WithColumn()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureSospeseTipologiaAnniMeseQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string>?)new[] { "SECONDO SALDO" });
        mediator.Setup(m => m.Send(It.IsAny<FattureSospeseRelExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureRelSospeseExcelDto>>?)SospeseData());

        var request = new FatturaSospeseRicercaRequest { Anno = 2026, Mese = 2 };

        var reports = await request.ReportFattureSospese(mediator.Object, AdminAuth());

        Assert.That(reports.Count, Is.EqualTo(1));
        var sospese = InspectSheets(reports)
            .Where(s => s.Sheet.StartsWith("Reg. Esec. Sospese", StringComparison.OrdinalIgnoreCase)
                     || s.Sheet.StartsWith("Enti Fatt. Sospese", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.That(sospese, Is.Not.Empty);
        Assert.That(sospese.All(s => s.HasColumn), Is.True);
    }

    /// <summary>SECONDO SALDO con dati -> report con colonna nei fogli sospesi.</summary>
    [Test]
    public async Task ReportFattureSospese_SecondoSaldo_WithData_ProducesReport_WithColumn()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureSospeseRelExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureRelSospeseExcelDto>>?)SospeseData());

        var request = new FatturaSospeseRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = new[] { "SECONDO SALDO" } };

        var reports = await request.ReportFattureSospese(mediator.Object, AdminAuth());

        Assert.That(reports.Count, Is.EqualTo(1));
        Assert.That(InspectSheets(reports).Where(s => s.Sheet.Contains("Sospese")).All(s => s.HasColumn), Is.True);
    }

    /// <summary>SECONDO SALDO senza dati rel (Send non mockato -> null) -> nessun report.</summary>
    [Test]
    public async Task ReportFattureSospese_SecondoSaldo_EmptyRel_NoReport()
    {
        var mediator = new Mock<IMediator>();
        var request = new FatturaSospeseRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = new[] { "SECONDO SALDO" } };

        var reports = await request.ReportFattureSospese(mediator.Object, AdminAuth());

        Assert.That(reports, Is.Empty);
    }

    /// <summary>PRIMO SALDO: rel presente ma dettaglio vuoto -> la guardia impedisce l'aggiunta del report.</summary>
    [Test]
    public async Task ReportFattureSospese_PrimoSaldo_EmptyDettaglio_NoReport()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureSospeseRelExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureRelSospeseExcelDto>>?)SospeseData());
        mediator.Setup(m => m.Send(It.IsAny<FattureSospeseQueryRicerca>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FattureListaDto?)new FattureListaDto()); // dettaglio vuoto

        var request = new FatturaSospeseRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = new[] { "PRIMO SALDO" } };

        var reports = await request.ReportFattureSospese(mediator.Object, AdminAuth());

        Assert.That(reports, Is.Empty);
    }

    /// <summary>Tipologia sconosciuta -> ramo default -> nessun report.</summary>
    [Test]
    public async Task ReportFattureSospese_UnknownTipologia_NoReport()
    {
        var mediator = new Mock<IMediator>();
        var request = new FatturaSospeseRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = new[] { "PAGOPA" } };

        var reports = await request.ReportFattureSospese(mediator.Object, AdminAuth());

        Assert.That(reports, Is.Empty);
    }

    // =================== ReportFatture (emesse) ===================

    /// <summary>NRE guard su ReportFatture: TipologiaFattura null + auto-popolamento vuoto -> vuoto, no throw.</summary>
    [Test]
    public async Task ReportFatture_NullTipologia_EmptyAutopopulate_ReturnsEmpty()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureTipologiaAnniMeseQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string>?)Array.Empty<string>());

        var request = new FatturaRicercaRequest { Anno = 2026, Mese = 2 };

        var reports = await request.ReportFatture(mediator.Object, AdminAuth());

        Assert.That(reports, Is.Not.Null);
        Assert.That(reports, Is.Empty);
    }

    /// <summary>
    /// SECONDO SALDO con dati: fogli non-sospesi SENZA colonna, sotto-foglio "Enti Fatt. {m} Sospesi" CON colonna.
    /// </summary>
    [Test]
    public async Task ReportFatture_SecondoSaldo_WithData_ColumnOnlyInSospesiSheet()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureRelExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureRelExcelDto>>?)NonSospeseData());
        mediator.Setup(m => m.Send(It.IsAny<FattureSospeseRelExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureRelSospeseExcelDto>>?)SospeseData());
        mediator.Setup(m => m.Send(It.IsAny<RelNonFatturateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<RelNonFatturataDto>?)new List<RelNonFatturataDto>());

        var request = new FatturaRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = new[] { "SECONDO SALDO" } };

        var reports = await request.ReportFatture(mediator.Object, AdminAuth());

        Assert.That(reports.Count, Is.EqualTo(1));

        var sheets = InspectSheets(reports);
        var nonSospese = sheets.Where(s =>
            s.Sheet.StartsWith("Regolari Esecuzioni", StringComparison.OrdinalIgnoreCase)
            || (s.Sheet.StartsWith("Enti Fatt.", StringComparison.OrdinalIgnoreCase)
                && !s.Sheet.EndsWith("Sospesi", StringComparison.OrdinalIgnoreCase))).ToList();
        var sospesi = sheets.Where(s => s.Sheet.EndsWith("Sospesi", StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(nonSospese, Is.Not.Empty);
            Assert.That(nonSospese.All(s => !s.HasColumn), Is.True,
                "I fogli non-sospesi NON devono contenere 'Rel Non Firmata'.");
            Assert.That(sospesi, Is.Not.Empty);
            Assert.That(sospesi.All(s => s.HasColumn), Is.True,
                "Il sotto-foglio 'Sospesi' deve contenere 'Rel Non Firmata'.");
        });
    }

    /// <summary>SECONDO SALDO senza dati rel -> nessun report.</summary>
    [Test]
    public async Task ReportFatture_SecondoSaldo_EmptyRel_NoReport()
    {
        var mediator = new Mock<IMediator>();
        var request = new FatturaRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = new[] { "SECONDO SALDO" } };

        var reports = await request.ReportFatture(mediator.Object, AdminAuth());

        Assert.That(reports, Is.Empty);
    }

    /// <summary>Tipologia sconosciuta -> nessun report.</summary>
    [Test]
    public async Task ReportFatture_UnknownTipologia_NoReport()
    {
        var mediator = new Mock<IMediator>();
        var request = new FatturaRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = new[] { "PAGOPA" } };

        var reports = await request.ReportFatture(mediator.Object, AdminAuth());

        Assert.That(reports, Is.Empty);
    }

    // =================== Rami ANTICIPO / ACCONTO ===================

    private static List<IEnumerable<FattureCommessaExcelDto>> CommessaData() =>
        new() { new List<FattureCommessaExcelDto> { new() { MeseValidita = 2, TotaleFattura = 100m } } };

    private static List<IEnumerable<FattureAccontoExcelDto>> AccontoData() =>
        new() { new List<FattureAccontoExcelDto> { new() { Mese = 2 } } };

    [Test]
    public async Task ReportFatture_Anticipo_WithData_ProducesReport()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureCommessaExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureCommessaExcelDto>>?)CommessaData());

        var request = new FatturaRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = new[] { "ANTICIPO" } };
        var reports = await request.ReportFatture(mediator.Object, AdminAuth());

        Assert.That(reports.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task ReportFatture_Acconto_WithData_ProducesReport()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureAccontoExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureAccontoExcelDto>>?)AccontoData());

        var request = new FatturaRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = new[] { "ACCONTO" } };
        var reports = await request.ReportFatture(mediator.Object, AdminAuth());

        Assert.That(reports.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task ReportFattureSospese_Anticipo_WithData_ProducesReport()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureCommessaExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureCommessaExcelDto>>?)CommessaData());

        var request = new FatturaSospeseRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = new[] { "ANTICIPO" } };
        var reports = await request.ReportFattureSospese(mediator.Object, AdminAuth());

        Assert.That(reports.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task ReportFattureSospese_Acconto_WithData_ProducesReport()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureAccontoExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureAccontoExcelDto>>?)AccontoData());

        var request = new FatturaSospeseRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = new[] { "ACCONTO" } };
        var reports = await request.ReportFattureSospese(mediator.Object, AdminAuth());

        Assert.That(reports.Count, Is.EqualTo(1));
    }

    // =================== Più tipologie nello stesso request ===================

    [Test]
    public async Task ReportFattureSospese_MultipleTipologie_ProducesMultipleReports()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureSospeseRelExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureRelSospeseExcelDto>>?)SospeseData());

        var request = new FatturaSospeseRicercaRequest
        {
            Anno = 2026,
            Mese = 2,
            TipologiaFattura = new[] { "SECONDO SALDO", "VAR. SEMESTRALE" }
        };
        var reports = await request.ReportFattureSospese(mediator.Object, AdminAuth());

        Assert.That(reports.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task ReportFatture_MultipleTipologie_ProducesMultipleReports()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureRelExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureRelExcelDto>>?)NonSospeseData());
        mediator.Setup(m => m.Send(It.IsAny<FattureSospeseRelExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureRelSospeseExcelDto>>?)SospeseData());
        mediator.Setup(m => m.Send(It.IsAny<RelNonFatturateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<RelNonFatturataDto>?)new List<RelNonFatturataDto>());

        var request = new FatturaRicercaRequest
        {
            Anno = 2026,
            Mese = 2,
            TipologiaFattura = new[] { "SECONDO SALDO", "VAR. SEMESTRALE" }
        };
        var reports = await request.ReportFatture(mediator.Object, AdminAuth());

        Assert.That(reports.Count, Is.EqualTo(2));
    }

    // =================== PRIMO SALDO happy path (dettaglio valorizzato) ===================

    /// <summary>
    /// PRIMO SALDO con rel + dettaglio valorizzato: report prodotto con il foglio "Dett Fatt Sosp"
    /// (da FattureExcel, quindi SENZA colonna) e i fogli rel sospesi CON la colonna.
    /// </summary>
    [Test]
    public async Task ReportFattureSospese_PrimoSaldo_WithDettaglio_ProducesReport_AndDettSheet()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureSospeseRelExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureRelSospeseExcelDto>>?)SospeseData());
        var dettaglio = new FattureListaDto
        {
            new FatturaDto { fattura = new TitoloFatturaDto { Numero = 1, Posizioni = new List<PosizioniDto>() } }
        };
        mediator.Setup(m => m.Send(It.IsAny<FattureSospeseQueryRicerca>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FattureListaDto?)dettaglio);

        var request = new FatturaSospeseRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = new[] { "PRIMO SALDO" } };
        var reports = await request.ReportFattureSospese(mediator.Object, AdminAuth());

        Assert.That(reports.Count, Is.EqualTo(1));

        var sheets = InspectSheets(reports);
        Assert.Multiple(() =>
        {
            Assert.That(sheets.Any(s => s.Sheet.StartsWith("Dett Fatt Sosp", StringComparison.OrdinalIgnoreCase)), Is.True,
                "Il foglio 'Dett Fatt Sosp' deve essere presente.");
            Assert.That(sheets.Where(s => s.Sheet.StartsWith("Dett Fatt Sosp", StringComparison.OrdinalIgnoreCase)).All(s => !s.HasColumn), Is.True,
                "Il foglio dettaglio (FattureExcel) NON deve contenere 'Rel Non Firmata'.");
            Assert.That(sheets.Where(s => s.Sheet.Contains("Sospese")).All(s => s.HasColumn), Is.True,
                "I fogli rel sospesi devono contenere 'Rel Non Firmata'.");
        });
    }

    // =================== Presenza/assenza del filtro TipologiaFattura (front end) ===================
    // Verifica esplicita del ramo auto-popolamento: invocato SOLO quando il filtro è assente
    // (null oppure array vuoto -> coerció a null dal setter di FatturaRicercaRequest).

    [Test]
    public async Task ReportFattureSospese_FiltroPresente_NonInvocaAutoPopolamento()
    {
        var mediator = new Mock<IMediator>();
        var request = new FatturaSospeseRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = new[] { "SECONDO SALDO" } };

        await request.ReportFattureSospese(mediator.Object, AdminAuth());

        mediator.Verify(m => m.Send(It.IsAny<FattureSospeseTipologiaAnniMeseQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ReportFattureSospese_FiltroNull_InvocaAutoPopolamento()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureSospeseTipologiaAnniMeseQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string>?)Array.Empty<string>());

        var request = new FatturaSospeseRicercaRequest { Anno = 2026, Mese = 2 }; // TipologiaFattura non passata (null)

        await request.ReportFattureSospese(mediator.Object, AdminAuth());

        mediator.Verify(m => m.Send(It.IsAny<FattureSospeseTipologiaAnniMeseQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ReportFattureSospese_FiltroVuoto_InvocaAutoPopolamento()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureSospeseTipologiaAnniMeseQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string>?)Array.Empty<string>());

        // array vuoto passato dal front end -> il setter lo coerce a null -> auto-popolamento
        var request = new FatturaSospeseRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = Array.Empty<string>() };

        await request.ReportFattureSospese(mediator.Object, AdminAuth());

        mediator.Verify(m => m.Send(It.IsAny<FattureSospeseTipologiaAnniMeseQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ReportFatture_FiltroPresente_NonInvocaAutoPopolamento()
    {
        var mediator = new Mock<IMediator>();
        var request = new FatturaRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = new[] { "SECONDO SALDO" } };

        await request.ReportFatture(mediator.Object, AdminAuth());

        mediator.Verify(m => m.Send(It.IsAny<FattureTipologiaAnniMeseQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ReportFatture_FiltroNull_InvocaAutoPopolamento()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<FattureTipologiaAnniMeseQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string>?)Array.Empty<string>());

        var request = new FatturaRicercaRequest { Anno = 2026, Mese = 2 };

        await request.ReportFatture(mediator.Object, AdminAuth());

        mediator.Verify(m => m.Send(It.IsAny<FattureTipologiaAnniMeseQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
