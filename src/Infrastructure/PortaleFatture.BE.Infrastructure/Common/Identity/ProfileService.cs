using System.Security;
using MediatR;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Auth.SelfCare;
using PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.Infrastructure.Common.Identity;

public class ProfileService : IProfileService
{
    private readonly ISelfCareTokenService _selfCareTokenService;
    private readonly ILogger<ProfileService> _logger;
    private readonly IMediator _handler;
    public ProfileService(
        ISelfCareTokenService selfCareTokenService,
        IMediator handler,
        ILogger<ProfileService> logger)
    {
        this._selfCareTokenService = selfCareTokenService;
        this._logger = logger;
        this._handler = handler;
    }

    public async Task<List<AuthenticationInfo>> GetInfo(string? selfcareToken)
    {
        if (selfcareToken == null)
        {
            var msg = "Fatal error with self care exchange token!";
            _logger.LogError(msg);
            throw new SecurityException(msg);
        }
        var selfcare = await _selfCareTokenService.ValidateContent(selfcareToken);
        return await Mapper(selfcare!);
    }

    private string Mapper(string mapper)
    {
        return mapper switch
        {
            nameof(Ruolo.OPERATOR) => Ruolo.OPERATOR,
            _ => Ruolo.ADMIN,
        };
    }

    private async Task<List<AuthenticationInfo>> Mapper(SelfCareDto model)
    {
        List<AuthenticationInfo> infos = new();
        foreach (var org in model.Organization!.Roles!)
        {
            var auhtInfo = new AuthenticationInfo()
            {
                Email = model.Email,
                Id = model.Uid,
                Prodotto = org.Product,
                Ruolo = Mapper(org.PartyRole!),
                IdEnte = model.Organization!.Id,
                DescrizioneRuolo = org.PartyRole, 
                IdTipoContratto = 0,
                Profilo = string.Empty
            };
            // recupero dati db selfcare
            var contratto = await _handler.Send(new ContrattoQueryGetById(auhtInfo));
            var ente = await _handler.Send(new EnteQueryGetById(auhtInfo));
            if (contratto == null || ente == null || contratto.Prodotto == null || ente.Profilo == null || contratto.Prodotto.ToLower() != org.Product!.ToLower())
            {
                var msg = "There is no reference in contratti o ente related to id:{idEnte}";
                _logger.LogError(msg, model.Organization!.Id);
                throw new SecurityException(msg);
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
}