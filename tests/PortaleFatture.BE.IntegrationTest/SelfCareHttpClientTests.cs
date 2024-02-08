using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Legacy;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.IntegrationTest;

public class SelfCareHttpClientTests
{
    private ILogger<SelfCareHttpClientTests> _logger;
    private ISelfCareHttpClient _client;
    private IConfiguration _conf;
    [SetUp]
    public void Setup()
    {
        _logger = ServiceProvider.GetRequiredService<ILogger<SelfCareHttpClientTests>>();
        _client = ServiceProvider.GetRequiredService<ISelfCareHttpClient>();
        _conf = ServiceProvider.GetRequiredService<IConfiguration>();
    }

    [Test]
    public async Task GetCertificates_ShouldSucceed_ReturnList()
    {
        var certificates = await _client.GetCertificatesAsync();
        ClassicAssert.IsNotNull(certificates);
        ClassicAssert.True(certificates.Count > 0);
    }

    [Test]
    public void GetSelfCareToken_ShouldSucceed_ReturnJWT()
    {
        var selfCareToken = _conf.GetSection("PortaleFattureOptions:JWT:TestToken").Value;
        var jwt = _client.GetSelfCareTokenAsync(selfCareToken);
        ClassicAssert.NotNull(jwt);
    }
}