using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test su query reale (MediatR + persistence + DB UAT)
/// per la funzionalità usata dagli endpoint:
/// - POST /api/fatture/pagopa/gestione-fatture/modifica/anni
/// - POST /api/fatture/pagopa/gestione-fatture/modifica/mesi
/// </summary>
public class GestioneFattureModificaQueryIntegrationTests
{
    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    private static AuthenticationInfo AdminAuth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    // La vista vwGestioneFattureFormAnniMesi usa Azione all'IMPERATIVO ('POSTICIPA'/'ELIMINA'),
    // NON al passato come la griglia ('POSTICIPATA'). Per un tipo SALDO l'azione ammessa e' POSTICIPA.
    // (Il test originale prendeva il seed dalla griglia: cross-vocabolario errato, mascherato su UAT
    //  dalla griglia vuota che faceva scattare Assert.Ignore.)
    private const string SeedTipologia = "SECONDO SALDO";
    private const string SeedAzione = "POSTICIPA";

    [Test]
    /// <summary>
    /// modifica/anni per una coppia (TipologiaFattura, Azione) valida in FormAnniMesi:
    /// deve tornare anni non vuoti, ordinati desc, e includere l'anno corrente (coperto dalla vista).
    /// </summary>
    public async Task GestioneFattureModificaAnniQuery_WithValidPair_ShouldReturnDescendingYears()
    {
        var years = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureModificaAnniQuery(AdminAuth())
            {
                TipologiaFattura = SeedTipologia,
                Azione = SeedAzione
            }));

        var list = years?.ToList() ?? [];

        Assert.Multiple(() =>
        {
            Assert.That(list, Is.Not.Empty);
            Assert.That(list.Contains(DateTime.Now.Year), Is.True, "La vista copre l'anno corrente.");
            Assert.That(list, Is.Ordered.Descending);
        });
    }

    [Test]
    /// <summary>
    /// modifica/mesi per (TipologiaFattura, Azione, Anno corrente) valida in FormAnniMesi:
    /// deve tornare mesi non vuoti (1..12), ordinati desc.
    /// </summary>
    public async Task GestioneFattureModificaMesiQuery_WithValidPair_ShouldReturnDescendingMonths()
    {
        var months = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureModificaMesiQuery(AdminAuth())
            {
                Anno = DateTime.Now.Year.ToString(),
                TipologiaFattura = SeedTipologia,
                Azione = SeedAzione
            }));

        var list = months?.ToList() ?? [];

        Assert.Multiple(() =>
        {
            Assert.That(list, Is.Not.Empty);
            Assert.That(list.All(m => m is >= 1 and <= 12), Is.True);
            Assert.That(list, Is.Ordered.Descending);
        });
    }

    private static async Task<T> ExecuteQueryOrIgnoreMissingView<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (SqlException ex) when (ex.Number == 208 && ex.Message.Contains("vwGestioneFattureGriglia", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("Vista [be].[vwGestioneFattureGriglia] non disponibile nell'ambiente corrente: test valido solo in UAT con schema completo.");
            throw;
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
