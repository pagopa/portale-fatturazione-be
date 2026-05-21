namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence.Builder; 
internal static class ContestazioniMassiveSQLBuilder
{
 
    private static string _sqlTipologiaReport = @"
SELECT [IdTipologiaReport]
      ,[CategoriaDocumento]
      ,[TipologiaDocumento]
      ,[Descrizione]
      ,[Attivo]
      ,[DataModifica]
  FROM [pfd].[TipologiaReport]
";
    private static string _sqlCountReports = @"SELECT COUNT([ReportId]) FROM [be].[vwCMOverviewProcessi]";

    private static string _sqlReports = @"
    SELECT [ReportId]
          ,[UniqueId]
          ,[Json]
          ,[anno]
          ,[mese]
          ,[InternalOrganizationId]
          ,[ContractId]
          ,[UtenteId]
          ,[Prodotto]
          ,[stato]
          ,[DescrizioneStato]
          ,[DataInserimento]
          ,[DataStepCorrente]
          ,[storage]
          ,[nomedocumento]
          ,[LinkDocumento]
          ,[ContentLanguage]
          ,[ContentType]
          ,[FkIdTipologiaReport]
          ,[hash]
          ,[RagioneSociale]
          ,[ActualContractId]
          ,[TipologiaDocumento]
          ,[CategoriaDocumento]
      FROM [be].[vwCMOverviewProcessi]";

    private static string _sqlRecap = @"
       SELECT 
       [IdEnte]
      ,[Anno]
      ,[Mese]
      ,[ProdottoPostalizzazione]
      ,[TipologiaFattura]
      ,[IdFlagContestazione]
      ,[FlagContestazione]
      ,[Totale]
      FROM [be].[vwCMOverviewStatoNotifiche]";

    private static string _sqlRecapIntegration = @"
        DECLARE @Cent decimal = 100.00;
        SELECT 
	        CASE
		        WHEN N.TipologiaFattura = 'ANTICIPO' THEN 1
		        WHEN N.TipologiaFattura = 'ACCONTO' THEN 2
		        WHEN N.TipologiaFattura = 'ASSEVERAZIONE' THEN 3
		        WHEN N.TipologiaFattura = 'PRIMO SALDO' THEN 4
		        WHEN N.TipologiaFattura = 'SECONDO SALDO' THEN 5
		        WHEN N.TipologiaFattura = 'VAR. SEMESTRALE' THEN 6
		        WHEN N.TipologiaFattura = 'VAR. ANNUALE' THEN 7
		        ELSE 6
	        END AS ordine,
            ISNULL(N.TipologiaFattura, 'CONTESTAZIONE') as TipologiaFattura,
            F.IdFlagContestazione,
	        F.FlagContestazione,
            COUNT(*) AS Totale,
            SUM(CASE WHEN N.[notificationtype] LIKE '%Analog%' THEN 1 ELSE 0 END) AS TotaleNotificheAnalogiche,
            SUM(CASE WHEN N.[notificationtype] = 'Digitale' OR N.[notificationtype] = '' THEN 1 ELSE 0 END) AS TotaleNotificheDigitali
        FROM 
            [pfd].[Notifiche] N
        LEFT JOIN 
            [pfw].[Contestazioni] C ON C.[FkIdNotifica] = N.[event_id]
        INNER JOIN 
            [pfw].[FlagContestazione] F ON F.[IdFlagContestazione] = ISNULL(C.[FkIdFlagContestazione], 1)
        WHERE  
              ([cost_eurocent]/@Cent) > 0   
        ";

    private static string _sqlEnti = @"
SELECT e.[InternalIstitutionId] as IdEnte,
       e.[description] as RagioneSociale,
	   c.onboardingtokenid as ContractId
  FROM [pfd].[Enti] e
  left outer join pfd.Contratti c
  on c.internalistitutionid = e.InternalIstitutionId
";

    private static string _sqlAnni = @"
SELECT  
      distinct [year]
   FROM [pfd].[Notifiche]
";

