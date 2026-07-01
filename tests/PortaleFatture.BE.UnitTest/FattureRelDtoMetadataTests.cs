using System.Reflection;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Unit test puri (nessun DB) sui metadati dei DTO del report REL: verificano che la colonna
/// "Rel Non Firmata" sia esposta SOLO da FattureRelSospeseExcelDto e NON da FattureRelExcelDto
/// (report emesse non sospese). La visibilità colonna nell'Excel deriva da [HeaderAttributev2].
/// </summary>
public class FattureRelDtoMetadataTests
{
    private const string RelNonFirmataCaption = "Rel Non Firmata";

    private static List<string> HeaderCaptions(Type t) =>
        t.GetProperties()
         .Select(p => (HeaderAttributev2?)Attribute.GetCustomAttribute(p, typeof(HeaderAttributev2)))
         .Where(a => a != null)
         .Select(a => a!.Caption!)
         .ToList();

    [Test]
    public void FattureRelExcelDto_ShouldNotExpose_RelNonFirmataColumn()
    {
        Assert.That(typeof(FattureRelExcelDto).GetProperty("RelNonFirmata"), Is.Null,
            "FattureRelExcelDto (non sospese) non deve avere la proprietà RelNonFirmata.");
        Assert.That(HeaderCaptions(typeof(FattureRelExcelDto)), Does.Not.Contain(RelNonFirmataCaption),
            "FattureRelExcelDto (non sospese) non deve esporre la colonna 'Rel Non Firmata'.");
    }

    [Test]
    public void FattureRelSospeseExcelDto_ShouldExpose_RelNonFirmataColumn()
    {
        var prop = typeof(FattureRelSospeseExcelDto).GetProperty("RelNonFirmata");
        Assert.That(prop, Is.Not.Null, "FattureRelSospeseExcelDto deve avere la proprietà RelNonFirmata.");

        var attr = (HeaderAttributev2?)Attribute.GetCustomAttribute(prop!, typeof(HeaderAttributev2));
        Assert.That(attr, Is.Not.Null, "RelNonFirmata deve avere [HeaderAttributev2].");
        Assert.That(attr!.Caption, Is.EqualTo(RelNonFirmataCaption));
        Assert.That(attr.Order, Is.EqualTo(23));
    }

    [Test]
    public void FattureRelSospeseExcelDto_ShouldInherit_BaseColumns()
    {
        Assert.That(typeof(FattureRelExcelDto).IsAssignableFrom(typeof(FattureRelSospeseExcelDto)), Is.True,
            "FattureRelSospeseExcelDto deve derivare da FattureRelExcelDto.");

        var captions = HeaderCaptions(typeof(FattureRelSospeseExcelDto));
        Assert.That(captions, Does.Contain("IdEnte"));
        Assert.That(captions, Does.Contain("Tipo Contratto"));
        Assert.That(captions, Does.Contain(RelNonFirmataCaption));
    }
}
