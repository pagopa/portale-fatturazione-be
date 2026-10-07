namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries.Persistence.Builder;

public static class AppIoContrattiSQLBuilder
{
    private static string _offSet = " OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY";

    /// <summary>
    /// Restituisce la clausola SQL di paginazione basata su OFFSET e FETCH NEXT.
    /// </summary>
    /// <returns>La clausola SQL di paginazione basata su OFFSET e FETCH NEXT.</returns>
    public static string OffSet() => _offSet;

    /// <summary>
    /// Restituisce la clausola SQL che ordina i risultati per contract_id in ordine crescente.
    /// </summary>
    /// <returns>La clausola SQL di ordinamento per contract_id crescente.</returns>
    public static string OrderBy() => " ORDER BY contract_id";

    /// <summary>
    /// Restituisce la clausola SQL che ordina i risultati per nome in ordine crescente.
    /// </summary>
    /// <returns>La clausola SQL di ordinamento per nome crescente.</returns>
    public static string OrderByName() => " ORDER BY name ASC";

    private static string _sql =
    @"
        SELECT
        [contract_id] as ContractId
        ,[name] as Name 
        ,[tax_code] as TaxCode
        ,[vat_code] as VatCode
        ,[vat_group] as VatGroup
        ,[sdi_code] as SdiCode
        ,[year_month] as YearMonth
        ,[year_quarter] as YearQuarter
        FROM [be].[vwAppioContracts]
    ";

    /// <summary>
    /// Seleziona tutti i contratti dalla vista vwAppioContracts.
    /// </summary>
    /// <returns>La query SQL che seleziona tutti i contratti.</returns>
    public static string SelectAll() => _sql;

    private static string _sqlContractsId = 
    @"
        SELECT 
        [contract_id] as ContractId, 
        [name] as Name, 
        [year_quarter] as YearQuarter 
        FROM [be].[vwAppioContracts]
    ";

    /// <summary>
    /// Seleziona id, nome e trimestre di tutti i contratti dalla vista vwAppioContracts.
    /// </summary>
    /// <returns>La query SQL che seleziona id, nome e trimestre dei contratti.</returns>
    public static string SelectContractsId() => _sqlContractsId;


    private static string _sqlCount =
    @"
        SELECT COUNT(*)
        FROM [be].[vwAppioContracts]
    ";

    /// <summary>
    /// Restituisce la query SQL che conta i contratti della vista vwAppioContracts.
    /// </summary>
    /// <returns>La query SQL di conteggio dei contratti.</returns>
    public static string SelectAllCount() => _sqlCount;

    private static string _sqlQuarters =
    @"
        SELECT  
        [year_quarter] as YearQuarter 
        FROM [be].[vwAppioContracts]
        GROUP BY [year_quarter]
    ";

    /// <summary>
    /// Seleziona i valori distinti di year_quarter dalla vista vwAppioContracts.
    /// </summary>
    /// <returns>La query SQL che seleziona i trimestri distinti.</returns>
    public static string SelectQuarters() => _sqlQuarters;

    /// <summary>
    /// Restituisce la clausola SQL che ordina i risultati per year_quarter in ordine decrescente.
    /// </summary>
    /// <returns>La clausola SQL di ordinamento per year_quarter decrescente.</returns>
    public static string OrderByQuarters() => " ORDER BY year_quarter DESC";
}