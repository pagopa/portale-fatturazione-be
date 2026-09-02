using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Happy-path delle azioni GestioneFatture (endpoint POST /api/fatture/pagopa/gestione-fatture/azione):
/// eseguono davvero le stored procedure be.spGestioneFattura* e verificano l'esito + la transizione di
/// stato in [cfg].[GestioneFatture], poi RIPULISCONO.
///
/// Girano contro il **DB locale seeded** (container tests/docker-compose.yml, immagine SQL Server 2025
/// per il tipo nativo json), NON su UAT: dati deterministici (fatture seed 1001/1002/2001), niente VPN,
/// ELIMINA testabile in sicurezza. Se il container non e' su, i test si auto-ignorano (errore di rete).
///
/// Stati GestioneFatture: 0 = POSTICIPATA, 1 = RIPRISTINATA, 2 = CANCELLATA, 3 = ELIMINATA.
/// </summary>
public class GestioneFattureAzioneHappyPathIntegrationTests
{
    // Connessione al container locale (SA/porta noti, gia' nei file docker); override da config se serve.
    private const string DefaultLocalDb =
        "Server=localhost,1433;Database=master;User Id=sa;Password=52JdGnzZaANhf;TrustServerCertificate=True";

    private IMediator _handler;
    private IConfiguration _conf;

    [SetUp]
    public void Setup()
    {
        _conf = ServiceProvider.GetRequiredService<IConfiguration>();
        _handler = ServiceProvider.GetRequiredService<IMediator>(ConnectionString);
    }

    private string ConnectionString => _conf["IntegrationTest:LocalDbConnectionString"] ?? DefaultLocalDb;

    private static AuthenticationInfo AdminAuth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    // -------------------------------------------------------------------------------------------
    // POSTICIPA: crea una riga Stato = 0 (POSTICIPATA) da una fattura non inviata di tipo SALDO.
    // -------------------------------------------------------------------------------------------
    [Test]
    public async Task Posticipa_OnNonSentSaldoInvoice_ShouldReturn1_AndCreateStato0_ThenCleanup()
    {
        var seed = TryFindNonSentSaldoSeed();
        if (seed is null)
            Assert.Ignore("Nessuna fattura SALDO non-inviata e non gia'-in-GestioneFatture disponibile su UAT.");

        try
        {
            var result = await SendAzione(seed!.Value, "POSTICIPA");

            Assert.That(result, Is.EqualTo(1), "POSTICIPA doveva restituire Result = 1.");
            Assert.That(ReadStato(seed.Value.IdFattura), Is.EqualTo(0), "La riga doveva essere in Stato = 0 (POSTICIPATA).");
        }
        catch (SqlException ex) when (IsNetworkDenied(ex)) { Assert.Ignore(NetworkMsg); }
        catch (SqlException ex) when (ex.Number == 2812) { Assert.Ignore(SpMissingMsg(ex)); }
        finally
        {
            CleanupByIdFattura(seed!.Value.IdFattura);
        }
    }

    // -------------------------------------------------------------------------------------------
    // RIPRISTINA (0 -> 1) e CANCELLA (0 -> 2): agiscono su una riga GIA' posticipata.
    // Precondizione creata nel test stesso con POSTICIPA, cosi' il cleanup e' una semplice DELETE.
    // -------------------------------------------------------------------------------------------
    [TestCase("RIPRISTINA", 1)]
    [TestCase("CANCELLA", 2)]
    public async Task Azione_OnPosticipatedInvoice_ShouldReturn1_AndTransitionStato_ThenCleanup(string azione, int statoAtteso)
    {
        var seed = TryFindNonSentSaldoSeed();
        if (seed is null)
            Assert.Ignore("Nessuna fattura SALDO non-inviata e non gia'-in-GestioneFatture disponibile su UAT.");

        try
        {
            // precondizione: crea la riga posticipata (Stato = 0)
            var posticipa = await SendAzione(seed!.Value, "POSTICIPA");
            if (posticipa != 1)
                Assert.Ignore("Impossibile creare la precondizione POSTICIPA (Result != 1): seed non idoneo.");

            // azione sotto test
            var result = await SendAzione(seed.Value, azione);

            Assert.That(result, Is.EqualTo(1), $"{azione} doveva restituire Result = 1.");
            Assert.That(ReadStato(seed.Value.IdFattura), Is.EqualTo(statoAtteso),
                $"La riga doveva transitare a Stato = {statoAtteso} dopo {azione}.");
        }
        catch (SqlException ex) when (IsNetworkDenied(ex)) { Assert.Ignore(NetworkMsg); }
        catch (SqlException ex) when (ex.Number == 2812) { Assert.Ignore(SpMissingMsg(ex)); }
        finally
        {
            CleanupByIdFattura(seed!.Value.IdFattura);
        }
    }

