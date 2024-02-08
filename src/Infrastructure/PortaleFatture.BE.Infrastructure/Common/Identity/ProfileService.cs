using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security;
using MediatR;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Auth.PagoPA;
using PortaleFatture.BE.Core.Auth.SelfCare;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.Infrastructure.Common.Identity;

public class ProfileService(
    ISelfCareTokenService selfCareTokenService,
    IPagoPATokenService pagoPATokenService,
    IMediator handler,
    IPortaleFattureOptions options,
    ILogger<ProfileService> logger) : IProfileService
{
    private readonly ISelfCareTokenService _selfCareTokenService = selfCareTokenService;
    private readonly IPagoPATokenService _pagoPATokenService = pagoPATokenService;
    private readonly ILogger<ProfileService> _logger = logger;
    private readonly IMediator _handler = handler;
    private readonly IPortaleFattureOptions _options = options;
    public async Task<List<AuthenticationInfo>?> GetSelfCareInfo(string? selfcareToken)
    {
        if (selfcareToken == null)
        {
            var msg = "Fatal error with self care exchange token. SelfcareToken missing!";
            _logger.LogError(msg);
            throw new SecurityException(msg);
        }
        var selfcare = await _selfCareTokenService.ValidateContent(selfcareToken, Convert.ToBoolean(_options.SelfCareTimeOut!));
        return await Mapper(selfcare!);
    }

    public async Task<AuthenticationInfo?> GetPagoPAInfo(string? pagoPAIdToken, string? azureADAccessToken)
    {
        if (pagoPAIdToken == null)
        {
            var msg = "Fatal error with pagoPA AzureAD token! AzureAD token missing!";
            _logger.LogError(msg);
            throw new SecurityException(msg);
        }
        var pagoPA = await _pagoPATokenService.ValidateContent(pagoPAIdToken, azureADAccessToken!, Convert.ToBoolean(_options.SelfCareTimeOut!));
        return Mapper(pagoPA!);
    }

    private string Mapper(string mapper)
    {
        return mapper switch
        {
            nameof(Ruolo.OPERATOR) => Ruolo.OPERATOR,
            _ => Ruolo.ADMIN,
        };
    }

    private string MapperGruppi(string gruppo)
    {
        if (gruppo.Contains(GroupRoles.APPROVIGIONAMENTO))
            return Profilo.Approvigionamento;
        else if (gruppo.Contains(GroupRoles.FINANZA))
            return Profilo.Finanza;
        else if (gruppo.Contains(GroupRoles.ASSISTENZA))
            return Profilo.Assistenza;
        else
            throw new DomainException("Gruppo AD sconosciuto!"); 
    } 

    private string MapperGruppoRuolo(string descrizioneRuolo)
    {
        return descrizioneRuolo;
    }

    private string MapperRuolo(string descrizioneRuolo)
    {
        if (descrizioneRuolo.Equals("operator", StringComparison.CurrentCultureIgnoreCase))
            return DisplayNameRole.OPERATORE;
        else
            return DisplayNameRole.AMMINISTRATORE;
    }

    private AuthenticationInfo? Mapper(PagoPADto model)
    {
        try
        {
            var allowedGroups = GroupRoles.GetRoles(_options.AzureAd!.AdGroup!);
            var groups = model.Groups!.Where(x => x != null && x.Contains(_options.AzureAd!.AdGroup!)).ToList();
            var portaleGroup = allowedGroups.Where(x => x.Value == true).FirstOrDefault();
            var allowedToPortale = groups.Where(x => portaleGroup.Key == x).FirstOrDefault();
            if (String.IsNullOrEmpty(allowedToPortale))
                throw new SecurityException("You are not allowed in Portale Fatture. There is no group valid Portale Fatture.");
            groups.Remove(allowedToPortale);  

            var group = groups.Where(x => allowedGroups.Select(x => x.Key).Contains(x)).FirstOrDefault();
            if (groups.IsNullNotAny() || string.IsNullOrEmpty(group))
                throw new SecurityException("Azure Ad roles not valid.");

            return new AuthenticationInfo()
            {
                Email = model.Email,
                Id = model.Uid,
                Ruolo = Mapper(DisplayNameRole.AMMINISTRATORE),
                IdEnte = null,
                DescrizioneRuolo = DisplayNameRole.AMMINISTRATORE,
                NomeEnte = null,
                GruppoRuolo = group,
                Auth = AuthType.PAGOPA,
                Profilo = MapperGruppi(group)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Errore autenticazione {ex.InnerException}");
        }
        return null;
    }

    private async Task<List<AuthenticationInfo>?> Mapper(SelfCareDto model)
    {
        try
        {
            List<AuthenticationInfo> infos = [];
            var roles = model.Organization!.Roles!.OrderByDescending(x=>x.Product);
            foreach (var org in roles)
            {
                var auhtInfo = new AuthenticationInfo()
                {
                    Email = model.Email,
                    Id = model.Uid,
                    Prodotto = org.Product,
                    Ruolo = Mapper(org.PartyRole!),
                    IdEnte = model.Organization!.Id,
                    DescrizioneRuolo = MapperRuolo(org.PartyRole!),
                    IdTipoContratto = 0,
                    Profilo = string.Empty,
                    GruppoRuolo = MapperGruppoRuolo(org.PartyRole!),
                    Auth = AuthType.SELFCARE
                };

                // recupero dati db selfcare
                var contratto = await _handler.Send(new ContrattoQueryGetById(auhtInfo));
                var ente = await _handler.Send(new EnteQueryGetById(auhtInfo));
                if (contratto == null || ente == null || contratto.Prodotto == null || ente.Profilo == null)
                {
                    var msg = "There is no reference in contratti o ente related to id:{idEnte} name:{Name}";
                    _logger.LogError(msg, model.Organization!.Id, model.Organization.Name);
                    throw new ConfigurationException(string.Format("There is no reference in contratti o ente related to id:{0} name:{1}", model.Organization!.Id, model.Organization.Name));
                }
                else
                {
                    auhtInfo.Profilo = ente!.Profilo;
                    auhtInfo.NomeEnte = ente!.Descrizione;
                    auhtInfo.IdTipoContratto = contratto.IdTipoContratto;
                    infos.Add(auhtInfo);
                }
            }
            return infos;
        }
        catch (ConfigurationException ex)
        {
            _logger.LogError(ex, $"Errore autenticazione {ex.InnerException}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Errore autenticazione {ex.InnerException}");
        }
        return null;
    }
}