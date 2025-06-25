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
    private static string _sqlCountReports = @"
SELECT  count(report_id)
  FROM [pfd].[ReportContestazioni] r
  inner join pfd.Enti e
  on e.InternalIstitutionId = r.internal_organization_id
  left join pfd.Contratti c
  on c.internalistitutionid = e.InternalIstitutionId
  inner join pfd.TipologiaReport t
  on t.IdTipologiaReport = r.FkIdTipologiaReport
";

    private static string _sqlReports = @"
WITH StatoGruppoEspanso AS (
    SELECT 
        CAST(sg.stato AS INT) AS stato,
        sg.descrizione AS descrizione,
        CAST(TRIM(value) AS INT) AS step
    FROM pfd.ReportContestazioniStato sg
    CROSS APPLY STRING_SPLIT(sg.steps, ',')
),
UltimoStepPerReport AS (
    SELECT 
        report_id,
        step,
        ROW_NUMBER() OVER (PARTITION BY report_id ORDER BY step DESC) AS rn
    FROM pfd.ReportContestazioniRighe
),
SingolaRigaPerReport AS (
    SELECT 
        *,
        ROW_NUMBER() OVER (PARTITION BY report_id ORDER BY step DESC) AS rn
    FROM pfd.ReportContestazioniRighe
)


SELECT r.[report_id] as ReportId
      ,[unique_id] as UniqueId
      ,[json] as Json
      ,[anno]
      ,[mese]
      ,[internal_organization_id] as InternalOrganizationId
      ,[contract_id] as ContractId
      ,[utente_id] as UtenteId
      ,[prodotto] as Prodotto
      ,g.stato 
	  ,g.descrizione as DescrizioneStato
      ,[data_inserimento] as DataInserimento
      ,[data_stepcorrente] as DataStepCorrente
      ,r.[storage]
      ,r.[nomedocumento]
      ,r.[link] as LinkDocumento
      ,[content_language] as ContentLanguage
      ,[content_type] as ContentType
      ,[FkIdTipologiaReport]
      ,[hash]
	  , description as RagioneSociale
	  , contract_id as ActualContractId
	  , t.TipologiaDocumento
	  , t.CategoriaDocumento
  FROM [pfd].[ReportContestazioni] r
  inner join pfd.Enti e
  on e.InternalIstitutionId = r.internal_organization_id
  left join pfd.Contratti c
  on c.internalistitutionid = e.InternalIstitutionId
  inner join pfd.TipologiaReport t
  on t.IdTipologiaReport = r.FkIdTipologiaReport
	LEFT JOIN SingolaRigaPerReport cr ON cr.report_id = r.report_id AND cr.rn = 1
	LEFT JOIN UltimoStepPerReport st ON st.report_id = r.report_id AND st.rn = 1
	LEFT JOIN StatoGruppoEspanso g ON g.step = st.step
";

    private static string _sqlRecap = @"
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
        return @"
GROUP BY 
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
        ORDER BY data_inserimento DESC";
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
}