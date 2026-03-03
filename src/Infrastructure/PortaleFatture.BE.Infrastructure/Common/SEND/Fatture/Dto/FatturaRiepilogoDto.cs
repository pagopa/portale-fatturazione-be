using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto
{
    public class FatturaRiepilogoDto
    {
        public string? IdEnte { get; set; }
        public string? RagioneSociale { get; set; }
        public int? AnnoRiferimento { get; set; }
        public int? MeseRiferimento { get; set; }
        public string? IdTipologiaContratto { get; set; }
        public string? TipologiaContratto { get; set; }
        public decimal? Anticipo { get; set; }
        public bool? AnticipoSospeso { get; set; }
        public decimal? Acconto { get; set; }
        public bool? AccontoSospeso { get; set; }
        public decimal? PrimoSaldo { get; set; }
        public bool? PrimoSaldoSospeso { get; set; }
        public decimal? SecondoSaldo { get; set; }
        public bool? SecondoSaldoSospeso { get; set; }
    }
}
