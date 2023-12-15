namespace PortaleFatture.BE.Core.Exceptions;

public class SessionException : Exception
{
    public SessionException()
    {
    }

    public SessionException(string message) : base(message)
    {
    }

    public SessionException(string message, Exception ex) : base(message, ex)
    {
    }
} 