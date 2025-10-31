using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.QueryHandlers;

public class ModuloCommessaPrevisionaleDownloadQueryHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<ModuloCommessaPrevisionaleDownloadQueryHandler> logger) : IRequestHandler<ModuloCommessaPrevisionaleDownloadQueryGet, IEnumerable<ModuloCommessaPrevisionaleDownloadDto>?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<ModuloCommessaPrevisionaleDownloadQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<IEnumerable<ModuloCommessaPrevisionaleDownloadDto>?> Handle(ModuloCommessaPrevisionaleDownloadQueryGet request, CancellationToken ct)
    {
        // valida input request
        if ((request.Anno <= 0 || request.Anno == null) || (request.Mese <= 0 || request.Mese == null))
        {
            var msg = "Passare un anno e un mese valido"; 
            throw new ArgumentException(msg);
        }

        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new ModuloCommessaPrevisionaleDownloadQueryGetPersistence(request, ct));
    }
}