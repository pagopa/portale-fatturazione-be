using MediatR;
using Microsoft.Extensions.Configuration;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test end-to-end (handler MediatR -> DB) per il report Fatture Sospese,
/// che legge la vista [be].[vwFattureSospeseReport] ed espone la colonna RelNonFirmata.
///
/// REQUISITI: i dati esistono solo in UAT. Impostare in appsettings.Development.json / user secrets:
///   - PortaleFattureOptions:ConnectionString -> connessione UAT
///   - IntegrationTest:Anno / IntegrationTest:Mese / IntegrationTest:TipologiaFattura -> periodo con dati
///
/// Convenzioni:
///   - I test "strutturali" (esecuzione query, periodo vuoto) richiedono solo connettivita' al DB
///     (validano schema/colonna) e passano anche senza dati.
///   - I test "data-dependent" usano Assume.That: senza dati per il periodo risultano Inconclusive, non falliti.
/// </summary>
public class FattureSospeseRelExcelQueryTests
{
    private IMediator _handler;
    private IConfiguration _conf;

    // indici dei bucket restituiti dagli handler: { rel(diretta), union, note }
    private const int ViewBranch = 0;
    private const int UnionBranch = 1;
    private const int NoteBranch = 2;

    [SetUp]
    public void Setup()
    {
        _handler = ServiceProvider.GetRequiredService<IMediator>();
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
    /// [be].[vwFattureSospeseReport] espone RelNonFirmata e l'UNION col ramo note e' allineato.
    /// Una colonna/vista errata farebbe fallire la query con "Invalid column name".
    /// </summary>
    [Test]
    public async Task Query_ShouldExecute_AndReturnThreeBuckets()
    {
        var result = await _handler.Send(BuildSospeseQuery());

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(3)); // rel diretta, union, note
        Assert.That(result[ViewBranch], Is.Not.Null);
        Assert.That(result[UnionBranch], Is.Not.Null);
        Assert.That(result[NoteBranch], Is.Not.Null);
    }

    /// <summary>
    /// Periodo sicuramente privo di dati: valida l'intera pipeline (SQL/mapping/UNION) senza dipendere
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
    /// Esercita il binding dei parametri opzionali (FkIdTipoContratto / FatturaInviata): la query deve
    /// eseguire senza errori e restituire i 3 bucket.
    /// </summary>
    [TestCase(null, null)]
    [TestCase(1, null)]
    [TestCase(null, 0)]
    [TestCase(null, 1)]
    [TestCase(null, 2)] // "in elaborazione"
    public async Task Query_WithOptionalFilters_ShouldExecute(int? fkIdTipoContratto, int? fatturaInviata)
    {
        var result = await _handler.Send(
            BuildSospeseQuery(fkIdTipoContratto: fkIdTipoContratto, fatturaInviata: fatturaInviata));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(3));
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

    // -------- Data-dependent (UAT) --------

    /// <summary>
    /// Nel ramo vista, RelNonFirmata deve essere costante per IdFattura (relazione 1:1):
    /// garantisce assenza di fan-out e quindi nessuna duplicazione/gonfiamento nella UNION.
    /// </summary>
    [Test]
    public async Task Query_ViewBranch_RelNonFirmata_IsConsistentPerIdFattura()
    {
        var result = await _handler.Send(BuildSospeseQuery());
        var viewRows = result![ViewBranch]?.ToList() ?? new List<FattureRelExcelDto>();

        Assume.That(viewRows.Count, Is.GreaterThan(0),
            "Nessun dato per il periodo: eseguire in UAT valorizzando IntegrationTest:Anno/Mese/TipologiaFattura.");

        var fanOut = viewRows
            .GroupBy(x => x.IdFattura)
            .Where(g => g.Select(x => x.RelNonFirmata).Distinct().Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.That(fanOut, Is.Empty,
            "RelNonFirmata deve essere costante per IdFattura (no fan-out / no duplicazione nella UNION).");
    }

    /// <summary>
    /// Nel ramo note la vista non espone la colonna: la query usa il placeholder '' (string.Empty)
    /// per mantenere allineato l'UNION.
    /// </summary>
    [Test]
    public async Task Query_NoteBranch_RelNonFirmata_IsEmptyPlaceholder()
    {
        var result = await _handler.Send(BuildSospeseQuery());
        var noteRows = result![NoteBranch]?.ToList() ?? new List<FattureRelExcelDto>();

        Assume.That(noteRows.Count, Is.GreaterThan(0),
            "Nessuna riga nel ramo note per il periodo: eseguire in UAT.");

        Assert.That(noteRows.All(x => x.RelNonFirmata == string.Empty), Is.True,
            "Le righe del ramo note devono avere RelNonFirmata = '' (placeholder).");
    }

    /// <summary>
    /// La UNION deduplica: il numero di righe del bucket union non puo' superare la somma di
    /// ramo vista + ramo note (protezione contro duplicazioni introdotte dalla colonna aggiunta).
    /// </summary>
    [Test]
    public async Task Query_UnionBranch_ShouldNotExceed_ViewPlusNote()
    {
        var result = await _handler.Send(BuildSospeseQuery());
        var view = result![ViewBranch]?.Count() ?? 0;
        var note = result[NoteBranch]?.Count() ?? 0;
        var union = result[UnionBranch]?.Count() ?? 0;

        Assume.That(view + note, Is.GreaterThan(0),
            "Nessun dato per il periodo: eseguire in UAT.");

        Assert.That(union, Is.LessThanOrEqualTo(view + note),
            "Il bucket union non deve contenere piu' righe della somma vista + note (no duplicazioni).");
    }

    // -------- Regressione report non-sospesi (DTO condiviso) --------

    /// <summary>
    /// Il report Fatture Emesse (non sospese) usa lo stesso DTO FattureRelExcelDto ma query che NON
    /// leggono la vista: deve continuare a eseguire senza errori. Per quei dati RelNonFirmata resta null.
    /// </summary>
    [Test]
    public async Task RelExcelQuery_NonSospese_ShouldExecute_AndRelNonFirmataIsNull()
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

        var rows = result[ViewBranch]?.ToList() ?? new List<FattureRelExcelDto>();
        Assume.That(rows.Count, Is.GreaterThan(0), "Nessun dato per il periodo: eseguire in UAT.");
        Assert.That(rows.All(x => x.RelNonFirmata == null), Is.True,
            "Nel report non-sospesi RelNonFirmata non e' selezionata: deve restare null.");
    }
}
