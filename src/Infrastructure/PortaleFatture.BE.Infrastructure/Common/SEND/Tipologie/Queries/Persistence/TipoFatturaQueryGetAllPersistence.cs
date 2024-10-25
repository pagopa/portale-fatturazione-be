using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

public class TipoFatturaQueryGetAllPersistence(int anno, int mese, bool cancellata) : DapperBase, IQuery<IEnumerable<string>>
{
    private static readonly string _sqlSelect = TipoFatturaSQLBuilder.SelectAll();
    private static readonly string _sqlSelectCancellate = TipoFatturaSQLBuilder.SelectAllCancellate();
    private readonly int _anno = anno;
    private readonly int _mese = mese;
    private readonly bool _cancellata = cancellata;
    public async Task<IEnumerable<string>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var sql = _cancellata ? _sqlSelectCancellate : _sqlSelect;
        return await ((IDatabase)this).SelectAsync<string>(connection!, sql.Add(schema), new
        {
            anno = _anno,
            mese = _mese
        }, transaction);
    }
}