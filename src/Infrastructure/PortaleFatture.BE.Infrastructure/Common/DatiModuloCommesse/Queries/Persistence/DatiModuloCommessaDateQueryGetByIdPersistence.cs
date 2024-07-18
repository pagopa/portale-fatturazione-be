using System.Data;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaDateQueryGetByIdPersistence : DapperBase, IQuery<IEnumerable<ModuloCommesseDateByAnnoDto>?>
{
    private readonly string? _prodotto;
    private readonly long? _idTipoContratto;
    private readonly int _annoValidita; 
    private readonly string? _idEnte;
    private static readonly string _sqlSelect = DatiModuloCommessaSQLBuilder.SelectByDate();

    public DatiModuloCommessaDateQueryGetByIdPersistence(string? idEnte, int annoValidita, long? idTipoContratto, string? prodotto)
    {
        this._idTipoContratto = idTipoContratto;
        this._prodotto = prodotto;
        this._annoValidita = annoValidita; 
        this._idEnte = idEnte;
    }
    public async Task<IEnumerable<ModuloCommesseDateByAnnoDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        return await ((IDatabase)this).SelectAsync<ModuloCommesseDateByAnnoDto>(connection!, _sqlSelect.Add(schema),
            new
            {
                idEnte = _idEnte, 
                annoValidita = _annoValidita,
                prodotto = _prodotto,
                idTipoContratto = _idTipoContratto
            }, transaction); 
    }
}