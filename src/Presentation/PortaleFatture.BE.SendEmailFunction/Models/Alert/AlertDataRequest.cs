using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PortaleFatture_BE_SendEmailFunction.Models.Alert
{
    public class AlertDataRequest
    {
        [JsonPropertyName("idAlert")]
        public int IdAlert { get; set; }
        [JsonPropertyName("oggetto")]
        public string? Oggetto { get; set; }
        [JsonPropertyName("messaggio")]
        public string? Messaggio { get; set; }

    }
}
