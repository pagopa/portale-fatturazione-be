using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Commands.Persistence;

internal class DatiModuloCommessaValoriRegioniInsertCommandPersistence(List<ValoriRegioneDto> command) : DapperBase, ICommand<int>
{ 
    public bool RequiresTransaction => false;
    private readonly List<ValoriRegioneDto> _command = command; 
    private static readonly string _sqlInsert = $@" 
INSERT INTO [pfw].[DatiModuloCommessaRegioni] 
    ([Internalistitutionid], [Anno], [Mese], [Provincia], [Regione], [AR], [890])
VALUES 
    (@Internalistitutionid, @Anno, @Mese, @IstatProvincia, @IstatRegione, @AR, @A890);
";
     
    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert, _command, transaction); 
    }
}
