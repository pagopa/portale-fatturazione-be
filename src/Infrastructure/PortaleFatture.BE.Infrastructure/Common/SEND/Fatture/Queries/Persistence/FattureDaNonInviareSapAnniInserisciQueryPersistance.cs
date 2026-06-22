using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public sealed  class FattureDaNonInviareSapAnniInserisciQueryPersistance(FattureDaNonInviareSapAnniInserisciQuery command) : DapperBase, IQuery<IEnumerable<FattureDaNonInviareAnniInserimentoDto>?>
{

    private readonly FattureDaNonInviareSapAnniInserisciQuery _command = command;
    private static readonly string _sqlSelectAll = FattureDaNonInviareSapBuilder.SelectEsclusioneFattureAnniInserisci();
    public async Task<IEnumerable<FattureDaNonInviareAnniInserimentoDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<FattureDaNonInviareAnniInserimentoDto>(connection!, _sqlSelectAll, _command, transaction);
    }

}