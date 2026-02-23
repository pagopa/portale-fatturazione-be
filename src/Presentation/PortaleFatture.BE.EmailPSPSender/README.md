# PortaleFatture.BE.EmailPSPSender

Console application for sending emails to PSP (Payment Service Providers) using Gmail API with OAuth2 authentication.

## Overview

This is a .NET 8.0 console application designed to send emails through Gmail's API using OAuth2 authentication. It's specifically used for PSP (Payment Service Provider) email communications in the Portale Fatturazione system.

## Technology Stack

- **.NET 8.0**: Target framework
- **Console Application**: Simple executable
- **Gmail API**: Email sending via Google's API
- **OAuth2**: Authentication mechanism
- **User Secrets**: Secure configuration storage

## Features

- OAuth2-based Gmail authentication using access and refresh tokens
- Email sending to PSP recipients
- Gmail folder management (Sent folder)
- Credential management via user secrets

## Configuration

The application uses User Secrets for secure configuration. Required settings:

- `AccessToken`: Gmail OAuth2 access token
- `RefreshToken`: Gmail OAuth2 refresh token
- `ClientId`: Google API client ID
- `ClientSecret`: Google API client secret
- `From`: Sender email address
- `FromName`: Sender display name
- `To`: Recipient email address
- `ToName`: Recipient display name

## Project Structure

```
PortaleFatture.BE.EmailPSPSender/
├── Infrastructure/
│   └── Documenti/
│       └── pagoPA/
│           └── credentials.json    # Google API credentials
├── Models/                         # Configuration models
├── Program.cs                      # Main entry point
└── PortaleFatture.BE.EmailPSPSender.csproj
```

## Usage

1. Configure user secrets with the required OAuth2 credentials
2. Place Google API credentials in `Infrastructure/Documenti/pagoPA/credentials.json`
3. Run the application:

```bash
dotnet run
```

The application will:
- Load credentials from user secrets
- Create a PSP email sender instance
- Send a test email
- Create/organize the sent email in the appropriate Gmail folder
- Display results to the console

## Dependencies

- `Microsoft.Extensions.Configuration.UserSecrets`: 8.0.1
- `PortaleFatture.BE.Infrastructure`: Infrastructure layer reference

## Email Sending

The application uses the `PspEmailSender` class which handles:
- OAuth2 token management and refresh
- Email composition and sending via Gmail API
- Sent folder organization
- Error handling and reporting

## Security

- OAuth2 credentials stored in User Secrets (not in source control)
- Google API credentials file copied to output directory
- Sensitive data never hardcoded

## Integration

This console application is part of the Portale Fatturazione ecosystem and shares infrastructure with:
- Main API (`PortaleFatture.BE.Api`)
- Infrastructure layer (`PortaleFatture.BE.Infrastructure`)

## Output

The application outputs:
- Sent folder operation results (JSON)
- Email sending status message
- Success/failure indication

Press any key to exit after execution completes.
