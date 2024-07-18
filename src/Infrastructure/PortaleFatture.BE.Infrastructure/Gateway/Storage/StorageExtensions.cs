using System.IO.Compression;

namespace PortaleFatture.BE.Infrastructure.Gateway.Storage;
public static class StorageExtensions
{
    private static ZipArchiveEntry CreateEntry(ZipArchive zip, string entryName, CompressionLevel compressionLevel = CompressionLevel.Fastest)
    {
        return zip.CreateEntry(entryName, compressionLevel);
    }
    public static void AddFile(this ZipArchive zip, string fileName, byte[] bytes)
    {
        var entry = CreateEntry(zip, fileName);
        using var entryStream = entry.Open();
        entryStream.Write(bytes, 0, bytes.Length);
    }
}