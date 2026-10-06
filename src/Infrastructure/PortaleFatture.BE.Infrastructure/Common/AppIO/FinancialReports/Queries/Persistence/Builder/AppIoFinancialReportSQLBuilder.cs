using System.Dynamic;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence.Builder;

/// <summary>
/// Query dei documenti contabili APP IO (PF-908), sulle viste [be].[vwAppioFinancialReportPositions]
/// (righe dei documenti), [be].[vwAppioFinancialReport] (transazioni e valore per articolo) e
/// [be].[vwAppioContracts]. Stessa struttura del gemello pagoPA, dove le posizioni stanno in
/// [ppa].[KPMG]: sono le posizioni a guidare, le altre due viste le arricchiscono in LEFT JOIN.
/// </summary>
/// <remarks>
/// Le join non moltiplicano le righe perché seguono le chiavi primarie delle tabelle sotto le viste:
/// appio.FinancialReports è (recipient_id, year_quarter, codice_articolo) e appio.Contracts è
/// (contract_id, year_quarter). ⚠️ Se il team Data aggiunge la join con le posizioni dentro
/// vwAppioFinancialReport (c'è un commento in tal senso nella vista), queste query vanno riviste.
/// </remarks>
public static class AppIoFinancialReportSQLBuilder
{
    private const string _from = @"
        FROM [be].[vwAppioFinancialReportPositions] p
        LEFT OUTER JOIN [be].[vwAppioFinancialReport] r
            ON p.contract_id = r.recipient_id
            AND p.year_quarter = r.year_quarter
            AND p.codice_articolo = r.codice_articolo
        LEFT OUTER JOIN [be].[vwAppioContracts] c
            ON p.contract_id = c.contract_id
            AND p.year_quarter = c.year_quarter
    ";

    private static readonly string _sqlRighe = @"
        SELECT
         ISNULL(r.[name], c.[name]) AS Name
        ,r.[category] AS Category
        ,p.[contract_id] AS ContractId
        ,p.[tipo_doc] AS TipoDoc
        ,ISNULL(p.[vat_code], p.[cod_fisc]) AS VatCode
        ,p.[valuta] AS Valuta
        ,p.[id] AS Id
        ,p.[numero] AS Numero
        ,p.[data] AS Data
        ,p.[progressivo_riga] AS ProgressivoRiga
        ,p.[codice_articolo] AS CodiceArticolo
        ,p.[descrizione_riga] AS DescrizioneRiga
        ,p.[q_ta] AS Quantita
        ,p.[importo] AS Importo
        ,p.[cod_iva] AS CodIva
        ,p.[year_quarter] AS YearQuarter
    " + _from;

    /// <summary>
    /// Righe dei documenti contabili (una per posizione), con categoria e nome dell'ente.
    /// </summary>
    /// <returns>La query SQL, senza WHERE né ORDER BY.</returns>
    public static string SelectRighe() => _sqlRighe;

    private static readonly string _sqlPosizioni = @"
        SELECT
         ISNULL(r.[name], c.[name]) AS Name
        ,p.[contract_id] AS ContractId
        ,p.[tipo_doc] AS TipoDoc
        ,p.[vat_code] AS VatCode
        ,p.[cod_fisc] AS CodFisc
        ,p.[valuta] AS Valuta
        ,p.[id] AS Id
        ,p.[numero] AS Numero
        ,p.[data] AS Data
        ,p.[codifica_art] AS CodificaArt
        ,p.[progressivo_riga] AS ProgressivoRiga
        ,p.[codice_articolo] AS CodiceArticolo
        ,p.[descrizione_riga] AS DescrizioneRiga
        ,p.[prezzo_unit] AS PrezzoUnit
        ,p.[q_ta] AS Quantita
        ,p.[importo] AS Importo
        ,p.[cod_iva] AS CodIva
        ,p.[percent_iva] AS PercentIva
        ,p.[iva] AS Iva
        ,p.[codice_sdi] AS CodiceSdi
        ,p.[rif_fattura] AS RifFattura
        ,p.[year_quarter] AS YearQuarter
    " + _from;

