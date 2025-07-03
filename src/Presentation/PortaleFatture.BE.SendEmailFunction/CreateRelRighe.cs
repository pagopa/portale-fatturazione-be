using Azure.Storage;
using Azure.Storage.Blobs;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture_BE_SendEmailFunction.Models;

namespace PortaleFatture_BE_SendEmailFunction;

public class CreateRelRighe(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<CreateRelRighe>();
    public async Task AddDocument(
    string sidtestata,
    string? nomeEnte,
    MemoryStream memoryStream,
    string? storageAccountName,
    string? storageAccountKey,
    string? blobContainerName,
    string extension = ".csv")
    {
        var idTestata = RelTestataKey.Deserialize(sidtestata);
        var storageSharedKeyCredential = new StorageSharedKeyCredential(storageAccountName, storageAccountKey);
        var blobServiceClient = new BlobServiceClient(new Uri($"https://{storageAccountName}.blob.core.windows.net"), storageSharedKeyCredential);
        var containerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
        try
        {
            memoryStream.Position = 0;
            var blobName = $"{idTestata.Anno}/{idTestata.Mese}/{idTestata.TipologiaFattura}/{idTestata.IdEnte}/{idTestata.IdContratto}/Rel_Report di dettaglio_{nomeEnte}_{idTestata.Mese}_{idTestata.Anno}{extension}";
            var blobClientDetailed = containerClient.GetBlobClient(blobName);
            await blobClientDetailed.UploadAsync(memoryStream, overwrite: true);
        }
        catch
        {
            var msg = $"Errore nel caricare il file {idTestata.Serialize()}!";
            _logger.LogError(msg);
            throw new DomainException(msg);
        }
    }

    [Function("CreateRelRighe")]
    public async Task<string> RunAsync([ActivityTrigger] CreateRelRigheDataRequest req)
    {
        var risposta = new RispostaRelRighe();

        try
        {
            // config
            ConfigurazioneSEND.StorageRELAccountName = GetEnvironmentVariable("StorageRELAccountName");
            ConfigurazioneSEND.StorageRELAccountKey = GetEnvironmentVariable("StorageRELAccountKey");
            ConfigurazioneSEND.StorageRELBlobContainerName = GetEnvironmentVariable("StorageRELBlobContainerName");
            ConfigurazioneSEND.ConnectionString = GetEnvironmentVariable("CONNECTION_STRING");

            if (String.IsNullOrEmpty(ConfigurazioneSEND.ConnectionString) ||
                    String.IsNullOrEmpty(ConfigurazioneSEND.StorageRELAccountName) ||
                    String.IsNullOrEmpty(ConfigurazioneSEND.StorageRELAccountKey) ||
                    String.IsNullOrEmpty(ConfigurazioneSEND.StorageRELBlobContainerName))
                throw new ConfigurationException("Wrong configuration")!;

            var services = new ServiceCollection();
            services.AddMediatR(x => x.RegisterServicesFromAssembly(typeof(RootInfrastructure).Assembly));
            services.AddSingleton<ISelfCareDbContextFactory>(new DbContextFactory(ConfigurazioneSEND.ConnectionString!, "pfd"));
            services.AddSingleton<IFattureDbContextFactory>(new DbContextFactory(ConfigurazioneSEND.ConnectionString!, "pfw"));
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddControllersWithViews()
                    .AddDataAnnotationsLocalization()
                    .AddViewLocalization();
            var serviceProvider = services.BuildServiceProvider();

     
            var mediator = serviceProvider.GetRequiredService<IMediator>();

            var anno = Convert.ToInt32(req.Anno);
            var mese = Convert.ToInt32(req.Mese);
            var tipologiafattura = req.TipologiaFattura;

            _logger.LogInformation("HTTP trigger function processed a request.");

           
            risposta = new RispostaRelRighe()
            {
                Anno = anno,
                Mese = mese,
                TipologiaFattura = tipologiafattura
            };

            var authInfo = new AuthenticationInfo()
            {

            };

            var query = new RelTestataQueryGetByListaEnti(authInfo)
            {
                Anno = anno,
                Mese = mese,
                TipologiaFattura = tipologiafattura,
            };

            _logger.LogInformation(query.Serialize());

            var allRels = await mediator.Send(query);

            _logger.LogInformation("Executed query.");

            if (!(allRels == null || allRels.Count == 0))
            {
                _logger.LogInformation($"Found Rels: {allRels.RelTestate!.Count()}");

                risposta.Count = allRels.RelTestate!.Count();

                foreach (var rel in allRels.RelTestate!)
                {
                    authInfo.IdEnte = rel.IdEnte;
                    var nomeEnte = rel.RagioneSociale;
                    var command = new RelRigheQueryGetById(authInfo)
                    {
                        IdTestata = rel.IdTestata   //76fd95f3-c1a1-410f-95c8-a6ac00989aae_97d4d355-ab9f-4a09-8fe1-d9901f181a77_SECONDO-SALDO_2024_7
                    };
                    var rels = await mediator.Send(command);
                    var stream = await rels!.ToStream<RigheRelDto, RigheRelDtoPagoPAMap>();

                    await AddDocument(
                        rel.IdTestata!,
                        nomeEnte,
                        (MemoryStream)stream,
                        ConfigurazioneSEND.StorageRELAccountName,
                        ConfigurazioneSEND.StorageRELAccountKey,
                        ConfigurazioneSEND.StorageRELBlobContainerName);
                }
            }
            else
            {
                risposta.Error += "Non ci sono rel per l'anno, mese e tipologia specificate";
                throw new DomainException(risposta.Serialize());
            }
        }
        catch (Exception ex)
        {
            risposta.DbConnection = false;
            risposta.Error = ex.Message;
            _logger.LogInformation(ex.Message);
            throw new DomainException(ex.Message, ex);
        }

        _logger.LogInformation(risposta.Serialize());
        return risposta.Serialize();
    }

    private static string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }
} 