﻿namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload.Response;

public class DatiModuloCommessaResponse
{
    public List<DatiModuloCommessaSimpleResponse>? ModuliCommessa { get; set; }
    public List<TotaleDatiModuloCommessa>? TotaliDatiModuloCommessa { get; set; } 
    public decimal  Totale { get; set; }
}
public class DatiModuloCommessaSimpleResponse
{
    public int NumeroNotificheNazionali { get; set; }
    public int NumeroNotificheInternazionali { get; set; }
    public int IdTipoSpedizione { get; set; } 
    public int TotaleTipoSpedizione { get; set; }
}

public class TotaleDatiModuloCommessaNotifica
{
    public int TotaleNumeroNotificheNazionali { get; set; }
    public int TotaleNumeroNotificheInternazionali { get; set; } 
    public int TotaleNumeroNotificheDaProcessare { get; set; }
}

public class TotaleDatiModuloCommessa
{
    public int IdCategoriaSpedizione { get; set; }  
    public decimal TotaleValoreCategoriaSpedizione { get; set; }
}