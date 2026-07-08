using PortaleFatture_BE_SendEmailFunction.Models.pagoPA;

namespace PortaleFatture.BE.UnitTest;

public class PreviewDefaultTests
{
    [Test]
    public void EmailPspDataRequest_DefaultPreview_ShouldBeTrue()
    {
        var req = new EmailPspDataRequest();

        Assert.That(req.Preview, Is.True);
    }

    [Test]
    public void EmailPspDataRequest_NullPreview_ShouldResolveToTrueByDefault()
    {
        var req = new EmailPspDataRequest
        {
            Preview = null
        };

        var effectivePreview = req.Preview ?? true;

        Assert.That(effectivePreview, Is.True);
    }

    [Test]
    public void EmailPspAdjustmentDataRequest_DefaultPreview_ShouldBeTrue()
    {
        var req = new EmailPspAdjustmentDataRequest();

        Assert.That(req.Preview, Is.True);
    }

    [Test]
    public void EmailPspAdjustmentDataRequest_NullPreview_ShouldResolveToTrueByDefault()
    {
        var req = new EmailPspAdjustmentDataRequest
        {
            Preview = null
        };

        var effectivePreview = req.Preview ?? true;

        Assert.That(effectivePreview, Is.True);
    }
}
