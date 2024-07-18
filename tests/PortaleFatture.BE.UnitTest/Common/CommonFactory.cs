using MediatR;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;

namespace PortaleFatture.BE.UnitTest.Common;

public static class CommonFactory
{
    public static async Task<DatiConfigurazioneModuloCommessa?> CreateDatiCommessaConfiguration(
        IMediator handler,
        long idTipoContratto,
        string? nomeProdotto,
        decimal? expectedMediaNazionale = 1.0M,
        decimal? expectedMediaInternazionale = 2.0M,
        int? expectedPercentuale = 50)
    {
        var reqProdotti = new ProdottoQueryGetAll();
        var prodotti = await handler.Send(reqProdotti);
        var prodotto = prodotti.Where(x => x.Nome == nomeProdotto).FirstOrDefault();
        var reqSpedizioni = new SpedizioneQueryGetAll();
        var rCategoriaSpedizioni = await handler.Send(reqSpedizioni);
        var spedizioni = rCategoriaSpedizioni.SelectMany(x => x.TipoSpedizione!);

        var reqContratti = new TipoContrattoQueryGetAll();
        var contratti = await handler.Send(reqContratti);
        var contratto = contratti.Where(x => x.Id == idTipoContratto).FirstOrDefault();

        var adesso = DateTime.UtcNow.ItalianTime();
        var expectedDescrizione = "descrizione";
        expectedPercentuale = expectedPercentuale == null ? 30 : expectedPercentuale;

        expectedMediaNazionale = expectedMediaNazionale == null ? (decimal)new Random().NextDouble() : expectedMediaNazionale;
        expectedMediaInternazionale = expectedMediaInternazionale == null ? (decimal)new Random().NextDouble() : expectedMediaInternazionale;

        var command = new DatiConfigurazioneModuloCommessaCreateCommand
        {
            Tipi = [],
            Categorie = []
        };
        foreach (var tipo in spedizioni)
        {
            command.Tipi.Add(new DatiConfigurazioneModuloCommessaCreateTipoCommand()
            {
                IdTipoSpedizione = tipo.Id,
                DataCreazione = adesso,
                DataInizioValidita = adesso,
                Descrizione = expectedDescrizione,
                MediaNotificaInternazionale = expectedMediaNazionale.Value + tipo.Id,
                MediaNotificaNazionale = expectedMediaInternazionale.Value + tipo.Id,
                Prodotto = prodotto!.Nome,
                IdTipoContratto = contratto!.Id
            });
        }
        foreach (var cat in rCategoriaSpedizioni)
        {

            command.Categorie.Add(new DatiConfigurazioneModuloCommessaCreateCategoriaCommand()
            {
                Percentuale = expectedPercentuale.Value + cat.Id,
                DataCreazione = adesso,
                DataInizioValidita = adesso,
                Descrizione = expectedDescrizione,
                Prodotto = prodotto!.Nome,
                IdTipoContratto = contratto!.Id,
                IdCategoriaSpedizione = cat.Id
            }); ;
        }

        return await handler.Send(command);
    }
}