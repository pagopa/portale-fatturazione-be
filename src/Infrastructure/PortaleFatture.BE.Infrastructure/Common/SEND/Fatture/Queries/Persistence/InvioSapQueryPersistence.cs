using System.Data;
using Dapper;
using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

// Persistence di lettura dell'Invio a SAP: solo SELECT sulle viste, parametri Dapper, nessuna scrittura.

public sealed class InvioSapGrigliaQueryPersistence(InvioSapGrigliaQuery query) : DapperBase, IQuery<IEnumerable<InvioSapGrigliaDto>>
{
    public async Task<IEnumerable<InvioSapGrigliaDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        var condizioni = new List<string> { "Anno = @Anno" };
        if (query.Mesi is { Length: > 0 }) condizioni.Add("Mese IN @Mesi");
        if (query.TipologieFattura is { Length: > 0 }) condizioni.Add("TipologiaFattura IN @TipologieFattura");
        var stato = CondizioneStato(query.StatoInvio, "StatoFattura");
        if (stato is not null) condizioni.Add(stato);

        var sql = InvioSapQueryBuilder.SelectGriglia + " WHERE " + string.Join(" AND ", condizioni) + InvioSapQueryBuilder.OrderByGriglia;
        return await ((IDatabase)this).SelectAsync<InvioSapGrigliaDto>(connection!, sql,
            new { query.Anno, query.Mesi, query.TipologieFattura }, transaction);
    }

    /// <summary>Regola di DATA: 0 da inviare, 1 inviata, NULL in elaborazione.</summary>
    internal static string? CondizioneStato(string? statoInvio, string colonna) => FiltroStatoInvioSap.Normalizza(statoInvio) switch
    {
        FiltroStatoInvioSap.DA_INVIARE => $"{colonna} = 0",
        FiltroStatoInvioSap.INVIATE => $"{colonna} = 1",
        FiltroStatoInvioSap.IN_ELABORAZIONE => $"{colonna} IS NULL",
        _ => null
    };
}

public sealed class InvioSapRigoDaInviareQueryPersistence(InvioSapRigoDaInviareQuery query) : DapperBase, IQuery<int?>
{
    public async Task<int?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
        => await ((IDatabase)this).SingleOrDefaultAsync<int?>(connection!, InvioSapQueryBuilder.SelectNumeroFattureRigoDaInviare,
            new { query.Anno, query.Mese, TipologiaFattura = query.TipologiaFattura.Trim() }, transaction);
}

public sealed class InvioSapDettaglioQueryPersistence(InvioSapDettaglioQuery query) : DapperBase, IQuery<InvioSapDettaglioListDto>
{
    public async Task<InvioSapDettaglioListDto> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        var condizioni = new List<string>
        {
            "AnnoRiferimento = @Anno", "MeseRiferimento = @Mese", "FkTipologiaFattura = @TipologiaFattura"
        };
        var stato = InvioSapGrigliaQueryPersistence.CondizioneStato(query.StatoInvio, "FatturaInviata");
        if (stato is not null) condizioni.Add(stato);
        if (!string.IsNullOrWhiteSpace(query.RagioneSociale)) condizioni.Add(@"RagioneSociale LIKE @RagioneSociale ESCAPE '\'");
        if (!string.IsNullOrWhiteSpace(query.NumeroFattura)) condizioni.Add(@"IdentificativoFattura LIKE @NumeroFattura ESCAPE '\'");
        if (query.DataFattura is not null) condizioni.Add("CAST(DataFattura AS date) = CAST(@DataFattura AS date)");

        var where = " WHERE " + string.Join(" AND ", condizioni);
        var select = InvioSapQueryBuilder.SelectDettaglio + where + InvioSapQueryBuilder.OrderByDettaglio;
        if (query.Page is not null && query.PageSize is not null) select += InvioSapQueryBuilder.OffSet;
        var sql = select + ";" + InvioSapQueryBuilder.CountDettaglio + where;

        var parametri = new
        {
            query.Anno,
            query.Mese,
            TipologiaFattura = query.TipologiaFattura.Trim(),
            RagioneSociale = string.IsNullOrWhiteSpace(query.RagioneSociale) ? null : $"%{InvioSapQueryBuilder.EscapeLike(query.RagioneSociale.Trim())}%",
            NumeroFattura = string.IsNullOrWhiteSpace(query.NumeroFattura) ? null : $"%{InvioSapQueryBuilder.EscapeLike(query.NumeroFattura.Trim())}%",
            query.DataFattura,
            query.Page,
            query.PageSize
        };

        var risultato = new InvioSapDettaglioListDto();
        using (var values = await ((IDatabase)this).QueryMultipleAsync<InvioSapDettaglioDto>(
            connection!, sql, parametri, transaction, CommandType.Text, null, CommandFlags.NoCache))
        {
            risultato.Fatture = (await values.ReadAsync<InvioSapDettaglioDto>()).ToList();
            risultato.Count = await values.ReadFirstAsync<int>();
        }
        return risultato;
    }
}

public sealed class InvioSapStatoFattureQueryPersistence(InvioSapStatoFattureQuery query) : DapperBase, IQuery<IEnumerable<InvioSapStatoFatturaDto>>
{
    public async Task<IEnumerable<InvioSapStatoFatturaDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        if (query.IdFatture.Count == 0) return [];
        return await ((IDatabase)this).SelectAsync<InvioSapStatoFatturaDto>(connection!, InvioSapQueryBuilder.SelectStatoFatture,
            new { IdFatture = query.IdFatture.Distinct().ToArray() }, transaction);
    }
}

public sealed class InvioSapReinviateQueryPersistence(InvioSapReinviateQuery query) : DapperBase, IQuery<IEnumerable<InvioSapReinviataDto>>
{
    public async Task<IEnumerable<InvioSapReinviataDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        var condizioni = new List<string> { "AnnoRiferimento = @Anno" };
        if (query.Mesi is { Length: > 0 }) condizioni.Add("MeseRiferimento IN @Mesi");
        if (query.TipologieFattura is { Length: > 0 }) condizioni.Add("TipologiaFattura IN @TipologieFattura");
        var sql = InvioSapQueryBuilder.SelectReinviate + " WHERE " + string.Join(" AND ", condizioni) + InvioSapQueryBuilder.OrderByReinviate;
        return await ((IDatabase)this).SelectAsync<InvioSapReinviataDto>(connection!, sql,
            new { query.Anno, query.Mesi, query.TipologieFattura }, transaction);
    }
}

public sealed class InvioSapLockQueryPersistence : DapperBase, IQuery<InvioSapLockDto?>
{
    public async Task<InvioSapLockDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
        => await ((IDatabase)this).SingleOrDefaultAsync<InvioSapLockDto>(connection!, InvioSapQueryBuilder.SelectLock,
            new { IdAzione = InvioSapRegole.IdAzioneGeneraJson }, transaction);
}
