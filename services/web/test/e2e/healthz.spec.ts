import { test, expect } from "@playwright/test";

test("healthz endpoint responds ok", async ({ request }) => {
  const response = await request.get("/healthz");

  expect(response.ok()).toBeTruthy();
  expect(await response.json()).toEqual({ status: "ok" });
});
