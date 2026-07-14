# frontend-booking

The public booking site (React + TypeScript + Vite). See the [repo root README](../README.md) for
what this app does, how to run the whole stack, and the project's assumptions/trade-offs.

## Local dev (outside Docker)

```
npm install
npm run dev
```

Requires the API running separately (see the root README) and `VITE_API_URL` in `.env.local`
pointing at it (defaults to `http://localhost:5000`, matching the Docker Compose setup).
