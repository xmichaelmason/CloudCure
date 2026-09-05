import jwt from "jsonwebtoken";

// Must match CloudCure.Api.Auth.InternalJwt on the .NET side exactly.
export const INTERNAL_JWT_ISSUER = "cloudcure-web";
export const INTERNAL_JWT_AUDIENCE = "cloudcure-api";

/**
 * Mints a fresh, short-lived internal service-to-service JWT for one outgoing API request.
 * Short-lived by design (60s) so a token leaked from a log/container has a tiny window of
 * use, unlike a long-lived credential. Never Auth0's own tokens — this is Node vouching for
 * a caller (or, before login resolves an identity, for itself) to the internal API.
 */
export function mintInternalToken(personId?: string, roles: string[] = []): string {
  const secret = process.env.INTERNAL_JWT_SECRET;
  if (!secret) {
    throw new Error("Missing required environment variable: INTERNAL_JWT_SECRET");
  }

  const claims: Record<string, unknown> = {};
  if (personId) {
    claims.sub = personId;
  }
  if (roles.length > 0) {
    claims.role = roles;
  }

  return jwt.sign(claims, secret, {
    issuer: INTERNAL_JWT_ISSUER,
    audience: INTERNAL_JWT_AUDIENCE,
    expiresIn: "60s",
  });
}
