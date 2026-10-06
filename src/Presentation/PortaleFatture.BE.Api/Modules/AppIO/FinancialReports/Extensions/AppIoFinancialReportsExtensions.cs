using System.Data;
using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Request;
using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Extensions;

public static class AppIoFinancialReportsExtensions
{
    /// <summary>
    /// Converte la richiesta dei trimestri nella query corrispondente.
    /// </summary>
    /// <param name="req">La richiesta con l'anno facoltativo.</param>
    /// <param name="authInfo">Le informazioni di autenticazione dell'utente.</param>
    /// <returns>La query dei trimestri.</returns>
    public static AppIoFinancialReportQuartersQuery Map(this AppIoFinancialReportsQuartersRequest req, AuthenticationInfo authInfo)
    {
        return new AppIoFinancialReportQuartersQuery(authInfo)
        {
            Year = req.Year
        };
    }

    /// <summary>
    /// Converte la richiesta nella query della griglia.
    /// </summary>
    /// <param name="req">La richiesta con i filtri.</param>
    /// <param name="authInfo">Le informazioni di autenticazione dell'utente.</param>
    /// <returns>La query della griglia.</returns>
    public static AppIoFinancialReportQueryGetByRicerca Map(this AppIoFinancialReportsRequest req, AuthenticationInfo authInfo)
    {
        return new AppIoFinancialReportQueryGetByRicerca(authInfo)
        {
            ContractIds = req.ContractIds.IsNullNotAny() ? null : req.ContractIds,
            YearQuarter = req.Quarters.IsNullNotAny() ? null : req.Quarters,
            Year = req.Year
        };
    }

    /// <summary>
    /// Converte la richiesta nella query del download "Documenti Contabili".
    /// </summary>
    /// <param name="req">La richiesta con i filtri.</param>
    /// <param name="authInfo">Le informazioni di autenticazione dell'utente.</param>
    /// <returns>La query del download.</returns>
    public static AppIoFinancialReportQueryGetByRicercaExcel MapExcel(this AppIoFinancialReportsRequest req, AuthenticationInfo authInfo)
    {
        return new AppIoFinancialReportQueryGetByRicercaExcel(authInfo)
        {
            ContractIds = req.ContractIds.IsNullNotAny() ? null : req.ContractIds,
            YearQuarter = req.Quarters.IsNullNotAny() ? null : req.Quarters,
            Year = req.Year
        };
    }

    /// <summary>
    /// Converte la richiesta nella query del download "Financial Report".
    /// </summary>
    /// <param name="req">La richiesta con i filtri.</param>
    /// <param name="authInfo">Le informazioni di autenticazione dell'utente.</param>
    /// <returns>La query del download.</returns>
    public static AppIoFinancialReportQueryGetDettaglioExcel MapDettaglio(this AppIoFinancialReportsRequest req, AuthenticationInfo authInfo)
    {
        return new AppIoFinancialReportQueryGetDettaglioExcel(authInfo)
        {
            ContractIds = req.ContractIds.IsNullNotAny() ? null : req.ContractIds,
            YearQuarter = req.Quarters.IsNullNotAny() ? null : req.Quarters,
            Year = req.Year
        };
    }

    // 2026_1 -> { Quarter = "Q1", Value = "2026_1" }
    /// <summary>
    /// Converte i trimestri nelle voci del filtro "Trimestre", ordinate per valore.
    /// </summary>
    /// <param name="quarters">I trimestri nel formato 'AAAA_T'.</param>
    /// <returns>Le voci del filtro.</returns>
    public static List<AppIoFinancialReportsQuartersResponse> MapQuarters(this IEnumerable<string> quarters)
    {
        return quarters.Select(x => new AppIoFinancialReportsQuartersResponse
        {
            Quarter = "Q" + x[(x.IndexOf('_') + 1)..],
            Value = x
        }).OrderBy(y => y.Value).ToList();
    }

    /// <summary>
    /// Ricava dai trimestri gli anni distinti, dal più recente.
    /// </summary>
    /// <param name="quarters">I trimestri nel formato 'AAAA_T'.</param>
    /// <returns>Gli anni distinti.</returns>
    public static List<string> MapYears(this IEnumerable<string> quarters)
    {
        return quarters.Select(x => x.Split("_")[0]).Distinct().OrderByDescending(y => y).ToList();
    }

    /// <summary>
    /// Converte la lista dei documenti nella risposta della griglia.
    /// </summary>
    /// <param name="dto">I documenti con il loro numero.</param>
    /// <returns>La risposta della griglia.</returns>
    public static AppIoFinancialReportsListResponse Map(this AppIoFinancialReportListDto dto)
    {
        return new AppIoFinancialReportsListResponse
        {
            Count = dto.Count,
            FinancialReports = dto.FinancialReports?.Select(x => new AppIoFinancialReportResponse
            {
                Key = x.Key,
                Name = x.Name,
                ContractId = x.ContractId,
                TipoDoc = x.TipoDoc,
                VatCode = x.VatCode,
                Valuta = x.Valuta,
                Id = x.Id,
                Numero = x.Numero,
                Data = x.Data,
                YearQuarter = x.YearQuarter,
                Posizioni = x.Posizioni.Select(p => new AppIoFinancialReportPosizioneResponse
                {
                    Category = p.Category,
                    ProgressivoRiga = p.ProgressivoRiga,
                    CodiceArticolo = p.CodiceArticolo,
                    DescrizioneRiga = p.DescrizioneRiga,
                    Quantita = p.Quantita,
                    Importo = p.Importo,
                    CodIva = p.CodIva
                })
            })
        };
    }

    /// <summary>
    /// Compone il workbook del "Financial Report": per ogni trimestre un foglio con le righe del
    /// financial report e uno con le posizioni. Il nome del foglio contiene anche l'anno, così due
    /// trimestri omonimi di anni diversi non collidono.
    /// </summary>
    /// <param name="dto">Righe del financial report e posizioni.</param>
    /// <returns>Il DataSet, oppure null se non c'è nessuna riga.</returns>
    public static DataSet? MapExcel(this AppIoFinancialReportDettaglioDto dto)
    {
        if (dto.FinancialReports.IsNullNotAny() && dto.Posizioni.IsNullNotAny())
            return null;

        var dataSet = new DataSet();

        foreach (var gruppo in (dto.FinancialReports ?? []).GroupBy(x => x.YearQuarter).OrderBy(g => g.Key))
            dataSet.Tables.Add(gruppo.FillTable(gruppo.Key.NomeFoglio("financial-report")));

        foreach (var gruppo in (dto.Posizioni ?? []).GroupBy(x => x.YearQuarter).OrderBy(g => g.Key))
            dataSet.Tables.Add(gruppo.FillTable(gruppo.Key.NomeFoglio("positions")));

        return dataSet;
    }

    // 2026_1 + "positions" -> "2026-q1-positions" (sempre sotto i 31 caratteri di Excel)
    private static string NomeFoglio(this string? yearQuarter, string suffisso)
        => $"{yearQuarter?.Replace("_", "-q")}-{suffisso}";
}
