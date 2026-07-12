# Golf Club Booking System

A small public-facing booking system for a golf club: members/guests can browse bookable resources
(tee times, driving range bays, golf carts, lesson slots, simulators) and book a time slot; admins
can manage resources and bookings through a separate admin app.

## Stack

- **Backend**: C#/.NET 8 Web API, Clean Architecture (`Domain` → `Application` → `Infrastructure`/`Api`)
- **Database**: PostgreSQL via EF Core (Npgsql), code-first migrations, auto-applied on startup
- **Frontend**: two separate React + TypeScript (Vite) apps — a public booking site and an admin panel
- **Containerization**: Docker Compose, one command from a clean checkout

## How to run it

Prerequisites: Docker (and Docker Compose, bundled with Docker Desktop). Nothing else — no local
.NET SDK, Node, or Postgres install required.

```
make run
```

If `make` isn't available on your machine, run the equivalent directly:

```
docker compose up --build
```

This builds and starts four containers:

| Service       | URL                     | Notes                                   |
|---------------|--------------------------|------------------------------------------|
| `postgres`    | localhost:5432            | Data persisted in a named Docker volume  |
| `api`         | http://localhost:5000     | Swagger UI at `/swagger` in Development  |
| `booking-web` | http://localhost:3000     | Public booking site                      |
| `admin-web`   | http://localhost:3001     | Admin panel                              |

The API automatically applies EF Core migrations and seeds demo data on first startup — no manual DB
setup:
- Resources: a tee time, two driving range bays, two golf carts, a lesson slot, and a simulator bay
- Users: an admin account (`admin@testAdmin.com` / `Admin`) and two demo members
  (`alice@example.com` / `bob@example.com`, both password `Password123`) — passwords are hashed with
  ASP.NET Core's `PasswordHasher`, never stored plain, even for these seed accounts

The admin app is gated by a shared key (not real authentication — see below). For local/demo use,
enter `dev-admin-key` in the admin app's "Admin key" field. This is configured via `Admin:ApiKey` in
`backend/src/GolfClub.Api/appsettings.json`.

To stop everything: `make stop` (or `docker compose down`). `make clean` also removes the Postgres
volume if you want a fully fresh database.

## Assumptions and trade-offs

- **No timezones.** Booking times are stored and compared as naive local timestamps (no `DateTimeOffset`/UTC
  conversion) representing the club's own local time. This means the API container's OS timezone must
  match the club's — set via the `TZ` environment variable on the `api` service in `docker-compose.yml`
  (defaults to `Europe/Helsinki`; change it for a different location). Fine for a single-club,
  single-timezone booking sheet; would need proper `DateTimeOffset`/UTC handling for a multi-location product.
- **Booking overlap check is a query, not a DB constraint.** `BookingService.CreateAsync` checks for
  overlapping bookings before inserting, inside a single request — there's a small race-condition window
  under concurrent requests for the same slot. A production version would add a Postgres exclusion
  constraint (`EXCLUDE USING gist`) on `(ResourceId, tsrange(Start, End))` as a hard backstop.
- **Booking cancellation has no ownership check.** Any booking ID can be cancelled via the public
  `DELETE /api/bookings/{id}` endpoint. A real version would require the customer's email or a
  confirmation token to prove ownership.
- **Resource availability slots are fixed-width**, generated from each resource's configured slot
  duration and operating hours — no support for custom/ad-hoc time ranges.

## Out of scope in this take-home — how I'd set it up for production

- **Payments**: not implemented, per the assignment. Would integrate Stripe/PayPal at the point of
  booking confirmation, with a `Payment` entity linked to `Booking` and a webhook handler for
  payment-status updates.
- **Real authentication/authorization**: the admin app currently uses a single shared API key
  (`X-Admin-Key` header) checked by `AdminAuthFilter` — good enough to gate the admin API for a
  take-home, not for production. I'd replace this with ASP.NET Core Identity or an external IdP
  (Auth0/Entra ID) issuing JWTs, per-admin accounts, and role-based policies instead of a shared secret.
- **Deployment**: currently local-only via Docker Compose. Production would run the API and both
  frontends as separate deployable images (e.g. Azure Container Apps/ECS), Postgres as a managed
  service (Azure Database for PostgreSQL/RDS) rather than a container, and the frontends behind a
  CDN rather than nginx-in-a-container.
- **CI/CD**: no pipeline yet. Would add a GitHub Actions workflow to run `dotnet test`/`npm run build`
  on PRs, build and push images on merge to `main`, and run migrations as a separate deploy step
  rather than on API startup (auto-migrating on boot is convenient for this take-home but risky in
  production with multiple API replicas starting concurrently).

## Testing

`Booking`'s "now" (used for the past-time check and `CreatedAt`) is injected via `IDateTimeProvider`
rather than read from `DateTime.Now` internally — the domain entity takes it as a constructor
parameter, and `SystemDateTimeProvider` (Infrastructure) supplies the real clock at runtime via DI.
This keeps domain/application unit tests deterministic: tests pass a fixed `DateTime` instead of
depending on when the suite happens to run. Run them with:

```
dotnet test backend/GolfClub.sln
```

## What I'd do next with more time

- More test coverage: `ResourceService` and API-level integration tests (domain invariants and
  `BookingService` are covered; `ResourceService` and the controllers/middleware aren't yet).
- A proper booking UI: calendar/date picker and slot grid on the booking site, rather than a flat list.
- Admin resource create/edit forms and booking cancellation from the admin UI (currently read-only
  beyond what the API supports directly).
- The Postgres exclusion constraint mentioned above, to close the overlap race condition properly.
- A mobile app (React Native) reusing the same API, once the web experience is solid — the assignment
  only asked for a React frontend, so this was intentionally out of scope for now.
