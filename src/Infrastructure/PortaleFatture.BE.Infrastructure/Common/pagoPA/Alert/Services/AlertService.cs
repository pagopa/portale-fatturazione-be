using System.Data;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Alert.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.Alert.Services;

public class AlertService(string cn) : IAlertService
{
    private readonly string _cn = cn;

    private readonly string _sqlSelectAlert = @"
SELECT TOP(1)
    [IdAlert],
    [FkIdGruppo],
    [Oggetto],
    [Messaggio],
    [DataInizio],
    [DataFine]
FROM [cfg].[Alert]
WHERE IdAlert = @IdAlert";

    private readonly string _sqlSelectRecipient = @"
SELECT 
    [IdGruppo],
    [Destinatario]
FROM cfg.AlertGroup
WHERE IdGruppo = @IdAlertGroup";


    private readonly string _sqlInsert = @"
INSERT INTO [pfd].[AlertServiceLog]
           ([EventDate]
           ,[Recipient]
           ,[FkIdAlert]
           ,[Sent])
     VALUES
           (@EventDate
           ,@Recipient
           ,@FkIdAlert
           ,@Sent);";

    public (AlertDto,List<string>) GetAlert(int IdAlert)
    {
        AlertDto alert = new AlertDto();
        int idAlertGroup = -1;
        List<string> emails = [];
        try
        {
            // Get Alert
            using var conn = new SqlConnection(_cn);
            conn.Open();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Parameters.Add("@IdAlert", SqlDbType.Int).Value = IdAlert;
            cmd.CommandText = _sqlSelectAlert;
            var reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    alert = new AlertDto(
                        reader.GetInt32(0)
                        , reader.GetInt32(1)
                        , reader.GetString(2)
                        , reader.GetString(3)
                        , reader.GetDateTime(4)
                        , reader.GetDateTime(5)
                        );
                    
                    idAlertGroup = reader.GetInt32(1);
                }
            }else
            {
                new Exception($"Nessun alert valido trovato. IdAlert: {IdAlert}");
            }
            reader.Close();


            // Get Recipients
            using var cmd2 = new SqlCommand();
            cmd2.Connection = conn;
            cmd2.Parameters.Add("@IdAlertGroup", SqlDbType.Int).Value = idAlertGroup;
            cmd2.CommandText = _sqlSelectRecipient;
            var reader2 = cmd2.ExecuteReader();
            if (reader2.HasRows)
            {
                while (reader2.Read())
                {
                    emails.Add(reader2.GetString(1));
                }
            }
            reader2.Close();
        }
        catch
        {
            return (new AlertDto(),[]);
        }
        return (alert,emails);
    }


    public bool InsertTracciatoEmail(AlertTracking alertLog)
    {
        try
        {
            using var conn = new SqlConnection(_cn);
            conn.Open();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Parameters.Add("@EventDate", SqlDbType.DateTime).Value = alertLog.EventDate;
            cmd.Parameters.Add("@Recipient", SqlDbType.NVarChar).Value = alertLog.Recipient;
            cmd.Parameters.Add("@FkIdAlert", SqlDbType.Int).Value = alertLog.FkIdAlert;
            cmd.Parameters.Add("@Sent", SqlDbType.Bit).Value = alertLog.Sent;
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