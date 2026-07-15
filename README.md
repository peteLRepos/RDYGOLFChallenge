# Golf Club Booking System

A small public-facing booking system for a golf club: members/guests can browse bookable resources
(tee times, driving range bays, lesson slots, simulators), book a time slot with up to four named
players, add a golf cart from the club's fleet, join an in-progress booking, and manage their own
bookings. Admins manage resources, the cart fleet, and every booking through a separate admin app.

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
- **Resources**: 6/9/18-hole tee-time courses (10-minute slots, priced), two driving range bays, a
  lesson slot with a pro (linked to the 6-Hole Course — see "Lessons and Simulators" below), and
  three independent simulator bays
- **Carts**: a 3-cart fleet ("Cart 1"/"Cart 2"/"Cart 3"), managed separately from Resources (see
  "Using it" below)
- **Users**: an admin account (`admin@testAdmin.com` / `Admin`) and two demo members
  (`alice@example.com` / `bob@example.com`, both password `Password123`) — passwords are hashed with
  ASP.NET Core's `PasswordHasher`, never stored plain, even for these seed accounts

To stop everything: `make stop` (or `docker compose down`). `make clean` also removes the Postgres
volume if you want a fully fresh database.

## Using it

**Booking site** (`booking-web`): browse courses on the home page, pick a day (can't go before
today), and click a time slot. An open slot opens a booking dialog with four player slots — you're
auto-filled into the first one, and you can search-and-add up to three more players, each with their
own Cash/Card payment method. You can also add a golf cart (+€30, one per booking) — the checkbox
live-checks the fleet's availability for that slot's 2-hour cart window and greys itself out with
"No carts available" if none are free. A slot that's already booked but not full shows how many
players and their combined handicap (never who they are) and lets you join it the same way (without
a cart option — see "Assumptions" below). The combined handicap is capped at 120 — go over it and
Confirm disables itself. If a slot is genuinely full (4/4), an **"Add me to queue"** button appears
next to it instead — see "Waitlist" below. **My Bookings** lists everything you've created or joined,
showing its attached cart if it has one: cancel a booking you created (once it's unpaid), unbook
just yourself from one you joined, or remove an individual guest you added without cancelling the
whole thing. A Check-in button appears within 15 minutes of a booking's start time.

**Lessons and Simulators**: the "Lesson with Pro" resource is linked to the 6-Hole Course — booking
either one for a given hour automatically blocks (and is blocked by) the other for that same hour,
so a lesson never gets interrupted by a tee-time booking on the same ground. The three Simulator
resources otherwise book like anything else, except the dialog replaces the fixed slot length with a
duration picker (1–5 hours) and drops the cart option (no cart use for simulator sessions) — price is
a flat €20/player regardless of how many hours are booked.

**Waitlist**: once a slot is completely full, any user can join its queue via "Add me to queue" on
the tee sheet. Whenever a seat there frees up — someone cancels their whole booking, or is removed
from a partial one — the longest-waiting queued user is automatically added as a player (or given a
fresh booking, if the whole thing was cancelled), using the same rules as a normal join (combined
handicap cap included — an entry that doesn't fit an opening stays queued for the next one, it isn't
dropped). There's no notification when this happens; the user only finds out by checking **My
Bookings** or the tee sheet again. Admins see every resource's queue on a dedicated **Waitlist** tab
and can remove any entry.

**Admin panel** (`admin-web`): login-gated before anything else loads. Browse every resource
(including inactive ones, which the public site hides), edit a course's price per player, and open
its tee sheet to see every slot's payment status (PAID/NOT PAID) alongside player count and handicap.
A Ready booking (checked in) renders green and locked. Clicking an open slot lets an admin book on
behalf of any user — whoever's placed in the first slot becomes that booking's owner, not the admin.
Clicking an existing booking shows the full roster, its attached cart (if any), and offers a Cancel
action that works regardless of who created it, plus a Mark as paid action for anything still unpaid.
A separate **Carts** tab manages the fleet itself: add a cart by name, disable one
(keeps its booking history but stops it being offered), or remove one entirely (blocked if it's ever
been linked to a booking — disable it instead). The **Waitlist** tab lists every queued entry across
every resource (who, for which slot, since when) with a Remove action.

Both apps share a JWT-based auth model: `POST /api/auth/login` / `POST /api/users` (register) return
a token, sent as `Authorization: Bearer <token>` on subsequent requests. The admin app additionally
checks the `isAdmin` claim client-side before accepting a login, on top of the API's own
`[Authorize(Roles = "Admin")]` enforcement.

## Assumptions and trade-offs

- **No timezones.** Booking times are stored and compared as naive local timestamps (no `DateTimeOffset`/UTC
  conversion) representing the club's own local time. This means the API container's OS timezone must
  match the club's — set via the `TZ` environment variable on the `api` service in `docker-compose.yml`
  (defaults to `Europe/Helsinki`; change it for a different location). Fine for a single-club,
  single-timezone booking sheet; would need proper `DateTimeOffset`/UTC handling for a multi-location product.
- **Booking overlap check is a query, not a DB constraint.** `BookingService` checks for overlapping
  bookings before inserting, inside a single request — there's a small race-condition window under
  concurrent requests for the same slot. A production version would add a Postgres exclusion
  constraint (`EXCLUDE USING gist`) on `(ResourceId, tsrange(Start, End))` as a hard backstop. `User.Email`
  uniqueness has the same race-condition shape, backstopped by a unique DB index — unlike the booking
  case, the backstop being hit is handled gracefully (translated into a friendly "email already
  exists" error instead of a raw 500).
- **The last remaining admin can't be demoted or deactivated** — there's no "break glass" recovery
  path otherwise, since there'd be no way to log back in as admin to undo it. Enforced with a Postgres
  advisory lock around the read-check-write sequence so two concurrent requests can't both slip past
  the check before either commits.
- **Resource availability slots are fixed-width**, generated from each resource's configured slot
  duration and operating hours — no support for custom/ad-hoc time ranges.
- **Forgot-password has no email delivery.** `POST /api/users/forgot-password` always returns `200`
  (never a `404` that would reveal whether an email is registered), but when the account *does*
  exist, the new plaintext password is returned directly in the response body for the UI to display —
  there's no email step in scope. This means knowing a member's email is enough to take over their
  account via the UI, even though the API itself no longer leaks account existence through its status
  code. Acceptable for this take-home's scope; a real version would send the new password (or a
  one-time reset link) to the email address instead of ever displaying it.
- **The public player-search endpoint (`GET /api/users/search`) returns each match's handicap**,
  unlike the rest of that endpoint's deliberately minimal response (no email, admin status, or active
  flag). The booking dialog needs it client-side to validate the 120 combined-handicap cap and grey
  out Confirm *before* submitting, not only after a round trip — a deliberate, narrow exception, not
  an oversight.
