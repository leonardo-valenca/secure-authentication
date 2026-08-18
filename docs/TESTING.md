# Running and validating this project locally

A practical guide for running the test suites and the full stack on your own machine — Visual Studio for the API, VS Code for the Angular app, Docker Compose for everything together.

No credentials or secrets are written down here on purpose — every command below either generates a throwaway value on the spot or points at `.env.example`, never a real `.env`.

## Prerequisites

- .NET SDK matching `src/backend/global.json` (`10.0.400` or a later patch — `rollForward: latestPatch` accepts newer patches automatically).
- Node 22.x (see `src/frontend/package.json` engines / CI config).
- Docker Desktop, running, for anything that touches `Api.IntegrationTests` or `docker compose`.

## Backend tests, in Visual Studio

1. Open `src/backend/SecureAuthentication.slnx`.
2. **Test → Test Explorer → Run All Tests**. Test discovery needs nothing extra — `xunit.runner.visualstudio` is already referenced in every test project.
3. Right-click any test class or method to run or debug just that one.

Three test projects, three different levels:

| Project | What it covers | Needs Docker? |
|---|---|---|
| `Domain.Tests` | Value objects, entities — pure unit tests | No |
| `Application.Tests` | Command handlers, mocked dependencies (NSubstitute) | No |
| `Api.IntegrationTests` | Real HTTP flow via `WebApplicationFactory`, against **real SQL Server and Redis containers** (Testcontainers) | **Yes** — Docker Desktop must already be running before these start, or the containers fail to spin up |

Command-line equivalent (useful outside the IDE, or to match what CI runs):

```bash
cd src/backend
dotnet test SecureAuthentication.slnx
```

## Frontend tests, in VS Code

The Angular test builder here is Vitest (`@angular/build:unit-test`) — no separate Karma config to manage.

```bash
cd src/frontend
npm install          # first time only
npm test             # or: npx ng test --watch=false
```

Drop `--watch=false` to keep it running while you edit. If you want inline pass/fail markers in the editor, the [Vitest VS Code extension](https://marketplace.visualstudio.com/items?itemName=vitest.explorer) works out of the box since it's already Vitest underneath.

## Running the API directly (Visual Studio / `dotnet run`)

Running the `Api` project (F5 in Visual Studio, or `dotnet run` from `src/backend/Api`) now opens Scalar's interactive API reference automatically at `/scalar/v1` — both the `http` and `https` launch profiles have `launchBrowser` wired to it (`Properties/launchSettings.json`). This only works in the `Development` environment (the profiles already set `ASPNETCORE_ENVIRONMENT=Development`); it's intentionally not exposed when running via Docker Compose's Production build.

Running this way needs a real reachable SQL Server — either `dotnet user-secrets set "ConnectionStrings:Database" "..."` pointed at one you have locally, or use Docker Compose instead (below), which handles that for you.

## Running everything with Docker Compose

From the repo root:

```bash
cp .env.example .env
```

Then open `.env` and fill in real values for `DB_PASSWORD`, `REDIS_PASSWORD`, and `JWT_SIGNING_KEY` — generate each with:

```bash
openssl rand -base64 24   # DB_PASSWORD, REDIS_PASSWORD
openssl rand -base64 32   # JWT_SIGNING_KEY
```

Leave `SMTP_HOST` blank to skip real email setup entirely — confirmation/reset links get logged to `docker compose logs api` instead of sent.

```bash
dotnet dev-certs https --export-path .certs/localhost.pem --format Pem --no-password   # once per session
docker compose up --build
```

This brings up SQL Server, Redis, the API, and Caddy (reverse proxy + TLS) behind one origin: **https://localhost**.

Useful commands while it's running:

```bash
docker compose logs -f api      # tail API logs (also where confirmation links show up without SMTP configured)
docker compose down -v          # tear down, including the database volume
```

## Validating edge cases, not just the happy path

Three layers, cheapest to most realistic:

1. **The automated suite** (above) already exercises the edge cases that matter most here: refresh-token reuse detection, account lockout after 5 failed logins, CSRF rejection, rate-limit 429s, JWT validation across a rotated signing key, 2FA with real TOTP codes, and the Redis-backed distributed rate limiter checked against real Redis keys, not just the HTTP response.
2. **Scalar's interactive docs** (`/scalar/v1`, opens automatically per above) — a live, clickable API explorer for trying individual endpoints and payloads by hand while running the API directly.
3. **A full flow through Docker Compose** — the same kind of check the CI smoke-test job runs: register → grab the confirmation link from `docker compose logs api` → confirm → login → hit `/me`. To exercise specific edge cases by hand:
   - Hammer any auth endpoint past its limit and confirm it 429s.
   - Log in with the wrong password 5 times and confirm the account locks out.
   - Enable 2FA (`/2fa/setup` → `/2fa/enable`) and confirm a subsequent login now requires the TOTP code before issuing a session.
   - Run `scripts/backup-db.sh`, change some data, then `scripts/restore-db.sh` and confirm it actually reverted.
   - `docker compose exec api curl -s http://localhost:8080/metrics` to see live Prometheus-format metrics from inside the network (it's deliberately unreachable from outside, unlike `/alive`/`/ready` which Caddy does route publicly).
