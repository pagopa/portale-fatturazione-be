using System.Reflection;
using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Middleware;
using PortaleFatture.BE.Function.API.Models;
using PortaleFatture.BE.Infrastructure;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Services;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Services;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Services;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari;
using PortaleFatture.BE.Infrastructure.Gateway;
using PortaleFatture.BE.Infrastructure.Gateway.Storage;
using PortaleFatture.BE.Infrastructure.Gateway.Storage.pagoPA;
using StorageNotifiche = PortaleFatture.BE.Core.Common.StorageNotifiche;


var libPath = Path.Combine(AppContext.BaseDirectory, "native");
Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", libPath);

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(app =>
    {
        app.UseMiddleware<AuthMiddleware>();
        app.UseMiddleware<LogCustomDataMiddleware>();
    })
    .ConfigureFunctionsWorkerDefaults() 
    .ConfigureServices((context, services) =>
    {
        var assemblies = new[]
            {
                Assembly.GetExecutingAssembly(),
                typeof(RootInfrastructure).Assembly
            };

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies));
        services.AddSingleton<ILogger<AuthMiddleware>, Logger<AuthMiddleware>>();

        services.AddLocalization(options => options.ResourcesPath = "Resources");

        var configuration = context.Configuration;
        var aesKey = configuration["AES_KEY"];
        var dbConnectionString = configuration["CONNECTION_STRING"];
        var customDomain = configuration["OpenApi:HostNames"];

        services.AddSingleton<IConfigurazione>(new Configurazione()
        {
            AESKey = aesKey,
            ConnectionString = dbConnectionString,
            CustomDomain = customDomain
        });

        services.AddSingleton<IAesEncryption>(new AesEncryption(aesKey!));
        services.AddSingleton<IFattureDbContextFactory>(new DbContextFactory(dbConnectionString!, "pfw"));
        services.AddSingleton<ISelfCareDbContextFactory>(new DbContextFactory(dbConnectionString!, "pfd"));
        services.AddControllersWithViews()
                .AddDataAnnotationsLocalization()
                .AddViewLocalization();

        SqlMapper.AddTypeHandler(typeof(FattureListaDto), new JsonTypeHandler());

        var options = (new PortaleFattureOptions()
        {
            StorageNotifiche = new StorageNotifiche()
            {
                AccountName = configuration["STORAGE_ACCOUNT_NAME"],
                AccountKey = configuration["STORAGE_ACCOUNT_KEY"],
                BlobContainerName = configuration["STORAGE_NOTIFICHE"],
                CustomDNS = configuration["STORAGE_CUSTOM_HOSTNAME"]
            }
        });
        options.StorageRelDownload = new StorageRelDownload()
        {
            AccountName = configuration["STORAGE_ACCOUNT_NAME"],
            AccountKey = configuration["STORAGE_ACCOUNT_KEY"],
            BlobContainerName = configuration["STORAGE_REL_DOWNLOAD"],
            CustomDNS = configuration["STORAGE_CUSTOM_HOSTNAME"]
        };

        options.StorageREL = new StorageREL()
        {
            StorageRELAccountKey = configuration["STORAGE_ACCOUNT_KEY"],
            StorageRELAccountName = configuration["STORAGE_ACCOUNT_NAME"],
            StorageRELBlobContainerName = "relrighe",
            StorageRELCustomDns = configuration["STORAGE_CUSTOM_HOSTNAME"]
        };

        options.Storage = new Storage()
        {
            ConnectionString = configuration["StorageDocumenti:ConnectionString"],
            RelFolder =  "rel" /*configuration["StorageDocumenti:DocumentiFolder"]*/
        };

        options.StorageContestazioni = new StorageContestazioni()
        {
            AccountKey = configuration["StorageContestazioni:AccountKey"],
            AccountName = configuration["StorageContestazioni:AccountName"],
            BlobContainerName = configuration["StorageContestazioni:BlobContainerName"],
            CustomDns = configuration["StorageContestazioni:CustomDns"]
        };


        services.AddSingleton<IPortaleFattureOptions>(provider =>
        {
            return options;
        });

        services.AddSingleton<IBlobStorageNotifiche>(provider =>
        {
            return new BlobStorageNotifiche(options);
        });

        services.AddSingleton<IBlobStorageRelDownload>(provider =>
        {
            return new BlobStorageRelDownload(options);
        });


        services.AddSingleton<IRelRigheStorageService, RelRigheStorageService>();
        services.AddSingleton<IRelStorageService, RelStorageService>();
        services.AddSingleton<IContestazioniStorageService, ContestazioniStorageService>();
        services.AddSingleton<IDocumentStorageSASService, DocumentStorageSASService>();

        services.AddSingleton<IScadenziarioService, ScadenziarioService>();

        var rootPath = Environment.CurrentDirectory;
        services.AddSingleton<IDocumentBuilder>(new DocumentBuilder(rootPath));

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("*")
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        services.AddSwaggerGen(options =>
        {
            options.SchemaFilter<JsonIgnoreSchemaFilter>();
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Portale Fatturazione API",
                Version = "v1",
                Description = "API documentation for the Portale Fatturazione."
            });

            options.AddSecurityDefinition("ApiKeyAuth", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-Api-Key",
                Description = "API key required to access the API endpoints."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKeyAuth"
                            }
                        },
                        Array.Empty<string>()
                    }
            });
        });
    })
    .ConfigureAppConfiguration((context, config) =>
        {
            context.Configuration.GetSection("Swagger").GetSection("UI").Bind(config);
        })
    .Build();

host.Run();


