import { Router } from "express";
import { approveStaff, listPendingApprovals } from "../apiClient";
import { requireActiveRole, requireAuthenticated } from "../middleware/requireRole";
import { generateToken } from "../security/csrf";

export const staffRouter = Router();

staffRouter.get(
  "/staff/pending-approvals",
  requireAuthenticated(),
  requireActiveRole("Admin"),
  async (req, res) => {
    const session = req.appSession!;
    const pending = await listPendingApprovals(session.personId, session.roles);
    res.render("staff/pending-approvals.njk", { pending, csrfToken: generateToken(req, res) });
  },
);

staffRouter.post(
  "/staff/pending-approvals/:personId/approve",
  requireAuthenticated(),
  requireActiveRole("Admin"),
  async (req, res) => {
    const session = req.appSession!;
    await approveStaff(session.personId, session.roles, req.params.personId, req.body.role);
    res.redirect("/staff/pending-approvals");
  },
);
