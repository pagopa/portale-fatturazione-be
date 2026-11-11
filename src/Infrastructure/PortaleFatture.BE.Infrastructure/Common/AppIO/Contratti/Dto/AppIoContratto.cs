using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;

/// <summary>
/// Contratto APP IO letto dalla vista [be].[vwAppioContracts]: una riga per contratto
/// (onboardingtokenid). Dall'11/11/2026 (PF-1016) il contratto non è più storicizzato per trimestre.
/// Gli attributi <see cref="HeaderPagoPAAttribute"/> definiscono intestazioni e ordine delle colonne
/// dell'export Excel (rotta api/appio/contracts/download).
/// </summary>
public sealed class AppIoContratto
{
    /// <summary>
    /// Identificativo del contratto (onboardingtokenid).
    /// </summary>
    [HeaderPagoPA(caption: "ContractId", Order = 1)]
    public string? ContractId { get; set; }

    /// <summary>
    /// Identificativo dell'ente (internalistitutionid): è il valore del filtro "Nome Ente".
    /// </summary>
    [HeaderPagoPA(caption: "InstitutionId", Order = 2)]
    public string? InstitutionId { get; set; }

    /// <summary>
    /// Nome dell'ente titolare del contratto.
    /// </summary>
    [HeaderPagoPA(caption: "Name", Order = 3)]
    public string? Name { get; set; }

    /// <summary>
    /// Prodotto del contratto.
    /// </summary>
    [HeaderPagoPA(caption: "Prodotto", Order = 4)]
    public string? Prodotto { get; set; }

    /// <summary>
    /// Tipo dell'ente (es. PA, GSP): è il valore del filtro "Tipo Ente".
    /// </summary>
    [HeaderPagoPA(caption: "InstitutionType", Order = 5)]
    public string? InstitutionType { get; set; }

    /// <summary>
    /// Indirizzo PEC dell'ente; null se assente nel contratto.
    /// </summary>
    [HeaderPagoPA(caption: "EmailPec", Order = 6)]
    public string? EmailPec { get; set; }

    /// <summary>
    /// Stato del contratto (es. ACTIVE, SUSPENDED).
    /// </summary>
    [HeaderPagoPA(caption: "ContractState", Order = 7)]
    public string? ContractState { get; set; }
}
