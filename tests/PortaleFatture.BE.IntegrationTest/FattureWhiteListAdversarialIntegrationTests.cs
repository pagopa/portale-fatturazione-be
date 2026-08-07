using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Test ADVERSARIAL sulla WhiteList di fatturazione, sul modello di
/// GestioneFattureAdversarialIntegrationTests: input ostili, valori estremi, injection, concorrenza.
/// Complementano FattureWhiteListCommandIntegrationTests, che copre i rifiuti *attesi*.
///
/// Perché su quest'area conta: una riga in whitelist **esclude un ente da un ciclo di fatturazione**.
/// Una riga spuria, un mese fuori scala o un doppione non producono un errore — producono una fattura
/// che non parte, o che parte e non doveva.
///
/// Sandbox: Anno 2099, cleanup per anno.
/// </summary>
public class FattureWhiteListAdversarialIntegrationTests
{
    private const int AnnoSandbox = 2099;
    private const string Ente = "11111111-1111-1111-1111-111111111111";
    private const string Tipologia = "PRIMO SALDO";

    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
        Pulisci();
    }

    [TearDown]
    public void TearDown() => Pulisci();

    // ---------------------------------------------------------------------------------------------
    // Valori fuori scala
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// `Mese` è un `int` senza vincoli: nulla impedisce 0, 13 o un negativo. Una riga così non
    /// corrisponde a nessun periodo reale, quindi non escluderà mai niente — ma resta a DB e compare
    /// nella griglia dell'amministratore, che la leggerà come un'esclusione attiva.
    /// </summary>
    [TestCase(0, TestName = "Mese · zero")]
    [TestCase(13, TestName = "Mese · tredici")]
    [TestCase(-1, TestName = "Mese · negativo")]
    [TestCase(int.MaxValue, TestName = "Mese · int.MaxValue")]
    public async Task MesiFuoriScala_Caratterizzazione(int mese)
    {
        bool? esito = null;
        try { esito = await _handler.Send(Aggiungi([mese])); }
        catch (Exception ex) { TestContext.Out.WriteLine($"mese {mese}: {ex.GetType().Name}"); }

        var righe = MesiSandbox();
        TestContext.Out.WriteLine($"mese {mese} -> esito {esito}, righe {righe.Count}");

        // L'invariante che conta non è il rifiuto (oggi non c'è) ma la COERENZA fra esito e stato:
        // se dice di aver inserito, la riga dev'esserci; se non l'ha inserita, non deve dire true.
        if (esito == true)
            Assert.That(righe, Is.EquivalentTo(new[] { mese }),
                "Esito positivo ma stato diverso da quello dichiarato.");
        else
            Assert.That(righe, Is.Empty, "Esito non positivo: non deve restare nulla a DB.");
    }

    [Test]
    public async Task AnnoFuoriScala_ShouldNonCorrompereNulla()
    {
        try { await _handler.Send(Aggiungi([1], anno: int.MaxValue)); }
        catch (Exception ex) { TestContext.Out.WriteLine($"eccezione: {ex.GetType().Name}"); }

        Assert.That(Scalare("SELECT COUNT(*) FROM sys.tables WHERE name = 'FattureWhiteList'"), Is.EqualTo(1),
            "La tabella deve esistere ancora.");
    }

    [Test]
    public async Task MoltiMesiInUnaSolaRichiesta_ShouldEssereAtomico()
    {
        // 200 mesi: ben oltre i 12 sensati. Serve a verificare che l'esito resti coerente con lo stato
        // anche su un batch grande — l'handler committa solo se le righe inserite coincidono con i
        // mesi richiesti, quindi un successo parziale non deve poter passare per successo.
        var mesi = Enumerable.Range(1, 200).ToArray();

        var esito = await _handler.Send(Aggiungi(mesi));

        Assert.That(MesiSandbox(), Has.Count.EqualTo(esito == true ? 200 : 0),
            "O tutte o nessuna: un inserimento parziale dichiarato come riuscito sarebbe il caso peggiore.");
    }

    // ---------------------------------------------------------------------------------------------
    // Injection e stringhe ostili
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task SqlInjection_InIdEnte_ShouldEssereValoreNonSql()
    {
        var evil = "11111111-1111-1111-1111-111111111111'; DROP TABLE pfd.FattureWhiteList; --";

        try { await _handler.Send(Aggiungi([1], idEnte: evil)); }
        catch (Exception ex) { TestContext.Out.WriteLine($"eccezione: {ex.GetType().Name}"); }

        Assert.That(Scalare("SELECT COUNT(*) FROM sys.tables WHERE name = 'FattureWhiteList'"), Is.EqualTo(1),
            "Injection non eseguita: il parametro è un valore. "
            + "(FkIdEnte è nvarchar(100): una stringa più lunga viene rifiutata da SQL, non troncata.)");
    }

    [Test]
    public async Task TipologiaConCaratteriSpeciali_ShouldEssereSalvataIntegra()
    {
        const string tipologia = "PRIMO SALDO — «test» 100% ok\\n\t";

        var esito = await _handler.Send(Aggiungi([1], tipologia: tipologia));

        if (esito == true)
            Assert.That(TipologieSandbox().Single(), Is.EqualTo(tipologia),
                "Nessuna trasformazione: la tipologia dev'essere salvata esattamente com'è arrivata.");
        else
            Assert.That(MesiSandbox(), Is.Empty);
    }

    // ---------------------------------------------------------------------------------------------
    // Cancellazione: liste ostili
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task Cancella_ConIdDuplicatiNellaStessaLista_Caratterizzazione()
    {
        // Lo stesso id ripetuto: l'UPDATE ne aggiorna UNO, ma il command sottrae il numero di id
        // RICHIESTI. Quindi 1 riga aggiornata - 2 id = -1, cioè un "fallimento" su un'operazione
        // che invece ha fatto esattamente ciò che doveva. È il contratto di ritorno a essere fragile.
        await _handler.Send(Aggiungi([1]));
        var id = IdSandbox().Single();

        var esito = await _handler.Send(new FatturaWhiteListCancellazioneCommand(Auth(), [id, id]));

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.EqualTo(-1),
                "Comportamento attuale: gli id duplicati contano due volte nella sottrazione.");
            Assert.That(DataFineValorizzate(), Is.EqualTo(1), "Ma la riga è stata cancellata davvero.");
        });
    }

    [Test]
    public async Task Cancella_ConListaVuota_ShouldNonToccareNulla()
    {
        await _handler.Send(Aggiungi([1, 2]));

        int? esito = null;
        try { esito = await _handler.Send(new FatturaWhiteListCancellazioneCommand(Auth(), [])); }
        catch (Exception ex) { TestContext.Out.WriteLine($"eccezione: {ex.GetType().Name}"); }

        TestContext.Out.WriteLine($"lista vuota -> esito {esito}");
        Assert.That(DataFineValorizzate(), Is.Zero,
            "Una richiesta senza id non deve cancellare nulla: un `IN ()` che degenerasse in "
            + "'tutte le righe' svuoterebbe la whitelist.");
    }

    // ---------------------------------------------------------------------------------------------
    // Concorrenza
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task InserimentiConcorrenti_StessoPeriodo_ShouldNonCorrompereIlConteggio()
    {
        // Doppio click su "salva": due inserimenti identici. Non c'è unicità sulla chiave logica,
        // quindi i doppioni sono POSSIBILI per costruzione — la cosa che deve reggere è che l'esito
        // dichiarato corrisponda alle righe effettivamente create.
        var a = _handler.Send(Aggiungi([7]));
        var b = _handler.Send(Aggiungi([7]));
        var esiti = await Task.WhenAll(a, b);

        var righe = MesiSandbox().Count;
        TestContext.Out.WriteLine($"esiti: {string.Join(",", esiti)} — righe: {righe}");

        Assert.That(righe, Is.EqualTo(esiti.Count(e => e == true)),
            "Ogni esito positivo deve corrispondere a una riga: né righe fantasma, né successi taciuti.");
    }

    [Test]
    public async Task CancellazioniConcorrenti_StessoId_ShouldRiuscireUnaSola()
    {
        await _handler.Send(Aggiungi([8]));
        var id = IdSandbox().Single();

        var esiti = await Task.WhenAll(
            _handler.Send(new FatturaWhiteListCancellazioneCommand(Auth(), [id])),
            _handler.Send(new FatturaWhiteListCancellazioneCommand(Auth(), [id])));

        TestContext.Out.WriteLine($"esiti: {string.Join(",", esiti)}");

        Assert.Multiple(() =>
        {
            Assert.That(DataFineValorizzate(), Is.EqualTo(1), "Una sola riga, cancellata una sola volta.");
            Assert.That(esiti.Count(e => e == 0), Is.EqualTo(1),
                "Il filtro `datafine is null` deve far riuscire esattamente una delle due.");
        });
    }

    // ---------------------------------------------------------------------------------------------

    private static FattureWhiteListFattureAggiungiCommand Aggiungi(
        int[] mesi, string? idEnte = null, string? tipologia = null, int? anno = null)
        => new(Auth())
        {
            Anno = anno ?? AnnoSandbox,
            Mesi = mesi,
            TipologiaFattura = tipologia ?? Tipologia,
            IdEnte = idEnte ?? Ente
        };

    private static AuthenticationInfo Auth() => new()
    {
        Id = "integration-test-adversarial-wl",
        IdEnte = Ente,
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    private static List<int> MesiSandbox()
    {
        var mesi = new List<int>();
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Mese FROM pfd.FattureWhiteList WHERE Anno = @anno";
        cmd.Parameters.AddWithValue("@anno", AnnoSandbox);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            mesi.Add(reader.GetInt32(0));
        return mesi;
    }

    private static List<string> TipologieSandbox()
    {
        var valori = new List<string>();
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT FkTipologiaFattura FROM pfd.FattureWhiteList WHERE Anno = @anno";
        cmd.Parameters.AddWithValue("@anno", AnnoSandbox);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            valori.Add(reader.GetString(0));
        return valori;
    }

    private static int[] IdSandbox()
    {
        var ids = new List<int>();
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT IdLista FROM pfd.FattureWhiteList WHERE Anno = @anno AND DataFine IS NULL";
        cmd.Parameters.AddWithValue("@anno", AnnoSandbox);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            ids.Add(reader.GetInt32(0));
        return [.. ids];
    }

    private static int DataFineValorizzate() =>
        Scalare($"SELECT COUNT(*) FROM pfd.FattureWhiteList WHERE Anno = {AnnoSandbox} AND DataFine IS NOT NULL");

    private static int Scalare(string sql)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return (int)cmd.ExecuteScalar()!;
    }

    private static void Pulisci()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM pfd.FattureWhiteList WHERE Anno IN (@anno, 2147483647)";
        cmd.Parameters.AddWithValue("@anno", AnnoSandbox);
        cmd.ExecuteNonQuery();
    }
}
