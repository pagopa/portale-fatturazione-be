using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Middleware;
using PortaleFatture.BE.Function.API.Models;
using PortaleFatture.BE.Infrastructure;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari;
using PortaleFatture.BE.Infrastructure.Gateway;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(app => {
        app.UseMiddleware<AuthMiddleware>();
        app.UseMiddleware<LogCustomDataMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        //services.AddFunctionsWorkerDefaults();
        var assemblies = new[]
            {
                Assembly.GetExecutingAssembly(),
                typeof(RootInfrastructure).Assembly
            };

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies));
        services.AddSingleton<ILogger<AuthMiddleware>, Logger<AuthMiddleware>>();

        var configuration = context.Configuration;
        var aesKey = configuration["AES_KEY"];  
        var dbConnectionString = configuration["CONNECTION_STRING"];
        var customDomain = configuration["OpenApi:HostNames"];

        services.AddSingleton<IConfigurazione>(new Configurazione()
        {
             AESKey = aesKey,
             ConnectionString  = dbConnectionString,
             CustomDomain = customDomain
        });

        services.AddSingleton<IAesEncryption>(new AesEncryption(aesKey!));
        services.AddSingleton<IFattureDbContextFactory>(new DbContextFactory(dbConnectionString!, "pfw"));
        services.AddSingleton<ISelfCareDbContextFactory>(new DbContextFactory(dbConnectionString!, "pfd"));
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddControllersWithViews()
                .AddDataAnnotationsLocalization()
                .AddViewLocalization();
        services.AddSingleton<IScadenziarioService, ScadenziarioService>();
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


