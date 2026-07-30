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


    private static readonly string _sqlEliminate = $"be.spGestioneFattureElimina";
    private static readonly string _sqlPosticipate = $"be.spGestioneFatturePosticipa";
    private static readonly string _sqlRipristina = $"be.spGestioneFattureRipristina";
    private static readonly string _sqlCancella = $"be.spGestioneFattureCancella";

    public async Task<int?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        string sqlBasedOnAction;

        string action = _command.Azione!.ToUpper();

        // La nota è opzionale a livello di command (l'endpoint la rende obbligatoria a monte): se presente,
        // le si imposta l'azione che l'ha generata; se assente NON deve far esplodere la persistence
        // (altrimenti l'NRE precede — e maschera — le validazioni azione/IdEnte).
        if (_command.Nota is not null)
            _command.Nota.Azione = action;

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
        parameters.Add("@IdFattura", dbType: DbType.Int64, direction: ParameterDirection.Input, value: _command.IdFattura);
        parameters.Add("@Anno", dbType: DbType.Int32, direction: ParameterDirection.Input, value: _command.Anno);
        parameters.Add("@Mese", dbType: DbType.Int32, direction: ParameterDirection.Input, value: _command.Mese);
        parameters.Add("@TipologiaFattura", dbType: DbType.String, direction: ParameterDirection.Input, value: _command.TipologiaFattura);
        parameters.Add("@IdUtente", dbType: DbType.String, direction: ParameterDirection.Input, value: _command.IdUtente);
        parameters.Add("@Note", dbType: DbType.String, direction: ParameterDirection.Input, value: JsonSerializer.Serialize(_command.Nota));

        parameters.Add("@ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

        return await ((IDatabase)this).ExecuteAsync<int>(connection!, sqlBasedOnAction, parameters, transaction, CommandType.StoredProcedure);

    }
}





