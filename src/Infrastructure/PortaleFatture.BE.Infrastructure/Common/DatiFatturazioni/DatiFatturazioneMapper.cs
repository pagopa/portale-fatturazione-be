using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands;
using PortaleFatture.BE.Infrastructure.Extensions;

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
           IdEnte = model.IdEnte,
           DataDocumento = model.DataDocumento,
           SplitPayment = model.SplitPayment,
           IdDocumento = model.IdDocumento,
           Map = model.Map,
           Pec = model.Pec,
           Prodotto = model.Prodotto,
           TipoCommessa = model.TipoCommessa,
           Contatti = model.Contatti!.Mapper() 
       };
}