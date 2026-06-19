using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public sealed class FattureDaNonInviareSapTipologiaFatturaQueryPersistence(FattureDaNonInviareSapTipologiaFatturaQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly FattureDaNonInviareSapTipologiaFatturaQuery _command = command;
    private static readonly string _sqlSelectAll = FattureDaNonInviareSapBuilder.SelectEsclusioneFattureTipologiaFattura();
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<string>(
       connection!, _sqlSelectAll, _command, transaction);
    }
}
