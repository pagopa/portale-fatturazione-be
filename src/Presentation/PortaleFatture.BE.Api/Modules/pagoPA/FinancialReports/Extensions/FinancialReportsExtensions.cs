using System.Data;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Office2010.CustomUI;
using Microsoft.IdentityModel.Logging;
using PortaleFatture.BE.Api.Modules.pagoPA.FinancialReports.Dto;
using PortaleFatture.BE.Api.Modules.pagoPA.FinancialReports.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries;
using PortaleFatture.BE.Infrastructure.Gateway.Storage.pagoPA;

namespace PortaleFatture.BE.Api.Modules.pagoPA.FinancialReports.Extensions;

public static class FinancialReportsExtensions
{
    private static readonly HttpClient _client = new();

    public static string FileExistsAsync(this string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("URI non valido", nameof(uri));
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Head, uri);
            var response = _client.Send(request);
            return response.IsSuccessStatusCode ? uri : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static FinancialReportsQuarterByIdResponse Map(this GridFinancialReportListDto reports, PSPListDto psps, IDocumentStorageSASService sasService)
    {
        var financialReport = reports.FinancialReports!.FirstOrDefault();
        if (financialReport!.Reports!.Where(x => x.Contains(DocumentiSASStorageKey.S3Prefix)).FirstOrDefault() != null)
            financialReport!.Reports = financialReport.Reports!.Select(x => sasService.GetSASToken(DocumentiSASStorageKey.Deserialize(x))).ToList();

        var psp = psps.PSPs == null ? null : psps.PSPs!.FirstOrDefault();
        List<string> checkedReports = [];
        foreach (var report in financialReport!.Reports!)
            checkedReports.Add(report.FileExistsAsync());

        financialReport!.Reports = checkedReports;
        return new FinancialReportsQuarterByIdResponse()
        {
            Report = financialReport,
            PSP = psp
        };
    }

    public static FinancialReportQueryGetByRicerca Map(this FinancialReportsRequest req, AuthenticationInfo authInfo)
    {
        return new FinancialReportQueryGetByRicerca(authInfo)
        {
            ContractIds = req.ContractIds.IsNullNotAny() ? null : req.ContractIds,
            MembershipId = req.MembershipId,
            RecipientId = req.RecipientId,
            ABI = req.ABI,
            Quarters = req.Quarters,
            Year = req.Year
        };
    }

    private static DetailReportDto FromKey(this string key)
    {
        var split = key.Split("|");
        return new DetailReportDto()
        {
            ContractId = split[0],
            YearQuarter = split[1],
            Numero = split[2]
        };
    }

    public static FinancialReportQueryGetByRicerca Map(this FinancialReportsPSPRequest req, AuthenticationInfo authInfo)
    {
        var detail = req.Key!.FromKey();
        return new FinancialReportQueryGetByRicerca(authInfo)
        {
            ContractIds = [detail.ContractId!],
            Quarters = [detail.YearQuarter!],
            Numero = detail.Numero!
        };
    }

    public static PSPQueryGetByRicerca Mapv2(this FinancialReportsPSPRequest req, AuthenticationInfo authInfo, string[]? quarters)
    {
        var detail = req.Key!.FromKey();
        return new PSPQueryGetByRicerca(authInfo)
        {
            ContractIds = [detail.ContractId!],
            YearQuarter = quarters
        };
    }

    public static FinancialReportQueryGetKPMGReportExcel Mapv2(this FinancialReportsRequest req, AuthenticationInfo authInfo)
    {
        return new FinancialReportQueryGetKPMGReportExcel(authInfo)
        {
            ContractIds = req.ContractIds.IsNullNotAny() ? null : req.ContractIds,
            MembershipId = req.MembershipId,
            RecipientId = req.RecipientId,
            ABI = req.ABI,
            Quarters = req.Quarters,
            Year = req.Year
        };
    }

    public static FinancialReportQueryGetByRicercaExcel Mapv3(this FinancialReportsRequest req, AuthenticationInfo authInfo)
    {
        return new FinancialReportQueryGetByRicercaExcel(authInfo)
        {
            ContractIds = req.ContractIds.IsNullNotAny() ? null : req.ContractIds,
            MembershipId = req.MembershipId,
            RecipientId = req.RecipientId,
            ABI = req.ABI,
            Quarters = req.Quarters,
            Year = req.Year
        };
    }

    public static FinancialReportQuartersQuery Map(this FinancialReportsQuartersRequest req, AuthenticationInfo authInfo)
    {
        return new FinancialReportQuartersQuery(authInfo)
        {
            Year = req.Year
        };
    }


    private static string? TableName(this string yearQuarter, int kpmg)
    {
        string? namedQuarter;
        if (yearQuarter.Contains("_1"))
            namedQuarter = "q1";
        else if (yearQuarter.Contains("_2"))
            namedQuarter = "q2";
        else if (yearQuarter.Contains("_3"))
            namedQuarter = "q3";
        else
            namedQuarter = "q4";

        var year = yearQuarter.Split("_")[0];
        return kpmg switch
        {
            0 => $"{namedQuarter}-financial-report",
            1 => $"{namedQuarter}-kpmg-import",
            2 => $"{namedQuarter}-kpi-pagamenti",
            _ => $"{namedQuarter.ToUpper()} {year} check Finance",
        };
    }

