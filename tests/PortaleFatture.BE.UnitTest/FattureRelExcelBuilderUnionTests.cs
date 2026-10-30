using System.Text.RegularExpressions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// PF-959: le REL PAC non fatturate entrano nello sheet "Regolari Esecuzioni" con una UNION in coda a
/// `_sqlRel`. Una UNION abbina le colonne per POSIZIONE, non per nome: una colonna spostata in uno dei
/// due rami non da' errore, ma mette per esempio il totale ivato nella colonna dell'imponibile. Il
/// compilatore non lo vede e la query gira lo stesso, per questo l'allineamento e' presidiato qui.
///
/// Lo stesso vincolo vale per `FattureUnionRelExcelPersistence`, che mette in UNION `_sqlRel` e
/// `_sqlNoteSenzaRel`: aggiungere una colonna solo alla prima rompe lo sheet "Enti Fatt.".
/// </summary>
public class FattureRelExcelBuilderUnionTests
{
    [Test]
    public void RamoPacNonFatturate_HaLeStesseColonneNelloStessoOrdineDiSelectRel()
    {
        var rel = Alias(FattureRelExcelBuilder.SelectRel());
        var pac = Alias(FattureRelExcelBuilder.SelectRelPacNonFatturate());

        Assert.That(pac, Is.EqualTo(rel));
    }

    [Test]
    public void RamoNoteSenzaRel_HaLeStesseColonneDiSelectRel()
    {
        var rel = Alias(FattureRelExcelBuilder.SelectRel());
        var note = Alias(FattureRelExcelBuilder.SelectNoteSenzaRel());

        Assert.That(note, Is.EqualTo(rel));
    }

    [Test]
    public void RamoPacNonFatturate_NonHaFatturaNeStorni()
    {
        var colonne = Colonne(FattureRelExcelBuilder.SelectRelPacNonFatturate())
            .ToDictionary(c => AliasDi(c), c => c, StringComparer.OrdinalIgnoreCase);

        Assert.Multiple(() =>
        {
            foreach (var campo in new[] { "IdFattura", "TipoDocumento", "DataFattura", "Progressivo", "CodiceMateriale" })
                Assert.That(colonne[campo], Does.StartWith("NULL"), campo);
            Assert.That(colonne["TotaleFatturaImponibile"], Does.StartWith("CAST(0"));
            Assert.That(colonne["RigaImponibile"], Does.StartWith("CAST(0"));
        });
    }

    /// <summary>
    /// Il ramo e' in coda alla query, e la persistence ci aggiunge i filtri opzionali: un ORDER BY o
    /// una seconda UNION dentro il ramo renderebbero invalido l'SQL composto.
    /// </summary>
    [Test]
    public void RamoPacNonFatturate_PuoRicevereFiltriInCoda()
    {
        var sql = SenzaCommenti(FattureRelExcelBuilder.SelectRelPacNonFatturate());

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Not.Contain("ORDER BY").IgnoreCase);
            Assert.That(sql, Does.Not.Contain("UNION").IgnoreCase);
            Assert.That(sql, Does.Contain("tp.Descrizione = 'PAC'"));
            Assert.That(sql, Does.Contain("rr.RelFatturata = 0"));
            Assert.That(sql, Does.Not.Contain("RelUpload").IgnoreCase,
                "il log di upload contiene anche i download: una REL scaricata ma non firmata sparirebbe");
        });
    }

    [Test]
    public void SelectRel_NonEspone_FatturaInviata()
    {
        Assert.That(Alias(FattureRelExcelBuilder.SelectRel()), Does.Not.Contain("FatturaInviata"));
    }

    // ---------------------------------------------------------------------------------------------

    private static List<string> Alias(string sql) => Colonne(sql).Select(AliasDi).ToList();

    /// <summary>Le espressioni fra il primo SELECT e il primo FROM, divise sulle virgole di primo livello.</summary>
    private static List<string> Colonne(string sql)
    {
        var testo = SenzaCommenti(sql);
        var inizio = Regex.Match(testo, @"\bSELECT\b", RegexOptions.IgnoreCase).Index + "SELECT".Length;
        var fine = Regex.Match(testo, @"\bFROM\b", RegexOptions.IgnoreCase).Index;
        var lista = testo[inizio..fine];

        var colonne = new List<string>();
        var profondita = 0;
        var corrente = new System.Text.StringBuilder();
        foreach (var ch in lista)
        {
            if (ch == '(') profondita++;
            if (ch == ')') profondita--;
            if (ch == ',' && profondita == 0)
            {
                colonne.Add(Regex.Replace(corrente.ToString(), @"\s+", " ").Trim());
                corrente.Clear();
                continue;
            }
            corrente.Append(ch);
        }
        colonne.Add(Regex.Replace(corrente.ToString(), @"\s+", " ").Trim());
        return colonne;
    }

    private static string AliasDi(string colonna)
    {
        var alias = Regex.Match(colonna, @"\bas\s+\[?(\w+)\]?\s*$", RegexOptions.IgnoreCase);
        if (alias.Success)
            return alias.Groups[1].Value;
        // senza alias: il nome e' quello della colonna, es. rr.[TipologiaFattura] o c.FkIdTipoContratto
        return Regex.Match(colonna, @"\[?(\w+)\]?\s*$").Groups[1].Value;
    }

    private static string SenzaCommenti(string sql) => Regex.Replace(sql, @"--[^\r\n]*", string.Empty);
}
