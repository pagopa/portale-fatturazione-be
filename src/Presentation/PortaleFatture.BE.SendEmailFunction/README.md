# PortaleFatture.BE.SendEmailFunction

Azure Functions application for sending emails, creating REL righe (invoice line items), and managing notification workflows using Durable Functions.

## Overview

This Azure Functions v4 application built on .NET 8.0 provides email sending capabilities (both SMTP and PSP Gmail), REL righe creation, alert notifications, and module commission forecasts. It uses Durable Functions for orchestration and long-running workflows.

## Technology Stack

- **.NET 8.0**: Target framework
- **Azure Functions v4**: Serverless platform
- **Durable Functions**: Workflow orchestration (1.2.3)
- **Azure WebJobs Extensions**: Background job processing
- **User Secrets**: Secure configuration storage
- **SMTP & Gmail API**: Email delivery

## Function Types

### Core Functions

1. **SendEmail**: General email sending via SMTP
2. **SendEmailPsp**: PSP email sending via Gmail API
3. **SendAlert**: Alert notification sending
4. **CreateRelRighe**: REL righe (invoice line) creation
5. **ModuloCommessaPrevisionale**: Commission module forecasting
6. **RichiestaNotifiche**: Notification requests handling

### Orchestrators

Located in `Orchestrators/` folder for managing complex, multi-step workflows.

## Project Structure

```
PortaleFatture.BE.SendEmailFunction/
├── Infrastructure/
│   └── Documenti/
│       ├── pagoPA/
│       │   └── email_psp.html       # PSP email template
│       ├── primo_saldo.html         # First payment template
│       ├── secondo_saldo.html       # Second payment template
│       └── var_semestrale.html      # Semi-annual variation template
├── Handlers/                        # Function handlers
├── Models/                          # DTOs and models
├── Orchestrators/                   # Durable orchestrations
├── CreateRelRighe.cs               # REL righe function
├── ModuloCommessaPrevisionale.cs   # Commission forecast
├── RichiestaNotifiche.cs           # Notification requests
├── SendAlert.cs                    # Alert sending
├── SendEmail.cs                    # SMTP email function
├── SendEmailPsp.cs                 # PSP Gmail function
├── Program.cs                      # Entry point
├── host.json                       # Function host config
└── local.settings.json             # Local settings
```

## Email Templates

HTML templates for different email types:

### PSP Emails
- `pagoPA/email_psp.html`: Payment Service Provider notifications

### REL Notifications
- `primo_saldo.html`: First payment period notifications
- `secondo_saldo.html`: Second payment period notifications
- `var_semestrale.html`: Semi-annual variation reports

## Configuration

### Local Settings

Configure `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

### User Secrets

Store sensitive credentials in user secrets:
- SMTP configuration
- Gmail API credentials
- Database connection strings
- API keys

## Functions Overview

### SendEmail
HTTP-triggered or queue-triggered function for sending emails via SMTP.

**Features**:
- SMTP-based delivery
- Template rendering
- Attachment support
- Delivery tracking

### SendEmailPsp
Specialized function for PSP email delivery via Gmail API.

**Features**:
- OAuth2 Gmail authentication
- Google API integration
- PSP-specific templates
- Sent folder organization

### SendAlert
Alert notification system for urgent communications.

**Features**:
- Priority messaging
- Multiple recipient support
- Alert categorization
- Delivery confirmation

### CreateRelRighe
Function for creating invoice line items (REL righe).

**Features**:
- Batch processing
- Data validation
- Database integration
- Error handling

### ModuloCommessaPrevisionale
Commission module forecasting and planning.

**Features**:
- Forecast calculations
- Data aggregation
- Reporting generation

### RichiestaNotifiche
Notification request processing and routing.

**Features**:
- Request validation
- Routing logic
- Status tracking
- Callback handling

## Durable Functions

The application supports durable orchestrations for:
- Multi-step email workflows
- Retry logic for failed sends
- State persistence
- Long-running notification processes
- Sub-orchestrations for complex scenarios

## Running Locally

1. Install Azure Functions Core Tools

2. Configure `local.settings.json` with required settings

3. Set up user secrets for sensitive data

4. Start the function app:

```bash
func start
```

5. Functions will be available at:
```
http://localhost:7071/api/{function-name}
```

## Email Sending

### SMTP Configuration
- Server address
- Port (typically 25, 587, or 465)
- Authentication credentials
- TLS/SSL settings

### Gmail PSP Configuration
- OAuth2 credentials
- Access token
- Refresh token
- Client ID and secret
- Application name

## Dependencies

### NuGet Packages

- `Microsoft.Azure.Functions.Worker`: 2.0.0
- `Microsoft.Azure.Functions.Worker.Extensions.DurableTask`: 1.2.3
- `Microsoft.Azure.Functions.Worker.Extensions.Http`: 3.3.0
- `Microsoft.Azure.Functions.Worker.Sdk`: 2.0.2
- `Microsoft.Azure.WebJobs.Extensions`: 5.0.0
- `Microsoft.Azure.WebJobs.Extensions.DurableTask`: 3.0.4

### Project References

- `PortaleFatture.BE.Infrastructure`: Infrastructure layer
- `PortaleFatture.BE.Api`: Shared API components

## Host Configuration

The `host.json` file configures:
- Durable task settings
- HTTP routing
- Logging levels
- Extension bundles

## Deployment

Deploy to Azure using:

```bash
func azure functionapp publish <function-app-name>
```

Or use:
- Visual Studio publish
- Azure DevOps pipeline
- GitHub Actions
- Azure CLI

## Integration

This function app integrates with:
- Main Web API for email requests
- Database for email tracking
- Azure Storage queues for message processing
- Gmail API for PSP emails
- SMTP servers for general emails

## Triggers

Functions can be triggered by:
- HTTP requests
- Azure Storage queues
- Timer schedules
- Event Grid events
- Service Bus messages

## Monitoring

Monitor functions through:
- Azure Application Insights
- Function execution logs
- Durable Function status queries
- Custom telemetry

## Error Handling

The application includes:
- Retry policies for transient failures
- Dead letter queues for failed messages
- Exception logging
- Alert notifications on critical failures

## Security

- User secrets for local development
- Azure Key Vault for production secrets
- Managed identities for Azure resources
- Secure credential storage
- TLS/SSL for email transmission

## Template Management

HTML templates are:
- Stored in `Infrastructure/Documenti/`
- Copied to output directory
- Rendered with dynamic data
- Support for localization

## Email Tracking

Email delivery is tracked with:
- Send timestamp
- Recipient information
- Success/failure status
- Error messages
- Retry attempts

## Orchestrator Patterns

Common patterns used:
- Function chaining
- Fan-out/fan-in
- Async HTTP APIs
- Human interaction
- Monitoring and alerting

## Testing

Test functions locally using:
- Postman/curl for HTTP triggers
- Azure Storage Explorer for queue messages
- Durable Functions monitor
- Unit tests for business logic

## Performance

Optimize performance with:
- Parallel execution
- Batch processing
- Connection pooling
- Async/await patterns
- Efficient querying

## Scaling

Azure Functions automatically scales based on:
- Queue length
- HTTP request volume
- Execution duration
- Resource utilization

## Best Practices

1. Use durable functions for long-running workflows
2. Implement idempotency for retry scenarios
3. Store secrets in Key Vault
4. Monitor execution costs
5. Use Application Insights for diagnostics
6. Implement proper error handling
7. Version your functions
8. Document triggers and bindings