    private static string _sqlMesi = @"
SELECT  
      distinct [month]
   FROM [pfd].[Notifiche]
";

    public static string OrderByYear()
    {
        return " ORDER BY year desc";
    }
    public static string OrderByMonth()
    {
        return " ORDER BY month desc";
    }

    public static string GroupByOrderByRecap()
    {
        return @"GROUP BY TipologiaFattura, IdFlagContestazione, FlagContestazione ORDER BY IdFlagContestazione";
    }

    public static string GroupByOrderByRecapIntegration()
    {
        return @"GROUP BY 
            N.TipologiaFattura,
            F.IdFlagContestazione,
	        F.FlagContestazione
        ORDER BY ordine, IdFlagContestazione
        ";
    }

    public static string SelectAnni()
    {
        return _sqlAnni;
    }

    public static string SelectEnti()
    {
        return _sqlEnti;
    }

    public static string SelectMesi()
    {
        return _sqlMesi;
    }

    public static string SelectRecap()
    {
        return _sqlRecap;
    }

    public static string SelectRecapIntegration()
    {
        return _sqlRecapIntegration;
    }

    public static string SelectReports()
    {
        return _sqlReports;
    }

    public static string SelectCountReports()
    {
        return _sqlCountReports;

    }
    private static string _offSet = " OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY";
    public static string OffSetReports()
    {
        return _offSet;
    }

    public static string OrderByReports()
    {
        return @" 
        ORDER BY [DataInserimento] DESC";
    }

    public static string SelectTipologiaReport()
    {
        return _sqlTipologiaReport;
    }

    public static string SelectReportSteps()
    {
        return @"
SELECT r.[report_id] as reportId
      ,s.[step]
	  ,s.descrizione as descrizionestep
      ,[TotaleNotificheAnalogicheARNazionali_AR] as TotaleNotificheAnalogicheARNazionaliAR
      ,[TotaleNotificheAnalogicheARInternazionali_RIR] as TotaleNotificheAnalogicheARInternazionaliRIR
      ,[TotaleNotificheAnalogicheRSNazionali_RS] as TotaleNotificheAnalogicheRSNazionaliRS
      ,[TotaleNotificheAnalogicheRSInternazionali_RIS] as TotaleNotificheAnalogicheRSInternazionaliRIS
      ,[TotaleNotificheAnalogiche890]
      ,[TotaleNotificheDigitali]
      ,[TotaleNotifiche]
      ,[Link]
      ,[NonContestataAnnullata]
      ,[ContestataEnte]
      ,[RispostaEnte]
      ,[Accettata]
      ,[RispostaSend]
      ,[RispostaRecapitista]
      ,[RispostaConsolidatore]
      ,[Rifiutata]
      ,[NonFatturabile]
      ,[Fatturabile]
      ,[Storage]
      ,[NomeDocumento]
      , CASE
            WHEN r.[step] >= 1 THEN
                CAST(
                    [DataCompletamento] AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time'
                AS DATETIME)
            ELSE
                [DataCompletamento]
        END AS [DataCompletamento]
  FROM [pfd].[ReportContestazioniRighe] r
  inner join pfd.ReportContestazioniStep s
  on r.step = s.step
  WHERE r.report_id=@IdReport
  order by step 
";
    }

    public static string SelectReportById()
    {
        return _sqlReports + $@"
  WHERE r.report_id=@IdReport";  
    }

    public static string SelectSteps()
    {
        return $@"
SELECT [step]
      ,[descrizione]
  FROM [pfd].[ReportContestazioniStep]
order by step";
    }

    public static string SelectContestazioneEnteAnniMesi()
    {
        return @"
SELECT 
    [MeseContestazione],
    [AnnoContestazione] 
FROM 
    [pfw].[ContestazioniCalendario]
WHERE 
    DATEADD(day, 30, [DataFine]) >= SYSDATETIMEOFFSET() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time'
ORDER BY 
    [AnnoContestazione] DESC, 
    [MeseContestazione] DESC;
";
    }
}