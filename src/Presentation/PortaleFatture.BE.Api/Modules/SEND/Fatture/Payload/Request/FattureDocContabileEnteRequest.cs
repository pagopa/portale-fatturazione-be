using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request
{
    public class FattureDocContabileEnteRequest()
    {
        public long? IdFattura { get; set; }
    }
}
