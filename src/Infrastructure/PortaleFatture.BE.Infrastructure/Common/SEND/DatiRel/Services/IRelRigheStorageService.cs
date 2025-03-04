namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Services; 
public interface IRelRigheStorageService
{
    string? GetBlobName(string sidtestata, string nomeEnte, string? extension = ".csv");
    string? GetSASToken(string sidtestata, string nomeEnte);
}