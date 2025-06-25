using System.Text.Json.Serialization;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Messaggi;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Commands;

public class ContestazioneReportCreateCommand(IAuthenticationInfo? authenticationInfo) : IRequest<bool?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; private set; } = authenticationInfo;
    [JsonPropertyName("unique_id")]
    public string? UniqueId { get; set; }
    public string? Json
    {
        get
        {
            return new
            {
                unique_id = this.UniqueId,
                internal_organization_id = this.InternalOrganizationId,
                contract_id = this.ContractId,
                utente_id = this.UtenteId,
                anno = this.Anno,
                mese = this.Mese
            }.SerializeAlsoReadOnly();
        }
    }


    public int Anno { get; set; }
    public int? Mese { get; set; }
    public string? InternalOrganizationId { get; set; }
    public string? ContractId { get; set; }
    public string? UtenteId { get; set; }
    public string? Prodotto { get; set; }
    public DateTime DataInserimento { get; set; } = DateTime.UtcNow.ItalianTime();
    public short Stato { get; set; } = TipologiaStatoMessaggio.CaricamentoFile;
    public DateTime? DataStepCorrente { get; set; }
    public string? NomeDocumento { get; set; }
    public string? LinkDocumento { get; set; }
    public string? Storage { get; set; }
    public string? Hash { get { return Json.GetHashSHA256(); } }
    public string? ContentType { get; set; }
    public string? ContentLanguage { get; set; }
    public int IdTipologiaReport { get; set; }
    public string? FileCaricato { get; set; }
    public bool IsUploadedByEnte { get; set; } // Indica se il file è stato caricato dall'ente, altrimenti da supporto
}