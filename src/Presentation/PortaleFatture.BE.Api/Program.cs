using PortaleFatture.BE.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddModules(builder.Configuration);

var configuredBuilder = await builder
    .Build()
    .UseModulesAsync();

configuredBuilder.Run();

public partial class Program
{
}