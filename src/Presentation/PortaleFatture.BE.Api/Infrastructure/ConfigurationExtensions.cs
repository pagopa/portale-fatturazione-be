using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using PortaleFatture.BE.Api.Infrastructure.Culture;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.Api.Infrastructure;
public static class ConfigurationExtensions
{
    private static readonly ModuleManager ModuleManager = new();

    [DebuggerStepThrough]
    private static string[] ToArray(this string list, char separator = ';')
    {
        return list.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToArray();
    }

    public static IServiceCollection AddModules(
        this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddAssemblyToModuleRegistration(typeof(ConfigurationExtensions).Assembly);

        services.AddLogging(o => o.AddConfiguration(configuration.GetSection("Logging")));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "issuer",
                    ValidateAudience = true,
                    ValidAudience = "audience",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("secret")),
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Is-Token-Expired", "true");
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services
            .AddAuthorization()
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(o =>
            {
                o.SwaggerDoc("v1", new OpenApiInfo { Title = "Portale Fatture", Version = "v1" });
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
            .AddCors(options =>
            {
                options.AddPolicy("portalefatture", o =>
                    o.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            })
            .AddGateways()
            .AddInfrastructure(configuration)
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

        services.AddHttpClient("gateway", (_, client) =>
        {
            client.DefaultRequestHeaders.Add("ConsistencyLevel", "eventual");
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(5))
        .AddPolicyHandler(ExponentialRetryPolicy());

        return services;
    }

    public static IServiceCollection AddAssemblyToModuleRegistration(this IServiceCollection services, Assembly assembly)
    {
        ModuleManager.AddModulesFromAssembly(assembly);
        ModuleManager.RegisterModules(services);

        return services;
    }


    private static IApplicationBuilder MapEndpoints(this WebApplication application)
    {
        return ModuleManager.MapEndpoints(application);
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
            .UseCors("portalefatture")
            .UseHttpsRedirection()
            .UseExceptionHandler(exceptionHandlerApp =>
                exceptionHandlerApp.Run(context =>
                {
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var exception = exceptionHandlerPathFeature?.Error;
                    var problem = exception switch
                    {
                        SecurityException => Results.Problem(statusCode: StatusCodes.Status401Unauthorized),
                        DomainException => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: exception.Message),
                        ValidationException => Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: exception.Message),
                        NotFoundException => Results.Problem(statusCode: StatusCodes.Status404NotFound, detail: exception.Message),
                        not null => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: exception.Message),
                        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Generic error, contact system administrator")
                    };
                    return problem.ExecuteAsync(context);
                }));

        // used by proxy to forward 
        application.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        application
            .MapHealthChecks("health")
            .WithMetadata(new AllowAnonymousAttribute());

        application
            .UseAuthentication()
            .UseAuthorization();

        application
            .MapEndpoints();

        return application;
    }
    public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
    {
        services.Configure<PortaleFattureOptions>(configuration.GetSection(nameof(PortaleFattureOptions)));
        services
            .AddPersistence()
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

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
        services.AddSingleton<IPagoPaHttpClient, PagoPaHttpClient>();
        return services;
    }

    private static IServiceCollection AddIdentities(this IServiceCollection services)
    {
        services
            .AddIdentityCore<AuthenticationInfo>(options =>
            {
                options.Lockout.AllowedForNewUsers = false;
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
            })
            .AddRoles<IdentityRole>() 
            .AddDefaultTokenProviders()
            .AddTokenProvider<DataProtectorTokenProvider<AuthenticationInfo>>(TokenOptions.DefaultProvider);

        services
            .AddScoped<ITokensService, JwtTokenService>()
            .AddScoped<IRolesService, IdentityRolesService>()
            .AddScoped<IUsersService, IdentityUsersService>();

        services
            .AddAuthorization();

        return services;
    }

    private static IServiceCollection AddJwtOrApiKeyAuthentication(this IServiceCollection services,
        JwtConfiguration jwtAuth)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.JwtAuthenticationConfiguration(jwtAuth)); 

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>(); 
        return services;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        var optionsMonitor = services
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<PortaleFattureOptions>>();

        var options = optionsMonitor.CurrentValue;
        var dbConnectionString = options.ConnectionString ??
                      throw new ConfigurationException("Db connection string not configured");


        var fattureSchema = options.FattureSchema ??
                      throw new ConfigurationException($"Db schema fatture not existent");

        var selfCareSchema = options.FattureSchema ??
              throw new ConfigurationException($"Db schema fatture not existent");

        services.AddSingleton<ISelfCareDbContextFactory>(new DbContextFactory(dbConnectionString, fattureSchema));
        services.AddSingleton<IFattureDbContextFactory>(new DbContextFactory(dbConnectionString, selfCareSchema)); 
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
            .RequireCors("portalefatture")
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