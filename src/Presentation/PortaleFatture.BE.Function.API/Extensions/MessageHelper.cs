namespace PortaleFatture.BE.Function.API.Extensions;

public static class MessageHelper
{
    public const string Unauthorized = "Unauthorized: Invalid or missing API Key.";
    public const string Forbidden = "Forbidden: Invalid or missing IP address."; 
    public const string BadRequestLogging = "Error logging data missing or malformed.";
    public const string NotFoundLogging = "Cannot proceed since logging failed.";
    public const string NotFound  = "There are no data.";
} 