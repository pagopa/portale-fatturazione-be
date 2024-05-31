using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;

public class DatiGeneraliDocumento
{
    public string? tipologia { get; set; }
    public string? riferimentoNumeroLinea { get; set; }
    public string? idDocumento { get; set; }
    public string? data { get; set; }
    public string? numItem { get; set; }
    public string? codiceCommessaConvenzione { get; set; }
    public string? CUP { get; set; }
    public string? CIG { get; set; }
}

public class Fattura
{ 
    public decimal totale { get; set; }
    public int numero { get; set; }
    public string? dataFattura { get; set; }
    public string? prodotto { get; set; }
    public string? identificativo { get; set; }
    public string? tipologiaFattura { get; set; }
    public string? istitutioID { get; set; }
    public string? onboardingTokenID { get; set; } 
    public string? ragionesociale { get; set; }
    public string? tipocontratto { get; set; }
    public string? idcontratto { get; set; }

    public string? tipoDocumento { get; set; }
    public string? divisa { get; set; }
    public string? metodoPagamento { get; set; }
    public string? causale { get; set; }
    public bool? split { get; set; }
    public string? sollecito { get; set; }
    public List<DatiGeneraliDocumento>? datiGeneraliDocumento { get; set; }
    public List<Posizioni>? posizioni { get; set; }
}

public class Posizioni
{
    public int numerolinea { get; set; }
    public string? testo { get; set; }
    public string? codiceMateriale { get; set; }
    public int? quantita { get; set; }
    public double prezzoUnitario { get; set; }
    public double imponibile { get; set; }
}

public class FatturaDto
{
    public Fattura? fattura { get; set; }
}


public class FattureListaDto : List<FatturaDto>
{
 
}