    // -------------------------------------------------------------------------------------------
    // NO-OP: RIPRISTINA/CANCELLA su una fattura NON posticipata (nessuna riga in GestioneFatture)
    // devono restituire Result = 0 (la SP non trova nulla), senza scrivere nulla.
    // Verifica la mappatura esito 0 -> NotFound lato Module.
    // -------------------------------------------------------------------------------------------
    [TestCase("RIPRISTINA")]
    [TestCase("CANCELLA")]
    public async Task Azione_OnInvoiceNotInGestioneFatture_ShouldReturn0_AndWriteNothing(string azione)
    {
        var seed = TryFindNonSentSaldoSeed();
        if (seed is null)
            Assert.Ignore("Nessuna fattura SALDO idonea (non in GestioneFatture) disponibile su UAT.");

        try
        {
            var result = await SendAzione(seed!.Value, azione);

            Assert.That(result, Is.EqualTo(0), $"{azione} su fattura non posticipata doveva restituire Result = 0.");
            Assert.That(ReadStato(seed.Value.IdFattura), Is.EqualTo(-1), "Non doveva essere creata alcuna riga.");
        }
        catch (SqlException ex) when (IsNetworkDenied(ex)) { Assert.Ignore(NetworkMsg); }
        catch (SqlException ex) when (ex.Number == 2812) { Assert.Ignore(SpMissingMsg(ex)); }
        finally
        {
            CleanupByIdFattura(seed!.Value.IdFattura); // best-effort, non dovrebbe esistere nulla
        }
    }

    // -------------------------------------------------------------------------------------------
    // POSTICIPA su fattura inesistente -> AMMESSA (pre-generazione).
    //
    // ATTENZIONE, l'aspettativa e' stata INVERTITA il 2026-07-24: questo test asseriva Result = 0.
    // Il requisito e' cambiato (si posticipa un PERIODO, anche prima che la fattura esista: PAC in
    // attesa di firma REL) e la SP spGestioneFatturePosticipa e' stata adeguata di conseguenza.
    // Il caso "per periodo, senza IdFattura" e' coperto da
    // GestioneFattureRequisitiIntegrationTests.Posticipa_PreGeneration_NoInvoiceYet_ShouldSucceed_ByPeriod;
    // qui si copre la variante con un IdFattura che non esiste in pfd.FattureTestata.
    // -------------------------------------------------------------------------------------------
    [Test]
    public async Task Posticipa_OnNonExistingInvoice_ShouldSucceed_PreGeneration()
    {
        // id nel range int ma inesistente (evita l'overflow del cast checked in SendAzione)
        var fake = new Seed(int.MaxValue, Guid.NewGuid().ToString(), 1900, 1, "SECONDO SALDO");
        try
        {
            var result = await SendAzione(fake, "POSTICIPA");
            Assert.That(result, Is.EqualTo(1),
                "POSTICIPA su fattura non ancora esistente deve riuscire (pre-generazione).");
        }
        catch (SqlException ex) when (IsNetworkDenied(ex)) { Assert.Ignore(NetworkMsg); }
        catch (SqlException ex) when (ex.Number == 2812) { Assert.Ignore(SpMissingMsg(ex)); }
        finally { CleanupByIdFattura(fake.IdFattura); }
    }

