using PortaleFatture.BE.Api.Modules.DatiCommesse.Payload;
using PortaleFatture.BE.Core.Entities.DatiCommesse;
using PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Commands;

namespace PortaleFatture.BE.Api.Modules.DatiCommesse.Extensions;
public static class DatiCommessaExtensions
{
    public static string GenerateFakeIdEnte()
    {
        return Guid.NewGuid().ToString();
    }

    public static DatiCommessaContattoCreateCommand Mapper(this DatiCommessaContattoCreateRequest model) =>
       new()
       {
           Email = model.Email,
           Tipo = model.Tipo
       };

    public static DatiCommessaCreateCommand Mapper(this DatiCommessaCreateRequest model, string idEnte) =>
       new()
       {
           Cig = model.Cig,
           CodCommessa = model.CodCommessa,
           Cup = model.Cup,
           FlagOrdineContratto = model.FlagOrdineContratto,
           DataDocumento = model.DataDocumento,
           SplitPayment = model.SplitPayment,
           IdTipoContratto = model.IdTipoContratto,
           IdDocumento = model.IdDocumento,
           Map = model.Map,
           IdEnte = idEnte,
           Contatti = model.Contatti?.Select(x => x.Mapper()).ToList()
       };

    public static DatiCommessaUpdateCommand Mapper(this DatiCommessaUpdateRequest model) =>
       new()
       {
           Id = model.Id,
           Cig = model.Cig,
           CodCommessa = model.CodCommessa,
           Cup = model.Cup,
           FlagOrdineContratto = model.FlagOrdineContratto,
           DataDocumento = model.DataDocumento,
           SplitPayment = model.SplitPayment,
           IdTipoContratto = model.IdTipoContratto,
           IdDocumento = model.IdDocumento,
           Map = model.Map, 
           Contatti = model.Contatti?.Select(x => x.Mapper()).ToList()
       };

    public static DatiCommessaContattoResponse Mapper(this DatiCommessaContatto model) =>
       new()
       {
           Email = model.Email,
           Tipo = model.Tipo
       };

    public static DatiCommessaResponse Mapper(this DatiCommessa model) =>
       new()
       {
           Cig = model.Cig,
           CodCommessa = model.CodCommessa,
           Cup = model.Cup,
           FlagOrdineContratto = model.FlagOrdineContratto,
           DataDocumento = model.DataDocumento,
           SplitPayment = model.SplitPayment,
           IdTipoContratto = model.IdTipoContratto,
           IdDocumento = model.IdDocumento,
           Map = model.Map,
           IdEnte = model.IdEnte,
           Id = model.Id,
           DataCreazione = model.DataCreazione,
           DataModifica = model.DataModifica,
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
}