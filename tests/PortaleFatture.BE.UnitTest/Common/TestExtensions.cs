using System.Text;

namespace PortaleFatture.BE.UnitTest.Common;

public static class TestExtensions
{
    public static long GetRandomId()
    {
        var rand = new Random();
        return rand.Next(1, int.MaxValue);
    }

    public static string GetRandomIdEnte()
    {
        return Guid.NewGuid().ToString();
    }
} 