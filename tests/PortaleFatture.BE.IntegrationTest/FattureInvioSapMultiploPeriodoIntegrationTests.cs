using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test (DB seedato) su FattureInvioSapMultiploPeriodoQuery, che dopo la modifica legge dalla
/// vista <c>be.vwDettaglioFattureDaInviare</c> (fatture da inviare / in elaborazione: FatturaInviata 0 o
/// NULL, escluso ente PagoPA e le fatture già in <c>cfg.GestioneFatture</c>). I filtri anno/mese/tipologia
/// sono OPZIONALI (WHERE dinamico): senza filtri torna tutto. Verifica: nessun filtro, filtro parziale,
/// filtro completo, mapping del DTO, esclusione delle fatture in staging.
///
/// Seed della vista (tutte Anno 2026): ANTICIPO(2001)/ACCONTO(2002) mese 5 ente3; PRIMO SALDO mese 6
/// INPS(3001)+ente2(1002); SECONDO SALDO mese 7 ente1(1001). Il seed non è chiuso
/// (altre aree possono aggiungere fatture non inviate): senza filtri si asserisce per inclusione.
/// Container spento → i test si ignorano.
/// </summary>
public class FattureInvioSapMultiploPeriodoIntegrationTests
{
    private const string Ente1 = "11111111-1111-1111-1111-111111111111";
    private const string Ente2 = "22222222-2222-2222-2222-222222222222";
    private const string Ente3 = "33333333-3333-3333-3333-333333333333";
    private const string EnteInps = "53b40136-65f2-424b-acfb-7fae17e35c60";

    private IMediator _handler = null!;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    private static AuthenticationInfo AdminAuth() => new()
    { IdEnte = Guid.NewGuid().ToString(), Prodotto = "prod-pn", Ruolo = Ruolo.ADMIN, IdTipoContratto = 1 };

    private Task<IEnumerable<FatturaInvioMultiploSapPeriodo>?> Query(int? anno, int? mese, string? tipologia) =>
        _handler.Send(new FattureInvioSapMultiploPeriodoQuery(AdminAuth())
        {
            AnnoRiferimento = anno,
            MeseRiferimento = mese,
            TipologiaFattura = tipologia
        });

    [Test]
    public async Task SenzaFiltri_RestituisceTutteLeFattureDaInviare()
    {
        CleanAllSeedPeriods(); // le 5 fatture seed devono essere tutte "da inviare" (non in staging)

        var ids = ((await Query(null, null, null))?.ToList() ?? []).Select(r => r.IdFattura).ToList();

        // NB: asserzione per inclusione, non su elenco chiuso. Il seed è condiviso fra le aree e cresce
        // (es. la fattura 7501, non inviata, aggiunta dai test di posticipa su fattura emessa): un
        // Is.EquivalentTo diventerebbe rosso a ogni fattura non inviata aggiunta da un'altra area,
        // senza che questa query sia cambiata. Ciò che va verificato qui e' che senza filtri escano
        // tutte le "da inviare" e SOLO quelle.
        Assert.Multiple(() =>
        {
            Assert.That(ids, Is.SupersetOf(new long[] { 1001, 1002, 2001, 2002, 3001 }),
                "Senza filtri la query deve restituire tutte le fatture 'da inviare' della vista.");
            Assert.That(ids, Does.Not.Contain(8001L),
                "8001 è già inviata (FatturaInviata=1): non deve comparire fra le 'da inviare'.");
            Assert.That(ids, Does.Not.Contain(9101L),
                "9101 è in cfg.GestioneFatture (seed statico, POSTICIPATA): la vista deve escluderla.");
        });
    }

    [Test]
    public async Task SoloMese_FiltraSoloSulMese_eMappaIlDto()
    {
        CleanupCfg(Ente3, "ANTICIPO", 2026, 5);
        CleanupCfg(Ente3, "ACCONTO", 2026, 5);

        var rows = (await Query(null, 5, null))?.ToList() ?? [];

        Assert.Multiple(() =>
        {
            Assert.That(rows.All(r => r.MeseRiferimento == 5), Is.True, "Filtro solo su mese.");
            Assert.That(rows.Select(r => r.IdFattura), Is.EquivalentTo(new long[] { 2001, 2002 }));
        });

        // mapping colonna vista -> DTO
        var anticipo = rows.Single(r => r.IdFattura == 2001);
        Assert.Multiple(() =>
        {
            Assert.That(anticipo.TipologiaFattura, Is.EqualTo("ANTICIPO"));
            Assert.That(anticipo.IdEnte, Is.EqualTo(Ente3));
            Assert.That(anticipo.RagioneSociale, Is.EqualTo("Ente Test 3"));
            Assert.That(anticipo.Importo, Is.EqualTo(305m));
            Assert.That(anticipo.AnnoRiferimento, Is.EqualTo(2026));
        });
    }

