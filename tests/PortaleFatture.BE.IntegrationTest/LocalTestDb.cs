using Microsoft.Extensions.Configuration;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Connessione al DB locale seeded per i test di Gestione Fatture (container tests/docker-compose.yml,
/// immagine SQL Server 2025). Deterministico, niente VPN. Override da config IntegrationTest:LocalDbConnectionString.
/// Avvio: da tests/ eseguire  docker compose up -d --build
/// </summary>
public static class LocalTestDb
{
    public const string Default =
        "Server=localhost,1433;Database=master;User Id=sa;Password=52JdGnzZaANhf;TrustServerCertificate=True";

    public static string ConnectionString =>
        ServiceProvider.GetRequiredService<IConfiguration>()["IntegrationTest:LocalDbConnectionString"] ?? Default;
}
