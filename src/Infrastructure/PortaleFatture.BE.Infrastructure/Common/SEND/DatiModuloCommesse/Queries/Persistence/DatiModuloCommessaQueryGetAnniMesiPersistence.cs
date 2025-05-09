using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaQueryGetAnniMesiPersistence(string? idEnte, string? prodotto) : DapperBase, IQuery<IEnumerable<ModuloCommessaAnnoMeseDto>?>
{
    private readonly string? _idEnte = idEnte;
    private readonly string? _prodotto = prodotto;
    private static readonly string _sqlSelect = DatiModuloCommessaAnniSQLBuilder.SelectByAnnoMese();
    private static readonly string _sqlOrderBy = DatiModuloCommessaAnniSQLBuilder.OrderByAnnoMeseDesc();

    public async Task<IEnumerable<ModuloCommessaAnnoMeseDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        object query;
        string? where;
        if (String.IsNullOrEmpty(_idEnte))
        {
            query = new
            {
                prodotto = _prodotto
            };
            where = $" WHERE FkProdotto=@prodotto";
        }

        else
        {
            query = new
            {
                idEnte = _idEnte,
                prodotto = _prodotto
            };
            where = $" WHERE FkProdotto=@prodotto AND FkIdEnte=@idEnte";
        } 
        return await ((IDatabase)this).SelectAsync<ModuloCommessaAnnoMeseDto>(connection!, _sqlSelect + where + _sqlOrderBy,
                  query , transaction);
    }
}