using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// `AzioneContestazioneQueryGetByIdNotificaHandler` end-to-end sul DB seedato: la stessa decisione
/// coperta a unit in `AzioneContestazioneMatriceUnitTests`, ma con le tre letture reali —
/// la notifica (schema pfd, via `NotificaQueryGetByIdPersistence`), la contestazione (schema pfw) e
/// il calendario — invece che simulate.
///
/// Cosa aggiunge rispetto agli unit test, che coprono gia' la matrice: il **mapping Dapper** di
/// `Notifica` (in particolare `StatoContestazione`, che non e' una colonna ma il risultato di
/// `INNER JOIN pfw.FlagContestazione … ISNULL(t.FkIdFlagContestazione, 1)`), il fatto che le due
/// query girino su **due factory/schema diversi**, e il calcolo reale delle finestre del calendario.
///
/// Seed di riferimento (`tests/Data/notifiche.sql`), ente1 / 2026-3:
///   EVT-3001  non contestata, `Fatturabile = 1`      → lock attivo
///   EVT-3002  CONTESTATA (stato 3), `Fatturabile = 0` → l'unica su cui si puo' agire
///   EVT-3003  digitale, `Fatturabile = 1` + TipologiaFattura → doppio lock
/// </summary>
public class AzioneContestazioneIntegrationTests
{
    private const string Ente = "11111111-1111-1111-1111-111111111111";
    private const string Contestata = "EVT-3002";
    private const string NonContestataMaBloccata = "EVT-3001";
    private const string DigitaleFatturata = "EVT-3003";

    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        AssicuraCalendarioAperto();
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    // ---------------------------------------------------------------------------------------------
    // I tre mondi sulla stessa notifica contestata
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task ProfiloEnte_SuNotificaContestata_ShouldPotereChiudereEModificare()
    {
        var esito = await Esegui(Contestata, "PA");

        Assert.Multiple(() =>
        {
            Assert.That(esito!.ChiusuraPermessa, Is.True, "L'Ente puo' accettare la controrisposta.");
            Assert.That(esito.CreazionePermessa, Is.True, "Su ContestataEnte la creazione vale 'modifica nota'.");
            Assert.That(esito.RispostaPermessa, Is.False, "Non ha ancora ricevuto una risposta a cui replicare.");
        });
    }

    [Test]
    public async Task SupportoInterno_SuNotificaContestata_ShouldPotereRispondereEChiudere()
    {
        var esito = await Esegui(Contestata, "SUP");

        Assert.Multiple(() =>
        {
            Assert.That(esito!.RispostaPermessa, Is.True);
            Assert.That(esito.ChiusuraPermessa, Is.True);
            Assert.That(esito.CreazionePermessa, Is.False, "Il supporto non apre contestazioni.");
        });
    }

