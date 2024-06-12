using PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries;

namespace PortaleFatture.BE.Api.Modules.Fatture.Extensions;

public static class FattureExtensions
{
    public static FattureQueryRicerca Map(this FatturaRicercaRequest req, AuthenticationInfo authInfo)
    {
        return new FattureQueryRicerca(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdEnti = req.IdEnti,
            TipologiaFattura = req.TipologiaFattura
        };
    }
    public static FattureRelExcelQuery Mapv2(this FatturaRicercaRequest req, AuthenticationInfo authInfo)
    {
        return new FattureRelExcelQuery(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdEnti = req.IdEnti,
            TipologiaFattura = req.TipologiaFattura
        };
    }
    public static IEnumerable<FattureExcel> Map(this FattureListaDto model)
    {
        var result = new List<FattureExcel>();
        foreach (var item in model)
        {
            foreach (var pos in item.fattura.Posizioni!)
            {
                result.Add(new FattureExcel()
                {
                    Numero = item.fattura!.Numero,
                    Posizione = pos.CodiceMateriale,
                    Totale =  pos.Imponibile.ToString("0.00"),
                });
            }
            result.Add(new FattureExcel()
            {
                Causale = item.fattura!.Causale,
                DataFattura = item.fattura!.DataFattura,
                Divisa = item.fattura!.Divisa,
                IdContratto = item.fattura!.IdContratto,
                Numero = item.fattura!.Numero,
                IstitutioID = item.fattura.IstitutioID,
                MetodoPagamento = item.fattura.MetodoPagamento,
                OnboardingTokenID = item.fattura.OnboardingTokenID,
                Prodotto = item.fattura.Prodotto,
                RagioneSociale = item.fattura.RagioneSociale,
                TipologiaFattura = item.fattura.TipologiaFattura,
                TipoContratto = item.fattura.TipoContratto,
                Totale = item.fattura.Totale.ToString("0.00"),
                Identificativo = item.fattura.Identificativo,
                Sollecito = item.fattura.Sollecito,
                Split = item.fattura.Split,
                TipoDocumento = item.fattura.TipoDocumento,
                Posizione = "totale:", 
            });

            result.Add(new FattureExcel());
        }
        return result;
    }
}