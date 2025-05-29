using System.Net;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Extensions;

public static class ApiKeysExtensions
{
    public static bool VerifyIp(this string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        if (ipAddress.Contains('/'))
        {
            try
            {
                var network = IPNetwork.Parse(ipAddress); // CIDR format
                return true;
            }
            catch
            {
                return false;
            }
        }
        else
        {
            return IPAddress.TryParse(ipAddress, out _); // Regular IP
        }
    }
} 