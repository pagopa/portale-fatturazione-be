IF OBJECT_ID('[stg].[PspEmailPreview]', 'U') IS NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'stg')
    BEGIN
        EXEC ('CREATE SCHEMA stg;');
    END;

    CREATE TABLE [stg].[PspEmailPreview]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdContratto] NVARCHAR(100) NULL,
        [Tipologia] NVARCHAR(100) NULL,
        [Anno] INT NULL,
        [Trimestre] NVARCHAR(20) NULL,
        [DataEvento] NVARCHAR(50) NULL,
        [Email] NVARCHAR(320) NULL,
        [Oggetto] NVARCHAR(MAX) NULL,
        [Corpo] NVARCHAR(MAX) NULL,
        [Link] NVARCHAR(MAX) NULL,
        [RagioneSociale] NVARCHAR(500) NULL,
        [Invio] BIT NOT NULL CONSTRAINT [DF_PspEmailPreview_Invio] DEFAULT(0),
        [TipoContratto] NVARCHAR(100) NULL,
        CONSTRAINT [PK_PspEmailPreview] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END;
