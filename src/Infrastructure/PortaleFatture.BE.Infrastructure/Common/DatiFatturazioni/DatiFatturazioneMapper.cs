using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni;
public static class DatiFatturazioneMapper
{
    public static IEnumerable<DatiFatturazioneContatto>? Mapper(this List<DatiFatturazioneContattoCreateCommand> model)
    {
        if (model == null)
            return null;
        return model.Select(x => x.Mapper());
    } 

    public static DatiFatturazioneContatto Mapper(this DatiFatturazioneContattoCreateCommand model) =>
      new()
      {
          Email = model.Email,
          IdDatiFatturazione = model.IdDatiFatturazione 
      };

    public static DatiFatturazione Mapper(this DatiFatturazioneCreateCommand model, long id) =>
       new()
       {
           Cig = model.Cig,
           CodCommessa = model.CodCommessa,
           Cup = model.Cup,
           DataCreazione = model.DataCreazione!.Value,
           Id = id, 
           DataDocumento = model.DataDocumento,
           SplitPayment = model.SplitPayment,
           IdDocumento = model.IdDocumento,
           Map = model.Map,
           Pec = model.Pec, 
           TipoCommessa = model.TipoCommessa,
           Contatti = model.Contatti!.Mapper(),
           IdEnte = model.AuthenticationInfo.IdEnte,
           Prodotto = model.AuthenticationInfo.Prodotto
       };
}