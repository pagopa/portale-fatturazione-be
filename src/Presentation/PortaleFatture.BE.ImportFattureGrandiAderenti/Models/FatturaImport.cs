using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PortaleFatture.BE.ImportFattureGrandiAderenti.Extensions;

namespace PortaleFatture.BE.ImportFattureGrandiAderenti.Models;

public class FatturaImport
{
    public List<FatturaInterna>? listaFatture { get; set; }
}

public class FatturaInterna()
{
    public Fattura? fattura { get; set; }
}
public class Fattura
{ 
    public string? numero { get; set; }
    public string? dataFattura { get; set; }
    public string? prodotto { get; set; }
    public string? identificativo { get; set; }
    public string? tipologiaFattura { get; set; }
    public string? istitutioID { get; set; }
    public string? onboardingTokenID { get; set; }
    public string? tipoDocumento { get; set; }
    public string? divisa { get; set; }
    public string? metodoPagamento { get; set; }
    public string? causale { get; set; }
    public bool split { get; set; }
    public string? sollecito { get; set; }
    public List<DatiGeneraliDocumento>? datiGeneraliDocumento { get; set; }
    public List<Posizione>? posizioni { get; set; }
}

public class DatiGeneraliDocumento
{
    public string? tipologia { get; set; }
    public string? idDocumento { get; set; }
    public string? numItem { get; set; }
    public string? codiceCommessaConvenzione { get; set; }
    public string? CUP { get; set; }
    public string? CIG { get; set; } 
    public string? data { get; set; }
}

public class Posizione
{
    public int numerolinea { get; set; }
    public string? testo { get; set; }
    public string? codiceMateriale { get; set; }
    public int quantita { get; set; }
  
    public decimal prezzoUnitario { get; set; }
 
    public decimal imponibile { get; set; }
    public string? periodoRiferimento { get; set; }
}