using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands.Persistence;

public class CreateORModifyApiKeyPersistence(CreateORModifyApiKeyCommand command) : DapperBase, IQuery<int?>
{
    private readonly CreateORModifyApiKeyCommand _command = command;
    private static readonly string _sqlUpdateORCreate =
        $@"
DECLARE @KeyCount INT = (
    SELECT COUNT(*) FROM [pfw].[ApiKeys] WHERE [FkIdEnte] = @FkIdEnte
);

DECLARE @ExistingMatch INT = (
    SELECT COUNT(*) FROM [pfw].[ApiKeys]
    WHERE [FkIdEnte] = @FkIdEnte AND ([ApiKey] = @ApiKey OR [ApiKey] =@PreviousApiKey)
);

IF @ExistingMatch = 0 AND @KeyCount > 1
BEGIN
    SELECT -1 AS RowsAffected;  
END
ELSE IF @Refresh = 1 AND @ExistingMatch > 0
BEGIN 
    UPDATE [pfw].[ApiKeys]
    SET 
        [ApiKey] = @ApiKey,
        [DataModifica] = @DataModifica, 
        [Attiva] = @Attiva
    WHERE [FkIdEnte] = @FkIdEnte and [ApiKey] = @PreviousApiKey;

    SELECT @@ROWCOUNT AS RowsAffected;
END
ELSE 
BEGIN
    MERGE [pfw].[ApiKeys] AS target
    USING (
        SELECT 
            @FkIdEnte AS FkIdEnte,
            @ApiKey AS ApiKey,
            @DataCreazione AS DataCreazione, 
            @Attiva AS Attiva
    ) AS source
    ON 
        (target.[ApiKey] = source.ApiKey OR (target.[ApiKey] IS NULL AND source.ApiKey IS NULL))
        AND target.[FkIdEnte] = source.FkIdEnte
    WHEN MATCHED THEN
        UPDATE SET
            target.[DataCreazione] = source.DataCreazione,
            target.[DataModifica] = @DataModifica, 
            target.[Attiva] = @Attiva
    WHEN NOT MATCHED THEN
        INSERT ([FkIdEnte], [ApiKey], [DataCreazione],   [Attiva])
        VALUES (source.FkIdEnte, source.ApiKey, source.DataCreazione, source.Attiva); 
    ;
    SELECT @@ROWCOUNT AS RowsAffected;
END
";

    public async Task<int?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync<int>(connection!, _sqlUpdateORCreate, _command, transaction);
    }
}
 