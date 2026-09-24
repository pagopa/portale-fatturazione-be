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

    /// <summary>
    /// Stessa filosofia, un gradino piu' in la': il DB c'e' ma il **seed non copre** un oggetto che
    /// la query sotto test attraversa. Senza questa guardia il test sarebbe rosso per una lacuna del
    /// seed e non per un difetto del prodotto — il rosso piu' fuorviante che ci sia, perche' la stack
    /// trace mostra codice di produzione.
    ///
    /// Il seed e' scritto a mano e va indietro rispetto al DB reale (v.
    /// `docs/test-integrazione-db-seedato.md`): quando manca qualcosa la strada e' chiedere la **DDL
    /// reale** e aggiungerla, non dedurla. Fino ad allora il test resta giallo con scritto cosa serve,
    /// e diventa verde da solo il giorno in cui l'oggetto entra nel seed.
    /// </summary>
    public static void SkipSeOggettoAssente(string? connectionString, params string[] oggetti)
    {
        SkipIfUnavailable(connectionString);

        using var conn = new SqlConnection(connectionString);
        conn.Open();

        foreach (var oggetto in oggetti)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT OBJECT_ID(@nome)";
            cmd.Parameters.AddWithValue("@nome", oggetto);

            if (cmd.ExecuteScalar() is null or DBNull)
                Assert.Ignore(
                    $"L'oggetto '{oggetto}' non e' nel DB seedato: test saltato, non fallito. "
                  + "Va aggiunto sotto tests/Data/ con la DDL reale, poi "
                  + "docker compose down -v && docker compose up -d --build.");
        }
    }
}
