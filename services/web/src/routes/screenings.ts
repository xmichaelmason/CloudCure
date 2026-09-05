import { Router } from "express";
import { ApiError, getScreeningTemplate, submitScreening } from "../apiClient";
import { requireAuthenticated, requireStaff } from "../middleware/requireRole";
import { generateToken } from "../security/csrf";

export const screeningsRouter = Router();

screeningsRouter.get("/patients/:patientId/screenings/new", requireAuthenticated(), requireStaff(), async (req, res) => {
  const session = req.appSession!;
  const templateCode = typeof req.query.template === "string" ? req.query.template : "covid19_v1";

  try {
    const template = await getScreeningTemplate(session.personId, session.roles, templateCode);
    res.render("screenings/new.njk", {
      template,
      patientId: req.params.patientId,
      csrfToken: generateToken(req, res),
    });
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) {
      res.status(404).render("errors/not-found");
      return;
    }
    throw err;
  }
});

screeningsRouter.post("/patients/:patientId/screenings", requireAuthenticated(), requireStaff(), async (req, res) => {
  const session = req.appSession!;
  const templateCode = req.body.templateCode as string;

  // Each yes/no question renders as `answer_<questionId>` — every question the template
  // listed must produce an answer entry, since the questionnaire's shape is entirely data
  // driven and Node never hardcodes which questions exist.
  const answers = Object.entries(req.body)
    .filter(([key]) => key.startsWith("answer_"))
    .map(([key, value]) => ({
      questionId: Number(key.replace("answer_", "")),
      answerBool: value === "yes" ? true : value === "no" ? false : undefined,
    }));

  await submitScreening(session.personId, session.roles, req.params.patientId, templateCode, answers);

  res.redirect(`/patients/${req.params.patientId}`);
});
