using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test sul comportamento di DapperBase.SingleAsync vs SingleOrDefaultAsync con 0 / 1 / N
/// righe. Contesto: il bug 500 su api/rel/pagopa/{id} nasce da SingleAsync usato dove la query puo'
/// dare 0 righe (QuerySingleAsync lancia -> 500 invece di 404).
///
/// DECISIONE: per rel/pagopa/{id} il 500 su 0 righe e' CORRETTO -> per una chiave valida il dettaglio
/// deve esistere, quindi l'assenza e' un'anomalia reale, non un 404. L'endpoint resta su SingleAsync.
/// (Il 500 su SECONDO SALDO / SEM. SOSPESI ha causa a monte nella vista be.vwRelDettaglio, fix lato Data.)
///
/// SingleOrDefaultAsync resta comunque su DapperBase come utility generica per i casi in cui "non trovato"
/// e' un esito legittimo: i suoi test sono ATTIVI e ne fissano il contratto (0 -> null, 1 -> valore,
/// &gt;1 -> lancia). Entrambi i gruppi usano query DATA-INDEPENDENT.
///
/// Le query di sonda sono DATA-INDEPENDENT (SELECT letterali / VALUES): servono solo connettivita' al DB,
/// nessun seed. Coerente con i test "strutturali" del progetto (es. FattureSospeseRelExcelQueryTests):
/// senza connessione (VPN giu') il test risulta Ignored, non fallito.
/// </summary>
public class DapperBaseSingleTests
{
    private sealed class Probe : DapperBase { } // concreta: DapperBase e' abstract

    private const string Sql0 = "SELECT CAST(1 AS int) WHERE 1 = 0";                      // 0 righe
    private const string Sql1 = "SELECT CAST(42 AS int)";                                 // 1 riga -> 42
    private const string SqlN = "SELECT x FROM (VALUES (1),(2)) AS t(x)";                 // 2 righe

    private string _connectionString = null!;

    [SetUp]
    /// Verifica che la connessione al DB sia configurata e raggiungibile. Se non lo e', ignora i test.
    public void Setup()
    {
        var conf = ServiceProvider.GetRequiredService<IConfiguration>();
        _connectionString = conf["PortaleFattureOptions:ConnectionString"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_connectionString))
            Assert.Ignore("PortaleFattureOptions:ConnectionString non configurata: test DB saltato.");

        // Se il DB non e' raggiungibile (es. VPN giu') degrada a Ignore invece di fallire.
        try
        {
            using var probe = new SqlConnection(_connectionString);
            probe.Open();
        }
        catch (Exception ex)
        {
            Assert.Ignore($"DB non raggiungibile ({ex.GetType().Name}): test DB saltato.");
        }
    }

    private static IDatabase Db() => new Probe();
    private IDbConnection Open()
    {
        var c = new SqlConnection(_connectionString);
        c.Open();
        return c; // i metodi DapperBase con transaction null chiudono la connessione (using) al termine
    }

    // ---------- SingleAsync: comportamento ATTUALE (attivi) ----------

    [Test]
    /// <summary>
    /// Verifica che SingleAsync lanci un'eccezione quando la query restituisce 0 righe.
    /// </summary>
    public void SingleAsync_ZeroRows_Throws()
    {
        // E' esattamente la causa del 500 su rel/pagopa/{id}: 0 righe -> eccezione.
        Assert.That(async () => await Db().SingleAsync<int>(Open(), Sql0, null, null, CommandType.Text),
            Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    /// <summary>
    /// Verifica che SingleAsync restituisca il valore corretto quando la query restituisce 1 riga.
    /// </summary>
    public async Task SingleAsync_OneRow_ReturnsValue()
    {
        var r = await Db().SingleAsync<int>(Open(), Sql1, null, null, CommandType.Text);
        Assert.That(r, Is.EqualTo(42));
    }

    [Test]
    /// <summary>
    /// Verifica che SingleAsync lanci un'eccezione quando la query restituisce più di 1 riga.
    /// </summary>
    public void SingleAsync_MultipleRows_Throws()
    {
        Assert.That(async () => await Db().SingleAsync<int>(Open(), SqlN, null, null, CommandType.Text),
            Throws.InstanceOf<InvalidOperationException>());
    }

    // ---------- SingleOrDefaultAsync: utility generica su DapperBase (attivi) ----------

    [Test]
    /// <summary>
    /// Verifica che SingleOrDefaultAsync restituisca il valore di default quando la query restituisce 0 righe.
    /// </summary>
    public async Task SingleOrDefaultAsync_ZeroRows_ReturnsDefault()
    {
        var r = await Db().SingleOrDefaultAsync<int?>(Open(), Sql0, null, null, CommandType.Text);
        Assert.That(r, Is.Null, "0 righe -> null (non deve lanciare): permette all'endpoint di fare 404.");
    }

    [Test]
    /// <summary>
    /// Verifica che SingleOrDefaultAsync restituisca il valore corretto quando la query restituisce 1 riga.
    /// </summary>
    public async Task SingleOrDefaultAsync_OneRow_ReturnsValue()
    {
        var r = await Db().SingleOrDefaultAsync<int>(Open(), Sql1, null, null, CommandType.Text);
        Assert.That(r, Is.EqualTo(42));
    }

    [Test]
    /// <summary>
    /// Verifica che SingleOrDefaultAsync lanci un'eccezione quando la query restituisce più di 1 riga.
    /// </summary>
    public void SingleOrDefaultAsync_MultipleRows_Throws()
    {
        // Anche SingleOrDefault lancia su >1: il duplicato resta un segnale (chiave non univoca).
        Assert.That(async () => await Db().SingleOrDefaultAsync<int>(Open(), SqlN, null, null, CommandType.Text),
            Throws.InstanceOf<InvalidOperationException>());
    }
}
