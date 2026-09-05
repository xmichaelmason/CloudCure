import { Router } from "express";
import { ApiError, createPatient, getPatient, listPatients } from "../apiClient";
import { requireAuthenticated, requireStaff } from "../middleware/requireRole";
import { generateToken } from "../security/csrf";

export const patientsRouter = Router();

function asStringArray(value: unknown): string[] {
  if (Array.isArray(value)) {
    return value.map(String);
  }
  if (typeof value === "string" && value.length > 0) {
    return [value];
  }
  return [];
}

patientsRouter.get("/patients", requireAuthenticated(), requireStaff(), async (req, res) => {
  const session = req.appSession!;
  const q = typeof req.query.q === "string" ? req.query.q : undefined;
  const page = req.query.page ? Number(req.query.page) : 1;

  const result = await listPatients(session.personId, session.roles, { q, page });
  res.render("patients/index.njk", { result, q: q ?? "" });
});

patientsRouter.get("/patients/new", requireAuthenticated(), requireStaff(), (req, res) => {
  res.render("patients/new.njk", { csrfToken: generateToken(req, res) });
});

patientsRouter.post("/patients", requireAuthenticated(), requireStaff(), async (req, res) => {
  const session = req.appSession!;
  const body = req.body;

  try {
    const created = await createPatient(session.personId, session.roles, {
      firstName: body.firstName,
      lastName: body.lastName,
      dateOfBirth: body.dateOfBirth,
      phone: body.phone,
      email: body.email || undefined,
      addressLine1: body.addressLine1 || undefined,
      addressLine2: body.addressLine2 || undefined,
      city: body.city || undefined,
      stateProvince: body.stateProvince || undefined,
      postalCode: body.postalCode || undefined,
      countryCode: body.countryCode || undefined,
      emergencyContactName: body.emergencyContactName || undefined,
      emergencyContactPhone: body.emergencyContactPhone || undefined,
      allergies: asStringArray(body.allergies),
      conditions: asStringArray(body.conditions),
      medications: asStringArray(body.medications),
      surgeries: asStringArray(body.surgeries),
    });

    res.redirect(`/patients/${created.patientId}`);
  } catch (err) {
    const message = err instanceof ApiError ? err.message : "Something went wrong. Please try again.";
    res.status(err instanceof ApiError ? err.status : 500).render("patients/new.njk", {
      csrfToken: generateToken(req, res),
      error: message,
      form: {
        ...body,
        allergies: asStringArray(body.allergies),
        conditions: asStringArray(body.conditions),
        medications: asStringArray(body.medications),
        surgeries: asStringArray(body.surgeries),
      },
    });
  }
});

patientsRouter.get("/patients/:patientId", requireAuthenticated(), requireStaff(), async (req, res) => {
  const session = req.appSession!;

  try {
    const patient = await getPatient(session.personId, session.roles, req.params.patientId);
    res.render("patients/show.njk", { patient, csrfToken: generateToken(req, res) });
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) {
      res.status(404).render("errors/not-found");
      return;
    }
    throw err;
  }
});
