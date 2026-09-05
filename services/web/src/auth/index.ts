import { auth, ConfigParams } from "express-openid-connect";
import jwt from "jsonwebtoken";
import { resolveIdentity } from "../apiClient";
import type { AppSessionData } from "../types/session";

/**
 * Runs after Auth0 validates the login and hands back tokens, before the session cookie is
 * written. Resolves (or provisions) the internal Person for this Auth0 subject and merges
 * `{ personId, roles, status }` into what express-openid-connect stores — later requests read
 * it back via `req.appSession`. Exported standalone so it's unit-testable without a real Auth0
 * round-trip (see test/unit/auth.test.ts).
 */
export async function extendSessionWithIdentity<TSession extends { id_token: string }>(
  session: TSession,
): Promise<TSession & AppSessionData> {
  const idTokenClaims = jwt.decode(session.id_token) as Record<string, unknown> | null;
  if (!idTokenClaims || typeof idTokenClaims.sub !== "string") {
    throw new Error("Auth0 callback did not include a decodable id_token with a subject.");
  }

  const auth0Subject = idTokenClaims.sub;
  const email = typeof idTokenClaims.email === "string" ? idTokenClaims.email : undefined;
  const firstName = typeof idTokenClaims.given_name === "string"
    ? idTokenClaims.given_name
    : (email?.split("@")[0] ?? "New");
  const lastName = typeof idTokenClaims.family_name === "string" ? idTokenClaims.family_name : "User";

  // Role is never read from the Auth0 token — only from what identity/resolve (backed by the
  // API's own person_roles table) reports back. This is the direct fix for the old app's
  // self-assignable-role registration bug.
  const identity = await resolveIdentity(auth0Subject, email, firstName, lastName);

  return {
    ...session,
    personId: identity.personId,
    roles: identity.roles,
    status: identity.status,
  };
}

/**
 * Node is the only piece that ever speaks Auth0's OIDC protocol — it terminates the
 * authorization-code flow and owns the resulting session cookie (httpOnly, encrypted by
 * express-openid-connect). The browser never sees an Auth0 token directly.
 */
export function createAuthMiddleware() {
  const config: ConfigParams = {
    authRequired: false,
    auth0Logout: true,
    secret: requireEnv("AUTH0_SECRET"),
    baseURL: requireEnv("AUTH0_BASE_URL"),
    clientID: requireEnv("AUTH0_CLIENT_ID"),
    clientSecret: process.env.AUTH0_CLIENT_SECRET,
    issuerBaseURL: `https://${requireEnv("AUTH0_DOMAIN")}`,
    authorizationParams: {
      response_type: "code",
      scope: "openid profile email",
    },
    afterCallback: (_req, _res, session) => extendSessionWithIdentity(session),
  };

  return auth(config);
}

function requireEnv(name: string): string {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Missing required environment variable: ${name}`);
  }
  return value;
}
