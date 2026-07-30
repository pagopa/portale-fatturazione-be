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
    /// Persistence class for executing the GestioneFattureReportQuery. This class retrieves a list of FatturaDocContabileRawDto based on the provided query parameters, including optional filtering by TipologiaFattura.
    /// </summary>
    /// <param name="command">The query command containing the parameters for the report.</param>
    public class GestioneFattureReportQueryPersistence(GestioneFattureReportQuery command) : DapperBase, IQuery<IEnumerable<GestioneFattureReportDto>>
    {
        private readonly GestioneFattureReportQuery _command = command;
        private static readonly string _sql = GestioneFattureQueryBuilder.SelectReport();
        public async Task<IEnumerable<GestioneFattureReportDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
        {
            // Filter by TipologiaFattura if provided
            var tipoFattura = _command.TipologiaFattura;

            // Determine if we need to filter by TipologiaFattura
            var filterByTipologia = tipoFattura?.Any() == true ? 1 : 0;

            return await ((IDatabase)this).SelectAsync<GestioneFattureReportDto>(
                    connection!, // Database connection
                    _sql, // SQL command to execute
                    new // Parameters for the query
                    {
                        TipologiaFattura = tipoFattura,
                        FilterByTipologia = filterByTipologia,
                    }
                    , transaction // Transaction for the query
                );
        }
    }
}
