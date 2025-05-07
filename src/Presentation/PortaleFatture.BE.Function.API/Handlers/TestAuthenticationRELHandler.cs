namespace PortaleFatture.BE.Function.Api.Handlers;

//internal class TestAuthenticationRELHandler
//{
//    [Function("TestAuthenticationRELHandler")]
//    public async Task<HttpResponseData?> TestAuthenticationRELOrchestration(
//    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "v1/rel/anno/mese")] HttpRequestData req,
//    [DurableClient] DurableTaskClient client,
//    FunctionContext context) 
//    {
//        var authMiddleware = new AuthMiddleware(req, context);
//        var authResponse = await authMiddleware.ValidateRequest();
//        if (authResponse != null)
//        {
//            return authResponse;
//        }

//        var data = new RELDataRequest();
//        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("TestAuthenticationRELOrchestrator", data);
//        var payload = new
//        {
//            message = "Orchestration started successfully.",
//            instanceId,
//            statusQueryGetUri = client.CreateHttpManagementPayload(instanceId).StatusQueryGetUri
//        };

//        var response = req.CreateResponse(HttpStatusCode.Accepted);
//        await response.WriteAsJsonAsync(payload);
//        return response;
//    }
//}