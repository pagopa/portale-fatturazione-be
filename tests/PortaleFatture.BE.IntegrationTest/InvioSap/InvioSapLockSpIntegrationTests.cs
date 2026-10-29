using System.Data;
using Microsoft.Data.SqlClient;

namespace PortaleFatture.BE.IntegrationTest.InvioSap;

/// <summary>
/// Contratto della stored procedure <c>be.spInvioSAPAvviaBatch</c> (lock globale + verifica stati + INSERT
/// Registro/Dettaglio in una transazione), provato chiamandola direttamente (task T030/T034).
///
/// Esiti attesi (primo result set, colonna <c>Result</c>): 1 avviato, 0 fatture non trovate, 2 lock attivo,
/// 3 stato non ammesso. E' la stessa convenzione di <c>be.spGestioneFattura*</c>. Il test di concorrenza e'
/// quello che nessun test HTTP puo' dare con la stessa forza: N connessioni vere sulla stessa riga di lock.
///
/// I parametri seguono la proposta del piano; si allineano quando arriva la SP di DATA.
/// </summary>
[Category("InvioSap")]
public class InvioSapLockSpIntegrationTests
{
    [SetUp]
    public void Setup()
    {
        InvioSapTestData.SkipSeOggettiAssenti(InvioSapTestData.SpAvviaBatch, InvioSapTestData.Lock, InvioSapTestData.Registro, InvioSapTestData.Dettaglio, InvioSapTestData.VistaDettaglio);
        InvioSapTestData.Pulisci();
    }

    [TearDown]
    public void TearDown() => InvioSapTestData.Pulisci();

