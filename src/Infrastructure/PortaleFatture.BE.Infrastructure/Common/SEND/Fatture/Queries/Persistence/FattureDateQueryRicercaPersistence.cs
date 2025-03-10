using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;
using System.Data;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;


public class FattureDateQueryRicercaPersistence(FattureDateQueryRicerca command) : DapperBase, IQuery<IEnumerable<FattureDateDto>?>
{
    private readonly FattureDateQueryRicerca _command = command;
    private static readonly string _sqlSelectAll = FattureQueryRicercaBuilder.SelectFattureDate();
    private static readonly string _sqlSelectAllCancellate = FattureQueryRicercaBuilder.SelectFattureDateCancellate();
    private static readonly string _sqlGroupBy = FattureQueryRicercaBuilder.OrderByFattureDate();
    public async Task<IEnumerable<FattureDateDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var anno = _command.Anno;
        var mese = _command.Mese;
        var tipoFattura = _command.TipologiaFattura;
        var idEnti = _command.IdEnti;

        var sqlFatture = _command.Cancellata ? _sqlSelectAllCancellate : _sqlSelectAll;

        var sql = sqlFatture + " WHERE AnnoRiferimento=@AnnoRiferimento and MeseRiferimento=@MeseRiferimento";


        if (!tipoFattura.IsNullNotAny())
            sql += " AND FkTipologiaFattura IN @TipologiaFattura";

        if (!_command.IdEnti.IsNullNotAny())
            sql += " AND FkIdEnte IN @IdEnti";
 

        var query = new
        {
            AnnoRiferimento = anno,
            MeseRiferimento = mese,
            TipologiaFattura = tipoFattura,
            IdEnti = idEnti
        };

        return await ((IDatabase)this).SelectAsync<FattureDateDto>(
        connection!,
        sql + _sqlGroupBy,
        query,
        transaction);
    }
}