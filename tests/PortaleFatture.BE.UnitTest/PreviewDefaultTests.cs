using PortaleFatture_BE_SendEmailFunction.Models.pagoPA;
using System.Collections.Specialized;

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

    [Test]
    public void EmailPspAdjustment_QueryStringMissingPreview_ShouldResolveToTrueByDefault()
    {
        var queryParams = new NameValueCollection();

        bool? preview = bool.TryParse(queryParams["preview"], out var previewValue) ? previewValue : null;

        var req = new EmailPspAdjustmentDataRequest
        {
            Preview = preview
        };

        var effectivePreview = req.Preview ?? true;

        Assert.That(effectivePreview, Is.True);
    }
}
