using PortaleFatture.BE.Api.Modules.DatiConfigurazioneModuloCommesse.Request;
using PortaleFatture.BE.Api.Modules.DatiConfigurazioneModuloCommesse.Response;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

namespace PortaleFatture.BE.Api.Modules.DatiConfigurazioneModuloCommesse.Extensions;

public static class DatiConfigurazioneModuloCommessaExtensions
{
    public static DatiConfigurazioneModuloCommessaCreateCommand? Mapper(this DatiConfigurazioneModuloCommessaCreateRequest model)
    {
        try
        {
            var idTipoContratto = model.IdTipoContratto;
            var prodotto = model.Prodotto;
            var tipi = model.Tipi;
            var categorie = model.Categorie;

            return new()
            {
                Tipi = tipi!.Select(x => new DatiConfigurazioneModuloCommessaCreateTipoCommand()
                {
                    IdTipoContratto = idTipoContratto,
                    Prodotto = prodotto,
                    MediaNotificaInternazionale = x.PrezzoMedioNotificaInternazionale,
                    MediaNotificaNazionale = x.PrezzoMedioNotificaNazionale,
                    IdTipoSpedizione = x.IdTipoSpedizione
                }).ToList(),
                Categorie = categorie!.Select(x => new DatiConfigurazioneModuloCommessaCreateCategoriaCommand()
                {
                    IdTipoContratto = idTipoContratto,
                    Prodotto = prodotto,
                    Percentuale = x.Percentuale,
                    IdCategoriaSpedizione = x.IdCategoriaSpedizione
                }).ToList(),
            };
        }
        catch  
        { 
            return null;
        } 
    }

    public static DatiConfigurazioneModuloCommessaResponse Mapper(this DatiConfigurazioneModuloCommessa model)
    {
        return new()
        {
            Tipi = model.Tipi!.Select(x => x.Mapper()),
            Categorie = model.Categorie!.Select(x => x.Mapper()),
        };
    }

    public static DatiConfigurazioneModuloCategoriaCommessaResponse Mapper(this DatiConfigurazioneModuloCategoriaCommessa model) =>
       new()
       {
           Descrizione = model.Descrizione!.Replace("[percent]", model.Percentuale.ToString()),
           IdCategoriaSpedizione = model.IdCategoriaSpedizione,
           Percentuale = model.Percentuale
       };

    public static DatiConfigurazioneModuloTipoCommessaResponse Mapper(this DatiConfigurazioneModuloTipoCommessa model) =>
       new()
       {
           Descrizione = model.Descrizione,
           MediaNotificaInternazionale = model.MediaNotificaInternazionale,
           MediaNotificaNazionale = model.MediaNotificaNazionale,
           IdTipoSpedizione = model.IdTipoSpedizione
       };
}


