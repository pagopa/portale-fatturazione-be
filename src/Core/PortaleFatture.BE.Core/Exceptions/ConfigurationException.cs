namespace PortaleFatture.BE.Core.Exceptions;

public class ConfigurationException : Exception
{
    public ConfigurationException()
    {
    }

    public ConfigurationException(string message) : base(message)
    {
    }

    public ConfigurationException(string message, Exception ex) : base(message, ex)
    {
    }
}