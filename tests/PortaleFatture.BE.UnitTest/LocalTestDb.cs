namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Connessione al DB locale seeded (container tests/docker-compose.yml, immagine SQL Server 2025).
/// Usata dai test che, pur vivendo in questo progetto, richiedono un DB reale (pregressi PF-705).
/// Avvio: da tests/ eseguire  docker compose up -d --build
/// </summary>
public static class LocalTestDb
{
    public const string ConnectionString =
        "Server=localhost,1433;Database=master;User Id=sa;Password=52JdGnzZaANhf;TrustServerCertificate=True";

    /// <summary>
    /// Esegue SQL arbitrario sul DB seeded. Serve ai test che scrivono dati per ripulirli prima e
    /// dopo: senza cleanup la seconda esecuzione fallisce (es. DatiFatturazione e' unico per ente).
    /// </summary>
    public static async Task ExecuteAsync(string sql)
    {
        using var conn = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }
}
