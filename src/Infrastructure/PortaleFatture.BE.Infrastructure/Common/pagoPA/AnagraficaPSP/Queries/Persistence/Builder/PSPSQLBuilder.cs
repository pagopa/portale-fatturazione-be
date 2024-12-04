namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries.Persistence.Builder;

internal static class PSPSQLBuilder
{
    private static string _sqlQuarters = @"
SELECT  distinct(year_quarter)
 FROM [ppa].[Contracts]
";


    private static string _sqlCount = @"
SELECT count(*)
  FROM [ppa].[Contracts]
";
    private static string _sql = @"
SELECT [contract_id] as ContractId
      ,[document_name] as DocumentName
      ,[provider_names] as ProviderNames
      ,[signed_date] as SignedDate
      ,[contract_type] as ContractType
      ,[name] as Name 
      ,[abi] as Abi
      ,[tax_code] as TaxCode
      ,[vat_code] as VatCode
      ,[vat_group] as VatGroup
      ,[pec_mail] as PecMail
      ,[courtesy_mail] as CourtesyMail
      ,[referentefattura_mail] as ReferenteFatturaMail
      ,[sdd] as Sdd
      ,[sdi_code] as SdiCode
      ,[membership_id] as MembershipId
      ,[recipient_id] as RecipientId
      ,[year_month] as YearMonth
      ,[year_quarter] as YearQuarter
  FROM [ppa].[Contracts]
";
    private static string _sqlContractsId = @"
SELECT [contract_id] as contractId, [name], [year_quarter] as YearQuarter   FROM [ppa].[Contracts]
";
    private static string _offSet = " OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY";
    public static string OffSet()
    {
        return _offSet;
    }

    public static string OrderBy()
    {
        return " ORDER BY contract_id";
    }

    public static string OrderByName()
    {
        return " ORDER BY name ASC";
    }

    public static string SelectAll()
    {
        return _sql;
    }

    public static string SelectContractsId()
    {
        return _sqlContractsId;
    }

    public static string SelectAllCount()
    {
        return _sqlCount;
    }
    public static string SelectQuarters()
    {
        return _sqlQuarters;
    }

    public static string OrderByQuarters()
    {
        return " order by year_quarter desc";
    } 
}