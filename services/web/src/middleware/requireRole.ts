import { RequestHandler } from "express";

/**
 * Presentation-layer guards only — the real enforcement point is the .NET API's own
 * [Authorize] policies, since Node just carries whatever roles identity/resolve last reported.
 * These exist for UX (redirect to a sensible page) rather than as the security boundary.
 */
export function requireAuthenticated(): RequestHandler {
  return (req, res, next) => {
    if (!req.oidc.isAuthenticated() || !req.appSession) {
      res.oidc.login({ returnTo: req.originalUrl });
      return;
    }
    next();
  };
}

export function requireActiveRole(...allowed: string[]): RequestHandler {
  return (req, res, next) => {
    const session = req.appSession;
    if (!session || session.status !== "Active" || !session.roles.some((role) => allowed.includes(role))) {
      res.status(403).render("errors/forbidden");
      return;
    }
    next();
  };
}

export function requireStaff(): RequestHandler {
  return requireActiveRole("Nurse", "Doctor", "Admin");
}
