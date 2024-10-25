using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.QueryHandlers
{
    public class DatiModuloCommessaQueryDocumentoHandler(
     IMediator handler,
     IStringLocalizer<Localization> localizer,
     ILogger<DatiModuloCommessaQueryDocumentoHandler> logger) : IRequestHandler<DatiModuloCommessaDocumentoQueryGet, ModuloCommessaDocumentoDto?>
    {
        private readonly IMediator _handler = handler;
        private readonly ILogger<DatiModuloCommessaQueryDocumentoHandler> _logger = logger;
        private readonly IStringLocalizer<Localization> _localizer = localizer;

        public async Task<ModuloCommessaDocumentoDto?> Handle(DatiModuloCommessaDocumentoQueryGet command, CancellationToken ct)
        {
            var (annoFatturazione, meseFatturazione, _, _) = Time.YearMonthDayFatturazione();
            command.AnnoValidita = command.AnnoValidita != null ? command.AnnoValidita : annoFatturazione;
            command.MeseValidita = command.MeseValidita != null ? command.MeseValidita : meseFatturazione;
            var authInfo = command.AuthenticationInfo;

            var datiFatturazione = await _handler.Send(new DatiFatturazioneQueryGetByIdEnte(authInfo), ct);
            if (datiFatturazione == null)
                return null;

            var datiModuloCommessa = await _handler.Send(new DatiModuloCommessaQueryGet(authInfo)
            {
                AnnoValidita = command.AnnoValidita,
                MeseValidita = command.MeseValidita,
            }, ct);
            if (datiModuloCommessa == null)
                return null;

            var ente = await _handler.Send(new EnteQueryGetById(authInfo)) ?? throw new DomainException("EnteSelfCareError");

            var prodotto = datiModuloCommessa.DatiModuloCommessa!.Select(x => x.Prodotto).FirstOrDefault();
            var idTipoContratto = datiModuloCommessa.DatiModuloCommessa!.Select(x => x.IdTipoContratto).FirstOrDefault();
            var configurazioneModuloCommessa = await _handler.Send(new DatiConfigurazioneModuloCommessaQueryGet()
            {
                Prodotto = prodotto,
                IdTipoContratto = idTipoContratto
            }, ct);

            var moduliTotaleAnno = await _handler.Send(new DatiModuloCommessaQueryGetByAnno(authInfo)
            {
                AnnoValidita = command.AnnoValidita,
            });

            var moduloTotaleMese = moduliTotaleAnno!.Where(x => x.MeseValidita == command.MeseValidita).FirstOrDefault();

            var prepare = new ModuloCommessaAggregateDto()
            {
                Ente = ente,
                DatiFatturazione = datiFatturazione,
                DatiModuloCommessa = datiModuloCommessa.DatiModuloCommessa,
                DatiModuloCommessaTotale = moduloTotaleMese,
                DatiConfigurazioneModuloCommessa = configurazioneModuloCommessa
            };

            return prepare.Mapper();
        }
    }
}