using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Verifica che gli endpoint GRIGLIA (POST api/fatture/pagopa/gestione-fatture) e DOWNLOAD
/// (POST api/fatture/pagopa/gestione-fatture/download) restituiscano la colonna IdFattura,
/// introdotta a DB il 2026-07-28 (FkIdFattura BIGINT nella tabella + colonna nelle viste
/// vwGestioneFattureGriglia / vwGestioneFattureDownload; DTO SimpleGestioneFattureDto.IdFattura long?).
///
/// Due ambienti:
///  - CONTAINER seeded (deterministico): semina una riga con IdFattura NOTO e OLTRE int.MaxValue,
///    quindi verifica il valore esatto -> blinda il mapping bigint->long? end-to-end su entrambe le viste.
///  - SMOKE su tutto il seed (prima giravano su UAT, spostati sul container il 06/11/2026): verifica
///    che la query giri e IdFattura sia leggibile su ogni riga, cioe' che nessuna delle due viste dia
///    "Invalid column name". Le viste del seed sono la DDL di DATA (tests/Data/views).
/// </summary>
[Category("GestioneFatture")]
public class GestioneFattureIdFatturaIntegrationTests
{
    private const string Ente = "44444444-4444-4444-4444-444444444444"; // ente con Enti+Contratti nel seed
    private const int Anno = 2099;                                       // periodo riservato a questi test
    private const long IdFatturaAtteso = 7770001234567L;                 // > int.MaxValue: verifica il bigint

