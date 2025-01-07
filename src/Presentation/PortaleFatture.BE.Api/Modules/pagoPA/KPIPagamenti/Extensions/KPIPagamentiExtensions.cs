using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Dto;

namespace PortaleFatture.BE.Api.Modules.pagoPA.KPIPagamenti.Extensions;

public static class KPIPagamentiExtensions
{
    public static IEnumerable<KPIPagamentiScontoExcelDto> Map(this IEnumerable<GridKPIPagamentiScontoReportDto> sconti)
    {
        List<KPIPagamentiScontoExcelDto> pagamenti = [];
        foreach (var sc in sconti)
        {
            pagamenti.Add(new KPIPagamentiScontoExcelDto()
            {
                Name = sc.Name,
                YearQuarter = sc.YearQuarter,
                RecipientId = sc.RecipientId,
                Totale = sc.Totale,
                TotaleSconto = sc.TotaleSconto,
                Link = sc.Link,
                KpiList = sc.KpiList,
                PercSconto = null,
                KpiOk = null,
                TrxTotal = sc!.Posizioni!.Sum(x=>x.TrxTotal==null?0: x.TrxTotal.Value),
                FlagMQ = sc!.FlagMQ,
            });
            foreach (var pos in sc!.Posizioni!)
            {
                pagamenti.Add(new KPIPagamentiScontoExcelDto()
                {
                    PSPName = pos.PSPName,
                    PspId = pos.PspId,
                    TrxTotal = pos.TrxTotal,
                    Totale = pos.ValueTotal,
                    KpiOk = pos.KpiOk,
                    PercSconto = pos.PercSconto,
                    TotaleSconto = pos.ValueDiscount,
                });
            }
        }
        return pagamenti;
    }
}