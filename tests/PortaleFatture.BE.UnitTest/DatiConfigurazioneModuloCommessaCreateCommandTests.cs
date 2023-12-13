using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;
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
        Assert.IsNotNull(conf);
        Assert.IsNotNull(conf.Categorie);
        Assert.IsTrue(conf.Categorie.Where(x => x.IdCategoriaSpedizione == 1).FirstOrDefault()!.Percentuale == 51);
        Assert.IsTrue(conf.Categorie.Where(x => x.IdCategoriaSpedizione == 2).FirstOrDefault()!.Percentuale == 52);
        Assert.IsNotNull(conf.Tipi);
        Assert.IsNotNull(conf.Tipi.Where(x => x.IdTipoSpedizione == 1).FirstOrDefault()!.MediaNotificaInternazionale == 2.0M + 1);
        Assert.IsNotNull(conf.Tipi.Where(x => x.IdTipoSpedizione == 1).FirstOrDefault()!.MediaNotificaNazionale == 1.0M + 1);
        Assert.IsNotNull(conf.Tipi.Where(x => x.IdTipoSpedizione == 2).FirstOrDefault()!.MediaNotificaInternazionale == 2.0M + 2);
        Assert.IsNotNull(conf.Tipi.Where(x => x.IdTipoSpedizione == 2).FirstOrDefault()!.MediaNotificaNazionale == 1.0M + 2);
        Assert.IsNotNull(conf.Tipi.Where(x => x.IdTipoSpedizione == 3).FirstOrDefault()!.MediaNotificaInternazionale == 2.0M + 3);
        Assert.IsNotNull(conf.Tipi.Where(x => x.IdTipoSpedizione == 3).FirstOrDefault()!.MediaNotificaNazionale == 1.0M + 3);
    }
}