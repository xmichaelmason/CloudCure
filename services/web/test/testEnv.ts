/**
 * Sets the environment variables createApp()/createAuthMiddleware() require at construction
 * time, so route tests can build a real Express app without a live Auth0 tenant. Import this
 * before importing anything from src/server or src/auth.
 */
export function setTestEnv(): void {
  process.env.AUTH0_SECRET = "test-auth0-secret-at-least-32-characters-long";
  process.env.AUTH0_BASE_URL = "http://localhost:3000";
  process.env.AUTH0_CLIENT_ID = "test-client-id";
  process.env.AUTH0_CLIENT_SECRET = "test-client-secret";
  process.env.AUTH0_DOMAIN = "test-tenant.us.auth0.com";
  process.env.INTERNAL_JWT_SECRET = "test-internal-jwt-secret-at-least-32-characters-long";
  process.env.CSRF_SECRET = "test-csrf-secret-at-least-32-characters-long";
}
