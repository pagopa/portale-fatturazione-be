using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using System.Data;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using System.Security.Cryptography;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaQueryGetByIdPersistence : DapperBase, IQuery<IEnumerable<DatiModuloCommessa>?>
{
    private readonly string? _prodotto;
    private readonly long? _idTipoContratto;
    private readonly int _annoValidita;
    private readonly long _meseValidita;
    private readonly string? _idEnte;
    private static readonly string _sqlSelect = DatiModuloCommessaSQLBuilder.SelectBy();

    public DatiModuloCommessaQueryGetByIdPersistence(string? idEnte, int annoValidita, int meseValidita, long? idTipoContratto, string? prodotto)
    {
        this._idTipoContratto = idTipoContratto;
        this._prodotto = prodotto;
        this._annoValidita = annoValidita;
        this._meseValidita = meseValidita;
        this._idEnte = idEnte;
    }
    public async Task<IEnumerable<DatiModuloCommessa>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        return await ((IDatabase)this).SelectAsync<DatiModuloCommessa>(connection!, _sqlSelect.Add(schema),
            new
            {
                idEnte = _idEnte,
                meseValidita = _meseValidita,
                annoValidita = _annoValidita,
                prodotto = _prodotto,
                idTipoContratto = _idTipoContratto
            }, transaction); 
    }
}