    public static DataSet? Map(this KPMGReportListDto aggregateReports)
    {
        DataSet? dataSet = null;
        var financials = aggregateReports.FinancialReports;
        var reports = aggregateReports.KPMGReports;
        var sconti = aggregateReports.Sconti;
        var scontiList = aggregateReports.ScontiLista;

        List<string> tableNames = [];
        if (!financials.IsNullNotAny())
        {
            dataSet ??= new();

            var financialsYearQuarter = financials!.GroupBy(item => new { item.YearQuarter });
            var fYearQuarter = financialsYearQuarter
                          .Select(group => group.Key.YearQuarter)
                          .Distinct();

            if (!fYearQuarter.IsNullNotAny())
            {
                foreach (var yearQuarter in fYearQuarter)
                {
                    var tableName = yearQuarter!.TableName(0);
                    tableNames.Add(tableName!);
                    var selected = financials!.Where(item => item.YearQuarter == yearQuarter);
                    dataSet.Tables.Add(selected!.FillTable(tableName!));
                }
            }
        }

        if (!reports.IsNullNotAny())
        {
            dataSet ??= new();

            var reportsYearQuarter = reports!.GroupBy(item => new { item.YearQuarter, item.Numero });
            var rYearQuarter = reportsYearQuarter
                      .Select(group => group.Key.YearQuarter)
                      .Distinct();

            if (!rYearQuarter.IsNullNotAny())
            {
                foreach (var yearQuarter in rYearQuarter)
                {
                    var tableName = yearQuarter!.TableName(1);
                    tableNames.Add(tableName!);
                    var selected = reports!.Where(item => item.YearQuarter == yearQuarter);
                    dataSet.Tables.Add(selected!.FillTable(tableName!));

                    //checks
                    tableName = yearQuarter!.TableName(3);
                    tableNames.Add(tableName!);
                    var checks = selected.GroupBy(item => new { item.ContractId })
                         .SelectMany(group =>
                         {
                             var result = new List<CheckFinance>();
                             foreach (var item in group)
                             {
                                 var check = new CheckFinance
                                 {
                                     ABI = group.First().Abi,
                                     RagioneSociale = group.First().Name,
                                     CodiceArticolo = item.CodiceArticolo,
                                     Importo = item.Importo,
                                     Numero = group.First().Numero,
                                     Quantità = item.Quantita,
                                     Sconti = null,
                                     Totale = group.Sum(i => i.Importo),
                                     ContractId = item.ContractId 
                                 };

                                 if (item != group.First())
                                 {
                                     check.ABI = string.Empty;
                                     check.RagioneSociale = string.Empty;
                                     check.Numero = string.Empty;
                                     check.Totale = null;
                                     check.Sconti = null;
                                 }
                                 result.Add(check);
                             }
                             return result;
                         });

                    var totalChecks = checks.ToList();

                    foreach (var check in totalChecks)
                    {
                        var item = check;
                        var sconto = sconti != null ? sconti.Where(x => x.YearQuarter == yearQuarter && x.RecipientId == check.ContractId).Select(x=> x.ValueDiscount) : [];
                        if (check.Totale != null)
                        {
                            item.Sconti = sconto.FirstOrDefault();
                            item.TotaleScontato = check.Totale - item.Sconti;
                        } 
                    } 
           
                    totalChecks.Add(new CheckFinance
                    {
                        Numero = "Totale Risultato",
                        Importo = totalChecks.Sum(item => item.Importo),
                        Sconti = totalChecks.Sum(item => item.Sconti),
                        Totale = totalChecks.Sum(item => item.Totale),
                        TotaleScontato = totalChecks.Sum(item => item.TotaleScontato),
                    });
                    dataSet.Tables.Add(totalChecks!.FillTable(tableName!));

                    //kpi-pagamenti
                    var scontoLista = scontiList != null? scontiList.Where(x => x.YearQuarter == yearQuarter) : [];
                    if(!scontoLista.IsNullNotAny())
                    {
                        tableName = yearQuarter!.TableName(2);
                        tableNames.Add(tableName!); 
                        dataSet.Tables.Add(scontoLista!.FillTable(tableName!));
                    } 
                }
            }
        }

        var desiredOrder = tableNames
                 .OrderBy(r => GetQuarterOrder(r))
                 .ThenBy(r => GetReportType(r))
                 .ToList();

        return ReorderTables(dataSet!, desiredOrder);
    }

    private static int GetReportType(string tableName)
    {
        var normalized = tableName.ToLower();

        if (normalized.Contains("financial-report"))
            return 1;
        else if (normalized.Contains("kpmg-import"))
            return 2;
        else if (normalized.Contains("kpi-pagamenti"))
            return 3;
        else
            return 4;
    }

    private static int GetQuarterOrder(string tableName)
    {
        var normalized = tableName.ToLower();
        var match = Regex.Match(normalized, @"q[1-3]");
        if (match.Success)
        {
            switch (match.Value)
            {
                case "q1": return 1;
                case "q2": return 2;
                case "q3": return 3;
                case "q4": return 4;
            }
        }
        return int.MaxValue;
    }

    private static DataSet ReorderTables(DataSet dataSet, List<string> desiredOrder)
    {
        var reorderedTables = new List<DataTable>();

        foreach (var tableName in desiredOrder)
        {
            var table = dataSet!.Tables[tableName];
            if (table != null)
            {
                reorderedTables.Add(table);
            }
        }

        dataSet.Tables.Clear();
        foreach (var table in reorderedTables)
            dataSet.Tables.Add(table);
        return dataSet;
    }
}