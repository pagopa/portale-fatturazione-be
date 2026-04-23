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
    /// <summary>
    /// Provides persistence logic to retrieve detailed PDF information for issued invoices using administrative
    /// queries.
    /// </summary>
    /// <param name="command">The query object containing parameters for retrieving issued invoice details.</param>
    public class FattureDettaglioEmessoPdfAdminPersistence(FattureDocContabileDettaglioAdminEmessoQuery command) : DapperBase, IQuery<IEnumerable<FatturaDocContabileEmessoDettaglioDto>>
    {
        private readonly FattureDocContabileDettaglioAdminEmessoQuery _command = command;
        private static readonly string _sql = FattureQueryRicercaBuilder.SelectDettaglioEmessoPdf();
        public async Task<IEnumerable<FatturaDocContabileEmessoDettaglioDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
        {
            return await ((IDatabase)this).SelectAsync<FatturaDocContabileEmessoDettaglioDto>(
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