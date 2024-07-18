using System.Data;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence;


public class RelTipologieFattureByIdEntePersistence(RelTipologieFattureByIdEnte command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly RelTipologieFattureByIdEnte _command = command;
    private static readonly string _sqlSelectAll = RelTestataSQLBuilder.SelectDistinctTipologiaFattura();
    private static readonly string _orderBy = RelTestataSQLBuilder.OrderByDistinctTipologiaFattura();
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var where = string.Empty;
        var idEnte = _command.AuthenticationInfo.IdEnte;
        where += " WHERE internal_organization_id=@IdEnte ";
        var anno = _command.Anno;
        var mese = _command.Mese; 
 
        where += " AND year=@anno";
        where += " AND month=@mese"; 

        var sqlEnte = _sqlSelectAll;
        sqlEnte += where + _orderBy; 
        sqlEnte = sqlEnte.Add(schema);

        var query = new RelQueryDto
        {
            Anno = anno,
            Mese = mese,
            IdEnte = idEnte 
        };

        return await ((IDatabase)this).SelectAsync<string>(
            connection!,
            sqlEnte,
            query,
            transaction);
    }
}