import { RequestHandler } from "express";
import type { AppSessionData } from "../src/types/session";

/**
 * Stands in for the real Auth0 middleware in route tests, which can't drive a live
 * authorization-code flow. Mimics just the surface routes actually use: req.oidc.isAuthenticated(),
 * res.oidc.login(), and req.appSession.
 */
export function fakeAuthMiddleware(session: AppSessionData | null): RequestHandler {
  return (req, res, next) => {
    req.oidc = {
      isAuthenticated: () => session !== null,
    } as unknown as typeof req.oidc;

    res.oidc = {
      login: (opts?: { returnTo?: string }) => res.redirect(`/login-stub?returnTo=${opts?.returnTo ?? ""}`),
    } as unknown as typeof res.oidc;

    if (session) {
      req.appSession = session;
    }

    next();
  };
}
