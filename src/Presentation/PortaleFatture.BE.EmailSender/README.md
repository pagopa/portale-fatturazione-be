# PortaleFatture.BE.EmailSender

Console application for sending REL (Regolare Esecuzione) notification emails to entities using SMTP.

## Overview

This .NET 8.0 console application is designed to send "Regolare Esecuzione" (Regular Execution) notification emails to entities based on invoice data. It processes CSV files containing entity information and sends customized HTML emails using SMTP.

## Technology Stack

- **.NET 8.0**: Target framework
- **Console Application**: Batch processing executable
- **SMTP**: Email protocol
- **User Secrets**: Secure configuration storage
- **CSV Processing**: Entity data import

## Features

- CSV-based entity data import
- HTML email template generation
- SMTP-based email sending
- Database integration for email tracking
- Configurable invoice types (PRIMO SALDO, SECONDO SALDO)
- Email delivery tracking and logging

## Configuration

The application uses User Secrets for secure configuration. Required settings:

```json
{
  "PortaleFattureOptions": {
    "ConnectionString": "Database connection string",
    "FROM": "Sender email address",
    "SMTP": "SMTP server address",
    "SMTP_PORT": "SMTP port number",
    "SMTP_AUTH": "SMTP authentication username",
    "SMTP_PASSWORD": "SMTP authentication password"
  }
}
```

## Project Structure

```
PortaleFatture.BE.EmailSender/
├── Infrastructure/
│   └── Documenti/
│       ├── Enti.csv                # Entity data (institution info)
│       ├── primo_saldo.html        # First payment email template
│       └── secondo_saldo.html      # Second payment email template
├── Models/                         # Configuration and data models
├── Program.cs                      # Main entry point
└── PortaleFatture.BE.EmailSender.csproj
```

## Usage

### CSV Format

The `Enti.csv` file should contain the following columns (semicolon-separated):
- IdEnte: Entity ID
- IdContratto: Contract ID
- TipologiaFattura: Invoice type (PRIMO SALDO, SECONDO SALDO)
- Anno: Year
- Mese: Month
- Pec: PEC email address
- RagioneSociale: Company name

### Running the Application

1. Configure user secrets with SMTP credentials
2. Update invoice parameters in `Program.cs`:
   - `anno`: Year
   - `mese`: Month
   - `tipologiafattura`: Invoice type
   - `ricalcola`: Recalculation flag (0/1)
3. Ensure `Enti.csv` is populated
4. Run the application:

```bash
dotnet run
```

## Email Processing

The application performs the following steps:

1. **Load Configuration**: Reads SMTP settings from user secrets
2. **Import CSV**: Reads entity data from `Infrastructure/Documenti/Enti.csv`
3. **Generate Emails**: Creates HTML emails using templates
4. **Send Emails**: Sends via SMTP to each entity's PEC address
5. **Track Delivery**: Records email status in database

## Email Templates

HTML templates support the following invoice types:
- **PRIMO SALDO**: First payment notification
- **SECONDO SALDO**: Second payment notification

Templates are located in `Infrastructure/Documenti/` and are customized per entity.

## Database Tracking

Email delivery is tracked in the database with:
- Sending timestamp
- Entity and contract information
- Success/failure status
- Error messages (if any)
- PEC address used
- Invoice details (year, month, type)

## Dependencies

- `Microsoft.Extensions.Configuration.UserSecrets`: 8.0.1
- `PortaleFatture.BE.Infrastructure`: Infrastructure layer reference

## Error Handling

The application includes comprehensive error handling:
- Database connection validation
- SMTP connection errors
- Email sending failures
- CSV parsing errors

All errors are logged to console and database.

## Output

The application outputs:
- Processing status for each entity
- Email sending results (success/failure messages)
- Final execution summary (JSON format)
- Error details if any exceptions occur

## Integration

This console application integrates with:
- Database for email tracking (`EmailRelService`)
- Infrastructure layer for email sending (`EmailSender`)
- Document builder for HTML template processing (`DocumentBuilder`)

## Execution Mode

Currently, the email sending logic is commented out in the code, allowing for:
- Testing template generation
- Dry-run validation
- Database connection verification

Uncomment the sending logic in the foreach loop to enable actual email delivery.
