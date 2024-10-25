using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaDateQueryGetByIdPersistence : DapperBase, IQuery<IEnumerable<ModuloCommesseDateByAnnoDto>?>
{
    private readonly string? _prodotto;
    private readonly long? _idTipoContratto;
    private readonly int _annoValidita;
    private readonly string? _idEnte;
    private static readonly string _sqlSelect = DatiModuloCommessaSQLBuilder.SelectByDate();

    public DatiModuloCommessaDateQueryGetByIdPersistence(string? idEnte, int annoValidita, long? idTipoContratto, string? prodotto)
    {
        _idTipoContratto = idTipoContratto;
        _prodotto = prodotto;
        _annoValidita = annoValidita;
        _idEnte = idEnte;
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