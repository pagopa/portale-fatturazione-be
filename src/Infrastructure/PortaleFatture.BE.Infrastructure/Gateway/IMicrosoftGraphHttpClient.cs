
namespace PortaleFatture.BE.Infrastructure.Gateway
{
    public interface IMicrosoftGraphHttpClient
    {
        Task<Dictionary<string, string?>> GetGroupsAsync(string azureADAccessToken, CancellationToken ct = default);
    }
}