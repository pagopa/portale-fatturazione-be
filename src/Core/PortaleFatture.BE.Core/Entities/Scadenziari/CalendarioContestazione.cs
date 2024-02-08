﻿using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.Scadenziari;

[Table("ContestazioniCalendario")]
public class CalendarioContestazione
{
    [Column("DataFine")]
    public DateTime DataFine { get; set; }
    [Column("DataInizio")]
    public DateTime DataInizio { get; set; }
    [Column("MeseContestazione")]
    public int MeseContestazione { get; set; }
    [Column("AnnoContestazione")]
    public int AnnoContestazione { get; set; }
    public DateTime Adesso { get; set; } 
    public bool Valid { get; set; }
} 