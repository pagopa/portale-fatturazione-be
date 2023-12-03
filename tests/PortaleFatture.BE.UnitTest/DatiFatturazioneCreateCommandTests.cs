using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.UnitTest.Common;

namespace PortaleFatture.BE.UnitTest;

public class DatiFatturazioneCreateCommandTests
{
    private IDbContextFactory _factory;
    private ILogger<DatiFatturazioneCreateCommandTests> _logger;
    private IStringLocalizer<Localization> _localizer;
    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        _factory = ServiceProvider.GetRequiredService<IFattureDbContextFactory>();
        _logger = ServiceProvider.GetRequiredService<ILogger<DatiFatturazioneCreateCommandTests>>();
        _localizer = ServiceProvider.GetRequiredService<IStringLocalizer<Localization>>();
        _handler = ServiceProvider.GetRequiredService<IMediator>();
    }

    [Test]
    public async Task CreateCommand_ShouldSucceed_WithoutContatti()
    {
        string? expectedCup = "ecup";
        string? expectedCig = "ecig";
        string? expectedCodCommessa = "ecommmessa";
        DateTime expectedDataDocumento = DateTime.UtcNow;
        bool? expectedSplitPayment = false;
        string? expectedTipoCommessa = "1";
        string? expectedIdDocumento = "eiddocumento";
        string? expectedMap = "emap";
        DateTime  expectedDataCreazione = DateTime.UtcNow;
        string? expectedIdEnte = TestExtensions.GetRandomIdEnte();
        string? expectedPec = "pippo@pec.it";
        string? expectedProdotto = "prod-pn";
        var authInfo = TestExtensions.GetAuthInfo(expectedIdEnte, expectedProdotto);

        var req = new DatiFatturazioneCreateCommand(authInfo)
        {
            Cig = expectedCig,
            CodCommessa = expectedCodCommessa,
            Contatti = null,
            Cup = expectedCup,
            DataCreazione = expectedDataCreazione,
            DataDocumento = expectedDataDocumento,
            Pec = expectedPec, 
            TipoCommessa = expectedTipoCommessa,
            IdDocumento = expectedIdDocumento, 
            Map = expectedMap,
            SplitPayment = expectedSplitPayment             
        };

        var actualDatiFatturazione = await _handler.Send(req);
        Assert.IsNotNull(actualDatiFatturazione);
        Assert.True(actualDatiFatturazione.Cig == expectedCig);
        Assert.True(actualDatiFatturazione.CodCommessa == expectedCodCommessa);
        Assert.IsNull(actualDatiFatturazione.DataModifica);
    }

    [Test]
    public async Task CreateCommand_ShouldSucceed_WithContatti()
    {
        string? expectedCup = "ecup";
        string? expectedCig = "ecig";
        string? expectedCodCommessa = "ecommmessa";
        DateTime expectedDataDocumento = DateTime.UtcNow;
        bool? expectedSplitPayment = false;
        string? expectedTipoCommessa = "1";
        string? expectedIdDocumento = "eiddocumento";
        string? expectedMap = "emap";
        DateTime expectedDataCreazione = DateTime.UtcNow;
        string? expectedIdEnte = TestExtensions.GetRandomIdEnte();
        string? expectedPec = "pippo@pec.it";
        string? expectedProdotto = "prod-pn";
        var authInfo = TestExtensions.GetAuthInfo(expectedIdEnte, expectedProdotto);
        var expectedContatti = new List<DatiFatturazioneContattoCreateCommand>()
        { new()
            {
                 Email = "expected1@pippo.com" 
            },
            new()
            {
                 Email = "expected2@pippo.com" 
            },
        };
        var req = new DatiFatturazioneCreateCommand(authInfo)
        {
            Cig = expectedCig,
            CodCommessa = expectedCodCommessa,
            Contatti = expectedContatti,
            Cup = expectedCup,
            DataCreazione = expectedDataCreazione,
            DataDocumento = expectedDataDocumento,
            Pec = expectedPec, 
            TipoCommessa = expectedTipoCommessa,
            IdDocumento = expectedIdDocumento, 
            Map = expectedMap,
            SplitPayment = expectedSplitPayment
        };

        var actualDatiFatturazione = await _handler.Send(req);
        Assert.IsNotNull(actualDatiFatturazione);
        var contatti = actualDatiFatturazione.Contatti!.OrderBy(x => x.Email).ToList();
        Assert.True(contatti.Count == 2);
        Assert.True(contatti[0].Email == "expected1@pippo.com");
        Assert.True(contatti[1].Email == "expected2@pippo.com");
        Assert.IsNull(actualDatiFatturazione.DataModifica);
    }
}