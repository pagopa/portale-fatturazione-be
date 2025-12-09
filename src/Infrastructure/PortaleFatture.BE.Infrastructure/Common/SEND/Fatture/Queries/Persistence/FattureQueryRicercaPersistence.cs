using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class FattureQueryRicercaPersistence(FattureQueryRicerca command) : DapperBase, IQuery<FattureListaDto?>
{
    private readonly FattureQueryRicerca _command = command;
    private static readonly string _sqlSelectAll = FattureQueryRicercaBuilder.SelectView();
    private static readonly string _sqlSelectAllCancellate = FattureQueryRicercaBuilder.SelectViewCancellate();
    private static readonly string _sqlSelectEnti = EnteSQLBuilder.SelectAll();
    public async Task<FattureListaDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var computedFatture = new List<FatturaDto>();

        var anno = _command.Anno;
        var mese = _command.Mese;
        var tipoFattura = _command.TipologiaFattura;

        var sqlFatture = _command.Cancellata ? _sqlSelectAllCancellate : _sqlSelectAll;
        var sqlEnti = _sqlSelectEnti.Add(schema); 

        if (!tipoFattura.IsNullNotAny())
            sqlFatture = sqlFatture.Replace("[condition_tipologiafattura]", "and FT.FkTipologiaFattura IN @TipologiaFattura"); 
        else
            sqlFatture = sqlFatture.Replace("[condition_tipologiafattura]", string.Empty); 

        var sql = string.Join(";", sqlEnti, sqlFatture); 
        
        var query = new
        {
            AnnoRiferimento = anno,
            MeseRiferimento = mese,
            TipologiaFattura = tipoFattura,
            FkIdTipoContratto = _command.FkIdTipoContratto,
            FatturaInviata = _command.FatturaInviata
        };

        using var values = await ((IDatabase)this).QueryMultipleAsync<FattureListaDto>(
        connection!,
        sql,
        query,
        transaction);
        var enti = await values.ReadAsync<EnteContrattoDto>();
        var fatture = await values.ReadFirstAsync<FattureListaDto>();

        if (!_command.IdEnti!.IsNullNotAny())
            enti = enti.Where(x => _command.IdEnti!.Contains(x.IdEnte)).ToList();

        foreach (var f in fatture)
        {
            var ente = enti.Where(x => x.IdEnte == f.fattura!.IstitutioID).FirstOrDefault();
            if (ente != null)
            {
                computedFatture.Add(f);
                f.fattura!.RagioneSociale = ente.RagioneSociale;
                f.fattura!.TipoContratto = ente.TipoContratto;
                f.fattura!.IdContratto = ente.IdContratto;

                //ordina posizioni
                f.fattura.Posizioni = f.fattura!.Posizioni?.OrderBy(p => p.NumeroLinea).ToList();
            }
        }
        var fattData = new FattureListaDto(); 

        fattData.AddRange(computedFatture);
        return fattData;
    }
}