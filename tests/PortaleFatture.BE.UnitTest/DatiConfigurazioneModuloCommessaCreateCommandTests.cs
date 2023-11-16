using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;

namespace PortaleFatture.BE.UnitTest;

public class DatiConfigurazioneModuloCommessaCreateCommandTests
{
    private IDbContextFactory _factory;
    private ILogger<DatiConfigurazioneModuloCommessaCreateCommandTests> _logger;
    private IStringLocalizer<Localization> _localizer;
    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        _factory = ServiceProvider.GetRequiredService<IFattureDbContextFactory>();
        _logger = ServiceProvider.GetRequiredService<ILogger<DatiConfigurazioneModuloCommessaCreateCommandTests>>();
        _localizer = ServiceProvider.GetRequiredService<IStringLocalizer<Localization>>();
        _handler = ServiceProvider.GetRequiredService<IMediator>();
    }

    [Test]
    public async Task Create_ShouldSucceed_True()
    {
        var reqProdotti = new ProdottoQueryGetAll();
        var prodotti = await _handler.Send(reqProdotti);
        var prodotto = prodotti.FirstOrDefault();
        var reqSpedizioni = new SpedizioneQueryGetAll();
        var rCategoriaSpedizioni = await _handler.Send(reqSpedizioni);
        var spedizioni = rCategoriaSpedizioni.SelectMany(x => x.TipoSpedizione!);

        var reqContratti = new TipoContrattoQueryGetAll();
        var contratti = await _handler.Send(reqContratti);
        var contratto = contratti.FirstOrDefault();

        DateTime adesso = DateTime.UtcNow.ItalianTime();
        var expectedDescrizione = "blebla";
        var expectedPercentuale = 30;

        var expectedMediaNazionale = (decimal) new Random().NextDouble();
        var expectedMediaInternazionale = (decimal)new Random().NextDouble();
        var command = new DatiConfigurazioneModuloCommessaCreateCommand
        {
            Tipi = new(),
            Categorie = new()
        };
        foreach (var tipo in spedizioni)
        {
            command.Tipi.Add(new DatiConfigurazioneModuloCommessaCreateTipoCommand()
            {
                TipoSpedizione = tipo.Id,
                DataCreazione = adesso,
                DataInizioValidita = adesso,
                Descrizione = expectedDescrizione,
                MediaNotificaInternazionale = expectedMediaNazionale,
                MediaNotificaNazionale = expectedMediaInternazionale,
                Prodotto = prodotto!.Nome,
                IdTipoContratto = contratto!.Id
            });
        }
        foreach (var cat in rCategoriaSpedizioni)
        {

            command.Categorie.Add(new DatiConfigurazioneModuloCommessaCreateCategoriaCommand()
            {
                Percentuale = expectedPercentuale,
                DataCreazione = adesso,
                DataInizioValidita = adesso,
                Descrizione = expectedDescrizione,
                Prodotto = prodotto!.Nome,
                IdTipoContratto = contratto!.Id,
                IdCategoriaSpedizione = cat.Id
            }); ;
        }
        var conf = await _handler.Send(command);
        Assert.IsNotNull(conf); 
    }

    //[Test]
    //public async Task Update_ShouldSucceed_True()
    //{
    //    var reqProdotti = new ProdottoQueryGetAll();
    //    var prodotti = await _handler.Send(reqProdotti);
    //    var prodotto = prodotti.FirstOrDefault();
    //    var reqSpedizioni = new SpedizioneQueryGetAll();
    //    var rspedizioni = await _handler.Send(reqSpedizioni);
    //    var spedizioni = rspedizioni.SelectMany(x => x.TipoSpedizione!);

    //    var reqContratti = new TipoContrattoQueryGetAll();
    //    var contratti = await _handler.Send(reqContratti);
    //    var contratto = contratti.FirstOrDefault();
    //    var lista = new DatiConfigurazioneModuloCommessaCreateListCommand();
    //    lista.DatiConfigurazioneModuloCommessaLista = new();
    //    DateTime adesso = DateTime.UtcNow.ItalianTime();
    //    var expectedDescrizione = "blebla";
    //    foreach (var tipo in spedizioni)
    //    {
    //        lista.DatiConfigurazioneModuloCommessaLista.Add(new DatiConfigurazioneModuloCommessaCreateCommand()
    //        {
    //            TipoSpedizione = tipo.Id,
    //            DataCreazione = adesso,
    //            DataInizioValidita = adesso,
    //            Descrizione = expectedDescrizione,
    //            MediaNotificaInternazionale = 10M,
    //            MediaNotificaNazionale = 9M,
    //            Prodotto = prodotto!.Nome,
    //            TipoContratto = contratto!.Id
    //        });
    //    }
    //    var conf = await _handler.Send(lista);
    //    Assert.IsNotNull(conf);
    //    foreach (var tipo in spedizioni)
    //    {
    //        lista.DatiConfigurazioneModuloCommessaLista.Add(new DatiConfigurazioneModuloCommessaCreateCommand()
    //        {
    //            TipoSpedizione = tipo.Id,
    //            DataCreazione = adesso,
    //            DataInizioValidita = adesso,
    //            Descrizione = expectedDescrizione,
    //            MediaNotificaInternazionale = 10M,
    //            MediaNotificaNazionale = 9M,
    //            Prodotto = prodotto!.Nome,
    //            TipoContratto = contratto!.Id
    //        });
    //    }
    //}
}