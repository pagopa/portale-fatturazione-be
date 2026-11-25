using System.Text.RegularExpressions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Unit test PURI (nessun DB) su FattureQueryRicercaBuilder.SelectFattureRicerca, il punto dove api/fatture
/// sceglie il ramo (EMESSE vs NON FATTURATE) e risolve il filtro tipologia sulla colonna giusta.
/// Blindano il "contratto SQL" della modifica: ramo Cancellate -> vista be.vwDocumentiEmessiNonFatturati
/// avvolta con JSON_QUERY e filtro su [fattura.tipologiaFattura]; ramo normale -> SelectView su FattureTestata
/// con FT.FkTipologiaFattura; e la rimozione del placeholder quando non c'e' filtro tipologia.
/// </summary>
[Category("Fatture")]
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

    // ============================== PF-1003: fattura.azione ==============================
    // Blindano la forma della query che calcola l'azione. Il comportamento e' verificato su DB seedato da
    // FattureRicercaAzioneIntegrationTests; qui si fissano le scelte che il DB non rende visibili: la vista
    // letta una volta (non correlata) e la regola presa dalla vista invece che scritta a mano.

    private static string Emesse() => FattureQueryRicercaBuilder.SelectFattureRicerca(cancellata: false, hasTipologia: false);

    private static string Compatta(string sql) => Regex.Replace(sql, @"\s+", " ");

    [Test]
    public void Azione_RamoEmesse_ShouldEsporreIlCampoUnaVolta()
        => Assert.That(Regex.Matches(Emesse(), Regex.Escape("AS 'fattura.azione'")), Has.Count.EqualTo(1));

    [Test]
    public void Azione_ShouldLeggereLaVistaUnaSolaVolta_FiltrataPerIParametriDelPeriodo()
    {
        var sql = Compatta(Emesse());

        Assert.Multiple(() =>
        {
            Assert.That(Regex.Matches(sql, Regex.Escape("[be].[vwGestioneFattureFormAnniMesi]")), Has.Count.EqualTo(1),
                "Un solo riferimento alla vista.");
            Assert.That(sql, Does.Match(@"LEFT JOIN \( SELECT v\.tipologia_fattura, v\.Azione FROM \[be\]\.\[vwGestioneFattureFormAnniMesi\] v WHERE v\.anno = @AnnoRiferimento AND v\.mese = @MeseRiferimento \) vam ON vam\.tipologia_fattura = FT\.FkTipologiaFattura"),
                "La vista va filtrata con i parametri, non con le colonne di FT: cosi' non dipende dalla riga.");
            Assert.That(sql, Does.Not.Contain("vam.anno = FT.AnnoRiferimento"),
                "Una condizione correlata farebbe ricalcolare la ricorsione della vista per ogni fattura (piano PROD 24/11/2026).");
            Assert.That(sql, Does.Not.Contain("NOT EXISTS ( SELECT 1 FROM [be].[vwGestioneFattureFormAnniMesi]"));
        });
    }

    [Test]
    public void Azione_ShouldVenireDallaVista_NonDaUnaRegolaScrittaAMano()
    {
        var sql = Compatta(Emesse());

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("ELSE vam.Azione END AS 'fattura.azione'"));
            Assert.That(sql, Does.Not.Contain("IN ('ANTICIPO', 'ACCONTO') THEN 'ELIMINA'"),
                "La regola ELIMINA/POSTICIPA e' nella vista: duplicarla qui la farebbe divergere se DATA la cambia.");
            Assert.That(sql, Does.Not.Contain("THEN 'POSTICIPA'"));
        });
    }

    [Test]
    public void Azione_ShouldEssereNullPerInviateEPerStato0e3_PrimaDellaVista()
    {
        var sql = Compatta(Emesse());
        var caseAzione = Regex.Match(sql, @"CASE WHEN FT\.FatturaInviata = 1 THEN NULL WHEN gf\.Stato IN \(\s?0,\s?3\s?\) THEN NULL ELSE vam\.Azione END AS 'fattura\.azione'");

        Assert.That(caseAzione.Success, Is.True,
            "Ordine e condizioni: gia' inviata -> NULL, posticipata/eliminata -> NULL, altrimenti la vista. "
            + "Ripristinate (1) e cancellate (2) non vanno escluse.");
        Assert.That(sql, Does.Not.Contain("WHEN gf.Stato IS NOT NULL THEN NULL"),
            "Escludere tutti gli stati toglierebbe l'azione a ripristinate e cancellate, su cui si puo' agire di nuovo.");
    }

    [Test]
    public void Azione_JoinConLaVista_ShouldStareDopoGestioneFattureEPrimaDelWhere()
    {
        var sql = Emesse();
        var gf = sql.IndexOf("LEFT JOIN cfg.GestioneFatture gf", StringComparison.Ordinal);
        var vam = sql.IndexOf(") vam ON", StringComparison.Ordinal);
        var where = sql.IndexOf("(gf.Stato <> 0 OR  gf.Stato IS NULL)", StringComparison.Ordinal);

        Assert.That(new[] { gf, vam, where }, Is.All.GreaterThan(-1));
        Assert.That(gf < vam && vam < where, Is.True);
    }

    [Test]
    public void Azione_RamoEmesse_ShouldConservareLEsclusioneDellePosticipate()
        => Assert.That(Emesse(), Does.Contain("(gf.Stato <> 0 OR  gf.Stato IS NULL)"));

    [Test]
    public void Azione_RamoNonFatturate_ShouldNonCalcolareLAzione()
    {
        var sql = FattureQueryRicercaBuilder.SelectFattureRicerca(cancellata: true, hasTipologia: false);
        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Not.Contain("fattura.azione"));
            Assert.That(sql, Does.Not.Contain("vwGestioneFattureFormAnniMesi"));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Azione_FiltroTipologia_ShouldNonToccareLaJoinConLaVista(bool hasTipologia)
    {
        var sql = Compatta(FattureQueryRicercaBuilder.SelectFattureRicerca(cancellata: false, hasTipologia));
        Assert.That(sql, Does.Contain("vam ON vam.tipologia_fattura = FT.FkTipologiaFattura"));
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
