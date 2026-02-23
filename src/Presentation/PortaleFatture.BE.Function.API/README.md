# PortaleFatture.BE.Function.API

Azure Functions application providing serverless API endpoints and durable orchestrations for document processing, notifications, and contestations management.

## Overview

This is an Azure Functions v4 application built on .NET 8.0 that provides serverless API endpoints and long-running orchestrations for the Portale Fatturazione system. It handles asynchronous workflows including document generation, notification processing, and dispute management.

## Technology Stack

- **.NET 8.0**: Target framework
- **Azure Functions v4**: Serverless platform
- **Durable Functions**: Orchestration and workflow management
- **ASP.NET Core Integration**: Full ASP.NET Core pipeline support
- **OpenAPI/Swagger**: API documentation
- **Docker**: Container support for Linux deployment
- **wkhtmltopdf**: PDF generation from HTML

## Key Features

- **Serverless API**: HTTP-triggered functions with OpenAPI documentation
- **Durable Orchestrations**: Long-running workflows and state management
- **Authentication**: API key-based authentication middleware
- **Document Generation**: PDF creation using wkhtmltopdf
- **Storage Integration**: Azure Blob Storage for documents and notifications
- **CORS Support**: Cross-origin resource sharing enabled

## Project Structure

```
PortaleFatture.BE.Function.API/
├── Autenticazione/              # Authentication functions
├── Contestazioni/               # Dispute management
│   └── Orchestrators/          # Dispute orchestration workflows
├── DocumentiEmessi/            # Issued documents processing
├── Extensions/                 # Extension methods
├── Infrastructure/             # Configuration and setup
├── Middleware/                 # Custom middleware
│   ├── AuthMiddleware.cs       # API key authentication
│   └── LogCustomDataMiddleware.cs
├── Models/                     # DTOs and models
├── ModuloCommessa/            # Order module functions
├── Notifiche/                 # Notification processing
├── RegolareEsecuzione/        # Regular execution functions
├── native/                    # Native binaries
│   ├── libwkhtmltox.so       # PDF library
│   └── wkhtmltopdf           # PDF converter
├── Program.cs                 # Application entry point
├── host.json                  # Function host configuration
├── local.settings.json        # Local development settings
└── Dockerfile                 # Container definition
```

## Function Categories

### Autenticazione
Authentication and authorization functions

### Contestazioni
Dispute/contestation management with orchestrators for:
- Bulk entity upload processing
- Contestation workflow management

### DocumentiEmessi
Issued document processing and management

### ModuloCommessa
Order module functions

### Notifiche
Notification processing and delivery

### RegolareEsecuzione
Regular execution and compliance functions

## Configuration

### Required Environment Variables

- `AES_KEY`: AES encryption key for sensitive data
- `CONNECTION_STRING`: Database connection string
- `OpenApi:HostNames`: Custom domain for OpenAPI documentation

### Storage Configuration

- `STORAGE_ACCOUNT_NAME`: Azure Storage account name
- `STORAGE_ACCOUNT_KEY`: Storage account key
- `STORAGE_NOTIFICHE`: Notifications blob container
- `STORAGE_REL_DOWNLOAD`: REL download container
- `STORAGE_CUSTOM_HOSTNAME`: Custom storage DNS endpoint

### Contestazioni Storage

- `StorageContestazioni:AccountName`: Account name
- `StorageContestazioni:AccountKey`: Account key
- `StorageContestazioni:BlobContainerName`: Container name
- `StorageContestazioni:CustomDns`: Custom DNS

### Documents Storage

- `StorageDocumenti:ConnectionString`: Connection string
- `StorageDocumenti:DocumentiFolder`: Documents folder path

## Authentication

The application uses API key authentication via the `AuthMiddleware`:

- API keys passed in `X-Api-Key` header
- Middleware validates keys before function execution
- OpenAPI definition includes security scheme

## OpenAPI/Swagger

Swagger documentation is available and configured with:
- Title: "Portale Fatturazione API"
- Version: v1
- API Key security scheme (X-Api-Key header)
- Custom host names support

Access Swagger UI at: `/api/swagger/ui`

## Database

Uses two database schemas:
- `pfw`: Invoice/billing schema
- `pfd`: SelfCare schema

Database context factory pattern for connection management.

## Services Registration

The application registers several services:
- **MediatR**: CQRS pattern implementation
- **Localization**: Multi-language support (Resources path)
- **AES Encryption**: Data encryption service
- **Database Factories**: Context factories for both schemas
- **Storage Services**: Blob storage services for various containers
- **Document Builder**: HTML to PDF document generation
- **Scadenzario Service**: Due date/schedule management

## PDF Generation

Uses wkhtmltopdf for PDF generation:
- Native binaries located in `native/` folder
- Supports Linux environment (libwkhtmltox.so)
- Configured via `LD_LIBRARY_PATH` environment variable

## CORS

CORS is configured to allow:
- All origins (*)
- All methods
- All headers

Note: Consider restricting origins in production.

## Running Locally

1. Configure `local.settings.json` with required settings:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CONNECTION_STRING": "your-connection-string",
    "AES_KEY": "your-aes-key",
    "STORAGE_ACCOUNT_NAME": "your-storage-account",
    "STORAGE_ACCOUNT_KEY": "your-storage-key"
  }
}
```

2. Run the function app:

```bash
func start
```

Or using Visual Studio/VS Code with Azure Functions extension.

## Docker Deployment

Build and run the container:

```bash
docker build -f src/Presentation/PortaleFatture.BE.Function.API/Dockerfile -t portalefatture-functions .
docker run -p 8080:80 portalefatture-functions
```

The Dockerfile:
- Targets Linux containers
- Includes native wkhtmltopdf binaries
- Multi-stage build for optimization

## Dependencies

### NuGet Packages

- `Microsoft.Azure.Functions.Worker`: 2.0.0
- `Microsoft.Azure.Functions.Worker.Extensions.DurableTask`: 1.3.0
- `Microsoft.Azure.Functions.Worker.Extensions.Http`: 3.3.0
- `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore`: 2.0.1
- `Microsoft.Azure.Functions.Worker.Extensions.OpenApi`: 1.5.1
- `Microsoft.Azure.Functions.Worker.Sdk`: 2.0.2
- `Swashbuckle.AspNetCore`: 8.1.1

### Project References

- `PortaleFatture.BE.Infrastructure`: Infrastructure layer
- `PortaleFatture.BE.Api`: Shared API components

## Durable Functions

The application supports durable orchestrations for:
- Long-running workflows
- State management
- Retry logic
- Sub-orchestrations
- Human interaction patterns

## Logging

Custom logging middleware (`LogCustomDataMiddleware`) for:
- Request/response logging
- Performance monitoring
- Error tracking
- Custom telemetry data

## Development

Use the included `local.settings.json` for local development. This file:
- Is not published to Azure
- Contains local-only settings
- Should be excluded from source control

## Deployment

Deploy to Azure Functions using:
- Azure CLI
- Visual Studio publish
- GitHub Actions
- Azure DevOps pipelines

Ensure all environment variables are configured in Azure Function App settings.

## Integration

This function app integrates with:
- Main Web API (`PortaleFatture.BE.Api`)
- Infrastructure layer for business logic
- Azure Storage for document management
- SQL Database for persistence
- External notification services
