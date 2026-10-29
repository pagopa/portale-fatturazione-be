using Microsoft.Data.SqlClient;

namespace PortaleFatture.BE.IntegrationTest.InvioSap;

/// <summary>
/// Invarianti delle viste <c>be.vwInvioSAPDettaglio</c> e <c>be.vwInvioSAPGriglia</c>, provate con SQL
/// diretto sul DB seedato (task T012/T037). Sono i contratti che il backend da' per scontati e che il
/// compilatore non puo' verificare: se DATA cambia una vista, qui si vede.
///
/// Gli invarianti fissano anche le **correzioni alla bozza del 27/10** (WHERE hardcoded, LEFT JOIN che
/// moltiplica le righe per batch): finche' non sono applicate, i primi due test sono rossi di proposito.
/// Restano gialli finche' le viste non esistono nel seed.
/// </summary>
[Category("InvioSap")]
public class InvioSapVistaIntegrationTests
{
    [SetUp]
    public void Setup() => InvioSapTestData.SkipSeOggettiAssenti(InvioSapTestData.VistaDettaglio, InvioSapTestData.VistaGriglia);

    [Test]
    public void Dettaglio_ShouldAvereUnaRigaPerFattura()
    {
        // La bozza fa LEFT JOIN su DettaglioInvioSAP: una fattura con due batch compare due volte.
        var duplicati = InvioSapTestData.Scalar($@"SELECT COUNT(*) FROM (
            SELECT IdFattura FROM {InvioSapTestData.VistaDettaglio} GROUP BY IdFattura HAVING COUNT(*) > 1) d");

        Assert.That(duplicati, Is.EqualTo(0), "Serve OUTER APPLY sull'ultimo batch, non un LEFT JOIN.");
    }

