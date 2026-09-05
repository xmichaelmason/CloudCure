import request from "supertest";
import { setTestEnv } from "../testEnv";
import { fakeAuthMiddleware } from "../fakeAuthMiddleware";
import { createApp } from "../../src/server";

setTestEnv();

describe("GET /", () => {
  it("shows a login link when not authenticated", async () => {
    const app = createApp({ authMiddleware: fakeAuthMiddleware(null) });

    const response = await request(app).get("/");

    expect(response.status).toBe(200);
    expect(response.text).toContain("Log in");
  });

  it("shows a pending-approval notice for a Pending session", async () => {
    const app = createApp({
      authMiddleware: fakeAuthMiddleware({ personId: "p1", roles: [], status: "Pending" }),
    });

    const response = await request(app).get("/");

    expect(response.status).toBe(200);
    expect(response.text).toContain("awaiting admin approval");
  });

  it("shows the dashboard with roles for an Active session", async () => {
    const app = createApp({
      authMiddleware: fakeAuthMiddleware({ personId: "p1", roles: ["Doctor"], status: "Active" }),
    });

    const response = await request(app).get("/");

    expect(response.status).toBe(200);
    expect(response.text).toContain("Doctor");
  });

  it("shows the admin approvals link only for an Admin session", async () => {
    const appAsAdmin = createApp({
      authMiddleware: fakeAuthMiddleware({ personId: "p1", roles: ["Admin"], status: "Active" }),
    });
    const appAsDoctor = createApp({
      authMiddleware: fakeAuthMiddleware({ personId: "p2", roles: ["Doctor"], status: "Active" }),
    });

    const adminResponse = await request(appAsAdmin).get("/");
    const doctorResponse = await request(appAsDoctor).get("/");

    expect(adminResponse.text).toContain("/staff/pending-approvals");
    expect(doctorResponse.text).not.toContain("/staff/pending-approvals");
  });
});
