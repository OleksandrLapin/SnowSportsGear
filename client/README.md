# Client

This project uses Angular CLI 18.0.7, but the app is part of a full .NET + Angular solution.

## Prerequisites

- Node.js 20.11.1+ (for the client)
- .NET SDK 8 (for the API)
- Docker (for SQL Server + Redis)
- mkcert (for the HTTPS dev server)

## One-time setup

From the repo root:

1) Start infrastructure:
```
docker compose up -d
```

2) Trust the .NET dev certificate:
```
dotnet dev-certs https --trust
```

3) Generate the client HTTPS certificate:
```
cd client/ssl
mkcert localhost
```

4) Install client deps:
```
cd client
npm install
```

## Run the full app (recommended)

Terminal 1:
```
cd API
dotnet run
```

Terminal 2:
```
cd client
ng serve
```

Open:
- Client: https://localhost:4200
- API: https://localhost:5001

## Build

```
cd client
npm run build
```

The build output goes to `API/wwwroot` (see `client/angular.json`), not `dist/`.

## Tests

```
cd client
npm test
```

## Notes

- Dev API URL is configured in `client/src/environments/environment.development.ts` as `https://localhost:5001/api/`.
- If you run the client on a different host/port, update CORS in `API/Program.cs` and the `environment.*.ts` URLs.
- Stripe and Email settings live in `API/appsettings.json` and are optional for basic browsing.
