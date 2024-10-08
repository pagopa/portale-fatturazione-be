using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;
using static System.Net.Mime.MediaTypeNames;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Service;

public class SynapseService : ISynapseService
{
    private readonly string? _synapseWorkspaceUrl;
    private readonly string? _workspaceName;
    private readonly string? _resourceGroupName;
    private readonly string? _subscriptionId;
    private readonly ILogger<SynapseService> _logger;
    public SynapseService(string? workspaceName, string? resourceGroupName, string? subscriptionId, ILogger<SynapseService> logger)
    {
        this._synapseWorkspaceUrl = $"https://{workspaceName}.dev.azuresynapse.net";
        this._workspaceName = workspaceName;
        this._resourceGroupName = resourceGroupName;
        this._subscriptionId = subscriptionId;
        this._logger = logger;
    }

    public string? GetSynapseWorkspaceUrl()
    {
        return this._synapseWorkspaceUrl;
    }

    public async Task<bool> InviaASapFatture(string? pipelineName, FatturaInvioSap parameters)
    {

        var apiVersion = "2021-06-01";

        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { "https://dev.azuresynapse.net/.default" }));
        var accessToken = token.Token; 
 
        var requestUri = $"https://{_workspaceName}.dev.azuresynapse.net/pipelines/{pipelineName}/createRun?api-version={apiVersion}";

        var pipelineParameters = new PipelineParameters()
        {
            AnnoRiferimento = parameters.AnnoRiferimento,
            MeseRiferimento = parameters.MeseRiferimento,
            TipologiaFattura = parameters.TipologiaFattura
        }; 

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            using var response = await httpClient.PostAsJsonAsync(requestUri, pipelineParameters);
            var status = response.IsSuccessStatusCode;
            if (!status)
                _logger.LogError(503, "InviaASapFatture" + "|" + response.Serialize() + "|" + pipelineParameters.Serialize() + "|" + status); 

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(503, "InviaASapFatture" + "|" + ex.Serialize() + "|" + pipelineParameters.Serialize());
            return false;
        } 
    }
} 