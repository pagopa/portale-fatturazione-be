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

public class DatiCommessaQueryGetByIdHandler : IRequestHandler<DatiCommessaQueryGetById, DatiCommessa>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly ILogger<DatiCommessaQueryGetByIdHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;
    public DatiCommessaQueryGetByIdHandler(
         IUnitOfWorkFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<DatiCommessaQueryGetByIdHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<DatiCommessa> Handle(DatiCommessaQueryGetById command, CancellationToken ct)
    {
        using var uow = await _factory.Create(true, cancellationToken: ct);
        var datiCommessa = await uow.Query(new DatiCommessaQueryGetByIdPersistence(command.Id), ct);
        if (datiCommessa is null)
            throw new DomainException(_localizer["DatiCommessaGetError", command.Id]);
        datiCommessa.Contatti = await uow.Query(new DatiCommessaContattoQueryGetByIdPersistence(command.Id), ct);
        return datiCommessa;
    } 
} 