using System;
using System.Globalization;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PortaleFatture.BE.Core.Extensions;

public static class DomainExtensions
{
    public static DateTime ActualTime(this DateTime localDate)
    {
        var cultureinfo = new CultureInfo("it-IT");
        var date = localDate.ToString(cultureinfo);
        return DateTime.Parse(date, cultureinfo);
    }

    public static DateTime ActualTime()
    {
        var cultureinfo = new CultureInfo("it-IT");
        var date = DateTime.UtcNow.ToString(cultureinfo);
        return DateTime.Parse(date, cultureinfo);
    }
}
