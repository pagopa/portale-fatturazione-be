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
    /// Provides a query handler that retrieves detailed PDF data for an invoice using the specified query
    /// parameters.
    /// </summary>
    /// <remarks>This class encapsulates the logic for executing a database query to obtain detailed
    /// information about an issued invoice in PDF format. It is typically used within data access layers that require
    /// invoice detail retrieval based on entity and invoice identifiers.</remarks>
    /// <param name="command">The query object containing the parameters required to identify the issued invoice details to retrieve.</param>
    public class FattureDettaglioEmessoPdfPersistence(FattureDettaglioEmessoPdfQuery command) : DapperBase, IQuery<IEnumerable<FatturaDocContabileEmessoDettaglioDto>>
    {
        private readonly FattureDettaglioEmessoPdfQuery _command = command;
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