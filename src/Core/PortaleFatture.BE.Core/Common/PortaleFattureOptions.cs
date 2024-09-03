using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Core.Common;

public sealed class PortaleFattureOptions : IPortaleFattureOptions
{
    public string? ConnectionString { get; set; }
    public string? SelfCareCertEndpoint { get; set; }
    public string? SelfCareUri { get; set; } 
    public string? SelfCareTimeOut { get; set; }
    public string? FattureSchema { get; set; }
    public string? SelfCareSchema { get; set; }
    public string? Vault { get; set; }
    public JwtConfiguration? JWT { get; set; } 
    public string? CORSOrigins { get; set; }  
    public string? AdminKey { get; set; }
    public string? SelfCareAudience { get; set; } 
    public string? ApplicationInsights { get; set; } 
    public AzureAd? AzureAd { get; set; } 
    public Storage? Storage { get; set; } 
    public StorageDocumenti? StorageDocumenti { get; set; } 
    public Synapse? Synapse { get; set; }
}

public class Synapse()
{
    public string? SynapseWorkspaceName{ get; set; }
    public string? PipelineNameSAP { get; set; } 
    public string? SubscriptionId { get; set; }
    public string? ResourceGroupName { get; set; }
}

public class Storage()
{ 
    public string? RelFolder { get; set; }
    public string? ConnectionString { get; set; } 
}

public class StorageDocumenti()
{
    public string? DocumentiFolder { get; set; } 
    public string? ConnectionString { get; set; }
}


public class AzureAd()
{
    public string? Instance { get; set; }
    public string? TenantId { get; set; }
    public string? ClientId { get; set; } 
    public string? AdGroup { get; set; }
} 
