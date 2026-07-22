using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;
public class GestioneFattureModificaAnniQueryPersistence(GestioneFattureModificaAnniQuery command) : DapperBase, IQuery<IEnumerable<int>?>
{
    private readonly GestioneFattureModificaAnniQuery _command = command;
    private static readonly string _sqlSelectAll = GestioneFattureQueryBuilder.SelectGestioneFattureModificaAnni();
    public async Task<IEnumerable<int>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var where = " WHERE azione=@Azione and tipologia_fattura=@TipologiaFattura ORDER BY Anno DESC";
        var final = _sqlSelectAll + where;
        return await ((IDatabase)this).SelectAsync<int>(connection!, final, _command, transaction);
    }

}
