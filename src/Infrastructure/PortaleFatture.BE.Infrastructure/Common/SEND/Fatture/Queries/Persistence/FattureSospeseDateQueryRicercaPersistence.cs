using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;
using System.Data;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class FattureSospeseDateQueryRicercaPersistence(FattureSospeseDateQueryRicerca command) : DapperBase, IQuery<IEnumerable<FattureDateDto>?>
{
    private readonly FattureSospeseDateQueryRicerca _command = command;
    private static readonly string _sqlSelectAll = FattureQueryRicercaBuilder.SelectFattureSospeseDate();
    private static readonly string _sqlGroupBy = FattureQueryRicercaBuilder.OrderByFattureSospeseDate();
    public async Task<IEnumerable<FattureDateDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var anno = _command.Anno;
        var mese = _command.Mese;
        var tipoFattura = _command.TipologiaFattura;
        var idEnti = _command.IdEnti;

        var sqlFatture = _sqlSelectAll;
        var sql = sqlFatture;
        var where = new List<string>();

        where.Add("FT.FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865'");
        where.Add("FT_EMESSA.IdFattura IS NULL");
        where.Add("MF.FkIdFatturaTmp IS NULL");
        where.Add("FT.FlagFatturata = 0");

        if (anno.HasValue)
            where.Add("FT.AnnoRiferimento=@AnnoRiferimento");
        if (mese.HasValue)
            where.Add("FT.MeseRiferimento=@MeseRiferimento");

        if (!tipoFattura.IsNullNotAny())
            where.Add("FT.FkTipologiaFattura IN @TipologiaFattura");

        if (!_command.IdEnti.IsNullNotAny())
            where.Add("FT.FkIdEnte IN @IdEnti");

        if (where.Any())
            sql += " WHERE " + string.Join(" AND ", where);

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
        transaction,
        commandTimeout: 120);
    }
}