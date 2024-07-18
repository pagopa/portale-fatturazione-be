namespace PortaleFatture.BE.Api.Infrastructure;

public interface IRegistrableModule : IModule
{
    void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder);
}