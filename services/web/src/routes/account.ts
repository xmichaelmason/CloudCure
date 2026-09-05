import { Router } from "express";
import { ApiError, registerStaff } from "../apiClient";
import { requireAuthenticated } from "../middleware/requireRole";
import { generateToken } from "../security/csrf";

export const accountRouter = Router();

accountRouter.get("/account/register", requireAuthenticated(), (req, res) => {
  res.render("account/register.njk", { csrfToken: generateToken(req, res) });
});

accountRouter.post("/account/register", requireAuthenticated(), async (req, res) => {
  const session = req.appSession!;
  const { workEmail, specialization, startDate, roomNumber, educationDegree } = req.body;

  try {
    await registerStaff(session.personId, session.roles, {
      workEmail,
      specialization,
      startDate,
      roomNumber: roomNumber || undefined,
      educationDegree,
    });
    res.redirect("/account/pending-approval");
  } catch (err) {
    const message = err instanceof ApiError ? err.message : "Something went wrong. Please try again.";
    res.status(err instanceof ApiError ? err.status : 500).render("account/register.njk", {
      csrfToken: generateToken(req, res),
      error: message,
    });
  }
});

accountRouter.get("/account/pending-approval", requireAuthenticated(), (_req, res) => {
  res.render("account/pending-approval.njk");
});
