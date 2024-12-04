using System.Data;
using System.Dynamic;
using Dapper;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries.Persistence;

public sealed class PSPQueryGetByRicercaPersistence(PSPQueryGetByRicerca command) : DapperBase, IQuery<PSPListDto>
{
    private readonly PSPQueryGetByRicerca _command = command;


    private static readonly string _sql = PSPSQLBuilder.SelectAll();
    private static readonly string _sqlCount = PSPSQLBuilder.SelectAllCount();
    private static readonly string _orderBy = PSPSQLBuilder.OrderBy();
    private static readonly string _offSet = PSPSQLBuilder.OffSet();
    public async Task<PSPListDto> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var psps = new PSPListDto();
        List<string> where = [];
        var page = _command.Page;
        var size = _command.Size;
        var offset = _offSet;

        var orderBy = _orderBy;

        dynamic parameters = new ExpandoObject();
        if (page.HasValue)
            parameters.Page = page;
        else
            offset = string.Empty;

        if (size.HasValue)
            parameters.Size = size;
        else
            offset = string.Empty;

        if (_command.YearQuarter.IsNullNotAny())
            where.AddInOrder(" year_quarter = (SELECT MAX(year_quarter) FROM [ppa].[Contracts])"); 
        else
        {
            where.AddInOrder(" year_quarter IN @YearQuarter");
            parameters.YearQuarter = _command.YearQuarter;
        }

        if (!_command.ContractIds.IsNullNotAny())
        {
            where.AddInOrder(" contract_id IN @ContractIds");
            parameters.ContractIds = _command.ContractIds;
        }
        if (!string.IsNullOrEmpty(_command.MembershipId))
        {
            where.AddInOrder(" membership_id = @MembershipId");
            parameters.MembershipId = _command.MembershipId;
        }
        if (!string.IsNullOrEmpty(_command.RecipientId))
        {
            where.AddInOrder(" recipient_id = @RecipientId");
            parameters.RecipientId = _command.RecipientId;
        }
        if (!string.IsNullOrEmpty(_command.ABI))
        {
            where.AddInOrder(@" 
 (provider_names LIKE '%'+ @abi +',%' 
  OR provider_names LIKE '%,' +  @abi + '%'
  OR provider_names LIKE '%ABI' + @abi + '%'
  OR abi = @abi
  OR abi = 'ABI' + @abi) ");
            parameters.ABI = _command.ABI;
        }

        var sWhere = string.Empty;

        if (!where.IsNullNotAny())
        {
            for (int i = 0; i < where.Count; i++)
            {
                if (i == 0)
                    where[i] = " WHERE " + where[i];
                else
                    where[i] = " AND " + where[i];
            }
            sWhere = string.Join(" ", where);
        }

        var sql = _sql + sWhere + orderBy + offset;
        var sqlCount = _sqlCount + sWhere;

        var sqlMultiple = String.Join(";", sql, sqlCount);
        using var values = await ((IDatabase)this).QueryMultipleAsync<PSP>(
            connection!,
            sqlMultiple,
            parameters,
            transaction,
            CommandType.Text,
            null,
            CommandFlags.NoCache);
        psps.PSPs = await values.ReadAsync<PSP>();
        psps.Count = await values.ReadFirstAsync<int>();

        return psps;
    }
}