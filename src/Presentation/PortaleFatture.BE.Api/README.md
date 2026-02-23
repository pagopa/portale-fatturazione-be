# PortaleFatture.BE.Api

ASP.NET Core 8.0 Web API for the Portale Fatturazione Backend system. This project provides a modular RESTful API for managing invoicing operations and PagoPA payment integration.

## Overview

This API serves as the presentation layer for the Portale Fatturazione system, implementing a custom modular architecture that supports two main business domains: SEND (invoice management) and PagoPA (payment services).

## Technology Stack

- **.NET 8.0**: Target framework
- **ASP.NET Core**: Web API framework
- **MediatR**: CQRS pattern implementation
- **Azure Services**:
  - Azure Key Vault: Secrets management
  - Azure Storage: Document and file storage
  - Azure Synapse Analytics: Data pipeline integration
  - Azure AD: Authentication and authorization
  - Application Insights: Logging and monitoring
- **Docker**: Containerization support
- **Swagger/OpenAPI**: API documentation

## Architecture

### Modular Design

The application uses a custom module-based architecture where each module represents a distinct business capability. Modules are automatically discovered and registered at startup.

**Key Infrastructure Components**:
- `IModule`: Base interface for all modules
- `IRegistrableModule`: Marker interface for modules that should be auto-registered
- `ModuleExtensions`: Extension methods for module registration and routing
- `MapAttribute`: Custom attribute for endpoint routing configuration

### Modules

#### SEND Module
Invoice and billing management system with the following sub-modules:
- **Accertamenti**: Assessment management
- **APIKey**: API key management
- **Asseverazione**: Certification
- **Auth**: Authentication for SEND users
- **DatiConfigurazioneModuloCommesse**: Order module configuration
- **DatiFatturazioni**: Billing data
- **DatiModuloCommesse**: Order module data
- **DatiRel**: REL data management
- **Fatture**: Invoice management
- **Messaggi**: Messaging system
- **Notifiche**: Notifications
- **Orchestratore**: Orchestration workflows
- **Tipologie**: Type definitions and configurations

#### PagoPA Module
Payment services integration:
- **AnagraficaPSP**: PSP registry management
- **Auth**: PagoPA authentication
- **FinancialReports**: Financial reporting
- **KPIPagamenti**: Payment KPIs and metrics

## Project Structure

```
PortaleFatture.BE.Api/
├── Infrastructure/
│   ├── Culture/                     # Localization support
│   ├── Documenti/                   # Document templates (HTML)
│   ├── ConfigurationExtensions.cs   # Configuration and secrets management
│   ├── IModule.cs                   # Module interface
│   ├── IRegistrableModule.cs        # Registrable module marker
│   ├── MapAttribute.cs              # Routing attribute
│   ├── Module.cs                    # Base module implementation
│   ├── ModuleExtensions.cs          # Module registration and routing
│   ├── ModuleManager.cs             # Module lifecycle management
│   └── NonceMultiTabsMiddleware.cs  # Security middleware for multi-tab sessions
├── Modules/
│   ├── pagoPA/                      # PagoPA module
│   └── SEND/                        # SEND module
├── Models/                          # DTOs and models
├── Orchestrators/                   # Business orchestration
├── Handlers/                        # Request handlers
├── Program.cs                       # Application entry point
├── Dockerfile                       # Docker configuration
└── appsettings.json                 # Application configuration
```

## Configuration

The application relies heavily on environment variables for configuration. All secrets and sensitive data are sourced from environment variables and optionally Azure Key Vault.

### Required Environment Variables

**Database & Core**:
- `CONNECTION_STRING`: Database connection string
- `ADMIN_KEY`: Administrative API key

**JWT Configuration**:
- `JWT_VALID_AUDIENCE`: JWT token audience
- `JWT_VALID_ISSUER`: JWT token issuer
- `JWT_SECRET`: JWT signing secret

**Azure AD**:
- `AZUREAD_INSTANCE`: Azure AD instance URL
- `AZUREAD_TENANTID`: Tenant ID
- `AZUREAD_CLIENTID`: Client ID
- `AZUREAD_ADGROUP`: AD security group

**SelfCare Integration**:
- `SELF_CARE_URI`: SelfCare service URI
- `SELFCARE_CERT_ENDPOINT`: Certificate endpoint
- `SELF_CARE_TIMEOUT`: Request timeout
- `SELF_CARE_AUDIENCE`: SelfCare audience

**SelfCare OnBoarding**:
- `SELFCAREONBOARDING_ENDPOINT`: OnBoarding endpoint
- `SELFCAREONBOARDING_URI`: OnBoarding URI
- `SELFCAREONBOARDING_AUTHTOKEN`: Authentication token

