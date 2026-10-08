using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries.Persistence.Builder;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// AppIoContrattiSQLBuilder (PF-908): le persistence compongono queste stringhe con WHERE, ORDER BY
/// e OFFSET, quindi conta che ogni frammento sia componibile e punti alla vista giusta. L'esecuzione
/// reale è coperta su DB seedato da AppIoContrattiQueryIntegrationTests e Http/AppIoContrattiHttpTests.
/// </summary>
public class AppIoContrattiSQLBuilderTests
{
    private static IEnumerable<TestCaseData> Select()
    {
        // Tutte le query SELECT devono leggere dalla vista [be].[vwAppioContracts], altrimenti non si popola il DTO
        yield return new TestCaseData(AppIoContrattiSQLBuilder.SelectAll()).SetArgDisplayNames("SelectAll");
        yield return new TestCaseData(AppIoContrattiSQLBuilder.SelectContractsId()).SetArgDisplayNames("SelectContractsId");
        yield return new TestCaseData(AppIoContrattiSQLBuilder.SelectAllCount()).SetArgDisplayNames("SelectAllCount");
        yield return new TestCaseData(AppIoContrattiSQLBuilder.SelectQuarters()).SetArgDisplayNames("SelectQuarters");
    }

    /// <summary>
    /// Verifica che tutte le query SELECT puntino alla vista corretta [be].[vwAppioContracts]
    /// </summary>
    /// <param name="sql">La query SQL da verificare</param>
    [TestCaseSource(nameof(Select))]
    public void Select_ShouldLeggereDallaVistaAppIO(string sql)
    {
        // Tutte le query SELECT devono leggere dalla vista [be].[vwAppioContracts], altrimenti non si popola il DTO
        Assert.That(sql, Does.Contain("FROM [be].[vwAppioContracts]"));
    }

    /// <summary>
    /// Verifica che la query SELECT ALL aliasi correttamente tutte le colonne sul DTO, altrimenti Dapper non le popola 
    /// </summary>
    [Test]
    public void SelectAll_ShouldAliasareTutteLeColonneSulDto()
    {
        // Dapper lega per nome: un alias sbagliato non dà errore, lascia la proprietà a null
        var sql = AppIoContrattiSQLBuilder.SelectAll();

        // Le colonne della vista [be].[vwAppioContracts] sono: contract_id, name, tax_code, vat_code, vat_group, sdi_code, year_month, year_quarter
        foreach (var alias in new[] { "ContractId", "Name", "TaxCode", "VatCode", "VatGroup", "SdiCode", "YearMonth", "YearQuarter" })
            Assert.That(sql, Does.Contain($"as {alias}"), alias);
    }

    /// <summary>
    /// Verifica che le query SELECT ALL e SELECT ALL COUNT non contengano clausole WHERE o ORDER BY, altrimenti non si possono accodare
    /// </summary>
    [Test]
    public void SelectAllESelectAllCount_ShouldNonContenereClausole_PerPoterAccodareIlWhere()
    {
        // la persistence accoda WHERE, ORDER BY e OFFSET a queste due query
        Assert.Multiple(() =>
        {
            Assert.That(AppIoContrattiSQLBuilder.SelectAll(), Does.Not.Contain("WHERE").IgnoreCase);
            Assert.That(AppIoContrattiSQLBuilder.SelectAll(), Does.Not.Contain("ORDER BY").IgnoreCase);
            Assert.That(AppIoContrattiSQLBuilder.SelectAllCount(), Does.Not.Contain("WHERE").IgnoreCase);
        });
    }

    /// <summary>
    /// Verifica che la query dei trimestri termini con il GROUP BY, altrimenti non si possono accodare HAVING e ORDER BY
    /// </summary>
    [Test]
    public void SelectQuarters_ShouldTerminareColGroupBy_PerPoterAccodareHavingEOrderBy()
    {
        // AppIoContrattiQuartersQueryPersistence accoda "HAVING … ORDER BY …": il GROUP BY deve
        // essere l'ultima clausola
        Assert.That(AppIoContrattiSQLBuilder.SelectQuarters().TrimEnd(), Does.EndWith("GROUP BY [year_quarter]"));
    }

    /// <summary>
    /// Verifica che la query OFFSET sia componibile con i parametri @page e @size, altrimenti non si può fare paging
    /// </summary>
    [Test]
    public void OffSet_ShouldUsareIParametriPageESize()
    {
        Assert.That(AppIoContrattiSQLBuilder.OffSet(),
            Is.EqualTo(" OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY"));
    }

    /// <summary>
    /// Verifica che le query ORDER BY inizino con uno spazio, altrimenti non si possono accodare
    /// </summary>
    [Test]
    public void OrderBy_ShouldIniziareConUnoSpazio_PerEssereAccodabili()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AppIoContrattiSQLBuilder.OrderBy(), Is.EqualTo(" ORDER BY contract_id"));
            Assert.That(AppIoContrattiSQLBuilder.OrderByName(), Is.EqualTo(" ORDER BY name ASC"));
            Assert.That(AppIoContrattiSQLBuilder.OrderByQuarters(), Is.EqualTo(" ORDER BY year_quarter DESC"));
        });
    }

    /// <summary>
    /// La ricerca per nome raggruppa per contratto e nome, e del trimestre prende il più recente: la
    /// vista ha una riga per contratto e trimestre, e senza raggruppamento lo stesso ente uscirebbe
    /// una volta per trimestre. Il GROUP BY deve essere accodabile fra il WHERE e l'ORDER BY.
    /// </summary>
    [Test]
    public void RicercaPerNome_ShouldRaggrupparePerContrattoENome_ConIlTrimestrePiuRecente()
    {
        var sql = AppIoContrattiSQLBuilder.SelectContractsId() + " WHERE 1=1"
            + AppIoContrattiSQLBuilder.GroupByContractsId() + AppIoContrattiSQLBuilder.OrderByName();

        Assert.Multiple(() =>
        {
            Assert.That(AppIoContrattiSQLBuilder.SelectContractsId(), Does.Contain("MAX([year_quarter]) as YearQuarter"));
            Assert.That(AppIoContrattiSQLBuilder.GroupByContractsId(), Is.EqualTo(" GROUP BY [contract_id], [name]"));
            Assert.That(sql.IndexOf("GROUP BY"), Is.LessThan(sql.IndexOf("ORDER BY")));
        });
    }
}
