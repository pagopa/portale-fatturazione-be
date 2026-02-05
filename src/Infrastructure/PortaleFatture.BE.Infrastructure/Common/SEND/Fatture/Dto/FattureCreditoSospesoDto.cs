using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto
{
    /// <summary>
    /// DTO finale con struttura gerarchica (fatture -> posizioni)
    /// </summary>
    public sealed class FattureCreditoSospesoDtoList
    {
        public decimal? ImportoSospeso { get; set; }
        public IEnumerable<FattureCreditoSospesoDto>? Dettagli { get; set; }
    }


    /// <summary>
    /// Classe base con i campi comuni della fattura
    /// </summary>
    public abstract class FattureCreditoSospesoBaseDto
    {
        public int IdFattura { get; set; }
        public decimal? ImportoSospesoParziale { get; set; }
        public int Progressivo { get; set; }
        public string? Prodotto { get; set; }
        public string? PeriodoFatturazione { get; set; }
        public string? TipologiaFattura { get; set; }
        public string? IstitutioId { get; set; }
        public string? OnboardingTokenId { get; set; }
        public string? RagioneSociale { get; set; }
        public string? IdContratto { get; set; }
        public string? TipoDocumento { get; set; }
        public string? TipoContratto { get; set; }
        public string? Divisa { get; set; }
        public string? MetodoPagamento { get; set; }
        public string? CausaleFattura { get; set; }
        public bool SplitPayment { get; set; }
        public int Inviata { get; set; }
        public string? Sollecito { get; set; }
        public string? Stato { get; set; }
        public string? RiferimentoNumeroLinea { get; set; }
        public string? IdDocumento { get; set; }
        public DateTime DataDocumento { get; set; }
        public string? NumItem { get; set; }
        public string? CodiceCommessaConvenzione { get; set; }
        public string? Cup { get; set; }
        public string? Cig { get; set; }
    }

    /// <summary>
    /// DTO flat per il mapping diretto dal database (Dapper)
    /// </summary>
    public sealed class FattureCreditoSospesoRawDto : FattureCreditoSospesoBaseDto
    {
        // DataFattura come DateTime per binding DB
        public DateTime DataFattura { get; set; }

        // Campi posizione inline per mapping flat
        public int NumeroLinea { get; set; }
        public string? Testo { get; set; }
        public string? CodiceMateriale { get; set; }
        public int Quantita { get; set; }
        public decimal PrezzoUnitario { get; set; }
        public decimal Imponibile { get; set; }
        public string? PeriodoRiferimento { get; set; }
    }

    /// <summary>
    /// DTO fattura con posizioni raggruppate
    /// </summary>
    public sealed class FattureCreditoSospesoDto : FattureCreditoSospesoBaseDto
    {
        // DataFattura come string formattata per output
        public string? DataFattura { get; set; }

        public IEnumerable<FattureCreditoSospesoPosizioniDto>? Posizioni { get; set; }
    }


    public sealed class FattureCreditoSospesoPosizioniDto
    {
        public int NumeroLinea { get; set; }
        public string? Testo { get; set; }
        public string? CodiceMateriale { get; set; }
        public int Quantita { get; set; }
        public decimal PrezzoUnitario { get; set; }
        public decimal Imponibile { get; set; }
        public string? PeriodoRiferimento { get; set; }
    }

    /// <summary>
    /// Extension per convertire da flat a gerarchico
    /// </summary>
    public static class FattureCreditoSospesoDtoExtensions
    {
        public static FattureCreditoSospesoDtoList ToGroupedDto(this IEnumerable<FattureCreditoSospesoRawDto> rawData)
        {
            var grouped = rawData
                .GroupBy(r => r.IdFattura)
                .Select(g => g.First().ToDto(g.Select(p => p.ToPosizioneDto())))
                .ToList();

            return new FattureCreditoSospesoDtoList
            {
                ImportoSospeso = grouped.Sum(x => x.ImportoSospesoParziale),
                Dettagli = grouped
            };
        }

        private static FattureCreditoSospesoDto ToDto(this FattureCreditoSospesoRawDto raw, IEnumerable<FattureCreditoSospesoPosizioniDto> posizioni) => new()
        {
            // Campi ereditati dalla base
            IdFattura = raw.IdFattura,
            ImportoSospesoParziale = raw.ImportoSospesoParziale,
            Progressivo = raw.Progressivo,
            Prodotto = raw.Prodotto,
            PeriodoFatturazione = raw.PeriodoFatturazione,
            TipologiaFattura = raw.TipologiaFattura,
            IstitutioId = raw.IstitutioId,
            OnboardingTokenId = raw.OnboardingTokenId,
            RagioneSociale = raw.RagioneSociale,
            IdContratto = raw.IdContratto,
            TipoDocumento = raw.TipoDocumento,
            TipoContratto = raw.TipoContratto,
            Divisa = raw.Divisa,
            MetodoPagamento = raw.MetodoPagamento,
            CausaleFattura = raw.CausaleFattura,
            SplitPayment = raw.SplitPayment,
            Inviata = raw.Inviata,
            Sollecito = raw.Sollecito,
            Stato = raw.Stato,
            RiferimentoNumeroLinea = raw.RiferimentoNumeroLinea,
            IdDocumento = raw.IdDocumento,
            DataDocumento = raw.DataDocumento,
            NumItem = raw.NumItem,
            CodiceCommessaConvenzione = raw.CodiceCommessaConvenzione,
            Cup = raw.Cup,
            Cig = raw.Cig,
            // Campi specifici
            DataFattura = raw.DataFattura.ToString("yyyy-MM-dd"),
            Posizioni = posizioni
        };

        private static FattureCreditoSospesoPosizioniDto ToPosizioneDto(this FattureCreditoSospesoRawDto raw) => new()
        {
            NumeroLinea = raw.NumeroLinea,
            Testo = raw.Testo,
            CodiceMateriale = raw.CodiceMateriale,
            Quantita = raw.Quantita,
            PrezzoUnitario = raw.PrezzoUnitario,
            Imponibile = raw.Imponibile,
            PeriodoRiferimento = raw.PeriodoRiferimento
        };
    }

}
