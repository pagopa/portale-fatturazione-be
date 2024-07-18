using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.Identity.Extensions;

public static class IdentityExtensions
{
    public static string Map(this string product)
    {
        if (product == Product.ProdSendino)
            return Product.ProdPN;
        else return product;
    }
} 