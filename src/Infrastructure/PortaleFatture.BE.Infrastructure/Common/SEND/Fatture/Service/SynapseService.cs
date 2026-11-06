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

    /// <summary>
    /// Timeout di <c>createRun</c> per la generazione JSON: la chiamata avviene a transazione aperta, quindi
    /// deve restare ben sotto il timeout del gateway (~60 s).
    /// </summary>
    public static readonly TimeSpan TimeoutAvvioGenerazione = TimeSpan.FromSeconds(20);

    public async Task<SynapseRunResult> AvviaGenerazioneJsonSap(string? pipelineName, InvioSapPipelineRequest richiesta, CancellationToken ct = default)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeoutAvvioGenerazione);

            var credential = new DefaultAzureCredential();
            var token = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(["https://dev.azuresynapse.net/.default"]), timeout.Token);
            var requestUri = $"https://{_workspaceName}.dev.azuresynapse.net/pipelines/{pipelineName}/createRun?api-version=2021-06-01";

            using var httpClient = new HttpClient { Timeout = TimeoutAvvioGenerazione };
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");
            using var response = await httpClient.PostAsJsonAsync(requestUri, richiesta, timeout.Token);

            string? runId = null;
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var corpo = await response.Content.ReadFromJsonAsync<Dictionary<string, object?>>(cancellationToken: timeout.Token);
                    runId = corpo is not null && corpo.TryGetValue("runId", out var valore) ? valore?.ToString() : null;
                }
                catch (Exception) { /* il runId serve solo ai log */ }
            }
            else
            {
                // Mai il body in chiaro: contiene solo id, ma la regola dei log e' una (be-invio-sap.md §12).
                _logger.LogError("AvviaGenerazioneJsonSap|batch {BatchId}|status {Status}", richiesta.BatchId, (int)response.StatusCode);
            }
            return new SynapseRunResult(response.IsSuccessStatusCode, (int)response.StatusCode, runId);
        }
        catch (Exception ex)
        {
            _logger.LogError("AvviaGenerazioneJsonSap|batch {BatchId}|{Errore}", richiesta.BatchId, ex.GetType().Name);
            return new SynapseRunResult(false, null, null);
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