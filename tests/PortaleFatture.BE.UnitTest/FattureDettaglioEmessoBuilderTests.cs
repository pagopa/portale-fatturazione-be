using System.Text.RegularExpressions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// PF-976 (PF-972): la query del dettaglio emesso aggancia una sola REL per riga. La REL definitiva e' filtrata
/// per tipologia (PRIMO SALDO per ACCONTO e ANTICIPO); la REL temporanea passa da un OUTER APPLY con TOP 1 e la
/// stessa regola. Il difetto era nato commentando le due condizioni sulla tipologia (commit `e52649a`): questi
/// test fermano chi le toglie di nuovo, senza bisogno del DB. Il comportamento sui dati e' in
/// `FattureDettaglioEmessoIntegrationTests`.
/// </summary>
public class FattureDettaglioEmessoBuilderTests
{
    private static readonly string Sql = SenzaCommenti(FattureQueryRicercaBuilder.SelectDettaglioEmesso());

    private const string RegolaDefinitiva =
        "trt.TipologiaFattura = CASE WHEN tft.FkTipologiaFattura IN ('ACCONTO','ANTICIPO') THEN 'PRIMO SALDO' ELSE tft.FkTipologiaFattura END";

    private const string RegolaTemporanea =
        "r.TipologiaFattura = CASE WHEN tftt.FkTipologiaFattura IN ('ACCONTO','ANTICIPO') THEN 'PRIMO SALDO' ELSE tftt.FkTipologiaFattura END";

    [Test]
    public void RelDefinitiva_FiltrataPerTipologia()
    {
        Assert.That(Normalizza(Join("[pfd].[RelTestata] trt", "LEFT JOIN pfd.Contratti")), Does.Contain(Normalizza(RegolaDefinitiva)));
    }

    [Test]
    public void RelTemporanea_UnaSolaRigaConLaStessaRegola()
    {
        var apply = Normalizza(Join("OUTER APPLY (", ") tmprl"));

        Assert.Multiple(() =>
        {
            Assert.That(apply, Does.StartWith(Normalizza("OUTER APPLY ( SELECT TOP 1")));
            Assert.That(apply, Does.Contain("FROM pfd.tmpRelTestata r"));
            Assert.That(apply, Does.Contain(Normalizza(RegolaTemporanea)));
        });
    }

    /// <summary>La versione originale agganciava `pfd.tmpRelTestata` con una LEFT JOIN: deve restare solo l'OUTER APPLY.</summary>
    [Test]
    public void RelTemporanea_NonPiuInLeftJoin()
    {
        Assert.That(Sql, Does.Not.Match(@"JOIN\s+pfd\.tmpRelTestata"));
    }

    /// <summary>Nessun ripiego su un'altra REL (decisione del team DATA): niente ORDER BY sulla tipologia.</summary>
    [Test]
    public void NessunRipiegoSuAltraTipologia()
    {
        var apply = Join("OUTER APPLY (", ") tmprl");

        Assert.That(Regex.Match(apply, @"ORDER BY(?<o>[^)]*)").Groups["o"].Value, Does.Not.Contain("TipologiaFattura"));
    }

    /// <summary>Il OUTER APPLY sostituisce la join ma le colonne lette dalla SELECT restano le stesse.</summary>
    [TestCase("TotaleAnalogico")]
    [TestCase("TotaleDigitale")]
    [TestCase("TotaleNotificheAnalogiche")]
    [TestCase("TotaleNotificheDigitali")]
    [TestCase("Totale")]
    [TestCase("TotaleAnalogicoIva")]
    [TestCase("TotaleDigitaleIva")]
    [TestCase("TotaleIva")]
    [TestCase("Caricata")]
    [TestCase("RelFatturata")]
    public void ColonneTemporaneeLetteDallaSelect_EspostoDalloApply(string colonna)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Sql, Does.Contain($"tmprl.[{colonna}]"));
            Assert.That(Join("OUTER APPLY (", ") tmprl"), Does.Contain($"r.[{colonna}]"));
        });
    }

    private static string Join(string da, string a)
    {
        var inizio = Sql.IndexOf(da, StringComparison.Ordinal);
        Assert.That(inizio, Is.GreaterThanOrEqualTo(0), $"'{da}' non trovato nella query");
        var fine = Sql.IndexOf(a, inizio, StringComparison.Ordinal);
        Assert.That(fine, Is.GreaterThan(inizio), $"'{a}' non trovato dopo '{da}'");
        return Sql[inizio..(fine + a.Length)];
    }

    private static string SenzaCommenti(string sql) => Regex.Replace(sql, @"--[^\r\n]*", string.Empty);

    private static string Normalizza(string sql) => Regex.Replace(sql, @"\s+", " ").Trim();
}
