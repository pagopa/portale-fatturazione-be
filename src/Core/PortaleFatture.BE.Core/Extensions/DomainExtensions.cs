﻿namespace PortaleFatture.BE.Core.Extensions;

public static class DomainExtensions
{
    private static readonly TimeZoneInfo _italianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
    public static DateTime ItalianTime(this DateTime dateUtcTime)
    { 
       return  TimeZoneInfo.ConvertTimeFromUtc(dateUtcTime, _italianTimeZone); 
    }

    public static (int, int) YearMonth(this DateTime dateTime)
    {
        return (dateTime.Year, dateTime.Month);
    }
}

public static class Time
{
    public static (int, int, int, DateTime) YearMonthDay()
    {
        var adesso = DateTime.UtcNow.ItalianTime();
        return (adesso.Year, adesso.Month, adesso.Day, adesso);
    }

    public static (int, int, int, DateTime) YearMonthDayFatturazione()
    {
        var adesso = DateTime.UtcNow.ItalianTime();
        var refFatturazione = adesso.AddMonths(1); // mese riferimento fatturazione = mese attuale + 1
        return (refFatturazione.Year, refFatturazione.Month, adesso.Day, adesso);
    } 
}