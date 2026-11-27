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

## Authentication (cookie sessions)

Login state is kept in a server-side session; the browser only holds an opaque token in a cookie.

| Endpoint | Auth | Description |
| --- | --- | --- |
| `POST /api/auth/register` | public | Create an account and sign in (sets the cookie) |
| `POST /api/auth/login` | public | Verify credentials and sign in (sets the cookie) |
| `GET /api/auth/me` | cookie | Return the signed-in user's profile (call on app startup) |
| `POST /api/auth/logout` | public | Delete the server session and expire the cookie |

- HSTS is enabled outside Development, so browsers refuse plain-http requests to the API.
- Cookie `mm_session`: `HttpOnly`, `Secure` (HTTPS; only relaxed for plain-http Development), `SameSite=Lax`, `Max-Age` 14 days.
- The token is 256 random bits; only its SHA-256 hash is stored (`UserSessions` table). The cookie holds no PII.
- Protect any endpoint with `[Authorize]`; requests without a valid session get `401`.
- Settings live under `Session` (`CookieName`, `LifetimeDays` 1-30, `SameSite` Lax/Strict) in `appsettings.json`.
- Browser frontends on another origin must list their origin in `Cors:AllowedOrigins` and send requests with
  `credentials: 'include'` (fetch) / `withCredentials: true` (axios).

Apply the database changes with:

```bash
dotnet ef database update --project MathMaturaProject.Data --startup-project MathMaturaProject.API
```
