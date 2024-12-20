using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Queries;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Extensions;

public static class FinancialReportExtensions
{
    public static KPIPagamentiScontoKPMGQuery Mapv2(this FinancialReportQueryGetKPMGReportExcel request)
    {
        return new KPIPagamentiScontoKPMGQuery(request.AuthenticationInfo)
        {
            ABI = request.ABI,
            ContractIds = request.ContractIds,
            Quarters = request.Quarters,
            MembershipId = request.MembershipId,
            RecipientId = request.RecipientId,
            Year = request.Year
        };
    }

    public static FinancialReportQueryGetFinancialReportExcel Map(this FinancialReportQueryGetKPMGReportExcel request)
    {
        return new FinancialReportQueryGetFinancialReportExcel(request.AuthenticationInfo)
        {
            ABI = request.ABI,
            ContractIds = request.ContractIds,
            Quarters = request.Quarters,
            MembershipId = request.MembershipId,
            RecipientId = request.RecipientId,
            Year = request.Year
        };
    }
    public static IEnumerable<GridFinancialReportDto> Mapv2(this IEnumerable<GridFinancialReportDto> fr)
    {
        foreach (var ff in fr!)
        {
            if (String.IsNullOrEmpty(ff.CodiceArticolo))
            {
                var values = ff.DescrizioneRiga!.Split(",");
                ff.DetailedReport = values[0];
                ff.AgentQuarterReport = values[1];
            }
            else
            {
                ff.Name = string.Empty;
                ff.ContractId = string.Empty;
                ff.TipoDoc = string.Empty;
                ff.CodiceAggiuntivo = string.Empty;
                ff.VatCode = string.Empty;
                ff.Valuta = string.Empty;
                ff.Id = null;
                ff.Numero = string.Empty;
                ff.Data = null;
                ff.Bollo = string.Empty;
                ff.RiferimentoData = null;
                ff.YearQuarter = string.Empty;
                ff.DetailedReport = string.Empty;
                ff.AgentQuarterReport = string.Empty;
                ff.DetailedReport = string.Empty;
                ff.AgentQuarterReport = string.Empty;
            }
        }
        return fr;
    }

    public static GridFinancialReportListDto Map(this GridFinancialReportListDto fr)
    {
        Dictionary<string, GridFinancialReportDto> listaReports = [];
        foreach (var ff in fr.FinancialReports!)
        {
            var key = ff.Key;
            if (!listaReports.TryGetValue(key!, out var actualValue))
                actualValue = ff;

            if (String.IsNullOrEmpty(ff.CodiceArticolo))
                actualValue.Reports = [.. actualValue.DescrizioneRiga!.Split(",")];
            else
            {
                var cc = new GridFinancialReportPosizioniDto
                {
                    ProgressivoRiga = ff.ProgressivoRiga,
                    CodiceArticolo = ff.CodiceArticolo,
                    DescrizioneRiga = ff.DescrizioneRiga,
                    Quantita = ff.Quantita!.Value,
                    Importo = ff.Importo!.Value,
                    CodIva = ff.CodIva,
                    Condizioni = ff.Condizioni,
                    Causale = ff.Causale,
                    IndTipoRiga = ff.IndTipoRiga,
                    Category = ff.Category
                };
                actualValue.Posizioni!.Add(cc);
            }

            listaReports.TryAdd(key, actualValue);
        }

        fr.FinancialReports = [.. listaReports.Values];
        return fr;
    }

    //public static string Key(this GridFinancialReportDto ff)
    //{
    //    return $"{ff.ContractId!}|{ff.YearQuarter}|{ff.Numero}";
    //}
}