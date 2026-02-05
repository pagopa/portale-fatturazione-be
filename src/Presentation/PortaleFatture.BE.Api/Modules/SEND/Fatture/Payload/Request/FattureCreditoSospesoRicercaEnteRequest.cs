using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request
{
    public class FattureCreditoSospesoRicercaEnteRequest()
    {
        public int? Anno { get; set; }
        public int? Mese { get; set; }

        private string[]? _tipologiaFattura;
        public string[]? TipologiaFattura
        {
            get { return _tipologiaFattura; }
            set { _tipologiaFattura = value!.IsNullNotAny() ? null : value; }
        }

        private DateTime[]? _dateFatture;
        public DateTime[]? DateFatture
        {
            get { return _dateFatture; }
            set { _dateFatture = value!.IsNullNotAny() ? null : value; }
        }
        //public DateTime? DataFattura { get; set; }
    }
}
