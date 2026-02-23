# PortaleFatture.BE.SelfCareOnBoardingAPI

Minimal Web API for simulating SelfCare onboarding endpoints during development and testing.

## Overview

This is a lightweight .NET 8.0 Web API that provides mock endpoints for SelfCare onboarding verification and status updates. It's designed for development, testing, and integration scenarios where the actual SelfCare service is not available or not needed.

## Technology Stack

- **.NET 8.0**: Target framework
- **ASP.NET Core Minimal API**: Lightweight API framework
- **Swagger/OpenAPI**: API documentation (6.6.2)

## Purpose

This mock API simulates SelfCare integration endpoints for:
- Testing onboarding workflows
- Development without external dependencies
- Integration testing
- Demo environments

## API Endpoints

### 1. Recipient Code Verification

**GET** `/v2/institutions/onboarding/recipientCode/verification`

Verifies if a recipient code (SDI code) is valid for onboarding.

**Query Parameters**:
- `originId`: Institution origin ID (Codice IPA)
- `recipientCode`: Recipient code (Codice SDI)

**Headers**:
- `Authorization`: Bearer token (required)

**Responses**:

- `200 OK`: Returns verification status
  - `"ACCEPTED"`: Code is accepted
  - `"DENIED_NO_BILLING"`: Denied - no billing available
  - `"DENIED_NO_ASSOCIATION"`: Denied - no association found

- `401 Unauthorized`: Missing or invalid authorization token
  ```json
  {
    "detail": "Authorization token is missing or invalid.",
    "instance": "/v2/institutions/onboarding/{recipientCode}/verification",
    "invalidParams": [
      {
        "name": "Authorization",
        "reason": "Bearer token is required but not provided."
      }
    ],
    "status": 401,
    "title": "Unauthorized",
    "type": "https://httpstatuses.com/401"
  }
  ```

**Test Codes**:
- `DENIED_NO_BILLING`: Returns denial for no billing
- `DENIED_NO_ASSOCIATION`: Returns denial for no association
- Any other code: Returns `ACCEPTED`

### 2. Onboarding Update

**PUT** `/onboarding/{onboardingId}/update`

Updates the status of an onboarding request.

**Path Parameters**:
- `onboardingId`: Onboarding request ID

**Query Parameters**:
- `status`: New status value

**Headers**:
- `Authorization`: Bearer token (required)

**Request Body**:
```json
{
  // OnboardingUpdateRequest object
}
```

**Responses**:
- `200 OK`: Update successful
- `401 Unauthorized`: Missing or invalid authorization token

## Project Structure

```
PortaleFatture.BE.SelfCareOnBoardingAPI/
├── Dto/
│   └── OnboardingUpdateRequest.cs  # Request DTOs
├── Program.cs                       # API definition
├── appsettings.json                # Configuration
├── appsettings.Development.json    # Dev configuration
└── PortaleFatture.BE.SelfCareOnBoardingAPI.csproj
```

## Configuration

### appsettings.json

Configure environment-specific settings in `appsettings.json` or `appsettings.Development.json`.

## Running the Application

### Development Mode

Run with Swagger UI enabled:

```bash
dotnet run
```

Access the API at:
- API: `http://localhost:5000` (or configured port)
- Swagger: `http://localhost:5000/swagger`

### Production Mode

In production, HTTPS redirection is enabled:

```bash
dotnet run --environment Production
```

## Swagger/OpenAPI

Swagger UI is available in development mode at `/swagger`:
- Interactive API documentation
- Test endpoints directly from browser
- View request/response schemas

## Authentication

The API requires a Bearer token in the Authorization header:

```
Authorization: Bearer <token>
```

**Note**: In this mock implementation, any non-empty token is accepted. The actual token value is logged to console but not validated.

## Console Logging

The API logs all requests to the console:

**Verification Endpoint**:
```
originId: {codiceIPA}
recipientCode: {codiceSDI}
authtoken: {bearerToken}
```

**Update Endpoint**:
```
onboardingId: {onboardingId}
status: {status}
authtoken: {bearerToken}
body request: {serialized JSON}
```

## Dependencies

### NuGet Packages
- `Swashbuckle.AspNetCore`: 6.6.2

## Usage Examples

### Verify Recipient Code (Accepted)

```bash
curl -X GET "http://localhost:5000/v2/institutions/onboarding/recipientCode/verification?originId=IPA123&recipientCode=ABC123" \
  -H "Authorization: Bearer test-token"
```

Response: `"ACCEPTED"`

### Verify Recipient Code (Denied - No Billing)

```bash
curl -X GET "http://localhost:5000/v2/institutions/onboarding/recipientCode/verification?originId=IPA123&recipientCode=DENIED_NO_BILLING" \
  -H "Authorization: Bearer test-token"
```

Response: `"DENIED_NO_BILLING"`

### Update Onboarding Status

```bash
curl -X PUT "http://localhost:5000/onboarding/12345/update?status=COMPLETED" \
  -H "Authorization: Bearer test-token" \
  -H "Content-Type: application/json" \
  -d '{}'
```

Response: `200 OK`

## Integration

This mock API is designed to be used by:
- `PortaleFatture.BE.Api`: Main API during development
- Integration tests
- Frontend applications during testing
- Demo environments

## Environment Modes

### Development
- Swagger UI enabled
- Console logging enabled
- No HTTPS redirection

### Production
- Swagger UI disabled
- HTTPS redirection enabled
- Minimal logging

## Security Considerations

**Important**: This is a mock API for development/testing only:
- No actual authentication validation
- No data persistence
- No business logic enforcement
- Should not be used in production

## Error Handling

The API includes basic error handling:
- 401 responses for missing authorization
- Structured error responses following RFC 7807
- Console error logging

## Response Format

Error responses follow the Problem Details format (RFC 7807):
- `type`: URI reference to error type
- `title`: Short, human-readable summary
- `status`: HTTP status code
- `detail`: Detailed explanation
- `instance`: URI reference to specific occurrence
- `invalidParams`: Array of invalid parameter details

## Testing

Use this API to test:
- Onboarding verification flows
- Different denial scenarios
- Authorization handling
- Error responses
- Status update workflows

## Customization

To add more test scenarios:
1. Add new recipient codes in the verification endpoint
2. Implement custom response logic
3. Add logging as needed
4. Extend DTOs for additional fields

## Limitations

- No database persistence
- No actual SelfCare integration
- No complex business rules
- Token validation is minimal
- Stateless (no session management)

## When to Use

Use this mock API when:
- Developing without SelfCare access
- Running integration tests
- Creating demo environments
- Testing error scenarios
- Isolating components

## When Not to Use

Do not use in:
- Production environments
- Security testing
- Performance testing
- Load testing
- Actual onboarding processes

## Deployment

This mock API can be deployed as:
- Docker container
- Azure App Service
- Local development server
- Test environment service

## Monitoring

Monitor via:
- Console logs
- Application logs
- HTTP access logs

## Future Enhancements

Potential improvements:
- In-memory data storage
- Configurable test scenarios
- Request/response recording
- Delayed responses for testing
- Additional SelfCare endpoints
