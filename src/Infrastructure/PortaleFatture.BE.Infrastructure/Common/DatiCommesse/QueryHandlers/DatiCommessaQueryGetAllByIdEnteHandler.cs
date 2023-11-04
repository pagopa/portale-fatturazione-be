using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.DatiCommesse;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using MediatR;
using PortaleFatture.BE.Core.Exceptions;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.QueryHandlers;

public class DatiCommessaQueryGetAllByIdEnteHandler : IRequestHandler<DatiCommessaQueryGetAllByIdEnte, IEnumerable<DatiCommessa>?>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly ILogger<DatiCommessaQueryGetAllByIdEnteHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;
    public DatiCommessaQueryGetAllByIdEnteHandler(
         IUnitOfWorkFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<DatiCommessaQueryGetAllByIdEnteHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IEnumerable<DatiCommessa>?> Handle(DatiCommessaQueryGetAllByIdEnte command, CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(command.IdEnte))
            throw new DomainException(_localizer["DatiCommessaEmptyIdEnte"]);
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new DatiCommessaQueryGetAllByIdEntePersistence(command.IdEnte!), ct); 
    } 
} 