using PortaleFatture.BE.Core.Entities.DatiCommesse;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse;
public static class DatiCommessaMapper
{
    public static DatiCommessaContatto Mapper(this DatiCommessaContattoCreateCommand model) =>
      new()
      {
          Email = model.Email,
          IdDatiCommessa = model.IdDatiCommessa,
          Tipo = model.Tipo
      };

    public static DatiCommessa Mapper(this DatiCommessaCreateCommand model) =>
       new()
       {
           Cig = model.Cig,
           CodCommessa = model.CodCommessa,
           Cup = model.Cup,
           DataCreazione = model.DataCreazione,
           DataModifica = model.DataModifica,
           FlagOrdineContratto = model.FlagOrdineContratto,
           Id = model.Id,
           IdEnte = model.IdEnte,
           DataDocumento = model.DataDocumento,
           SplitPayment = model.SplitPayment,
           IdTipoContratto = model.IdTipoContratto,
           IdDocumento = model.IdDocumento,
           Map = model.Map,
           Contatti = model.Contatti!.IsNullNotAny()? null : model.Contatti!.Select(x => x.Mapper())  
       }; 
}