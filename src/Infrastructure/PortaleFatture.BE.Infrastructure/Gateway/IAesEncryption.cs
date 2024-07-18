namespace PortaleFatture.BE.Infrastructure.Gateway
{
    public interface IAesEncryption
    {
        string DecryptString(string cipherText);
        string EncryptString(string plainText);
    }
}