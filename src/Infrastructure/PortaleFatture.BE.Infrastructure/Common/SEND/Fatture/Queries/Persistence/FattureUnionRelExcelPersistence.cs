using System.Data;
using Dapper;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class FattureUnionRelExcelPersistence(FattureRelExcelQuery command) : DapperBase, IQuery<IEnumerable<FattureRelExcelDto>?>
{
    private readonly FattureRelExcelQuery _command = command;
    private static readonly string _sqlNo = FattureRelExcelBuilder.SelectNoteSenzaRel();
    private static readonly string _sqlRel = FattureRelExcelBuilder.SelectRel();
    private static readonly string _order = FattureRelExcelBuilder.OrderByRel();
    public async Task<IEnumerable<FattureRelExcelDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var computedFatture = new Dictionary<string, FattureRelExcelDto>();

        var anno = _command.Anno;
        var mese = _command.Mese;
        var tipoFattura = _command.TipologiaFattura;
        var query = new DynamicParameters();
        query.Add("anno", anno);
        query.Add("mese", mese);
        query.Add("TipologiaFattura", tipoFattura);

        string where = string.Empty;
        if (!_command.IdEnti!.IsNullNotAny())
        {
            query.Add("IdEnti", _command.IdEnti);
            where += " AND t.FKIdEnte in @IdEnti ";
        }

        if (_command.FkIdTipoContratto.HasValue)
        {
            query.Add("FkIdTipoContratto", _command.FkIdTipoContratto, DbType.Int32);
            where += " AND c.FkIdTipoContratto = @FkIdTipoContratto ";
        }

        var sql = _sqlRel + where + " UNION " + _sqlNo + where + _order; 

        var values = await ((IDatabase)this).SelectAsync<FattureRelExcelDto>(
        connection!,
        sql,
        query,
        transaction);

        foreach (var r in values)
        {

            var key = r.IdEnte + r.TipologiaFattura;
            computedFatture.TryAdd(key, r);
            var item = computedFatture[key];

            if (r.CodiceMateriale!.Contains("STORNO"))
            {
                if (r.CodiceMateriale!.Contains("ANT.") || r.CodiceMateriale!.Contains("ANTICIPO"))
                {
                    if (r.CodiceMateriale!.Contains("NA"))
                        item.StornoAnticipoAnalogico += r.RigaImponibile;
                    else
                        item.StornoAnticipoDigitale += r.RigaImponibile;
                }
                else if (r.CodiceMateriale!.Contains("ACCONTO"))
                {
                    if (r.CodiceMateriale!.Contains("NA"))
                        item.StornoAccontoAnalogico += r.RigaImponibile;
                    else
                        item.StornoAccontoDigitale += r.RigaImponibile;
                }
                //item.TotaleFatturaImponibile = item.TotaleFatturaImponibile - r.RigaImponibile;
            }
        }
        return computedFatture.Select(x => x.Value).OrderByDescending(x => x.TotaleFatturaImponibile);
    }
}