using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public sealed class WhiteListFatturaEnteAnniInserisciQueryPersistence(WhiteListFatturaEnteAnniInserisciQuery command) : DapperBase, IQuery<IEnumerable<WhiteListFatturaEnteAnniInserimentoDto>?>
{
    private readonly WhiteListFatturaEnteAnniInserisciQuery _command = command;
    private static readonly string _sqlSelectAll = FattureQueryRicercaBuilder.SelectWhiteListAnniInserisci();
    public async Task<IEnumerable<WhiteListFatturaEnteAnniInserimentoDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {  
        return await ((IDatabase)this).SelectAsync<WhiteListFatturaEnteAnniInserimentoDto>(connection!, _sqlSelectAll, _command, transaction);
    }
}