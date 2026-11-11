namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Response;

/// <summary>
/// Dettaglio di un documento emesso APP IO (contratto e trimestre): anagrafica e righe.
/// </summary>
public class AppIoFinancialReportsDettaglioResponse
{
    /// <summary>Nome dell'ente.</summary>
    public string? Name { get; set; }

    /// <summary>Identificativo del contratto.</summary>
    public string? ContractId { get; set; }

    /// <summary>Prodotto del contratto.</summary>
    public string? Prodotto { get; set; }

    /// <summary>Data di creazione del contratto.</summary>
    public DateTime? DataCreazione { get; set; }

    /// <summary>Piano tariffario.</summary>
    public string? PricingPlan { get; set; }

    /// <summary>Partita IVA.</summary>
    public string? PartitaIva { get; set; }

    /// <summary>Codice fiscale (tributario) dell'ente.</summary>
    public string? CodiceFiscale { get; set; }

    /// <summary>Data dell'ultimo aggiornamento del contratto.</summary>
    public DateTime? UltimoAggiornamento { get; set; }

    /// <summary>Anno e trimestre, nel formato 'AAAA_T'.</summary>
    public string? YearQuarter { get; set; }

    /// <summary>Anno di riferimento.</summary>
    public int? Year { get; set; }

    /// <summary>Trimestre di riferimento (1-4).</summary>
    public int? Quarter { get; set; }

    /// <summary>Totale del contratto nel trimestre, dalla vista.</summary>
    public decimal? ImportoTotale { get; set; }

    /// <summary>Righe dei documenti.</summary>
    public IEnumerable<AppIoFinancialReportsDettaglioPosizioneResponse>? Posizioni { get; set; }
}

/// <summary>
/// Riga di documento del dettaglio.
/// </summary>
public class AppIoFinancialReportsDettaglioPosizioneResponse
{
    /// <summary>Numero del documento.</summary>
    public string? Numero { get; set; }

    /// <summary>Tipo del documento.</summary>
    public string? TipoDoc { get; set; }

    /// <summary>Valuta.</summary>
    public string? Valuta { get; set; }

    /// <summary>Codice aggiuntivo.</summary>
    public string? CodiceAggiuntivo { get; set; }

    /// <summary>Data del documento.</summary>
    public DateTime? DataReport { get; set; }

    /// <summary>Data di riferimento.</summary>
    public DateTime? DataRiferimento { get; set; }

    /// <summary>Codice dell'articolo fatturato.</summary>
    public string? CodiceArticolo { get; set; }

    /// <summary>Quantità.</summary>
    public long? Quantita { get; set; }

    /// <summary>Importo della riga.</summary>
    public decimal? Importo { get; set; }

    /// <summary>Codice IVA.</summary>
    public string? CodIva { get; set; }

    /// <summary>Condizioni di pagamento.</summary>
    public string? Condizioni { get; set; }

    /// <summary>Causale.</summary>
    public string? Causale { get; set; }
}
