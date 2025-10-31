using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaQueryGetByDescrizionePersistence(string[]? idEnti, int annoValidita, int meseValidita, string? prodotto) : DapperBase, IQuery<IEnumerable<ModuloCommessaPrevisionaleByAnnoDto>?>
{
    private readonly string? _prodotto = prodotto;
    private readonly int _annoValidita = annoValidita;
    private readonly long _meseValidita = meseValidita;
    private readonly string[]? _idEnti = idEnti;
    private static readonly string _sqlSelect = DatiModuloCommessaTotaleSQLBuilder.SelectPrevisionaleByAnnoAndMesePagoPA();

    public async Task<IEnumerable<ModuloCommessaPrevisionaleByAnnoDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        return await ((IDatabase)this).SelectAsync<ModuloCommessaPrevisionaleByAnnoDto>(connection!, _sqlSelect,
            new
            {
                idEnte = _idEnti,
                mese = _meseValidita,
                annofiltro = _annoValidita,
                prodotto = _prodotto == null ? "prod-pn" : _prodotto,
            }, transaction);
    }
}