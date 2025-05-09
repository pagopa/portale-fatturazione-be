using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.QueryHandlers
{
    public class DatiModuloCommessaGetAnniMesiHandler(
     IFattureDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     ILogger<DatiModuloCommessaGetAnniMesiHandler> logger) : IRequestHandler<DatiModuloCommessaGetAnniMesi, IEnumerable<ModuloCommessaAnnoMeseDto>?>
    {
        private readonly IFattureDbContextFactory _factory = factory;
        private readonly ILogger<DatiModuloCommessaGetAnniMesiHandler> _logger = logger;
        private readonly IStringLocalizer<Localization> _localizer = localizer;

        public async Task<IEnumerable<ModuloCommessaAnnoMeseDto>?> Handle(DatiModuloCommessaGetAnniMesi request, CancellationToken ct)
        {
            var idEnte = request.AuthenticationInfo.IdEnte;
            var prodotto = request.AuthenticationInfo.Prodotto;
            using var uow = await _factory.Create(cancellationToken: ct);
            var result = await uow.Query(new DatiModuloCommessaQueryGetAnniMesiPersistence(idEnte, prodotto), ct);
            return result;
        } 
    }
}