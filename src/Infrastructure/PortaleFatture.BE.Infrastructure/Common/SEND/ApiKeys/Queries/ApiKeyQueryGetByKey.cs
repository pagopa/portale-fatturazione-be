using MediatR;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries;

 
public class ApiKeyQueryGetByKey() : IRequest<ApiKeyDto?>
{
    public string? ApiKey { get; set; }
}