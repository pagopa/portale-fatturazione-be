
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

/// <summary>
/// Rappresenta i dettagli di una fattura e dei relativi documenti contabili, inclusi identificativi, importi,
/// informazioni contrattuali e dati di relazione tra analogico e digitale.
/// </summary>
/// <remarks>Questa classe viene utilizzata per trasferire i dati dettagliati di una fattura, comprese le
/// informazioni necessarie per la gestione contabile e la riconciliazione tra documenti analogici e digitali. 
/// </remarks>
public class FattureDocContabiliDettaglioDto
{
    public long IdFattura { get; set; }

    public string? RagioneSociale { get; set; }

    public string? TipoDocumento { get; set; }

    public string? IdDocumento { get; set; }

    public string? Cup { get; set; }

    public string? IdEnte { get; set; }

    public string? DataFattura { get; set; }

    public long? Progressivo { get; set; }

    public decimal? TotaleFatturaImponibile { get; set; }

    public string? IdContratto { get; set; }

    public int? Anno { get; set; }

    public int? Mese { get; set; }

    public string? TipologiaFattura { get; set; }

    public string? TipologiaFatturaSospesa { get; set; }

    public decimal RelTotaleAnalogico { get; set; }

    public decimal RelTotaleDigitale { get; set; }

    public int RelTotaleNotificheAnalogiche { get; set; }

    public int RelTotaleNotificheDigitali { get; set; }

    public int RelTotaleNotifiche { get; set; }

    public decimal RelTotale { get; set; }

    public decimal RelTotaleIvatoAnalogico { get; set; }

    public decimal RelTotaleIvatoDigitale { get; set; }

    public decimal RelTotaleIvato { get; set; }

    public bool? Caricata { get; set; }

    public bool? RelFatturata { get; set; }

    public int? FkIdTipoContratto { get; set; }

    public string? TipologiaContratto { get; set; }

    public decimal? AnticipoDigitale { get; set; }

    public decimal? AnticipoAnalogico { get; set; }

    public decimal? AccontoDigitale { get; set; }

    public decimal? AccontoAnalogico { get; set; }

    public decimal? StornoAnalogico { get; set; }
    public decimal? StornoDigitale { get; set; }

    public decimal? TotaleStorni
    {
        get
        {
            return StornoAnalogico + StornoDigitale;
        }
    }
    public decimal Iva { get; set; }
}

/// <summary>
/// Represents the raw detail data for an issued accounting document (fattura) that is suspended, including information
/// about the suspension and related financial details.
/// </summary>
/// <remarks>This DTO extends FattureDocContabiliDettaglioDto to provide additional fields specific to suspended
/// invoices, such as suspension identifiers, payment method, and suspension status. Use this type when handling
/// detailed data for suspended issued invoices in accounting workflows.</remarks>
public class FatturaDocContabileEmessoDettaglioRawDto : FattureDocContabiliDettaglioDto
{
    public string? IdFatturaSospesa { get; set; }

    public string? DataFatturaSospesa { get; set; }

    public string? ProgressivoSospesa { get; set; }

    public string? TipoDocumentoSospesa { get; set; }

    public decimal? TotaleFatturaImponibileSospesa { get; set; }

    public decimal? TotaleFatturaSospesa { get; set; }

    public string? MetodoPagamentoSospesa { get; set; }

    public string? CausaleFatturaSospesa { get; set; }

    public bool SplitPaymentSospesa { get; set; }

    public int InviataSospesa { get; set; }

    public string? SollecitoSospesa { get; set; }
}

/// <summary>
/// Rappresenta i dettagli di una fattura emessa collegata a una sospensione, includendo informazioni sulla fattura
/// sospesa, i relativi importi e lo stato delle notifiche associate.
/// </summary>
/// <remarks>Questa classe estende FattureDocContabiliDettaglioDto aggiungendo proprietà specifiche per la
/// gestione delle fatture sospese e delle relative notifiche. Può essere utilizzata per tracciare lo stato, i totali e
/// le informazioni di pagamento delle fatture sospese collegate a una fattura emessa.</remarks>
public class FatturaDocContabileEmessoDettaglioDto : FattureDocContabiliDettaglioDto
{
    public string? IdFatturaSospesa { get; set; }

    public string? DataFatturaSospesa { get; set; }

    public long? ProgressivoSospesa { get; set; }

    public string? TipoDocumentoSospesa { get; set; }

    public decimal? TotaleFatturaSospesaImponibile { get; set; }

    public decimal? TotaleFatturaSospesa { get; set; }

    public string? MetodoPagamentoSospesa { get; set; }

    public string? CausaleFatturaSospesa { get; set; }

    public bool SplitPaymentSospesa { get; set; }

    public int InviataSospesa { get; set; }

    public string? SollecitoSospesa { get; set; }

    public int? AnnoSospesa { get; set; }

    public int? MeseSospesa { get; set; }

    public decimal? RelTotaleAnalogicoSospeso { get; set; }

    public decimal? RelTotaleDigitaleSospeso { get; set; }

    public int? RelTotaleNotificheAnalogicheSospeso { get; set; }

    public int? RelTotaleNotificheDigitaliSospeso { get; set; }

    public int? RelTotaleNotificheSospeso { get; set; }

    public decimal? RelTotaleSospeso { get; set; }

    public decimal? RelTotaleIvatoAnalogicoSospeso { get; set; }

    public decimal? RelTotaleIvatoDigitaleSospeso { get; set; }

    public decimal? RelTotaleIvatoSospeso { get; set; }

    public bool? CaricataSospeso { get; set; }

    public bool? RelFatturataSospeso { get; set; }

    public IEnumerable<FatturaDocContabileEmessoSospesaDettaglioDto>? FattureSospese { get; set; }
}

/// <summary>
/// Rappresenta i dettagli di una fattura sospesa associata a un documento contabile emesso.
/// </summary>
/// <remarks>Utilizzare questa classe per trasferire le informazioni relative a una fattura sospesa, inclusi
/// identificativi, dati contabili, metodo di pagamento e stato di invio. Tutte le proprietà sono opzionali, salvo
/// diversa indicazione, e possono essere utilizzate per la visualizzazione o l'elaborazione dei dati di fatturazione
/// sospesa.</remarks>
public class FatturaDocContabileEmessoSospesaDettaglioDto
{
    public string? IdFatturaSospesa { get; set; }

    public string? DataFatturaSospesa { get; set; }

    public string? ProgressivoSospesa { get; set; }

    public string? TipoDocumentoSospesa { get; set; }

    public decimal? TotaleFatturaImponibileSospesa { get; set; }

    public decimal? TotaleFatturaSospesa { get; set; }

    public string? MetodoPagamentoSospesa { get; set; }

    public string? CausaleFatturaSospesa { get; set; }

    public bool SplitPaymentSospesa { get; set; }

    public int InviataSospesa { get; set; }

    public string? SollecitoSospesa { get; set; }
}