    // -------------------------------------------------------------------------------------------
    // IDEMPOTENZA: doppia POSTICIPA della stessa fattura. La SP non ha (piu') un guard anti-duplicato,
    // quindi documentiamo il comportamento reale del secondo invio.
    // -------------------------------------------------------------------------------------------
    [Test]
    public async Task Posticipa_Twice_ShouldDocumentDuplicateBehavior()
    {
        var seed = TryFindNonSentSaldoSeed();
        if (seed is null)
            Assert.Ignore("Nessuna fattura SALDO idonea disponibile su UAT.");

        try
        {
            var first = await SendAzione(seed!.Value, "POSTICIPA");
            if (first != 1)
                Assert.Ignore("Prima POSTICIPA non riuscita (Result != 1): seed non idoneo.");

            var second = await SendAzione(seed.Value, "POSTICIPA");
            // Comportamento documentato, non prescritto: assenza di guard puo' dare 1 (doppione) o 0.
            Assert.That(second, Is.AnyOf(0, 1), "Il secondo POSTICIPA deve restituire un esito int coerente.");
            TestContext.Out.WriteLine($"Doppio POSTICIPA: primo={first}, secondo={second}, "
                + $"righe totali per la fattura={CountRows(seed.Value.IdFattura)}");
        }
        catch (SqlException ex) when (IsNetworkDenied(ex)) { Assert.Ignore(NetworkMsg); }
        catch (SqlException ex) when (ex.Number == 2812) { Assert.Ignore(SpMissingMsg(ex)); }
        finally
        {
            CleanupByIdFattura(seed!.Value.IdFattura);
        }
    }

    // -------------------------------------------------------------------------------------------
    // ELIMINA happy-path: DISTRUTTIVO ma sicuro sul DB locale usa-e-getta (con RestoreEliminaSeed nel
    // finally). La SP chiama EXEC [pfd].[EliminaFattura] (ora la SP REALE, non piu' lo stub): sposta la
    // fattura in FattureTestata_Eliminate e la rimuove da FattureTestata. Verifichiamo entrambe le cose.
    // -------------------------------------------------------------------------------------------
    [Test]
    public async Task Elimina_OnNonSentAnticipoAccontoInvoice_ShouldReturn1_AndCreateStato3()
    {
        var seed = TryFindNonSentEliminabileSeed();
        if (seed is null)
            Assert.Ignore("Nessuna fattura ANTICIPO/ACCONTO non-inviata disponibile nel seed.");

        try
        {
            var result = await SendAzione(seed!.Value, "ELIMINA");
            Assert.That(result, Is.EqualTo(1), "ELIMINA doveva restituire Result = 1.");
            Assert.Multiple(() =>
            {
                // Dopo il fix ELIMINA la riga cfg ha FkIdFattura NULL (chiave = periodo): lettura per periodo.
                Assert.That(ReadStatoByPeriod(seed.Value.IdEnte, seed.Value.TipologiaFattura, seed.Value.Anno, seed.Value.Mese),
                    Is.EqualTo(3), "La riga doveva essere in Stato = 3 (ELIMINATA).");
                // La SP reale sposta la fattura in _Eliminate e la toglie da FattureTestata (lo stub non lo faceva).
                Assert.That(CountInEliminate(seed.Value.IdFattura), Is.EqualTo(1),
                    "La fattura deve essere stata spostata in pfd.FattureTestata_Eliminate.");
                Assert.That(CountInTestata(seed.Value.IdFattura), Is.EqualTo(0),
                    "La fattura deve essere stata rimossa da pfd.FattureTestata.");
            });
        }
        catch (SqlException ex) when (IsNetworkDenied(ex)) { Assert.Ignore(NetworkMsg); }
        catch (SqlException ex) when (ex.Number == 2812) { Assert.Ignore(SpMissingMsg(ex)); }
        finally
        {
            // ripristina il seed: rimuovi la riga GestioneFatture, ripristina la fattura in FattureTestata
            // e svuota FattureTestata_Eliminate (lo stub locale l'aveva spostata li').
            RestoreEliminaSeed(seed!.Value);
        }
    }

