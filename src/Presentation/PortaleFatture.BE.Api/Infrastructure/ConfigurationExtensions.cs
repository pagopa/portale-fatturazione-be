using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using PortaleFatture.BE.Api.Infrastructure.Culture;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure;
using PortaleFatture.BE.Infrastructure.Common.Documenti;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Scadenziari;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.Api.Infrastructure;
public static class ConfigurationExtensions
{
    private static readonly ModuleManager _moduleManager = new();
    public static IServiceCollection AddModules(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var isProd = builder.Environment.IsProduction();

        PortaleFattureOptions options = new();
        configuration.GetSection(nameof(PortaleFattureOptions)).Bind(options);

        if (isProd)
        {
            AsyncHelper.RunSync(options.VaultClientSettings);

            builder.Logging.AddApplicationInsights(
                configureTelemetryConfiguration: (config) =>
                config.ConnectionString = options.ApplicationInsights,
                configureApplicationInsightsLoggerOptions: (options) => { }
                );
        }

        services.AddSingleton<IPortaleFattureOptions>(options);

        services.AddAssemblyToModuleRegistration(typeof(ConfigurationExtensions).Assembly);

        services.AddLogging(o => o.AddConfiguration(configuration.GetSection(Module.LoggingLabel)));

        services
            .AddJwtOrApiKeyAuthentication(options!)
            .AddIdentities(options!);

        services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(o =>
            {
                o.SwaggerDoc("v1", new OpenApiInfo { Title = "Portale Fatture", Version = "v1" });
                o.AddSecurityDefinition("nonce", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Query,
                    Name = "nonce",
                    Description = "Nonce query string expected session key"
                });
                o.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "nonce"
                            }
                        },
                        Array.Empty<string>()
                    }
                }); 
                o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JSON Web Token based security"
                });
                o.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            })
            .AddCors(opt =>
            {
                opt.AddPolicy(Module.CORSLabel, o =>
                    o
                    .WithOrigins(options.CORSOrigins!.Split(";"))
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            })
            .AddGateways()
            .AddInfrastructure()
            .AddLocalization();

        return services;
    }

    public static IServiceCollection AddGateways(this IServiceCollection services)
    {
        static IAsyncPolicy<HttpResponseMessage> ExponentialRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(10, retryAttempt => TimeSpan.FromSeconds(1));
        }

        services.AddHttpClient(Module.GatewayLabel, (_, client) =>
        {
            client.DefaultRequestHeaders.Add("ConsistencyLevel", "eventual");
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(5))
        .AddPolicyHandler(ExponentialRetryPolicy());

        return services;
    }

    public static IServiceCollection AddAssemblyToModuleRegistration(this IServiceCollection services, Assembly assembly)
    {
        _moduleManager.AddModulesFromAssembly(assembly);
        _moduleManager.RegisterModules(services);

        return services;
    }

    private static IApplicationBuilder MapEndpoints(this WebApplication application)
    {
        return _moduleManager.MapEndpoints(application);
    }

    [DebuggerStepThrough]
    public static WebApplication UseModules(this WebApplication application)
    {
        if (!application.Environment.IsDevelopment())
            application.UseHttpsRedirection();

        application
            .UseCulture()
            .UseSwagger()
            .UseSwaggerUI()
            .UseCors(Module.CORSLabel)
            .UseHttpsRedirection()
            .UseExceptionHandler(exceptionHandlerApp =>
                exceptionHandlerApp.Run(context =>
                {
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var exception = exceptionHandlerPathFeature?.Error;
                    var problem = exception switch
                    {
                        SessionException => Results.Problem(statusCode: StatusCodes.Status419AuthenticationTimeout, detail: exception.Message),
                        ConfigurationException => Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: exception.Message),
                        SecurityException => Results.Problem(statusCode: StatusCodes.Status401Unauthorized),
                        RoleException => Results.Problem(statusCode: StatusCodes.Status403Forbidden),
                        DomainException => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: exception.Message),
                        ValidationException => Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: exception.Message),
                        NotFoundException => Results.Problem(statusCode: StatusCodes.Status404NotFound, detail: exception.Message),
                        not null => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: exception.Message),
                        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Generic error, contact system administrator")
                    };
                    return problem.ExecuteAsync(context);
                }));

        application.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        application
            .MapHealthChecks(Module.HealthcheckLabel)
            .WithMetadata(new AllowAnonymousAttribute());

        application
            .UseAuthentication()
            .UseAuthorization();

        application
            .MapEndpoints();

        application.UseMiddleware<NonceMultiTabsMiddleware>();

        return application;
    }

    private static IServiceCollection AddInfrastructure(
    this IServiceCollection services)
    {
        services
            .AddPersistence();

        services
            .AddPortaleFattureHealthChecks();

        services.AddMediatR(x => x.RegisterServicesFromAssembly(typeof(RootInfrastructure).Assembly));

        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = true;
            options.SerializerOptions.IncludeFields = true;
        });

        services.AddHttpClient();
        services.AddSingleton<ISelfCareHttpClient, SelfCareHttpClient>();
        services.AddSingleton<IPagoPAHttpClient, PagoPAHttpClient>();
        services.AddSingleton<IMicrosoftGraphHttpClient, MicrosoftGraphHttpClient>();

        var hostingEnvironment = services
            .BuildServiceProvider()
            .GetRequiredService<IWebHostEnvironment>();
        var rootPath = hostingEnvironment.ContentRootPath;
        services.AddSingleton<IDocumentBuilder>(new DocumentBuilder(rootPath));
        return services;
    }

    private static IServiceCollection AddIdentities(this IServiceCollection services, IPortaleFattureOptions options)
    {
        JwtConfiguration jwtAuth = options.JWT!;
        services
            .AddScoped<ITokenService>(_ => new JwtTokenService(jwtAuth))
            .AddScoped<IIdentityUsersService, IdentityUsersService>()
            .AddSingleton<ISelfCareTokenService, SelfCareTokenService>()
            .AddSingleton<IPagoPATokenService, PagoPATokenService>()
            .AddSingleton<IProfileService, ProfileService>()
            .AddSingleton<IAesEncryption>(new AesEncryption(jwtAuth.Secret!));

        services
            .AddAuthorization();

        return services;
    }

    private static IServiceCollection AddJwtOrApiKeyAuthentication(this IServiceCollection services, IPortaleFattureOptions foptions)
    {
        IdentityModelEventSource.ShowPII = true;
        services
           .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
           .AddJwtBearer(options => options.JwtAuthenticationConfiguration(foptions.JWT!));
        services.AddAuthorizationBuilder()
        .AddPolicy(Module.SelfCarePolicy, policy =>
            policy
                .RequireClaim(Module.SelfCarePolicyClaim, AuthType.SELFCARE))
        .AddPolicy(Module.PagoPAPolicy, policy =>
            policy
                .RequireClaim(Module.SelfCarePolicyClaim, AuthType.PAGOPA));

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        return services;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        var options = services
            .BuildServiceProvider()
            .GetRequiredService<IPortaleFattureOptions>();

        var dbConnectionString = options.ConnectionString ??
              throw new ConfigurationException("Db connection string not configured");

        var fattureSchema = options.FattureSchema ??
                      throw new ConfigurationException($"Db schema fatture not existent");

        var selfCareSchema = options.SelfCareSchema ??
              throw new ConfigurationException($"Db schema fatture not existent");

        services.AddSingleton<ISelfCareDbContextFactory>(new DbContextFactory(dbConnectionString, selfCareSchema));
        services.AddSingleton<IFattureDbContextFactory>(new DbContextFactory(dbConnectionString, fattureSchema));
        services.AddSingleton<IScadenziarioService, ScadenziarioService>();

        return services;
    }

    private static IServiceCollection AddPortaleFattureHealthChecks(
        this IServiceCollection services)
    {
        services
            .AddHealthChecks();
        return services;
    }


    [DebuggerStepThrough]
    private static RouteHandlerBuilder SetOpenApi(this RouteHandlerBuilder builder, string[] tags, string? name = null,
        string? displayName = null, string? description = null, string? summary = null, string? securityPolicy = null,
        params object[] metadata)
    {
        var result = builder
            .WithOpenApi()
            .RequireCors(Module.CORSLabel)
            .WithTags(tags);

        if (securityPolicy is not null)
            builder.RequireAuthorization(securityPolicy);
        else
            builder.RequireAuthorization();

        if (name is not null)
            result = result.WithSummary(name);

        if (displayName is not null)
            result = result.WithName(displayName);

        if (description is not null)
            result = result.WithDescription(description);

        if (summary is not null)
            result = result.WithSummary(summary);

        if (metadata.Any())
            result = result.WithMetadata(metadata);

        return result;
    }

    [DebuggerStepThrough]
    public static RouteHandlerBuilder SetOpenApi(this RouteHandlerBuilder builder, string groupName,
        string? name = null,
        string? displayName = null, string? description = null, string? summary = null, string? securityPolicy = null,
        params object[] metadata)
    {
        return builder
            .SetOpenApi(new[] { groupName }, name, displayName, description, summary,
            securityPolicy, metadata);
    }
}