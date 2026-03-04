using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

public class TipoFatturaSospeseQueryGetAllPersistence(int anno, int mese, bool cancellata) : DapperBase, IQuery<IEnumerable<string>>
{
    private static readonly string _sqlSelectSospese = TipoFatturaSQLBuilder.SelectAllSospese();
    private readonly int _anno = anno;
    private readonly int _mese = mese;
    private readonly bool _cancellata = cancellata;
    public async Task<IEnumerable<string>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<string>(connection!, _sqlSelectSospese.Add(schema), new
        {
            anno = _anno,
            mese = _mese
        }, transaction);
    }
}
