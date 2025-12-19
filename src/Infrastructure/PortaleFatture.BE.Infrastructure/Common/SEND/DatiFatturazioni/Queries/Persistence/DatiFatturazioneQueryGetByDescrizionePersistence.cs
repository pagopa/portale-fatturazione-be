using System.Data;
using Dapper;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries.Persistence;

public class DatiFatturazioneQueryGetByDescrizionePersistence(IPortaleFattureOptions options, string[]? idEnti, string? prodotto, string? profilo, int? idTipoContratto, int? page, int? size) : DapperBase, IQuery<DatiFatturazioneEnteWithCountDto?>
{
    private readonly IPortaleFattureOptions _options = options;
    private readonly string[]? _idEnti = idEnti;
    private readonly string? _prodotto = prodotto;
    private readonly string? _profilo = profilo;
    private readonly int? _idTipoContratto = idTipoContratto;
    private readonly int? _pageNumber = page;
    private readonly int? _pageSize = size;

    private static readonly string _sqlSelect = DatiFatturazioneSQLBuilder.SelectByDescrizionev2();
    private static readonly string _sqlCount = DatiFatturazioneSQLBuilder.SelectByDescrizioneCount();
    public async Task<DatiFatturazioneEnteWithCountDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        DatiFatturazioneEnteWithCountDto df = new();

        var conditions = new List<string>();

        if (!_idEnti!.IsNullNotAny())
            conditions.Add("e.InternalIstitutionId IN @idEnti");

        if (!string.IsNullOrEmpty(_prodotto))
            conditions.Add("c.product = @prodotto");
        else
            conditions.Add("c.product is not null");

        if (!string.IsNullOrEmpty(_profilo))
            conditions.Add("e.institutionType = @profilo");
        else
            conditions.Add("e.institutionType is not null");

        if (_idTipoContratto.HasValue)
            conditions.Add("c.FkIdTipoContratto = @idTipoContratto");
        else
            conditions.Add("c.FkIdTipoContratto is not null");

        var whereClause = conditions.Count != 0 ? " WHERE " + string.Join(" AND ", conditions) : "";
        var sql = _sqlSelect + whereClause;
        var sqlCount = _sqlCount + whereClause; 

        sql += " ORDER BY e.InternalIstitutionId ";

        if(_pageNumber.HasValue || _pageSize.HasValue)
        {
            sql += " OFFSET (@pageNumber - 1) * @pageSize ROWS ";
            sql += " FETCH NEXT @pageSize ROWS ONLY ";
        }

        var sqlMultiple = String.Join(";", sql, sqlCount);

        using var values = await ((IDatabase)this).QueryMultipleAsync<GridFinancialReportDto>(
            connection!,
            sqlMultiple,
            new
            {
                idEnti = _idEnti,
                prodotto = _prodotto,
                profilo = _profilo,
                idTipoContratto = _idTipoContratto,
                pageNumber = _pageNumber,
                pageSize = _pageSize
            },
            transaction,
            CommandType.Text,
            null,
            CommandFlags.NoCache);
        df.DatiFatturazioneEnte = await values.ReadAsync<DatiFatturazioneEnteDto>();
        df.Count = await values.ReadFirstAsync<int>();

        return df;
    }
}