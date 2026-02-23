using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;

namespace PortaleFatture.BE.ImportFattureGrandiAderenti.Models;

internal static class SQLHelper
{
    public static SqlConnection GetConnection(string sqlServer, string database)
    { 
        var connectionString = $"Server=tcp:{sqlServer};Database={database};Encrypt=True;Authentication=ActiveDirectoryInteractive;";
        return new SqlConnection(connectionString);
    }
}
