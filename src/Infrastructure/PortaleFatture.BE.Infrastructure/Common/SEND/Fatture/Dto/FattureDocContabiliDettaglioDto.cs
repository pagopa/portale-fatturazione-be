
namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

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
    public decimal Iva { get; set; }
}

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


    // public static class FattureDocContabileEmessoDtoExtensions
    // {
    //     public static FatturaDocContabileEmessoDettaglioDto ToGroupedDto(this IEnumerable<FatturaDocContabileEmessoDettaglioRawDto> rawData)
    //     {
    //         var grouped = rawData
    //             .GroupBy(r => r.IdFattura)
    //             .Select(g => g.First().ToDto())
    //             .ToList();

    //         return new FatturaDocContabileEmessoDettaglioDto
    //         {
    //             ImportoSospeso = grouped.Sum(x => x.ImportoSospesoParziale),
    //             Importo = grouped.Sum(x => x.TotaleFatturaCalc ?? 0),
    //             Dettagli = grouped
    //         };
    //     }

    //     private static FatturaDocContabileEmessoSospesaDettaglioDto ToDto(this FatturaDocContabileEmessoDettaglioRawDto raw) => new()
    //     {
    //         // Campi ereditati dalla base
    //         IdFatturaSospesa = raw.IdFattura,
    //         TotaleFattura = raw.TotaleFattura,
    //         ImportoSospesoParziale = raw.ImportoSospesoParziale,
    //         Progressivo = raw.Progressivo,
    //         Prodotto = raw.Prodotto,
    //         PeriodoFatturazione = raw.PeriodoFatturazione,
    //         TipologiaFattura = raw.TipologiaFattura,
    //         IstitutioId = raw.IstitutioId,
    //         OnboardingTokenId = raw.OnboardingTokenId,
    //         RagioneSociale = raw.RagioneSociale,
    //         IdContratto = raw.IdContratto,
    //         TipoDocumento = raw.TipoDocumento,
    //         TipoContratto = raw.TipoContratto,
    //         Divisa = raw.Divisa,
    //         MetodoPagamento = raw.MetodoPagamento,
    //         CausaleFattura = raw.CausaleFattura,
    //         SplitPayment = raw.SplitPayment,
    //         Inviata = raw.Inviata,
    //         Sollecito = raw.Sollecito,
    //         Stato = raw.Stato,
    //         RiferimentoNumeroLinea = raw.RiferimentoNumeroLinea,
    //         IdDocumento = raw.IdDocumento,
    //         DataDocumento = raw.DataDocumento,
    //         NumItem = raw.NumItem,
    //         CodiceCommessaConvenzione = raw.CodiceCommessaConvenzione,
    //         Cup = raw.Cup,
    //         Cig = raw.Cig,
    //         // Campi specifici
    //         DataFattura = raw.DataFattura.ToString("yyyy-MM-dd"),
    //         Posizioni = posizioni
    //     }
    // }
