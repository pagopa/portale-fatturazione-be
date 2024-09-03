using System.Data;
using PortaleFatture.BE.Core.Entities.SelfCare.Dto;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence;

public class FattureQueryRicercaByEntePersistence(FattureQueryRicercaByEnte command) : DapperBase, IQuery<FattureListaDto?>
{
    private readonly FattureQueryRicercaByEnte _command = command;
    private static readonly string _sqlSelectAll = FattureQueryRicercaBuilder.SelectViewByIdEnte();
    private static readonly string _sqlSelectEnte = EnteSQLBuilder.SelectByIdEnte();
    public async Task<FattureListaDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var computedFatture = new List<FatturaDto>();

        var anno = _command.Anno;
        var mese = _command.Mese;
        var tipoFattura = _command.TipologiaFattura;
        var idEnte = _command.AuthenticationInfo!.IdEnte;
 
        var sql = String.Join(";", _sqlSelectEnte, _sqlSelectAll);

        var query = new
        {
            AnnoRiferimento = anno,
            MeseRiferimento = mese,
            TipologiaFattura = tipoFattura,
            IdEnte = idEnte
        };

        var values = await ((IDatabase)this).QueryMultipleAsync<FattureListaDto>(
        connection!,
        sql,
        query,
        transaction);

        var myEnte = await values.ReadFirstAsync<EnteContrattoDto>();
        var fatture = await values.ReadFirstAsync<FattureListaDto>();
   
        if (myEnte == null)
            return null;

        foreach (var f in fatture)
        {
            computedFatture.Add(f);
            f.fattura!.RagioneSociale = myEnte.RagioneSociale;
            f.fattura!.TipoContratto = myEnte.TipoContratto;
            f.fattura!.IdContratto = myEnte.IdContratto;
        }
        var fattData = new FattureListaDto();
        fattData.AddRange(computedFatture);
        return fattData;
    }
}