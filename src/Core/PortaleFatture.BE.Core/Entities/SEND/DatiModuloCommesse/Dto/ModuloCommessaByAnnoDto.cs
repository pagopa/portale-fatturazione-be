namespace PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;

public class ModuloCommessaPrevisionaleTotaleDto
{
    public bool Modifica { get; set; }
    public int AnnoValidita { get; set; }
    public int MeseValidita { get; set; }
    public string? IdEnte { get; set; } 
    public string? RagioneSociale { get; set; }
    public long IdTipoContratto { get; set; }
    public string? Stato { get; set; }
    public string? Prodotto { get; set; }
    public decimal? Totale { get; set; }
    public DateTime? DataInserimento { get; set; }
    public DateTime? DataChiusura { get; set; } 
    public DateTime? DataChiusuraLegale { get; set; }
    public decimal? TotaleDigitaleNaz { get; set; }
    public decimal? TotaleDigitaleInternaz { get; set; }
    public decimal? TotaleAnalogicoARNaz { get; set; }
    public decimal? TotaleAnalogicoARInternaz { get; set; }
    public decimal? TotaleAnalogico890Naz { get; set; } 
    public int? TotaleNotificheDigitaleNaz { get; set; }
    public int? TotaleNotificheDigitaleInternaz { get; set; }
    public int? TotaleNotificheAnalogicoARNaz { get; set; }
    public int? TotaleNotificheAnalogicoARInternaz { get; set; }
    public int? TotaleNotificheAnalogico890Naz { get; set; } 
    public int? TotaleNotificheDigitale { get; set; }
    public int? TotaleNotificheAnalogico { get; set; }
    public int? TotaleNotifiche { get; set; }
    public string? Source { get; set; }
    public string? Quarter { get; set; } 
    public List<ValoriRegioneDto>? ValoriRegione { get; set; }  
    public string? TipologiaContratto { get; set; } 
    public DateTime? DataContratto { get; set; }
}

public class ModuloCommessaPrevisionaleByAnnoDto
{
    public int AnnoValidita { get; set; }
    public int MeseValidita { get; set; }
    public int FkIdTipoSpedizione { get; set; }
    public int? NumeroNotificheInternazionali { get; set; }
    public int? NumeroNotificheNazionali { get; set; }
    public decimal? ValoreInternazionali { get; set; }
    public decimal? ValoreNazionali { get; set; }
    public string? Source { get; set; }
    public string? FkIdEnte { get; set; } 
    public string? RagioneSociale { get; set; }
    public int FkIdTipoContratto { get; set; }
    public string? FkProdotto { get; set; }
    public string? FkIdStato { get; set; }
    public DateTime? DataInserimento { get; set; }
    public DateTime? DataChiusura { get; set; } 
    public DateTime? DataChiusuraLegale { get; set; }
    public string? Quarter { get; set; }
    public bool Modifica { get; set; } 
    public string? TipologiaContratto { get; set; } 
    public DateTime? DataContratto { get; set; }
}


public class ModuloCommessaByAnnoDto
{
    public bool Modifica { get; set; }
    public int AnnoValidita { get; set; }
    public int MeseValidita { get; set; }
    public string? IdEnte { get; set; }
    public long IdTipoContratto { get; set; }
    public string? Stato { get; set; }
    public string? Prodotto { get; set; }
    public decimal Totale { get; set; }
    public DateTime DataModifica { get; set; }
    public Dictionary<int, ModuloCommessaMeseTotaleDto>? Totali { get; set; }
}

public class ModuloCommessaMeseTotaleDto
{
    public decimal TotaleCategoria { get; set; }
    public int IdCategoriaSpedizione { get; set; }
    public string? Tipo { get; set; }
}