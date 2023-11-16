using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
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
        string? expectedCig = "ecig";
        string? expectedCodCommessa = "ecommmessa";
        DateTimeOffset expectedDataDocumento = DateTime.UtcNow.ItalianTime();
        bool? expectedSplitPayment = false;
        string? expectedTipoCommessa = "1";
        string? expectedIdDocumento = "eiddocumento";
        string? expectedMap = "emap";
        DateTimeOffset expectedDataCreazione = DateTime.UtcNow.ItalianTime();
        string? expectedIdEnte = TestExtensions.GetRandomIdEnte();
        string? expectedPec = "pippo@pec.it";
        string? expectedProdotto = "prodotto1";

        var request = new DatiFatturazioneCreateCommand()
        {
            Cig = expectedCig,
            CodCommessa = expectedCodCommessa,
            Contatti = null,
            Cup = expectedCup,
            DataCreazione = expectedDataCreazione,
            DataDocumento = expectedDataDocumento,
            Pec = expectedPec,
            Prodotto = expectedProdotto,
            TipoCommessa = expectedTipoCommessa,
            IdDocumento = expectedIdDocumento,
            IdEnte = expectedIdEnte,
            Map = expectedMap,
            SplitPayment = expectedSplitPayment
        };

        var actualDatiFatturazione = await _handler.Send(request);
        Assert.IsNotNull(actualDatiFatturazione);

        DateTimeOffset expectedDataModifica = DateTime.UtcNow.AddMinutes(1);
        var expectedUpdatedPec = "modified@pec.it";
        var updateRequest = new DatiFatturazioneUpdateCommand()
        {
            Id = actualDatiFatturazione.Id,
            Cig = expectedCig,
            CodCommessa = expectedCodCommessa,
            Contatti = null,
            Cup = expectedCup,
            DataModifica = expectedDataModifica,
            DataDocumento = expectedDataDocumento,
            Pec = expectedUpdatedPec,
            Prodotto = expectedProdotto,
            TipoCommessa = expectedTipoCommessa,
            IdDocumento = expectedIdDocumento,
            IdEnte = expectedIdEnte,
            Map = expectedMap,
            SplitPayment = expectedSplitPayment
        };

        actualDatiFatturazione = await _handler.Send(updateRequest);

        Assert.True(actualDatiFatturazione.Cig == expectedCig);
        Assert.True(actualDatiFatturazione.CodCommessa == expectedCodCommessa);
        Assert.AreEqual(actualDatiFatturazione.DataCreazione.ToString("{0:dd.MM.yyyy hh:mm.ss}"),expectedDataCreazione.ToString("{0:dd.MM.yyyy hh:mm.ss}"));
        Assert.AreEqual(actualDatiFatturazione.DataModifica!.Value.ToString("{0:dd.MM.yyyy hh:mm.ss}"),expectedDataModifica.ToString("{0:dd.MM.yyyy hh:mm.ss}")); 
        Assert.True(actualDatiFatturazione.Pec == expectedUpdatedPec);
    } 
}