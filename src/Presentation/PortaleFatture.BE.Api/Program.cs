using PortaleFatture.BE.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddModules();

var configuredBuilder = builder
    .Build()
    .UseModules();

// comment re-run

configuredBuilder.Run();

public partial class Program
{
}