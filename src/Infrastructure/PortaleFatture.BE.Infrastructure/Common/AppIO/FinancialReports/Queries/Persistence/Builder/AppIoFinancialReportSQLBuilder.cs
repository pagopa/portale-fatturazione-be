using System.Dynamic;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence.Builder;

/// <summary>
/// Query dei documenti emessi APP IO (modello dati dell'11/11/2026, PF-1016) sulle viste
/// [be].[vwAppioFinalReport] (griglia e download), [be].[vwAppioFinalReportDetails] (dettaglio),
/// [be].[vwAppioFinalReportFiltroAnno] e [be].[vwAppioFinalReportFiltroCodFiscaleEnte] (filtri).
/// </summary>
/// <remarks>
/// Le viste fanno già la join fra appio.messages_final_report e appio.contracts: qui non ci sono
/// join, solo filtri. ⚠️ Le tabelle non hanno PK: se DATA carica più volte lo stesso documento
/// (billing_iteration/run_id diversi), le righe si duplicano anche qui (punto aperto con DATA).
/// </remarks>
public static class AppIoFinancialReportSQLBuilder
{
    private static readonly string _sqlRighe = @"
        SELECT
         f.[onboardingtokenid] AS ContractId
        ,f.[internalistitutionid] AS InstitutionId
        ,f.[institution_description] AS Name
        ,f.[product] AS Prodotto
        ,f.[institution_taxcode] AS CodiceFiscale
        ,f.[year] AS Year
        ,f.[quarter] AS Quarter
        ,f.[trimestre] AS YearQuarter
        ,f.[ImportoTotale] AS ImportoTotale
        ,f.[Numero] AS Numero
        ,f.[Data] AS Data
        ,f.[gold_updated_at] AS GoldUpdatedAt
        ,f.[Codice_articolo] AS CodiceArticolo
        ,f.[Q_ta] AS Quantita
        ,f.[Importo] AS Importo
        ,f.[Cod_iva] AS CodIva
        ,f.[Condizioni] AS Condizioni
        ,f.[CAUSALE] AS Causale
        FROM [be].[vwAppioFinalReport] f
    ";

    /// <summary>
    /// Righe dei documenti emessi, una per riga di documento.
    /// </summary>
    /// <returns>La query SQL, senza WHERE né ORDER BY.</returns>
    public static string SelectRighe() => _sqlRighe;

    /// <summary>
    /// Ordinamento delle righe: per ente e contratto, dal trimestre più recente, poi per documento e
    /// articolo. Il raggruppamento della griglia conserva quest'ordine.
    /// </summary>
    /// <returns>La clausola ORDER BY.</returns>
    public static string OrderByRighe()
        => " ORDER BY f.institution_description, f.onboardingtokenid, f.[year] DESC, f.[quarter] DESC, f.Numero, f.Codice_articolo";

    private static readonly string _sqlDettaglio = @"
        SELECT
         d.[nome_ente] AS Name
        ,d.[onboardingtokenid] AS ContractId
        ,d.[prodotto] AS Prodotto
        ,d.[data_creazione] AS DataCreazione
        ,d.[pricingplan] AS PricingPlan
        ,d.[p_iva] AS PartitaIva
        ,d.[CodTributario] AS CodiceFiscale
        ,d.[ultimo_aggiornamento] AS UltimoAggiornamento
        ,d.[year_quarter] AS YearQuarter
        ,d.[year] AS Year
        ,d.[quarter] AS Quarter
        ,d.[ImportoTotale] AS ImportoTotale
        ,d.[Numero] AS Numero
        ,d.[Tipo_doc] AS TipoDoc
        ,d.[Valuta] AS Valuta
        ,d.[CODICE_AGGIUNTIVO] AS CodiceAggiuntivo
        ,d.[data_report] AS DataReport
        ,d.[data_riferimento] AS DataRiferimento
        ,d.[Codice_articolo] AS CodiceArticolo
        ,d.[Q_ta] AS Quantita
        ,d.[Importo] AS Importo
        ,d.[Cod_iva] AS CodIva
        ,d.[Condizioni] AS Condizioni
        ,d.[CAUSALE] AS Causale
        FROM [be].[vwAppioFinalReportDetails] d
        WHERE d.onboardingtokenid = @ContractId
        AND d.year_quarter = @YearQuarter
        ORDER BY d.Numero, d.Codice_articolo
    ";

    /// <summary>
    /// Righe del dettaglio di un contratto in un trimestre (parametri @ContractId e @YearQuarter).
    /// </summary>
    /// <returns>La query SQL completa.</returns>
    public static string SelectDettaglio() => _sqlDettaglio;

    private static readonly string _sqlQuarters = @"
        SELECT [trimestre]
        FROM [be].[vwAppioFinalReport]
    ";