    [Test]
    public async Task AnnoMeseTipologia_PrimoSaldo_2026_6_RestituisceIDueEnti()
    {
        CleanupCfg(Ente2, "PRIMO SALDO", 2026, 6);
        CleanupCfg(EnteInps, "PRIMO SALDO", 2026, 6);

        var rows = (await Query(2026, 6, "PRIMO SALDO"))?.ToList() ?? [];

        Assert.That(rows.Count, Is.EqualTo(2), "PRIMO SALDO 2026/6: attese 2 fatture (ente2 + INPS).");
        Assert.Multiple(() =>
        {
            Assert.That(rows.All(r => r.AnnoRiferimento == 2026 && r.MeseRiferimento == 6), Is.True);
            Assert.That(rows.All(r => string.Equals(r.TipologiaFattura, "PRIMO SALDO", StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(rows.Select(r => r.IdEnte), Is.EquivalentTo(new[] { Ente2, EnteInps }));
        });
    }

    [Test]
    public async Task PeriodoSenzaFatture_RestituisceVuoto()
    {
        var rows = (await Query(2026, 11, "PRIMO SALDO"))?.ToList() ?? [];
        Assert.That(rows, Is.Empty);
    }

    [Test]
    public async Task Esclude_FattureGiaInGestioneFatture()
    {
        // 1001 = SECONDO SALDO 2026/7 ente1. Messa in staging (cfg.GestioneFatture) la vista deve escluderla.
        CleanupCfg(Ente1, "SECONDO SALDO", 2026, 7);
        try
        {
            var prima = (await Query(2026, 7, "SECONDO SALDO"))?.ToList() ?? [];
            Assume.That(prima.Any(r => r.IdFattura == 1001), Is.True, "Precondizione: 1001 visibile prima dello staging.");

            InsertCfg(Ente1, "SECONDO SALDO", 2026, 7, stato: 0);

            var dopo = (await Query(2026, 7, "SECONDO SALDO"))?.ToList() ?? [];
            Assert.That(dopo.Any(r => r.IdFattura == 1001), Is.False,
                "Una fattura già in cfg.GestioneFatture non deve comparire tra quelle 'da inviare'.");
        }
        finally { CleanupCfg(Ente1, "SECONDO SALDO", 2026, 7); }
    }

    // ---- helper SQL ----

    private void CleanAllSeedPeriods()
    {
        CleanupCfg(Ente3, "ANTICIPO", 2026, 5);
        CleanupCfg(Ente3, "ACCONTO", 2026, 5);
        CleanupCfg(EnteInps, "PRIMO SALDO", 2026, 6);
        CleanupCfg(Ente2, "PRIMO SALDO", 2026, 6);
        CleanupCfg(Ente1, "SECONDO SALDO", 2026, 7);
    }

    private static void CleanupCfg(string ente, string tip, int anno, int mese) => Exec(
        "DELETE FROM cfg.GestioneFatture WHERE FkIdEnte=@e AND FkTipologiaFattura=@t AND Anno=@a AND Mese=@m",
        ("@e", ente), ("@t", tip), ("@a", anno), ("@m", mese));

    private static void InsertCfg(string ente, string tip, int anno, int mese, int stato) => Exec(
        "INSERT INTO cfg.GestioneFatture (FkIdEnte, FkTipologiaFattura, Anno, Mese, Stato, IdUtenteInserimento, Azione) " +
        "VALUES (@e,@t,@a,@m,@s,'itest','POSTICIPATA')",
        ("@e", ente), ("@t", tip), ("@a", anno), ("@m", mese), ("@s", stato));

    private static void Exec(string sql, params (string Name, object Value)[] ps)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        foreach (var p in ps) cmd.Parameters.AddWithValue(p.Name, p.Value);
        cmd.ExecuteNonQuery();
    }
}
