using Microsoft.Data.SqlClient;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Dati di appoggio dei test del testbook APP IO (PF-550_TESTBOOK_APP-IO, casi T01–T26).
///
/// Il testbook è scritto sui dati di UAT: cerca l'ente "acquedotto" (T06) e si aspetta i profili GSP,
/// PA e PSP (T02, T05). Il seed (tests/Data/appio.sql) non ha né un ente con quel nome né un profilo
/// PSP, quindi le fixture del testbook aggiungono due contratti <b>sintetici</b> all'inizio e li
/// tolgono alla fine. Stanno fuori dal seed apposta: le altre suite APP IO fissano conteggi sulle
/// righe APPIO-OB%, che così restano invariati.
///
/// I due contratti non hanno documenti contabili: i casi T13–T25 lavorano sui documenti del seed.
/// </summary>
public static class AppIoTestbookDati
{
    /// <summary>Testo cercato in T06.</summary>
    public const string RicercaEnte = "acquedotto";

    /// <summary>Ente che l'autocomplete di T06 deve proporre (nome sintetico).</summary>
    public const string NomeAcquedotto = "ACQUEDOTTO TESTBOOK S.P.A.";

    public const string EnteAcquedotto = "APPIO-TB-ENTE-1";
    public const string ContrattoAcquedotto = "APPIO-TB1";
    public const string PecAcquedotto = "acquedotto@pec.example.test";

    public const string NomeBanca = "Banca Testbook AppIO";
    public const string EnteBanca = "APPIO-TB-ENTE-2";
    public const string ContrattoBanca = "APPIO-TB2";

    /// <summary>Contratti del seed (APPIO-OB1…OB5) più i due del testbook.</summary>
    public const int ContrattiTotali = 7;

    private const string Pulizia = "DELETE FROM appio.contracts WHERE onboardingtokenid LIKE 'APPIO-TB%';";

    private const string Inserimento = $"""
        INSERT INTO appio.contracts
         (id, internalistitutionid, product, [state], onboardingtokenid, pricingplan, institution, createdat, updatedat,
          testInstitution, institution_taxcode, institution_description, institution_institutiontype, billing_vatnumber) VALUES
         ('APPIO-TBK1', '{EnteAcquedotto}', 'prod-io', 'ACTIVE', '{ContrattoAcquedotto}', 'FA', N'{"{"}"digitaladdress":"{PecAcquedotto}"{"}"}',
          '2025-06-10T09:00:00', '2026-03-06T10:00:00', 0, '00000000091', '{NomeAcquedotto}', 'GSP', '00000000091'),
         ('APPIO-TBK2', '{EnteBanca}', 'prod-io', 'ACTIVE', '{ContrattoBanca}', 'FA', N'{"{"}"digitaladdress":"banca@pec.example.test"{"}"}',
          '2025-07-10T09:00:00', '2026-03-07T10:00:00', 0, '00000000092', '{NomeBanca}', 'PSP', '00000000092');
        """;

    /// <summary>Inserisce i due contratti (dopo aver tolto eventuali residui di un'esecuzione interrotta).</summary>
    public static void Inserisci() => Esegui(Pulizia + Inserimento);

    /// <summary>Toglie i due contratti.</summary>
    public static void Rimuovi() => Esegui(Pulizia);

    private static void Esegui(string sql)
    {
        // Senza DB i test vengono saltati dal loro [SetUp]: qui non c'è nulla da preparare
        try
        {
            using var conn = new SqlConnection(LocalTestDb.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
        catch (SqlException ex) when (ex.Number is not 208)
        {
            TestContext.Out.WriteLine($"Dati del testbook APP IO non preparati: {ex.Message}");
        }
    }
}
