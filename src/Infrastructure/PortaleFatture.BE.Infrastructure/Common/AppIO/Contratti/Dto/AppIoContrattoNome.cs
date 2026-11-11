namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;

/// <summary>
/// Voce del filtro "Nome Ente" APP IO, letta da [be].[vwAppioContractsFiltroNome]: una per ente.
/// Due enti omonimi restano due voci distinte.
/// </summary>
public sealed class AppIoContrattoNome
{
    /// <summary>
    /// Identificativo dell'ente (internalistitutionid), da rimandare nel filtro InstitutionIds.
    /// </summary>
    public string? InstitutionId { get; set; }

    /// <summary>
    /// Nome dell'ente.
    /// </summary>
    public string? Name { get; set; }
}
