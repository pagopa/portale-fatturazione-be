using System.Collections.Generic;
using System.Data;
using Dapper;
using MimeKit;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence;

public class FattureWorkFlowQueryPersistence(FattureWorkFlowQuery command) : DapperBase, IQuery<IEnumerable<FattureVerifyModifica>?>
{
    private readonly FattureWorkFlowQuery _command = command;
    private static readonly string _sql = FattureQueryRicercaBuilder.SelectVerificaModificaFatture();
    public async Task<IEnumerable<FattureVerifyModifica>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        List<FattureVerifyModifica> fattureVerifica = [];
        foreach (var item in _command.WorkFlow!)
        {
            foreach (var kvp in item.WorkFlow!)
            {
                var dparameters = new Dictionary<string, object>
                    {
                        { "@Anno", item.Anno },
                        { "@Mese", item.Mese },
                        { "@ListaTipologiaFattura", kvp.Value == null ? [] : kvp.Value!.Select(p => p.TipologiaFattura).Distinct().ToList()},
                        { "@TipologiaFattura", (string)kvp.Key }
                    };

                var result = await ((IDatabase)this).SelectAsync<FattureVerifyModifica>(
                   connection!,
                   _sql,
                   dparameters,
                   transaction);

                fattureVerifica.AddRange(result);
            }
        }
        return fattureVerifica;
    } 
}