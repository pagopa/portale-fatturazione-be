using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries.Persistence.Builder;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// SQL dell'anagrafica APP IO (modello dati dell'11/11/2026, PF-1016): da quali viste legge e come
/// si compone con i filtri accodati dalle persistence. L'esecuzione reale è coperta su DB seedato.
/// </summary>
[Category("AppIO")]
public class AppIoContrattiSQLBuilderTests
{
    /// <summary>
    /// Ogni query legge solo dalla vista dedicata dello schema be, mai dalla tabella appio.contracts.
    /// </summary>
    [TestCase("SelectAll", "[be].[vwAppioContracts]")]
    [TestCase("SelectAllCount", "[be].[vwAppioContracts]")]
    [TestCase("SelectNomi", "[be].[vwAppioContractsFiltroNome]")]
    [TestCase("SelectTipiEnte", "[be].[vwAppioContractsFiltroTipoEnte]")]
    public void Select_ShouldLeggereDallaVistaDedicata(string metodo, string vista)
    {
        var sql = (string)typeof(AppIoContrattiSQLBuilder).GetMethod(metodo)!.Invoke(null, null)!;

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain(vista));
            Assert.That(sql, Does.Not.Contain("appio."));
        });
    }

    /// <summary>
    /// La griglia aliasa tutte le colonne della vista sulle proprietà del DTO: contractId è
    /// onboardingtokenid, il valore del filtro "Nome Ente" è institution_id.
    /// </summary>
    [Test]
    public void SelectAll_ShouldAliasareTutteLeColonneSulDto()
    {
        var sql = AppIoContrattiSQLBuilder.SelectAll();

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("[onboardingtokenid] AS ContractId"));
            Assert.That(sql, Does.Contain("[institution_id] AS InstitutionId"));
            Assert.That(sql, Does.Contain("[institution_name] AS Name"));
            Assert.That(sql, Does.Contain("[prodotto] AS Prodotto"));
            Assert.That(sql, Does.Contain("[institution_type] AS InstitutionType"));
            Assert.That(sql, Does.Contain("[email_PEC] AS EmailPec"));
            Assert.That(sql, Does.Contain("[contract_state] AS ContractState"));
        });
    }

    /// <summary>
    /// Le query a cui le persistence accodano WHERE e ORDER BY non contengono già clausole.
    /// </summary>
    [Test]
    public void SelectDaComporre_ShouldNonContenereClausole()
    {
        foreach (var sql in new[] { AppIoContrattiSQLBuilder.SelectAll(), AppIoContrattiSQLBuilder.SelectAllCount(), AppIoContrattiSQLBuilder.SelectNomi() })
            Assert.That(sql, Does.Not.Contain("WHERE").And.Not.Contain("ORDER BY").And.Not.Contain("GROUP BY"));
    }

    /// <summary>
    /// L'ordinamento della griglia ha un secondo criterio univoco: con enti omonimi (OB1 e OB4 nel
    /// seed) l'ordine resta stabile fra le pagine.
    /// </summary>
    [Test]
    public void OrderBy_ShouldAvereUnCriterioUnivoco_PerPaginareInModoStabile()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AppIoContrattiSQLBuilder.OrderBy(), Is.EqualTo(" ORDER BY institution_name, onboardingtokenid"));
            Assert.That(AppIoContrattiSQLBuilder.OrderByName(), Is.EqualTo(" ORDER BY institution_name, institution_id"));
        });
    }

    /// <summary>
    /// La paginazione usa i parametri page e size.
    /// </summary>
    [Test]
    public void OffSet_ShouldUsareIParametriPageESize()
    {
        Assert.That(AppIoContrattiSQLBuilder.OffSet(), Is.EqualTo(" OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY"));
    }
}
