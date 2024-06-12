﻿using System.Data;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence;

public class FattureRelExcelBuilderPersistence(FattureRelExcelQuery command) : DapperBase, IQuery<IEnumerable<FattureRelExcelDto>?>
{
    private readonly FattureRelExcelQuery _command = command;
    private static readonly string _sqlSelectRel = FattureRelExcelBuilder.SelectRel();
    public async Task<IEnumerable<FattureRelExcelDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var computedFatture = new Dictionary<string, FattureRelExcelDto>();

        var anno = _command.Anno;
        var mese = _command.Mese;
        var tipoFattura = _command.TipologiaFattura;

        var sqlRel = _sqlSelectRel; 
        var query = new
        {
            Anno = anno,
            Mese = mese,
            TipologiaFattura = tipoFattura,
        };
 
        var values = await ((IDatabase)this).SelectAsync<FattureRelExcelDto>(
        connection!,
        _sqlSelectRel,
        query,
        transaction);

        foreach (var r in values)
        {

            var key = r.IdEnte + r.TipologiaFattura;
            computedFatture.TryAdd(key, r);
            var item = computedFatture[key];

            if (r.CodiceMateriale!.Contains("STORNO"))
            {
                if (r.CodiceMateriale!.Contains("ANT.") || r.CodiceMateriale!.Contains("ANTICIPO"))
                {
                    if (r.CodiceMateriale!.Contains("NA"))
                        item.StornoAnticipoAnalogico += r.RigaImponibile;
                    else
                        item.StornoAnticipoDigitale += r.RigaImponibile;
                }
                else if (r.CodiceMateriale!.Contains("ACCONTO"))
                {
                    if (r.CodiceMateriale!.Contains("NA"))
                        item.StornoAccontoAnalogico += r.RigaImponibile;
                    else
                        item.StornoAccontoDigitale += r.RigaImponibile;
                }
            } 
        }
        return computedFatture.Select(x => x.Value).OrderByDescending(x => x.RelTotale);
    }
}