using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaRegioniQueryGetPersistence : DapperBase, IQuery<IEnumerable<ModuloCommessaRegioneDto>?>
{ 
    private static readonly string _sqlSelect = DatiModuloCommessaSQLBuilder.SelectRegioni();

    public DatiModuloCommessaRegioniQueryGetPersistence()
    {
    
    }

    public async Task<IEnumerable<ModuloCommessaRegioneDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        return await ((IDatabase)this).SelectAsync<ModuloCommessaRegioneDto>(connection!, _sqlSelect.Add(schema),
            new
            { 
            }, transaction);
    }
}