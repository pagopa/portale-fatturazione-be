using ClosedXML.Excel;
using MediatR;
using Moq;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
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

    // =================== ReportNonInviate (/api/fatture/pagopa/non-inviate/report) ===================
    // Copre lo sheet "Non Fatturate" (posticipate + eliminate, per tipologia) — la modifica — e il
    // pregresso del wiring (rami SALDO/ANTICIPO, filtro per tipologia, casi vuoti, tipologie multiple).

    private static IEnumerable<AnniMesiTipologiaDto> Amt(params string[] tipologie) =>
        tipologie.Select(t => new AnniMesiTipologiaDto { Anno = 2026, Mese = 2, TipologiaFattura = t }).ToList();

    private static GestioneFattureReportDto GfRow(string tipologia, string stato, string ragione) => new()
    {
        IdEnte = "11111111-1111-1111-1111-111111111111",
        RagioneSociale = ragione,
        IdContratto = "c1",
        TipoContratto = "PAC",
        TipologiaFattura = tipologia,
        Anno = 2026,
        Mese = 2,
        Stato = stato
    };

    // Mock per il ramo SALDO: tipologie da NonFatturateTipologiaQueryRicerca, dati rel non vuoti (cosi'
    // il report della tipologia viene prodotto) e la lista gestioneFatture (posticipate/eliminate).
    private static Mock<IMediator> MediatorForSaldo(string[] tipologie, IEnumerable<GestioneFattureReportDto>? gestione)
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<NonFatturateTipologiaQueryRicerca>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<AnniMesiTipologiaDto>?)Amt(tipologie));
        m.Setup(x => x.Send(It.IsAny<FattureRelExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureRelExcelDto>>?)NonSospeseData());
        m.Setup(x => x.Send(It.IsAny<RelNonFatturateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<RelNonFatturataDto>?)new List<RelNonFatturataDto>());
        m.Setup(x => x.Send(It.IsAny<GestioneFattureReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<GestioneFattureReportDto>?)gestione?.ToList());
        return m;
    }

    private static List<string> SheetNames(byte[] xlsx)
    {
        using var wb = new XLWorkbook(new MemoryStream(xlsx));
        return wb.Worksheets.Select(w => w.Name).ToList();
    }

    private static bool SheetContains(byte[] xlsx, string sheetName, string text)
    {
        using var wb = new XLWorkbook(new MemoryStream(xlsx));
        var ws = wb.Worksheets.FirstOrDefault(w => string.Equals(w.Name, sheetName, StringComparison.OrdinalIgnoreCase));
        return ws != null && ws.CellsUsed().Any(c => c.GetString().Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Modifica: il report SALDO deve includere lo sheet "Non Fatturate" quando ci sono posticipate/eliminate.</summary>
    [Test]
    public async Task ReportNonInviate_SaldoConGestioneFatture_IncludeSheetNonFatturate()
    {
        var gestione = new[]
        {
            GfRow("SECONDO SALDO", "POSTICIPATA", "Ente-POST"),
            GfRow("SECONDO SALDO", "ELIMINATA",   "Ente-ELIM"),
        };
        var mediator = MediatorForSaldo(new[] { "SECONDO SALDO" }, gestione);
        var request = new NonFatturateRicercaRequest { TipologiaFattura = new[] { "SECONDO SALDO" } };

        var reports = await request.ReportNonInviate(mediator.Object, AdminAuth());

        Assert.That(reports.ContainsKey("Lista SECONDO SALDO"), Is.True, "Il report della tipologia deve essere prodotto.");
        Assert.That(SheetNames(reports["Lista SECONDO SALDO"]), Does.Contain("Non Fatturate"),
            "Il report SALDO deve includere lo sheet 'Non Fatturate'.");
    }

    /// <summary>Modifica: lo sheet "Non Fatturate" contiene SIA posticipate SIA eliminate.</summary>
    [Test]
    public async Task ReportNonInviate_SheetNonFatturate_ContieneSiaPosticipateCheEliminate()
    {
        var gestione = new[]
        {
            GfRow("SECONDO SALDO", "POSTICIPATA", "Ente-POST"),
            GfRow("SECONDO SALDO", "ELIMINATA",   "Ente-ELIM"),
        };
        var mediator = MediatorForSaldo(new[] { "SECONDO SALDO" }, gestione);
        var request = new NonFatturateRicercaRequest { TipologiaFattura = new[] { "SECONDO SALDO" } };

        var reports = await request.ReportNonInviate(mediator.Object, AdminAuth());
        var xlsx = reports["Lista SECONDO SALDO"];

        Assert.Multiple(() =>
        {
            Assert.That(SheetContains(xlsx, "Non Fatturate", "Ente-POST"), Is.True, "Deve contenere la posticipata.");
            Assert.That(SheetContains(xlsx, "Non Fatturate", "Ente-ELIM"), Is.True, "Deve contenere l'eliminata.");
            Assert.That(SheetContains(xlsx, "Non Fatturate", "POSTICIPATA"), Is.True, "Stato POSTICIPATA presente.");
            Assert.That(SheetContains(xlsx, "Non Fatturate", "ELIMINATA"), Is.True, "Stato ELIMINATA presente.");
        });
    }

    /// <summary>Pregresso: senza posticipate/eliminate il report SALDO c'e' ma NON lo sheet "Non Fatturate".</summary>
    [Test]
    public async Task ReportNonInviate_SenzaGestioneFatture_NessunSheetNonFatturate()
    {
        var mediator = MediatorForSaldo(new[] { "SECONDO SALDO" }, gestione: new List<GestioneFattureReportDto>());
        var request = new NonFatturateRicercaRequest { TipologiaFattura = new[] { "SECONDO SALDO" } };

        var reports = await request.ReportNonInviate(mediator.Object, AdminAuth());

        Assert.That(reports.ContainsKey("Lista SECONDO SALDO"), Is.True, "Il report SALDO deve comunque essere prodotto.");
        Assert.That(SheetNames(reports["Lista SECONDO SALDO"]), Does.Not.Contain("Non Fatturate"),
            "Senza posticipate/eliminate NON deve esserci lo sheet 'Non Fatturate'.");
    }

    /// <summary>Correttezza: le righe di un'ALTRA tipologia non finiscono nel report SECONDO SALDO.</summary>
    [Test]
    public async Task ReportNonInviate_SheetNonFatturate_FiltraPerTipologia()
    {
        var gestione = new[]
        {
            GfRow("SECONDO SALDO", "POSTICIPATA", "Ente-SS"),
            GfRow("PRIMO SALDO",   "ELIMINATA",   "Ente-PS-altrui"),
        };
        var mediator = MediatorForSaldo(new[] { "SECONDO SALDO" }, gestione);
        var request = new NonFatturateRicercaRequest { TipologiaFattura = new[] { "SECONDO SALDO" } };

        var reports = await request.ReportNonInviate(mediator.Object, AdminAuth());
        var xlsx = reports["Lista SECONDO SALDO"];

        Assert.Multiple(() =>
        {
            Assert.That(SheetContains(xlsx, "Non Fatturate", "Ente-SS"), Is.True);
            Assert.That(SheetContains(xlsx, "Non Fatturate", "Ente-PS-altrui"), Is.False,
                "Le righe di altra tipologia non devono comparire nel report SECONDO SALDO.");
        });
    }

    /// <summary>Pregresso: piu' tipologie SALDO -> un report per tipologia, ciascuno col proprio "Non Fatturate".</summary>
    [Test]
    public async Task ReportNonInviate_MultipleSaldo_UnReportPerTipologia_SenzaMescolare()
    {
        var gestione = new[]
        {
            GfRow("SECONDO SALDO", "POSTICIPATA", "Ente-SS"),
            GfRow("PRIMO SALDO",   "ELIMINATA",   "Ente-PS"),
        };
        var mediator = MediatorForSaldo(new[] { "SECONDO SALDO", "PRIMO SALDO" }, gestione);
        var request = new NonFatturateRicercaRequest { TipologiaFattura = new[] { "SECONDO SALDO", "PRIMO SALDO" } };

        var reports = await request.ReportNonInviate(mediator.Object, AdminAuth());

        Assert.Multiple(() =>
        {
            Assert.That(reports.ContainsKey("Lista SECONDO SALDO"), Is.True);
            Assert.That(reports.ContainsKey("Lista PRIMO SALDO"), Is.True);
            Assert.That(SheetContains(reports["Lista SECONDO SALDO"], "Non Fatturate", "Ente-SS"), Is.True);
            Assert.That(SheetContains(reports["Lista PRIMO SALDO"], "Non Fatturate", "Ente-PS"), Is.True);
            Assert.That(SheetContains(reports["Lista SECONDO SALDO"], "Non Fatturate", "Ente-PS"), Is.False,
                "I 'Non Fatturate' non devono mescolarsi tra tipologie.");
        });
    }

    /// <summary>Pregresso: ramo ANTICIPO produce il report ma senza "Non Fatturate" (non esistono sospese).</summary>
    [Test]
    public async Task ReportNonInviate_Anticipo_ProduceReport_SenzaNonFatturate()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<NonFatturateTipologiaQueryRicerca>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<AnniMesiTipologiaDto>?)Amt("ANTICIPO"));
        m.Setup(x => x.Send(It.IsAny<FattureCommessaExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureCommessaExcelDto>>?)CommessaData());
        m.Setup(x => x.Send(It.IsAny<GestioneFattureReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<GestioneFattureReportDto>?)new List<GestioneFattureReportDto>());

        var request = new NonFatturateRicercaRequest { TipologiaFattura = new[] { "ANTICIPO" } };
        var reports = await request.ReportNonInviate(m.Object, AdminAuth());

        Assert.That(reports.ContainsKey("Lista ANTICIPO"), Is.True);
        Assert.That(SheetNames(reports["Lista ANTICIPO"]), Does.Not.Contain("Non Fatturate"),
            "Per ANTICIPO non esistono sospese: nessuno sheet 'Non Fatturate' (comportamento pregresso).");
    }

    /// <summary>Pregresso: nessuna tipologia da elaborare -> nessun report.</summary>
    [Test]
    public async Task ReportNonInviate_NessunaTipologia_ReturnsEmpty()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<NonFatturateTipologiaQueryRicerca>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<AnniMesiTipologiaDto>?)new List<AnniMesiTipologiaDto>());
        m.Setup(x => x.Send(It.IsAny<GestioneFattureReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<GestioneFattureReportDto>?)new List<GestioneFattureReportDto>());

        var request = new NonFatturateRicercaRequest();
        var reports = await request.ReportNonInviate(m.Object, AdminAuth());

        Assert.That(reports, Is.Empty);
    }

    // =================== Regressione DIRETTA su ReportFattureRel: sheet "Non Fatturate" fuori dal loop ===
    // Bug originale: lo sheet era nel ramo else (indice >= 2), quindi compariva SOLO con >= 3 gruppi in
    // 'fatture' e spariva silenziosamente con 1-2 gruppi. Ora e' fuori dal loop: dipende solo da
    // gestioneFatture, non dal numero di gruppi.

    private static GestioneFattureReportExcelDto GfExcel(string ragione, string stato) => new()
    {
        IdEnte = "11111111-1111-1111-1111-111111111111",
        RagioneSociale = ragione,
        TipoContratto = "PAC",
        TipologiaFattura = "SECONDO SALDO",
        Anno = 2026,
        Mese = 2,
        Stato = stato
    };

    private static List<IEnumerable<FattureRelExcelDto>> RelGroups(int n)
    {
        var groups = new List<IEnumerable<FattureRelExcelDto>>();
        for (int i = 0; i < n; i++)
            groups.Add(new List<FattureRelExcelDto> { NonSospesaRow() });
        return groups;
    }

    private static byte[] CallReportFattureRel(int groups, IEnumerable<GestioneFattureReportExcelDto>? gestione) =>
        RelGroups(groups).ReportFattureRel(
            fattureSospese: null,
            relNonFirmate: new List<RelNonFatturataDto>(),
            gestioneFatture: gestione,
            month: "",
            tipologia: "SECONDO SALDO");

    /// <summary>
    /// Con gestioneFatture non vuota lo sheet "Non Fatturate" deve esserci con 1, 2 o 3+ gruppi. Il caso
    /// a 1 gruppo e' esattamente cio' che il bug rompeva (prima serviva >= 3 gruppi).
    /// </summary>
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void ReportFattureRel_ConGestioneFatture_IncludeNonFatturate_IndipendenteDalNumeroDiGruppi(int groups)
    {
        var gestione = new[] { GfExcel("Ente-POST", "POSTICIPATA"), GfExcel("Ente-ELIM", "ELIMINATA") };

        var xlsx = CallReportFattureRel(groups, gestione);

        Assert.Multiple(() =>
        {
            Assert.That(SheetNames(xlsx), Does.Contain("Non Fatturate"),
                $"Con {groups} gruppo/i lo sheet 'Non Fatturate' deve esserci (bug: prima compariva solo con >= 3 gruppi).");
            Assert.That(SheetContains(xlsx, "Non Fatturate", "POSTICIPATA"), Is.True, "Posticipata presente.");
            Assert.That(SheetContains(xlsx, "Non Fatturate", "ELIMINATA"), Is.True, "Eliminata presente.");
        });
    }

    /// <summary>Con gestioneFatture null o vuota lo sheet "Non Fatturate" NON deve essere generato.</summary>
    [Test]
    public void ReportFattureRel_SenzaGestioneFatture_NessunNonFatturate()
    {
        var xlsxNull = CallReportFattureRel(2, gestione: null);
        var xlsxEmpty = CallReportFattureRel(2, gestione: new List<GestioneFattureReportExcelDto>());

        Assert.Multiple(() =>
        {
            Assert.That(SheetNames(xlsxNull), Does.Not.Contain("Non Fatturate"), "gestioneFatture null -> nessuno sheet.");
            Assert.That(SheetNames(xlsxEmpty), Does.Not.Contain("Non Fatturate"), "gestioneFatture vuota -> nessuno sheet.");
        });
    }

    // =================== Colonna "Note" dello sheet "Non Fatturate" ==============================
    // Il requisito e' fatto di DUE pezzi indipendenti, e vanno verificati tutti e due sul file vero:
    //   1) il valore in cella contiene i '\n' fra un'occorrenza e l'altra;
    //   2) la cella ha WrapText -- senza, Excel mostra tutto su una riga sola e il '\n' non si vede.
    // Il secondo dipende da Style = XCellStyle.Wrapper sull'attributo del DTO: e' l'unica cosa che
    // lega la colonna al CellFormat con WrapText, ed e' facilissimo perderla spostando la colonna.

    private static IXLCell CellaSottoIntestazione(IXLWorkbook wb, string sheet, string intestazione)
    {
        var ws = wb.Worksheet(sheet);
        var header = ws.Row(1).CellsUsed()
            .FirstOrDefault(c => string.Equals(c.GetString(), intestazione, StringComparison.OrdinalIgnoreCase));
        Assert.That(header, Is.Not.Null, $"Nello sheet '{sheet}' manca la colonna '{intestazione}'.");
        return ws.Cell(2, header!.Address.ColumnNumber);
    }

    [Test]
    public void ReportFattureRel_ColonnaNote_ConcatenataEConACapoNellaCella()
    {
        var gf = GfExcel("Ente-POST", "POSTICIPATA");
        gf.Note = "2026-02-01 10:00:00 prima nota" + FattureExtensions.SeparatoreNote + "2026-03-01 11:30:00 seconda nota";

        var xlsx = CallReportFattureRel(1, new[] { gf });

        using var wb = new XLWorkbook(new MemoryStream(xlsx));
        var cella = CellaSottoIntestazione(wb, "Non Fatturate", "Note");

        Assert.Multiple(() =>
        {
            Assert.That(cella.GetString(), Is.EqualTo(gf.Note), "Le note devono arrivare in cella integre.");
            Assert.That(cella.GetString().Split('\n'), Has.Length.EqualTo(2),
                "Le due occorrenze devono stare su due righe della stessa cella.");
            Assert.That(cella.Style.Alignment.WrapText, Is.True,
                "Senza WrapText (XCellStyle.Wrapper sull'HeaderAttributev2) Excel appiattisce le note su una riga sola.");
        });
    }

    [Test]
    public void ReportFattureRel_ColonnaNote_SenzaNote_CellaVuotaMaColonnaPresente()
    {
        // Caso predominante nei dati reali: Note = '[]' -> FlattenNote restituisce null. La colonna
        // deve comunque esserci (altrimenti il file cambia forma da un periodo all'altro).
        var gf = GfExcel("Ente-POST", "POSTICIPATA");
        gf.Note = null;

        var xlsx = CallReportFattureRel(1, new[] { gf });

        using var wb = new XLWorkbook(new MemoryStream(xlsx));
        var cella = CellaSottoIntestazione(wb, "Non Fatturate", "Note");

        Assert.That(cella.GetString(), Is.Empty);
    }

    // =================== Ordine delle colonne dello sheet "Non Fatturate" ========================
    // Requisito 08/09/2026: "Stato" prima colonna, "Note" seconda. E' un requisito di POSIZIONE, e
    // l'unica cosa che la decide e' l'Order degli HeaderAttributev2 sul DTO -- un valore che si
    // cambia per sbaglio rinumerando o aggiungendo una colonna, senza che nulla fallisca (fino
    // all'08/09 c'era un Order duplicato e l'ordine reale usciva giusto solo perche' OrderBy di LINQ
    // e' stabile). Si asserisce quindi sul FILE, non sul DTO: e' quello che vede l'utente.

    [Test]
    public void ReportFattureRel_NonFatturate_StatoPrimaColonna_NotesSeconda()
    {
        var gf = GfExcel("Ente-POST", "POSTICIPATA");
        gf.Note = "2026-02-01 10:00:00 prima nota";

        var xlsx = CallReportFattureRel(1, new[] { gf });

        using var wb = new XLWorkbook(new MemoryStream(xlsx));
        var intestazioni = wb.Worksheet("Non Fatturate").Row(1).CellsUsed()
            .OrderBy(c => c.Address.ColumnNumber)
            .Select(c => c.GetString())
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(intestazioni.Take(2), Is.EqualTo(new[] { "Stato", "Note" }),
                "Le prime due colonne devono essere Stato e Note, in quest'ordine. "
              + $"Ordine attuale: {string.Join(" | ", intestazioni)}");
            Assert.That(intestazioni, Is.Unique, "Intestazioni duplicate: c'e' un Order ripetuto sul DTO.");
        });
    }

    [Test]
    public void GestioneFattureReportExcelDto_OrderDelleColonne_TuttiUnivoci()
    {
        // Guardia contro il ritorno del difetto: due colonne con lo stesso Order non rompono nulla
        // subito (OrderBy stabile -> vince l'ordine di dichiarazione) ma rendono l'ordine reale
        // dipendente da come sono scritte le proprieta', non dai numeri. Qui si rompe subito.
        var ordini = typeof(GestioneFattureReportExcelDto).GetProperties()
            .Select(pi => (HeaderAttributev2?)Attribute.GetCustomAttribute(pi, typeof(HeaderAttributev2)))
            .Where(a => a != null)
            .Select(a => a!.Order)
            .ToList();

        Assert.That(ordini, Is.Unique, $"Order duplicati fra le colonne: {string.Join(", ", ordini)}");
    }
}
