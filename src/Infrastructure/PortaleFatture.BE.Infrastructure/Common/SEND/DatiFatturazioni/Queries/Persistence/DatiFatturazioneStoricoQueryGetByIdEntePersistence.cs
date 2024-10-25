using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.DatiFatturazioni;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries.Persistence;

public class DatiFatturazioneStoricoQueryGetByIdEntePersistence(string idEnte, int annoValidita, int meseValidita) : DapperBase, IQuery<DatiFatturazioneStorico?>
{
    private readonly string _idEnte = idEnte;
    private readonly int _annoValidita = annoValidita;
    private readonly int _meseValidita = meseValidita;
    private static readonly string _sqlSelect = DatiFatturazioneStoricoSQLBuilder.SelectByIdEnteAnnoMese();

    public async Task<DatiFatturazioneStorico?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var value = (await ((IDatabase)this).SelectAsync<DatiFatturazioneStorico>(connection!, _sqlSelect.Add(schema),
            new
            {
                idente = _idEnte,
                annovalidita = _annoValidita,
                meseValidita = _meseValidita
            }, transaction)).FirstOrDefault();
        return value;
    }
}