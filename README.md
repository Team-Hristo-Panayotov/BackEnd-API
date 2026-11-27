# MathMatura – Back-End API

ASP.NET Core Web API (.NET 10).

## Structure

```
MathMaturaProject.slnx
MathMaturaProject.API/
├── Controllers/   API endpoints
├── Models/        Domain / database entities
├── DTOs/          Request and response objects
├── Services/      Business logic
├── Data/          DbContext and data access
├── Middleware/    Custom middleware (errors, logging, ...)
├── Properties/    launchSettings.json
├── appsettings.json
└── Program.cs
```

## Run locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project MathMaturaProject.API
```

The API listens on `http://localhost:5051` (and `https://localhost:7191` with the `https` profile).

- Health check: `GET http://localhost:5051/api/health`
- OpenAPI document (Development): `GET http://localhost:5051/openapi/v1.json`

## Login and register

The API signs users in with a token (a JWT). The frontend sends it back as `Authorization: Bearer <token>`.

### One-time setup

The token signing key is a secret, so it is not in `appsettings.json`. Without it the API refuses to start. Set it once per machine:

```bash
dotnet user-secrets set "Jwt:Key" "<at least 32 random characters>" --project MathMaturaProject.API
```

On a server, use the environment variable `Jwt__Key` instead. Then create the tables:

```bash
dotnet ef database update --project MathMaturaProject.Data --startup-project MathMaturaProject.API
```

This needs `dotnet-ef` 10 (`dotnet tool update -g dotnet-ef --version 10.0.1`) and SQL Server 2025 or Azure SQL, because some columns use the `json` type.

### Endpoints

| Call | Success | Errors |
|---|---|---|
| `POST /api/auth/register` | 201 | 400 `VALIDATION_FAILED`, 409 `EMAIL_TAKEN` |
| `POST /api/auth/login` | 200 | 400 `VALIDATION_FAILED`, 401 `INVALID_LOGIN` or `ACCOUNT_LOCKED` |
| `GET /api/auth/me` | 200 | 401 when the token is missing, wrong or expired |

Register and login reply with `{ accessToken, expiresAt, user: { userId, email, displayName, role } }`. New users always get the role `User`. Five wrong passwords in a row lock an account for 15 minutes. Try the calls in `MathMaturaProject.API.http`.

### Settings

| Setting | Default | Meaning |
|---|---|---|
| `Jwt:Key` | none | Signing key. User-secrets or `Jwt__Key` only. |
| `Jwt:Issuer`, `Jwt:Audience` | `MathMatura.API`, `MathMatura.Client` | Names written into each token and checked on the way back. |
| `Jwt:ExpiresMinutes` | 60 | How long a token works. |
| `Cors:AllowedOrigins` | `http://localhost:5173` in Development | Websites allowed to call the API from a browser. |

## Feedback box

`POST /api/feedback` saves a message from the feedback box. No login is needed. When the request carries a token, the message is linked to that user.

```json
{ "type": "Problem", "title": "Грешка в урок 3", "content": "Отговорът на задача 5 е грешен." }
```

| Field | Rule | Error codes |
|---|---|---|
| `type` | `Problem` or `Suggestion`, any letter case | `TYPE_REQUIRED`, `TYPE_INVALID` |
| `title` | 10 to 200 characters | `TITLE_REQUIRED`, `TITLE_TOO_SHORT`, `TITLE_TOO_LONG` |
| `content` | 10 to 1000 characters | `CONTENT_REQUIRED`, `CONTENT_TOO_SHORT`, `CONTENT_TOO_LONG` |

Spaces at the ends are removed and not counted. Success is `201` with `id`, `type`, `title`, `content` and `createdAt`. Errors are `400 VALIDATION_FAILED`, with the codes grouped by field.

## Tests

```bash
dotnet test
```

The tests start the API in memory on SQLite, so they need no SQL Server.
