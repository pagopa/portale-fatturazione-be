using System.Text.Json;
using System.Text.Json.Nodes;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Messaggi;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Commands;

public class MessaggioCreateCommand(IAuthenticationInfo? authenticationInfo) : IRequest<bool?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? TipologiaDocumento { get; set; }
    public string? CategoriaDocumento { get; set; }
    public DateTime DataInserimento { get; set; } = DateTime.UtcNow.ItalianTime();
    public short Stato { get; set; } = TipologiaStatoMessaggio.PresaInCarico;
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public string? IdUtente { get; set; } = authenticationInfo!.Id;
    public string? IdEnte { get; set; } = string.IsNullOrWhiteSpace(authenticationInfo!.IdEnte) ? null : authenticationInfo!.IdEnte;
    public string? Json { get; set; }
    public string? Prodotto { get; set; } = Product.ProdPN;
    public string? GruppoRuolo { get; set; } = authenticationInfo!.GruppoRuolo;
    public string? Auth { get; set; } = authenticationInfo!.Auth;
    public bool Lettura { get; set; } = false;
    public string? LinkDocumento { get; set; }
    public string? ContentLanguage { get; set; }
    public string? ContentType { get; set; }
    public long? IdReport { get; set; }
    public string? Hash { get; set; }
}