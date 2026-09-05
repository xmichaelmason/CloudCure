import { doubleCsrf } from "csrf-csrf";

/**
 * Double-submit-cookie CSRF protection. Node owns the session cookie (see src/auth), so this
 * is Node's responsibility, not the .NET API's. The session identifier binds a token to the
 * signed-in person; forms rendered before login don't currently need CSRF protection since
 * every state-changing POST route requires authentication first.
 */
export const { generateToken, doubleCsrfProtection } = doubleCsrf({
  getSecret: () => requireEnv("CSRF_SECRET"),
  getSessionIdentifier: (req) => req.appSession?.personId ?? "anonymous",
  // Our forms are plain HTML posts carrying a hidden `_csrf` field, not an XHR/htmx request
  // setting a header — the library's default token retriever only checks the header.
  getTokenFromRequest: (req) => req.body?._csrf,
  cookieOptions: {
    sameSite: "lax",
    // Secure cookies are never sent back over plain HTTP — true in real deployments (behind
    // TLS), but must be false for local dev/test or the cookie silently never round-trips.
    secure: process.env.NODE_ENV === "production",
  },
});

function requireEnv(name: string): string {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Missing required environment variable: ${name}`);
  }
  return value;
}
