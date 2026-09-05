# Getting CloudCure running locally

This walks through getting the full stack up on your machine, setting up Auth0, and
creating your first working account.

## What you need installed

- **Docker** and **Docker Compose** (running `docker compose version` should work)
- **.NET 10 SDK** — needed to run database migrations from your machine
- **Node.js** — only needed if you want to run `services/web` outside of Docker, or run
  its test suite

## 1. Clone the repo and set up your environment file

```bash
cd v2
cp .env.example .env
```

Open `.env` and fill in real values. A few of these matter more than others right now:

- `INTERNAL_JWT_SECRET`, `CSRF_SECRET`, `AUTH0_SECRET` — any long random string works
  for local development. Don't reuse these in a real deployment.
- `AUTH0_DOMAIN`, `AUTH0_CLIENT_ID`, `AUTH0_CLIENT_SECRET` — see the next section.

## 2. Set up an Auth0 application

CloudCure uses Auth0 to handle login. You'll need your own Auth0 tenant (a free one is
fine for development):

1. Go to [auth0.com](https://auth0.com/) and create an account, then a new tenant.
2. Create an **Application** of type **Regular Web Application** (not "Single Page
   App" — the login session lives on the server, in `services/web`, not in the
   browser).
3. In the application's settings, set:
   - **Allowed Callback URLs**: `http://localhost:3000/callback`
   - **Allowed Logout URLs**: `http://localhost:3000`
4. Copy the **Domain**, **Client ID**, and **Client Secret** from that page into your
   `.env` file as `AUTH0_DOMAIN`, `AUTH0_CLIENT_ID`, and `AUTH0_CLIENT_SECRET`.

## 3. Start everything

```bash
docker compose up -d
```

This starts four containers: `postgres`, `api`, `web`, and `observability`
(OpenObserve, for logs and traces). Check they're all healthy:

```bash
docker compose ps
```

## 4. Apply the database migrations

The containers come up with an empty database — migrations need to be applied
separately, from your machine:

```bash
cd services/api
dotnet tool restore
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=cloudcure_v2;Username=cloudcure;Password=cloudcure"
dotnet ef database update --project src/CloudCure.Infrastructure --startup-project src/CloudCure.Api
```

(The connection string here points at the `postgres` container's port, published to
your machine by `docker-compose.override.yml`.)

## 5. Open the app and log in

Visit **http://localhost:3000**. Click log in, go through Auth0, and you'll land back
on a page that says your account is waiting for approval. That's expected — every new
account starts with no role until an admin grants one.

## 6. Create your first admin

The very first admin account has nobody to approve them, so this one step has to be
done by hand, directly in the database:

```bash
docker exec -it $(docker compose ps -q postgres) psql -U cloudcure -d cloudcure_v2
```

Find your person by email, then grant yourself Admin:

```sql
-- 1 = Pending, 2 = Nurse, 3 = Doctor, 4 = Admin
-- 1 = Pending, 2 = Active, 3 = Revoked
UPDATE person_roles
SET role_id = 4, status = 2
WHERE person_id = (SELECT id FROM people WHERE email = 'you@example.com');
```

Log out and back in on the site, and you should now see the admin pages. From here,
you can approve any other staff account (nurses, doctors) through the normal
pending-approvals page in the app — the manual database step above is only ever needed
once, for the first admin.

## Running the tests

```bash
# .NET — spins up real, temporary Postgres containers per test class
cd services/api
dotnet test

# Node
cd services/web
npm install
npm test
```

## Troubleshooting

- **Callback URL mismatch from Auth0** — double check the callback/logout URLs in your
  Auth0 application settings exactly match `http://localhost:3000/callback` and
  `http://localhost:3000`, including no trailing slash mismatches.
- **`api` container is unhealthy** — check `docker compose logs api`. The most common
  cause locally is `postgres` not being ready yet; it should self-resolve within a few
  seconds, since `api` waits on Postgres's own health check.
- **Migrations fail to connect** — make sure `postgres`'s port is actually published to
  your machine (it's exposed via `docker-compose.override.yml`, which is only applied
  automatically when you run plain `docker compose` commands from the `v2/` directory).
- **Everything is stuck in "pending approval"** — that's correct until an admin exists.
  See step 6.
