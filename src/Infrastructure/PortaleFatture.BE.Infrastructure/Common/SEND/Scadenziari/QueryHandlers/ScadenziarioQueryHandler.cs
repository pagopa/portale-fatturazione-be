using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Scadenziari;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.QueryHandlers;

public class ScadenziarioQueryHandler(
     IStringLocalizer<Localization> localizer,
     ILogger<ScadenziarioQueryHandler> logger) : IRequestHandler<ScadenziarioQueryGetByTipo, (bool, Scadenziario)>
{
    private readonly ILogger<ScadenziarioQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<(bool, Scadenziario)> Handle(ScadenziarioQueryGetByTipo request, CancellationToken cancellationToken)
    {
        var authInfo = request.AuthenticationInfo;
        var tipo = request.Tipo;
        var meseVerifica = request.Mese;
        var annoVerifica = request.Anno;

        Scadenziario scadenziario;
        if (tipo == TipoScadenziario.DatiModuloCommessa)
            scadenziario = await Task.Run(() => new Scadenziario()
            {
                GiornoFine = 19,
                GiornoInizio = 1
            });
        else if (tipo == TipoScadenziario.DatiFatturazione)
            scadenziario = await Task.Run(() => new Scadenziario()
            {
                GiornoFine = 31,
                GiornoInizio = 1
            });
        else
            throw new ConfigurationException("Time table not configured for the specific data.");

        ////if (authInfo.Ruolo != Ruolo.ADMIN)
        ////    return (false, scadenziario);

        if (tipo == TipoScadenziario.DatiModuloCommessa)
        {
            var (annoFatturazione, meseFatturazione, giornoAttuale, adesso) = Time.YearMonthDayFatturazione();
            scadenziario.Mese = meseFatturazione;
            scadenziario.Anno = annoFatturazione;
            scadenziario.Adesso = adesso;
            if (meseFatturazione != meseVerifica || annoFatturazione != annoVerifica) // deve essere il mese corrente fatturazione + 1  
                return (false, scadenziario);

            if (giornoAttuale >= scadenziario.GiornoInizio && giornoAttuale <= scadenziario.GiornoFine)
                return (true, scadenziario);
            else
                return (false, scadenziario);
        }
        return (false, scadenziario);
    }
}