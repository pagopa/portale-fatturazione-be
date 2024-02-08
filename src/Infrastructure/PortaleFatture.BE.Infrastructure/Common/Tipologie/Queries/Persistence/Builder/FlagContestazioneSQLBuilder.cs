﻿using Dapper;
using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

public static class FlagContestazioneSQLBuilder
{
    private static SqlBuilder CreateSelect()
    {
        FlagContestazione? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.Id).GetAsColumn<FlagContestazione>());
        builder.Select(nameof(@obj.Flag).GetAsColumn<FlagContestazione>());
        return builder;
    }

    public static string SelectAll()
    {
        var tableName = nameof(FlagContestazione); 
        var builder = CreateSelect();
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName}");
        return builderTemplate.RawSql;
    }
}