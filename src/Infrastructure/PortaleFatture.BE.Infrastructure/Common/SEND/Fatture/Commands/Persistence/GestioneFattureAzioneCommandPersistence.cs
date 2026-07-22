using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

public class GestioneFattureAzioneCommandPersistence(GestioneFattureAzioneCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int?>
{
    public bool RequiresTransaction => false;
    private readonly GestioneFattureAzioneCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;


    private static readonly string _sqlEliminate = $"be.GestioneFattureElimina";

    private static readonly string _sqlPosticipate = $"be.GestioneFatturePosticipa";
    private static readonly string _sqlRipristina = $"be.GestioneFattureRipristina";
    private static readonly string _sqlCancella = $"be.GestioneFattureCancella";

    public async Task<int?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        string sqlBasedOnAction;

        string action = _command.Azione!.ToUpper();

        switch (action)
        {
            case "POSTICIPA":
                sqlBasedOnAction = _sqlPosticipate;
                break;
            case "ELIMINA":
                sqlBasedOnAction = _sqlEliminate;
                break;
            case "RIPRISTINA":
                sqlBasedOnAction = _sqlRipristina;
                break;
            case "CANCELLA":
                sqlBasedOnAction = _sqlCancella;
                break;
            default:
                throw new ArgumentException($"L'azione {_command.Azione} non esiste");
        }
            
        var parameters = new DynamicParameters();

        parameters.Add("@IdEnte", dbType: DbType.Guid, direction: ParameterDirection.Input, value: Guid.Parse(_command.IdEnte!));
        parameters.Add("@IdFattura", dbType: DbType.Int32, direction: ParameterDirection.Input, value: _command.IdFattura);
        parameters.Add("@Anno", dbType: DbType.Int32, direction: ParameterDirection.Input, value: _command.Anno);
        parameters.Add("@Mese", dbType: DbType.Int32, direction: ParameterDirection.Input, value: _command.Mese);
        parameters.Add("@TipologiaFattura", dbType: DbType.String, direction: ParameterDirection.Input, value: _command.TipologiaFattura);
        parameters.Add("@IdUtente", dbType: DbType.String, direction: ParameterDirection.Input, value: _command.IdUtente);
        parameters.Add("@Note", dbType: DbType.String, direction: ParameterDirection.Input, value: JsonSerializer.Serialize(_command.Note));

        parameters.Add("@ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

        return await ((IDatabase)this).ExecuteAsync<int>(connection!, sqlBasedOnAction, parameters, transaction, CommandType.StoredProcedure);

    }
}





