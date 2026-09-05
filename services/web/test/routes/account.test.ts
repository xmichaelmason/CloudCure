import request from "supertest";
import { setTestEnv } from "../testEnv";
import { fakeAuthMiddleware } from "../fakeAuthMiddleware";
import { createApp } from "../../src/server";
import * as apiClient from "../../src/apiClient";

jest.mock("../../src/apiClient");

setTestEnv();

const registerStaffMock = apiClient.registerStaff as jest.MockedFunction<typeof apiClient.registerStaff>;

describe("account routes", () => {
  beforeEach(() => registerStaffMock.mockReset());

  it("GET /account/pending-approval requires authentication", async () => {
    const app = createApp({ authMiddleware: fakeAuthMiddleware(null) });

    const response = await request(app).get("/account/pending-approval");

    expect(response.status).toBe(302);
    expect(response.headers.location).toContain("/login-stub");
  });

  it("GET /account/pending-approval renders for an authenticated pending user", async () => {
    const app = createApp({
      authMiddleware: fakeAuthMiddleware({ personId: "p1", roles: [], status: "Pending" }),
    });

    const response = await request(app).get("/account/pending-approval");

    expect(response.status).toBe(200);
    expect(response.text).toContain("Awaiting approval");
  });

  it("GET /account/register renders a form with no role field", async () => {
    const app = createApp({
      authMiddleware: fakeAuthMiddleware({ personId: "p1", roles: [], status: "Pending" }),
    });

    const response = await request(app).get("/account/register");

    expect(response.status).toBe(200);
    expect(response.text).toContain("name=\"workEmail\"");
    // The old app's core registration bug was a role selector here — it must never come back.
    expect(response.text).not.toContain("name=\"role\"");
  });

  it("POST /account/register calls registerStaff with the session's personId and redirects to pending-approval", async () => {
    registerStaffMock.mockResolvedValue(undefined);
    const agent = request.agent(
      createApp({ authMiddleware: fakeAuthMiddleware({ personId: "p1", roles: [], status: "Pending" }) }),
    );

    const csrfResponse = await agent.get("/account/register");
    const csrfToken = extractCsrfToken(csrfResponse.text);

    const response = await agent.post("/account/register").type("form").send({
      _csrf: csrfToken,
      workEmail: "a@clinic.example",
      specialization: "General",
      startDate: "2026-01-01",
      educationDegree: "MD",
    });

    expect(response.status).toBe(302);
    expect(response.headers.location).toBe("/account/pending-approval");
    expect(registerStaffMock).toHaveBeenCalledWith(
      "p1",
      [],
      expect.objectContaining({ workEmail: "a@clinic.example", specialization: "General" }),
    );
  });

  it("POST /account/register without a valid CSRF token is rejected", async () => {
    const app = createApp({
      authMiddleware: fakeAuthMiddleware({ personId: "p1", roles: [], status: "Pending" }),
    });

    const response = await request(app).post("/account/register").type("form").send({
      workEmail: "a@clinic.example",
      specialization: "General",
      startDate: "2026-01-01",
      educationDegree: "MD",
    });

    expect(response.status).toBe(403);
    expect(registerStaffMock).not.toHaveBeenCalled();
  });
});

function extractCsrfToken(html: string): string {
  const match = html.match(/name="_csrf" value="([^"]+)"/);
  if (!match) {
    throw new Error("Could not find CSRF token in rendered HTML.");
  }
  return match[1];
}
