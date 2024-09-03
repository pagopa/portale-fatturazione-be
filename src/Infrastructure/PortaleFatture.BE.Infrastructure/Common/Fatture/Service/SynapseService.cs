using System.Text;
using Azure.Identity;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Service;

public class SynapseService : ISynapseService
{
    private readonly string? _synapseWorkspaceUrl;
    private readonly string? _workspaceName;
    private readonly string? _resourceGroupName;
    private readonly string? _subscriptionId;
    public SynapseService(string? workspaceName, string? resourceGroupName, string? subscriptionId)
    {
        this._synapseWorkspaceUrl = $"https://{workspaceName}.dev.azuresynapse.net";
        this._workspaceName = workspaceName;
        this._resourceGroupName = resourceGroupName;
        this._subscriptionId = subscriptionId;
    }

    public string? GetSynapseWorkspaceUrl()
    {
        return this._synapseWorkspaceUrl;
    } 

    public async Task<bool> InviaASapFatture(string? pipelineName, FatturaInvioSap parameters)
    {
        string apiVersion = "2021-06-01";

        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { "https://dev.azuresynapse.net/.default" }));
        var accessToken = token.Token;

        var httpClient = new HttpClient();
        var requestUri = $"https://{_workspaceName}.dev.azuresynapse.net/pipelines/{pipelineName}/createRun?api-version={apiVersion}";

        var pipelineParameters = new PipelineParameters()
        {
            AnnoRiferimento = parameters.AnnoRiferimento,
            MeseRiferimento = parameters.MeseRiferimento,
            TipologiaFattura = parameters.TipologiaFattura
        };

        var requestBody = new RequestPipeline()
        {
            Parameters = pipelineParameters
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken) },
            Content = new StringContent(requestBody.Serialize(), Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(requestMessage);

        return response.IsSuccessStatusCode;
    }
}
