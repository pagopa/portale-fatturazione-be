﻿using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Auth.PagoPA;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Exceptions;

namespace PortaleFatture.BE.Infrastructure.Gateway;

public class PagoPATokenService(
    IPagoPAHttpClient httpClient,
    IMicrosoftGraphHttpClient httpMicrosoftGraphClient,
    IPortaleFattureOptions options,
    ILogger<PagoPATokenService> logger) : IPagoPATokenService
{
    private readonly IPagoPAHttpClient _httpClient = httpClient;
    private readonly ILogger<PagoPATokenService> _logger = logger;
    private readonly IPortaleFattureOptions _options = options;
    private readonly IMicrosoftGraphHttpClient _httpMicrosoftGraphClient = httpMicrosoftGraphClient;
    public async Task<(ClaimsPrincipal?, bool)> Validate(string selfcareToken, bool requireExpirationTime = false, CancellationToken ct = default)
    {
        var securityToken = _httpClient.GetSelfCareTokenAsync(selfcareToken);
        var kid = securityToken!.Header.Kid;
        var certificate = await _httpClient.GetCertificateByKidAsync(kid, ct);
        return Verify(certificate, selfcareToken, requireExpirationTime);
    }

    public async Task<PagoPADto?> ValidateContent(string selfcareToken, string  azureADAccessToken, bool requireExpirationTime = false, CancellationToken ct = default)
    {
        (var tk, var verify) = await Validate(selfcareToken, requireExpirationTime, ct);
        if (verify)
        {
            // call graph 4 groups
            var groups = await _httpMicrosoftGraphClient.GetGroupsAsync(azureADAccessToken, ct);
            return Mapper(tk, groups);
        } 
        throw new SecurityException();
    }

    private PagoPADto Mapper(ClaimsPrincipal? tk, Dictionary<string, string?> groups)
    {
        var claims = tk!.Claims;
        var roles = claims.Where(x => x.Type == ClaimTypes.Role).ToList();
        if (roles == null || roles.Count == 0)
            throw new RoleException("There are no roles in the claims.");

        var idGroups = roles.Select(x => x.Value);
      
        return new PagoPADto()
        {
            Email = claims.Where(x => x.Type == CustomClaim.PreferredUsername).FirstOrDefault()!.Value,
            Uid = claims.Where(x => x.Type == CustomClaim.Oid).FirstOrDefault()!.Value,
            Name = claims.Where(x => x.Type == CustomClaim.Name).FirstOrDefault()!.Value,
            Roles = roles.Select(x => x.Value),
            Groups = groups.Where(x=> idGroups!.Contains(x.Key)).Select(x=>x.Value)!,
        };
    }

    private (ClaimsPrincipal?, bool) Verify(CertificateKey? certificate, string selfcareToken, bool requireExpirationTime)
    {
        var rsa = new RSACryptoServiceProvider();
        rsa.ImportParameters(
          new RSAParameters()
          {
              Modulus = FromBase64Url(certificate!.N!),
              Exponent = FromBase64Url(certificate!.E!)
          });

        var validationParameters = new TokenValidationParameters
        {
            RequireExpirationTime = requireExpirationTime,
            RequireSignedTokens = true,
            ValidAudience = _options.AzureAd!.ClientId,
            ValidateAudience = true,
            ValidIssuer = $"https://login.microsoftonline.com/{_options.AzureAd.TenantId}/v2.0",
            ValidateIssuer = true,
            ValidateLifetime = requireExpirationTime,
            IssuerSigningKey = new RsaSecurityKey(rsa)
        };
        var handler = new JwtSecurityTokenHandler();

        ClaimsPrincipal claimPrincipal;
        try
        {
            claimPrincipal = handler.ValidateToken(selfcareToken, validationParameters, out var validatedSecurityToken);
            if (validatedSecurityToken == null)
                return (null, false);
        }
        catch
        {
            var msg = "Token Exchange Expired! JWT: { jwt }";
            _logger.LogError(msg, selfcareToken);
            throw new SecurityException(msg);
        }
        return (claimPrincipal, true);
    }

    private static byte[] FromBase64Url(string base64Url)
    {
        var padded = base64Url.Length % 4 == 0
            ? base64Url : base64Url + "====".Substring(base64Url.Length % 4);
        var base64 = padded.Replace("_", "/")
                              .Replace("-", "+");
        return Convert.FromBase64String(base64);
    }
}