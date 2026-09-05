import path from "path";
import cookieParser from "cookie-parser";
import express from "express";
import helmet from "helmet";
import nunjucks from "nunjucks";
import { createAuthMiddleware } from "./auth";
import { doubleCsrfProtection } from "./security/csrf";
import { homeRouter } from "./routes/home";
import { accountRouter } from "./routes/account";
import { staffRouter } from "./routes/staff";
import { patientsRouter } from "./routes/patients";
import { screeningsRouter } from "./routes/screenings";
import { encountersRouter } from "./routes/encounters";

export interface CreateAppOptions {
  /** Overridable for tests, which can't drive a real Auth0 authorization-code flow. */
  authMiddleware?: express.RequestHandler;
}

export function createApp(options: CreateAppOptions = {}): express.Express {
  const app = express();

  app.use(helmet());
  app.use(cookieParser());
  app.use(express.urlencoded({ extended: true }));
  app.use(express.json());
  app.use(express.static(path.join(__dirname, "public")));

  nunjucks.configure(path.join(__dirname, "views"), { autoescape: true, express: app });
  app.set("view engine", "njk");

  app.get("/healthz", (_req, res) => {
    res.status(200).json({ status: "ok" });
  });

  // Auth middleware populates req.oidc / req.appSession for everything below it.
  app.use(options.authMiddleware ?? createAuthMiddleware());

  app.use((req, res, next) => {
    res.locals.isAuthenticated = req.oidc.isAuthenticated();
    res.locals.currentPerson = req.appSession ?? null;
    next();
  });

  app.use(doubleCsrfProtection);

  app.use(homeRouter);
  app.use(accountRouter);
  app.use(staffRouter);
  app.use(patientsRouter);
  app.use(screeningsRouter);
  app.use(encountersRouter);

  return app;
}

if (require.main === module) {
  const port = process.env.PORT ? Number(process.env.PORT) : 3000;
  const app = createApp();
  app.listen(port, () => {
    // eslint-disable-next-line no-console
    console.log(`CloudCure web BFF listening on port ${port}`);
  });
}
