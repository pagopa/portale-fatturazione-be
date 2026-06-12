using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

/// <summary>
/// Classe di persistenza per la query di report sull'andamento del credito sospeso. Questa classe implementa l'interfaccia <see cref="IQuery{TResult}"/> per eseguire la query e restituire una collezione di <see cref="ReportAndamentoCreditoSospesoDto"/> in base ai parametri specificati nella query. Utilizza Dapper per l'accesso al database e costruisce la query SQL tramite il <see cref="FattureSospeseQueryBuilder"/>.
/// </summary>
/// <param name="command">L'oggetto query contenente i parametri per filtrare i risultati.</param>
public class ReportAndamentoCreditoSospesoQueryPersistence(ReportAndamentoCreditoSospesoQuery command)
    : DapperBase, IQuery<IEnumerable<ReportAndamentoCreditoSospesoDto>>
{
    private readonly ReportAndamentoCreditoSospesoQuery _command = command;
    private static readonly string _sql = FattureSospeseQueryBuilder.SelectReportAndamentoCreditoSospeso();

    /// <summary>
    /// Esegue la query per ottenere il report di andamento del credito sospeso, filtrando per anno, mese e tipologia di fattura se specificata.
    /// </summary>
    /// <param name="connection">La connessione al database da utilizzare.</param>
    /// <param name="schema">Lo schema del database.</param>
    /// <param name="transaction">La transazione da utilizzare, se presente.</param>
    /// <param name="cancellationToken">Il token di cancellazione per annullare l'operazione.</param>
    /// <returns>Una collezione di oggetti <see cref="ReportAndamentoCreditoSospesoDto"/> rappresentanti il report di andamento del credito sospeso.</returns>
    public async Task<IEnumerable<ReportAndamentoCreditoSospesoDto>> Execute(
        IDbConnection? connection,
        string schema,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var tipoFattura = _command.TipologiaFattura;
        var filterByTipologia = tipoFattura?.Any() == true ? 1 : 0;

        return await ((IDatabase)this).SelectAsync<ReportAndamentoCreditoSospesoDto>(
            connection!, _sql,
            new
            {
                _command.Anno,
                _command.Mese,
                TipologiaFattura = tipoFattura,
                FilterByTipologia = filterByTipologia
            },
            transaction);
    }
}