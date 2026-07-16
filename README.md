# Golf Club Booking System

A small public-facing booking system for a golf club: members/guests browse bookable resources (tee
times, driving range bays, lesson slots, simulators), book a slot with up to 4 players, add a golf
cart, join an in-progress booking or a waitlist, and manage their own bookings. Admins manage
resources, carts, the waitlist, and every booking through a separate admin app.

**Stack**: C#/.NET 8 (Clean Architecture) · PostgreSQL/EF Core · two React + TypeScript (Vite) apps
(public site + admin panel) · Docker Compose.

## How to run it

Prerequisites: Docker (and Compose, bundled with Docker Desktop) — nothing else.

```
make run
```

If `make` isn't available: `docker compose up --build`.

| Service       | URL                    | Notes                                  |
|---------------|-------------------------|------------------------------------------|
| `api`         | http://localhost:5000    | Swagger UI at `/swagger` in Development |
| `booking-web` | http://localhost:3000    | Public booking site                     |
| `admin-web`   | http://localhost:3001    | Admin panel                             |

Migrations and demo data seed automatically on first startup. Admin login: `admin@testAdmin.com` /
`Admin`. Demo members: `alice@example.com` / `bob@example.com`, password `Password123`.

`make stop` to stop; `make clean` also wipes the Postgres volume for a fresh database.

## Assumptions and trade-offs

- **No timezones** — bookings are naive local timestamps, not UTC. The API container's OS timezone
  must match the club's (`TZ` in `docker-compose.yml`).
- **Booking/cart/lesson-block overlap checks are queries, not DB constraints** — a small race-condition
  window exists under concurrent requests for the same slot. A production version would add a Postgres
  exclusion constraint as a hard backstop.
- **The last remaining admin can't be demoted or deactivated** — there'd be no way back into the admin
  panel otherwise.
- **Forgot-password has no email delivery** — the new password is shown directly in the UI instead of
  emailed, since email is out of scope. Means knowing a member's email is enough to take over their
  account.
- **No automated tests were added for anything built after the initial backend foundation** — an
  explicit choice to move faster on remaining features. The original suite (~130 tests) stayed green
  throughout; there's no frontend or integration test coverage.

## Out of scope — how I'd set it up for production

- **Payments**: would integrate Stripe/PayPal with a `Payment` entity and webhook handler. Cash
  bookings currently have no reconciliation UI.
- **Auth**: JWT works end-to-end, but the signing secret is a plaintext dev value, not a managed
  secret, and there's no token refresh/revocation or login rate limiting.
- **Deployment**: currently Docker Compose only. Production would run the API/frontends as separate
  images, Postgres as a managed service, frontends behind a CDN.
- **CI/CD**: none yet — would add a GitHub Actions workflow to test/build on PRs and deploy on merge.

## What I'd do next

- Admin UI for what's already built on the backend: move booking, user management, resource
  create/activate, editing an existing booking's roster, cart controls after creation.
- Automated test coverage for everything built after the initial foundation, plus integration tests.
- The Postgres exclusion constraint and a real email step for forgot-password, mentioned above.
- A mobile app (React Native) reusing the same API — the assignment only asked for React, so this
  was intentionally out of scope.
