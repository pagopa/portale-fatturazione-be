using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Legacy;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.UnitTest.Common;

namespace PortaleFatture.BE.UnitTest;

public class DatiFatturazioneUpdateCommandTests
{
    private IDbContextFactory _factory;
    private ILogger<DatiFatturazioneUpdateCommandTests> _logger;
    private IStringLocalizer<Localization> _localizer;
    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        _factory = ServiceProvider.GetRequiredService<IFattureDbContextFactory>();
        _logger = ServiceProvider.GetRequiredService<ILogger<DatiFatturazioneUpdateCommandTests>>();
        _localizer = ServiceProvider.GetRequiredService<IStringLocalizer<Localization>>();
        _handler = ServiceProvider.GetRequiredService<IMediator>();
    }

    [Test]
    public async Task UpdateCommand_ShouldSucceed_WithoutContatti()
    {
        string? expectedCup = "ecup";
        bool expectedNotaLegale = true;
        string? expectedCodCommessa = "ecommmessa";
        DateTime expectedDataDocumento = DateTime.UtcNow.ItalianTime();
        bool? expectedSplitPayment = false;
        string? expectedTipoCommessa = "1";
        string? expectedIdDocumento = "eiddocumento";
        string? expectedMap = "emap";
        DateTime expectedDataCreazione = DateTime.UtcNow.ItalianTime();
        string? expectedIdEnte = TestExtensions.GetRandomIdEnte();
        string? expectedPec = "pippo@pec.it";
        string? expectedProdotto = "prod-pn";
        var authInfo = TestExtensions.GetAuthInfo(expectedIdEnte, expectedProdotto);
        var expectedContatto = "pippo@gmail.com";
        List<DatiFatturazioneContattoCreateCommand> contatti =
        [
            new DatiFatturazioneContattoCreateCommand()
            {
                Email = expectedContatto
            },
        ];

        var request = new DatiFatturazioneCreateCommand(authInfo)
        {
            NotaLegale = expectedNotaLegale,
            CodCommessa = expectedCodCommessa,
            Contatti = contatti,
            Cup = expectedCup,
            DataCreazione = expectedDataCreazione,
            DataDocumento = expectedDataDocumento,
            Pec = expectedPec,
            TipoCommessa = expectedTipoCommessa,
            IdDocumento = expectedIdDocumento,
            Map = expectedMap,
            SplitPayment = expectedSplitPayment
        };

        var actualDatiFatturazione = await _handler.Send(request);
        ClassicAssert.IsNotNull(actualDatiFatturazione);

        DateTime expectedDataModifica = DateTime.UtcNow.AddMinutes(1);
        var expectedUpdatedPec = "modified@pec.it";
        expectedNotaLegale = false;

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

        var updateRequest = new DatiFatturazioneUpdateCommand(authInfo)
        {
            Id = actualDatiFatturazione.Id,
            NotaLegale = expectedNotaLegale,
            CodCommessa = expectedCodCommessa,
            Contatti = expectedContatti,
            Cup = expectedCup,
            DataModifica = expectedDataModifica,
            DataDocumento = expectedDataDocumento,
            Pec = expectedUpdatedPec,
            TipoCommessa = expectedTipoCommessa,
            IdDocumento = expectedIdDocumento,
            Map = expectedMap,
            SplitPayment = expectedSplitPayment
        };

        actualDatiFatturazione = await _handler.Send(updateRequest);

        ClassicAssert.True(actualDatiFatturazione.NotaLegale == expectedNotaLegale);
        ClassicAssert.True(actualDatiFatturazione.CodCommessa == expectedCodCommessa);
        ClassicAssert.True(actualDatiFatturazione.Pec == expectedUpdatedPec);
        ClassicAssert.IsNotNull(actualDatiFatturazione.Contatti);
    }


    [Test]
    public async Task UpdateCommand_ShouldSucceed_WithEmptyContatti()
    {
        string? expectedCup = "ecup";
        bool expectedNotaLegale = true;
        string? expectedCodCommessa = "ecommmessa";
        DateTime expectedDataDocumento = DateTime.UtcNow.ItalianTime();
        bool? expectedSplitPayment = false;
        string? expectedTipoCommessa = "1";
        string? expectedIdDocumento = "eiddocumento";
        string? expectedMap = "emap";
        DateTime expectedDataCreazione = DateTime.UtcNow.ItalianTime();
        string? expectedIdEnte = TestExtensions.GetRandomIdEnte();
        string? expectedPec = "pippo@pec.it";
        string? expectedProdotto = "prod-pn";
        var authInfo = TestExtensions.GetAuthInfo(expectedIdEnte, expectedProdotto);
        var expectedContatto = "pippo@gmail.com";
        List<DatiFatturazioneContattoCreateCommand> contatti =
        [
            new DatiFatturazioneContattoCreateCommand()
            {
                Email = expectedContatto
            },
        ];

        var request = new DatiFatturazioneCreateCommand(authInfo)
        {
            NotaLegale = expectedNotaLegale,
            CodCommessa = expectedCodCommessa,
            Contatti = contatti,
            Cup = expectedCup,
            DataCreazione = expectedDataCreazione,
            DataDocumento = expectedDataDocumento,
            Pec = expectedPec,
            TipoCommessa = expectedTipoCommessa,
            IdDocumento = expectedIdDocumento,
            Map = expectedMap,
            SplitPayment = expectedSplitPayment
        };

        var actualDatiFatturazione = await _handler.Send(request);
        ClassicAssert.IsNotNull(actualDatiFatturazione);

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

        DateTime expectedDataModifica = DateTime.UtcNow.AddMinutes(1);
        var expectedUpdatedPec = "modified@pec.it";
        expectedNotaLegale = false;
        var updateRequest = new DatiFatturazioneUpdateCommand(authInfo)
        {
            Id = actualDatiFatturazione.Id,
            NotaLegale = expectedNotaLegale,
            CodCommessa = expectedCodCommessa,
            Contatti = expectedContatti,
            Cup = expectedCup,
            DataModifica = expectedDataModifica,
            DataDocumento = expectedDataDocumento,
            Pec = expectedUpdatedPec,
            TipoCommessa = expectedTipoCommessa,
            IdDocumento = expectedIdDocumento,
            Map = expectedMap,
            SplitPayment = expectedSplitPayment
        };

        actualDatiFatturazione = await _handler.Send(updateRequest);

        ClassicAssert.True(actualDatiFatturazione.NotaLegale == expectedNotaLegale);
        ClassicAssert.True(actualDatiFatturazione.CodCommessa == expectedCodCommessa);
        ClassicAssert.True(actualDatiFatturazione.Pec == expectedUpdatedPec);
        ClassicAssert.IsNotNull(actualDatiFatturazione.Contatti);
    }

    [Test]
    public async Task UpdateCommand_ShouldSucceed_WithDifferentContatti()
    {
        string? expectedCup = "ecup";
        bool expectedNotaLegale = true;
        string? expectedCodCommessa = "ecommmessa";
        DateTime expectedDataDocumento = DateTime.UtcNow.ItalianTime();
        bool? expectedSplitPayment = false;
        string? expectedTipoCommessa = "1";
        string? expectedIdDocumento = "eiddocumento";
        string? expectedMap = "emap";
        DateTime expectedDataCreazione = DateTime.UtcNow.ItalianTime();
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

        var request = new DatiFatturazioneCreateCommand(authInfo)
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

        var actualDatiFatturazione = await _handler.Send(request);
        ClassicAssert.IsNotNull(actualDatiFatturazione);

        DateTime expectedDataModifica = DateTime.UtcNow.AddMinutes(1);
        var expectedUpdatedPec = "modified@pec.it";
        var expectedContatto = "pippo@gmail.com";
        expectedNotaLegale = false;
        List<DatiFatturazioneContattoCreateCommand> contatti =
        [
            new DatiFatturazioneContattoCreateCommand()
            {
                Email = expectedContatto,
                IdDatiFatturazione = actualDatiFatturazione.Id
            },
        ];

        var updateRequest = new DatiFatturazioneUpdateCommand(authInfo)
        {
            Id = actualDatiFatturazione.Id,
            NotaLegale = expectedNotaLegale,
            CodCommessa = expectedCodCommessa,
            Contatti = contatti,
            Cup = expectedCup,
            DataModifica = expectedDataModifica,
            DataDocumento = expectedDataDocumento,
            Pec = expectedUpdatedPec,
            TipoCommessa = expectedTipoCommessa,
            IdDocumento = expectedIdDocumento,
            Map = expectedMap,
            SplitPayment = expectedSplitPayment
        };

        actualDatiFatturazione = await _handler.Send(updateRequest);

        ClassicAssert.True(actualDatiFatturazione.NotaLegale == expectedNotaLegale);
        ClassicAssert.True(actualDatiFatturazione.CodCommessa == expectedCodCommessa);
        ClassicAssert.True(actualDatiFatturazione.Contatti!.Count() == 1);
        ClassicAssert.True(actualDatiFatturazione.Contatti!.ToList()[0].Email == expectedContatto);
    }

    [Test]
    public async Task UpdateCommand_ShouldSucceed_WithSameContatti()
    {
        string? expectedCup = "ecup";
        bool expectedNotaLegale = true;
        string? expectedCodCommessa = "ecommmessa";
        DateTime expectedDataDocumento = DateTime.UtcNow.ItalianTime();
        bool? expectedSplitPayment = false;
        string? expectedTipoCommessa = "1";
        string? expectedIdDocumento = "eiddocumento";
        string? expectedMap = "emap";
        DateTime expectedDataCreazione = DateTime.UtcNow.ItalianTime();
        string? expectedIdEnte = TestExtensions.GetRandomIdEnte();
        string? expectedPec = "pippo@pec.it";
        string? expectedProdotto = "prod-pn";
        var authInfo = TestExtensions.GetAuthInfo(expectedIdEnte, expectedProdotto);
        var expectedContatto = "pippo@gmail.com";
        List<DatiFatturazioneContattoCreateCommand> contatti =
        [
            new DatiFatturazioneContattoCreateCommand()
            {
                Email = expectedContatto
            },
        ];

        var request = new DatiFatturazioneCreateCommand(authInfo)
        {
            NotaLegale = expectedNotaLegale,
            CodCommessa = expectedCodCommessa,
            Contatti = contatti,
            Cup = expectedCup,
            DataCreazione = expectedDataCreazione,
            DataDocumento = expectedDataDocumento,
            Pec = expectedPec,
            TipoCommessa = expectedTipoCommessa,
            IdDocumento = expectedIdDocumento,
            Map = expectedMap,
            SplitPayment = expectedSplitPayment
        };

        var actualDatiFatturazione = await _handler.Send(request);
        ClassicAssert.IsNotNull(actualDatiFatturazione);

        DateTime expectedDataModifica = DateTime.UtcNow.AddMinutes(1);
        var expectedUpdatedPec = "modified@pec.it";


        var updateRequest = new DatiFatturazioneUpdateCommand(authInfo)
        {
            Id = actualDatiFatturazione.Id,
            NotaLegale = expectedNotaLegale,
            CodCommessa = expectedCodCommessa,
            Contatti = contatti,
            Cup = expectedCup,
            DataModifica = expectedDataModifica,
            DataDocumento = expectedDataDocumento,
            Pec = expectedUpdatedPec,
            TipoCommessa = expectedTipoCommessa,
            IdDocumento = expectedIdDocumento,
            Map = expectedMap,
            SplitPayment = expectedSplitPayment
        };

        actualDatiFatturazione = await _handler.Send(updateRequest);

        ClassicAssert.True(actualDatiFatturazione.NotaLegale == expectedNotaLegale);
        ClassicAssert.True(actualDatiFatturazione.CodCommessa == expectedCodCommessa);
        ClassicAssert.True(actualDatiFatturazione.Contatti!.Count() == 1);
        ClassicAssert.True(actualDatiFatturazione.Contatti!.ToList()[0].Email == expectedContatto);
    }
}