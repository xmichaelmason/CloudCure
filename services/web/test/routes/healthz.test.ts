import request from "supertest";
import { setTestEnv } from "../testEnv";
import { createApp } from "../../src/server";

describe("GET /healthz", () => {
  it("returns 200 ok", async () => {
    setTestEnv();
    const app = createApp();

    const response = await request(app).get("/healthz");

    expect(response.status).toBe(200);
    expect(response.body).toEqual({ status: "ok" });
  });
});
