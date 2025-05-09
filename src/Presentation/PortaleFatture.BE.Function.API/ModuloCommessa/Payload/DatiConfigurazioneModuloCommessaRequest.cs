﻿using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

public class DatiConfigurazioneModuloCommessaRequest
{
    public string? Prodotto { get; set; } = "prod-pn";
    public int? idTipoContratto { get; set; } 
    public Session? Session { get; set; } 
} 