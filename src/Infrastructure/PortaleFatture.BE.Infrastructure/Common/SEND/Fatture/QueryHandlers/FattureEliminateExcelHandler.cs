using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;

public class FattureEliminateExcelHandler(
    IFattureDbContextFactory factory,
    IStringLocalizer<Localization> localizer,
    ILogger<FattureEliminateExcelHandler> logger)
    : IRequestHandler<FattureEliminateExcelQuery, IEnumerable<FattureEliminateExcelDto>>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<FattureEliminateExcelHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<IEnumerable<FattureEliminateExcelDto>> Handle(FattureEliminateExcelQuery request, CancellationToken cancellationToken)
    {
        var query = new FattureEliminateQuery(request.AuthenticationInfo)
        {
            Anno = request.Anno,
            Mese = request.Mese,
            TipologiaFattura = request.TipologiaFattura,
            DateFattura = request.DateFattura
        };

        using var rs = await _factory.Create(cancellationToken: cancellationToken);
        var rawResult = await rs.Query(new FattureEliminateQueryPersistence(query), cancellationToken);

        var dtoList = rawResult.ToEliminateDto();

        if (dtoList.Dettagli == null)
            return [];
        return dtoList.Dettagli.Select(x => new FattureEliminateExcelDto
        {
            IstitutioId = x.IstitutioId,
            RagioneSociale = x.RagioneSociale,
            IdContratto = x.IdContratto,
            Prodotto = x.Prodotto,
            TipologiaFattura = x.TipologiaFattura,
            Progressivo = x.Progressivo,
            DataFattura = x.DataFattura,
            TotaleFattura = x.TotaleFattura,
            Stato = x.Stato,
            CausaleFattura = x.CausaleFattura,
            TipoDocumento = x.TipoDocumento,
            Divisa = x.Divisa
        });
    }
}
