import request from "supertest";
import { setTestEnv } from "../testEnv";
import { fakeAuthMiddleware } from "../fakeAuthMiddleware";
import { createApp } from "../../src/server";
import * as apiClient from "../../src/apiClient";

// Preserve the real ApiError class — a plain jest.mock() automocks it too, leaving
// instances with a broken constructor (status/message end up undefined).
jest.mock("../../src/apiClient", () => ({
  ...jest.requireActual("../../src/apiClient"),
  createEncounter: jest.fn(),
  getEncounter: jest.fn(),
  recordVitals: jest.fn(),
  recordAssessment: jest.fn(),
  finalizeDiagnosis: jest.fn(),
  assignDoctor: jest.fn(),
  getBodyRegions: jest.fn(),
  searchStaff: jest.fn(),
}));

setTestEnv();

const createEncounterMock = apiClient.createEncounter as jest.MockedFunction<typeof apiClient.createEncounter>;
const getEncounterMock = apiClient.getEncounter as jest.MockedFunction<typeof apiClient.getEncounter>;
const recordVitalsMock = apiClient.recordVitals as jest.MockedFunction<typeof apiClient.recordVitals>;
const recordAssessmentMock = apiClient.recordAssessment as jest.MockedFunction<typeof apiClient.recordAssessment>;
const finalizeDiagnosisMock = apiClient.finalizeDiagnosis as jest.MockedFunction<typeof apiClient.finalizeDiagnosis>;
const assignDoctorMock = apiClient.assignDoctor as jest.MockedFunction<typeof apiClient.assignDoctor>;
const getBodyRegionsMock = apiClient.getBodyRegions as jest.MockedFunction<typeof apiClient.getBodyRegions>;
const searchStaffMock = apiClient.searchStaff as jest.MockedFunction<typeof apiClient.searchStaff>;

const nurseSession = { personId: "nurse-1", roles: ["Nurse"], status: "Active" };
const doctorSession = { personId: "doctor-1", roles: ["Doctor"], status: "Active" };

function encounterAt(stage: apiClient.EncounterDetail["stage"], overrides: Partial<apiClient.EncounterDetail> = {}): apiClient.EncounterDetail {
  return {
    encounterId: 5,
    patientId: 1,
    patientName: "Ada Lovelace",
    stage,
    attendingDoctorName: null,
    vitals: null,
    assessment: null,
    diagnosis: null,
    ...overrides,
  };
}

/**
 * Every POST route sits behind the global CSRF middleware, which needs a cookie+token pair
 * established by a prior GET. /account/register is a convenient source — it calls
 * generateToken() and needs no other apiClient mocking, and the CSRF cookie is scoped to the
 * session identifier (personId), not the route, so a token harvested there is valid for any
 * POST in the same agent/session.
 */
async function agentWithCsrfToken(session: typeof nurseSession) {
  const agent = request.agent(createApp({ authMiddleware: fakeAuthMiddleware(session) }));
  const response = await agent.get("/account/register");
  const match = response.text.match(/name="_csrf" value="([^"]+)"/);
  if (!match) {
    throw new Error("Could not find CSRF token in rendered HTML.");
  }
  return { agent, csrfToken: match[1] };
}

