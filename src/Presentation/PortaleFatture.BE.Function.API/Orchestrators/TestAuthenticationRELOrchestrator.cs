using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.Api.Models;

namespace PortaleFatture.BE.Function.Api.Orchestrators;

//internal class TestAuthenticationRELOrchestrator
//{
//    [Function("TestAuthenticationRELOrchestrator")]
//    public async Task<RELDataResponse> RunAsync(
//        [OrchestrationTrigger] TaskOrchestrationContext context)
//    {
//        var data = context.GetInput<RELDataRequest>();
//        var rel = await context.CallActivityAsync<RELDataResponse>("TestAuthenticationREL", data);
//        return rel;
//    }
//}