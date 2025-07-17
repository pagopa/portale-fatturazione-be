
namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Services;

public interface IFunctionNotificheCaller
{
    Task<FunctionResponse?> CallAzureFunction(dynamic request);
    Task<(DurableFunctionResponse?, string?)> CallDurableFunctionWebhook(string functionUrl);
}