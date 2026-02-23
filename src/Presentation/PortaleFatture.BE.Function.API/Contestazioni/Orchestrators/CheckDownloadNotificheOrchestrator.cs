using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

namespace PortaleFatture.BE.Function.API.Contestazioni.Orchestrators;
 
public class CheckDownloadNotificheOrchestrator
{
    [Function("CheckDownloadNotificheOrchestrator")]
    public async Task<CheckDownloadNotificheDto> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<CheckDownloadNotificheInternalRequest>();
        return await context.CallActivityAsync<CheckDownloadNotificheDto>("CheckDownloadNotifiche", data); 
    }
}