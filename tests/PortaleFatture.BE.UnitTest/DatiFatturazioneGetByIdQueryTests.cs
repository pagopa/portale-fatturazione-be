using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Legacy;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.UnitTest.Common;

namespace PortaleFatture.BE.UnitTest;

public class DatiFatturazioneGetByIdQueryTests
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
    public async Task GetById_ShouldFail_WithoutContatti()
    {
        string? expectedCup = "ecup";
        bool expectedNotaLegale = false;
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
            NotaLegale = expectedNotaLegale,
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

        ClassicAssert.ThrowsAsync<ValidationException>(async () => await _handler.Send(req));
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

        req.Contatti = expectedContatti;

        var actualDatiFatturazione = await _handler.Send(req);
        ClassicAssert.IsNotNull(actualDatiFatturazione);  

        var id = actualDatiFatturazione.Id;
        var select = new DatiFatturazioneQueryGetById()
        {
            Id = id
        };
            ;
        actualDatiFatturazione = await _handler.Send(select);
        ClassicAssert.IsNotNull(actualDatiFatturazione);
        ClassicAssert.True(actualDatiFatturazione.NotaLegale == expectedNotaLegale);
        ClassicAssert.True(actualDatiFatturazione.CodCommessa == expectedCodCommessa);
        ClassicAssert.IsNull(actualDatiFatturazione.DataModifica);
    }

    [Test]
    public async Task GetById_ShouldSucceed_WithContatti()
    {
        string? expectedCup = "ecup";
        bool expectedNotaLegale = true;
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
            NotaLegale = expectedNotaLegale,
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
        ClassicAssert.IsNotNull(actualDatiFatturazione); 

        var id = actualDatiFatturazione.Id;
        var select = new DatiFatturazioneQueryGetById()
        {
            Id = id
        };
        ;
        actualDatiFatturazione = await _handler.Send(select); 

        var contatti = actualDatiFatturazione.Contatti!.OrderBy(x => x.Email).ToList();
        ClassicAssert.True(contatti.Count == 2);
        ClassicAssert.True(contatti[0].Email == "expected1@pippo.com");
        ClassicAssert.True(contatti[1].Email == "expected2@pippo.com");
        ClassicAssert.IsNull(actualDatiFatturazione.DataModifica);
        ClassicAssert.IsTrue(actualDatiFatturazione.NotaLegale);
    }
}