using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Extensions;

internal static class KPIPagamentiExtensions
{
    public static (int, int, int) GetYearMonthQuarter(this string yearQuarter)
    {
        if (string.IsNullOrWhiteSpace(yearQuarter))
            throw new ArgumentException("YearQuarter cannot be null or empty.", nameof(yearQuarter));

        var parts = yearQuarter.Split('_');
        var year = Convert.ToInt32(parts[0]);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var quarter) || quarter < 1 || quarter > 4)
            throw new ArgumentException("Invalid YearQuarter format. Expected format: YYYY_Q (e.g., 2024_1).", nameof(yearQuarter));


        var startMonth = (quarter - 1) * 3 + 1;
        var endMonth = startMonth + 2;

        return (year, startMonth, endMonth);
    }

    public static IEnumerable<GridKPIPagamentiScontoReportDto> Map(this IEnumerable<KPIPagamentiScontoDto> sc)
    {
        Dictionary<string, GridKPIPagamentiScontoReportDto> lista = [];
        foreach (var ff in sc!)
        {
            var key = ff.RecipientId + ff.YearQuarter;

            lista.TryGetValue(key!, out var actualValue);
             
            if (actualValue == null)
            {
                actualValue = new();
                actualValue!.Posizioni = [];
            }

            actualValue!.Posizioni!.Add(ff); 

            actualValue.Name = ff.RecipientName;
            actualValue.RecipientId = ff.RecipientId;
            actualValue.Totale += ff.ValueTotal;
            actualValue.TotaleSconto+= ff.ValueDiscount;
            actualValue.YearQuarter = ff.YearQuarter; 
            actualValue.Link = ff.LinkReport;
            actualValue.KpiList = ff.KpiList;
            actualValue.FlagMQ = ff.FlagMQ;
            lista.TryAdd(key, actualValue);
        }
        return lista.Values.ToList();
    }
}