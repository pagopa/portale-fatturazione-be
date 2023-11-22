using System.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.IntegrationTest;


public class SelfCareTokenServiceTests
{
    private ILogger<SelfCareTokenServiceTests> _logger;
    private ISelfCareTokenService _service;
    private IConfiguration _conf;
    [SetUp]
    public void Setup()
    {
        _logger = ServiceProvider.GetRequiredService<ILogger<SelfCareTokenServiceTests>>();
        _service = ServiceProvider.GetRequiredService<ISelfCareTokenService>();
        _conf = ServiceProvider.GetRequiredService<IConfiguration>();
    }

    [Test]
    public async Task GetSelfCareToken_ShouldSucceed_ReturnJWT()
    {
        var selfCareToken = _conf.GetSection("PortaleFattureOptions:JWT:TestToken").Value;
        var (_, result) = await _service.Validate(selfCareToken!);
        Assert.True(result);
    }

    [Test]
    public async Task GetSelfCareTokenContent_ShouldSucceed_ReturnData()
    {
        var selfCareToken = _conf.GetSection("PortaleFattureOptions:JWT:TestToken").Value;
        var result = await _service.ValidateContent(selfCareToken!);
        Assert.IsNotNull(result);
    }


    [Test]
    public void GetSelfCareToken_ShouldFail_ReturnFalse()
    {
        var selfCareToken = _conf.GetSection("PortaleFattureOptions:JWT:TestToken").Value;
        Assert.ThrowsAsync<SecurityException>(async () => await _service.Validate(selfCareToken!, true));
    }
}