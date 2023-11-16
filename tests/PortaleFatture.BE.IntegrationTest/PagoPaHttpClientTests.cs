using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.CommandHandlers;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.IntegrationTest;

public class PagoPaHttpClientTests
{ 
    private ILogger<PagoPaHttpClientTests> _logger; 
    private IPagoPaHttpClient _client;

    [SetUp]
    public void Setup()
    { 
        _logger = ServiceProvider.GetRequiredService<ILogger<PagoPaHttpClientTests>>();
        _client = ServiceProvider.GetRequiredService<IPagoPaHttpClient>();
    }

    [Test]
    public async Task GetCertificates_ShouldSucceed_ReturnList()
    {
        var certificates = await _client.GetCertificatesAsync();
        Assert.IsNotNull(certificates);
        Assert.True(certificates.Count > 0);
    }
}