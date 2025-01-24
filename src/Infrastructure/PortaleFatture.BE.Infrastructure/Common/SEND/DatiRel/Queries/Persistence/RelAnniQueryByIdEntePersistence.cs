using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence;

public class RelAnniQueryByIdEntePersistence(RelAnniByIdEnteQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly RelAnniByIdEnteQuery _command = command;
    private static readonly string _sql = RelTestataSQLBuilder.SelectAnni();
    private static readonly string _orderBy = RelTestataSQLBuilder.GroupByOrderByYear;
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var authInfo = _command.AuthenticationInfo;
        dynamic parameters = new ExpandoObject();

        var where = " WHERE internal_organization_id=@IdEnte ";
        parameters.IdEnte = authInfo.IdEnte; 

        return await ((IDatabase)this).SelectAsync<string>(
            connection!, _sql + where + _orderBy, parameters, transaction);
    }
}