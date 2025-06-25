namespace PortaleFatture.BE.Core.Exceptions;

public class UploadException : Exception
{
    public UploadException()
    {
    }

    public UploadException(string message) : base(message)
    {
    }

    public UploadException(string message, Exception ex) : base(message, ex)
    {
    }
}