using System.Reflection;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.ImportModuloCommessaAderenti;

var connectionString = "Server=tcp:fat-d-sql.database.windows.net,1433;Initial Catalog=fat-sqldb;Persist Security Info=False;User ID=pfwebuser;Password=x&5DLJ-12CcF9ma;MultipleActiveResultSets=true;Encrypt=True;TrustServerCertificate=False;Connection Timeout=320;";

var query = @"INSERT INTO pfw.DatiModuloCommessaAderenti (dataExport, internalistitutionid, segmento, macrocategoriaVendita, sottocategoriaVendita, provincia, regione)
                 VALUES (@dataExport, @internalistitutionid, @segmento, @macrocategoriaVendita, @sottocategoriaVendita, @provincia, @regione)";

var appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

var fileName = "Documenti/categorizzazione_clienti_send_2025-07-14_08-50.json";  
var filePath = Path.Combine(appDirectory!, fileName);  

var datalist = JsonFileReader.ProcessJsonFile(filePath);

try
{
    using var connection = new SqlConnection(connectionString);
    connection.Open(); 
    using (var command = new SqlCommand(query, connection))
    {
        command.Parameters.Add("@dataExport", System.Data.SqlDbType.NVarChar);
        command.Parameters.Add("@internalistitutionid", System.Data.SqlDbType.NVarChar);
        command.Parameters.Add("@segmento", System.Data.SqlDbType.NVarChar);
        command.Parameters.Add("@macrocategoriaVendita", System.Data.SqlDbType.NVarChar);
        command.Parameters.Add("@sottocategoriaVendita", System.Data.SqlDbType.NVarChar);
        command.Parameters.Add("@provincia", System.Data.SqlDbType.NVarChar);
        command.Parameters.Add("@regione", System.Data.SqlDbType.NVarChar);

        foreach (var item in datalist)
        {
            command.Parameters["@dataExport"].Value = item.DataExport;
            command.Parameters["@internalistitutionid"].Value = item.Internalistitutionid;
            command.Parameters["@segmento"].Value = string.IsNullOrEmpty(item.Segmento)? DBNull.Value: item.Segmento;
            command.Parameters["@macrocategoriaVendita"].Value = string.IsNullOrEmpty(item.MacrocategoriaVendita) ? DBNull.Value : item.MacrocategoriaVendita;
            command.Parameters["@sottocategoriaVendita"].Value = string.IsNullOrEmpty(item.SottocategoriaVendita) ? DBNull.Value : item.SottocategoriaVendita;
            command.Parameters["@provincia"].Value = item.Provincia;
            command.Parameters["@regione"].Value = item.Regione;

            command.ExecuteNonQuery();
        }
    }  
}
catch (Exception ex)
{ 
    Console.WriteLine(ex.Message);
}

Console.WriteLine("Tutti i dati dell'array inseriti con successo usando ADO.NET!");