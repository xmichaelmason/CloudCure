import request from "supertest";
import { setTestEnv } from "../testEnv";
import { fakeAuthMiddleware } from "../fakeAuthMiddleware";
import { createApp } from "../../src/server";
import * as apiClient from "../../src/apiClient";

// Preserve the real ApiError class — a plain jest.mock() automocks it too, leaving
// instances with no working constructor (status/message end up undefined).
jest.mock("../../src/apiClient", () => ({
  ...jest.requireActual("../../src/apiClient"),
  createPatient: jest.fn(),
  listPatients: jest.fn(),
  getPatient: jest.fn(),
}));

setTestEnv();

const createPatientMock = apiClient.createPatient as jest.MockedFunction<typeof apiClient.createPatient>;
const listPatientsMock = apiClient.listPatients as jest.MockedFunction<typeof apiClient.listPatients>;
const getPatientMock = apiClient.getPatient as jest.MockedFunction<typeof apiClient.getPatient>;

const staffSession = { personId: "nurse-1", roles: ["Nurse"], status: "Active" };

describe("patient routes", () => {
  beforeEach(() => {
    createPatientMock.mockReset();
    listPatientsMock.mockReset();
    getPatientMock.mockReset();
  });

  it("GET /patients/new is forbidden for a non-staff session", async () => {
    const app = createApp({ authMiddleware: fakeAuthMiddleware({ personId: "p1", roles: [], status: "Pending" }) });

    const response = await request(app).get("/patients/new");

    expect(response.status).toBe(403);
  });

  it("GET /patients/new renders the intake form with no role field", async () => {
    const app = createApp({ authMiddleware: fakeAuthMiddleware(staffSession) });

    const response = await request(app).get("/patients/new");

    expect(response.status).toBe(200);
    expect(response.text).toContain("name=\"firstName\"");
    expect(response.text).toContain("data-list=\"allergies\"");
  });

  it("POST /patients creates the patient with parsed history-list arrays and redirects to the detail page", async () => {
    createPatientMock.mockResolvedValue({ patientId: 42 });
    const agent = request.agent(createApp({ authMiddleware: fakeAuthMiddleware(staffSession) }));

    const formResponse = await agent.get("/patients/new");
    const csrfToken = extractCsrfToken(formResponse.text);

    const response = await agent.post("/patients").type("form").send(
      "_csrf=" + encodeURIComponent(csrfToken) +
      "&firstName=Ada&lastName=Lovelace&dateOfBirth=1990-01-01&phone=4155552671" +
      "&allergies=Penicillin&allergies=Peanuts&conditions=&medications=&surgeries=",
    );

    expect(response.status).toBe(302);
    expect(response.headers.location).toBe("/patients/42");
    expect(createPatientMock).toHaveBeenCalledWith(
      "nurse-1",
      ["Nurse"],
      expect.objectContaining({
        firstName: "Ada",
        lastName: "Lovelace",
        allergies: ["Penicillin", "Peanuts"],
        conditions: [],
      }),
    );
  });

  it("POST /patients re-renders the form with an error on a validation failure from the API", async () => {
    createPatientMock.mockRejectedValue(new apiClient.ApiError(400, "Invalid input"));
    const agent = request.agent(createApp({ authMiddleware: fakeAuthMiddleware(staffSession) }));

    const formResponse = await agent.get("/patients/new");
    const csrfToken = extractCsrfToken(formResponse.text);

    const response = await agent.post("/patients").type("form").send(
      "_csrf=" + encodeURIComponent(csrfToken) +
      "&firstName=Ada&lastName=Lovelace&dateOfBirth=1990-01-01&phone=not-a-phone",
    );

    expect(response.status).toBe(400);
    expect(response.text).toContain("Invalid input");
  });

  it("GET /patients lists results from a lightweight search", async () => {
    listPatientsMock.mockResolvedValue({
      items: [{ patientId: 1, firstName: "Katherine", lastName: "Johnson", dateOfBirth: "1918-08-26", phoneDisplay: "(415) 555-2671" }],
      page: 1,
      pageSize: 25,
      totalCount: 1,
    });
    const app = createApp({ authMiddleware: fakeAuthMiddleware(staffSession) });

    const response = await request(app).get("/patients?q=Katherine");

    expect(response.status).toBe(200);
    expect(response.text).toContain("Katherine");
    expect(listPatientsMock).toHaveBeenCalledWith("nurse-1", ["Nurse"], { q: "Katherine", page: 1 });
  });

  it("GET /patients/:id renders the patient detail page", async () => {
    getPatientMock.mockResolvedValue({
      patientId: 7,
      firstName: "Rosalind",
      lastName: "Franklin",
      dateOfBirth: "1920-07-25",
      phoneDisplay: "(415) 555-2671",
      email: null,
      emergencyContactName: null,
      emergencyContactPhoneDisplay: null,
      allergies: ["Penicillin"],
      conditions: [],
      medications: [],
      surgeries: [],
    });
    const app = createApp({ authMiddleware: fakeAuthMiddleware(staffSession) });

    const response = await request(app).get("/patients/7");

    expect(response.status).toBe(200);
    expect(response.text).toContain("Rosalind Franklin");
    expect(response.text).toContain("Penicillin");
  });

  it("GET /patients/:id renders a 404 page when the API reports not found", async () => {
    getPatientMock.mockRejectedValue(new apiClient.ApiError(404, "Not found"));
    const app = createApp({ authMiddleware: fakeAuthMiddleware(staffSession) });

    const response = await request(app).get("/patients/999999");

    expect(response.status).toBe(404);
  });
});

function extractCsrfToken(html: string): string {
  const match = html.match(/name="_csrf" value="([^"]+)"/);
  if (!match) {
    throw new Error("Could not find CSRF token in rendered HTML.");
  }
  return match[1];
}
