import { Router } from "express";

export const homeRouter = Router();

homeRouter.get("/", (_req, res) => {
  res.render("home.njk");
});
