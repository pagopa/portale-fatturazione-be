using Azure;
using PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload;
using PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Extensions;

public static class DatiModuloCommessaViewModelExtensions
{
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

    public static DatiModuloCommessaResponse? Mapper(this ModuloCommessaDto model, string ruolo)
    {
        var stato = model == null ? null : model.DatiModuloCommessaTotale!.Select(x => x.Stato).FirstOrDefault();
 
        var cmd = new DatiModuloCommessaResponse
        {
            ModuliCommessa = [],
            Totale = [],
            TotaleModuloCommessaNotifica = new(),
            Modifica = stato == null ? ruolo == Ruolo.ADMIN : stato == StatoModuloCommessa.ApertaCaricato && ruolo == Ruolo.ADMIN
        }; 

        if (model is null)
            return cmd;

        foreach (var md in model.DatiModuloCommessa!)
            cmd.ModuliCommessa.Add(new DatiModuloCommessaSimpleResponse()
            {
                IdTipoSpedizione = md.IdTipoSpedizione,
                NumeroNotificheInternazionali = md.NumeroNotificheInternazionali,
                NumeroNotificheNazionali = md.NumeroNotificheNazionali,
                TotaleNotifiche = md.TotaleNotifiche
            });

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
        return cmd;
    }
}