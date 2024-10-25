using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Legacy;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Commands;
using PortaleFatture.BE.UnitTest.Common;

namespace PortaleFatture.BE.UnitTest;

public class DatiModuloCommessaCreateTests
{
    private IDbContextFactory _factory;
    private ILogger<DatiModuloCommessaCreateTests> _logger;
    private IStringLocalizer<Localization> _localizer;
    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        _factory = ServiceProvider.GetRequiredService<IFattureDbContextFactory>();
        _logger = ServiceProvider.GetRequiredService<ILogger<DatiModuloCommessaCreateTests>>();
        _localizer = ServiceProvider.GetRequiredService<IStringLocalizer<Localization>>();
        _handler = ServiceProvider.GetRequiredService<IMediator>();
    }

    [Test]
    public void CreateCommand_ShouldFailNoAdmin_True()
    {
        string? expectedIdEnte = TestExtensions.GetRandomIdEnte();
        string? expectedProdotto = "prod-pn";
        var expectedTipoContratto = 1;
        var authInfo = TestExtensions.GetAuthInfo(expectedIdEnte, expectedProdotto, Ruolo.OPERATOR, expectedTipoContratto);
        var (anno, mese, _, _) = Time.YearMonthDayFatturazione();
 
        List<DatiModuloCommessaCreateCommand> cmds = [];

        for (var i = 1; i < 4; i++)
        {
            var request = new DatiModuloCommessaCreateCommand()
            {
                AnnoValidita = anno,
                MeseValidita = mese,
                IdEnte = expectedIdEnte,
                IdTipoContratto = expectedTipoContratto,
                IdTipoSpedizione = i,
                NumeroNotificheInternazionali = 1,
                NumeroNotificheNazionali = 1,
            };
            cmds.Add(request);
        }
        var command = new DatiModuloCommessaCreateListCommand(authInfo)
        {
            DatiModuloCommessaListCommand = cmds
        };

        Assert.ThrowsAsync<ValidationException>(async () => await _handler.Send(command)); 
    }

    [Test]
    public async Task CreateCommand_ShouldSucceed_True()
    { 
        string? expectedIdEnte = TestExtensions.GetRandomIdEnte();
        string? expectedProdotto = "prod-pn";
        var expectedTipoContratto = 1;
        var authInfo = TestExtensions.GetAuthInfo(expectedIdEnte, expectedProdotto, Ruolo.ADMIN, expectedTipoContratto);
        var (anno, mese, _, _) = Time.YearMonthDayFatturazione();

        var conf = await CommonFactory.CreateDatiCommessaConfiguration(_handler, expectedTipoContratto, expectedProdotto);
        List<DatiModuloCommessaCreateCommand> cmds = new();

        for (var i = 1; i < 4; i++)
        {
            var request = new DatiModuloCommessaCreateCommand()
            {
                AnnoValidita = anno,
                MeseValidita = mese,
                IdEnte = expectedIdEnte,
                IdTipoContratto = expectedTipoContratto,
                IdTipoSpedizione = i,
                NumeroNotificheInternazionali = 1,
                NumeroNotificheNazionali = 1,  
            };
            cmds.Add(request);
        }
        var command = new DatiModuloCommessaCreateListCommand(authInfo)
        {
            DatiModuloCommessaListCommand = cmds
        }; 

        var moduloCommessa = await _handler.Send(command);
        var commesse = moduloCommessa!.DatiModuloCommessa;
        ClassicAssert.NotNull(commesse);
        ClassicAssert.IsTrue(commesse.Count() == 3);

        var commess1 = commesse.Where(x => x.IdTipoSpedizione == 1).FirstOrDefault();
        ClassicAssert.IsNotNull(commess1);
        ClassicAssert.IsTrue(commess1.IdEnte == expectedIdEnte);
        ClassicAssert.IsTrue(commess1.MeseValidita == mese);
        ClassicAssert.IsTrue(commess1.AnnoValidita == anno);
        ClassicAssert.IsTrue(commess1.IdTipoContratto == expectedTipoContratto);
    } 
}