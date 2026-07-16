# Golf Club Booking System

A small public-facing booking system for a golf club: members/guests can browse bookable resources
(tee times, driving range bays, lesson slots, simulators), book a time slot with up to four named
players, add a golf cart, join an in-progress booking, join a waitlist when a slot is full, and
manage their own bookings. Admins manage resources, the cart fleet, the waitlist, and every booking
through a separate admin app.

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

If `make` isn't available, run the equivalent directly: `docker compose up --build`.

| Service       | URL                     | Notes                                   |
|---------------|--------------------------|------------------------------------------|
| `postgres`    | localhost:5432            | Data persisted in a named Docker volume  |
| `api`         | http://localhost:5000     | Swagger UI at `/swagger` in Development  |
| `booking-web` | http://localhost:3000     | Public booking site                      |
| `admin-web`   | http://localhost:3001     | Admin panel                              |

Migrations and demo data (resources, a 3-cart fleet, an admin account, and two demo members) seed
automatically on first startup — no manual DB setup. Admin login: `admin@testAdmin.com` / `Admin`.
Demo members: `alice@example.com` / `bob@example.com`, both password `Password123`.

To stop: `make stop`. `make clean` also wipes the Postgres volume for a fully fresh database.

## Using it

**Booking site**: browse courses, pick a day, click a time slot to book (up to 4 named players, an
optional golf cart) or join one that's already started. A full slot offers a waitlist join instead —
when a seat frees up, the longest-waiting queued user is added automatically. Lessons automatically
block (and are blocked by) the matching hour on the 6-Hole Course; Simulators use a duration picker
(1–5 hours, flat per-player price) instead of a fixed slot and don't offer a cart. **My Bookings**
lists everything you've created or joined, with cancel/unbook/remove-guest actions.

**Admin panel**: login-gated. Browse and price every resource (including inactive ones), manage the
cart fleet and the waitlist, and open a resource's tee sheet to book on anyone's behalf, cancel any
booking, or mark one paid.

Both apps share JWT-based auth (`POST /api/auth/login` / `POST /api/users` to register); the admin
app additionally checks the `isAdmin` claim client-side on top of the API's own role enforcement.

## Assumptions and trade-offs

- **No timezones** — bookings are stored/compared as naive local timestamps, not UTC. The API
  container's OS timezone must match the club's (`TZ` in `docker-compose.yml`, defaults to
  `Europe/Helsinki`). Fine for a single-club, single-timezone product.
- **Booking overlap checks (and cart/lesson-block checks) are queries, not DB constraints** — a small
  race-condition window exists under concurrent requests for the same slot. `User.Email` uniqueness
  has the same shape but is backstopped by a DB unique index. A production version would add a
  Postgres exclusion constraint as a hard backstop for the booking case too.
- **The last remaining admin can't be demoted or deactivated** — there's no other way back into the
  admin panel if that happened. Guarded with a Postgres advisory lock against the same race.
- **Forgot-password has no email delivery** — the new password is returned directly in the API
  response and shown in the UI, since there's no email step in scope. This means knowing a member's
  email is enough to take over their account. Acceptable for this take-home; a real version would
  email a reset link instead of ever displaying the password.
- **No automated tests were added for anything built after the initial backend foundation** — an
  explicit choice made partway through to move faster on remaining features. The original suite
  (`Domain`/`Application` unit tests, ~130 tests) was kept green throughout; there's no frontend test
  coverage or API-level integration tests.
- **Waitlist fulfillment has no notification step**, and a cancelled multi-hour Simulator booking only
  re-offers its first hour to the queue, not the whole range it spanned — both deliberate
  simplifications rather than gaps in the underlying design.

## Out of scope in this take-home — how I'd set it up for production

- **Payments**: not implemented, per the assignment. Would integrate Stripe/PayPal at booking
  confirmation with a `Payment` entity and a webhook handler. Cash bookings currently have no
  reconciliation UI.
- **Production-grade authentication**: JWT auth works end-to-end, but `Jwt:Secret` is a plaintext dev
  value rather than a managed secret, and there's no token refresh/revocation. Would also add rate
  limiting on login/forgot-password.
- **Deployment**: currently local-only via Docker Compose. Production would run the API and both
  frontends as separate deployable images, Postgres as a managed service, and the frontends behind a
  CDN.
- **CI/CD**: no pipeline yet. Would add a GitHub Actions workflow to test/build on PRs and deploy on
  merge, with migrations as a separate step rather than running on API startup.

## Testing

`Booking`'s "now" is injected via `IDateTimeProvider` rather than read from the system clock, keeping
domain/application unit tests deterministic. Run them with:

```
dotnet test backend/GolfClub.sln
```

## What I'd do next with more time

- **Admin UI for what's already built on the backend**: moving a booking, user management
  (list/deactivate/promote), resource create/activate toggle, editing an existing booking's roster,
  and cart controls after booking creation — all have working endpoints with no admin panel button
  yet.
- **Close the two documented gaps above**: the admin tee sheet doesn't visually show lesson↔6-hole
  blocks, and waitlist fulfillment has no notification.
- **Automated test coverage** for everything built after the initial foundation, plus API-level
  integration tests — neither exists today.
- The Postgres exclusion constraint mentioned above, and a real email delivery step for
  forgot-password.
- A mobile app (React Native) reusing the same API, once the web experience is solid — the assignment
  only asked for a React frontend, so this was intentionally out of scope for now.
