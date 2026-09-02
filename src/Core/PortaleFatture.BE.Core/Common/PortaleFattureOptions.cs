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
    public StoragePagoPAFinancial? StoragePagoPAFinancial { get; set; } 
    public SelfCareOnBoarding? SelfCareOnBoarding { get; set; } 
    public SupportAPIService? SupportAPIService { get; set; } 
    public StorageREL? StorageREL { get; set; } 
    public StorageContestazioni? StorageContestazioni { get; set; }
    public StorageNotifiche? StorageNotifiche { get; set; } 
    public AzureFunction? AzureFunction { get; set; } 
    public StorageRelDownload? StorageRelDownload { get; set; }  
    public Language? Language { get; set; }
}

public class StorageRelDownload()
{
    public string? AccountName { get; set; }
    public string? AccountKey { get; set; }
    public string? BlobContainerName { get; set; }
    public string? CustomDNS { get; set; }
}

public class StorageREL()
{
    public string? StorageRELAccountName { get; set; }
    public string? StorageRELAccountKey { get; set; }
    public string? StorageRELBlobContainerName { get; set; }
    public string? StorageRELCustomDns { get; set; }
}

public class StorageNotifiche()
{
    public string? AccountName { get; set; }
    public string? AccountKey { get; set; }
    public string? BlobContainerName { get; set; } 
    public string? CustomDNS { get; set; }
}

public class SupportAPIService()
{
    public string? Endpoint { get; set; }
    public string? RecipientCodeUri { get; set; }
    public string? AuthToken { get; set; }
}

public class SelfCareOnBoarding()
{
    public string? Endpoint { get; set; }
    public string? RecipientCodeUri{ get; set; } 
    public string? AuthToken { get; set; } 
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

public class StoragePagoPAFinancial()
{
    public string? AccountName { get; set; }
    public string? AccountKey { get; set; } 
    public string? BlobContainerName { get; set; }
}

public class StorageContestazioni()
{
    public string? AccountName { get; set; }
    public string? AccountKey { get; set; }
    public string? BlobContainerName { get; set; } 
    public string? CustomDns { get; set; }
}

public class AzureAd()
{
    public string? Instance { get; set; }
    public string? TenantId { get; set; }
    public string? ClientId { get; set; } 
    public string? AdGroup { get; set; }
}

public class AzureFunction()
{
    public string? NotificheUri { get; set; }
    public string? AppKey { get; set; } 
}

public class Language()
{
    public string? Endpoint { get; set; }
    public string? Key { get; set; }

    /// <summary>
    /// Tempo massimo concesso alle operazioni long-running di Azure AI Language (oggi solo la sintesi
    /// del testo). Default **45 secondi**, scelto per stare **sotto** il taglio del gateway che sta
    /// davanti a questo backend (~60s, v. docs/autenticazione.md): serve a produrre un 504 gestito e
    /// tracciato invece di una connessione interrotta dal gateway, che non lascerebbe traccia nei
    /// nostri log. Se un domani la soglia del gateway cambiasse, questo valore va rivisto di conseguenza.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 45;

    /// <summary>
    /// Lunghezza massima del testo per le operazioni **sincrone** (rilevazione PII e della lingua).
    /// Default **5.120 caratteri**, il limite per documento delle API sincrone di Azure AI Language.
    /// </summary>
    public int MaxChars { get; set; } = 5_120;

    /// <summary>
    /// Lunghezza massima per la **sintesi**, che passa dall'API asincrona e ha un limite molto più
    /// alto. Default **125.000 caratteri**.
    ///
    /// ATTENZIONE Entrambi i valori sono limiti **del servizio Azure**, non nostri: vanno riverificati sulla
    /// documentazione Microsoft quando si aggiorna l'SDK o si cambia tier del servizio. Sono qui in
    /// configurazione proprio per poterli correggere senza ricompilare.
    /// </summary>
    public int MaxCharsSummarize { get; set; } = 125_000;
}
