import request from "supertest";
import { setTestEnv } from "../testEnv";
import { fakeAuthMiddleware } from "../fakeAuthMiddleware";
import { createApp } from "../../src/server";
import * as apiClient from "../../src/apiClient";

// Preserve the real ApiError class — a plain jest.mock() automocks it too, leaving
// instances with a broken constructor (status/message end up undefined).
jest.mock("../../src/apiClient", () => ({
  ...jest.requireActual("../../src/apiClient"),
  getScreeningTemplate: jest.fn(),
  submitScreening: jest.fn(),
}));

setTestEnv();

const getScreeningTemplateMock = apiClient.getScreeningTemplate as jest.MockedFunction<typeof apiClient.getScreeningTemplate>;
const submitScreeningMock = apiClient.submitScreening as jest.MockedFunction<typeof apiClient.submitScreening>;

const staffSession = { personId: "nurse-1", roles: ["Nurse"], status: "Active" };

const covidTemplate: apiClient.ScreeningTemplate = {
  id: 1,
  code: "covid19_v1",
  title: "COVID-19 Screening",
  questions: [
    { id: 1, displayOrder: 1, questionText: "Are you experiencing a fever?", answerType: "YesNo" },
    { id: 2, displayOrder: 2, questionText: "Have you traveled recently?", answerType: "YesNo" },
  ],
};

describe("screening routes", () => {
  beforeEach(() => {
    getScreeningTemplateMock.mockReset();
    submitScreeningMock.mockReset();
  });

  it("GET /patients/:id/screenings/new is forbidden for a non-staff session", async () => {
    const app = createApp({ authMiddleware: fakeAuthMiddleware({ personId: "p1", roles: [], status: "Pending" }) });

    const response = await request(app).get("/patients/1/screenings/new");

    expect(response.status).toBe(403);
  });

  it("renders every question from the template with no hardcoded question text", async () => {
    getScreeningTemplateMock.mockResolvedValue(covidTemplate);
    const app = createApp({ authMiddleware: fakeAuthMiddleware(staffSession) });

    const response = await request(app).get("/patients/1/screenings/new?template=covid19_v1");

    expect(response.status).toBe(200);
    expect(response.text).toContain("Are you experiencing a fever?");
    expect(response.text).toContain("Have you traveled recently?");
    expect(response.text).toContain('name="answer_1"');
    expect(response.text).toContain('name="answer_2"');
    expect(getScreeningTemplateMock).toHaveBeenCalledWith("nurse-1", ["Nurse"], "covid19_v1");
  });

  it("renders a 404 page when the template code is unknown", async () => {
    getScreeningTemplateMock.mockRejectedValue(new apiClient.ApiError(404, "Not found"));
    const app = createApp({ authMiddleware: fakeAuthMiddleware(staffSession) });

    const response = await request(app).get("/patients/1/screenings/new?template=nope");

    expect(response.status).toBe(404);
  });

  it("POST submits one answer per question and redirects to the patient page", async () => {
    getScreeningTemplateMock.mockResolvedValue(covidTemplate);
    submitScreeningMock.mockResolvedValue({ screeningId: 99 });
    const agent = request.agent(createApp({ authMiddleware: fakeAuthMiddleware(staffSession) }));

    const formResponse = await agent.get("/patients/1/screenings/new");
    const csrfToken = extractCsrfToken(formResponse.text);

    const response = await agent.post("/patients/1/screenings").type("form").send({
      _csrf: csrfToken,
      templateCode: "covid19_v1",
      answer_1: "yes",
      answer_2: "no",
    });

    expect(response.status).toBe(302);
    expect(response.headers.location).toBe("/patients/1");
    expect(submitScreeningMock).toHaveBeenCalledWith(
      "nurse-1",
      ["Nurse"],
      "1",
      "covid19_v1",
      expect.arrayContaining([
        { questionId: 1, answerBool: true },
        { questionId: 2, answerBool: false },
      ]),
    );
  });
});

function extractCsrfToken(html: string): string {
  const match = html.match(/name="_csrf" value="([^"]+)"/);
  if (!match) {
    throw new Error("Could not find CSRF token in rendered HTML.");
  }
  return match[1];
}
