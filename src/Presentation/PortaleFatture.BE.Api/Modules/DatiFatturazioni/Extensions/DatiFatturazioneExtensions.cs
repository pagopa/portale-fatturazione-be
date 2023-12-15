using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Request;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands;

namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni.Extensions;
public static class DatiFatturazioneExtensions
{
    public static string GenerateFakeIdEnte()
    {
        return Guid.NewGuid().ToString();
    }

    public static DatiFatturazioneContattoCreateCommand Mapper(this DatiFatturazioneContattoCreateRequest model) =>
       new()
       {
           Email = model.Email
       };

    public static DatiFatturazioneCreateCommand Mapper(this DatiFatturazioneCreateRequest model, AuthenticationInfo info) =>
       new(info)
       {
           NotaLegale = model.NotaLegale,
           CodCommessa = model.CodCommessa,
           Cup = model.Cup,
           DataDocumento = model.DataDocumento,
           SplitPayment = model.SplitPayment,
           IdDocumento = model.IdDocumento,
           Map = model.Map,
           Contatti = model.Contatti?.Select(x => x.Mapper()).ToList(),
           Pec = model.Pec,
           TipoCommessa = model.TipoCommessa 
       };

    public static DatiFatturazioneUpdateCommand Mapper(this DatiFatturazioneUpdateRequest model, AuthenticationInfo authInfo) =>
       new(authInfo)
       {
           Id = model.Id,
           NotaLegale = model.NotaLegale,
           CodCommessa = model.CodCommessa,
           Cup = model.Cup,
           DataDocumento = model.DataDocumento,
           SplitPayment = model.SplitPayment,
           IdDocumento = model.IdDocumento,
           Map = model.Map,
           Contatti = model.Contatti?.Select(x => x.Mapper()).ToList(),
           Pec = model.Pec,
           TipoCommessa = model.TipoCommessa,
       };

    public static DatiFatturazioneContattoResponse Mapper(this DatiFatturazioneContatto model) =>
       new()
       {
           Email = model.Email
       };

    public static DatiFatturazioneResponse Mapper(this DatiFatturazione model) =>
       new()
       {
           NotaLegale = model.NotaLegale,
           CodCommessa = model.CodCommessa,
           Cup = model.Cup,
           DataDocumento = model.DataDocumento.DateTime,
           SplitPayment = model.SplitPayment,
           IdDocumento = model.IdDocumento,
           Map = model.Map,
           IdEnte = model.IdEnte,
           Id = model.Id,
           DataCreazione = model.DataCreazione.DateTime,
           DataModifica = model.DataModifica != null? model.DataModifica!.Value.DateTime: null,
           Pec = model.Pec,
           TipoCommessa = model.TipoCommessa,
           Prodotto = model.Prodotto,
           Contatti = model.Contatti?.Select(x => x.Mapper())
       };

    public static TipoContrattoResponse Mapper(this TipoContratto model) =>
       new()
       {
           Id = model.Id,
           Descrizione = model.Descrizione
       };

    public static IEnumerable<TipoContrattoResponse> Mapper(this IEnumerable<TipoContratto> models)
    {
        return models.Select(x => x.Mapper());
    }


    public static TipoCommessaResponse Mapper(this TipoCommessa model) =>
       new()
       {
           Id = model.Id,
           Descrizione = model.Descrizione
       };

    public static IEnumerable<TipoCommessaResponse> Mapper(this IEnumerable<TipoCommessa> models)
    {
        return models.Select(x => x.Mapper());
    }

    public static ProdottoResponse Mapper(this Prodotto model) =>
           new()
           {
               Nome = model.Nome
           };

    public static IEnumerable<ProdottoResponse> Mapper(this IEnumerable<Prodotto> models)
    {
        return models.Select(x => x.Mapper());
    }

    public static IEnumerable<TipoSpedizioneResponse> Mapper(this IEnumerable<TipoSpedizione> models)
    {
        return models.Select(x => x.Mapper());
    }

    public static TipoSpedizioneResponse Mapper(this TipoSpedizione model) =>
       new()
       {
           Descrizione = model.Descrizione,
           Id = model.Id,
           Tipo = model.Tipo
       };

    public static IEnumerable<CategoriaSpedizioneResponse> Mapper(this IEnumerable<CategoriaSpedizione> models)
    {
        return models.Select(x => x.Mapper());
    }

    public static CategoriaSpedizioneResponse Mapper(this CategoriaSpedizione model) =>
       new()
       {
           Descrizione = model.Descrizione,
           Id = model.Id,
           Tipo = model.Tipo,
           TipoSpedizione = model.TipoSpedizione!.Mapper().ToList()
       };
}