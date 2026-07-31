using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Unit test PURI (nessun DB) su FattureQueryRicercaBuilder.SelectFattureRicerca, il punto dove api/fatture
/// sceglie il ramo (EMESSE vs NON FATTURATE) e risolve il filtro tipologia sulla colonna giusta.
/// Blindano il "contratto SQL" della modifica: ramo Cancellate -> vista be.vwDocumentiEmessiNonFatturati
/// avvolta con JSON_QUERY e filtro su [fattura.tipologiaFattura]; ramo normale -> SelectView su FattureTestata
/// con FT.FkTipologiaFattura; e la rimozione del placeholder quando non c'e' filtro tipologia.
/// </summary>
public class FattureQueryRicercaBuilderTests
{
    [Test]
    public void SelectFattureRicerca_Cancellata_ConTipologia_UsaVistaNonFatturate_eColonnaVista()
    {
        var sql = FattureQueryRicercaBuilder.SelectFattureRicerca(cancellata: true, hasTipologia: true);

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("[be].[vwDocumentiEmessiNonFatturati]"), "ramo Cancellate -> nuova vista.");
            Assert.That(sql, Does.Contain("listaFatture ="), "contratto JSON preservato (wrap).");
            Assert.That(sql, Does.Contain("JSON_QUERY([fattura.posizioni])"), "sotto-JSON embeddato, non escapato.");
            Assert.That(sql, Does.Contain("JSON_QUERY([fattura.datiGeneraliDocumento])"));
            Assert.That(sql, Does.Contain("and [fattura.tipologiaFattura] IN @TipologiaFattura"), "filtro su colonna vista.");
            Assert.That(sql, Does.Not.Contain("[condition_tipologiafattura]"), "placeholder risolto.");
        });
    }

    [Test]
    public void SelectFattureRicerca_NonCancellata_ConTipologia_UsaSelectView_eColonnaBase()
    {
        var sql = FattureQueryRicercaBuilder.SelectFattureRicerca(cancellata: false, hasTipologia: true);

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("[pfd].[FattureTestata]"), "ramo normale -> fatture emesse.");
            Assert.That(sql, Does.Contain("gf.Stato <> 0"), "il ramo emesse esclude le Posticipate.");
            Assert.That(sql, Does.Contain("and FT.FkTipologiaFattura IN @TipologiaFattura"), "filtro su colonna base.");
            Assert.That(sql, Does.Not.Contain("be.vwDocumentiEmessiNonFatturati"), "non usa la vista Non Fatturate.");
            Assert.That(sql, Does.Not.Contain("[condition_tipologiafattura]"));
        });
    }

    [Test]
    public void SelectFattureRicerca_SenzaTipologia_RimuoveIlPlaceholder_SenzaCondizione()
    {
        var sqlCancellate = FattureQueryRicercaBuilder.SelectFattureRicerca(cancellata: true, hasTipologia: false);
        var sqlNormale = FattureQueryRicercaBuilder.SelectFattureRicerca(cancellata: false, hasTipologia: false);

        Assert.Multiple(() =>
        {
            Assert.That(sqlCancellate, Does.Not.Contain("[condition_tipologiafattura]"), "placeholder sempre risolto.");
            Assert.That(sqlCancellate, Does.Not.Contain("IN @TipologiaFattura"), "senza tipologia -> nessuna condizione.");
            Assert.That(sqlNormale, Does.Not.Contain("[condition_tipologiafattura]"));
            Assert.That(sqlNormale, Does.Not.Contain("IN @TipologiaFattura"));
        });
    }
}
