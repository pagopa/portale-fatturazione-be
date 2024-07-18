using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace PortaleFatture.BE.Infrastructure.Gateway;
public class AesEncryption(string key) : IAesEncryption
{
    private readonly string _key = key;

    public string EncryptString(string plainText)
    {
        var iv = new byte[16];
        byte[] array;

        using (var aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.IV = iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write);
            using (var streamWriter = new StreamWriter((Stream)cryptoStream))
            {
                streamWriter.Write(plainText);
            }

            array = memoryStream.ToArray();
        }

        return ToString(array);
    }

    public string DecryptString(string cipherText)
    {
        var iv = new byte[16];
        var buffer = FromString(cipherText);

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_key);
        aes.IV = iv;
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using var memoryStream = new MemoryStream(buffer);
        using var cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read);
        using var streamReader = new StreamReader((Stream)cryptoStream);
        return streamReader.ReadToEnd();
    }
    private string ToString(byte[] input)
    {
        return WebEncoders.Base64UrlEncode(input);
    }

    public byte[] FromString(string input)
    {
        return WebEncoders.Base64UrlDecode(input);
    }
}