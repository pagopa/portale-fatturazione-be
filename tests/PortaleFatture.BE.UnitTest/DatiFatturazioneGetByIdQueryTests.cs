using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Legacy;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries;
using PortaleFatture.BE.UnitTest.Common;

namespace PortaleFatture.BE.UnitTest;

public class DatiFatturazioneGetByIdQueryTests
{
    /// <summary>Ente dedicato a questa fixture nel seed (tests/Data/dati_fatturazione.sql).</summary>
    private const string IdEnteSeed = "55555555-5555-5555-5555-555555555555";
    private const string CodiceSdiSeed = "ABCDEF1";

    private IDbContextFactory _factory;
    private ILogger<DatiFatturazioneCreateCommandTests> _logger;
    private IStringLocalizer<Localization> _localizer;
    private IMediator _handler;

    [SetUp]
    public async Task Setup()
    {
        _factory = ServiceProvider.GetRequiredService<IFattureDbContextFactory>(LocalTestDb.ConnectionString);
        _logger = ServiceProvider.GetRequiredService<ILogger<DatiFatturazioneCreateCommandTests>>(LocalTestDb.ConnectionString);
        _localizer = ServiceProvider.GetRequiredService<IStringLocalizer<Localization>>(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
        await Pulisci();
    }

    [TearDown]
    public async Task TearDown() => await Pulisci();

    private static Task Pulisci() => LocalTestDb.ExecuteAsync($@"
        DELETE c FROM pfw.DatiFatturazioneContatti c
          INNER JOIN pfw.DatiFatturazione d ON d.IdDatiFatturazione = c.FkIdDatiFatturazione
         WHERE d.FkIdEnte = '{IdEnteSeed}';
        DELETE FROM pfw.DatiFatturazione WHERE FkIdEnte = '{IdEnteSeed}';
        DELETE FROM pfw.[Log] WHERE FkIdEnte = '{IdEnteSeed}';");

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
        string? expectedIdEnte = IdEnteSeed;
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
            SplitPayment = expectedSplitPayment,
            CodiceSDI = CodiceSdiSeed
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
        string? expectedIdEnte = IdEnteSeed;
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
            SplitPayment = expectedSplitPayment,
            CodiceSDI = CodiceSdiSeed
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