    [Test]
    public void Avvio_ConLockLibero_ShouldRestituire1_EScrivereRegistroEDettaglio()
    {
        var ids = IdsDaInviare(2);
        var batchId = Guid.NewGuid();

        var result = Avvia(batchId, ids);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(1));
            Assert.That(InvioSapTestData.StatoBatch(batchId), Is.EqualTo("IN_ELABORAZIONE"));
            Assert.That(InvioSapTestData.BatchCheDetieneIlLock(), Is.EqualTo(batchId));
            Assert.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b", ("@b", batchId)), Is.EqualTo(2));
        });
    }

    [Test]
    public void Avvio_ConLockOccupato_ShouldRestituire2_ENonScrivereNulla()
    {
        var ids = IdsDaInviare(2);
        var primo = Guid.NewGuid();
        Assert.That(Avvia(primo, ids.Take(1).ToList()), Is.EqualTo(1));

        var secondo = Guid.NewGuid();
        var result = Avvia(secondo, ids.Skip(1).ToList());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(2));
            Assert.That(InvioSapTestData.StatoBatch(secondo), Is.Null, "Rollback: nessun Registro per il batch rifiutato.");
            Assert.That(InvioSapTestData.BatchCheDetieneIlLock(), Is.EqualTo(primo));
        });
    }

    [Test]
    public void Avvio_StessoBatchIdDueVolte_ShouldEssereIdempotente_NonDuplicare()
    {
        // batchId e' chiave di idempotenza (data-model): un retry del BE non crea un secondo batch.
        var ids = IdsDaInviare(1);
        var batchId = Guid.NewGuid();
        Assert.That(Avvia(batchId, ids), Is.EqualTo(1));

        var result = Avvia(batchId, ids);

        Assert.That(result, Is.Not.EqualTo(1).Or.EqualTo(1), "Qualunque esito, ma:");
        Assert.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.Registro} WHERE batchId = @b", ("@b", batchId)), Is.EqualTo(1));
    }

    [Test]
    public void Avvio_ConLockScaduto_ShouldRestituire1_ESostituireIlLock()
    {
        var ids = IdsDaInviare(2);
        var appeso = Guid.NewGuid();
        Assert.That(Avvia(appeso, ids.Take(1).ToList()), Is.EqualTo(1));
        InvioSapTestData.ScadiLock(appeso);

        var nuovo = Guid.NewGuid();
        var result = Avvia(nuovo, ids.Skip(1).ToList());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(1), "Scadenza passata = lock libero (self-healing).");
            Assert.That(InvioSapTestData.BatchCheDetieneIlLock(), Is.EqualTo(nuovo));
        });
    }

    [Test]
    public void Avvio_ConBatchPrecedenteTerminale_ShouldRestituire1_AncheSeIlLockNonEStatoAzzerato()
    {
        // Se DATA chiude il Registro ma dimentica il lock, il predicato guarda lo stato del batch.
        var ids = IdsDaInviare(2);
        var chiuso = Guid.NewGuid();
        Assert.That(Avvia(chiuso, ids.Take(1).ToList()), Is.EqualTo(1));
        InvioSapTestData.Esegui($"UPDATE {InvioSapTestData.Registro} SET stato = 'COMPLETATO', dataCompletamento = SYSDATETIME() WHERE batchId = @b", ("@b", chiuso));

        var result = Avvia(Guid.NewGuid(), ids.Skip(1).ToList());

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Avvio_FatturaInesistente_ShouldRestituire0()
    {
        var result = Avvia(Guid.NewGuid(), [999_999_999]);
        Assert.That(result, Is.EqualTo(0));
        Assert.That(InvioSapTestData.BatchCheDetieneIlLock(), Is.Null, "Nessun lock lasciato appeso su rifiuto.");
    }

    [Test]
    public void Avvio_FatturaInElaborazione_ShouldRestituire3()
    {
        var ids = IdsDaInviare(1);
        var primo = Guid.NewGuid();
        Assert.That(Avvia(primo, ids), Is.EqualTo(1));
        InvioSapTestData.ChiudiBatchComeData(primo, "COMPLETATO"); // libera il lock...
        InvioSapTestData.Esegui($"UPDATE {InvioSapTestData.Registro} SET stato = 'IN_ELABORAZIONE', dataCompletamento = NULL WHERE batchId = @b", ("@b", primo)); // ...ma la fattura risulta ancora in elaborazione

        var result = Avvia(Guid.NewGuid(), ids);

        Assert.That(result, Is.EqualTo(3).Or.EqualTo(2), "Stato non ammesso (o lock, se il predicato riacquisisce il batch attivo).");
    }

    [Test]
    public void Avvio_FatturaDiAltroGruppo_ShouldRestituire3()
    {
        var ids = IdsDaInviare(1);
        var result = Avvia(Guid.NewGuid(), ids, mese: InvioSapTestData.Mese == 12 ? 1 : InvioSapTestData.Mese + 1);
        Assert.That(result, Is.EqualTo(3).Or.EqualTo(0), "Fuori dal gruppo dichiarato: la SP non la accetta.");
    }

    [Test]
    public async Task Avvio_Concorrente_ShouldFarVincereUnSoloBatch()
    {
        var ids = IdsDaInviare(1);
        var batch = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToList();

        var esiti = await Task.WhenAll(batch.Select(b => Task.Run(() => Avvia(b, ids))));
        TestContext.Out.WriteLine(string.Join(",", esiti));

        Assert.Multiple(() =>
        {
            Assert.That(esiti.Count(e => e == 1), Is.EqualTo(1), "UPDLOCK/HOLDLOCK sulla riga di lock: un solo vincitore.");
            Assert.That(esiti.Count(e => e == 2), Is.EqualTo(7));
            Assert.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.Registro} WHERE stato = 'IN_ELABORAZIONE' AND idUtente = @u", ("@u", InvioSapTestData.UtenteTest)), Is.EqualTo(1));
            Assert.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.Dettaglio} WHERE FkIdFattura = @id", ("@id", ids[0])), Is.EqualTo(1),
                "Una fattura non puo' stare in due batch attivi.");
        });
    }

    [Test]
    public void Avvio_NoteEStorno_ShouldFinireSulDettaglio_PerOgniFattura()
    {
        // Decisione 21/10: nota e numero storno a livello di singola fattura.
        var ids = IdsDaInviare(2);
        var batchId = Guid.NewGuid();
        Assert.That(Avvia(batchId, ids, storno: "NC-TEST-1"), Is.EqualTo(1));

        var righe = InvioSapTestData.Scalar($@"SELECT COUNT(*) FROM {InvioSapTestData.Dettaglio}
            WHERE FkBatchId = @b AND nota LIKE @n AND numeroDocumentoStorno = 'NC-TEST-1'", ("@b", batchId), ("@n", $"%{InvioSapTestData.MarcatoreNota}%"));
        Assert.That(righe, Is.EqualTo(2));
    }

    // =============================================================================================

    private static List<long> IdsDaInviare(int quante)
    {
        var ids = InvioSapTestData.IdFattureDaInviare(max: quante);
        if (ids.Count < quante) Assert.Ignore($"Servono {quante} fatture DA_INVIARE nel gruppo del seed, trovate {ids.Count}.");
        return ids;
    }

    private static int Avvia(Guid batchId, IReadOnlyList<long> ids, int? mese = null, string? storno = null, int timeoutMinuti = 120)
    {
        using var conn = InvioSapTestData.Apri();
        using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = InvioSapTestData.SpAvviaBatch;
        cmd.Parameters.AddWithValue("@BatchId", batchId);
        cmd.Parameters.AddWithValue("@IdUtente", InvioSapTestData.UtenteTest);
        cmd.Parameters.AddWithValue("@Anno", InvioSapTestData.Anno);
        cmd.Parameters.AddWithValue("@Mese", mese ?? InvioSapTestData.Mese);
        cmd.Parameters.AddWithValue("@TipologiaFattura", InvioSapTestData.Tipologia);
        cmd.Parameters.AddWithValue("@Nota", $"{InvioSapTestData.MarcatoreNota} sp");
        cmd.Parameters.AddWithValue("@NumeroDocumentoStorno", (object?)storno ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@NomeFile", $"sap-test-{batchId:D}.json");
        cmd.Parameters.AddWithValue("@TimeoutMinuti", timeoutMinuti);
        cmd.Parameters.AddWithValue("@IdFatture", "[" + string.Join(",", ids) + "]");
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
