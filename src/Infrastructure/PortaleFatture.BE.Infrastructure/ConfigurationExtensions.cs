using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;

namespace PortaleFatture.BE.Infrastructure;

public static class ConfigurationExtensions
{
    public static JwtBearerOptions JwtAuthenticationConfiguration(this JwtBearerOptions options, JwtConfiguration jwtAuth)
    {
        if (jwtAuth == null)
        {
            throw new ConfigurationException("Jwt configuration error");
        }

        if (jwtAuth.ValidIssuer == null || jwtAuth.ValidAudience == null || jwtAuth.Secret == null)
        {
            throw new ConfigurationException("Jwt configuration error");
        }

        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtAuth.ValidIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAuth.ValidAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAuth.Secret)),
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("IS-TOKEN-EXPIRED", "true");
                }

                return Task.CompletedTask;
            }
        };

        return options;
    }
}