describe("encounter routes", () => {
  beforeEach(() => {
    createEncounterMock.mockReset();
    getEncounterMock.mockReset();
    recordVitalsMock.mockReset();
    recordAssessmentMock.mockReset();
    finalizeDiagnosisMock.mockReset();
    assignDoctorMock.mockReset();
    getBodyRegionsMock.mockReset();
    searchStaffMock.mockReset();
  });

  it("POST /patients/:id/encounters creates an encounter and redirects to vitals entry", async () => {
    createEncounterMock.mockResolvedValue({ encounterId: 5 });
    const { agent, csrfToken } = await agentWithCsrfToken(nurseSession);

    const response = await agent.post("/patients/1/encounters").type("form").send({ _csrf: csrfToken });

    expect(response.status).toBe(302);
    expect(response.headers.location).toBe("/encounters/5/vitals");
    expect(createEncounterMock).toHaveBeenCalledWith("nurse-1", ["Nurse"], "1");
  });

  it("GET vitals form redirects to the encounter page when the stage has moved past VitalsPending", async () => {
    getEncounterMock.mockResolvedValue(encounterAt("AssessmentPending"));
    const app = createApp({ authMiddleware: fakeAuthMiddleware(nurseSession) });

    const response = await request(app).get("/encounters/5/vitals");

    expect(response.status).toBe(302);
    expect(response.headers.location).toBe("/encounters/5");
  });

  it("POST vitals advances to the assessment page on success", async () => {
    recordVitalsMock.mockResolvedValue(undefined);
    const { agent, csrfToken } = await agentWithCsrfToken(nurseSession);

    const response = await agent.post("/encounters/5/vitals").type("form").send({
      _csrf: csrfToken,
      systolicMmHg: "120", diastolicMmHg: "80", oxygenSaturationPct: "98", heartRateBpm: "72", respiratoryRateBpm: "16",
      temperatureValue: "98.6", temperatureUnit: "Fahrenheit", heightValue: "170", heightUnit: "Centimeters",
      weightValue: "70", weightUnit: "Kilograms",
    });

    expect(response.status).toBe(302);
    expect(response.headers.location).toBe("/encounters/5/assessment");
    expect(recordVitalsMock).toHaveBeenCalledWith("nurse-1", ["Nurse"], "5", expect.objectContaining({ systolicMmHg: 120 }));
  });

  it("POST vitals re-renders the form with a 409 error when the stage has already moved on", async () => {
    recordVitalsMock.mockRejectedValue(new apiClient.ApiError(409, "Cannot transition an encounter from AssessmentPending to AssessmentPending."));
    getEncounterMock.mockResolvedValue(encounterAt("AssessmentPending"));
    const { agent, csrfToken } = await agentWithCsrfToken(nurseSession);

    const response = await agent.post("/encounters/5/vitals").type("form").send({ _csrf: csrfToken, systolicMmHg: "120" });

    expect(response.status).toBe(409);
    expect(response.text).toContain("Cannot transition");
  });

  it("GET assessment renders body regions grouped, with no hardcoded region list", async () => {
    getEncounterMock.mockResolvedValue(encounterAt("AssessmentPending"));
    getBodyRegionsMock.mockResolvedValue([
      { id: 1, code: "head", displayName: "Head", regionGroup: "Head/Neck" },
      { id: 2, code: "chest", displayName: "Chest", regionGroup: "Torso" },
    ]);
    const app = createApp({ authMiddleware: fakeAuthMiddleware(nurseSession) });

    const response = await request(app).get("/encounters/5/assessment");

    expect(response.status).toBe(200);
    expect(response.text).toContain("Head/Neck");
    expect(response.text).toContain("Head");
    expect(response.text).toContain("Torso");
    expect(response.text).toContain("Chest");
  });

  it("POST assessment collects checked body regions as an array and advances the encounter", async () => {
    recordAssessmentMock.mockResolvedValue(undefined);
    const { agent, csrfToken } = await agentWithCsrfToken(nurseSession);

    const response = await agent.post("/encounters/5/assessment").type("form").send(
      `_csrf=${encodeURIComponent(csrfToken)}&chiefComplaint=Headache&historyOfPresentIllness=Since+morning&painScale=4&painBodyRegionIds=1&painBodyRegionIds=2`,
    );

    expect(response.status).toBe(302);
    expect(response.headers.location).toBe("/encounters/5");
    expect(recordAssessmentMock).toHaveBeenCalledWith("nurse-1", ["Nurse"], "5", expect.objectContaining({
      painBodyRegionIds: [1, 2],
    }));
  });

  it("GET diagnosis is forbidden for a nurse (RequireDoctor-equivalent guard)", async () => {
    const app = createApp({ authMiddleware: fakeAuthMiddleware(nurseSession) });

    const response = await request(app).get("/encounters/5/diagnosis");

    expect(response.status).toBe(403);
  });

  it("GET diagnosis renders the assessment/vitals summary for a doctor", async () => {
    getEncounterMock.mockResolvedValue(encounterAt("AwaitingDoctor", {
      assessment: { chiefComplaint: "Headache", historyOfPresentIllness: "x", painScale: 4, painBodyRegions: [] },
      vitals: {
        systolicMmHg: 120, diastolicMmHg: 80, oxygenSaturationPct: 98, heartRateBpm: 72, respiratoryRateBpm: 16,
        temperatureValue: 98.6, temperatureUnit: "Fahrenheit", heightValue: 170, heightUnit: "Centimeters",
        weightValue: 70, weightUnit: "Kilograms",
      },
    }));
    const app = createApp({ authMiddleware: fakeAuthMiddleware(doctorSession) });

    const response = await request(app).get("/encounters/5/diagnosis");

    expect(response.status).toBe(200);
    expect(response.text).toContain("Headache");
  });

  it("POST diagnosis finalizes and redirects to the encounter page", async () => {
    finalizeDiagnosisMock.mockResolvedValue(undefined);
    const { agent, csrfToken } = await agentWithCsrfToken(doctorSession);

    const response = await agent.post("/encounters/5/diagnosis").type("form").send({
      _csrf: csrfToken,
      doctorDiagnosis: "Tension headache",
      recommendedTreatment: "Rest",
    });

    expect(response.status).toBe(302);
    expect(response.headers.location).toBe("/encounters/5");
    expect(finalizeDiagnosisMock).toHaveBeenCalledWith("doctor-1", ["Doctor"], "5", "Tension headache", "Rest");
  });

  it("the /report alias renders a printable version of a finalized encounter", async () => {
    getEncounterMock.mockResolvedValue(encounterAt("Finalized", {
      diagnosis: { doctorDiagnosis: "Tension headache", recommendedTreatment: "Rest", finalizedByName: "Dr. Six", finalizedAt: "2026-01-01" },
    }));
    const app = createApp({ authMiddleware: fakeAuthMiddleware(doctorSession) });

    const response = await request(app).get("/encounters/5/report");

    expect(response.status).toBe(200);
    expect(response.text).toContain("Tension headache");
    expect(response.text).toContain("Print report");
    expect(response.text).toContain('class="no-print"');
  });

  it("encounter detail page shows the appropriate next action for the current stage", async () => {
    getEncounterMock.mockResolvedValue(encounterAt("VitalsPending"));
    const app = createApp({ authMiddleware: fakeAuthMiddleware(nurseSession) });

    const response = await request(app).get("/encounters/5");

    expect(response.status).toBe(200);
    expect(response.text).toContain("/encounters/5/vitals");
  });

  it("GET assign-doctor searches for doctors and renders results", async () => {
    searchStaffMock.mockResolvedValue([{ staffMemberId: 9, firstName: "Meredith", lastName: "Grey", specialization: "Surgery" }]);
    const app = createApp({ authMiddleware: fakeAuthMiddleware(nurseSession) });

    const response = await request(app).get("/patients/1/encounters/5/assign-doctor?q=Grey");

    expect(response.status).toBe(200);
    expect(response.text).toContain("Meredith Grey");
    expect(searchStaffMock).toHaveBeenCalledWith("nurse-1", ["Nurse"], { role: "Doctor", q: "Grey" });
  });

  it("POST assign-doctor assigns the selected doctor and redirects to the encounter page", async () => {
    assignDoctorMock.mockResolvedValue(undefined);
    searchStaffMock.mockResolvedValue([{ staffMemberId: 9, firstName: "Meredith", lastName: "Grey", specialization: "Surgery" }]);
    const agent = request.agent(createApp({ authMiddleware: fakeAuthMiddleware(nurseSession) }));

    const formResponse = await agent.get("/patients/1/encounters/5/assign-doctor");
    const csrfToken = extractCsrfToken(formResponse.text);

    const response = await agent.post("/patients/1/encounters/5/assign-doctor").type("form").send({
      _csrf: csrfToken,
      staffMemberId: "9",
    });

    expect(response.status).toBe(302);
    expect(response.headers.location).toBe("/encounters/5");
    expect(assignDoctorMock).toHaveBeenCalledWith("nurse-1", ["Nurse"], "5", 9);
  });
});

function extractCsrfToken(html: string): string {
  const match = html.match(/name="_csrf" value="([^"]+)"/);
  if (!match) {
    throw new Error("Could not find CSRF token in rendered HTML.");
  }
  return match[1];
}