- **Admin bookings skip the "caller must be the first player" rule** that the public create endpoint
  enforces (`POST /api/admin/bookings` vs. `POST /api/bookings`) — an admin booking on someone else's
  behalf is never the booking's owner themselves, by design.
- **Carts are a separate `Cart` entity, not a `Resource`** — a cart has no time-sheet of its own
  (no opening/closing hours or slot duration), it's pooled interchangeable inventory. A cart
  reservation is a fixed 2-hour block starting at the *booking's* start time (`Cart.ReservationHours`),
  independent of the underlying resource's own slot length — a 10-minute tee time still holds a cart
  for a full round. Availability is computed by checking which carts have an overlapping,
  non-cancelled booking, the same way tee-time slot availability works — there's no stored "booked"
  flag to get out of sync, so cancelling a booking frees its cart automatically.
- **Only the original booker can attach a cart, and only at booking creation.** Joining someone else's
  in-progress booking never offers a cart option — the booker already decided that when they created
  it. `POST/DELETE /api/bookings/{id}/cart` exist for adding/removing a cart on an already-created
  booking, but there's no UI for it yet (only wired into the create dialog).
- **A cart can't be deleted once any booking has ever referenced it**, even a cancelled one —
  `CartService.DeleteAsync` refuses and tells the admin to disable it instead, so booking history
  never loses its cart reference. Backed by a `Restrict` FK at the DB level as well.
- **Moving a booking with a cart attached doesn't re-validate cart availability at the new time** —
  `AdminBookingsController`'s existing Move endpoint changes `Start`/`End` but never re-checks whether
  the booking's cart is still free for the shifted 2-hour window, so two bookings could end up
  claiming the same cart. Not addressed in this scope (see "what I'd do next").
- **Lesson↔6-Hole-Course blocking is computed live, the same way cart availability is** — a
  `Resource.LinkedResourceId` self-reference, resolved on every availability/overlap check rather than
  a stored flag, so it can never drift out of sync. The admin tee sheet doesn't yet render this
  block visually the way the public booking site does (an "Unavailable" slot with no player info) —
  it still shows as a normal open slot to an admin, though the backend still rejects a conflicting
  booking attempt on submit either way.
