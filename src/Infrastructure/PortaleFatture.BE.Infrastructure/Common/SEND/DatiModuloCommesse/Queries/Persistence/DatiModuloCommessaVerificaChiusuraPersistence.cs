using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;
 

public class DatiModuloCommessaVerificaChiusuraPersistence(int anno, int mese) : DapperBase, IQuery<bool>
{
    private readonly int _anno = anno;
    private readonly int _mese = mese;
    private static readonly string _sqlSelect = DatiModuloCommessaSQLBuilder.VerificaChiusuraAnnoMese();

    public async Task<bool> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        return await ((IDatabase)this).SingleAsync<bool>(connection!, _sqlSelect.Add(schema),
              new
              {
                  anno = _anno,
                  mese = _mese
              }, transaction);
    }
}