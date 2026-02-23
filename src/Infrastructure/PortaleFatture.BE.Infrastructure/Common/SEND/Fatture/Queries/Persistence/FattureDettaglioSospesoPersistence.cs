using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence
{
    public class FattureDettaglioSospesoPersistence(FattureDocContabileDettaglioQuery command) : DapperBase, IQuery<IEnumerable<FattureDocContabiliDettaglioDto>>
    {
        private readonly FattureDocContabileDettaglioQuery _command = command;
        private static readonly string _sql = FattureQueryRicercaBuilder.SelectDettaglioSospesoEnte();
        public async Task<IEnumerable<FattureDocContabiliDettaglioDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
        {
            return await ((IDatabase)this).SelectAsync<FattureDocContabiliDettaglioDto>(
                connection!, _sql,
                new
                {
                    _command.IdEnte,
                    _command.IdFattura
                }
                , transaction);
        }
    }
}