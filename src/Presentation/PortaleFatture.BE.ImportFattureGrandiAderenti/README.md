# PortaleFatture.BE.ImportFattureGrandiAderenti

Console application for importing invoice data from JSON files for large adherents (Grandi Aderenti) into the SQL database.

## Overview

This .NET 8.0 console application processes JSON files containing invoice data for large adherents and imports them into the SQL Server database. It handles invoice headers (testata) and line items (righe) with support for various document types including credit notes.

## Technology Stack

- **.NET 8.0**: Target framework
- **Console Application**: Batch import utility
- **Microsoft.Data.SqlClient**: SQL Server connectivity
- **User Secrets**: Secure configuration storage
- **JSON Processing**: Invoice data deserialization
- **Transactions**: Database transaction support

## Features

- Batch import of invoice data from JSON files
- Support for multiple invoice types (standard invoices, credit notes)
- Transaction-based processing for data integrity
- Handling of invoice headers and line items
- Support for document metadata (CIG, CUP, commissions)
- Automatic calculation of totals with storno (reversal) support
- Environment-specific configuration (dev/uat/prod)

## Configuration

The application uses User Secrets and Environment Variables:

### Required Settings

```json
{
  "Env": "dev|uat|prod",
  "ConnectionStrings": {
    "SqlServerdev": "SQL Server hostname for dev",
    "Databasedev": "Database name for dev",
    "SqlServeruat": "SQL Server hostname for UAT",
    "Databaseuat": "Database name for UAT",
    "SqlServerprod": "SQL Server hostname for prod",
    "Databaseprod": "Database name for prod"
  }
}
```

## Project Structure

```
PortaleFatture.BE.ImportFattureGrandiAderenti/
├── data/
│   └── fatture/
│       ├── anticipo/              # Advance payment invoices
│       │   ├── FatturePagoPa_01_2025.json
│       │   └── FatturePagoPa_12_2024.json
│       ├── primo saldo/           # First payment invoices
│       │   ├── FatturePagoPa_01_2025.json
│       │   ├── FatturePagoPa_INPS_*.json
│       │   └── FatturePagoPa_reg._Lombardia_*.json
│       └── secondo saldo/         # Second payment invoices
├── Extensions/                     # Helper extensions
├── Models/                        # Data models
├── Program.cs                     # Main entry point
└── PortaleFatture.BE.ImportFattureGrandiAderenti.csproj
```

## JSON File Format

Invoice JSON files should contain:

### Invoice Header (Fattura)
- `istitutioID`: Institution/entity ID
- `dataFattura`: Invoice date
- `tipoDocumento`: Document type (TD01, TD04, etc.)
- `tipologiaFattura`: Invoice type (anticipo, primo saldo, secondo saldo)
- `numero`: Invoice number/progressive
- `identificativo`: Invoice identifier (format: MM/YYYY)
- `divisa`: Currency
- `metodoPagamento`: Payment method
- `causale`: Invoice reason/description
- `onboardingTokenID`: Contract code
- `split`: Split payment flag
- `sollecito`: Reminder flag

### Document Metadata (datiGeneraliDocumento)
- `CUP`: Unique Project Code
- `CIG`: Smart CIG
- `idDocumento`: Document ID
- `data`: Document date
- `numItem`: Item number
- `codiceCommessaConvenzione`: Commission/convention code

### Line Items (posizioni)
- `numerolinea`: Line number
- `testo`: Description
- `codiceMateriale`: Material/service code
- `quantita`: Quantity
- `prezzoUnitario`: Unit price
- `imponibile`: Taxable amount
- `periodoRiferimento`: Reference period

## Database Schema

The application imports data into:

### Tables
- `pfw.Fatture`: Invoice headers
- `pfw.FattureRighe`: Invoice line items

### Key Fields Imported

**Invoice Header:**
- Product, date, document type, invoice type
- Entity reference and billing data reference
- Totals, currency, payment method
- Year/month reference
- Document metadata (CIG, CUP, etc.)

**Line Items:**
- Line number, description, material code
- Quantity, unit price, taxable amount
- Reference period
- Stamp duty flag

## Usage

### Preparing Data

1. Place JSON files in appropriate folders under `data/fatture/`:
   - `anticipo/` for advance payments
   - `primo saldo/` for first payments
   - `secondo saldo/` for second payments

2. Ensure JSON files follow the expected format

### Running the Import

1. Set the environment variable:
```bash
setx Env "dev"
```

2. Configure connection strings in user secrets

3. Run the application:
```bash
dotnet run
```

The application will:
- Connect to the database
- Scan the `data/fatture/` directory
- Process files matching the invoice type (e.g., "primo saldo")
- Import invoices and line items within a transaction
- Display progress and results

## Processing Logic

### Invoice Total Calculation
The application calculates totals automatically:
- Standard lines: Add imponibile
- Storno lines (material code contains "STORNO"): Subtract imponibile
- Credit notes (TD04): Negate the total

### Transaction Handling
All imports occur within a database transaction:
- Rollback on any error
- Commit only after all invoices are processed
- Ensures data consistency

### Foreign Key Resolution
For each invoice, the application:
1. Queries for existing billing data (`FkIdDatiFatturazione`)
2. Uses the result if found, otherwise uses DBNull

## Document Types Supported

- **TD01**: Standard invoice
- **TD04**: Credit note (totals are negated)

## Invoice Types

- **anticipo**: Advance payment
- **primo saldo**: First payment
- **secondo saldo**: Second payment

## Error Handling

The application includes:
- Database connection error handling
- Transaction rollback on failures
- Console output for success/failure per invoice
- Detailed error messages

## Output

Console output includes:
- Connection status
- Processing progress per invoice
- Success/failure messages
- Error details if any
- Transaction commit confirmation

Press any key to exit after completion.

## Dependencies

- `Microsoft.Extensions.Configuration.UserSecrets`: 6.0.1
- `PortaleFatture.BE.Infrastructure`: Infrastructure layer reference

## Product Code

All invoices are imported with product code: `prod-pn`

## Identifier Format

Invoice identifiers follow the format: `prod-pn-{invoice_number}`

## Reference Period

Year and month references are extracted from the `identificativo` field (format: MM/YYYY).

## Integration

This console application integrates with:
- SQL Server database (`pfw` schema)
- Infrastructure layer for data models and helpers
- JSON file system for source data

## Notes

- Files marked with `CopyToOutputDirectory: Always` are included in the build output
- Most JSON files have `CopyToOutputDirectory: Never` to save space
- The application processes invoices sequentially within a single transaction
- Large batches may require extended connection timeout settings
