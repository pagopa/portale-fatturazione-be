using System.Net.Mail;

namespace PortaleFatture.BE.Core.Extensions;

public static class ValidationExtensions
{
    public static bool IsNullNotAny<T>(this IEnumerable<T> enumerable)
    {
        return enumerable == null || !enumerable.Any();
    }

    //public static bool IsNull<T>(this T? obj) where T : class
    //{
    //    return obj == null;
    //}

    public static bool IsNull(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    public static bool IsNotValidEmail(this string value)
    {
        if (value.IsNull()) return true;

        try
        {
            var email = new MailAddress(value);
            return email.Address != value.Trim();
        }
        catch
        {
            return true;
        }
    }

    public static bool IsNotValidGuid(this string value)
    {
        return !Guid.TryParse(value, out _);
    }
}
