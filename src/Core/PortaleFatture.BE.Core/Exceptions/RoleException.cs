namespace PortaleFatture.BE.Core.Exceptions;

public class RoleException : Exception
{
    public RoleException(string message) : base(message)
    {
    }

    public RoleException(string message, Exception ex) : base(message, ex)
    {
    }
} 