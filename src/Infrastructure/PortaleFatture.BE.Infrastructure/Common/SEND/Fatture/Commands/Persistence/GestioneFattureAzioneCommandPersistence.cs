using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

public class GestioneFattureAzioneCommandPersistence(GestioneFattureAzioneCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<bool?>
{
    public bool RequiresTransaction => false;
    private readonly GestioneFattureAzioneCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;


    private static readonly string _sqlEliminate = $"be.GestioneFattureElimina";

    private static readonly string _sqlPosticipate = $"be.GestioneFatturePosticipa";
    private static readonly string _sqlRipristina = $"be.GestioneFattureRipristina";
    private static readonly string _sqlCancella = $"be.GestioneFattureCancella";

    public async Task<bool?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        string _sql_based_on_action = "";
        string _action = _command.Azione.ToUpper();
        if (_action == "POSTICIPA")
        {
            _sql_based_on_action = _sqlPosticipate;
        }
        else if (_action == "ELIMINA")
        {
            _sql_based_on_action = _sqlEliminate;
        }
        else if (_action == "RIPRISTINA")
        {
            _sql_based_on_action = _sqlRipristina;
        }
        else if (_action == "CANCELLA")
        {
            _sql_based_on_action = _sqlCancella;
        }
        else if(_action != "POSTICIPA" && _action != "ELIMINA" && _action != "RIPRISTINA" && _action != "CANCELLA")
        {
            throw new Exception($"L'azione {_command.Azione} non esiste");
        }
       

        var parameters = new
        {
            IdEnte = _command.IdEnte,
            TipologiaFattura = _command.TipologiaFattura,
            Anno = _command.Anno,
            Mese = _command.Mese,
            Note = JsonSerializer.Serialize(_command.Note),
            IdUtente = _command.IdUtente,
            IdFattura = _command.IdFattura,
        };
        return await ((IDatabase)this).ExecuteAsync<bool>(connection!, _sql_based_on_action, parameters, transaction, CommandType.StoredProcedure);

    }
}





