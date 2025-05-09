using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Models;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands;

namespace PortaleFatture.BE.Function.API.Extensions;
public static class ApiExtensions
{
    public static string GetUri(this IConfigurazione configuration, string? uri)
    {
        var customDomain = configuration.CustomDomain!.Replace("/api", string.Empty);
        return Regex.Replace(uri!, @"^https?://[^/]+", customDomain);
    }

    public static string GetUri(this FunctionContext context, string uri)
    {
        var configuration = context.InstanceServices.GetRequiredService<IConfigurazione>(); 
        var customDomain = configuration.CustomDomain!.Replace("/api", string.Empty); 
        return Regex.Replace(uri!, @"^https?://[^/]+", customDomain); 
    }

    public static bool SkipSwagger(this HttpRequestData? httpRequestData)
    {
        var uri = httpRequestData!.Url?.ToString();
        return uri!.Contains("/api/swagger", StringComparison.OrdinalIgnoreCase) || uri.Contains("/api/swagger.json", StringComparison.OrdinalIgnoreCase);
    }

    public static string? GetEnvironmentVariable(this string name)
    {
        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }


    public static string? GetApiKey(this HttpRequestData? httpRequestData)
    {
        return httpRequestData!.Headers
            .FirstOrDefault(h => h.Key.Equals("x-api-key", StringComparison.OrdinalIgnoreCase))
            .Value?.FirstOrDefault();
    }

    public static string? ExtractIpAddress(this HttpRequestData? httpRequestData)
    {
        var ipAddress = httpRequestData!.Headers
            .FirstOrDefault(h => h.Key.Equals("custom-forwarded-for", StringComparison.OrdinalIgnoreCase)).Value
            ?.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()
            ?? httpRequestData.Headers
            .FirstOrDefault(h => h.Key.Equals("x-forwarded-for", StringComparison.OrdinalIgnoreCase)).Value
            ?.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()
            ?? httpRequestData.Headers
            .FirstOrDefault(h => h.Key.Equals("x-original-for", StringComparison.OrdinalIgnoreCase)).Value
            ?.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()
            ?? null;

        if (ipAddress?.Contains(':') == true)
        {
            if (ipAddress.StartsWith("[") && ipAddress.Contains("]:"))
                ipAddress = ipAddress.Substring(1, ipAddress.IndexOf("]:") - 1);
            else
                ipAddress = ipAddress.Split(':')[0];
        }

#if DEBUG
        if (ipAddress == "::1" || ipAddress == "127.0.0.1" || string.IsNullOrEmpty(ipAddress))
            ipAddress = "127.0.0.1";
#endif
        return ipAddress;
    }

    public static void SetItem<T>(this FunctionContext context, string key, T value)
    {
        context.Items[key] = value!;
    }

    public static T? GetItem<T>(this FunctionContext context, string key)
    {
        if (context.Items.TryGetValue(key, out var value) && value is T typed)
        {
            return typed;
        }
        return default;
    }

    public static CreateApyLogCommand  Request(this FunctionContext context)
    {
        var idEnte = context.GetItem<string>("IdEnte");
        var functionName = context.GetItem<string>("FunctionName");
        var ipAddress = context.GetItem<string>("IpAddress");
        var uri = context.GetItem<string>("Uri");
        var stage = context.GetItem<string>("Stage");
        var method = context.GetItem<string>("Method");
        var payload = context.GetItem<string>("Payload");
        var id = context.GetItem<string>("RequestId");

        var authenticationInfo = new AuthenticationInfo()
        {
            IdEnte = idEnte
        };
        return new CreateApyLogCommand(authenticationInfo)
        {
            Id = id,
            FunctionName = functionName,
            IpAddress = ipAddress,
            Payload = payload,
            Stage = stage,
            Uri = uri,
            Method = method,
            Timestamp = DateTime.UtcNow.ItalianTime()
        };
    }

    public static CreateApyLogCommand  Response(this FunctionContext context)
    {
        var idEnte = context.GetItem<string>("IdEnte");
        var functionName = context.GetItem<string>("FunctionName");
        var ipAddress = context.GetItem<string>("IpAddress");
        var uri = context.GetItem<string>("Uri");
        var stage = ScopeType.RESPONSE;
        var method = context.GetItem<string>("Method"); 
        var id = context.GetItem<string>("RequestId");

        var authenticationInfo = new AuthenticationInfo()
        {
            IdEnte = idEnte
        };
        var log = new CreateApyLogCommand(authenticationInfo)
        {
            Id = id,
            FunctionName = functionName,
            IpAddress = ipAddress, 
            Stage = stage,
            Uri = uri,
            Method = method,
            Timestamp = DateTime.UtcNow.ItalianTime()
        }; 

        log.Payload = log.Serialize(); 
        return log;
    }
    public static CreateApyLogCommand Response(this FunctionContext context, Session session)
    {
        var idEnte = session.FkIdEnte;
        var functionName = session.FunctionName;
        var ipAddress = session.IpAddress;
        var uri = session.Uri;
        var stage = ScopeType.RESPONSE;
        var method = session.Method;
        var id = session.Id;
        var payload = session.Payload;

        var authenticationInfo = new AuthenticationInfo()
        {
            IdEnte = idEnte
        };

        var log = new CreateApyLogCommand(authenticationInfo)
        {
            Id = id,
            FunctionName = functionName,
            IpAddress = ipAddress,
            Stage = stage,
            Uri = uri,
            Method = method,
            Timestamp = DateTime.UtcNow.ItalianTime(),
            Payload = payload   
        };

        log.Payload = log.Serialize();
        return log;
    }

    public static Session GetSession(this FunctionContext context)
    {
        var idEnte = context.GetItem<string>("IdEnte");
        var functionName = context.GetItem<string>("FunctionName");
        var ipAddress = context.GetItem<string>("IpAddress");
        var uri = context.GetItem<string>("Uri");
        var stage = context.GetItem<string>("Stage");
        var method = context.GetItem<string>("Method");
        var payload = context.GetItem<string>("Payload");
        var id = context.GetItem<string>("RequestId");

   
        return new Session()
        {
            FkIdEnte = idEnte,
            Id = id,
            FunctionName = functionName,
            IpAddress = ipAddress,
            Payload = payload,
            Stage = stage,
            Uri = uri,
            Method = method,
            Timestamp = DateTime.UtcNow.ItalianTime()
        };
    }
}