import type { AppSessionData } from "./session";

// express-openid-connect's own types declare req.oidc but not req.appSession (its shape is
// whatever afterCallback returns, so it's left untyped by the library) — see src/auth/index.ts.
declare global {
  namespace Express {
    interface Request {
      appSession?: AppSessionData;
    }
  }
}

export {};
