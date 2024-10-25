using Azure;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Commands;

namespace PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Extensions;

public static class DatiModuloCommessaViewModelExtensions
{
    public static DatiModuloCommessaByAnnoResponse Mapper(this ModuloCommessaByAnnoDto model)
    {
        Dictionary<int, ModuloCommessaMeseTotaleResponse>? totali = [];
        foreach (var tot in model.Totali!)
        {
            totali.Add(tot.Key, new ModuloCommessaMeseTotaleResponse()
            {
                IdCategoriaSpedizione = tot.Value.IdCategoriaSpedizione,
                Tipo = tot.Value.Tipo,
                TotaleCategoria = $"{tot.Value.TotaleCategoria} €"
            });
        }
        return new DatiModuloCommessaByAnnoResponse()
        {
            TotaleAnalogico = totali.Where(x => x.Key == 1).FirstOrDefault().Value == null ? "0,00 €" : totali.Where(x => x.Key == 1).FirstOrDefault().Value.TotaleCategoria,
            TotaleDigitale = totali.Where(x => x.Key == 2).FirstOrDefault().Value == null ? "0,00 €" : totali.Where(x => x.Key == 2).FirstOrDefault().Value.TotaleCategoria,
            AnnoValidita = model.AnnoValidita,
            IdEnte = model.IdEnte,
            IdTipoContratto = model.IdTipoContratto,
            MeseValidita = model.MeseValidita,
            Modifica = model.Modifica,
            Prodotto = model.Prodotto,
            Stato = model.Stato,
            Totale = $"{model.Totale} €",
            DataModifica = model.DataModifica.ToString("d")
        };
    }

    public static DatiModuloCommessaParzialiTotaleResponse Mapper(this DatiModuloCommessaParzialiTotale model)
    {
        return new DatiModuloCommessaParzialiTotaleResponse()
        {
            Analogico890Nazionali = $"{model.Analogico890Nazionali} €",
            AnalogicoARInternazionali = $"{model.AnalogicoARInternazionali} €",
            AnalogicoARNazionali = $"{model.AnalogicoARNazionali} €",
            Digitale = $"{model.Digitale} €",
            AnnoValidita = model.AnnoValidita,
            IdEnte = model.IdEnte,
            IdTipoContratto = model.IdTipoContratto,
            MeseValidita = model.MeseValidita,
            Modifica = model.Modifica,
            Prodotto = model.Prodotto,
            Stato = model.Stato,
            Totale = $"{model.Totale} €",
        };
    }

    public static DatiModuloCommessaCreateListCommand Mapper(this DatiModuloCommessaPagoPACreateRequest model, AuthenticationInfo authInfo, long idTipoContratto)
    {
        authInfo.Prodotto = model.Prodotto;
        authInfo.IdEnte = model.IdEnte;
        authInfo.IdTipoContratto = idTipoContratto;

        var cmd = new DatiModuloCommessaCreateListCommand(authInfo)
        {
            DatiModuloCommessaListCommand = []
        };

        foreach (var md in model.ModuliCommessa!)
            cmd.DatiModuloCommessaListCommand.Add(md.Mapper());
        return cmd;
    }

    public static DatiModuloCommessaCreateListCommand Mapper(this DatiModuloCommessaCreateRequest model, AuthenticationInfo authInfo)
    {
        var cmd = new DatiModuloCommessaCreateListCommand(authInfo)
        {
            DatiModuloCommessaListCommand = []
        };

        foreach (var md in model.ModuliCommessa!)
            cmd.DatiModuloCommessaListCommand.Add(md.Mapper());
        return cmd;
    }

    public static DatiModuloCommessaCreateCommand Mapper(this DatiModuloCommessaCreateSimpleRequest model)
    {
        return new DatiModuloCommessaCreateCommand()
        {
            NumeroNotificheInternazionali = model.NumeroNotificheInternazionali,
            NumeroNotificheNazionali = model.NumeroNotificheNazionali,
            IdTipoSpedizione = model.IdTipoSpedizione
        };
    }

    public static DatiModuloCommessaResponse? Mapper(this ModuloCommessaDto model, AuthenticationInfo info)
    {
        var cmd = new DatiModuloCommessaResponse
        {
            ModuliCommessa = [],
            Totale = [],
            TotaleModuloCommessaNotifica = new(),
            Modifica = model.Modifica,
            Anno = model.Anno,
            Mese = model.Mese,
            IdTipoContratto = info.IdTipoContratto!.Value
        };

        if (model is null)
            return cmd;

        foreach (var md in model.DatiModuloCommessa!)
        {
            cmd.ModuliCommessa.Add(new DatiModuloCommessaSimpleResponse()
            {
                IdTipoSpedizione = md.IdTipoSpedizione,
                NumeroNotificheInternazionali = md.NumeroNotificheInternazionali,
                NumeroNotificheNazionali = md.NumeroNotificheNazionali,
                TotaleNotifiche = md.TotaleNotifiche
            });
            cmd.IdTipoContratto = md.IdTipoContratto;
        }

        foreach (var mdt in model.DatiModuloCommessaTotale!)
        {
            cmd.Totale.Add(new TotaleDatiModuloCommessa()
            {
                TotaleValoreCategoriaSpedizione = mdt.TotaleCategoria,
                IdCategoriaSpedizione = mdt.IdCategoriaSpedizione
            });
        }

        cmd.TotaleModuloCommessaNotifica = new TotaleDatiModuloCommessaNotifica();
        foreach (var mdc in cmd.ModuliCommessa)
        {
            cmd.TotaleModuloCommessaNotifica.TotaleNumeroNotificheNazionali += mdc.NumeroNotificheNazionali;
            cmd.TotaleModuloCommessaNotifica.TotaleNumeroNotificheInternazionali += mdc.NumeroNotificheInternazionali;
            cmd.TotaleModuloCommessaNotifica.TotaleNumeroNotificheDaProcessare += mdc.TotaleNotifiche;
        }
        cmd.TotaleModuloCommessaNotifica.Totale = cmd.Totale.Select(x => x.TotaleValoreCategoriaSpedizione).Sum();

        var dataCreazione = model.DatiModuloCommessa!.Select(x => x.DataCreazione).FirstOrDefault();
        var dataModifica = model.DatiModuloCommessa!.Select(x => x.DataModifica).FirstOrDefault();
        cmd.DataModifica = dataModifica == DateTime.MinValue ? dataCreazione : dataModifica;

        return cmd;
    }
}