using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing;
using MailKit.Search;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;


namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public sealed class GestioneFattureTipologiaFatturaQueryPersistence(GestioneFattureTipologiaFatturaQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{

    private readonly GestioneFattureTipologiaFatturaQuery _command = command;
    private static readonly string _sqlSelectAll = GestioneFattureBuilder.SelectGestioneFattureTipologiaFattura();
    private static readonly string _sqlGroupOrder = GestioneFattureBuilder.SelectGestioneFattureTipologiaFatturaGroupOrder();
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var conditions = new List<string>();

        if (_command.Anno.HasValue)
            conditions.Add("Anno = @anno");

        if (!_command.Mesi.IsNullNotAny() && _command.Mesi.Length > 0)
            conditions.Add("Mese IN @mesi");

        var where = conditions.Count > 0
            ? " WHERE " + string.Join(" AND ", conditions)
            : string.Empty;


        var sql = _sqlSelectAll + where + _sqlGroupOrder;
        return await ((IDatabase)this).SelectAsync<string>(
       connection!,
       sql,
       _command,
       transaction
       );
    }

}



