using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Legacy;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.UnitTest.Common;

namespace PortaleFatture.BE.UnitTest;

public class DatiConfigurazioneModuloCommessaCreateCommandTests
{
    private IDbContextFactory _factory;
    private ILogger<DatiConfigurazioneModuloCommessaCreateCommandTests> _logger;
    private IStringLocalizer<Localization> _localizer;
    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        _factory = ServiceProvider.GetRequiredService<IFattureDbContextFactory>();
        _logger = ServiceProvider.GetRequiredService<ILogger<DatiConfigurazioneModuloCommessaCreateCommandTests>>();
        _localizer = ServiceProvider.GetRequiredService<IStringLocalizer<Localization>>();
        _handler = ServiceProvider.GetRequiredService<IMediator>();
    }

    [Test]
    public async Task Create_ShouldSucceed_True()
    {

        var conf = await CommonFactory.CreateDatiCommessaConfiguration(_handler, 1, "prod-pn");
        ClassicAssert.IsNotNull(conf);
        ClassicAssert.IsNotNull(conf.Categorie);
        ClassicAssert.IsTrue(conf.Categorie.Where(x => x.IdCategoriaSpedizione == 1).FirstOrDefault()!.Percentuale == 51);
        ClassicAssert.IsTrue(conf.Categorie.Where(x => x.IdCategoriaSpedizione == 2).FirstOrDefault()!.Percentuale == 52);
        ClassicAssert.IsNotNull(conf.Tipi);
        ClassicAssert.IsNotNull(conf.Tipi.Where(x => x.IdTipoSpedizione == 1).FirstOrDefault()!.MediaNotificaInternazionale == 2.0M + 1);
        ClassicAssert.IsNotNull(conf.Tipi.Where(x => x.IdTipoSpedizione == 1).FirstOrDefault()!.MediaNotificaNazionale == 1.0M + 1);
        ClassicAssert.IsNotNull(conf.Tipi.Where(x => x.IdTipoSpedizione == 2).FirstOrDefault()!.MediaNotificaInternazionale == 2.0M + 2);
        ClassicAssert.IsNotNull(conf.Tipi.Where(x => x.IdTipoSpedizione == 2).FirstOrDefault()!.MediaNotificaNazionale == 1.0M + 2);
        ClassicAssert.IsNotNull(conf.Tipi.Where(x => x.IdTipoSpedizione == 3).FirstOrDefault()!.MediaNotificaInternazionale == 2.0M + 3);
        ClassicAssert.IsNotNull(conf.Tipi.Where(x => x.IdTipoSpedizione == 3).FirstOrDefault()!.MediaNotificaNazionale == 1.0M + 3);
    }
}