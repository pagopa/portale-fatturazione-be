using Dapper;
using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries.Persistence.Builder;

internal static class ContestazioneSQLBuilder
{ 
    private static SqlBuilder CreateSelect()
    {
        Contestazione? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.Id).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.TipoContestazione).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.IdNotifica).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.NoteEnte).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.NoteSend).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.NoteRecapitista).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.NoteConsolidatore).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.RispostaEnte).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.StatoContestazione).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.Onere).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.DataInserimentoEnte).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.DataModificaEnte).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.DataInserimentoSend).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.DataModificaSend).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.DataInserimentoRecapitista).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.DataModificaRecapitista).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.DataInserimentoConsolidatore).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.DataModificaConsolidatore).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.DataChiusura).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.Anno).GetAsColumn<Contestazione>());
        builder.Select(nameof(@obj.Mese).GetAsColumn<Contestazione>());
        return builder;
    }

    private static string WhereByIdNotifica()
    {
        Contestazione? obj;
        var fieldId = nameof(@obj.IdNotifica).GetColumn<Contestazione>();
        return $"{fieldId} = @{nameof(@obj.IdNotifica)}";
    }


    public static string SelectByIdNotifica()
    {
        var tableName = nameof(Contestazione);
        tableName = tableName.GetTable<Contestazione>(); 
        var builder = CreateSelect();
        var where = WhereByIdNotifica();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    } 
}