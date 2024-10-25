using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Spreadsheet;
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
    private static readonly HttpClient _client = new HttpClient();

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
        financialReport!.Reports = financialReport.Reports!.Select(x => sasService.GetSASToken(DocumentiSASStorageKey.Deserialize(x))).ToList();
        var psp = psps.PSPs!.FirstOrDefault();
        List<string> checkedReports = [];
        foreach (var report in financialReport!.Reports)
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
            Quarters = req.Quarters
        };
    }

    public static FinancialReportQueryGetByRicerca Map(this FinancialReportsPSPRequest req, AuthenticationInfo authInfo)
    {
        return new FinancialReportQueryGetByRicerca(authInfo)
        {
            ContractIds = [req.ContractId!],
            Quarters = [req.Quarter!]
        };
    }

    public static PSPQueryGetByRicerca Mapv2(this FinancialReportsPSPRequest req, AuthenticationInfo authInfo)
    {
        return new PSPQueryGetByRicerca(authInfo)
        {
            ContractIds = [req.ContractId!]
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
            Quarters = req.Quarters
        };
    }

    public static FinancialReportQueryGetFinancialReportExcel Mapv3(this FinancialReportsRequest req, AuthenticationInfo authInfo)
    {
        return new FinancialReportQueryGetFinancialReportExcel(authInfo)
        {
            ContractIds = req.ContractIds.IsNullNotAny() ? null : req.ContractIds,
            MembershipId = req.MembershipId,
            RecipientId = req.RecipientId,
            ABI = req.ABI,
            Quarters = req.Quarters
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
        var namedQuarter = yearQuarter switch
        {
            "2024_1" => "q1",
            "2024_2" => "q2",
            "2024_3" => "q3",
            _ => "q4",
        };
        var year = yearQuarter.Split("_")[0];
        return kpmg switch
        {
            0 => $"{namedQuarter}-financial-report",
            1 => $"{namedQuarter}-kpmg-import",
            _ => $"{namedQuarter.ToUpper()} {year} check Finance",
        };
    }

    public static DataSet? Map(this KPMGReportListDto aggregateReports)
    {
        DataSet? dataSet = null;
        var financials = aggregateReports.FinancialReports;
        var reports = aggregateReports.KPMGReports;

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

            var reportsYearQuarter = reports!.GroupBy(item => new { item.YearQuarter });
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
                    tableName = yearQuarter!.TableName(2);
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
                                     Totale = item.CodiceArticolo!.Contains("BOLLO") ? 0 : group.Sum(i => i.Importo),
                                 };

                                 if (item != group.First())
                                 {
                                     check.ABI = string.Empty;
                                     check.RagioneSociale = string.Empty;
                                     check.Numero = string.Empty;
                                     check.Totale = null;
                                 }
                                 result.Add(check);
                             }
                             return result;
                         });

                    var totalChecks = checks.ToList();
                    totalChecks.Add(new CheckFinance
                    {
                        Numero = "Totale Risultato",
                        Importo = checks.Sum(item => item.Importo) 
                    });
                    dataSet.Tables.Add(totalChecks!.FillTable(tableName!));
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
        else
            return 3;
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