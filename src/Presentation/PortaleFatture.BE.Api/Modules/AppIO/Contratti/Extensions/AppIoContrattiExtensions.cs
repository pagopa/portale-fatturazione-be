using PortaleFatture.BE.Api.Modules.AppIO.Contratti.Request;
using PortaleFatture.BE.Api.Modules.AppIO.Contratti.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries;

namespace PortaleFatture.BE.Api.Modules.AppIO.Contratti.Extensions;

public static class AppIoContrattiExtensions
{

    /// <summary>
    /// Converte una AppIoContrattiQuartersRequest in una AppIoContrattiQuartersQuery, includendo le informazioni di autenticazione.
    /// </summary>
    /// <param name="req">La richiesta con l'anno di cui recuperare i trimestri.</param>
    /// <param name="authInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
    /// <returns>Una AppIoContrattiQuartersQuery valorizzata con i dati della richiesta e le informazioni di autenticazione.</returns>
    public static AppIoContrattiQuartersQuery Map(this AppIoContrattiQuartersRequest req, AuthenticationInfo authInfo)
    {
        return new AppIoContrattiQuartersQuery(authInfo)
        {
            Year = req.Year
        };
    }

    /// <summary>
    /// Converte una AppIoContrattiRequest in una AppIoContrattiQueryGetByRicerca, includendo le informazioni di autenticazione e i parametri facoltativi di paginazione.
    /// </summary>
    /// <param name="req">La richiesta con gli id dei contratti e i trimestri da cercare.</param>
    /// <param name="authInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
    /// <param name="page">Il numero di pagina, facoltativo.</param>
    /// <param name="pageSize">La dimensione della pagina, facoltativa.</param>
    /// <returns>Una AppIoContrattiQueryGetByRicerca valorizzata con i dati della richiesta, le informazioni di autenticazione e gli eventuali parametri di paginazione.</returns>
    public static AppIoContrattiQueryGetByRicerca Map(this AppIoContrattiRequest req, AuthenticationInfo authInfo, int? page = null, int? pageSize = null)
    {
        return new AppIoContrattiQueryGetByRicerca(authInfo)
        {
            Page = page,
            Size = pageSize,
            ContractIds = req.ContractIds.IsNullNotAny() ? null : req.ContractIds,
            YearQuarter = req.Quarters.IsNullNotAny() ? null : req.Quarters
        };
    }

    /// <summary>
    /// Converte una AppIoContrattiNameRequest in una AppIoContrattiQueryGetByName, includendo le informazioni di autenticazione.
    /// </summary>
    /// <param name="req">La richiesta con il nome e i trimestri in cui cercare i contratti.</param>
    /// <param name="authInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
    /// <returns>Una AppIoContrattiQueryGetByName valorizzata con i dati della richiesta e le informazioni di autenticazione.</returns>
    public static AppIoContrattiQueryGetByName Map(this AppIoContrattiNameRequest req, AuthenticationInfo authInfo)
    {
        return new AppIoContrattiQueryGetByName(authInfo)
        {
            Name = req.Name,
            YearQuarter = req.Quarters.IsNullNotAny() ? null : req.Quarters
        };
    }

    // 2026_1 -> { Quarter = "Q1", Value = "2026_1" }
    /// <summary>
    /// Converte un elenco di trimestri in una lista di AppIoContrattiQuartersResponse, ricavando il numero del trimestre e formattandolo come "Q" seguito dal numero.
    /// </summary>
    /// <param name="quarters">L'elenco dei trimestri da convertire.</param>
    /// <returns>Una lista di AppIoContrattiQuartersResponse ordinata per valore.</returns>
    public static List<AppIoContrattiQuartersResponse> Map(this IEnumerable<string> quarters)
    {
        return quarters.Select(x => new AppIoContrattiQuartersResponse
        {
            Quarter = "Q" + x[(x.IndexOf('_') + 1)..],
            Value = x
        }).OrderBy(y => y.Value).ToList();
    }

    /// <summary>
    /// Ricava da un elenco di trimestri la lista degli anni distinti, dal più recente al meno recente.
    /// </summary>
    /// <param name="quarters">L'elenco dei trimestri da cui ricavare gli anni.</param>
    /// <returns>La lista degli anni distinti, come stringhe, dal più recente al meno recente.</returns>
    public static List<string> MapYears(this IEnumerable<string> quarters)
    {
        return quarters.Select(x => x.Split("_")[0]).Distinct().OrderByDescending(y => y).ToList();
    }

    /// <summary>
    /// Converte un elenco di AppIoContrattoNome in un elenco di AppIoContrattoNomeResponse.
    /// </summary>
    /// <param name="contratti">L'elenco di AppIoContrattoNome da convertire.</param>
    /// <returns>Un elenco di AppIoContrattoNomeResponse.</returns>
    public static IEnumerable<AppIoContrattoNomeResponse> Map(this IEnumerable<AppIoContrattoNome> contratti)
    {
        return contratti.Select(x => new AppIoContrattoNomeResponse
        {
            ContractId = x.ContractId,
            Name = x.Name,
            YearQuarter = x.YearQuarter
        });
    }

    /// <summary>
    /// Converte un AppIoContrattiListDto in un AppIoContrattiListResponse, con il conteggio totale e l'elenco degli AppIoContrattoResponse.
    /// </summary>
    /// <param name="dto">L'AppIoContrattiListDto da convertire.</param>
    /// <returns>Un AppIoContrattiListResponse.</returns>
    public static AppIoContrattiListResponse Map(this AppIoContrattiListDto dto)
    {
        return new AppIoContrattiListResponse
        {
            Count = dto.Count,
            Contratti = dto.Contratti?.Select(x => new AppIoContrattoResponse
            {
                ContractId = x.ContractId,
                Name = x.Name,
                TaxCode = x.TaxCode,
                VatCode = x.VatCode,
                VatGroup = x.VatGroup,
                SdiCode = x.SdiCode,
                YearMonth = x.YearMonth,
                YearQuarter = x.YearQuarter
            })
        };
    }
}
