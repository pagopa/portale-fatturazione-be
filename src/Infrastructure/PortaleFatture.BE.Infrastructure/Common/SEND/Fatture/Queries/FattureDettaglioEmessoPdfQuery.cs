using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

/// <summary>
/// Represents a query for retrieving detailed information about issued invoices in PDF format.
/// </summary>
/// <remarks>Use this class to construct and execute queries that return detailed PDF representations of issued
/// invoices. Inherits authentication and filtering capabilities from FattureDocContabileDettaglioEmessoQuery.</remarks>
public class FattureDettaglioEmessoPdfQuery : FattureDocContabileDettaglioEmessoQuery
{
    public FattureDettaglioEmessoPdfQuery(IAuthenticationInfo authenticationInfo)
        : base(authenticationInfo)
    {
    }
}