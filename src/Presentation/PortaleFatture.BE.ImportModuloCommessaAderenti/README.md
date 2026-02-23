# PortaleFatture.BE.ImportModuloCommessaAderenti

Console application for importing module commission categorization data for adherents from JSON files into the SQL database.

## Overview

This .NET 8.0 console application imports client categorization data for the SEND module commissions (Modulo Commessa) from JSON files into the SQL Server database. It processes client segmentation, sales categories, and geographical information.

## Technology Stack

- **.NET 8.0**: Target framework
- **Console Application**: Batch import utility
- **Microsoft.Data.SqlClient**: SQL Server connectivity (5.2.2)
- **System.Data.SqlClient**: Legacy SQL client (4.9.0)
- **JSON Processing**: Client categorization data

## Features

- JSON file parsing for client categorization
- Bulk data import to SQL Server
- Client segmentation data import
- Sales category hierarchy (macro and sub-categories)
- Geographical data (province and region)
- Export date tracking

## Project Structure

```
PortaleFatture.BE.ImportModuloCommessaAderenti/
├── Documenti/
│   └── categorizzazione_clienti_send_2025-07-14_08-50.json
├── JsonFileReader.cs              # JSON parsing utility
├── MooduloCommessaAderentiDto.cs  # Data model
├── Program.cs                     # Main entry point
└── PortaleFatture.BE.ImportModuloCommessaAderenti.csproj
```

## JSON File Format

The JSON file should contain an array of client categorization objects:

```json
[
  {
    "dataExport": "2025-07-14",
    "internalistitutionid": "INSTITUTION_ID",
    "segmento": "Segment name",
    "macrocategoriaVendita": "Macro category",
    "sottocategoriaVendita": "Sub category",
    "provincia": "Province code",
    "regione": "Region name"
  }
]
```

### Fields

- **dataExport**: Export date (date format)
- **internalistitutionid**: Internal institution identifier
- **segmento**: Client segment classification
- **macrocategoriaVendita**: Sales macro-category
- **sottocategoriaVendita**: Sales sub-category
- **provincia**: Province code
- **regione**: Region name

## Database Schema

### Target Table
`pfw.DatiModuloCommessaAderenti`

### Columns
- `dataExport` (NVARCHAR): Export date
- `internalistitutionid` (NVARCHAR): Institution ID
- `segmento` (NVARCHAR): Segment (nullable)
- `macrocategoriaVendita` (NVARCHAR): Macro sales category (nullable)
- `sottocategoriaVendita` (NVARCHAR): Sub sales category (nullable)
- `provincia` (NVARCHAR): Province
- `regione` (NVARCHAR): Region

## Configuration

The connection string is currently hardcoded in `Program.cs`:

```csharp
var connectionString = "Server=tcp:fat-d-sql.database.windows.net,1433;Initial Catalog=fat-sqldb;...";
```

**Note**: This should be moved to configuration for security and flexibility.

## Usage

### Preparing Data

1. Place the JSON file in `Documenti/` folder
2. Ensure the filename matches: `categorizzazione_clienti_send_2025-07-14_08-50.json`
3. Verify JSON format matches the expected structure

### Running the Import

Run the application:

```bash
dotnet run
```

The application will:
1. Locate the JSON file in the output directory
2. Parse the JSON data
3. Connect to the SQL Server database
4. Insert each record into the database
5. Display success message or error details

## Processing Logic

### Data Handling
- Empty string values for segmento, macrocategoriaVendita, and sottocategoriaVendita are converted to `DBNull`
- Non-nullable fields (provincia, regione) are inserted as-is
- All other fields are inserted with their values

### Insert Strategy
- Individual inserts per record (not bulk insert)
- Parameterized queries to prevent SQL injection
- No transaction wrapping (each insert is independent)

## Error Handling

The application includes:
- Try-catch block around database operations
- Console output of exception messages
- Continues processing remaining records on individual failures

## Output

Console output includes:
- Error messages if any exception occurs
- Success message: "Tutti i dati dell'array inseriti con successo usando ADO.NET!"

## Dependencies

### NuGet Packages
- `Microsoft.Data.SqlClient`: 5.2.2
- `System.Data.SqlClient`: 4.9.0

## Security Considerations

**Important**: The current implementation has hardcoded credentials in `Program.cs`:

```csharp
User ID=pfwebuser;Password=x&5DLJ-12CcF9ma;
```

**Recommendations**:
1. Move connection string to User Secrets or environment variables
2. Use managed identities when deployed to Azure
3. Remove credentials from source code
4. Implement proper configuration management

## Data Categories

### Segmentation
Client segmentation for commission module categorization

### Sales Categories
Two-level hierarchy:
- **Macro Category**: High-level sales category
- **Sub Category**: Detailed sales category

### Geography
- **Province**: Italian province codes
- **Region**: Italian region names

## File Deployment

The JSON file is set to `CopyToOutputDirectory: Always` in the project file, ensuring it's available at runtime.

## Integration

This console application integrates with:
- SQL Server database (`pfw` schema)
- SEND module commission system
- Client categorization data pipeline

## Improvements Needed

1. **Configuration Management**: Move connection string to secure configuration
2. **Bulk Insert**: Implement bulk insert for better performance
3. **Transaction Support**: Wrap inserts in a transaction for atomicity
4. **Validation**: Add data validation before insert
5. **Logging**: Implement proper logging instead of console output
6. **Error Recovery**: Add retry logic for transient failures
7. **Duplicate Handling**: Check for existing records before insert

## Date Format

Export dates should be in ISO format: `YYYY-MM-DD`

## Institution ID

Institution IDs should match the values in the main entity tables for referential integrity.

## Execution

The application runs synchronously and exits after processing all records. Monitor console output for completion status.