    /// <summary>
    /// Posizioni con tutte le colonne della vista, per il foglio "positions" del Financial Report.
    /// </summary>
    /// <returns>La query SQL, senza WHERE né ORDER BY.</returns>
    public static string SelectPosizioni() => _sqlPosizioni;

    /// <summary>
    /// Ordinamento delle righe: per documento, poi per progressivo di riga. Il raggruppamento della
    /// griglia conserva quest'ordine.
    /// </summary>
    /// <returns>La clausola ORDER BY.</returns>
    public static string OrderByRighe() => " ORDER BY p.contract_id, p.year_quarter, p.numero, p.progressivo_riga";

    private static readonly string _sqlValori = @"
        SELECT
         r.[recipient_id] AS RecipientId
        ,r.[name] AS Name
        ,r.[category] AS Category
        ,r.[current_trx] AS CurrentTrx
        ,r.[value] AS Value
        ,r.[codice_articolo] AS CodiceArticolo
        ,r.[year_quarter] AS YearQuarter
        FROM [be].[vwAppioFinancialReport] r
    ";

    /// <summary>
    /// Righe del financial report, per il foglio "financial-report" del Financial Report.
    /// </summary>
    /// <returns>La query SQL, senza WHERE né ORDER BY.</returns>
    public static string SelectValori() => _sqlValori;

    /// <summary>
    /// Ordinamento delle righe del financial report.
    /// </summary>
    /// <returns>La clausola ORDER BY.</returns>
    public static string OrderByValori() => " ORDER BY r.recipient_id, r.year_quarter, r.codice_articolo";

    private static readonly string _sqlQuarters = @"
        SELECT [year_quarter] AS YearQuarter
        FROM [be].[vwAppioFinancialReportPositions]
        GROUP BY [year_quarter]
    ";

    /// <summary>
    /// Trimestri distinti in cui ci sono documenti contabili (dalle posizioni, come pagoPA da KPMG).
    /// </summary>
    /// <returns>La query SQL, che termina con il GROUP BY.</returns>
    public static string SelectQuarters() => _sqlQuarters;

    /// <summary>
    /// Ordinamento dei trimestri, dal più recente.
    /// </summary>
    /// <returns>La clausola ORDER BY.</returns>
    public static string OrderByQuarters() => " ORDER BY year_quarter DESC";

    /// <summary>
    /// Compone la WHERE comune alle tre rotte che leggono i documenti.
    /// Trimestri: quelli richiesti; in mancanza, tutti quelli dell'anno; in mancanza anche
    /// dell'anno, il più recente presente fra le posizioni.
    /// </summary>
    /// <param name="filtro">Il filtro della richiesta.</param>
    /// <param name="alias">L'alias della vista su cui si filtra ("p" per le posizioni, "r" per il financial report).</param>
    /// <param name="colonnaContratto">La colonna del contratto in quella vista (contract_id o recipient_id).</param>
    /// <returns>La clausola WHERE e i parametri da passare a Dapper.</returns>
    public static (string Where, ExpandoObject Parameters) Where(IAppIoFinancialReportFiltro filtro, string alias, string colonnaContratto)
    {
        List<string> where = [];
        var parameters = new ExpandoObject();
        var p = (IDictionary<string, object?>)parameters;

        if (!filtro.YearQuarter.IsNullNotAny())
        {
            where.Add($"{alias}.year_quarter IN @YearQuarter");
            p["YearQuarter"] = filtro.YearQuarter;
        }
        else if (!string.IsNullOrEmpty(filtro.Year))
        {
            // year_quarter è 'AAAA_T'; '[_]' perché in LIKE il trattino basso è un jolly
            where.Add($"{alias}.year_quarter LIKE @Year + '[_]%'");
            p["Year"] = filtro.Year;
        }
        else
            where.Add($"{alias}.year_quarter = (SELECT MAX(year_quarter) FROM [be].[vwAppioFinancialReportPositions])");

        if (!filtro.ContractIds.IsNullNotAny())
        {
            where.Add($"{alias}.{colonnaContratto} IN @ContractIds");
            p["ContractIds"] = filtro.ContractIds;
        }

        return (" WHERE " + string.Join(" AND ", where), parameters);
    }
}
