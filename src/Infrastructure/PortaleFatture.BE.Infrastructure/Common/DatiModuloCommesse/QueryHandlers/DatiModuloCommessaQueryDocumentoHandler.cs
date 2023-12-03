using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.QueryHandlers
{
    public class DatiModuloCommessaQueryDocumentoHandler(
     IMediator handler,
     IStringLocalizer<Localization> localizer,
     ILogger<DatiModuloCommessaQueryDocumentoHandler> logger) : IRequestHandler<DatiModuloCommessaDocumentoQueryGet, ModuloCommessaDocumentoDto?>
    {
        private readonly IMediator _handler = handler;
        private readonly ILogger<DatiModuloCommessaQueryDocumentoHandler> _logger = logger;
        private readonly IStringLocalizer<Localization> _localizer = localizer;

        public async Task<ModuloCommessaDocumentoDto?> Handle(DatiModuloCommessaDocumentoQueryGet request, CancellationToken ct)
        {
            var (anno, mese, _) = Time.YearMonth();
            request.AnnoValidita = request.AnnoValidita != null ? request.AnnoValidita : anno;
            request.MeseValidita = request.MeseValidita != null ? request.MeseValidita : mese;
            var authInfo = request.AuthenticationInfo;

            var datiFatturazione = await _handler.Send(new DatiFatturazioneQueryGetByIdEnte(authInfo));
            if (datiFatturazione == null)
                return null;

            var datiModuloCommessa = await _handler.Send(new DatiModuloCommessaQueryGet(authInfo)
            {
                AnnoValidita = request.AnnoValidita,
                MeseValidita = request.MeseValidita,
            });
            if (datiModuloCommessa == null)
                return null;

            var ente = await _handler.Send(new EnteQueryGetById(authInfo));
            if (ente == null)
                throw new DomainException("xxx");

            var categorie = await _handler.Send(new SpedizioneQueryGetAll());
            if (categorie == null)
                if (ente == null)
                    throw new DomainException("xxx");

            var prepare = new ModuloCommessaAggregateDto()
            {
                Ente = ente,
                DatiFatturazione = datiFatturazione,
                DatiModuloCommessa = datiModuloCommessa.DatiModuloCommessa,
                Categorie = categorie
            };

            return prepare.Mapper();
        }
    }
}