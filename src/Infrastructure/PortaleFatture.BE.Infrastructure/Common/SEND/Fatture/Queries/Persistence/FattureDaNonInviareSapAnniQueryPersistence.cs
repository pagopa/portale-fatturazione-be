using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public sealed class FattureDaNonInviareSapAnniQueryPersistence(FattureDaNonInviareSapAnniQuery command) : DapperBase, IQuery<IEnumerable<int>?>
{
    private readonly FattureDaNonInviareSapAnniQuery _command = command;
    private static readonly string _sqlSelectAll = FattureDaNonInviareSapBuilder.SelectEsclusioneFattureAnni();
    public async Task<IEnumerable<int>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<int>(
       connection!, _sqlSelectAll, _command, transaction);
    }

}


