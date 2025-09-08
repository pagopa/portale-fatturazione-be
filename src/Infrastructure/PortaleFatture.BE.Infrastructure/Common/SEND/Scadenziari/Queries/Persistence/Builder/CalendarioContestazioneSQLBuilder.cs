using Dapper;
using PortaleFatture.BE.Core.Entities.SEND.Scadenziari;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries.Persistence.Builder;

internal static class CalendarioContestazioneSQLBuilder
{
    private static SqlBuilder CreateSelect()
    {
        CalendarioContestazione? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.AnnoContestazione).GetAsColumn<CalendarioContestazione>());
        builder.Select(nameof(@obj.MeseContestazione).GetAsColumn<CalendarioContestazione>());
        builder.Select(nameof(@obj.DataInizio).GetAsColumn<CalendarioContestazione>());
        builder.Select(nameof(@obj.DataFine).GetAsColumn<CalendarioContestazione>());
        builder.Select(nameof(@obj.DataVerifica).GetAsColumn<CalendarioContestazione>());
        return builder;
    }

    private static string WhereByAnnoMese()
    {
        CalendarioContestazione? obj;
        var fieldAnno = nameof(@obj.AnnoContestazione).GetColumn<CalendarioContestazione>();
        var fieldMese = nameof(@obj.MeseContestazione).GetColumn<CalendarioContestazione>();
        return $"{fieldAnno} = @{nameof(@obj.AnnoContestazione)} AND {fieldMese} = @{nameof(@obj.MeseContestazione)}";
    }


    public static string SelectByAnnoMese()
    {
        var tableName = nameof(CalendarioContestazione);
        tableName = tableName.GetTable<CalendarioContestazione>();
        var builder = CreateSelect();
        var where = WhereByAnnoMese();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    }

    private static string _selectAll = $@"
SELECT [MeseContestazione]
      ,[AnnoContestazione]
      ,[DataInizio]
      ,[DataFine]
      ,DATEADD(day, 30, [DataFine]) AS ChiusuraContestazioni
      ,DATEADD(day, 45, [DataFine]) AS TempoRisposta
      ,[DataVerifica]
      ,[DataCalcoloPrimoSecondo]
FROM [pfw].[ContestazioniCalendario] 
";
    public static string SelectAll()
    {
      return _selectAll;
    }
    public static string OrderBy()
    {
        CalendarioContestazione? @obj = null;
        return $" ORDER BY {nameof(@obj.AnnoContestazione).GetColumn<CalendarioContestazione>()} DESC, {nameof(@obj.MeseContestazione).GetColumn<CalendarioContestazione>()} DESC";
    }
}