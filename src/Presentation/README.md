# Presentation Layer - Portale Fatturazione Backend

This directory contains all presentation layer projects for the Portale Fatturazione Backend system. The presentation layer includes Web APIs, Azure Functions, console utilities, and data import tools.

## Overview

The Portale Fatturazione Backend presentation layer is organized into multiple specialized projects, each serving a specific purpose in the overall system architecture. These projects range from the main Web API to specialized utility applications for data import and email processing.

## Projects Summary

### 1. PortaleFatture.BE.Api
**Type**: ASP.NET Core 8.0 Web API
**Purpose**: Main RESTful API for the Portale Fatturazione system

The core API application implementing a modular architecture with two main business domains:
- **SEND Module**: Invoice and billing management (13 sub-modules)
- **PagoPA Module**: Payment services integration (4 sub-modules)

**Key Features**:
- Custom modular architecture with automatic module discovery
- Multi-provider authentication (Azure AD, SelfCare, PagoPA)
- Swagger/OpenAPI documentation
- Azure integrations (Key Vault, Storage, Synapse, Application Insights)
- Docker support
- Role-based authorization

**Endpoints**: RESTful API with Swagger UI at `/swagger`

[View Full Documentation](./PortaleFatture.BE.Api/README.md)

---

### 2. PortaleFatture.BE.Function.API
**Type**: Azure Functions v4 (.NET 8.0)
**Purpose**: Serverless API and durable orchestrations

Provides serverless HTTP endpoints and long-running orchestrations for:
- Document generation (PDF using wkhtmltopdf)
- Notification processing
- Contestations management
- Workflow orchestration

**Key Features**:
- Durable Functions for complex workflows
- API key authentication
- OpenAPI/Swagger documentation
- Azure Storage integration
- PDF generation capabilities
- Docker support for Linux

**Endpoints**: Available at `/api/{function-name}` with Swagger at `/api/swagger/ui`

[View Full Documentation](./PortaleFatture.BE.Function.API/README.md)

---

### 3. PortaleFatture.BE.SendEmailFunction
**Type**: Azure Functions v4 (.NET 8.0)
**Purpose**: Email sending and workflow orchestration

Handles email delivery through multiple channels:
- SMTP email sending
- PSP (Gmail API) email sending
- Alert notifications
- REL righe creation
- Commission module forecasts

**Key Features**:
- Durable orchestrations for email workflows
- Multiple email providers (SMTP, Gmail)
- HTML template support
- Retry logic and error handling
- Background job processing

**Functions**:
- SendEmail, SendEmailPsp, SendAlert
- CreateRelRighe, ModuloCommessaPrevisionale, RichiestaNotifiche

[View Full Documentation](./PortaleFatture.BE.SendEmailFunction/README.md)

---

### 4. PortaleFatture.BE.SelfCareOnBoardingAPI
**Type**: ASP.NET Core 8.0 Minimal API
**Purpose**: Mock SelfCare onboarding endpoints

Development/testing API simulating SelfCare integration:
- Recipient code verification
- Onboarding status updates
- Authorization simulation

**Key Features**:
- Lightweight minimal API
- Swagger documentation
- Test scenario support
- Console logging
- Bearer token authentication (mock)

**Use Cases**: Development, testing, integration testing, demos

[View Full Documentation](./PortaleFatture.BE.SelfCareOnBoardingAPI/README.md)

---

### 5. PortaleFatture.BE.EmailSender
**Type**: .NET 8.0 Console Application
**Purpose**: REL notification email batch sending

Sends "Regolare Esecuzione" (Regular Execution) notification emails:
- CSV-based entity data import
- HTML email template rendering
- SMTP delivery
- Database tracking

**Email Types**:
- PRIMO SALDO (First payment)
- SECONDO SALDO (Second payment)

**Input**: CSV files with entity and PEC information
**Output**: Batch email delivery with database tracking

[View Full Documentation](./PortaleFatture.BE.EmailSender/README.md)

---

### 6. PortaleFatture.BE.EmailPSPSender
**Type**: .NET 8.0 Console Application
**Purpose**: PSP email sending via Gmail API

Sends emails to Payment Service Providers using Gmail API:
- OAuth2 authentication
- Gmail API integration
- Sent folder organization

**Configuration**: User Secrets with OAuth2 credentials
**Output**: Email delivery with Gmail folder management

[View Full Documentation](./PortaleFatture.BE.EmailPSPSender/README.md)

---

### 7. PortaleFatture.BE.ImportFattureGrandiAderenti
**Type**: .NET 8.0 Console Application
**Purpose**: Large adherent invoice import

