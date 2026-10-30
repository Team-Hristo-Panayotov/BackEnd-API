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
