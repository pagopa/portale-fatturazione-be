﻿using PortaleFatture.BE.Api.Modules.Auth.Extensions;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Extensions;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.Api.Infrastructure;

public class NonceMultiTabsMiddleware(RequestDelegate next, IAesEncryption encryption)
{
    private readonly RequestDelegate _next = next;
    private readonly IAesEncryption? _encryption = encryption;
    private static readonly Dictionary<string, string> _whitelist = new()
        {
            //{ "/swagger/v1/swagger.json", "/swagger/v1/swagger.json"},
            //{ "/swagger/index.html", "/swagger/index.html"},
            { "/favicon.ico", "/favicon.ico" },
            { "/", "/" },
            { "/health", "/health" },
            { "/api/auth/profilo", "/api/auth/profilo" },
            { "/api/auth/selfcare/login", "/api/auth/selfcare/login" }
        };
    public async Task InvokeAsync(HttpContext context)
    {
        var url = context.Request.Path;
        var isInWhitelist = _whitelist.TryGetValue(url, out var value);
        if (!isInWhitelist)
        {
            var authInfo = context.GetAuthInfo();
            var nonce = context.Request.Query["nonce"];
            if (string.IsNullOrWhiteSpace(nonce))
                throw new SessionException("Session expired!");
            var decryptedNonce = _encryption!.DecryptString(nonce!);
            var verify = authInfo.Verify(decryptedNonce!);
            if (!verify)
                throw new SessionException("Session expired!");
        }
        await _next(context);
    }
}