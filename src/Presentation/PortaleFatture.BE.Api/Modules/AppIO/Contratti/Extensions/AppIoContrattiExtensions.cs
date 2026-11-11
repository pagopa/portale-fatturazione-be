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
    /// Converte una AppIoContrattiRequest in una AppIoContrattiQueryGetByRicerca, includendo le informazioni di autenticazione e i parametri facoltativi di paginazione.
    /// </summary>
    /// <param name="req">La richiesta con gli enti e i tipi ente da cercare.</param>
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
            InstitutionIds = req.InstitutionIds.IsNullNotAny() ? null : req.InstitutionIds,
            TipiEnte = req.TipiEnte.IsNullNotAny() ? null : req.TipiEnte
        };
    }

    /// <summary>
    /// Converte una AppIoContrattiNameRequest in una AppIoContrattiQueryGetByName, includendo le informazioni di autenticazione.
    /// </summary>
    /// <param name="req">La richiesta con il nome da cercare.</param>
    /// <param name="authInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
    /// <returns>Una AppIoContrattiQueryGetByName valorizzata con i dati della richiesta e le informazioni di autenticazione.</returns>
    public static AppIoContrattiQueryGetByName Map(this AppIoContrattiNameRequest req, AuthenticationInfo authInfo)
    {
        return new AppIoContrattiQueryGetByName(authInfo)
        {
            Name = req.Name
        };
    }

    /// <summary>
    /// Converte un elenco di AppIoContrattoNome in un elenco di AppIoContrattoNomeResponse.
    /// </summary>
    /// <param name="nomi">L'elenco di AppIoContrattoNome da convertire.</param>
    /// <returns>Un elenco di AppIoContrattoNomeResponse.</returns>
    public static IEnumerable<AppIoContrattoNomeResponse> Map(this IEnumerable<AppIoContrattoNome> nomi)
    {
        return nomi.Select(x => new AppIoContrattoNomeResponse
        {
            InstitutionId = x.InstitutionId,
            Name = x.Name
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
                InstitutionId = x.InstitutionId,
                Name = x.Name,
                Prodotto = x.Prodotto,
                InstitutionType = x.InstitutionType,
                EmailPec = x.EmailPec,
                ContractState = x.ContractState
            })
        };
    }
}