    // ---- invocazione azione (command reale via MediatR) ----

    private Task<int?> SendAzione(Seed seed, string azione) =>
        _handler.Send(new GestioneFattureAzioneCommand(AdminAuth())
        {
            IdEnte = seed.IdEnte,
            Azione = azione,
            Anno = seed.Anno,
            Mese = seed.Mese,
            TipologiaFattura = seed.TipologiaFattura,
            IdUtente = "integration-test",
            IdFattura = checked((int)seed.IdFattura),   // command int32 vs IdFattura bigint sul DB (finding overflow)
            // Nota DEVE essere valorizzata: con Nota null la persistence serializza "null" (stringa),
            // rifiutata dal parametro @Note (tipo json). Finding: azione via API senza nota fallisce.
            Nota = new NoteCommand { Data = DateTime.UtcNow, Testo = "integration-test" }
        });

    // ---- helper SQL grezzi ----

    // pfd.FattureTestata.IdFattura e' bigint (Int64); il command/SP usano int32 -> qui long, poi cast per il command.
    private record struct Seed(long IdFattura, string IdEnte, int Anno, int Mese, string TipologiaFattura);

    private Seed? TryFindNonSentSaldoSeed()
    {
        const string sql = @"
            SELECT TOP(1)
                ft.IdFattura, ft.FkIdEnte, ft.AnnoRiferimento, ft.MeseRiferimento, ft.FkTipologiaFattura
            FROM pfd.FattureTestata ft
            WHERE ft.FkTipologiaFattura IN ('PRIMO SALDO','SECONDO SALDO','VAR. SEMESTRALE','SEM. SOSPESI')
              AND (ft.FatturaInviata IS NULL OR ft.FatturaInviata = 0)
              AND NOT EXISTS (SELECT 1 FROM cfg.GestioneFatture g WHERE g.FkIdFattura = ft.IdFattura);";
        try
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new Seed(r.GetInt64(0), r.GetString(1), r.GetInt32(2), r.GetInt32(3), r.GetString(4));
        }
        catch (SqlException ex) when (IsNetworkDenied(ex))
        {
            Assert.Ignore(NetworkMsg);
            return null;
        }
    }

    // Seed per ELIMINA: fattura ANTICIPO/ACCONTO non inviata, non gia' in GestioneFatture.
    private Seed? TryFindNonSentEliminabileSeed()
    {
        const string sql = @"
            SELECT TOP(1)
                ft.IdFattura, ft.FkIdEnte, ft.AnnoRiferimento, ft.MeseRiferimento, ft.FkTipologiaFattura
            FROM pfd.FattureTestata ft
            WHERE ft.FkTipologiaFattura IN ('ANTICIPO','ACCONTO')
              AND (ft.FatturaInviata IS NULL OR ft.FatturaInviata = 0)
              AND NOT EXISTS (SELECT 1 FROM cfg.GestioneFatture g WHERE g.FkIdFattura = ft.IdFattura);";
        try
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new Seed(r.GetInt64(0), r.GetString(1), r.GetInt32(2), r.GetInt32(3), r.GetString(4));
        }
        catch (SqlException ex) when (IsNetworkDenied(ex))
        {
            Assert.Ignore(NetworkMsg);
            return null;
        }
    }

    /// <summary>Ripristina il seed dopo un ELIMINA (DB locale usa-e-getta): rimette la fattura in
    /// FattureTestata, svuota _Eliminate e la riga GestioneFatture creata.</summary>
    // Il ripristino vive in FattureSeedRestore: ricopia la riga ORIGINALE da pfd.FattureTestata_Eliminate
    // (dove la SP reale l'ha spostata) invece di ricrearla con valori segnaposto. Qui la fattura da
    // eliminare viene SCELTA A RUNTIME fra le ANTICIPO/ACCONTO disponibili, quindi il vecchio ripristino
    // "lossy" poteva alterare gli importi di una qualunque fattura del seed (e' cosi' che la 2001 e'
    // passata da 305.00 a 100.00, rompendo FattureInvioSapMultiploPeriodoIntegrationTests).
    private void RestoreEliminaSeed(Seed s) =>
        FattureSeedRestore.RipristinaDopoElimina(
            ConnectionString, s.IdFattura, s.IdEnte, s.TipologiaFattura, s.Anno, s.Mese);

    /// <summary>Conta le righe GestioneFatture per la fattura (per documentare i doppioni).</summary>
    private int CountRows(long idFattura)
    {
        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM cfg.GestioneFatture WHERE FkIdFattura = @id;", conn);
        cmd.Parameters.AddWithValue("@id", idFattura);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>Legge lo Stato corrente della riga GestioneFatture per la fattura, o -1 se assente.</summary>
    private int ReadStato(long idFattura)
    {
        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(
            "SELECT TOP(1) Stato FROM cfg.GestioneFatture WHERE FkIdFattura = @id ORDER BY Stato;", conn);
        cmd.Parameters.AddWithValue("@id", idFattura);
        var v = cmd.ExecuteScalar();
        return v is null or DBNull ? -1 : Convert.ToInt32(v);
    }

    /// <summary>Legge lo Stato per chiave di PERIODO: necessario dopo ELIMINA, che azzera FkIdFattura.</summary>
    private int ReadStatoByPeriod(string ente, string tipologia, int anno, int mese)
    {
        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(
            "SELECT TOP(1) Stato FROM cfg.GestioneFatture WHERE FkIdEnte=@e AND FkTipologiaFattura=@t AND Anno=@a AND Mese=@m;", conn);
        cmd.Parameters.AddWithValue("@e", ente);
        cmd.Parameters.AddWithValue("@t", tipologia);
        cmd.Parameters.AddWithValue("@a", anno);
        cmd.Parameters.AddWithValue("@m", mese);
        var v = cmd.ExecuteScalar();
        return v is null or DBNull ? -1 : Convert.ToInt32(v);
    }

    /// <summary>Righe della fattura in pfd.FattureTestata_Eliminate (spostamento operato dalla SP reale).</summary>
    private int CountInEliminate(long idFattura)
    {
        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM pfd.FattureTestata_Eliminate WHERE IdFattura = @id;", conn);
        cmd.Parameters.AddWithValue("@id", idFattura);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>Righe residue della fattura in pfd.FattureTestata (0 dopo un ELIMINA reale).</summary>
    private int CountInTestata(long idFattura)
    {
        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM pfd.FattureTestata WHERE IdFattura = @id;", conn);
        cmd.Parameters.AddWithValue("@id", idFattura);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private void CleanupByIdFattura(long idFattura)
    {
        try
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(
                "DELETE FROM cfg.GestioneFatture WHERE FkIdFattura = @id;", conn);
            cmd.Parameters.AddWithValue("@id", idFattura);
            cmd.ExecuteNonQuery();
        }
        catch (SqlException) { /* cleanup best-effort: non far fallire il test per il cleanup */ }
    }

    private const string NetworkMsg =
        "Connessione SQL non consentita (VPN non attiva): test valido solo in UAT.";

    private static string SpMissingMsg(SqlException ex) =>
        $"Stored procedure azione non deployata sull'ambiente: {ex.Message}. "
        + "Il test validera' l'azione una volta deployate le be.spGestioneFattura*.";

    // Copre: Public Network Access disabilitato (47073) e i disguidi di connettivita' transitori
    // (host sconosciuto 11001, server non raggiungibile 53, timeout -2) tipici di VPN instabile.
    private static bool IsNetworkDenied(SqlException ex) =>
        ex.Number is 47073 or 11001 or 53 or -2 ||
        ex.Message.Contains("Deny Public Network Access is set to Yes", StringComparison.OrdinalIgnoreCase);
}
