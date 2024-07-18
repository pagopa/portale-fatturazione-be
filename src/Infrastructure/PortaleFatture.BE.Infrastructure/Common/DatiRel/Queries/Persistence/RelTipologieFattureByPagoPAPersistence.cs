using System.Data;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence;


public class RelTipologieFattureByPagoPAPersistence(RelTipologieFattureByPagoPA command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly RelTipologieFattureByPagoPA _command = command;
    private static readonly string _sqlSelectAll = RelTestataSQLBuilder.SelectDistinctTipologiaFatturaPagoPA();
    private static readonly string _orderBy = RelTestataSQLBuilder.OrderByDistinctTipologiaFattura();
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        var anno = _command.Anno;
        var mese = _command.Mese;

        var where = " WHERE year=@anno";
        where += " AND month=@mese";

        var sqlEnte = _sqlSelectAll;
        sqlEnte += where + _orderBy;
        sqlEnte = sqlEnte.Add(schema);

        var query = new RelQueryDto
        {
            Anno = anno,
            Mese = mese 
        };

        return await ((IDatabase)this).SelectAsync<string>(
            connection!,
            sqlEnte,
            query,
            transaction);
    }
}