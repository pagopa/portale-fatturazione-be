using PortaleFatture.BE.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddModules(builder.Configuration);

var configuredBuilder = builder
    .Build()
    .UseModules();

configuredBuilder.Run();

public partial class Program
{
}