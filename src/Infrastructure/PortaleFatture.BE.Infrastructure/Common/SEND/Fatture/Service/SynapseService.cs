using System.Net.Http.Json;
using Azure.Identity;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Service;

public class SynapseService : ISynapseService
{
    private readonly string? _synapseWorkspaceUrl;
    private readonly string? _workspaceName;
    private readonly string? _resourceGroupName;
    private readonly string? _subscriptionId;
    private readonly ILogger<SynapseService> _logger;
    public SynapseService(string? workspaceName, string? resourceGroupName, string? subscriptionId, ILogger<SynapseService> logger)
    {
        _synapseWorkspaceUrl = $"https://{workspaceName}.dev.azuresynapse.net";
        _workspaceName = workspaceName;
        _resourceGroupName = resourceGroupName;
        _subscriptionId = subscriptionId;
        _logger = logger;
    }

    public string? GetSynapseWorkspaceUrl()
    {
        return _synapseWorkspaceUrl;
    }

    private PipelineParameter? ReturnParameter(string? tipologiaFattura, int anno, int mese)
    {
        var smese = mese.ToString().Length == 1 ? $"0{mese}" : mese.ToString();
        return new PipelineParameter()
        {
            TipoFattura = tipologiaFattura,
            MesiDaFatturare = [$"{anno}{smese}"]   //202401
        };
    }

    public async Task<bool> InviaASapFatture(string? pipelineName, FatturaInvioSap parameters)
    {

        var apiVersion = "2021-06-01";

        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { "https://dev.azuresynapse.net/.default" }));
        var accessToken = token.Token;

        var requestUri = $"https://{_workspaceName}.dev.azuresynapse.net/pipelines/{pipelineName}/createRun?api-version={apiVersion}";
 
        var pipelineParameters = new ParametriFatturazioneInvioSAP()
        {
             Parametri =
             [
                ReturnParameter(parameters.TipologiaFattura!, parameters.AnnoRiferimento, parameters.MeseRiferimento)
             ] 
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

    public async Task<bool> InviaASapFattureMultiplo(string? pipelineName, List<FatturaInvioSap> parameters)
    {
        var pars = parameters.Select(x => ReturnParameter(x.TipologiaFattura!, x.AnnoRiferimento, x.MeseRiferimento)).ToList();
        var pipelineParameters = new ParametriFatturazioneInvioSAP()
        {
            Parametri = pars!
        };

        var aggregatedParameters = pipelineParameters.Parametri
            .GroupBy(p => p.TipoFattura)
            .Select(g => new PipelineParameter
            {
                TipoFattura = g.Key,
                MesiDaFatturare = g.SelectMany(p => p.MesiDaFatturare ?? []).Distinct().ToList()
            })
            .ToList();

        pipelineParameters.Parametri = aggregatedParameters;

        var apiVersion = "2021-06-01";

        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { "https://dev.azuresynapse.net/.default" }));
        var accessToken = token.Token;

        var requestUri = $"https://{_workspaceName}.dev.azuresynapse.net/pipelines/{pipelineName}/createRun?api-version={apiVersion}";


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