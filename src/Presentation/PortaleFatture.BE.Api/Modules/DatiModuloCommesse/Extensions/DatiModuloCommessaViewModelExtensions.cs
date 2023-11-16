using PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload;
using PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Extensions;

public static class DatiModuloCommessaViewModelExtensions
{
    public static DatiModuloCommessaCreateListCommand Mapper(this DatiModuloCommessaCreateRequest model)
    {
        var cmd = new DatiModuloCommessaCreateListCommand
        {
            DatiModuloCommessaListCommand = new()
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

    public static DatiModuloCommessaResponse Mapper(this List<DatiModuloCommessa> models)
    {
        var cmd = new DatiModuloCommessaResponse
        {
            ModuliCommessa = new()
        };

        foreach (var md in models)
            cmd.ModuliCommessa.Add(new DatiModuloCommessaSimpleResponse()
            {
                IdTipoSpedizione = md.IdTipoSpedizione,
                NumeroNotificheInternazionali = md.NumeroNotificheInternazionali,
                NumeroNotificheNazionali = md.NumeroNotificheNazionali
            });
        return cmd;
    }
}