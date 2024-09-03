using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Core.Entities.Fatture;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Service;

public class ServiceWorkFlowFatture : IServiceWorkFlowFatture
{
    private List<WorkFlowRequisitoFatture>? _requisiti { get; set; }
    public ServiceWorkFlowFatture()
    {
        _requisiti =
        [
             new()
             {
                  TipologiaFattura = TipologiaFattura.ANTICIPO,
                  Ordine = 1,
                  Condition = 2,
             },
               new()
             {
                  TipologiaFattura = TipologiaFattura.ACCONTO,
                  Ordine = 2,
                  Condition = 3,
                  ExtraCondition = "IdEnte"
             },
            new()
             {
                  TipologiaFattura = TipologiaFattura.PRIMOSALDO,
                  Ordine = 3,
                  Condition = 3
             },
               new()
             {
                  TipologiaFattura = TipologiaFattura.SECONDOSALDO,
                  Ordine = 4,
                  Condition = 4
             },
             new()
             {
                  TipologiaFattura = TipologiaFattura.VAR_SEMESTRALE,
                  Ordine = 5,
                  Condition = 5
             },
              new()
             {
                  TipologiaFattura = TipologiaFattura.VAR_ANNUALE,
                  Ordine = 6,
                  Condition =  null
             }
        ];
    }

    public List<WorkFlowRequisitoFatture>? GetRequisiti()
    {
        return _requisiti;
    }

    public List<WorkFlowRequisitoFatture>? BiggerEqualThanCondition(int? condition)
    {
        return _requisiti!.Where(x => x.Ordine >= condition).OrderBy(x=> x.Ordine).ToList();
    }
}