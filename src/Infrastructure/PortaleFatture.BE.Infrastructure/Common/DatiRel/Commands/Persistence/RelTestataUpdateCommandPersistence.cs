using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.Commands.Persistence;

public class RelTestataUpdateCommandPersistence(RelUploadStateCreateCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly RelUploadStateCreateCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlInsert = @" 
    UPDATE [schema][RelTestata]
    SET  [Caricata] = 1
    WHERE internal_organization_id = @IdEnte
          AND contract_id = @IdContratto
          AND TipologiaFattura = @TipologiaFattura
          AND year=@Anno
          AND month=@Mese;
";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert.Add(schema), _command, transaction);
    }
}