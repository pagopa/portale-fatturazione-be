﻿namespace PortaleFatture.BE.Core.Entities.Scadenziari;

public class Scadenziario
{
    public TipoScadenziario Tipo { get; set; } 
    public int GiornoInizio { get; set; } 
    public int GiornoFine { get; set; }
} 