    [Test]
    public async Task Recapitista_SuNotificaContestata_ShouldPotereSoloRispondere()
    {
        var esito = await Esegui(Contestata, "REC");

        Assert.Multiple(() =>
        {
            Assert.That(esito!.RispostaPermessa, Is.True);
            Assert.That(esito.ChiusuraPermessa, Is.False);
            Assert.That(esito.CreazionePermessa, Is.False);
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Il lock, che qui e' verificabile davvero perche' il calendario e' aperto
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// ATTENZIONE Il lock scatta su `Fatturabile`, non su un flag che si chiama "fatturata": la colonna
    /// `pfd.Notifiche.Fatturabile` viene esposta dalla query come `Fatturata` (`n.Fatturabile as
    /// Fatturata`) e l'handler la legge come "gia' inclusa in un ciclo di fatturazione". Nome della
    /// colonna e significato nel codice non coincidono — da tenere presente prima di modificare l'uno
    /// o l'altro.
    ///
    /// Il test ha valore **solo perche' il calendario e' aperto**: se non lo fosse, tutti i permessi
    /// sarebbero false comunque e l'asserzione non proverebbe nulla. E' il motivo della guardia in
    /// `[SetUp]`.
    /// </summary>
    [TestCase(NonContestataMaBloccata, TestName = "lock · Fatturabile = 1")]
    [TestCase(DigitaleFatturata, TestName = "lock · Fatturabile = 1 e TipologiaFattura valorizzata")]
    public async Task NotificaBloccata_ShouldNegareOgniAzione_AncheConCalendarioAperto(string idNotifica)
    {
        var esito = await Esegui(idNotifica, "PA");

        Assert.Multiple(() =>
        {
            Assert.That(esito!.ChiusuraPermessa, Is.False);
            Assert.That(esito.CreazionePermessa, Is.False);
            Assert.That(esito.RispostaPermessa, Is.False);
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Il payload: le tre letture arrivano davvero fino al DTO
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task SuNotificaContestata_ShouldPortareLaContestazioneCompleta()
    {
        var esito = await Esegui(Contestata, "PA");

        Assert.Multiple(() =>
        {
            Assert.That(esito!.Contestazione, Is.Not.Null, "La riga in pfw.Contestazioni esiste.");
            Assert.That(esito.Contestazione!.NoteEnte, Is.EqualTo("Notifica mai recapitata"));
            Assert.That(esito.Contestazione.Onere, Is.EqualTo("Recapitista"));
            Assert.That(esito.Contestazione.StatoContestazione, Is.EqualTo(3));
        });
    }

    /// <summary>
    /// Su una notifica mai contestata **non esiste riga** in `pfw.Contestazioni` (v.
    /// `business-contestazioni.md`: lo stato 1 e' l'assenza di riga, non un valore). La persistence
    /// restituisce null ed e' corretto: il DTO deve arrivare comunque, con lo stato 1 ricostruito
    /// dall'`ISNULL` della query.
    /// </summary>
    [Test]
    public async Task SuNotificaMaiContestata_ShouldAvereContestazioneNullaEStatoUno()
    {
        var esito = await Esegui(NonContestataMaBloccata, "PA");

        Assert.Multiple(() =>
        {
            Assert.That(esito!.Contestazione, Is.Null);
            Assert.That(esito.Notifica!.StatoContestazione, Is.EqualTo(1),
                "Ricostruito dall'INNER JOIN con ISNULL(FkIdFlagContestazione, 1).");
        });
    }

    [Test]
    public async Task IlRisultato_ShouldPortareNotificaECalendarioDelPeriodoGiusto()
    {
        var esito = await Esegui(Contestata, "PA");

        Assert.Multiple(() =>
        {
            Assert.That(esito!.Notifica!.IdNotifica, Is.EqualTo(Contestata));
            Assert.That(esito.Notifica.Anno, Is.EqualTo("2026"));
            Assert.That(esito.Notifica.Mese, Is.EqualTo("3"));
            Assert.That(esito.Calendario, Is.Not.Null);
            Assert.That(esito.Calendario!.AnnoContestazione, Is.EqualTo(2026),
                "Il calendario e' quello del periodo della notifica, non quello corrente.");
            Assert.That(esito.Calendario.MeseContestazione, Is.EqualTo(3));
        });
    }

    // ---------------------------------------------------------------------------------------------
    // I due cancelli, end-to-end
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task RuoloOperator_ShouldNegareOgniAzione()
    {
        var esito = await Esegui(Contestata, "PA", Ruolo.OPERATOR);

        Assert.Multiple(() =>
        {
            Assert.That(esito!.ChiusuraPermessa, Is.False);
            Assert.That(esito.CreazionePermessa, Is.False);
            Assert.That(esito.RispostaPermessa, Is.False);
        });
    }

    /// <summary>
    /// Periodo con la finestra gia' chiusa (2026/4 nel seed, con date interamente nel passato): la
    /// notifica e' contestata e l'utente e' ADMIN, ma non si puo' piu' fare nulla. E' la prova
    /// end-to-end che il calendario e' davvero il cancello — le date sono confrontate con l'ora reale,
    /// quindi il seed usa finestre esplicitamente passate/future per non dipendere dal giorno in cui
    /// gira la suite.
    /// </summary>
    [Test]
    public async Task PeriodoConFinestraChiusa_ShouldNegareOgniAzione()
    {
        var esito = await Esegui("EVT-3004", "PA");

        Assert.Multiple(() =>
        {
            Assert.That(esito!.Notifica!.StatoContestazione, Is.EqualTo(3), "La notifica e' contestata…");
            Assert.That(esito.ChiusuraPermessa, Is.False, "…ma la finestra e' chiusa.");
            Assert.That(esito.CreazionePermessa, Is.False);
            Assert.That(esito.RispostaPermessa, Is.False);
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Notifica inesistente
    // ---------------------------------------------------------------------------------------------

    [Test]
    public void NotificaInesistente_ShouldSollevareDomainException()
    {
        var ex = Assert.ThrowsAsync<DomainException>(async () => await Esegui("EVT-NON-ESISTE", "PA"));

        Assert.That(ex!.Message, Does.Contain("EVT-NON-ESISTE"));
    }

    // ---------------------------------------------------------------------------------------------

    private async Task<AzioneNotificaDto?> Esegui(string idNotifica, string profilo, string ruolo = Ruolo.ADMIN)
        => await _handler.Send(new AzioneContestazioneQueryGetByIdNotifica(
            new AuthenticationInfo
            {
                Id = "integration-test-azione",
                IdEnte = Ente,
                Prodotto = "prod-pn",
                Profilo = profilo,
                Ruolo = ruolo,
                IdTipoContratto = 1
            },
            idNotifica));

    /// <summary>
    /// Guardia contro un **falso verde**. `CalendarioContestazioneQueryGetPersistence` ha un
    /// `catch { return null; }`: se `pfw.ContestazioniCalendario` non esiste, l'errore viene inghiottito
    /// e l'handler riceve un calendario con entrambi i flag a `false` — cioe' *tutti i permessi
    /// negati*. Meta' di questi test passerebbero senza provare nulla.
    ///
    /// Meglio quindi ignorare la suite con un motivo esplicito che vederla verde a vuoto.
    /// </summary>
    private static void AssicuraCalendarioAperto()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
IF OBJECT_ID('pfw.ContestazioniCalendario') IS NULL SELECT -1
ELSE SELECT COUNT(*) FROM pfw.ContestazioniCalendario
     WHERE AnnoContestazione = 2026 AND MeseContestazione = 3
       AND GETDATE() BETWEEN DataInizio AND DataFine;";
        var esito = Convert.ToInt32(cmd.ExecuteScalar());

        if (esito < 0)
            Assert.Ignore("pfw.ContestazioniCalendario non esiste nel DB seedato. Senza quella tabella "
                + "il calendario degrada a 'tutto negato' e questi test passerebbero senza provare nulla.");
        if (esito == 0)
            Assert.Ignore("Manca la riga di calendario APERTA per 2026/3 (DataInizio nel passato, "
                + "DataFine nel futuro). Senza finestra aperta le asserzioni positive sono vuote.");
    }
}
