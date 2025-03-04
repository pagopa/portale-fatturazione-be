using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Core.Common; 
public interface IPortaleFattureOptions
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
    public AzureAd? AzureAd { get; set; } 
    public Storage? Storage { get; set; } 
    public StorageDocumenti? StorageDocumenti { get; set; } 
    public Synapse? Synapse { get; set; } 
    public SelfCareOnBoarding? SelfCareOnBoarding { get; set; }
    public StoragePagoPAFinancial? StoragePagoPAFinancial { get; set; } 
    public SupportAPIService? SupportAPIService { get; set; } 
    public StorageREL? StorageREL { get; set; }
}