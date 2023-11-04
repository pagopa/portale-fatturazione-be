namespace PortaleFatture.BE.Core.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception ex) : base(message, ex)
    {
    }
}