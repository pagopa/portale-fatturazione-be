using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.Alert.Dto
{
    public class AlertDto
    {
        public int IdAlert { get; set; }
        public int IdGruppo { get; set; }
        public string? Oggetto { get; set;}
        public string? Messaggio { get; set; }
        public DateTime DataInizio { get; set; }
        public DateTime DataFine { get; set; }

        public AlertDto()
        {
        }

        public AlertDto(int IdAlert, int IdGruppo, string Oggetto, string Messaggio, DateTime DataInizio, DateTime DataFine) { 
            this.IdAlert = IdAlert;
            this.IdGruppo = IdGruppo;
            this.Oggetto = Oggetto;
            this.Messaggio = Messaggio;
            this.DataInizio = DataInizio;
            this.DataFine = DataFine;
        }
    }

    public sealed class AlertTracking
    {
        public DateTime EventDate { get; set; }
        public string? Recipient { get; set; }
        public int FkIdAlert { get; set; }
        public bool Sent {  get; set; }
    }
}
