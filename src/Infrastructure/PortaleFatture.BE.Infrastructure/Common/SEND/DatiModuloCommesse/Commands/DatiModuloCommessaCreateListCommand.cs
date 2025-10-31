using System.Text.Json.Serialization;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Commands;

public class DatiModuloCommessaCreateListCommand(IAuthenticationInfo authenticationInfo) : IRequest<ModuloCommessaDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public int Anno { get; set; } 
    public int Mese { get; set; }
    public List<DatiModuloCommessaCreateCommand>? DatiModuloCommessaListCommand { get; set; }
    public List<ValoriRegioneDto>? ValoriRegioni { get; set; }
}