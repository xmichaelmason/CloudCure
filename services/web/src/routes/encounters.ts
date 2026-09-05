import { Router } from "express";
import {
  ApiError,
  assignDoctor,
  createEncounter,
  finalizeDiagnosis,
  getBodyRegions,
  getEncounter,
  recordAssessment,
  recordVitals,
  searchStaff,
} from "../apiClient";
import { requireAuthenticated, requireActiveRole, requireStaff } from "../middleware/requireRole";
import { generateToken } from "../security/csrf";

export const encountersRouter = Router();

function asNumberArray(value: unknown): number[] {
  const arr = Array.isArray(value) ? value : value ? [value] : [];
  return arr.map(Number).filter((n) => !Number.isNaN(n));
}

encountersRouter.post("/patients/:patientId/encounters", requireAuthenticated(), requireStaff(), async (req, res) => {
  const session = req.appSession!;
  const created = await createEncounter(session.personId, session.roles, req.params.patientId);
  res.redirect(`/encounters/${created.encounterId}/vitals`);
});

// /report is the same view — the print stylesheet (.no-print / @media print in site.css)
// already turns it into a clean printable page, so no separate template is needed.
encountersRouter.get(["/encounters/:encounterId", "/encounters/:encounterId/report"], requireAuthenticated(), requireStaff(), async (req, res) => {
  const session = req.appSession!;
  try {
    const encounter = await getEncounter(session.personId, session.roles, req.params.encounterId);
    res.render("encounters/show.njk", { encounter });
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) {
      res.status(404).render("errors/not-found");
      return;
    }
    throw err;
  }
});

encountersRouter.get("/encounters/:encounterId/vitals", requireAuthenticated(), requireStaff(), async (req, res) => {
  const session = req.appSession!;
  const encounter = await getEncounter(session.personId, session.roles, req.params.encounterId);
  if (encounter.stage !== "VitalsPending") {
    res.redirect(`/encounters/${req.params.encounterId}`);
    return;
  }
  res.render("encounters/vitals.njk", { encounter, csrfToken: generateToken(req, res) });
});

encountersRouter.post("/encounters/:encounterId/vitals", requireAuthenticated(), requireStaff(), async (req, res) => {
  const session = req.appSession!;
  const body = req.body;

  try {
    await recordVitals(session.personId, session.roles, req.params.encounterId, {
      systolicMmHg: Number(body.systolicMmHg),
      diastolicMmHg: Number(body.diastolicMmHg),
      oxygenSaturationPct: Number(body.oxygenSaturationPct),
      heartRateBpm: Number(body.heartRateBpm),
      respiratoryRateBpm: Number(body.respiratoryRateBpm),
      temperatureValue: Number(body.temperatureValue),
      temperatureUnit: body.temperatureUnit,
      heightValue: Number(body.heightValue),
      heightUnit: body.heightUnit,
      weightValue: Number(body.weightValue),
      weightUnit: body.weightUnit,
    });
    res.redirect(`/encounters/${req.params.encounterId}/assessment`);
  } catch (err) {
    const message = err instanceof ApiError ? err.message : "Something went wrong. Please try again.";
    const encounter = await getEncounter(session.personId, session.roles, req.params.encounterId);
    res.status(err instanceof ApiError ? err.status : 500).render("encounters/vitals.njk", {
      encounter,
      csrfToken: generateToken(req, res),
      error: message,
      form: body,
    });
  }
});

encountersRouter.get("/encounters/:encounterId/assessment", requireAuthenticated(), requireStaff(), async (req, res) => {
  const session = req.appSession!;
  const encounter = await getEncounter(session.personId, session.roles, req.params.encounterId);
  if (encounter.stage !== "AssessmentPending") {
    res.redirect(`/encounters/${req.params.encounterId}`);
    return;
  }
  const bodyRegions = await getBodyRegions(session.personId, session.roles);
  const groups = groupByRegion(bodyRegions);
  res.render("encounters/assessment.njk", { encounter, groups, csrfToken: generateToken(req, res) });
});

