using PortaleFatture.BE.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddModules();

var configuredBuilder = builder
    .Build()
    .UseModules();
 
configuredBuilder.Run();

public partial class Program
{
}