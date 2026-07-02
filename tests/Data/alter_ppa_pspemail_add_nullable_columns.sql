IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'ppa')
BEGIN
    EXEC ('CREATE SCHEMA ppa;');
END;

IF OBJECT_ID('[ppa].[PspEmail]', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('ppa.PspEmail', 'Oggetto') IS NULL
    BEGIN
        ALTER TABLE [ppa].[PspEmail] ADD [Oggetto] NVARCHAR(MAX) NULL;
    END;

    IF COL_LENGTH('ppa.PspEmail', 'Corpo') IS NULL
    BEGIN
        ALTER TABLE [ppa].[PspEmail] ADD [Corpo] NVARCHAR(MAX) NULL;
    END;

    IF COL_LENGTH('ppa.PspEmail', 'Link') IS NULL
    BEGIN
        ALTER TABLE [ppa].[PspEmail] ADD [Link] NVARCHAR(MAX) NULL;
    END;
END;
