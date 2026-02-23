namespace PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;
public sealed class DocumentiContestazioniSASSStorageKey(
    string? containerName,
    string? idEnte,
    string? contractId,
    string? uniqueId,
    string? fileName)
{
    public string? FileName { get; init; } = fileName;
    public string? ContainerName { get; init; } = containerName;
    public string? UniqueId { get; init; } = uniqueId;
    public string? ContractId { get; init; } = contractId;
    public string? IdEnte { get; init; } = idEnte;
    public override string ToString()
    {
        return $"{IdEnte}/{ContractId}/{UniqueId}/{FileName}";
    }

    public string DocumentReportLinkJson()
    {
        return $"{ContainerName}/{IdEnte}/{ContractId}/{UniqueId}";
    }

    public string DocumentNomeReportJson()
    {
        return $"{UniqueId}.json";
    }

    public static DocumentiContestazioniSASSStorageKey Deserialize(string linkDocumento)
    {
        var dati = linkDocumento!.Split("/");
        var containerName = dati[0];
        var idEnte = dati[1];
        var contractId = dati[2];
        var uniqueId = dati[3];
        var fileName = dati[4];
        return new DocumentiContestazioniSASSStorageKey(containerName, idEnte, contractId, uniqueId, fileName);
    }

    public string Serialize()
    {
        return $"{this.ContainerName}/{this.IdEnte}/{this.ContractId}/{this.UniqueId}/{this.FileName}";
    }
}