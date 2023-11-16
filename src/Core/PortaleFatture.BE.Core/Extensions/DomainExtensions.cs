using System.ComponentModel.DataAnnotations.Schema;
using PortaleFatture.BE.Core.Exceptions;

namespace PortaleFatture.BE.Core.Extensions;

public static class DomainExtensions
{
    private static readonly TimeZoneInfo _italianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
    public static DateTime ItalianTime(this DateTime dateUtcTime)
    {
        var italianTime = TimeZoneInfo.ConvertTimeFromUtc(dateUtcTime, _italianTimeZone);

        if (italianTime.IsDaylightSavingTime())
            italianTime = italianTime.AddHours(-1);

        return italianTime;
    }

    public static (int, int) YearMonth(this DateTime dateUtcTime)
    { 
        return (dateUtcTime.Year, dateUtcTime.Month);
    } 
}