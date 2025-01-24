using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence;

public class RelAnniQueryPersistence(RelAnniQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly RelAnniQuery _command = command;
    private static readonly string _sql = RelTestataSQLBuilder.SelectAnni();
    private static readonly string _orderBy = RelTestataSQLBuilder.OrderByYear;
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {

        return await ((IDatabase)this).SelectAsync<string>(
            connection!, _sql + _orderBy, _command, transaction);
    }
}