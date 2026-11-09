using MediatR;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;

// Handler di lettura dell'Invio a SAP: un contesto non transazionale per query (DapperBase chiude la
// connessione dopo ogni query fuori transazione). Nessuna scrittura.

public sealed class InvioSapGrigliaQueryHandler(IFattureDbContextFactory factory) : IRequestHandler<InvioSapGrigliaQuery, IEnumerable<InvioSapGrigliaDto>>
{
    public async Task<IEnumerable<InvioSapGrigliaDto>> Handle(InvioSapGrigliaQuery request, CancellationToken ct)
    {
        using var rs = await factory.Create(cancellationToken: ct);
        return await rs.Query(new InvioSapGrigliaQueryPersistence(request), ct);
    }
}

public sealed class InvioSapRigoDaInviareQueryHandler(IFattureDbContextFactory factory) : IRequestHandler<InvioSapRigoDaInviareQuery, int?>
{
    public async Task<int?> Handle(InvioSapRigoDaInviareQuery request, CancellationToken ct)
    {
        using var rs = await factory.Create(cancellationToken: ct);
        return await rs.Query(new InvioSapRigoDaInviareQueryPersistence(request), ct);
    }
}

public sealed class InvioSapDettaglioQueryHandler(IFattureDbContextFactory factory) : IRequestHandler<InvioSapDettaglioQuery, InvioSapDettaglioListDto>
{
    public async Task<InvioSapDettaglioListDto> Handle(InvioSapDettaglioQuery request, CancellationToken ct)
    {
        using var rs = await factory.Create(cancellationToken: ct);
        return await rs.Query(new InvioSapDettaglioQueryPersistence(request), ct);
    }
}

public sealed class InvioSapStatoFattureQueryHandler(IFattureDbContextFactory factory) : IRequestHandler<InvioSapStatoFattureQuery, IEnumerable<InvioSapStatoFatturaDto>>
{
    public async Task<IEnumerable<InvioSapStatoFatturaDto>> Handle(InvioSapStatoFattureQuery request, CancellationToken ct)
    {
        using var rs = await factory.Create(cancellationToken: ct);
        return await rs.Query(new InvioSapStatoFattureQueryPersistence(request), ct);
    }
}

public sealed class InvioSapReinviateQueryHandler(IFattureDbContextFactory factory) : IRequestHandler<InvioSapReinviateQuery, IEnumerable<InvioSapReinviataDto>>
{
    public async Task<IEnumerable<InvioSapReinviataDto>> Handle(InvioSapReinviateQuery request, CancellationToken ct)
    {
        using var rs = await factory.Create(cancellationToken: ct);
        return await rs.Query(new InvioSapReinviateQueryPersistence(request), ct);
    }
}

public sealed class InvioSapLockQueryHandler(IFattureDbContextFactory factory) : IRequestHandler<InvioSapLockQuery, InvioSapLockDto?>
{
    public async Task<InvioSapLockDto?> Handle(InvioSapLockQuery request, CancellationToken ct)
    {
        using var rs = await factory.Create(cancellationToken: ct);
        return await rs.Query(new InvioSapLockQueryPersistence(), ct);
    }
}