encountersRouter.post("/encounters/:encounterId/assessment", requireAuthenticated(), requireStaff(), async (req, res) => {
  const session = req.appSession!;
  const body = req.body;

  try {
    await recordAssessment(session.personId, session.roles, req.params.encounterId, {
      chiefComplaint: body.chiefComplaint,
      historyOfPresentIllness: body.historyOfPresentIllness,
      painScale: Number(body.painScale),
      painBodyRegionIds: asNumberArray(body.painBodyRegionIds),
    });
    res.redirect(`/encounters/${req.params.encounterId}`);
  } catch (err) {
    const message = err instanceof ApiError ? err.message : "Something went wrong. Please try again.";
    const encounter = await getEncounter(session.personId, session.roles, req.params.encounterId);
    const bodyRegions = await getBodyRegions(session.personId, session.roles);
    res.status(err instanceof ApiError ? err.status : 500).render("encounters/assessment.njk", {
      encounter,
      groups: groupByRegion(bodyRegions),
      csrfToken: generateToken(req, res),
      error: message,
      form: body,
    });
  }
});

encountersRouter.get(
  "/encounters/:encounterId/diagnosis",
  requireAuthenticated(),
  requireActiveRole("Doctor"),
  async (req, res) => {
    const session = req.appSession!;
    const encounter = await getEncounter(session.personId, session.roles, req.params.encounterId);
    if (encounter.stage !== "AwaitingDoctor") {
      res.redirect(`/encounters/${req.params.encounterId}`);
      return;
    }
    res.render("encounters/diagnosis.njk", { encounter, csrfToken: generateToken(req, res) });
  },
);

encountersRouter.post(
  "/encounters/:encounterId/diagnosis",
  requireAuthenticated(),
  requireActiveRole("Doctor"),
  async (req, res) => {
    const session = req.appSession!;
    const body = req.body;

    try {
      await finalizeDiagnosis(session.personId, session.roles, req.params.encounterId, body.doctorDiagnosis, body.recommendedTreatment);
      res.redirect(`/encounters/${req.params.encounterId}`);
    } catch (err) {
      const message = err instanceof ApiError ? err.message : "Something went wrong. Please try again.";
      const encounter = await getEncounter(session.personId, session.roles, req.params.encounterId);
      res.status(err instanceof ApiError ? err.status : 500).render("encounters/diagnosis.njk", {
        encounter,
        csrfToken: generateToken(req, res),
        error: message,
        form: body,
      });
    }
  },
);

encountersRouter.get(
  "/patients/:patientId/encounters/:encounterId/assign-doctor",
  requireAuthenticated(),
  requireStaff(),
  async (req, res) => {
    const session = req.appSession!;
    const q = typeof req.query.q === "string" ? req.query.q : undefined;
    const doctors = await searchStaff(session.personId, session.roles, { role: "Doctor", q });
    res.render("encounters/assign-doctor.njk", {
      patientId: req.params.patientId,
      encounterId: req.params.encounterId,
      doctors,
      q: q ?? "",
      csrfToken: generateToken(req, res),
    });
  },
);

encountersRouter.post(
  "/patients/:patientId/encounters/:encounterId/assign-doctor",
  requireAuthenticated(),
  requireStaff(),
  async (req, res) => {
    const session = req.appSession!;
    await assignDoctor(session.personId, session.roles, req.params.encounterId, Number(req.body.staffMemberId));
    res.redirect(`/encounters/${req.params.encounterId}`);
  },
);

function groupByRegion<T extends { regionGroup: string }>(items: T[]): Record<string, T[]> {
  return items.reduce<Record<string, T[]>>((groups, item) => {
    (groups[item.regionGroup] ??= []).push(item);
    return groups;
  }, {});
}
