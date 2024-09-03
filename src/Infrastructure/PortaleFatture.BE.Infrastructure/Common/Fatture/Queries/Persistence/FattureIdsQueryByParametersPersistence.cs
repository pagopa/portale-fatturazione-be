using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence;

public class FattureIdsQueryByParametersPersistence(FattureIdsQueryByParameters command) : DapperBase, IQuery<bool>
{
    private readonly FattureIdsQueryByParameters _command = command;
    private static readonly string _sqlSelect = FattureQueryRicercaBuilder.SqlSelectIdsByParameters();
    public async Task<bool> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var anno = _command.Anno;
        var mese = _command.Mese;
        var tipoFattura = _command.TipologiaFattura;
        var fatturaInviata = _command.FatturaInviata;
        var statoAtteso = _command.StatoAtteso;

        var query = new
        {
            Anno = anno,
            Mese = mese,
            TipologiaFattura = tipoFattura,
            FatturaInviata = fatturaInviata,
            StatoAtteso = statoAtteso
        };

        var values = await ((IDatabase)this).SelectAsync<long>(
        connection!,
        _sqlSelect,
        query,
        transaction);

        _command.IdFatture = values;
        return values.Any();
    }
}