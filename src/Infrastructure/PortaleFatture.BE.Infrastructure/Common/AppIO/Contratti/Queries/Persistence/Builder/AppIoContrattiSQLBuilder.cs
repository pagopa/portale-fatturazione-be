namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries.Persistence.Builder;

/// <summary>
/// Query dell'anagrafica APP IO sulle viste [be].[vwAppioContracts], [be].[vwAppioContractsFiltroNome]
/// e [be].[vwAppioContractsFiltroTipoEnte] (modello dati dell'11/11/2026, PF-1016).
/// </summary>
public static class AppIoContrattiSQLBuilder
{
    private static readonly string _offSet = " OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY";

    /// <summary>
    /// Restituisce la clausola SQL di paginazione basata su OFFSET e FETCH NEXT.
    /// </summary>
    /// <returns>La clausola SQL di paginazione basata su OFFSET e FETCH NEXT.</returns>
    public static string OffSet() => _offSet;

    /// <summary>
    /// Ordinamento della griglia: per nome dell'ente, poi per contratto. Il secondo criterio rende
    /// l'ordine stabile fra le pagine anche con enti omonimi.
    /// </summary>
    /// <returns>La clausola ORDER BY.</returns>
    public static string OrderBy() => " ORDER BY institution_name, onboardingtokenid";

    private static readonly string _sql =
    @"
        SELECT
         [onboardingtokenid] AS ContractId
        ,[institution_id] AS InstitutionId
        ,[institution_name] AS Name
        ,[prodotto] AS Prodotto
        ,[institution_type] AS InstitutionType
        ,[email_PEC] AS EmailPec
        ,[contract_state] AS ContractState
        FROM [be].[vwAppioContracts]
    ";

    /// <summary>
    /// Seleziona i contratti dalla vista vwAppioContracts.
    /// </summary>
    /// <returns>La query SQL, senza WHERE né ORDER BY.</returns>
    public static string SelectAll() => _sql;

    private static readonly string _sqlCount =
    @"
        SELECT COUNT(*)
        FROM [be].[vwAppioContracts]
    ";

    /// <summary>
    /// Conta i contratti della vista vwAppioContracts.
    /// </summary>
    /// <returns>La query SQL di conteggio, senza WHERE.</returns>
    public static string SelectAllCount() => _sqlCount;

    private static readonly string _sqlNomi =
    @"
        SELECT
         [institution_id] AS InstitutionId
        ,[institution_name] AS Name
        FROM [be].[vwAppioContractsFiltroNome]
    ";

    /// <summary>
    /// Seleziona le voci del filtro "Nome Ente" (una per ente: la vista è già DISTINCT).
    /// </summary>
    /// <returns>La query SQL, senza WHERE né ORDER BY.</returns>
    public static string SelectNomi() => _sqlNomi;

    /// <summary>
    /// Ordinamento delle voci del filtro "Nome Ente": per nome, poi per ente.
    /// </summary>
    /// <returns>La clausola ORDER BY.</returns>
    public static string OrderByName() => " ORDER BY institution_name, institution_id";

    private static readonly string _sqlTipiEnte =
    @"
        SELECT [TipoEnte]
        FROM [be].[vwAppioContractsFiltroTipoEnte]
        ORDER BY [TipoEnte]
    ";

    /// <summary>
    /// Seleziona i tipi ente distinti, in ordine alfabetico.
    /// </summary>
    /// <returns>La query SQL completa.</returns>
    public static string SelectTipiEnte() => _sqlTipiEnte;
}