**Support API Service**:
- `SUPPORTAPISERVICE_ENDPOINT`: Support API endpoint
- `SUPPORTAPISERVICE_URI`: Support API URI
- `SUPPORTAPISERVICE_AUTHTOKEN`: Authentication token

**Storage Accounts**:
- `STORAGE_CONNECTIONSTRING`: Main storage connection
- `STORAGE_REL_FOLDER`: REL folder path
- `STORAGE_DOCUMENTI_CONNECTIONSTRING`: Documents storage connection
- `STORAGE_DOCUMENTI_FOLDER`: Documents folder path

**PagoPA Financial Storage**:
- `STORAGE_FINANCIAL_ACCOUNTNAME`: Account name
- `STORAGE_FINANCIAL_ACCOUNTKEY`: Account key
- `STORAGE_FINANCIAL_CONTAINERNAME`: Container name

**REL Storage**:
- `StorageRELAccountName`: Account name
- `StorageRELAccountKey`: Account key
- `StorageRELBlobContainerName`: Container name
- `StorageRELCustomDns`: Custom DNS endpoint

**Notifications Storage**:
- `StorageNotificheAccountName`: Account name
- `StorageNotificheAccountKey`: Account key
- `StorageNotificheBlobContainerName`: Container name
- `StorageNotificheCustomDNS`: Custom DNS endpoint

**Azure Synapse**:
- `SYNAPSE_WORKSPACE_NAME`: Workspace name
- `PIPELINE_NAME_SAP`: SAP pipeline name
- `SYNAPSE_SUBSCRIPTIONID`: Subscription ID
- `SYNAPSE_RESOURCEGROUPNAME`: Resource group name

**Azure Function**:
- `AzureFunctionNotificheUri`: Notifications function URI
- `AzureFunctionAppKey`: Function app key

**Monitoring**:
- `APPLICATION_INSIGHTS`: Application Insights connection string

**Security**:
- `CORS_ORIGINS`: Allowed CORS origins

**Optional**:
- `KEY_VAULT_NAME`: Azure Key Vault name (for secret retrieval)

## Database Schemas

The application uses two primary database schemas:
- `pfw`: Invoice/billing data schema
- `pfd`: SelfCare data schema

## Authentication & Authorization

The API supports multiple authentication providers:

1. **Azure AD**: Enterprise authentication using Microsoft Identity
2. **SelfCare**: Custom SelfCare token-based authentication
3. **PagoPA**: PagoPA ID token and access token authentication

**Roles**:
- `OPERATOR`: Standard operational access
- `ADMIN`: Administrative access

## Running the Application

### Prerequisites
- .NET 8.0 SDK
- SQL Server or compatible database
- Azure subscription (for cloud services)

### Local Development

1. Set required environment variables or use `appsettings.Development.json`
2. Run the application:
   ```bash
   dotnet run
   ```
3. Access Swagger UI at: `https://localhost:<port>/swagger`

### Docker

Build and run using Docker:

```bash
docker build -t portalefatture-api -f src/Presentation/PortaleFatture.BE.Api/Dockerfile .
docker run -p 8080:80 portalefatture-api
```

## API Documentation

API documentation is available via Swagger/OpenAPI at the `/swagger` endpoint when running in development mode.

## Kestrel Configuration

The application is configured with the following Kestrel limits:
- **Max Request Body Size**: 2 GB
- **Request Timeout**: 10 minutes
- **Keep Alive Timeout**: 10 minutes
- **Request Headers Timeout**: 10 minutes
- **Max Response Buffer Size**: 1 MB

## Logging

Logging is configured to minimize noise in production:
- Microsoft hosting logs: Error level only
- Entity Framework Core: Error level only
- ASP.NET Core: Error level only

Logs are sent to Application Insights when configured.

## Security Features

- **CORS**: Configurable origin restrictions
- **JWT Authentication**: Secure token-based authentication
- **Azure AD Integration**: Enterprise identity management
- **Multi-Tab Nonce Middleware**: Session security for multi-tab scenarios
- **Role-Based Authorization**: Fine-grained access control

## Dependencies

Key package dependencies:
- `Microsoft.Identity.Web`: 3.8.4
- `Azure.Security.KeyVault.Secrets`: 4.6.0
- `Microsoft.Extensions.Logging.ApplicationInsights`: 2.22.0
- `Swashbuckle.AspNetCore`: 6.8.1
- `Microsoft.Extensions.Caching.Memory`: 8.0.1
- `System.Text.Json`: 8.0.5

## Related Projects

- `PortaleFatture.BE.Infrastructure`: Infrastructure layer
- `PortaleFatture.BE.Core`: Core business logic and domain models

## License

[Add license information]

## Support

[Add support/contact information]