Imports invoice data from JSON files to SQL Server:
- Batch JSON file processing
- Invoice headers and line items
- Transaction-based processing
- Support for credit notes (TD04)

**Data Types**: Anticipo, Primo Saldo, Secondo Saldo
**Format**: JSON files with invoice data
**Database**: SQL Server with `pfw` schema

[View Full Documentation](./PortaleFatture.BE.ImportFattureGrandiAderenti/README.md)

---

### 8. PortaleFatture.BE.ImportModuloCommessaAderenti
**Type**: .NET 8.0 Console Application
**Purpose**: Commission module categorization import

Imports client categorization data from JSON:
- Segmentation data
- Sales categories (macro/sub)
- Geographical information

**Input**: JSON file with categorization data
**Output**: SQL Server table population
**Schema**: `pfw.DatiModuloCommessaAderenti`

[View Full Documentation](./PortaleFatture.BE.ImportModuloCommessaAderenti/README.md)

---

### 9. PortaleFatture.BE.ImportFinancepagoPA
**Type**: Python 3.x Project
**Purpose**: PagoPA financial data import

Python scripts for importing Parquet files to SQL Server:
- Contracts import
- KPMG reports import
- Financial reports import
- Schema validation

**Key Features**:
- Parquet file processing (pandas)
- Azure AD authentication (pyodbc)
- Environment-specific config (dev/uat/prod)
- Hadoop/Spark integration support

**Scripts**:
- `import_contracts.py`
- `import_kpmg.py`
- `import_financial_report.py`
- `import_check_schema.py`

[View Full Documentation](./PortaleFatture.BE.ImportFinancepagoPA/README.md)

---

## Architecture Patterns

### API Projects
- **Modular Design**: Custom module system with automatic discovery
- **CQRS**: MediatR pattern for commands and queries
- **Repository Pattern**: Database access abstraction
- **Dependency Injection**: Built-in ASP.NET Core DI

### Azure Functions
- **Durable Functions**: State management and orchestration
- **Isolated Worker Process**: .NET 8 isolated mode
- **Trigger-based**: HTTP, Timer, Queue, and Event triggers
- **Serverless**: Auto-scaling and pay-per-use

### Console Applications
- **Batch Processing**: One-time or scheduled execution
- **User Secrets**: Secure local configuration
- **Transaction Support**: Database integrity
- **Error Handling**: Comprehensive logging

### Python Projects
- **Pandas**: Data manipulation
- **Azure AD Auth**: Secure database access
- **Environment Config**: Multi-environment support
- **Parquet Processing**: Efficient columnar data

## Technology Stack Summary

| Technology | Projects Using It |
|------------|------------------|
| .NET 8.0 | Api, Function.API, SendEmailFunction, All Console Apps, SelfCareOnBoardingAPI |
| Azure Functions v4 | Function.API, SendEmailFunction |
| ASP.NET Core Web API | Api, SelfCareOnBoardingAPI |
| Durable Functions | Function.API, SendEmailFunction |
| Python 3.x | ImportFinancepagoPA |
| MediatR | Api, Function.API |
| Azure Storage | Api, Function.API, SendEmailFunction |
| Azure Key Vault | Api |
| Swagger/OpenAPI | Api, Function.API, SelfCareOnBoardingAPI |
| Docker | Api, Function.API |
| SQL Server | All .NET projects, ImportFinancepagoPA |
| SMTP | EmailSender, SendEmailFunction |
| Gmail API | EmailPSPSender, SendEmailFunction |

## Database Schemas

### pfw (Portale Fatture Web)
Invoice and billing data:
- Fatture (Invoices)
- FattureRighe (Invoice lines)
- DatiModuloCommessaAderenti
- And more...

### pfd (Portale Fatture Data)
SelfCare and configuration data:
- User data
- Entity information
- Configuration tables

## Authentication Methods

1. **Azure AD**: Main API, Function.API
2. **SelfCare Token**: Main API
3. **PagoPA Token**: Main API
4. **API Key**: Function.API
5. **OAuth2 (Gmail)**: EmailPSPSender, SendEmailFunction
6. **Azure AD (Python)**: ImportFinancepagoPA

## Deployment Targets

### Azure App Service
- PortaleFatture.BE.Api
- PortaleFatture.BE.SelfCareOnBoardingAPI

### Azure Functions
- PortaleFatture.BE.Function.API
- PortaleFatture.BE.SendEmailFunction

