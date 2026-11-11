using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Request;
using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;

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
            Year = string.IsNullOrWhiteSpace(req.Year) ? null : req.Year
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
            InstitutionIds = req.InstitutionIds.IsNullNotAny() ? null : req.InstitutionIds,
            CodiciFiscali = req.CodiciFiscali.IsNullNotAny() ? null : req.CodiciFiscali,
            YearQuarter = req.Quarters.IsNullNotAny() ? null : req.Quarters,
            Year = string.IsNullOrWhiteSpace(req.Year) ? null : req.Year
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
            InstitutionIds = req.InstitutionIds.IsNullNotAny() ? null : req.InstitutionIds,
            CodiciFiscali = req.CodiciFiscali.IsNullNotAny() ? null : req.CodiciFiscali,
            YearQuarter = req.Quarters.IsNullNotAny() ? null : req.Quarters,
            Year = string.IsNullOrWhiteSpace(req.Year) ? null : req.Year
        };
    }

    /// <summary>
    /// Converte la richiesta del dettaglio nella query corrispondente.
    /// </summary>
    /// <param name="req">La richiesta con contratto e trimestre.</param>
    /// <param name="authInfo">Le informazioni di autenticazione dell'utente.</param>
    /// <returns>La query del dettaglio.</returns>
    public static AppIoFinancialReportQueryGetDettaglio Map(this AppIoFinancialReportsDettaglioRequest req, AuthenticationInfo authInfo)
    {
        return new AppIoFinancialReportQueryGetDettaglio(authInfo)
        {
            ContractId = req.ContractId,
            YearQuarter = req.YearQuarter
        };
    }

    /// <summary>
    /// Dice se la richiesta del dettaglio indica un documento: contratto e trimestre sono entrambi obbligatori.
    /// </summary>
    /// <param name="req">La richiesta, eventualmente assente.</param>
    /// <returns>true se contratto e trimestre sono valorizzati.</returns>
    public static bool IsValida(this AppIoFinancialReportsDettaglioRequest? req)
        => !string.IsNullOrWhiteSpace(req?.ContractId) && !string.IsNullOrWhiteSpace(req?.YearQuarter);

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
    /// Converte gli anni nella lista di stringhe del filtro "Anno", dal più recente.
    /// </summary>
    /// <param name="years">Gli anni.</param>
    /// <returns>Gli anni come stringhe.</returns>
    public static List<string> MapYears(this IEnumerable<int> years)
        => years.OrderByDescending(y => y).Select(y => y.ToString()).ToList();

    /// <summary>
    /// Converte la lista delle righe padre nella risposta della griglia.
    /// </summary>
    /// <param name="dto">Le righe padre con il loro numero.</param>
    /// <returns>La risposta della griglia.</returns>
    public static AppIoFinancialReportsListResponse Map(this AppIoFinancialReportListDto dto)
    {
        return new AppIoFinancialReportsListResponse
        {
            Count = dto.Count,
            FinancialReports = dto.FinancialReports?.Select(x => new AppIoFinancialReportResponse
            {
                Key = x.Key,
                ContractId = x.ContractId,
                InstitutionId = x.InstitutionId,
                Name = x.Name,
                Prodotto = x.Prodotto,
                CodiceFiscale = x.CodiceFiscale,
                Year = x.Year,
                Quarter = x.Quarter,
                YearQuarter = x.YearQuarter,
                ImportoTotale = x.ImportoTotale,
                Posizioni = x.Posizioni.Select(p => new AppIoFinancialReportPosizioneResponse
                {
                    Numero = p.Numero,
                    Data = p.Data,
                    CodiceArticolo = p.CodiceArticolo,
                    Quantita = p.Quantita,
                    Importo = p.Importo,
                    CodIva = p.CodIva,
                    Condizioni = p.Condizioni,
                    Causale = p.Causale
                })
            })
        };
    }

    /// <summary>
    /// Converte il dettaglio nella risposta della pagina di dettaglio.
    /// </summary>
    /// <param name="dto">Il dettaglio.</param>
    /// <returns>La risposta.</returns>
    public static AppIoFinancialReportsDettaglioResponse Map(this AppIoFinancialReportDettaglioDto dto)
    {
        return new AppIoFinancialReportsDettaglioResponse
        {
            Name = dto.Name,
            ContractId = dto.ContractId,
            Prodotto = dto.Prodotto,
            DataCreazione = dto.DataCreazione,
            PricingPlan = dto.PricingPlan,
            PartitaIva = dto.PartitaIva,
            CodiceFiscale = dto.CodiceFiscale,
            UltimoAggiornamento = dto.UltimoAggiornamento,
            YearQuarter = dto.YearQuarter,
            Year = dto.Year,
            Quarter = dto.Quarter,
            ImportoTotale = dto.ImportoTotale,
            Posizioni = dto.Posizioni.Select(p => new AppIoFinancialReportsDettaglioPosizioneResponse
            {
                Numero = p.Numero,
                TipoDoc = p.TipoDoc,
                Valuta = p.Valuta,
                CodiceAggiuntivo = p.CodiceAggiuntivo,
                DataReport = p.DataReport,
                DataRiferimento = p.DataRiferimento,
                CodiceArticolo = p.CodiceArticolo,
                Quantita = p.Quantita,
                Importo = p.Importo,
                CodIva = p.CodIva,
                Condizioni = p.Condizioni,
                Causale = p.Causale
            })
        };
    }
}
