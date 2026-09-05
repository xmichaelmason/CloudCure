import request from "supertest";
import { setTestEnv } from "../testEnv";
import { fakeAuthMiddleware } from "../fakeAuthMiddleware";
import { createApp } from "../../src/server";
import * as apiClient from "../../src/apiClient";

jest.mock("../../src/apiClient");

setTestEnv();

const listPendingApprovalsMock = apiClient.listPendingApprovals as jest.MockedFunction<typeof apiClient.listPendingApprovals>;
const approveStaffMock = apiClient.approveStaff as jest.MockedFunction<typeof apiClient.approveStaff>;

describe("staff approval routes", () => {
  beforeEach(() => {
    listPendingApprovalsMock.mockReset();
    approveStaffMock.mockReset();
  });

  it("GET /staff/pending-approvals is forbidden for a non-Admin session", async () => {
    const app = createApp({
      authMiddleware: fakeAuthMiddleware({ personId: "p1", roles: ["Doctor"], status: "Active" }),
    });

    const response = await request(app).get("/staff/pending-approvals");

    expect(response.status).toBe(403);
    expect(listPendingApprovalsMock).not.toHaveBeenCalled();
  });

  it("GET /staff/pending-approvals is forbidden for a Pending (no active role) session", async () => {
    const app = createApp({
      authMiddleware: fakeAuthMiddleware({ personId: "p1", roles: [], status: "Pending" }),
    });

    const response = await request(app).get("/staff/pending-approvals");

    expect(response.status).toBe(403);
  });

  it("GET /staff/pending-approvals lists pending applicants for an Admin session", async () => {
    listPendingApprovalsMock.mockResolvedValue([
      { personId: "applicant-1", firstName: "Marie", lastName: "Curie", email: "marie@example.com", requestedAt: "2026-01-01T00:00:00Z" },
    ]);
    const app = createApp({
      authMiddleware: fakeAuthMiddleware({ personId: "admin-1", roles: ["Admin"], status: "Active" }),
    });

    const response = await request(app).get("/staff/pending-approvals");

    expect(response.status).toBe(200);
    expect(response.text).toContain("Marie");
    expect(listPendingApprovalsMock).toHaveBeenCalledWith("admin-1", ["Admin"]);
  });

  it("POST /staff/pending-approvals/:personId/approve approves with the selected role and redirects back", async () => {
    listPendingApprovalsMock.mockResolvedValue([
      { personId: "applicant-1", firstName: "Marie", lastName: "Curie", email: "marie@example.com", requestedAt: "2026-01-01T00:00:00Z" },
    ]);
    approveStaffMock.mockResolvedValue(undefined);
    const agent = request.agent(
      createApp({ authMiddleware: fakeAuthMiddleware({ personId: "admin-1", roles: ["Admin"], status: "Active" }) }),
    );

    const listResponse = await agent.get("/staff/pending-approvals");
    const csrfToken = extractCsrfToken(listResponse.text);

    const response = await agent.post("/staff/pending-approvals/applicant-1/approve").type("form").send({
      _csrf: csrfToken,
      role: "Doctor",
    });

    expect(response.status).toBe(302);
    expect(response.headers.location).toBe("/staff/pending-approvals");
    expect(approveStaffMock).toHaveBeenCalledWith("admin-1", ["Admin"], "applicant-1", "Doctor");
  });
});

function extractCsrfToken(html: string): string {
  const match = html.match(/name="_csrf" value="([^"]+)"/);
  if (!match) {
    throw new Error("Could not find CSRF token in rendered HTML.");
  }
  return match[1];
}
