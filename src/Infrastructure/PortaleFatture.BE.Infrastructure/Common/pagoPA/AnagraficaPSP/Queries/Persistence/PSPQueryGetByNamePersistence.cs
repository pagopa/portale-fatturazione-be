using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries.Persistence;

public sealed class PSPQueryGetByNamePersistence(PSPQueryGetByName command) : DapperBase, IQuery<IEnumerable<ContractIdPSP>>
{
    private readonly PSPQueryGetByName _command = command; 
    private static readonly string _sql = PSPSQLBuilder.SelectContractsId(); 
    private static readonly string _orderBy = PSPSQLBuilder.OrderBy();
    public async Task<IEnumerable<ContractIdPSP>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var where = " WHERE name like '%' + @name + '%'";
        var orderBy = _orderBy;
        var name = _command.Name;

        var sql = _sql + where + orderBy;

        dynamic parameters = new ExpandoObject();
        parameters.Name = name;

         return await ((IDatabase)this).SelectAsync<ContractIdPSP>(
            connection!,
            sql,
            parameters,
            transaction); 
    }
}