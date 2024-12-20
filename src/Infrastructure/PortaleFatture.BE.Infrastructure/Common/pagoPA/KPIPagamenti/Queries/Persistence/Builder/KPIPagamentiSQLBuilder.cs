namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Queries.Persistence.Builder;

internal static class KPIPagamentiSQLBuilder
{
    private static string _sqlScontoKPMG = @"
SELECT  
    k.recipient_id as RecipientId,
	k.year_quarter as YearQuarter,
    SUM(ISNULL(k.trx_total, 0)) AS TrxTotal,
    SUM(ISNULL(k.value_total, 0)) AS ValueTotal ,
    SUM(ISNULL(k.value_discount, 0)) AS ValueDiscount
FROM 
    [ppa].[KpiPagamenti_Sconto] k
    left outer join ppa.vContracts c
    on c.provider_name = k.psp_id
    and c.year_quarter = k.year_quarter
";

    private static string _sqlScontoCount = @"
SELECT
       count(DISTINCT(kk.recipient_id + '|' + kk.year_quarter))
  FROM [ppa].[KpiPagamenti_Sconto] kk
  left outer join ppa.vContracts cc
  on cc.provider_name = kk.psp_id
  and cc.year_quarter = kk.year_quarter
  left outer join ppa.Contracts c
  on c.contract_id = kk.recipient_id
  and c.year_quarter = kk.year_quarter
";
    private static string _sqlSconto = @"
SELECT
       c.name as RecipientName
      ,cc.name as PSPName
      ,kk.[psp_id] as PspId
      ,kk.[recipient_id] as RecipientId
      ,kk.[year_quarter] as YearQuarter
      ,kk.[trx_total] as TrxTotal
      ,ISNULL(kk.[value_total],0) as ValueTotal
      ,kk.[KpiOk] as KpiOk
      ,kk.[PercSconto] as PercSconto
      ,ISNULL(kk.[value_discount],0) as ValueDiscount
      ,kk.[linkReport] as LinkReport
      ,kk.[KpiList] as KpiList
      ,kk.FlagMQ as FlagMQ
  FROM [ppa].[KpiPagamenti_Sconto] kk
  left outer join ppa.vContracts cc
  on cc.provider_name = kk.psp_id
  and cc.year_quarter = kk.year_quarter
  left outer join ppa.Contracts c
  on c.contract_id = kk.recipient_id
  and c.year_quarter = kk.year_quarter
";

    private static string _sqlMatrice = @"
SELECT [kpi_id] as KpiId
      ,[kpi_threshold] as KpiThreshold
      ,[start] 
      ,[end]
      ,[psp_id] as PspId
      ,[psp_company_name] as PspCompanyName
      ,[psp_broker_id] as PspBrokerId
      ,[psp_broker_company_name] as PspBrokerCompanyName
      ,[perc_kpi] as PercKpi
      ,[kpi_outcome] as KpiOutcome
      ,[dl_event_tms] as DlEventTms
      ,[dl_ingestion_tms] as DlIngestionTms
  FROM [ppa].[KPIPagamenti]  
";
    public static string OrderMatriceByQuarter()
    {
        return " ORDER BY psp_id";
    }

    public static string OrderSconto()
    {
        return " ORDER BY CAST(RIGHT(kk.recipient_id, CHARINDEX('-', REVERSE(kk.recipient_id)) - 1) AS INT)";
    }

    public static string SelectMatriceByQuarter()
    {
        return _sqlMatrice;
    }

    public static string SelectSconto()
    {
        return _sqlSconto;
    }

    public static string SelectCountSconto()
    {
        return _sqlScontoCount;
    }

    public static string SelectScontoKPMG()
    {
        return _sqlScontoKPMG;
    }

    public static string GrouByOrderScontoKPMG()
    {
        return @" GROUP BY 
    k.recipient_id,
	k.year_quarter
ORDER BY CAST(RIGHT(k.recipient_id, CHARINDEX('-', REVERSE(k.recipient_id)) - 1) AS INT)";
    }
} 