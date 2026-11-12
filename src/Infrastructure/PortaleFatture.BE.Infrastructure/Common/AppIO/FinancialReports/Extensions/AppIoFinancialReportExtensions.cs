using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Extensions;

public static class AppIoFinancialReportExtensions
{
    /// <summary>
    /// Raggruppa le righe per contratto e trimestre: la testata viene dalla prima riga del gruppo
    /// (ImportoTotale è già il totale del trimestre, calcolato dalla vista), ogni riga diventa una
    /// posizione. Conserva l'ordine delle righe in ingresso, sia dei gruppi sia delle posizioni.
    /// </summary>
    /// <param name="righe">Le righe lette dal DB, ordinate per contratto, trimestre e documento.</param>
    /// <returns>La lista delle righe padre con il loro numero.</returns>
    public static AppIoFinancialReportListDto Raggruppa(this IEnumerable<AppIoFinancialReportRigaDto>? righe)
    {
        var gruppi = new Dictionary<string, AppIoFinancialReportDto>();
        foreach (var riga in righe ?? [])
        {
            var key = $"{riga.ContractId}|{riga.YearQuarter}";
            if (!gruppi.TryGetValue(key, out var gruppo))
            {
                gruppo = new AppIoFinancialReportDto
                {
                    ContractId = riga.ContractId,
                    InstitutionId = riga.InstitutionId,
                    Name = riga.Name,
                    Prodotto = riga.Prodotto,
                    CodiceFiscale = riga.CodiceFiscale,
                    Year = riga.Year,
                    Quarter = riga.Quarter,
                    YearQuarter = riga.YearQuarter,
                    ImportoTotale = riga.ImportoTotale
                };
                gruppi.Add(key, gruppo);
            }

            gruppo.Posizioni.Add(new AppIoFinancialReportPosizioneDto
            {
                Numero = riga.Numero,
                Data = riga.Data,
                GoldUpdatedAt = riga.GoldUpdatedAt,
                CodiceArticolo = riga.CodiceArticolo,
                Quantita = riga.Quantita,
                Importo = riga.Importo,
                CodIva = riga.CodIva,
                Condizioni = riga.Condizioni,
                Causale = riga.Causale
            });
        }

        // Dictionary conserva l'ordine di inserimento finché non si rimuovono chiavi
        return new AppIoFinancialReportListDto
        {
            FinancialReports = [.. gruppi.Values],
            Count = gruppi.Count
        };
    }

    /// <summary>
    /// Compone il dettaglio di un documento: la testata dalla prima riga (le colonne di anagrafica
    /// sono le stesse su tutte le righe dello stesso contratto e trimestre), ogni riga diventa una
    /// posizione, nell'ordine in ingresso.
    /// </summary>
    /// <param name="righe">Le righe del dettaglio, di un solo contratto e trimestre.</param>
    /// <returns>Il dettaglio, oppure null se non c'è nessuna riga.</returns>
    public static AppIoFinancialReportDettaglioDto? Componi(this IEnumerable<AppIoFinancialReportDettaglioRigaDto>? righe)
    {
        var lista = righe?.ToList() ?? [];
        if (lista.Count == 0)
            return null;

        var testata = lista[0];
        return new AppIoFinancialReportDettaglioDto
        {
            Name = testata.Name,
            ContractId = testata.ContractId,
            Prodotto = testata.Prodotto,
            DataCreazione = testata.DataCreazione,
            PricingPlan = testata.PricingPlan,
            PartitaIva = testata.PartitaIva,
            CodiceFiscale = testata.CodiceFiscale,
            UltimoAggiornamento = testata.UltimoAggiornamento,
            YearQuarter = testata.YearQuarter,
            Year = testata.Year,
            Quarter = testata.Quarter,
            ImportoTotale = testata.ImportoTotale,
            Posizioni = [.. lista.Select(x => new AppIoFinancialReportDettaglioPosizioneDto
            {
                Numero = x.Numero,
                TipoDoc = x.TipoDoc,
                Valuta = x.Valuta,
                CodiceAggiuntivo = x.CodiceAggiuntivo,
                DataReport = x.DataReport,
                DataRiferimento = x.DataRiferimento,
                CodiceArticolo = x.CodiceArticolo,
                Quantita = x.Quantita,
                Importo = x.Importo,
                CodIva = x.CodIva,
                Condizioni = x.Condizioni,
                Causale = x.Causale
            })]
        };
    }
}