    private IMediator _container;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _container = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
        Pulisci();
        Semina();
    }

    [TearDown]
    public void TearDown() => Pulisci();

    private static AuthenticationInfo AdminAuth() => new()
    { IdEnte = Guid.NewGuid().ToString(), Prodotto = "prod-pn", Ruolo = Ruolo.ADMIN, IdTipoContratto = 1 };

    // ---------- CONTAINER: valore esatto (bigint end-to-end) ----------

    [Test]
    public async Task Griglia_Container_ShouldReturn_IdFattura_Bigint()
    {
        var res = await _container.Send(new GestioneFattureQuery(AdminAuth()) { Anno = Anno });
        var riga = res!.GestioneFatture!.FirstOrDefault(r => r.Ente == Ente);

        Assert.That(riga, Is.Not.Null, "La riga seminata deve comparire in griglia.");
        Assert.That(riga!.IdFattura, Is.EqualTo(IdFatturaAtteso),
            "La griglia deve restituire IdFattura, valorizzato correttamente anche oltre int.MaxValue (bigint->long?).");
    }

    [Test]
    public async Task Download_Container_ShouldReturn_IdFattura_Bigint()
    {
        var res = await _container.Send(new GestioneFattureDownloadQuery(AdminAuth()) { Anno = Anno });
        var riga = res!.GestioneFatture!.FirstOrDefault(r => r.Ente == Ente);

        Assert.That(riga, Is.Not.Null, "La riga seminata deve comparire nel download.");
        Assert.That(riga!.IdFattura, Is.EqualTo(IdFatturaAtteso),
            "Il download deve restituire IdFattura (colonna vista + SimpleGestioneFattureDto.IdFattura).");
    }

    [Test]
    public async Task Download_FatturaSenzaNote_CompareComunque_PerOuterApply()
    {
        // Regressione (finding 2026-07-28): la vwGestioneFattureDownload usa OUTER APPLY OPENJSON(Note).
        // Una fattura con Note '[]' (nessuna nota, il DEFAULT della colonna) deve COMPARIRE nel download,
        // con la colonna Note vuota. Con CROSS APPLY sparirebbe (OPENJSON su array vuoto -> 0 righe ->
        // INNER apply la esclude). Se questo test diventa rosso, qualcuno ha rimesso CROSS APPLY.
        const int mese = 2;
        Exec(@"
            INSERT INTO cfg.GestioneFatture
                (FkIdEnte, FkTipologiaFattura, Anno, Mese, DataInserimento, IdUtenteInserimento, Stato, Azione, FkIdFattura, Note)
            VALUES (@e, 'SECONDO SALDO', @a, @m, GETDATE(), 'itest', 0, 'POSTICIPATA', 888, N'[]')",
            ("@e", Ente), ("@a", Anno), ("@m", mese));

        var res = await _container.Send(new GestioneFattureDownloadQuery(AdminAuth()) { Anno = Anno });
        var riga = res!.GestioneFatture!.FirstOrDefault(r => r.Ente == Ente && r.Mese == mese);

        Assert.That(riga, Is.Not.Null,
            "Una fattura senza note deve comparire nel download grazie a OUTER APPLY (con CROSS APPLY sparirebbe).");
        Assert.That(riga!.IdFattura, Is.EqualTo(888L));
        // Nessuna nota reale. NB: la vista produce " " (uno spazio), non stringa vuota, perche'
        // con OUTER APPLY j.Data/j.Testo sono NULL e CONCAT(NULL,' ',NULL) da' " ", aggregato da
        // STRING_AGG. Artefatto cosmetico del download (spazio al posto di vuoto), da segnalare.
        Assert.That(string.IsNullOrWhiteSpace(riga.Note), Is.True,
            "Nessuna nota -> colonna Note vuota o solo spazi, non un errore ne' testo spurio.");
    }

    // ---------- SEED: smoke su tutte le righe, senza filtro di anno ----------
    // Erano smoke su UAT (spostati sul container il 06/11/2026). Senza Anno la query legge tutto il seed,
    // riga del Setup compresa: se una delle due viste perdesse la colonna, fallirebbe qui.

    [Test]
    public async Task Griglia_Seed_EspongaIdFattura_SenzaErrori()
    {
        var res = await _container.Send(new GestioneFattureQuery(AdminAuth()));

        Assert.That(res?.GestioneFatture, Is.Not.Null.And.Not.Empty, "Senza filtro la griglia deve leggere il seed.");
        Assert.That(() => res!.GestioneFatture!.Select(r => r.IdFattura).ToList(), Throws.Nothing,
            "La griglia deve esporre IdFattura su ogni riga del seed senza errori.");
    }

    [Test]
    public async Task Download_Seed_EspongaIdFattura_SenzaErrori()
    {
        var res = await _container.Send(new GestioneFattureDownloadQuery(AdminAuth()));

        Assert.That(res?.GestioneFatture, Is.Not.Null.And.Not.Empty, "Senza filtro il download deve leggere il seed.");
        Assert.That(() => res!.GestioneFatture!.Select(r => r.IdFattura).ToList(), Throws.Nothing,
            "Il download deve esporre IdFattura su ogni riga del seed senza errori.");
    }

    // ---------- infra ----------

    private void Semina() => Exec(@"
        INSERT INTO cfg.GestioneFatture
            (FkIdEnte, FkTipologiaFattura, Anno, Mese, DataInserimento, IdUtenteInserimento, Stato, Azione, FkIdFattura, Note)
        VALUES (@e, 'PRIMO SALDO', @a, 1, GETDATE(), 'itest', 0, 'POSTICIPATA', @f,
                N'[{""Data"":""2099-01-01T00:00:00"",""Testo"":""idfattura test""}]')",
        ("@e", Ente), ("@a", Anno), ("@f", IdFatturaAtteso));

    private void Pulisci() =>
        Exec("DELETE FROM cfg.GestioneFatture WHERE FkIdEnte=@e AND Anno=@a", ("@e", Ente), ("@a", Anno));

    private void Exec(string sql, params (string, object)[] ps)
    {
        try
        {
            using var conn = new SqlConnection(LocalTestDb.ConnectionString); conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            foreach (var p in ps) cmd.Parameters.AddWithValue(p.Item1, p.Item2);
            cmd.ExecuteNonQuery();
        }
        catch (SqlException) { /* best-effort */ }
    }
}
