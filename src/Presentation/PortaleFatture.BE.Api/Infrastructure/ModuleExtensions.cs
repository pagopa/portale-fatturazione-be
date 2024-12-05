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
        model.AzureAd ??= new();
        model.Storage ??= new();
        model.StorageDocumenti??= new();
        model.Synapse??= new();
        model.StoragePagoPAFinancial??= new();
        model.SelfCareOnBoarding??= new();
        model.SupportAPIService??= new();

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

        var relFolder = Environment.GetEnvironmentVariable("STORAGE_REL_FOLDER") ??
             throw new ConfigurationException("Please specify a STORAGE_REL_FOLDER!");

        var documentFolder = Environment.GetEnvironmentVariable("STORAGE_DOCUMENTI_FOLDER") ??
             throw new ConfigurationException("Please specify a STORAGE_DOCUMENTI_FOLDER!");

        var storageConnectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTIONSTRING") ??
             throw new ConfigurationException("Please specify a STORAGE_CONNECTIONSTRING!");

        var storageDocumentiConnectionString = Environment.GetEnvironmentVariable("STORAGE_DOCUMENTI_CONNECTIONSTRING") ??
             throw new ConfigurationException("Please specify a STORAGE_DOCUMENTI_CONNECTIONSTRING!");

        var applicationInsights = Environment.GetEnvironmentVariable("APPLICATION_INSIGHTS") ??
             throw new ConfigurationException("Please specify an APPLICATION_INSIGHTS!"); 
        var azureADInstance = Environment.GetEnvironmentVariable("AZUREAD_INSTANCE") ??
             throw new ConfigurationException("Please specify an AZUREAD_INSTANCE!");

        var azureADTenantId = Environment.GetEnvironmentVariable("AZUREAD_TENANTID") ??
             throw new ConfigurationException("Please specify an AZUREAD_TENANTID!");

        var azureADClientId = Environment.GetEnvironmentVariable("AZUREAD_CLIENTID") ??
             throw new ConfigurationException("Please specify an AZUREAD_CLIENTID!");

        var azureADGroup = Environment.GetEnvironmentVariable("AZUREAD_ADGROUP") ??
             throw new ConfigurationException("Please specify an AZUREAD_ADGROUP!");

        var synapseWorkspaceName = Environment.GetEnvironmentVariable("SYNAPSE_WORKSPACE_NAME") ??
             throw new ConfigurationException("Please specify a SYNAPSE_WORKSPACE_NAME!");

        var pipelineNameSAP = Environment.GetEnvironmentVariable("PIPELINE_NAME_SAP") ??
             throw new ConfigurationException("Please specify PIPELINE_NAME_SAP!");

        var subscriptionId = Environment.GetEnvironmentVariable("SYNAPSE_SUBSCRIPTIONID") ??
            throw new ConfigurationException("Please specify a SYNAPSE_SUBSCRIPTIONID!");

        var synapseResourceGroupName = Environment.GetEnvironmentVariable("SYNAPSE_RESOURCEGROUPNAME") ??
             throw new ConfigurationException("Please specify SYNAPSE_RESOURCEGROUPNAME!");

        //  
        var storageAccountName = Environment.GetEnvironmentVariable("STORAGE_FINANCIAL_ACCOUNTNAME") ??
            throw new ConfigurationException("Please specify STORAGE_FINANCIAL_ACCOUNTNAME!");

        var storageAccountKey = Environment.GetEnvironmentVariable("STORAGE_FINANCIAL_ACCOUNTKEY") ??
            throw new ConfigurationException("Please specify a SYNAPSE_SUBSCRIPTIONID!");

        var blobContainerName = Environment.GetEnvironmentVariable("STORAGE_FINANCIAL_CONTAINERNAME") ??
             throw new ConfigurationException("Please specify SYNAPSE_RESOURCEGROUPNAME!"); 

        var selfCareOnBoardingEndpoint = Environment.GetEnvironmentVariable("SELFCAREONBOARDING_ENDPOINT") ??
             throw new ConfigurationException("Please specify SELFCAREONBOARDING_ENDPOINT!");

        var selfCareOnBoardingRecipientCodeUri = Environment.GetEnvironmentVariable("SELFCAREONBOARDING_URI") ??
             throw new ConfigurationException("Please specify SELFCAREONBOARDING_URI!");

        var selfCareOnBoardingAuthToken  = Environment.GetEnvironmentVariable("SELFCAREONBOARDING_AUTHTOKEN") ??
             throw new ConfigurationException("Please specify SELFCAREONBOARDING_AUTHTOKEN!");

        var supportAPIServiceEndpoint = Environment.GetEnvironmentVariable("SUPPORTAPISERVICE_ENDPOINT") ??
             throw new ConfigurationException("Please specify SUPPORTAPISERVICE_ENDPOINT!");

        var supportAPIServiceRecipientCodeUri = Environment.GetEnvironmentVariable("SUPPORTAPISERVICE_URI") ??
             throw new ConfigurationException("Please specify SUPPORTAPISERVICE_URI!");

        var supportAPIServiceAuthToken = Environment.GetEnvironmentVariable("SUPPORTAPISERVICE_AUTHTOKEN") ??
             throw new ConfigurationException("Please specify SUPPORTAPISERVICE_AUTHTOKEN!");

        model.ConnectionString = connectionString; //await model.ConnectionString.Mapper();

        model.JWT.ValidAudience = validAudience;
        model.JWT.Secret = secret; //await model.JWT.Secret.Mapper();
        model.JWT.ValidIssuer = validIssuer;

        model.CORSOrigins = corsOrigins;

        model.SelfCareUri = selfCareUri;
        model.SelfCareCertEndpoint = selfCareCertEndpoint;
        model.SelfCareTimeOut = selfCareTimeOut;
        model.SelfCareAudience = selfCareAudience;

        model.AdminKey = adminKey;
        model.AzureAd.ClientId = azureADClientId;
        model.AzureAd.TenantId = azureADTenantId;
        model.AzureAd.Instance = azureADInstance;
        model.AzureAd.AdGroup = azureADGroup;

        model.Storage.RelFolder = relFolder;
        model.Storage.ConnectionString = storageConnectionString;
        model.StorageDocumenti!.ConnectionString = storageDocumentiConnectionString;
        model.StorageDocumenti!.DocumentiFolder = documentFolder;

        model.ApplicationInsights = applicationInsights;

        model.Synapse.SynapseWorkspaceName = synapseWorkspaceName;
        model.Synapse.PipelineNameSAP = pipelineNameSAP;
        model.Synapse.ResourceGroupName = synapseResourceGroupName;
        model.Synapse.SubscriptionId = subscriptionId;

        model.StoragePagoPAFinancial.BlobContainerName = blobContainerName;
        model.StoragePagoPAFinancial.AccountName = storageAccountName;
        model.StoragePagoPAFinancial.AccountKey = storageAccountKey;

        model.SelfCareOnBoarding.Endpoint = selfCareOnBoardingEndpoint;
        model.SelfCareOnBoarding.RecipientCodeUri = selfCareOnBoardingRecipientCodeUri;
        model.SelfCareOnBoarding.AuthToken = selfCareOnBoardingAuthToken;

        model.SupportAPIService.Endpoint = supportAPIServiceEndpoint;
        model.SupportAPIService.RecipientCodeUri = supportAPIServiceRecipientCodeUri;
        model.SupportAPIService.AuthToken = supportAPIServiceAuthToken;

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