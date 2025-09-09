using System.Data;
using Dapper;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

public class FatturaCancellazioneRipritistinoCommandPersistence(FatturaCancellazioneRipristinoCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly FatturaCancellazioneRipristinoCommand _command = command;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var dataTable = _command.IdFatture!.CreateRipristinoFattureTable();

        var procedureName = _command.Cancellazione == true
            ? $"pfd.SospendiFattura"
            : $"pfd.RipristinaFattura";

        var parameterName = _command.Cancellazione == true
            ? "@SospendiFatture"
            : "@RipristinoFatture";

        var tableTypeName = $"pfd.RipristinoFatture";

        var parameters = new DynamicParameters();
        parameters.Add(parameterName, dataTable.AsTableValuedParameter(tableTypeName));
        parameters.Add("@ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);


        return await ((IDatabase)this).SingleAsync<int>(connection!, procedureName, parameters, transaction, CommandType.StoredProcedure); 
    }
}