using Microsoft.Data.SqlClient;

namespace PortaleFatture.BE.IntegrationTest.InvioSap;

/// <summary>
/// Punto unico di contatto fra i test di PF-735 e il DB seedato: rotte, nomi degli oggetti, query di
/// appoggio e pulizia (task T010 del piano).
///
/// ATTENZIONE — i nomi di tabelle e colonne qui sotto sono quelli **proposti** nel piano del 27/10/2026
/// (<c>pfw.RegistroInvioSAP</c>, <c>pfw.DettaglioInvioSAP</c>, <c>pfw.LockInvioSAP</c>): la DDL e' di
/// proprieta' del team DATA e non e' ancora arrivata. Quando arriva, si allinea **solo questo file** e il
/// seed <c>tests/Data/invio_sap.sql</c>; i test restano gialli (non rossi) finche' gli oggetti mancano,
/// grazie a <see cref="SkipSeOggettiAssenti"/>. Regola di casa: la DDL si chiede, non si deduce.
/// </summary>
public static class InvioSapTestData
{
    // Rotte del contratto (contracts/api.md 14/10/2026 + proposte del piano).
    public const string RottaGriglia = "/api/fatture/invio-sap";
    public const string RottaDettaglio = "/api/fatture/invio-sap/dettaglio";
    public const string RottaDownload = "/api/fatture/invio-sap/download";
    public const string RottaDownloadReinviate = "/api/fatture/invio-sap/download/reinviate";
    public const string RottaGenera = "/api/fatture/invio-sap/genera";
    public const string RottaGenerazioni = "/api/fatture/invio-sap/generazioni";

    // Oggetti DB (proposta, v. nota in testa).
    public const string Registro = "pfw.RegistroInvioSAP";
    public const string Dettaglio = "pfw.DettaglioInvioSAP";
    public const string Lock = "pfw.LockInvioSAP";
    public const string VistaDettaglio = "be.vwInvioSAPDettaglio";
    public const string VistaGriglia = "be.vwInvioSAPGriglia";
    public const string SpAvviaBatch = "be.spInvioSAPAvviaBatch";
    public const string ChiaveLock = "GENERAZIONE_JSON";

    /// <summary>Utente emesso da TestAuthHandler (claim Name): serve a riconoscere e ripulire i batch dei test.</summary>
    public const string UtenteTest = "integration-test-user";

    /// <summary>Marcatore nelle note dei batch creati dai test: la pulizia per nota e' indipendente dall'utente.</summary>
    public const string MarcatoreNota = "[TEST PF-965]";

    /// <summary>Ente PagoPA, escluso da ogni perimetro di invio (stessa costante del counter legacy).</summary>
    public const string EntePagoPA = "4a4149af-172e-4950-9cc8-63ccc9a6d865";

    // Gruppo del seed con fatture da inviare: fattura 1001, ente1, SECONDO SALDO 2026/7 (FatturaInviata = 0).
    public const string Tipologia = "SECONDO SALDO";
    public const int Anno = 2026;
    public const int Mese = 7;

    public static void SkipSeOggettiAssenti(params string[] oggetti) =>
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString,
            oggetti.Length == 0 ? [Registro, Dettaglio, Lock, VistaDettaglio] : oggetti);

    public static SqlConnection Apri()
    {
        var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        return conn;
    }

    public static object? Scalar(string sql, params (string nome, object? valore)[] parametri)
    {
        using var conn = Apri();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (nome, valore) in parametri) cmd.Parameters.AddWithValue(nome, valore ?? DBNull.Value);
        return cmd.ExecuteScalar();
    }

    public static int Esegui(string sql, params (string nome, object? valore)[] parametri)
    {
        using var conn = Apri();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (nome, valore) in parametri) cmd.Parameters.AddWithValue(nome, valore ?? DBNull.Value);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>Id delle fatture DA_INVIARE del gruppo del seed, lette dalla vista (stesso perimetro dell'API).</summary>
    public static List<long> IdFattureDaInviare(string tipologia = Tipologia, int anno = Anno, int mese = Mese, int max = 10)
    {
        using var conn = Apri();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT TOP (@max) IdFattura FROM {VistaDettaglio}
            WHERE FkTipologiaFattura = @t AND AnnoRiferimento = @a AND MeseRiferimento = @m AND StatoInvio = 'DA_INVIARE'
            ORDER BY IdFattura";
        cmd.Parameters.AddWithValue("@max", max);
        cmd.Parameters.AddWithValue("@t", tipologia);
        cmd.Parameters.AddWithValue("@a", anno);
        cmd.Parameters.AddWithValue("@m", mese);
        var ids = new List<long>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) ids.Add(reader.GetInt64(0));
        return ids;
    }

    public static string? StatoBatch(Guid batchId) =>
        Scalar($"SELECT stato FROM {Registro} WHERE batchId = @b", ("@b", batchId)) as string;

    public static string? StatoFattura(long idFattura) =>
        Scalar($"SELECT StatoInvio FROM {VistaDettaglio} WHERE IdFattura = @id", ("@id", idFattura)) as string;

    public static Guid? BatchCheDetieneIlLock() =>
        Scalar($"SELECT batchId FROM {Lock} WHERE chiave = @c", ("@c", ChiaveLock)) as Guid?;

    /// <summary>Simula un batch rimasto appeso: la scadenza del lock e' gia' passata.</summary>
    public static void ScadiLock(Guid batchId) =>
        Esegui($"UPDATE {Lock} SET scadenza = DATEADD(MINUTE, -1, SYSDATETIME()) WHERE batchId = @b", ("@b", batchId));

    /// <summary>Simula la chiusura da parte di DATA (che scrive direttamente le tabelle: decisione 27/10).</summary>
    public static void ChiudiBatchComeData(Guid batchId, string stato = "COMPLETATO", string? errore = null)
    {
        Esegui($@"UPDATE {Registro} SET stato = @s, dataCompletamento = SYSDATETIME(), errore = @e WHERE batchId = @b",
            ("@s", stato), ("@e", errore), ("@b", batchId));
        Esegui($"UPDATE {Lock} SET batchId = NULL WHERE batchId = @b", ("@b", batchId));
    }

    /// <summary>
    /// Rimuove tutto cio' che i test hanno creato: batch dell'utente di test o con il marcatore nella nota,
    /// relative righe di dettaglio e messaggi, e libera il lock se lo detenevano. Best-effort: se gli
    /// oggetti non esistono ancora, non e' un errore.
    /// </summary>
    public static void Pulisci()
    {
        try
        {
            Esegui($@"
                DECLARE @b TABLE (batchId UNIQUEIDENTIFIER);
                INSERT INTO @b SELECT r.batchId FROM {Registro} r
                    WHERE r.idUtente = @u
                       OR EXISTS (SELECT 1 FROM {Dettaglio} d WHERE d.FkBatchId = r.batchId AND d.nota LIKE @marcatore);
                UPDATE {Lock} SET batchId = NULL WHERE batchId IN (SELECT batchId FROM @b);
                DELETE FROM pfd.Messaggi WHERE Hash IN (SELECT CONVERT(NVARCHAR(64), batchId) FROM @b);
                DELETE FROM {Dettaglio} WHERE FkBatchId IN (SELECT batchId FROM @b);
                DELETE FROM {Registro} WHERE batchId IN (SELECT batchId FROM @b);",
                ("@u", UtenteTest), ("@marcatore", $"%{MarcatoreNota}%"));
        }
        catch (SqlException) { /* oggetti assenti o DDL diversa: i test sono comunque gialli */ }
    }
}
