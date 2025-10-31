using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Commands.Persistence;

public  class DatiModuloCommessaValoriRegioniDeleteCommandPersistence(string? idEnte, int Anno, int Mese) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false; 
    private static readonly string _sqlDelete = $@"
DELETE FROM [pfw].[DatiModuloCommessaRegioni] 
WHERE [Internalistitutionid] = @Internalistitutionid 
    AND [Anno] = @Anno 
    AND [Mese] = @Mese;";


    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var parameters = new { Internalistitutionid = idEnte, Anno, Mese };
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlDelete, parameters, transaction);
    }
} 