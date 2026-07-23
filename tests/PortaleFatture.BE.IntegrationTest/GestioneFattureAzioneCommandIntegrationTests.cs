using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test su command reale (MediatR + handler + persistence + DB UAT)
/// per la funzionalità usata dall'endpoint POST /api/fatture/pagopa/gestione-fatture/azione.
/// </summary>
public class GestioneFattureAzioneCommandIntegrationTests
{
    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        _handler = ServiceProvider.GetRequiredService<IMediator>();
    }

    private static AuthenticationInfo AdminAuth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    [Test]
    /// <summary>
    /// Verifica che un'azione non valida venga rifiutata dal livello persistence/handler
    /// con ArgumentException, senza tentare di eseguire il comando sul DB.
    /// </summary>
    public async Task GestioneFattureAzioneCommand_WithInvalidAction_ShouldThrowArgumentException()
    {
        var command = new GestioneFattureAzioneCommand(AdminAuth())
        {
            IdEnte = "11111111-1111-1111-1111-111111111111",
            Azione = "AZIONE_NON_VALIDA",
            Anno = 2026,
            Mese = 7,
            TipologiaFattura = "SECONDO SALDO",
            IdUtente = "user-test",
            IdFattura = 123
        };

        try
        {
            await ExecuteCommandOrIgnoreConnectionErrors(() => _handler.Send(command));
            Assert.Fail("Expected ArgumentException.");
        }
        catch (ArgumentException ex)
        {
            Assert.That(ex.Message, Does.Contain("non esiste"));
        }
    }

    [TestCase("POSTICIPA")]
    [TestCase("ELIMINA")]
    [TestCase("RIPRISTINA")]
    [TestCase("CANCELLA")]
    /// <summary>
    /// Verifica che ogni azione supportata superi la validazione dello switch interno
    /// e arrivi al parsing di IdEnte; con IdEnte non parsabile deve emergere FormatException
    /// prima di qualsiasi invocazione effettiva verso il database.
    /// </summary>
    public async Task GestioneFattureAzioneCommand_WithSupportedActionAndInvalidIdEnte_ShouldThrowFormatException(string azione)
    {
        var command = new GestioneFattureAzioneCommand(AdminAuth())
        {
            IdEnte = "NOT-A-GUID",
            Azione = azione,
            Anno = 2026,
            Mese = 7,
            TipologiaFattura = "SECONDO SALDO",
            IdUtente = "user-test",
            IdFattura = 123
        };

        try
        {
            await ExecuteCommandOrIgnoreConnectionErrors(() => _handler.Send(command));
            Assert.Fail("Expected FormatException.");
        }
        catch (FormatException)
        {
            Assert.Pass();
        }
    }

    private static async Task<T> ExecuteCommandOrIgnoreConnectionErrors<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (SqlException ex) when (
            ex.Number == 47073 ||
            ex.Message.Contains("Deny Public Network Access is set to Yes", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("Connessione SQL non consentita nell'ambiente corrente (Public Network Access disabilitato / VPN non attiva): test valido solo in UAT con accesso DB.");
            throw;
        }
    }
}