### Azure Container Instances / AKS
- PortaleFatture.BE.Api (Docker)
- PortaleFatture.BE.Function.API (Docker)

### Scheduled Tasks / Jobs
- PortaleFatture.BE.EmailSender
- PortaleFatture.BE.EmailPSPSender
- PortaleFatture.BE.ImportFattureGrandiAderenti
- PortaleFatture.BE.ImportModuloCommessaAderenti

### Azure Data Factory / Databricks / Synapse
- PortaleFatture.BE.ImportFinancepagoPA

## Integration Points

```
┌─────────────────────────────────────────────────────────────┐
│                    Azure Front Door / API Gateway            │
└───────────────────────────┬─────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
┌───────────────┐  ┌────────────────┐  ┌──────────────────┐
│ BE.Api        │  │ Function.API   │  │ SendEmailFunc    │
│ (Main API)    │  │ (Serverless)   │  │ (Email Service)  │
└───────┬───────┘  └────────┬───────┘  └────────┬─────────┘
        │                   │                   │
        │         ┌─────────┴─────────┐        │
        │         │                   │        │
        ▼         ▼                   ▼        ▼
┌─────────────────────────────────────────────────────────┐
│              Azure SQL Database (pfw, pfd schemas)       │
└─────────────────────────────────────────────────────────┘
        ▲         ▲                   ▲        ▲
        │         │                   │        │
        │    ┌────┴────┐         ┌────┴────┐  │
        │    │         │         │         │  │
┌───────┴────┴──┐  ┌───┴─────────┴──┐  ┌───┴──┴──────────┐
│ Import Apps   │  │ Email Apps     │  │ Python Import   │
│ (Console)     │  │ (Console/Func) │  │ (Parquet)       │
└───────────────┘  └────────────────┘  └─────────────────┘
```

## Environment Configuration

### Development
- User Secrets for sensitive data
- Local SQL Server or Azure SQL
- Development storage emulator
- Console logging enabled
- Swagger UI enabled

### UAT/Staging
- Azure Key Vault for secrets
- Azure SQL Database
- Azure Storage
- Application Insights
- Limited Swagger access

### Production
- Azure Key Vault mandatory
- Managed identities
- Full Azure integration
- Enhanced monitoring
- Swagger disabled (or API key protected)

## Running the Projects

### APIs
```bash
cd src/Presentation/PortaleFatture.BE.Api
dotnet run
```

### Azure Functions
```bash
cd src/Presentation/PortaleFatture.BE.Function.API
func start
```

### Console Applications
```bash
cd src/Presentation/PortaleFatture.BE.EmailSender
dotnet run
```

### Python Projects
```bash
cd src/Presentation/PortaleFatture.BE.ImportFinancepagoPA
python import_contracts.py prod ./data/file.parquet
```

## Common Dependencies

All .NET projects reference:
- `PortaleFatture.BE.Infrastructure`: Infrastructure layer
- `PortaleFatture.BE.Core`: Core business logic (via Infrastructure)

## Security Best Practices

1. **Never commit secrets**: Use User Secrets, Key Vault, or environment variables
2. **Use managed identities**: In Azure for database and storage access
3. **Enable HTTPS**: For all API endpoints
4. **Implement authentication**: On all production endpoints
5. **Audit logging**: Enable for all database access
6. **CORS policies**: Restrict to known origins
7. **Input validation**: On all user inputs
8. **SQL injection prevention**: Use parameterized queries
9. **Rate limiting**: Implement on public APIs
10. **Security headers**: Configure proper HTTP headers

## Monitoring and Observability

### Application Insights
- Request/response telemetry
- Exception tracking
- Performance monitoring
- Custom metrics
- Dependency tracking

### Logging
- Structured logging
- Log levels (Error, Warning, Info, Debug)
- Correlation IDs for distributed tracing
- Azure Monitor integration

### Alerting
- Failed request thresholds
- Performance degradation
- Exception rates
- Resource utilization

## Documentation

Each project contains its own detailed README with:
- Project purpose and overview
- Technology stack
- Configuration requirements
- Usage instructions
- API endpoints (for APIs)
- Dependencies
- Deployment guidelines

## Contributing

When adding new presentation layer projects:
1. Follow the established naming convention: `PortaleFatture.BE.{ProjectName}`
2. Create a comprehensive README.md in the project folder
3. Update this recap README
4. Document all environment variables and secrets
5. Include Swagger/OpenAPI for HTTP APIs
6. Implement proper error handling and logging
7. Add health check endpoints where applicable

## Support

For issues or questions related to specific projects, refer to the individual project README files or contact the development team.
