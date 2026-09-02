using Microsoft.Data.SqlClient;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Helper per rendere gli integration test DB-dipendenti degradabili a "ignorati" (warning giallo)
/// invece che "falliti" (rosso) quando l'infrastruttura non c'e':
///   - VPN di UAT non attiva (test che puntano al DB UAT);
///   - container SQL locale spento (test che usano LocalTestDb / docker compose).
/// Chiamare in [SetUp]: se il DB non risponde entro un breve timeout, il test viene saltato con un
/// messaggio che spiega cosa avviare, cosi' una suite lanciata senza infra non produce falsi rossi.
/// </summary>
public static class TestDb
{
    public static void SkipIfUnavailable(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            Assert.Ignore("Connection string non configurata: DB non verificabile.");

        // timeout corto: non vogliamo aspettare i ~15-30s di default quando l'host non c'e'.
        var csb = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = 3 };
        try
        {
            using var conn = new SqlConnection(csb.ConnectionString);
            conn.Open();
        }
        catch (SqlException e)
        {
            Assert.Ignore(
                "DB non raggiungibile: la suite e' stata saltata, non fallita. "
              + "Per UAT: attivare la VPN. Per i test locali: avviare il container "
              + "(da tests/: docker compose up -d --build). Dettaglio: " + e.Message);
        }
    }
}
