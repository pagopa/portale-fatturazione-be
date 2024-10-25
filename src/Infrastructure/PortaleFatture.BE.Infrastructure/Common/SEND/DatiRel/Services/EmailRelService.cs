using System.Data;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Services;

public class EmailRelService(string cn) : IEmailRelService
{
    private readonly string _cn = cn;
    private readonly string _sqlSelect = @"
        SELECT  
         [internal_organization_id] as idEnte
        ,[contract_id] as idContratto
        ,[TipologiaFattura]
        ,[year] as anno
        ,[month] as mese
        ,[PEC] as pec
        ,[RagioneSociale] as ragionesociale
        FROM [pfd].[EmailRel]
        WHERE [year] = @year
        AND [month] = @month
        AND [TipologiaFattura] = @tipologiaFattura
        AND Totale >0;";

    private readonly string _sqlInsert = @"
    INSERT INTO [pfd].[RelEmail]
            ([FkIdEnte]
            ,[contract_id]
            ,[TipologiaFattura]
            ,[year]
            ,[month]
            ,[DataEvento]
            ,[Pec]
            ,[Messaggio]
            ,[RagioneSociale]
            ,[Invio])
        VALUES
            (@idEnte
            ,@idContratto
            ,@TipologiaFattura
            ,@anno
            ,@mese
            ,@data
            ,@pec
            ,@messaggio
            ,@RagioneSociale
            ,@Invio);";

    public IEnumerable<RelEmail>? GetSenderEmail(int? anno, int? mese, string tipologiaFattura)
    {
        List<RelEmail> emails = [];
        try
        {
            using var conn = new SqlConnection(_cn);
            conn.Open();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Parameters.Add("@year", SqlDbType.Int).Value = anno;
            cmd.Parameters.Add("@month", SqlDbType.Int).Value = mese;
            cmd.Parameters.Add("@tipologiaFattura", SqlDbType.NVarChar).Value = tipologiaFattura;
            cmd.CommandText = _sqlSelect;
            var reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    emails.Add(new RelEmail()
                    {
                        IdEnte = reader.GetString(0),
                        IdContratto = reader.GetString(1),
                        TipologiaFattura = reader.GetString(2),
                        Anno = reader.GetInt32(3),
                        Mese = reader.GetInt32(4),
                        Pec = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        RagioneSociale = reader.GetString(6)
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

    public bool InsertTracciatoEmail(RelEmailTracking email)
    {
        try
        {
            using var conn = new SqlConnection(_cn);
            conn.Open();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Parameters.Add("@idEnte", SqlDbType.NVarChar).Value = email.IdEnte;
            cmd.Parameters.Add("@idContratto", SqlDbType.NVarChar).Value = email.IdContratto;
            cmd.Parameters.Add("@TipologiaFattura", SqlDbType.NVarChar).Value = email.TipologiaFattura;
            cmd.Parameters.Add("@anno", SqlDbType.Int).Value = email.Anno;
            cmd.Parameters.Add("@mese", SqlDbType.Int).Value = email.Mese;
            cmd.Parameters.Add("@data", SqlDbType.NVarChar).Value = email.Data;
            cmd.Parameters.Add("@pec", SqlDbType.NVarChar).Value = email.Pec;
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