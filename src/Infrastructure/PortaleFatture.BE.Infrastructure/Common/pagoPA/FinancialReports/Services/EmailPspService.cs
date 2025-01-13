using System.Data;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Entities.pagoPA.AnagraficaPSP;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Services;

public class EmailPspService(string cn) : IEmailPspService
{
    private readonly string _cn = cn;

    private readonly string _sqlSelectReinvio = @"
WITH RankedEmails AS (
    SELECT 
        [IdContratto],
        [Tipologia],
        [Anno],
        [Trimestre],
        [DataEvento],
        [Email],
        [Messaggio],
        [RagioneSociale],
        [Invio],
        ROW_NUMBER() OVER (
            PARTITION BY [IdContratto], [Trimestre] 
            ORDER BY [DataEvento] DESC
        ) AS RowNum
    FROM [ppa].[PspEmail]
)
SELECT 
    [IdContratto],
    [Tipologia],
    [Anno],
    [Trimestre],
    [DataEvento],
    [Email],
    [Messaggio],
    [RagioneSociale],
    [Invio]
FROM RankedEmails
WHERE RowNum = 1
AND Invio=0 AND trimestre = @year_quarter";

    private readonly string _sqlCountInvioEmail = @"
    SELECT 
        count(idcontratto) 
    FROM [ppa].[PspEmail]
    WHERE trimestre = @year_quarter";

    private readonly string _sqlSelect = @"
SELECT 
    k.contract_id, 
    c.name,
    k.year_quarter,
    ISNULL([referentefattura_mail],[courtesy_mail]) as email,
    k.descrizione_riga as links,
	linkReport as DiscountReport
FROM   ppa.kpmg k
INNER JOIN   [ppa].[Contracts] c
ON     c.contract_id = k.contract_id
AND    c.year_quarter = k.year_quarter
left JOIN
       (
                       SELECT DISTINCT [recipient_id],
                                       year_quarter ,
                                       [linkReport]
                       FROM            [ppa].[KpiPagamenti_Sconto]
                       WHERE           percsconto > 0) AS sconti
ON     k.contract_id = sconti.recipient_id 
and k.year_quarter = sconti.year_quarter
where k.year_quarter =@year_quarter
AND    len(k.descrizione_riga) > 0";

    private readonly string _sqlInsert = @"
INSERT INTO [ppa].[PspEmail]
           ([IdContratto]
           ,[Tipologia]
           ,[Anno]
           ,[Trimestre]
           ,[DataEvento]
           ,[Email]
           ,[Messaggio]
           ,[RagioneSociale]
           ,[Invio])
     VALUES
           (@IdContratto
           ,@Tipologia
           ,@Anno
           ,@Trimestre
           ,@data
           ,@Email
           ,@Messaggio
           ,@RagioneSociale
           ,@Invio);";

    public bool CountInvio(string? trimestre)
    {
        try
        {
            using var conn = new SqlConnection(_cn);
            conn.Open();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Parameters.Add("@year_quarter", SqlDbType.NVarChar).Value = trimestre;
            cmd.CommandText = _sqlCountInvioEmail;
            var count = cmd.ExecuteScalar();
            if (count is not null)
                return Convert.ToInt32(count) > 0;
            else
                return false;
        }
        catch
        {
            return false;
        } 
    }

    public List<string?> GetSenderEmailReinvio(string? trimestre)
    {
        List<string?> emails = [];
        try
        {
            using var conn = new SqlConnection(_cn);
            conn.Open();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Parameters.Add("@year_quarter", SqlDbType.NVarChar).Value = trimestre;
            cmd.CommandText = _sqlSelectReinvio;
            var reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    var reports = reader.GetString(4).Split(",");
                    emails.Add(reader.GetString(0));
                }
            }
            reader.Close();
        }
        catch
        {
            return []; 
        }
        return emails; 
    }

    public IEnumerable<PspEmail>? GetSenderEmail(string? trimestre)
    {
        List<PspEmail> emails = [];
        try
        {
            using var conn = new SqlConnection(_cn);
            conn.Open();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Parameters.Add("@year_quarter", SqlDbType.NVarChar).Value = trimestre;
            cmd.CommandText = _sqlSelect;
            var reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    var reports = reader.GetString(4).Split(",");
                    emails.Add(new PspEmail()
                    {
                        IdContratto = reader.GetString(0),
                        RagioneSociale = reader.GetString(1),
                        Tipologia = EmailPspTipologia.Financial,
                        Anno = Convert.ToInt32(reader.GetString(2)!.Split("_")[0]),
                        Trimestre = reader.GetString(2),
                        Email = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        AgentReport = reports.Where(x => x.ToLower().Contains("agentquarter")).FirstOrDefault(),
                        DetailReport = reports.Where(x => x.ToLower().Contains("detailed")).FirstOrDefault(),
                        DiscountReport = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                    });
                }
            }
            reader.Close();
        }
        catch
        {


        }
        return emails;
    }

    public bool InsertTracciatoEmail(PspEmailTracking email)
    {
        try
        {
            using var conn = new SqlConnection(_cn);
            conn.Open();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Parameters.Add("@IdContratto", SqlDbType.NVarChar).Value = email.IdContratto;
            cmd.Parameters.Add("@Tipologia", SqlDbType.NVarChar).Value = email.Tipologia;
            cmd.Parameters.Add("@anno", SqlDbType.Int).Value = email.Anno;
            cmd.Parameters.Add("@trimestre", SqlDbType.NVarChar).Value = email.Trimestre;
            cmd.Parameters.Add("@data", SqlDbType.NVarChar).Value = email.Data;
            cmd.Parameters.Add("@email", SqlDbType.NVarChar).Value = email.Email;
            cmd.Parameters.Add("@messaggio", SqlDbType.NVarChar).Value = email.Messaggio;
            cmd.Parameters.Add("@RagioneSociale", SqlDbType.NVarChar).Value = email.RagioneSociale;
            cmd.Parameters.Add("@Invio", SqlDbType.Bit).Value = email.Invio;
            cmd.CommandText = _sqlInsert;
            var rows = cmd.ExecuteNonQuery();
            return rows == 1;
        }
        catch
        {

            return false;
        }
    }
}