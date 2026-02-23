﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortaleFatture.BE.Core.Entities.SEND.Fatture
{
    public class DocumentoContabileSospeso
    {
      public string? RagioneSociale { get; set; } = string.Empty;
      public string? IdContratto { get; set; } = string.Empty;
      public int Mese { get; set; } = 0;
      public int Anno { get; set; } = 0;
      public decimal Totale { get; set; } = 0;
      public decimal TotaleAnalogico { get; set; } = 0;
      public decimal AnticipoAnalogico { get; set; } = 0;
      public decimal AnticipoDigitale { get; set; } = 0;
      public decimal ImportoSottoSoglia { get; set; } = 0;
      public decimal TotaleDigitale { get; set; }
      public int TotaleNotificheDigitali { get; set; } = 0;
      public int TotaleNotificheAnalogiche { get; set; } = 0;
      public decimal TotaleAnticipo { get; set; } = 0;
      public decimal TotaleStorno { get; set; } = 0;
      public decimal StornoDigitale { get; set; } = 0;
      public decimal StornoAnalogico { get; set; } = 0;
      public string? TipologiaFattura { get; set; } = string.Empty;
    }
}
