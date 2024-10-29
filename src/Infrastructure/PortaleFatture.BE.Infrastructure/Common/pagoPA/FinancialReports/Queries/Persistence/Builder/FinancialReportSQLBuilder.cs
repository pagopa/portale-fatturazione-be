namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries.Persistence.Builder;

internal static class FinancialReportSQLBuilder
{
    private static string _sqlKPMGReport = @"
SELECT 
       c.name  
	  ,c.abi 
      ,k.[contract_id] as ContractId
      ,[tipo_doc] as TipoDoc
      ,[sezionale]
      ,[codice_aggiuntivo] as CodiceAggiuntivo
      ,[denominazione_destinatario] as DenominazioneDestinatario
      ,[indirizzo]
      ,[citta]
      ,[prov]
      ,[cap]
      ,[stato]
      ,k.[vat_code] as VatCode
      ,[cod_fisc] as CodFisc
      ,[valuta]
      ,[id]
      ,[numero]
      ,[data]
      ,[bollo]
      ,[codifica_art] as CodificaArt
      ,[progressivo_riga] as ProgressivoRiga
      ,k.[codice_articolo] as CodiceArticolo
      ,[descrizione_riga] as DescrizioneRiga
      ,[prezzo_unit] as PrezzoUnit
      ,[q_ta] as Quantita
      ,[importo]
      ,[cod_iva] as CodIva
      ,[percent_iva] as PercentIva
      ,[iva]
      ,[condizioni]
      ,[causale]
      ,[ind_tipo_riga] as IndTipoRiga
      ,[codice_sdi] as CodiceSdi
      ,[num_doc_rif] as NumDocRif
      ,[data_doc_rif] as DataDocRif
      ,[tipo_doc_rif] as TipoDocRif
      ,[tipo_dato] as TipoDato
      ,[riferimento_testo] as RiferimentoTesto
      ,[riferimento_numero] as RiferimentoNumero
      ,[riferimento_data] as RiferimentoData
      ,[anno]
      ,[rif_fattura] as RifFattura
      ,[sconto]
      ,[data_limite_pagamento] as DataLimitePagamento
      ,[banca]
      ,[iban_riferimento_per_pagamento] as IbanRiferimentoPerPagamento
      ,[cig]
      ,[indirizzo_pec] as IndirizzoPec
      ,[cup]
      ,[tipo_doc_rif1] as TipoDocRif1
      ,k.[year_quarter] as YearQuarter
  FROM [ppa].[KPMG] k
	left outer join [ppa].[FinancialReports] r
	ON k.contract_id = r.recipient_id
	and k.year_quarter = r.year_quarter
    and k.codice_articolo = r.codice_articolo
    left outer join ppa.Contracts c
    on k.contract_id = c.contract_id
";

    private static string _sqlFinancialReport = @"
SELECT r.[abi] as ABI
      ,r.[recipient_id] as RecipientId
      ,r.[name]
      ,r.[category]
      ,r.[current_trx] as CurrentTrx
      ,r.[value]
      ,r.[codice_articolo] as CodiceArticolo
      ,r.[year_quarter] as YearQuarter 
  FROM  [ppa].[FinancialReports] r 
    left outer join ppa.Contracts c
    on r.[recipient_id] = c.contract_id
	left outer join [ppa].[KPMG] k
	ON k.contract_id = c.contract_id
	and k.year_quarter = r.year_quarter
    and k.codice_articolo = r.codice_articolo  
";
    private static string _sqlQuarters = @"
SELECT  distinct(year_quarter)
  FROM [ppa].[KPMG]
";

    private static string _sqlCount = @"
SELECT count(DISTINCT(k.contract_id + '|' + k.year_quarter))
  FROM [ppa].[KPMG] k
	left outer join [ppa].[FinancialReports] r
	ON k.contract_id = r.recipient_id
	and k.year_quarter = r.year_quarter
    and k.codice_articolo = r.codice_articolo
    left outer join ppa.Contracts c
    on k.contract_id = c.contract_id
";
    private static string _sql = @"
SELECT 
      ISNULL(r.[name], c.[name]) as Name,  
	  r.category as Category,
      k.[contract_id] as ContractId
     ,k.[tipo_doc] as TipoDoc
     ,k.[codice_aggiuntivo]  as CodiceAggiuntivo
     ,ISNULL(k.vat_code, k.cod_fisc) as VatCode
     ,k.[valuta]
     ,k.[id]
     ,k.[numero]
     ,k.[data]
     ,k.[bollo] 
     ,k.[progressivo_riga] as ProgressivoRiga
     ,k.[codice_articolo] as CodiceArticolo
     ,k.[descrizione_riga] as DescrizioneRiga
     ,k.[q_ta] as Quantita
     ,k.[importo]
     ,k.[cod_iva] as CodIva
     ,k.[condizioni]
     ,k.[causale]
     ,k.[ind_tipo_riga] as IndTipoRiga
     ,k.[riferimento_data] as RiferimentoData 
     ,k.[year_quarter] as YearQuarter
  FROM [ppa].[KPMG] k
	left outer join [ppa].[FinancialReports] r
	ON k.contract_id = r.recipient_id
	and k.year_quarter = r.year_quarter
    and k.codice_articolo = r.codice_articolo
    left outer join ppa.Contracts c
    on k.contract_id = c.contract_id
";
 
    private static string _offSet = " OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY";
    public static string OffSet()
    {
        return _offSet;
    }

    public static string OrderBy()
    {
        return " order by k.contract_id asc, k.year_quarter, k.numero,k.progressivo_riga asc";
    } 
 
    public static string SelectAll()
    {
        return _sql;
    }

    public static string SelectQuarters()
    {
        return _sqlQuarters;
    }

    public static string OrderByQuarters()
    {
        return " order by year_quarter desc";
    }


    public static string SelectAllCount()
    {
        return _sqlCount;
    }

    public static string SelectFinancialReport()
    {
        return _sqlFinancialReport;
    }

    public static string SelectKPMGReport()
    {
        return _sqlKPMGReport;
    }

    public static string OrderByFinancialReport()
    {
        return " order by r.recipient_id asc";
    }

    public static string OrderByKPMGReport()
    {
        return " order by k.contract_id asc";
    }
}