using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Command di scrittura sulla WhiteList di fatturazione (`pfd.FattureWhiteList`), che era una delle
/// aree senza alcuna copertura — v. la sezione "command di scrittura" in coverage/test-backlog.md.
///
/// Perche' conta: la whitelist ESCLUDE un ente da un ciclo di fatturazione; le sue fatture finiscono
/// nelle tabelle `*_Eliminate` invece di essere emesse. Un difetto qui non produce un errore, produce
/// una fattura che non parte (o che parte e non doveva) — la stessa classe di problemi gia' vista su
/// cfg.GestioneFatture, emersa li' solo perche' quell'area i test ce li aveva.
///
/// I due command hanno contratti di ritorno OPPOSTI, ed e' il motivo principale per cui vale la pena
/// fissarli con dei test:
///   - Aggiungi   -> bool: true solo se le righe inserite sono ESATTAMENTE quante i mesi richiesti,
///                   dentro una transazione (commit/rollback esplicito nell'handler);
///   - Cancella   -> int : "righe aggiornate MENO id richiesti", quindi **0 = successo pieno** e
///                   valori NEGATIVI = qualche id non ha fatto effetto. Senza transazione.
///
/// Girano sul DB locale seeded (tests/docker-compose.yml), non su UAT. Sandbox: **Anno 2099**, che
/// nessun altro seed usa; il cleanup cancella per anno, quindi non puo' toccare altri dati.
/// </summary>
public class FattureWhiteListCommandIntegrationTests
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
    // Aggiungi
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task Aggiungi_TreMesi_ShouldReturnTrue_AndInserireUnaRigaPerMese()
    {
        var esito = await _handler.Send(new FattureWhiteListFattureAggiungiCommand(Auth())
        {
            Anno = AnnoSandbox,
            Mesi = [1, 2, 3],
            TipologiaFattura = Tipologia,
            IdEnte = Ente
        });

        Assert.That(esito, Is.True, "Con tutte le righe inserite l'handler deve committare e tornare true.");

        var righe = LeggiSandbox();
        Assert.Multiple(() =>
        {
            Assert.That(righe.Select(r => r.Mese), Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(righe.All(r => r.DataFine is null), Is.True,
                "Una riga appena inserita e' un'esclusione ATTIVA: DataFine deve restare NULL.");
            Assert.That(righe.All(r => r.DataInizio > DateTime.MinValue), Is.True,
                "DataInizio e' valorizzata dal command (ora italiana), non dal DB.");
        });
    }

    [Test]
    public async Task Aggiungi_ConTipologiaTroppoLunga_ShouldReturnFalse_AndNonLasciareRighe()
    {
        // Provoca un errore SQL a valle (troncamento) per esercitare il ramo catch -> Rollback -> false.
        // E' il percorso che protegge dall'inserimento parziale: l'handler committa SOLO se le righe
        // inserite coincidono con i mesi richiesti.
        var esito = await _handler.Send(new FattureWhiteListFattureAggiungiCommand(Auth())
        {
            Anno = AnnoSandbox,
            Mesi = [4, 5],
            TipologiaFattura = new string('X', 200),
            IdEnte = Ente
        });

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.False, "Un errore durante l'inserimento deve produrre false, non un'eccezione al chiamante.");
            Assert.That(LeggiSandbox(), Is.Empty, "Dopo il rollback non deve restare alcuna riga.");
        });
    }

    [Test]
    public async Task Aggiungi_SenzaMesi_ShouldReturnTrue_SenzaInserireNulla_Caratterizzazione()
    {
        // CARATTERIZZAZIONE, non approvazione. La condizione dell'handler e' `rowAffected == Mesi.Length`:
        // con un array vuoto diventa 0 == 0, quindi una richiesta che non fa NULLA riporta successo.
        // Il chiamante non ha modo di distinguerlo da un inserimento reale. L'endpoint dovrebbe
        // rifiutare a monte una lista di mesi vuota; se un domani lo fara', questo test va aggiornato.
        var esito = await _handler.Send(new FattureWhiteListFattureAggiungiCommand(Auth())
        {
            Anno = AnnoSandbox,
            Mesi = [],
            TipologiaFattura = Tipologia,
            IdEnte = Ente
        });

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.True, "Comportamento attuale: nessun mese -> 'successo'.");
            Assert.That(LeggiSandbox(), Is.Empty);
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Cancella (soft-delete: valorizza DataFine)
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task Cancella_IdEsistenti_ShouldReturn0_AndValorizzareDataFine()
    {
        var ids = Inserisci(1, 2);

        var esito = await _handler.Send(new FatturaWhiteListCancellazioneCommand(Auth(), ids));

        Assert.That(esito, Is.Zero,
            "Contratto del command: righe aggiornate MENO id richiesti. Zero significa che sono state "
            + "cancellate tutte — non 'nessuna riga toccata'.");

        Assert.That(LeggiSandbox().All(r => r.DataFine is not null), Is.True,
            "La cancellazione e' un soft-delete: le righe restano, con DataFine valorizzata.");
    }

    [Test]
    public async Task Cancella_IdInesistente_ShouldReturnNegativo()
    {
        var esito = await _handler.Send(new FatturaWhiteListCancellazioneCommand(Auth(), [int.MaxValue]));

        Assert.That(esito, Is.EqualTo(-1),
            "Un id che non esiste non aggiorna nulla: 0 righe - 1 id richiesto = -1.");
    }

    [Test]
    public async Task Cancella_SuGiaCancellata_ShouldReturnNegativo_MaLasciareLaDataFineOriginale()
    {
        var ids = Inserisci(6);
        await _handler.Send(new FatturaWhiteListCancellazioneCommand(Auth(), ids));
        var dataFinePrimaChiamata = LeggiSandbox().Single().DataFine;

        var esito = await _handler.Send(new FatturaWhiteListCancellazioneCommand(Auth(), ids));

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.EqualTo(-1),
                "Il WHERE filtra `datafine is null`, quindi la seconda chiamata non aggiorna nulla.");
            Assert.That(LeggiSandbox().Single().DataFine, Is.EqualTo(dataFinePrimaChiamata),
                "Ripetere la cancellazione non deve spostare la data della prima: l'effetto e' idempotente "
                + "anche se il valore di ritorno non lo e'.");
        });
    }

    [Test]
    public async Task Cancella_MixValidoEInesistente_ShouldCancellareIlValido_SenzaRollback()
    {
        // A differenza di Aggiungi, questo handler NON apre una transazione (Create() senza commit:true):
        // un id sbagliato nella lista non annulla l'effetto sugli altri. Il chiamante vede solo un
        // numero negativo e non sa QUALI id non hanno avuto effetto.
        var ids = Inserisci(7);
        var richiesti = new[] { ids[0], int.MaxValue };

        var esito = await _handler.Send(new FatturaWhiteListCancellazioneCommand(Auth(), richiesti));

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.EqualTo(-1), "1 riga aggiornata - 2 id richiesti = -1.");
            Assert.That(LeggiSandbox().Single().DataFine, Is.Not.Null,
                "L'id valido resta cancellato: nessun rollback dell'effetto parziale.");
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Helper
    // ---------------------------------------------------------------------------------------------

    private static AuthenticationInfo Auth() => new()
    {
        Id = "integration-test-whitelist",
        IdEnte = Ente,
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    /// <summary>Inserisce righe attive (DataFine NULL) nella sandbox e ne restituisce gli id.</summary>
    private static int[] Inserisci(params int[] mesi)
    {
        var ids = new List<int>();
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();

        foreach (var mese in mesi)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO pfd.FattureWhiteList (FkIdEnte, Anno, Mese, DataInizio, FkTipologiaFattura, IdUtente)
VALUES (@ente, @anno, @mese, GETDATE(), @tipologia, 'seed-test');
SELECT CAST(SCOPE_IDENTITY() AS int);";
            cmd.Parameters.AddWithValue("@ente", Ente);
            cmd.Parameters.AddWithValue("@anno", AnnoSandbox);
            cmd.Parameters.AddWithValue("@mese", mese);
            cmd.Parameters.AddWithValue("@tipologia", Tipologia);
            ids.Add((int)cmd.ExecuteScalar()!);
        }

        return [.. ids];
    }

    private static List<(int Mese, DateTime DataInizio, DateTime? DataFine)> LeggiSandbox()
    {
        var righe = new List<(int, DateTime, DateTime?)>();
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Mese, DataInizio, DataFine FROM pfd.FattureWhiteList WHERE Anno = @anno ORDER BY Mese";
        cmd.Parameters.AddWithValue("@anno", AnnoSandbox);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            righe.Add((reader.GetInt32(0), reader.GetDateTime(1), reader.IsDBNull(2) ? null : reader.GetDateTime(2)));

        return righe;
    }

    private static void Pulisci()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM pfd.FattureWhiteList WHERE Anno = @anno";
        cmd.Parameters.AddWithValue("@anno", AnnoSandbox);
        cmd.ExecuteNonQuery();
    }
}
