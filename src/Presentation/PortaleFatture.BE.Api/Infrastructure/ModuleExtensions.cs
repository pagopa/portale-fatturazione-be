using System.Diagnostics;
using System.Reflection;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Exceptions;

namespace PortaleFatture.BE.Api.Infrastructure;

public static class ModuleExtensions
{
    public static async Task<PortaleFattureOptions> VaultClientSettings(this PortaleFattureOptions model)
    {
        model.JWT ??= new();

        var selfCareUri = Environment.GetEnvironmentVariable("SELF_CARE_URI") ??
             throw new ConfigurationException("Please specify a SELF_CARE_URI!");

        var selfCareCertEndpoint = Environment.GetEnvironmentVariable("SELFCARE_CERT_ENDPOINT") ??
             throw new ConfigurationException("Please specify a SELFCARE_CERT_ENDPOINT!");

        var validAudience = Environment.GetEnvironmentVariable("JWT_VALID_AUDIENCE") ??
             throw new ConfigurationException("Please specify a JWT_VALID_AUDIENCE!");

        var validIssuer = Environment.GetEnvironmentVariable("JWT_VALID_ISSUER") ??
             throw new ConfigurationException("Please specify a JWT_VALID_ISSUER!");

        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
             throw new ConfigurationException("Please specify a CONNECTION_STRING!");

        var secret = Environment.GetEnvironmentVariable("JWT_SECRET") ??
             throw new ConfigurationException("Please specify a JWT_VALID_ISSUER!");

        var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS") ??
             throw new ConfigurationException("Please specify a CORS_ORIGINS!");

        var selfCareTimeOut = Environment.GetEnvironmentVariable("SELF_CARE_TIMEOUT") ??
             throw new ConfigurationException("Please specify a SELF_CARE_TIMEOUT!");

        var selfCareAudience = Environment.GetEnvironmentVariable("SELF_CARE_AUDIENCE") ??
             throw new ConfigurationException("Please specify a SELF_CARE_AUDIENCE!");

        var adminKey = Environment.GetEnvironmentVariable("ADMIN_KEY") ??
             throw new ConfigurationException("Please specify a ADMIN_KEY!");

        var applicationInsights = Environment.GetEnvironmentVariable("APPLICATION_INSIGHTS") ??
             throw new ConfigurationException("Please specify an APPLICATION_INSIGHTS!"); 

        model.ConnectionString = connectionString; //await model.ConnectionString.Mapper();
        model.SelfCareUri = selfCareUri;
        model.SelfCareCertEndpoint = selfCareCertEndpoint;
        model.JWT.ValidAudience = validAudience;
        model.JWT.Secret = secret; //await model.JWT.Secret.Mapper();
        model.JWT.ValidIssuer = validIssuer;
        model.CORSOrigins = corsOrigins;
        model.SelfCareTimeOut = selfCareTimeOut;
        model.SelfCareAudience = selfCareAudience;
        model.AdminKey = adminKey;
        model.ApplicationInsights = applicationInsights;
        return model;
    }

    private static string _kvUri = "https://{0}.vault.azure.net";
    private static async Task<string> Mapper(this string? secretName)
    {
        var keyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME") ??
            throw new ConfigurationException("Please specify an KEY_VAULT_NAME!");

        var kvUri = String.Format(_kvUri, keyVaultName);
        var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential()) ??
            throw new ConfigurationException("Could not find any vault holy connected to KEY_VAULT_NAME!");

        var secret = await client.GetSecretAsync(secretName);
        return secret.Value.Value;
    }

    public static IEndpointRouteBuilder Map(this IEndpointRouteBuilder app, Type moduleType)
    { 
        if (app == null || moduleType == null)
            throw new ConfigurationException("Please specify an IEndpointRouteBuilder OR Module Type!");

        foreach (var method in moduleType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            app.Map(method, moduleType);
        }

        return app;
    }

    public static IEndpointConventionBuilder? Map(this IEndpointRouteBuilder app, MethodInfo method, Type moduleType,
        string? routePrefix = null)
    { 
        if (app == null || moduleType == null || method == null)
            throw new ConfigurationException("Please specify an IEndpointRouteBuilder OR MethodInfo OR Module Type!");

        var mapAttr = method.GetCustomAttribute<MapAttribute>();
        if (mapAttr is null)
        {
            return null;
        }

        var route = mapAttr.Route ?? method.Name.ToLowerInvariant();
        if (!string.IsNullOrEmpty(routePrefix))
        {
            route = $"{routePrefix}/{route}";
        }

        var options = new RequestDelegateFactoryOptions { ThrowOnBadRequest = true };
        var metaData = RequestDelegateFactory.InferMetadata(method, options);
        var delegateResult = RequestDelegateFactory.Create(method,
            ctx => ctx.RequestServices.GetRequiredService(moduleType), options, metaData);
        return app.Map(mapAttr.Method, route, delegateResult.RequestDelegate);
    }

    [DebuggerStepThrough]
    public static IEndpointConventionBuilder Map(this IEndpointRouteBuilder app, MapMethod method, string route,
        RequestDelegate mapDelegate)
    { 
        if (app == null || mapDelegate == null)
            throw new ConfigurationException("Please specify an IEndpointRouteBuilder OR Map RequestDelegate!");

        return method switch
        {
            MapMethod.Get => app.MapGet(route, mapDelegate),
            MapMethod.Post => app.MapPost(route, mapDelegate),
            MapMethod.Put => app.MapPut(route, mapDelegate),
            MapMethod.Delete => app.MapDelete(route, mapDelegate),
            MapMethod.Patch => app.MapPatch(route, mapDelegate),
            _ => throw new NotSupportedException($"Not supported MapMethod {method}")
        };
    }
}