    [Test]
    public void Dettaglio_ShouldCoprirePiuPeriodi_NonSoloMaggio2026()
    {
        // La bozza ha `WHERE AnnoRiferimento = 2026 AND MeseRiferimento = 5` cablato.
        var periodi = InvioSapTestData.Scalar($@"SELECT COUNT(DISTINCT CONCAT(AnnoRiferimento, '-', MeseRiferimento))
            FROM {InvioSapTestData.VistaDettaglio}");

        Assert.That(periodi, Is.GreaterThan(1), "Il filtro per periodo lo mette il backend, non la vista.");
    }

    [Test]
    public void Dettaglio_StatoInvio_ShouldEssereSempreUnoDeiTre()
    {
        var fuori = InvioSapTestData.Scalar($@"SELECT COUNT(*) FROM {InvioSapTestData.VistaDettaglio}
            WHERE StatoInvio IS NULL OR StatoInvio NOT IN ('DA_INVIARE','IN_ELABORAZIONE','INVIATA')");
        Assert.That(fuori, Is.EqualTo(0));
    }

    [Test]
    public void Dettaglio_DaInviare_ShouldAvereDataEFileVuoti_EZeroInvii()
    {
        // RF-4: mai processata => colonne vuote.
        var incoerenti = InvioSapTestData.Scalar($@"SELECT COUNT(*) FROM {InvioSapTestData.VistaDettaglio}
            WHERE StatoInvio = 'DA_INVIARE' AND (DataUltimaGenerazione IS NOT NULL OR NomeFile IS NOT NULL OR NumeroInvii > 0 OR Reinviata = 1)");
        Assert.That(incoerenti, Is.EqualTo(0));
    }

    [Test]
    public void Dettaglio_Reinviata_ShouldImplicareInviata_EReinviiMinoriDegliInvii()
    {
        var incoerenti = InvioSapTestData.Scalar($@"SELECT COUNT(*) FROM {InvioSapTestData.VistaDettaglio}
            WHERE (Reinviata = 1 AND (StatoInvio <> 'INVIATA' OR NumeroReinvii = 0))
               OR NumeroReinvii > NumeroInvii
               OR (NumeroReinvii > 0 AND Reinviata = 0)");
        Assert.That(incoerenti, Is.EqualTo(0), "NumeroReinvii esclude il primo invio; Reinviata = NumeroReinvii > 0.");
    }

    [Test]
    public void Dettaglio_InElaborazione_ShouldCorrispondereAUnBatchAttivo_OAFatturaInviataNull()
    {
        var incoerenti = InvioSapTestData.Scalar($@"SELECT COUNT(*) FROM {InvioSapTestData.VistaDettaglio} v
            JOIN pfd.FattureTestata ft ON ft.IdFattura = v.IdFattura
            WHERE v.StatoInvio = 'IN_ELABORAZIONE'
              AND ft.FatturaInviata IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM {InvioSapTestData.Dettaglio} d JOIN {InvioSapTestData.Registro} r ON r.batchId = d.FkBatchId
                              WHERE d.FkIdFattura = v.IdFattura AND r.stato = 'IN_ELABORAZIONE')");
        Assert.That(incoerenti, Is.EqualTo(0));
    }

    [Test]
    public void Dettaglio_InviataLegacy_ShouldRisultareInviata_SenzaBatch()
    {
        // Migrazione: fatture inviate prima di PF-735 (FatturaInviata = 1, nessun batch) => INVIATA, senza
        // inventare storico (nota, batch, storno). RF: "nessuna nota, batch o numero di storno inventati".
        var legacy = InvioSapTestData.Scalar($@"SELECT COUNT(*) FROM {InvioSapTestData.VistaDettaglio} v
            JOIN pfd.FattureTestata ft ON ft.IdFattura = v.IdFattura
            WHERE ft.FatturaInviata = 1 AND v.NumeroInvii = 0");
        if (Convert.ToInt32(legacy) == 0) Assert.Ignore("Il seed non ha fatture inviate pre-PF-735.");

        var sbagliate = InvioSapTestData.Scalar($@"SELECT COUNT(*) FROM {InvioSapTestData.VistaDettaglio} v
            JOIN pfd.FattureTestata ft ON ft.IdFattura = v.IdFattura
            WHERE ft.FatturaInviata = 1 AND v.NumeroInvii = 0
              AND (v.StatoInvio <> 'INVIATA' OR v.NomeFile IS NOT NULL OR v.UltimoNumeroDocumentoStorno IS NOT NULL)");
        Assert.That(sbagliate, Is.EqualTo(0));
    }

    [Test]
    public void Dettaglio_ShouldEscludere_EntePagoPA_PosticipateEdEliminate()
    {
        var pagopa = InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.VistaDettaglio} WHERE FkIdEnte = @e", ("@e", InvioSapTestData.EntePagoPA));
        var staging = InvioSapTestData.Scalar($@"SELECT COUNT(*) FROM {InvioSapTestData.VistaDettaglio} v
            WHERE EXISTS (SELECT 1 FROM cfg.GestioneFatture gf
                          WHERE gf.FkIdEnte = v.FkIdEnte AND gf.FkTipologiaFattura = v.FkTipologiaFattura
                            AND gf.Anno = v.AnnoRiferimento AND gf.Mese = v.MeseRiferimento AND gf.Stato IN (0, 3))");

        Assert.Multiple(() =>
        {
            Assert.That(pagopa, Is.EqualTo(0), "L'ente PagoPA non si fattura a se stesso.");
            Assert.That(staging, Is.EqualTo(0), "Posticipate (0) ed eliminate (3) fuori; ripristinate e cancellate dentro.");
        });
    }

    [Test]
    public void Griglia_ShouldEssereLaSommaDelDettaglio()
    {
        // Un solo predicato per griglia e dettaglio (chiude il finding §11 per le nuove rotte).
        var differenze = InvioSapTestData.Scalar($@"
            WITH d AS (
                SELECT FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, StatoInvio,
                       COUNT(*) AS n, SUM(TotaleFattura) AS imp, SUM(CASE WHEN Reinviata = 1 THEN 1 ELSE 0 END) AS rein
                FROM {InvioSapTestData.VistaDettaglio}
                GROUP BY FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, StatoInvio)
            SELECT COUNT(*) FROM d
            FULL OUTER JOIN {InvioSapTestData.VistaGriglia} g
              ON g.TipologiaFattura = d.FkTipologiaFattura AND g.AnnoRiferimento = d.AnnoRiferimento
             AND g.MeseRiferimento = d.MeseRiferimento AND g.StatoInvio = d.StatoInvio
            WHERE g.TipologiaFattura IS NULL OR d.FkTipologiaFattura IS NULL
               OR g.NumeroFatture <> d.n OR ABS(g.ImportoTotale - d.imp) > 0.005 OR g.NumeroReinviate <> d.rein");

        Assert.That(differenze, Is.EqualTo(0), "Griglia e dettaglio devono raccontare la stessa cosa.");
    }

    [Test]
    public void Dettaglio_Note_ShouldEssereJsonValido_EOrdinatoPerData()
    {
        using var conn = InvioSapTestData.Apri();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT TOP 20 Note FROM {InvioSapTestData.VistaDettaglio} WHERE NumeroInvii > 0";
        using var reader = cmd.ExecuteReader();
        var letti = 0;
        while (reader.Read())
        {
            letti++;
            var note = reader.IsDBNull(0) ? null : reader.GetString(0);
            Assert.That(note, Is.Not.Null.And.Not.Empty);
            using var doc = System.Text.Json.JsonDocument.Parse(note!);
            var date = doc.RootElement.EnumerateArray().Select(e => e.GetProperty("Data").GetDateTime()).ToList();
            Assert.That(date, Is.Ordered, "Storico in ordine cronologico (N.invio crescente).");
        }
        if (letti == 0) Assert.Ignore("Nessuna fattura con storico nel seed.");
    }
}
