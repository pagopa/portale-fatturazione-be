﻿using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Auth.SelfCare;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Gateway;

public class SelfCareTokenService : ISelfCareTokenService
{
    private readonly ISelfCareHttpClient _httpClient;
    private readonly ILogger<SelfCareTokenService> _logger;
    private readonly IPortaleFattureOptions _options;
    public SelfCareTokenService(
        ISelfCareHttpClient httpClient,
        IPortaleFattureOptions options,
        ILogger<SelfCareTokenService> logger)
    {
        this._httpClient = httpClient;
        this._logger = logger;
        this._options = options;
    }

    public async Task<(ClaimsPrincipal?, bool)> Validate(string selfcareToken, bool requireExpirationTime = false, CancellationToken ct = default)
    {
        var securityToken = _httpClient.GetSelfCareTokenAsync(selfcareToken);
        var kid = securityToken!.Header.Kid;
        var certificate = await _httpClient.GetCertificateByKidAsync(kid, ct);
        return Verify(certificate, selfcareToken, requireExpirationTime);
    }

    public async Task<SelfCareDto?> ValidateContent(string selfcareToken, bool requireExpirationTime = false, CancellationToken ct = default)
    {
        (var tk, var verify) = await Validate(selfcareToken, requireExpirationTime, ct);
        if (verify)
            return Mapper(tk);
        return null;
    }

    private SelfCareDto Mapper(ClaimsPrincipal? tk)
    {
        var claims = tk!.Claims;
        return new SelfCareDto()
        {
            Email = claims.Where(x => x.Type == ClaimTypes.Email).FirstOrDefault() == null ? null : claims.Where(x => x.Type == ClaimTypes.Email).FirstOrDefault()!.Value,
            Uid = claims.Where(x => x.Type == CustomClaim.Uid).FirstOrDefault()!.Value,
            Organization = claims.Where(x => x.Type == CustomClaim.Organization).FirstOrDefault()!.Value.Deserialize<SelfCareOrganizationDto>(),
        };
    }

    private string ReadJwt(string selfcareToken)
    {
        var data = string.Empty;
        try
        { 
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(selfcareToken))
            {
                var token = handler.ReadJwtToken(selfcareToken);

                foreach (var claim in token.Claims)
                {
                    if(claim.Type  == CustomClaim.Uid)
                        data += "uid: " + claim.Value + " ";

                    if (claim.Type == CustomClaim.Expire)
                    {
                        var expirationTimeUtc = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(claim.Value)).UtcDateTime;
                        var italianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                        var expirationTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(expirationTimeUtc, italianTimeZone); 
                        var italianCulture = new CultureInfo("it-IT");
                        string formattedDate = expirationTimeLocal.ToString("f", italianCulture);
                        data += "exp: " + formattedDate + " ";
                    }

                    if (claim.Type == CustomClaim.Organization)
                    {
                        dynamic? jsonObject = JsonConvert.DeserializeObject<dynamic>(claim.Value!);
                        data += $" idEnte: {jsonObject!.id}" + $" ente: {jsonObject!.name} ";
                    }
                     
                }
                return data;
            }
            else
                return string.Empty;
        }
        catch
        {
            return string.Empty;
        } 
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
            ValidAudience = _options.SelfCareAudience,
            ValidateAudience = true,
            ValidIssuer = _options.SelfCareUri,
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
            var msg = $"Token Exchange Expired! {ReadJwt(selfcareToken)}";
            _logger.LogError(msg);
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