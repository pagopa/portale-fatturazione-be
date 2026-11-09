using Microsoft.Data.SqlClient;

namespace PortaleFatture.BE.UnitTest.Common;

/// <summary>
/// Fotografia e ripristino della configurazione GLOBALE del Modulo Commessa (pfw.CostoNotifiche,
/// pfw.PercentualeAnticipo) per le fixture che chiamano <see cref="CommonFactory.CreateDatiCommessaConfiguration"/>.
///
/// La create scrive configurazione valida per tipo contratto e prodotto, non per ente, e chiude quella
/// precedente: senza ripristino restava attiva per tutti i test successivi dello stesso DB seedato.
/// ModuloCommessaDocumentoDownloadHttpTests leggeva cosi' le descrizioni lasciate qui invece delle
/// proprie (scoperto il 06/11/2026: rosso solo nella suite completa, verde da solo). Le tabelle non hanno
/// una chiave identity, quindi si copiano per intero.
///
/// Uso: <c>Fotografa()</c> in [OneTimeSetUp], <c>Ripristina()</c> in [OneTimeTearDown]. Regge perche' le
/// fixture unit non girano in parallelo (default NUnit).
/// </summary>
public static class ConfigurazioneModuloCommessaSnapshot
{
    private static readonly string[] Tabelle = ["pfw.CostoNotifiche", "pfw.PercentualeAnticipo"];

    private static string Copia(string tabella) => tabella.Replace("pfw.", "pfw.zz_bkp_test_");

    private static void Esegui(string sql)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    public static void Fotografa()
    {
        foreach (var t in Tabelle)
            Esegui($"DROP TABLE IF EXISTS {Copia(t)}; SELECT * INTO {Copia(t)} FROM {t};");
    }

    public static void Ripristina()
    {
        foreach (var t in Tabelle)
            Esegui($"IF OBJECT_ID('{Copia(t)}') IS NOT NULL BEGIN DELETE FROM {t}; INSERT INTO {t} SELECT * FROM {Copia(t)}; DROP TABLE {Copia(t)}; END");
    }
}
