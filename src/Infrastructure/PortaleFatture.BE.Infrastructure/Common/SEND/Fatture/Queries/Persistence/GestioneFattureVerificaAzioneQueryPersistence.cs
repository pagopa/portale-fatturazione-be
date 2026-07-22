using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class GestioneFattureVerificaAzioneQueryPersistence(GestioneFattureModificaVerificaQuery command, IStringLocalizer<Localization> localizer) : DapperBase, IQuery<bool>
{
    private readonly GestioneFattureModificaVerificaQuery _command = command;
    private static readonly string _sqlSelectAll = GestioneFattureQueryBuilder.SelectGestioneFattureVerificaAzione();
    public async Task<bool> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var where = " WHERE azione=@Azione and tipologia_fattura=@TipologiaFattura and anno=@Anno and mese=@Mese";
        var final = _sqlSelectAll + where;
        var result =  await ((IDatabase)this).SelectAsync<int>(connection!, final, _command, transaction);

        return result.Any();
    }

}