    /// <summary>
    /// Trimestri distinti in cui ci sono documenti emessi. Va completata con un eventuale filtro e con
    /// <see cref="GroupByQuarters"/>.
    /// </summary>
    /// <returns>La query SQL, senza WHERE né GROUP BY.</returns>
    public static string SelectQuarters() => _sqlQuarters;

    /// <summary>
    /// Raggruppamento e ordinamento dei trimestri, dal più recente. Si ordina per anno e trimestre
    /// numerici, non per la stringa 'AAAA_T'.
    /// </summary>
    /// <returns>Le clausole GROUP BY e ORDER BY.</returns>
    public static string GroupByQuarters() => " GROUP BY [trimestre], [year], [quarter] ORDER BY [year] DESC, [quarter] DESC";

    /// <summary>
    /// Anni distinti dei documenti emessi, dal più recente.
    /// </summary>
    /// <returns>La query SQL completa.</returns>
    public static string SelectYears() => " SELECT [Anno] FROM [be].[vwAppioFinalReportFiltroAnno] ORDER BY [Anno] DESC";

    /// <summary>
    /// Codici fiscali distinti degli enti che hanno almeno un documento emesso, in ordine crescente.
    /// </summary>
    /// <remarks>
    /// Le regole stanno nella vista [be].[vwAppioFinalReportFiltroCodFiscaleEnte], non qui: dal
    /// 12/11/2026 fa la join con i documenti (solo enti con documenti) e scarta i codici fiscali nulli.
    /// </remarks>
    /// <returns>La query SQL completa.</returns>
    public static string SelectCodiciFiscali()
        => " SELECT [CodFiscaleEnte] FROM [be].[vwAppioFinalReportFiltroCodFiscaleEnte] ORDER BY [CodFiscaleEnte]";

    /// <summary>
    /// Percorso sul blob storage del file del financial report di un trimestre (parametri @Year e
    /// @Quarter). Serve solo al backend per scaricare il file: non va esposto al client.
    /// </summary>
    /// <remarks>
    /// Il file è uno per trimestre e file_path è ripetuto uguale su tutte le righe: basta la prima.
    /// L'ordinamento serve solo a rendere stabile il risultato se un giorno i percorsi differissero
    /// (riga aggiornata più di recente, a parità il maggiore in ordine alfabetico).
    /// </remarks>
    /// <returns>La query SQL completa.</returns>
    public static string SelectFilePath() => @"
        SELECT TOP 1 f.[file_path]
        FROM [be].[vwAppioFinalReport] f
        WHERE f.[year] = @Year
        AND f.[quarter] = @Quarter
        AND f.[file_path] IS NOT NULL
        ORDER BY f.[gold_updated_at] DESC, f.[file_path] DESC
    ";

    /// <summary>
    /// Compone la WHERE comune alla griglia e al download "Documenti Contabili".
    /// Trimestri: quelli richiesti; in mancanza, tutti quelli dell'anno; in mancanza anche
    /// dell'anno, il più recente presente.
    /// </summary>
    /// <param name="filtro">Il filtro della richiesta.</param>
    /// <returns>La clausola WHERE e i parametri da passare a Dapper.</returns>
    public static (string Where, ExpandoObject Parameters) Where(IAppIoFinancialReportFiltro filtro)
    {
        List<string> where = [];
        var parameters = new ExpandoObject();
        var p = (IDictionary<string, object?>)parameters;

        if (!filtro.YearQuarter.IsNullNotAny())
        {
            where.Add("f.trimestre IN @YearQuarter");
            p["YearQuarter"] = filtro.YearQuarter;
        }
        else if (!string.IsNullOrEmpty(filtro.Year))
        {
            // trimestre è 'AAAA_T'; '[_]' perché in LIKE il trattino basso è un jolly. Si filtra sulla
            // stringa e non su [year] perché l'anno arriva come testo: un valore non numerico non trova
            // righe invece di far fallire la conversione.
            where.Add("f.trimestre LIKE @Year + '[_]%'");
            p["Year"] = filtro.Year;
        }
        else
            where.Add("f.trimestre = (SELECT TOP 1 trimestre FROM [be].[vwAppioFinalReport] ORDER BY [year] DESC, [quarter] DESC)");

        if (!filtro.InstitutionIds.IsNullNotAny())
        {
            where.Add("f.internalistitutionid IN @InstitutionIds");
            p["InstitutionIds"] = filtro.InstitutionIds;
        }

        if (!filtro.CodiciFiscali.IsNullNotAny())
        {
            where.Add("f.institution_taxcode IN @CodiciFiscali");
            p["CodiciFiscali"] = filtro.CodiciFiscali;
        }

        return (" WHERE " + string.Join(" AND ", where), parameters);
    }
}
