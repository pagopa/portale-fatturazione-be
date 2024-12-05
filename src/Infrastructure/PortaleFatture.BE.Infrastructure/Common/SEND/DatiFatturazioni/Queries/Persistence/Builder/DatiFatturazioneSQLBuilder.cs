using Dapper;
using DocumentFormat.OpenXml.Spreadsheet;
using PortaleFatture.BE.Core.Entities.SEND.DatiFatturazioni;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries.Persistence.Builder;

public static class DatiFatturazioneSQLBuilder
{
    private static string WhereByIdEnte()
    {
        DatiFatturazione? obj;
        var fieldIdEnteName = nameof(@obj.IdEnte).GetColumn<DatiFatturazione>();
        return $"{fieldIdEnteName} = @{nameof(@obj.IdEnte)}";
    }

    private static string WhereById()
    {
        DatiFatturazione? obj;
        var fieldId = nameof(@obj.Id).GetColumn<DatiFatturazione>();
        return $"{fieldId} = @{nameof(@obj.Id)}";
    }

    private static SqlBuilder CreateSelect()
    {
        DatiFatturazione? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.Id).GetAsColumn<DatiFatturazione>());
        builder.Select(nameof(@obj.Cup));
        builder.Select(nameof(@obj.NotaLegale));
        builder.Select(nameof(@obj.CodCommessa));
        builder.Select(nameof(@obj.DataDocumento));
        builder.Select(nameof(@obj.SplitPayment));
        builder.Select(nameof(@obj.IdEnte).GetAsColumn<DatiFatturazione>());
        builder.Select(nameof(@obj.IdDocumento));
        builder.Select(nameof(@obj.Map));
        builder.Select(nameof(@obj.TipoCommessa).GetAsColumn<DatiFatturazione>());
        builder.Select(nameof(@obj.Prodotto).GetAsColumn<DatiFatturazione>());
        builder.Select(nameof(@obj.Pec));
        builder.Select(nameof(@obj.DataCreazione));
        builder.Select(nameof(@obj.DataModifica));
        builder.Select(nameof(@obj.CodiceSDI));
        return builder;
    }

    public static string SelectById()
    {
        var tableName = nameof(DatiFatturazione);
        var builder = CreateSelect();
        var where = WhereById();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    }

    public static string SelectByIdEnte()
    {
        var tableName = nameof(DatiFatturazione);
        var builder = CreateSelect();
        var where = WhereByIdEnte();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    }

    public static string SelectByDescrizione(bool all = true)
    {
        DatiFatturazioneEnteDto? @obj = null;
        var idEnte = nameof(@obj.IdEnte).GetColumn<DatiFatturazioneEnteDto>();
        var internalIdEnte = nameof(@obj.InternalInstituitionId).GetColumn<DatiFatturazioneEnteDto>();
        var id = nameof(Ente.IdEnte).GetColumn<Ente>();
        var prodotto = nameof(@obj.Prodotto).GetColumn<DatiFatturazioneEnteDto>();
        var internalProdotto = nameof(@obj.InternalProduct).GetColumn<DatiFatturazioneEnteDto>();
        var profilo = nameof(@obj.Profilo).GetColumn<DatiFatturazioneEnteDto>();

        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.RagioneSociale).GetAsColumn<DatiFatturazioneEnteDto>());
        builder.Select($"ISNULL({idEnte},e.{internalIdEnte}) as {nameof(@obj.IdEnte)}");
        builder.Select($"ISNULL({prodotto},{internalProdotto}) as {nameof(@obj.Prodotto)}");
        builder.Select($"{nameof(@obj.Profilo).GetAsColumn<DatiFatturazioneEnteDto>()}");
        builder.Select(nameof(@obj.Id).GetAsColumn<DatiFatturazioneEnteDto>());
        builder.Select(nameof(@obj.Cup));
        builder.Select(nameof(@obj.NotaLegale));
        builder.Select(nameof(@obj.CodCommessa));
        builder.Select(nameof(@obj.DataDocumento));
        builder.Select(nameof(@obj.SplitPayment));
        builder.Select(nameof(@obj.IdDocumento));
        builder.Select(nameof(@obj.Map));
        builder.Select(nameof(@obj.TipoCommessa).GetAsColumn<DatiFatturazioneEnteDto>());
        builder.Select(nameof(@obj.Pec));
        builder.Select(nameof(@obj.DataCreazione));
        builder.Select(nameof(@obj.DataModifica));
        builder.Select(nameof(@obj.DataModifica));

        var tableName = $"[schema]{nameof(DatiFatturazioneEnteDto).GetTable<DatiFatturazioneEnteDto>()} f";
        var rightEnteTable = $"[schema_inner].{nameof(Ente).GetTable<Ente>()}";
        var innerContrattoTable = $"[schema_inner].{nameof(Contratto).GetTable<Contratto>()}";

        builder.RightJoin($"{rightEnteTable} e on e.{internalIdEnte} = f.{idEnte}");
        builder.InnerJoin($"{innerContrattoTable} c on e.{internalIdEnte} = c.{internalIdEnte}");

        if (!all)
            builder.Where(WhereByIdEnti());

        var builderTemplate = builder.AddTemplate($"Select /*top*/ /**select**/ from {tableName} /**rightjoin**/ /**innerjoin**/ /**where**/ ");
        return builderTemplate.RawSql;
    }

    private static string WhereBySearch()
    {
        Ente? obj;
        var fieldDescription = nameof(@obj.Descrizione).GetColumn<Ente>();
        return $"{fieldDescription} LIKE '%' + @{nameof(@obj.Descrizione)} + '%'";
    }
    private static string WhereByIdEnti()
    {
        Ente? obj;
        var fieldDescription = nameof(@obj.IdEnte).GetColumn<Ente>();
        return $"e.{fieldDescription} IN  @IdEnti ";
    }
}