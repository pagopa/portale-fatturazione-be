using MediatR;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries;

 
public class ApiKeyQueryGetByKeyEnte() : IRequest<ApiKeyEnteDto?>
{
    public string? ApiKey { get; set; }
}