- **A cancelled booking only re-offers its own slot start to the waitlist, not every slot it
  spanned.** For most resources that's the same thing, but a cancelled multi-hour Simulator booking
  frees several consecutive hourly slots at once — only the first hour's queue gets a chance to
  fill it automatically; the rest just show as open again. A deliberate simplification rather than
  walking the whole freed range.
- **Waitlist fulfillment has no notification step.** When a queued user is automatically added to a
  booking, nothing tells them it happened — they find out by checking My Bookings or the tee sheet
  again. A real version would need an email/push step the same way a booking confirmation would (see
  "what I'd do next").
- **No automated tests were added for anything built after the initial backend foundation** — an
  explicit choice partway through this project to move faster on remaining features, made together
  with whoever's reviewing this. The pre-existing suite (`Domain`/`Application` unit tests, ~130 tests)
  was kept green throughout and is still run before every commit; nothing new was added on top of it,
  and there's no frontend test coverage at all.

## Out of scope in this take-home — how I'd set it up for production

- **Payments**: not implemented, per the assignment. Would integrate Stripe/PayPal at the point of
  booking confirmation, with a `Payment` entity linked to `Booking` and a webhook handler for
  payment-status updates. Right now "Card" payments are recorded as immediately paid and "Cash"
  payments wait for an admin to reconcile them in person — there's no UI for that reconciliation step
  yet (see "what I'd do next").
- **Production-grade authentication**: real JWT-based auth throughout — signed tokens, role claims,
  hashed passwords, no shared secrets. Still not production-hardened: `Jwt:Secret` is a plaintext dev
  value in `appsettings.json` rather than a managed secret (Key Vault/Secrets Manager), and there's no
  token refresh/revocation (a token stays valid for its full lifetime even if the user is deactivated
  mid-session — deactivation only blocks *future* logins). For production I'd also add rate limiting
  on `/api/auth/login` and `/api/users/forgot-password` to slow down credential-stuffing and
  password-reset abuse.
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
This keeps domain/application unit tests deterministic. Run them with:

```
dotnet test backend/GolfClub.sln
```

See "Assumptions and trade-offs" above for why this suite wasn't extended alongside the frontend work.

## What I'd do next with more time

- **Admin "move booking" UI** — the endpoint already exists (`POST /api/admin/bookings/{id}/move`)
  but has no button in the admin panel yet. ("Mark as paid" now does — the booking dialog shows the
  attached cart, if any, and a Mark as paid action for an unpaid booking.)
- **Admin user management screen** — `GET/PATCH /api/admin/users` (list, deactivate, promote/demote)
  is fully built but unused by `admin-web`, which only has the resources/bookings views.
- **Resource creation and activate/deactivate from the UI** — `POST /api/admin/resources` and
  `PATCH /api/admin/resources/{id}/active` both exist, but the admin panel only wires up viewing and
  editing price; adding a new resource or toggling one off currently needs a direct API call.
- **Admin tee sheet showing lesson↔6-hole-course blocks visually**, and **email notification when a
  waitlist entry is fulfilled**, both noted above as known gaps.
- **Editing an existing booking's roster from the admin dialog** — right now admin can create a new
  booking or cancel an existing one, but can't add/remove players from a booking that's already in
  progress the way the public "join"/"unbook" flow lets a regular user do for their own bookings.
- **A UI for adding/removing a cart after booking creation** — `POST/DELETE /api/bookings/{id}/cart`
  already exist, but only the create dialog's checkbox uses them; My Bookings and the admin booking
  dialog have no cart controls yet.
- **Re-validate cart availability when an admin moves a booking**, closing the gap noted above.
- A real payment-reconciliation flow for cash bookings, noted above as explicitly deferred.
- **Automated test coverage** for everything built after the initial handicap/payment foundation —
  the booking-player join/leave rules, the admin-create path, the whole cart feature, and all of the
  frontend — plus API-level integration tests, neither of which exist today.
- The Postgres exclusion constraint mentioned above, to close the booking-overlap race condition
  properly (the same race exists for cart reservations too, at much lower odds given the fleet is
  usually larger than one), and a real email delivery step for forgot-password instead of displaying
  it in the UI.
- A mobile app (React Native) reusing the same API, once the web experience is solid — the assignment
  only asked for a React frontend, so this was intentionally out of scope for now.
