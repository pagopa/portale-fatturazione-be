using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;

public sealed class KPMGReportDto
{ 
    public string? Abi { get; set; } 
    public string? Name { get; set; }

    [HeaderPagoPA(caption: "contract_id", Order = 1)]
    public string? ContractId { get; set; }

    [HeaderPagoPA(caption: "Tipo_doc", Order = 2)]
    public string? TipoDoc { get; set; }

    [HeaderPagoPA(caption: "sezionale", Order = 3)]
    public string? Sezionale { get; set; }

    [HeaderPagoPA(caption: "CODICE_AGGIUNTIVO", Order = 4)]
    public string? CodiceAggiuntivo { get; set; }

    [HeaderPagoPA(caption: "Denominazione_destinatario", Order = 5)]
    public string? DenominazioneDestinatario { get; set; }

    [HeaderPagoPA(caption: "indirizzo", Order = 6)]
    public string? Indirizzo { get; set; }

    [HeaderPagoPA(caption: "Citta", Order = 7)]
    public string? Citta { get; set; }

    [HeaderPagoPA(caption: "Prov", Order = 8)]
    public string? Prov { get; set; }

    [HeaderPagoPA(caption: "CAP", Order = 9)]
    public string? Cap { get; set; }

    [HeaderPagoPA(caption: "Stato", Order = 10)]
    public string? Stato { get; set; }

    [HeaderPagoPA(caption: "vat_code", Order = 11)]
    public string? VatCode { get; set; }

    [HeaderPagoPA(caption: "Cod_fisc", Order = 11)]
    public string? CodFisc { get; set; }


    [HeaderPagoPA(caption: "Valuta", Order = 12)]
    public string? Valuta { get; set; }

    [HeaderPagoPA(caption: "ID", Order = 13)]
    public int? Id { get; set; }

    [HeaderPagoPA(caption: "Numero", Order = 14)]
    public string? Numero { get; set; }

    [HeaderPagoPA(caption: "Data", Order = 15)]
    public DateTime? Data { get; set; }

    [HeaderPagoPA(caption: "Bollo", Order = 16)]
    public string? Bollo { get; set; }

    [HeaderPagoPA(caption: "CODIFICA_ART", Order = 17)]
    public string? CodificaArt { get; set; } 
   
    [HeaderPagoPA(caption: "Progressivo_Riga", Order = 18)]
    public int ProgressivoRiga { get; set; } 
 
    [HeaderPagoPA(caption: "Codice_articolo", Order = 19)]
    public string? CodiceArticolo { get; set; }

    [HeaderPagoPA(caption: "Descrizione_riga", Order = 20)]
    public string? DescrizioneRiga { get; set; }

    [HeaderPagoPA(caption: "Prezzo_unit", Order = 21)]
    public string? PrezzoUnit { get; set; }
 
    [HeaderPagoPA(caption: "Q_ta", Order = 22)]
    public int? Quantita { get; set; } 
 
    [HeaderPagoPA(caption: "Importo", Order = 23)]
    public decimal? Importo { get; set; }
 
    [HeaderPagoPA(caption: "Cod_iva", Order = 24)]
    public string? CodIva { get; set; }

    [HeaderPagoPA(caption: "Percent_IVA", Order = 25)]
    public string? PercentIva { get; set; }

    [HeaderPagoPA(caption: "IVA", Order = 26)]
    public string? Iva { get; set; }
 
    [HeaderPagoPA(caption: "Condizioni", Order = 16)]
    public string? Condizioni { get; set; }
 
    [HeaderPagoPA(caption: "CAUSALE", Order = 17)]
    public string? Causale { get; set; }
 
    [HeaderPagoPA(caption: "ind_tipo_riga", Order = 18)]
    public int IndTipoRiga { get; set; }

    [HeaderPagoPA(caption: "Codice_SDI", Order = 19)]
    public string? CodiceSdi { get; set; }

    [HeaderPagoPA(caption: "NUM_DOC_RIF", Order = 20)]
    public string? NumDocRif { get; set; }

    [HeaderPagoPA(caption: "DATA_DOC_RIF", Order = 21)]
    public string? DataDocRif { get; set; }

    [HeaderPagoPA(caption: "TIPO_DOC_RIF", Order = 22)]
    public string? TipoDocRif { get; set; }

    [HeaderPagoPA(caption: "Tipo_dato", Order = 23)]
    public string? TipoDato { get; set; }

    [HeaderPagoPA(caption: "Riferimento_testo", Order = 24)]
    public string? RiferimentoTesto { get; set; }

    [HeaderPagoPA(caption: "Riferimento_numero", Order = 25)]
    public string? RiferimentoNumero { get; set; }

    [HeaderPagoPA(caption: "Riferimento_data", Order = 26)]
    public DateTime? RiferimentoData { get; set; }

    [HeaderPagoPA(caption: "Riferimento_data", Order = 27)]
    public string? Anno { get; set; }

    [HeaderPagoPA(caption: "Rif_Fattura", Order = 28)]
    public string? RifFattura { get; set; }

    [HeaderPagoPA(caption: "Sconto", Order = 29)]
    public string? Sconto { get; set; }

    [HeaderPagoPA(caption: "Data_limite_pagamento", Order = 30)]
    public string? DataLimitePagamento { get; set; }

    [HeaderPagoPA(caption: "Banca", Order = 31)]
    public string? Banca { get; set; }

    [HeaderPagoPA(caption: "Iban_riferimento_per_pagamento", Order = 32)]
    public string? IbanRiferimentoPerPagamento { get; set; }

    [HeaderPagoPA(caption: "CIG", Order = 33)]
    public string? Cig { get; set; }

    [HeaderPagoPA(caption: "indirizzo_pec", Order = 34)]
    public string? IndirizzoPec { get; set; }

    [HeaderPagoPA(caption: "CUP", Order = 33)]
    public string? Cup { get; set; }

    [HeaderPagoPA(caption: "TIPO_DOC_RIF1", Order = 33)]
    public string? TipoDocRif1 { get; set; } 
    public string? YearQuarter { get; set; } 
}
